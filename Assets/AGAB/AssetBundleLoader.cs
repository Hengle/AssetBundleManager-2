using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using AssetBundles;
using UniRx;
using UnityEngine.Networking;


public class AssetBundleLoader : MonoBehaviour
{
    /// <summary>
    /// 是否使用本地服务器debug
    /// </summary>
    public bool IsLocalServerDebug;
    /// <summary>
    /// 是使用远程服务器下载AB，还是使用本地包内AB
    /// </summary>
    public bool IsFromRemoteServer;

    public string Base_Address = "http://124.254.60.82:10005/AssetBundles/Samples/";

    private Action<float> _progressNotifier;
    private bool _isDownloading = false;

    public void GetAssetBundleAsset<T>(string assetBundleName, string assetName,
        Action<float> progressNotifier, Action<string> onFailed, Action<T> onSuccess) where T : UnityEngine.Object
    {
        StartCoroutine(GetAsset(assetBundleName, assetName, progressNotifier, onFailed, onSuccess));
    }

    /// <summary>
    /// 指定asset bundle是否已有更新的版本，注意如果所依赖的包有更新，也会反馈true
    /// </summary>
    /// <param name="assetBundleName"></param>
    /// <returns></returns>
    public bool IsBundleHaveNewVeresion(string assetBundleName)
    {
        if (!Caching.IsVersionCached(assetBundleName, GetAssetBundleHash(assetBundleName)))
        {
            return true;
        }

        //询问依赖包的缓存情况
        string[] dependencies = GetDependencies(assetBundleName);
        for (int i = 0; i < dependencies.Length; i++)
        {
            if (!Caching.IsVersionCached(dependencies[i], GetAssetBundleHash(dependencies[i])))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 获取在manifest中的指定bundle的版本，也就是最新版本号
    /// </summary>
    /// <param name="assetBundleName"></param>
    /// <returns></returns>
    public Hash128 GetAssetBundleHash(string assetBundleName)
    {
        if (AssetBundleManager.AssetBundleManifestObject == null)
        {
            Debug.LogError("Please initialize AssetBundleManifest by calling AssetBundleManager.Initialize()");
            return new Hash128();
        }
        return AssetBundleManager.AssetBundleManifestObject.GetAssetBundleHash(assetBundleName);
    }

    /// <summary>
    /// 获取一个bundle的所有依赖包名
    /// </summary>
    /// <param name="assetBundleName"></param>
    /// <returns></returns>
    public string[] GetDependencies(string assetBundleName)
    {
        return AssetBundleManager.AssetBundleManifestObject.GetAllDependencies(assetBundleName);
    }

    /// <summary>
    /// 包含所有依赖包的需下载大小
    /// </summary>
    /// <param name="assetBundleName"></param>
    /// <param name="callback"></param>
    public void GetFileDownloadSize(string assetBundleName, Action<string> onFailed, Action<int> onSuccess)
    {
        StartCoroutine(GetFileAndDependenciesSize(assetBundleName, onFailed, onSuccess));
    }

    private IEnumerator GetFileAndDependenciesSize(string assetBundleName, Action<string> onFailed , Action<int> onSuccess)
    {
        int fileSize = 0;
        yield return StartCoroutine(GetFileSize(assetBundleName, onFailed, txtSize => fileSize += int.Parse(txtSize)));
        //询问依赖包
        string[] dependencies = GetDependencies(assetBundleName);
        for (int i = 0; i < dependencies.Length; i++)
        {
            yield return StartCoroutine(GetFileSize(dependencies[i], onFailed, txtSize => fileSize += int.Parse(txtSize)));
        }
        onSuccess(fileSize);
    }
    
    /// <summary>
    /// 获取当前下载进度，0-1
    /// </summary>
    /// <returns></returns>
    public float GetDownloadingProgress()
    {
        //所有下载项的综合下载进度
        var downloads = AssetBundleManager.DownloadingWWWs;
        int downloadCount = downloads.Count;
        if (downloadCount == 0) return 1;

        float allProgress = 0;
        foreach (var download in downloads)
        {
            allProgress += download.Value.progress;
        }
        return allProgress / downloadCount;
    }

    private IEnumerator GetFileSize(string assetBundleName, Action<string> onError, Action<string> onSuccess)
    {   
        string address = AssetBundleManager.BaseDownloadingURL + assetBundleName;
        UnityWebRequest www = UnityWebRequest.Get(address);
        yield return www.Send();
        if (www.isNetworkError)
        {
            Debug.Log(www.error);
            if (onError != null)
            {
                onError(www.error);
            }
        }
        else
        {
            onSuccess(www.GetResponseHeader("content-length"));
        }
    }

    /// <summary>
    /// 所有操作都要在Init得到返回之后
    /// </summary>
    /// <param name="callback"></param>
    public void Init(Action callback)
    {
        StartCoroutine(Initialize(callback));
    }

    // Use this for initialization
    private IEnumerator GetAsset<T>(string assetBundleName, string assetName,
        Action<float> progressNotifier, Action<string> onFailed, Action<T> onSuccess) where T : UnityEngine.Object
    {
        // Load asset.
        yield return StartCoroutine(InstantiateAssetAsync(assetBundleName, assetName, progressNotifier, onFailed, onSuccess));
    }

    // Initialize the downloading url and AssetBundleManifest object.
    protected IEnumerator Initialize(Action callback)
    {
        // Don't destroy this gameObject as we depend on it to run the loading script.
        DontDestroyOnLoad(gameObject);

        if (IsLocalServerDebug)
        {
            AssetBundleManager.SetDevelopmentAssetBundleServer();
        }
        else
        {
            if (IsFromRemoteServer)
            {
                AssetBundleManager.SetSourceAssetBundleURL(Base_Address);
            }
            else
            {
                AssetBundleManager.SetSourceAssetBundleURL(Application.dataPath + "/");
            }
        }

        // Initialize AssetBundleManifest which loads the AssetBundleManifest object.
        var request = AssetBundleManager.Initialize();
        if (request != null)
            yield return StartCoroutine(request);

        if (callback != null)
        {
            callback();
        }
    }

    protected IEnumerator InstantiateAssetAsync<T>(string assetBundleName, string assetName,
        Action<float> progressNotifier, Action<string> onFailed, Action<T> onSuccess) where T : UnityEngine.Object
    {
        // This is simply to get the elapsed time for this phase of AssetLoading.
        float startTime = Time.realtimeSinceStartup;

        // Load asset from assetBundle.
        AssetBundleLoadAssetOperation request = AssetBundleManager.LoadAssetAsync(assetBundleName, assetName, typeof(T));
        if (request == null)
            yield break;
        _progressNotifier = progressNotifier;
        AssetBundleManager.DownloadingErrors.ObserveAdd().Subscribe(errMsg =>
        {
            Debug.Log(errMsg.Value);
            if (onFailed != null)
            {
                onFailed(errMsg.Value);
            }
        });
        _isDownloading = true;
        yield return StartCoroutine(request);
        _isDownloading = false;

        if (request is AssetBundleLoadAssetOperationFull)
        {
            //Full表示是正式环境下进行的加载，在这种情况下，如果有出错，要把错误输出提交出去
            var requestFull = (AssetBundleLoadAssetOperationFull) request;
            if (!string.IsNullOrEmpty(requestFull.DownloadingError) && onFailed != null)
            {
                onFailed(requestFull.DownloadingError);
                yield break;
            }
        }
        
        // Get the asset.
        var asset = request.GetAsset<T>();
        RemapShader(asset);

        // Calculate and display the elapsed time.
        float elapsedTime = Time.realtimeSinceStartup - startTime;
        if (asset == null)
        {
            onFailed("获取Asset失败");
        }
        Debug.Log(assetName + (asset == null ? " was not" : " was") + " loaded successfully in " + elapsedTime + " seconds");

        onSuccess(asset);
    }

    /// <summary>
    /// 因为有时候从ab得到的材质会丢shader，所以在从ab里拿出任何asset后都进行一下所包含材质的shader重新链接
    /// </summary>
    /// <param name="obj"></param>
    public void RemapShader(UnityEngine.Object obj)
    {
        List<Material> listMat = new List<Material>();
        listMat.Clear();

        if (obj is Material)
        {
            Material m = obj as Material;
            listMat.Add(m);
        }

        else if (obj is GameObject)
        {
            GameObject go = obj as GameObject;
            Renderer[] rends = go.GetComponentsInChildren<Renderer>();

            if (null != rends)
            {
                foreach (Renderer item in rends)
                {
                    Material[] materialsArr = item.sharedMaterials;
                    foreach (Material m in materialsArr)
                        listMat.Add(m);
                }
            }
        }

        for (int i = 0; i < listMat.Count; i++)
        {
            Material m = listMat[i];
            if (null == m)
                continue;
            var shaderName = m.shader.name;
            var newShader = Shader.Find(shaderName);
            if (newShader != null)
                m.shader = newShader;
        }
    }

    void Update()
    {
        if (_isDownloading)
        {
            if (_progressNotifier != null)
            {
                _progressNotifier(GetDownloadingProgress());
            }
        }
    }
}
