using System.Collections;
using System.Collections.Generic;
using AssetBundles;
using UnityEngine;

public class Test : MonoBehaviour
{
    public AssetBundleLoader loader;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
	    if (Input.GetKeyUp(KeyCode.Space))
	    {
            loader.GetAssetBundleObject("monkey", "monkey", OnLoadedObject);

        }
	}

    private void OnLoadedObject(GameObject prefab)
    {
        var go = Instantiate(prefab);
    }
}
