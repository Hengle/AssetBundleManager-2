using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using AssetBundles;
using UnityEngine.Experimental.Networking;


public class AssetBundleLoader : MonoBehaviour
{

    public static string Base_Address = "http://124.254.60.82:10005/AssetBundles/";

    public void GetAssetBundleObject(string assetBundleName, string assetName, Action<GameObject> callback)
    {
        StartCoroutine(GetObject(assetBundleName, assetName, callback));
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
    public static Hash128 GetAssetBundleHash(string assetBundleName)
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
    public static string[] GetDependencies(string assetBundleName)
    {
        return AssetBundleManager.AssetBundleManifestObject.GetAllDependencies(assetBundleName);
    }

    /// <summary>
    /// 包含所有依赖包的需下载大小
    /// </summary>
    /// <param name="assetBundleName"></param>
    /// <param name="callback"></param>
    public void GetFileDownloadSize(string assetBundleName, Action<int> callback)
    {
        StartCoroutine(GetFileAndDependenciesSize(assetBundleName, callback));
    }

    private IEnumerator GetFileAndDependenciesSize(string assetBundleName, Action<int> callback)
    {
        int fileSize = 0;
        yield return StartCoroutine(GetFileSize(assetBundleName, txtSize => fileSize += int.Parse(txtSize)));
        //询问依赖包
        string[] dependencies = GetDependencies(assetBundleName);
        for (int i = 0; i < dependencies.Length; i++)
        {
            yield return StartCoroutine(GetFileSize(dependencies[i], txtSize => fileSize += int.Parse(txtSize)));
        }
        callback(fileSize);
    }

    private IEnumerator GetFileSize(string assetBundleName, Action<string> callback)
    {   
        string address = AssetBundleManager.BaseDownloadingURL + assetBundleName;
        UnityWebRequest www = UnityWebRequest.Get(address);
        yield return www.Send();
        if (www.isError)
        {
            Debug.Log(www.error);
        }
        else
        {
            callback(www.GetResponseHeader("content-length"));
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
    private IEnumerator GetObject(string assetBundleName, string assetName, Action<GameObject> callback)
    {
        // Load asset.
        yield return StartCoroutine(InstantiateGameObjectAsync(assetBundleName, assetName, callback));
    }

    // Initialize the downloading url and AssetBundleManifest object.
    protected IEnumerator Initialize(Action callback)
    {
        // Don't destroy this gameObject as we depend on it to run the loading script.
        DontDestroyOnLoad(gameObject);

        // With this code, when in-editor or using a development builds: Always use the AssetBundle Server
        // (This is very dependent on the production workflow of the project. 
        // 	Another approach would be to make this configurable in the standalone player.)
#if UNITY_EDITOR
        AssetBundleManager.SetDevelopmentAssetBundleServer();
#else
		// Use the following code if AssetBundles are embedded in the project for example via StreamingAssets folder etc:
		//AssetBundleManager.SetSourceAssetBundleURL(Application.dataPath + "/");
		// Or customize the URL based on your deployment or configuration
		AssetBundleManager.SetSourceAssetBundleURL(Base_Address);
#endif

        // Initialize AssetBundleManifest which loads the AssetBundleManifest object.
        var request = AssetBundleManager.Initialize();
        if (request != null)
            yield return StartCoroutine(request);

        if (callback != null)
        {
            callback();
        }
    }

    protected IEnumerator InstantiateGameObjectAsync(string assetBundleName, string assetName, Action<GameObject> _callback)
    {
        // This is simply to get the elapsed time for this phase of AssetLoading.
        float startTime = Time.realtimeSinceStartup;

        // Load asset from assetBundle.
        AssetBundleLoadAssetOperation request = AssetBundleManager.LoadAssetAsync(assetBundleName, assetName, typeof(GameObject));
        if (request == null)
            yield break;
        yield return StartCoroutine(request);

        // Get the asset.
        GameObject prefab = request.GetAsset<GameObject>();
        RemapShader(prefab);

        // Calculate and display the elapsed time.
        float elapsedTime = Time.realtimeSinceStartup - startTime;
        Debug.Log(assetName + (prefab == null ? " was not" : " was") + " loaded successfully in " + elapsedTime + " seconds");

        _callback(prefab);
    }

    /// <summary>
    /// 因为有时候从ab得到的材质会丢shader，所以在从ab里拿出任何asset后都进行一下所包含材质的shader重新链接
    /// </summary>
    /// <param name="obj"></param>
    public static void RemapShader(UnityEngine.Object obj)
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
}
