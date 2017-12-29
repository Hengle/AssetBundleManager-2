using System.Collections;
using System.Collections.Generic;
using AssetBundles;
using UnityEngine;
using UnityEngine.UI;

public class Test : MonoBehaviour
{
    public AssetBundleLoader loader;
    public Text LogText;
    public GameObject SpineTestPrefab;

    private GameObject _spineGo;

    public string AssetBundleName, AssetName;

    public GameObject SpineRef;

	// Use this for initialization
	void Awake ()
	{
	    Application.logMessageReceived += (condition, trace, type) =>
	    {
	        LogText.text = condition + "\n" + LogText.text;
	    };

        loader.Init((() => Debug.Log("AssetBundle Init Done")));
	}

    public void ChechNewVersion()
    {
        bool hasNewVersion = loader.IsBundleHaveNewVeresion("spine");
        Debug.Log("新版本 : " + hasNewVersion);
        if (hasNewVersion)
        {
            loader.GetFileDownloadSize("spine" +
                "",
                Debug.LogError,
                response => Debug.Log("文件大小："+response));
        }
        
    }

	public void GetAssetBundleAsset() {

       loader.GetAssetBundleAsset<SkeletonDataAsset>(AssetBundleName, AssetName, Progress, OnError, OnSuccess);
	}

    private void OnSuccess(SkeletonDataAsset asset)
    {
        _spineGo = Instantiate(SpineTestPrefab);
        var spineAnimation = _spineGo.GetComponent<SkeletonAnimation>();
        spineAnimation.skeletonDataAsset = asset;
        //需要reset并remap shader才能正确显示材质
        spineAnimation.Reset();
        loader.RemapShader(spineAnimation.gameObject);
    }

    public void Clear()
    {
        Destroy(_spineGo);
        _spineGo = null;
        Resources.UnloadUnusedAssets();
        loader.UnloadAssetBundle(AssetBundleName);
    }
    
    private void OnError(string msg)
    {
        Debug.LogError(msg);
    }

    private void Progress(float progress)
    {
        Debug.Log(progress);
    }

    public void DeleteRef()
    {
        Destroy(SpineRef);
        SpineRef = null;
        Resources.UnloadUnusedAssets();
    }
}
