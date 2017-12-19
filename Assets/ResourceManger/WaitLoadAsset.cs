using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaitLoadAsset{
    public WaitLoadAsset(string path, string name, Action<UnityEngine.Object> cbFun)
    {
        m_Path = path;
        m_Name = name;
        m_CBFun = cbFun;
    }
    private string m_Path;
    public string Path
    {
        get { return m_Path; }
    }
    private string m_Name;
    public string AssetName
    {
        get { return m_Name; }
    }
    private Action<UnityEngine.Object> m_CBFun;
    public Action<UnityEngine.Object> CBFun
    {
        get { return m_CBFun; }
    }
    //是否加载同一个资源
    public bool m_IsLoading = false;
    //是否是公共资源
    public bool m_IsCommon = false;


}


public class WaitResLoadAsset
{
    public WaitResLoadAsset(string resName, Action<UnityEngine.Object> cBFun)
    {
        m_ResName = resName;
        m_CBFun = cBFun;
    }

    private string m_ResName;
    public string ResName
    {
        get { return m_ResName; }
    }
    private Action<UnityEngine.Object> m_CBFun;
    public Action<UnityEngine.Object> CBFun
    {
        get { return m_CBFun; }
    }
}
