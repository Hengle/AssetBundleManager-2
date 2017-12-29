using System.Collections;
using System.Collections.Generic;
using AssetBundles;
using UnityEngine;
using UnityEngine.UI;

public class Test : MonoBehaviour
{
    public AssetBundleLoader loader;
    public Text LogText;

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
        loader.GetAssetBundleAsset<GameObject>("spine", "SpineTest", Progress, OnError, OnSuccess);
	}

    private void OnSuccess(Object prefab)
    {
        var go = Instantiate(prefab);
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
