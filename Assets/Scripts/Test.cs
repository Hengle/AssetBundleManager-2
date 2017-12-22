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
                "", response => Debug.Log("文件大小："+response));
        }
        
    }

	// Update is called once per frame
	public void GetObjectFromAB () {
        loader.GetAssetBundleObject("spine", "SpineTest", OnLoadedObject);
	}

    private void OnLoadedObject(Object prefab)
    {
        var go = Instantiate(prefab);
    }
}
