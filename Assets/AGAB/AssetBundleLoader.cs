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

    public string Base_Address = "http://124.254.60.82:10005/AssetBundles/Samples/";

    private Action<float> _progressNotifier;
    private bool _isDownloading = false;

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

    private IEnumerator GetFileAndDependenciesSize(string assetBundleName, Action<string> onFailed, Action<int> onSuccess)
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
        //todo 目前不是综合速度，因为manager管理的www会不断清理，所以计数会下降
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
    public void Init(Action callback, bool isFromRemoteServer = true)
    {
        StartCoroutine(Initialize(isFromRemoteServer, callback));
    }


    #region 将获取bundle与获取asset分步骤操作的方式

    /// <summary>
    /// 缓存本地预存的ab包，注意由于当前使用的项目PX里并没有清理的需求，目前缓存的同时也留在内存中
    /// </summary>
    /// <param name="bundleNames"></param>
    /// <param name="onFailed"></param>
    /// <param name="onSuccess"></param>
    /// <param name="progressNotifier"></param>
    public void CacheLocalAssetBundles(string[] bundleNames, Action<float> progressNotifier,
        Action<string> onFailed, Action onSuccess)
    {
        //重新初始化为本地，拉取后再初始化回来
        Init((() =>
        {
            //拉取本地AB包
            int p = 0;
            IterateCacheLocalBundle(bundleNames, p, progressNotifier, onFailed, () =>
            {
                //重新恢复远程设置
                Init(onSuccess);
            });
        }), false);
    }

    private void IterateCacheLocalBundle(string[] bundleNames, int p, Action<float> progressNotifier,
        Action<string> onFailed, Action onSuccess)
    {
        LoadAssetBundle(bundleNames[p], progressNotifier, onFailed, bundle =>
        {
            p++;
            if (p < bundleNames.Length)
            {
                IterateCacheLocalBundle(bundleNames, p, progressNotifier, onFailed, onSuccess);
            }
            else
            {
                onSuccess();
            }
        });
    }
    
    public void LoadAssetBundle(string assetBundleName,
        Action<float> progressNotifier, Action<string> onFailed, Action<LoadedAssetBundle> onSuccess)
    {
        StartCoroutine(LoadAssetBundleAsync(assetBundleName, progressNotifier, onFailed, onSuccess));
    }

    public void GetAssetFromLoadedBundle<T>(string assetBundleName, string assetName,
        out string error, Action<T> onSuccess)
        where T : UnityEngine.Object
    {
        var bundle = AssetBundleManager.GetLoadedAssetBundle(assetBundleName, out error);
        GetAssetFromLoadedBundle<T>(bundle, assetName, onSuccess);
    }

    public void GetAssetFromLoadedBundle<T>(LoadedAssetBundle bundle, string assetName,
        Action<T> onSuccess)
        where T: UnityEngine.Object
    {
        var request = bundle.m_AssetBundle.LoadAssetAsync(assetName, typeof(T));
        request.ObserveEveryValueChanged((bundleRequest => bundleRequest.isDone)).Subscribe((isDone =>
        {
            if (isDone)
            {
                T asset = request.asset as T;
                RemapShader(asset);
                onSuccess(asset);
            }
        }));
    }

    protected IEnumerator LoadAssetBundleAsync(string assetBundleName,
    Action<float> progressNotifier, Action<string> onFailed, Action<LoadedAssetBundle> onSuccess)
    {
        // Load asset from assetBundle.
        AssetBundleLoadBundleOperation request = AssetBundleManager.LoadBundleAsync(assetBundleName);
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

        var bundle = request.GetLoadedBundle();
        if (bundle == null)
        {
            onFailed("获取Bundle失败");
        }
        onSuccess(bundle);
    }

    #endregion

#region 按照manager的思路，自动连续进行bundle和asset获取的方式

    public void GetAssetBundleAsset<T>(string assetBundleName, string assetName,
        Action<float> progressNotifier, Action<string> onFailed, Action<T> onSuccess) where T : UnityEngine.Object
    {
        StartCoroutine(GetAsset(assetBundleName, assetName, progressNotifier, onFailed, onSuccess));
    }

    private IEnumerator GetAsset<T>(string assetBundleName, string assetName,
        Action<float> progressNotifier, Action<string> onFailed, Action<T> onSuccess) where T : UnityEngine.Object
    {
        // Load asset.
        yield return StartCoroutine(InstantiateAssetAsync(assetBundleName, assetName, progressNotifier, onFailed, onSuccess));
    }

    // Initialize the downloading url and AssetBundleManifest object.
    protected IEnumerator Initialize(bool isFromRemoteServer, Action callback)
    {
        // Don't destroy this gameObject as we depend on it to run the loading script.
        DontDestroyOnLoad(gameObject);

        if (IsLocalServerDebug)
        {
            AssetBundleManager.SetDevelopmentAssetBundleServer();
        }
        else
        {
            if (isFromRemoteServer)
            {
                AssetBundleManager.SetSourceAssetBundleURL(Base_Address);
            }
            else
            {
                #if UNITY_EDITOR || UNITY_STANDALONE                
                AssetBundleManager.SetSourceAssetBundleURL("file://"+Application.dataPath + "/StreamingAssets/");
                #elif UNITY_ANDROID
                AssetBundleManager.SetSourceAssetBundleURL("jar:file://" + Application.dataPath + "!/assets/");
                #elif UNITY_IOS
                AssetBundleManager.SetSourceAssetBundleURL(Application.dataPath + "/Raw/");
                #endif
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
            var requestFull = (AssetBundleLoadAssetOperationFull)request;
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

    #endregion

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

    public void UnloadAssetBundle(string bundleName)
    {
        AssetBundleManager.UnloadAssetBundle(bundleName);
    }
}
