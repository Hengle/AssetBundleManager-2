using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TDE;
using System.Threading;

public class ResourceMgr : SingletonComponent<ResourceMgr>
{
    //private static ResourceMgr m_inst;
    //public static ResourceMgr GetInst()
    //{
    //    return m_inst;
    //}
    //assetbundle路径
    private string m_AssetBundlePath;
    //缓存的最大数
    private const int m_MaxCechaCount = 10;
    //缓存的列表
    private Hashtable m_ChcheDict = new Hashtable();
    //正在加载的列表
    private Dictionary<string, int> m_LoadingDict = new Dictionary<string, int>();
    //等待加载的对象列表
    private List<WaitLoadAsset> m_WaitList = new List<WaitLoadAsset>();

    private static object m_ObjLock = new object();

    //Resources等待加载列表
    private List<WaitResLoadAsset> m_ResourcesWaitList = new List<WaitResLoadAsset>();

    private List<string> m_ResourcesLoadingList = new List<string>();

    private Hashtable m_ResourcesCechaList = new Hashtable();

    private static object m_ResObjLock = new object();
    //本地资源字典最大数
    private const int MaxResoureceDictConut = 50;
    public void AddLoadAssetObject(string path, string name, Action<UnityEngine.Object> cbFun)
    {
        bool _isHave = m_ChcheDict.Contains(name);
        if (_isHave == false)
        {
            WaitLoadAsset _waitData = new WaitLoadAsset(path, name, cbFun);
            _waitData.m_IsCommon = false;
            m_WaitList.Add(_waitData);
            //StartCoroutine(IELoadAssetBundle(_waitData));
        }
        else
        {
            UnityEngine.Object _object = (UnityEngine.Object)m_ChcheDict[name];
            cbFun(_object);
        }
    }

    public void AddCommonLoadAssetObject(string path, string name, Action<UnityEngine.Object> cbFun)
    {
        bool _isHave = m_ChcheDict.Contains(name);
        if (_isHave == false)
        {
            WaitLoadAsset _waitData = new WaitLoadAsset(path, name, cbFun);
            _waitData.m_IsCommon = true;
            m_WaitList.Add(_waitData);
            //StartCoroutine(IELoadAssetBundle(_waitData));
        }
        else
        {
            UnityEngine.Object _object = (UnityEngine.Object)m_ChcheDict[name];
            cbFun(_object);
        }
    }

    public AssetBundle AddCommonTexture(string texPath)
    {
        string _LoadPath = string.Format("{0}{1}", m_AssetBundlePath, texPath);
        AssetBundle _asset = AssetBundle.LoadFromFile(_LoadPath);
        return _asset;
    }

    IEnumerator IELoadAssetBundle(WaitLoadAsset waitData, bool isPublic)
    {
        bool _isHave = m_ChcheDict.Contains(waitData.AssetName);
        if (_isHave)
        {
            UnityEngine.Object _object = (UnityEngine.Object)m_ChcheDict[waitData.AssetName];
            waitData.CBFun(_object);
            m_LoadingDict.Remove(waitData.AssetName);
        }
        else
        {
            string _LoadPath = string.Format("{0}{1}", m_AssetBundlePath, waitData.Path);
            AssetBundle _asset = AssetBundle.LoadFromFile(_LoadPath);
            yield return _asset;
            UnityEngine.Object _go = _asset.LoadAsset(waitData.AssetName);
            if (!isPublic)
            {
                _asset.Unload(false);
            }

            if (m_ChcheDict.Count > m_MaxCechaCount)
            {
                string[] _DictKeys = new string[1];
                m_ChcheDict.Keys.CopyTo(_DictKeys, 1);
                m_ChcheDict.Remove(_DictKeys[0]);
            }
            m_ChcheDict.Add(waitData.AssetName, _go);
            m_LoadingDict.Remove(waitData.AssetName);
            waitData.CBFun(_go);
        }
    }

    override public void Awake()
    {
        m_AssetBundlePath = string.Format("{0}/{1}/", Application.dataPath, "AssetBundles");
        //ResourceMgr.SetInstance(this.GetComponent<ResourceMgr>());
    }

