using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using AssetBundles;


public class AssetBundleLoader : MonoBehaviour
{
    private Action<GameObject> _callback;
    private bool _isInited = false;

    public void GetAssetBundleObject(string assetBundleName, string assetName, Action<GameObject> _callback)
    {
        StartCoroutine(GetObject(assetBundleName, assetName, _callback));
    }

    // Use this for initialization
    private IEnumerator GetObject(string assetBundleName, string assetName, Action<GameObject> _callback)
    {
        if (!_isInited)
        {
            yield return StartCoroutine(Initialize());
            _isInited = true;
        }

        // Load asset.
        yield return StartCoroutine(InstantiateGameObjectAsync(assetBundleName, assetName, _callback));
    }

    // Initialize the downloading url and AssetBundleManifest object.
    protected IEnumerator Initialize()
    {
        // Don't destroy this gameObject as we depend on it to run the loading script.
        DontDestroyOnLoad(gameObject);

        // With this code, when in-editor or using a development builds: Always use the AssetBundle Server
        // (This is very dependent on the production workflow of the project. 
        // 	Another approach would be to make this configurable in the standalone player.)
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        AssetBundleManager.SetDevelopmentAssetBundleServer();
#else
		// Use the following code if AssetBundles are embedded in the project for example via StreamingAssets folder etc:
		//AssetBundleManager.SetSourceAssetBundleURL(Application.dataPath + "/");
		// Or customize the URL based on your deployment or configuration
		AssetBundleManager.SetSourceAssetBundleURL("http://www.MyWebsite/MyAssetBundles");
#endif

        // Initialize AssetBundleManifest which loads the AssetBundleManifest object.
        var request = AssetBundleManager.Initialize();
        if (request != null)
            yield return StartCoroutine(request);
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
        ResetShader(prefab);

        // Calculate and display the elapsed time.
        float elapsedTime = Time.realtimeSinceStartup - startTime;
        Debug.Log(assetName + (prefab == null ? " was not" : " was") + " loaded successfully in " + elapsedTime + " seconds");

        _callback(prefab);
    }

    /// <summary>
    /// 因为有时候从ab得到的材质会丢shader，所以在从ab里拿出任何asset后都进行一下所包含材质的shader重新链接
    /// </summary>
    /// <param name="obj"></param>
    public static void ResetShader(UnityEngine.Object obj)

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
