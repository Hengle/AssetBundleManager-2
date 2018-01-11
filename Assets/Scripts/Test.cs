using System.Collections;
using System.Collections.Generic;
using AssetBundles;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.UI;

public class Test : MonoBehaviour
{
    public AssetBundleLoader loader;
    public Text LogText;
    public GameObject SpineTestPrefab;

    private GameObject _spineGo;

    public string AssetBundleName, AssetName;

	// Use this for initialization
	void Awake ()
	{
        //Application.logMessageReceived += (condition, trace, type) =>
        //{
        //    LogText.text = condition + "\n" + LogText.text;
        //};

        loader.Init((() => Debug.Log("AssetBundle Init Done")));
	}

#region 按钮方法

    public void ChechABNewVersion()
    {
        bool hasNewVersion = loader.IsBundleHaveNewVeresion(AssetBundleName);
        Debug.Log("新版本 : " + hasNewVersion);
        if (hasNewVersion)
        {
            loader.GetFileDownloadSize(AssetBundleName,
                Debug.LogError,
                response => Debug.Log("文件大小："+response));
        }
        
    }

    public void LoadAssetBundle()
    {
        loader.LoadAssetBundle(AssetBundleName, Progress, OnError, 
            bundle => Debug.Log(bundle.m_AssetBundle.name + " Loaded"));
    }

    public void LoadAssetBundleLocal()
    {
        loader.CacheLocalAssetBundles(new[]{AssetBundleName}, Progress, OnError, () =>
        {
            Debug.Log("完成");
        } );
    }

    public void GetAssetFromLoadedBundle()
    {
        string error;
        loader.GetAssetFromLoadedBundle<SkeletonDataAsset>(AssetBundleName, AssetName, out error, OnSuccess);
    }

    public void GetAssetBundleAsset() {

       loader.GetAssetBundleAsset<SkeletonDataAsset>(AssetBundleName, AssetName, Progress, OnError, OnSuccess);
	}

    public void Clear()
    {
        Destroy(_spineGo);
        _spineGo = null;
        Resources.UnloadUnusedAssets();
    }

#endregion

    private void OnSuccess(SkeletonDataAsset asset)
    {
        Debug.Log("获取到Asset");
        _spineGo = Instantiate(SpineTestPrefab);
        var spineAnimation = _spineGo.GetComponent<SkeletonAnimation>();
        spineAnimation.skeletonDataAsset = asset;
        //需要reset并remap shader才能正确显示材质
        spineAnimation.Reset();
        loader.RemapShader(spineAnimation.gameObject);
    }

    private void OnError(string msg)
    {
        Debug.LogError(msg);
    }

    private void Progress(float progress)
    {
        Debug.Log(progress);
    }

}