    // Use this for initialization
    void Start () {
		
	}
	
    /// <summary>
    /// 用于定时器
    /// </summary>
    public void CheckLoadingDict()
    {
        if (m_WaitList.Count > 0)
        {
            int _isChche = 0;
            m_LoadingDict.TryGetValue(m_WaitList[0].AssetName, out _isChche);
            if (_isChche != 1)
            {
                m_LoadingDict.Add(m_WaitList[0].AssetName, 1);
                StartCoroutine(IELoadAssetBundle(m_WaitList[0], m_WaitList[0].m_IsCommon));
                m_WaitList.RemoveAt(0);
            }
        }
    }

	void Update () {
        if (m_WaitList.Count > 0)
        {
            int _isChche = 0;
            m_LoadingDict.TryGetValue(m_WaitList[0].AssetName, out _isChche);
            if (_isChche != 1)
            {
                m_LoadingDict.Add(m_WaitList[0].AssetName, 1);
                StartCoroutine(IELoadAssetBundle(m_WaitList[0], m_WaitList[0].m_IsCommon));
                m_WaitList.RemoveAt(0);
            }
        }
        //Resources
        if (m_ResourcesWaitList.Count > 0)
        {
            if (!m_ResourcesLoadingList.Contains(m_ResourcesWaitList[0].ResName))
            {
                m_ResourcesLoadingList.Add(m_ResourcesWaitList[0].ResName);
                StartCoroutine(IELoadAssetBundle(m_ResourcesWaitList[0]));
            }
        }
	}
    private void DestoryUnusedAssets()
    {
        Resources.UnloadUnusedAssets();
        System.GC.Collect();
    }
    /// <summary>
    /// 获得Resource目录下的资源文件
    /// </summary>
    /// <param name="resName"></param>
    /// <returns></returns>
    public void LoadLocalResoures(string resName, Action<UnityEngine.Object> cbFun)
    {
        if (m_ResourcesCechaList.Count > MaxResoureceDictConut)
        {
            ClearLocalResoures();
        }
        if (m_ResourcesCechaList.Contains(resName))
        {
            cbFun((UnityEngine.Object)m_ResourcesCechaList[resName]);
        }
        else
        {
            //StartCoroutine(IELoadAssetBundle(resName, cbFun));
            m_ResourcesWaitList.Add(new WaitResLoadAsset(resName, cbFun));
        }
    }

    IEnumerator IELoadAssetBundle(WaitResLoadAsset waitAssetData)
    {
        if (m_ResourcesCechaList.Contains(waitAssetData.ResName))
        {
            waitAssetData.CBFun((UnityEngine.Object)m_ResourcesCechaList[waitAssetData.ResName]);
            m_ResourcesLoadingList.Remove(waitAssetData.ResName);
            m_ResourcesWaitList.RemoveAt(0);
        }
        else
        {
            UnityEngine.Object ob = Resources.Load(waitAssetData.ResName);
            yield return ob;
            m_ResourcesCechaList.Add(waitAssetData.ResName, ob);
            m_ResourcesLoadingList.Remove(waitAssetData.ResName);
            waitAssetData.CBFun(ob);
            m_ResourcesWaitList.RemoveAt(0);
        }
    }

    public void ClearAllAssetRes()
    {
        //foreach (string key in m_ResourcesCechaList.Keys)
        //{
        //    m_ResourcesCechaList[key] = null;
        //}
        m_ResourcesCechaList.Clear();
        //foreach (string key in m_ChcheDict.Keys)
        //{
        //    m_ChcheDict[key] = null;
        //}
        m_ChcheDict.Clear();
        DestoryUnusedAssets();
    }

    private void ClearLocalResoures()
    {
        //foreach(string key in m_ResourcesCechaList.Keys)
        //{
        //    m_ResourcesCechaList[key] = null;
        //}
        m_ResourcesCechaList.Clear();
        DestoryUnusedAssets();
    }

    private void ClearLocalAssetBundle()
    {
        //foreach (string key in m_ChcheDict.Keys)
        //{
        //    m_ChcheDict[key] = null;
        //}
        m_ChcheDict.Clear();
        DestoryUnusedAssets();
    }
}
