using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

public class ResourseItemInfo
{
    public AssetLoadState testState = AssetLoadState.None;
    public int mainCount = 0;
    public int currentCount = 0;
    public bool isUnload = false;
    private bool isInit;
    public bool IsInit
    {
        get { return isInit; }

        set { isInit = value; }

    }
    private string _resourceID;
    public object ob = new object();
    public string ResourceID
    {
        get { return _resourceID; }
        set { _resourceID = value; }
    }

    private string _lastWriteTime;

    public string LastWriteTime
    {
        get { return _lastWriteTime; }
        set { _lastWriteTime = value; }
    }

    private bool _isInObjectPool = false;
    public bool IsInObjectPool
    {
        get { return _isInObjectPool; }
        set { _isInObjectPool = value; }
    }

    private int _objectPoolMinSize = 4;
    public int ObjectPoolMinSize
    {
        get { return _objectPoolMinSize; }
        set { _objectPoolMinSize = value; }
    }

    private int _ObjectPoolMaxSize = 8;
    public int ObjectPoolMaxSize
    {
        get { return _ObjectPoolMaxSize; }
        set { _ObjectPoolMaxSize = value; }
    }

    private bool _isDependence;

    public bool IsDependence
    {
        get { return _isDependence; }
        set { _isDependence = value; }
    }

    public int _retainCount = 0;
    public ResourseItemInfo BeginRetain()
    {
        lock (m_lock)
        {
            _retainCount++;
           // Debug.LogError(this.ResoursePath + ":" + _retainCount);
            return this;
        }
    }
    public object m_lock = new object();
    public void EndRetain(bool isDestroy = false)
    {
        lock(m_lock)
        {
            _retainCount--;
            _retainCount = Mathf.Clamp(_retainCount, 0, _retainCount);
        }
       

      
    }

    private bool isUnLoadFalse = false;
    /// <summary>
    /// 当加载完毕（大厅或者战场场景）后，是否立即Unload(false)掉当前assetBundle
    /// </summary>
    public bool IsUnLoadFalse
    {
        get { return isUnLoadFalse; }
        set { isUnLoadFalse = value; }
    }


    public bool IsCanUnload()
    {
        lock(m_lock)
        {
            return this._retainCount <= 0;
        }
       
    }

    private List<string> _subAssets;

    public List<string> SubAssets
    {
        get
        {
            if (_subAssets == null)
            {
                _subAssets = new List<string>();
            }
            return _subAssets;
        }
    }

    //为了不序列化，不搞public 属性
    List<ResourseItemInfo> _subResItemInfo = new List<ResourseItemInfo>();

    public void AddSubResItemInfo(ResourseItemInfo resItemInfo)
    {
       
            _subResItemInfo.Add(resItemInfo);
     
  
    }

    public List<ResourseItemInfo> GetSubResItemInfo()
    {
        return _subResItemInfo;
    }

    //将来，可以考虑把父资源传进来，根据父资源的所有子资源状态，决定是否可以开始加载。
    public bool IsCanBeginLoadFromDisk()
    {
        if(this.CurAssetLoadState==AssetLoadState.Loading)
        {
            return false;
        }
        bool returnValue = false;

        if (this.IsDependence)//如果是子资源，则只有当前还没开始加载，才认为可以开始加载。
        {
            if (this.CurAssetLoadState == AssetLoadState.NotLoad)
            {
                return true;
            }
            else
            {
                return false;

            }

        }
        else
        {
            //主资源
            if (this.GetSubResItemInfo().Count > 0)
            {
                var unFinishedSubResItemsCount = this.GetSubResItemInfo().Count(p => p.CurAssetLoadState != AssetLoadState.Loaded);//如果有子资源，则必须所有的子资源都加载完毕，才能加载主资源。
                if (unFinishedSubResItemsCount == 0)
                {
                    returnValue = this.CurAssetLoadState == AssetLoadState.NotLoad;//子资源已加载完毕，并且当前主资源还没有开始加载，则当前主资源可以开始加载。
                }
                else
                {
                    returnValue = false;
                }
            }
            else
            {
                returnValue = this.CurAssetLoadState == AssetLoadState.NotLoad;//如果没有子资源，则只要主资源还没开始加载，就认为可以开始加载。
            }
        }

        return returnValue;
    }

    private AssetBundle _assetBundle;
    public AssetBundle AssetBundle
    {
        get { return _assetBundle; }
        set {

            _assetBundle = value;


        }
    }

    private AssetLoadState _curAssetLoadState = AssetLoadState.NotLoad;
    public AssetLoadState CurAssetLoadState
    {
        get
        {
            //if(_curAssetLoadState!=AssetLoadState.Loading)
            //{
            //    if (!isUnLoadFalse)
            //    {
            //        if (this.AssetBundle == null)
            //        {
            //            return AssetLoadState.NotLoad;
            //        }

            //    }
            //    else
            //    {
            //        if(mainAssetObj==null)
            //        {
            //            return AssetLoadState.NotLoad;
            //        }

            //    }

            //}

            return _curAssetLoadState;
        }
        set { _curAssetLoadState = value; }
    }

    private string _resourceName;

    public string ResourceName
    {
        get { return _resourceName; }
        set { _resourceName = value; }
    }

    private int _resourceLength;

    public int ResourceLength
    {
        get { return _resourceLength; }
        set { _resourceLength = value; }
    }

    private string _resoursePath;

    public string ResoursePath
    {
        get { return _resoursePath; }
        set { _resoursePath = value; }
    }

    public string ResourseRelativePath
    {
        get
        {
            return "ResourseManager/ResourceList/" + _resoursePath;
        }
    }
    private UnityEngine.Object mainAssetObj;

    public UnityEngine.Object MainAssetObj
    {
       
        get { return this.mainAssetObj; }
        set { this.mainAssetObj = value; }

    }
    public string ResourseLocalPersistentPath
    {
        get
        {
          
            return string.Empty;// GameSystem.Instance.ResourseManager.IOService.CreateFullPersistentFilePath(this.ResourseRelativePath);
        }
    }

    public bool IsLocalExist()
    {
        return true;//GameSystem.Instance.ResourseManager.IOService.IsPersistentFileExist(this.ResourseRelativePath);
    }

    public string ResourseWebPath
    {
        get
        {
            return string.Empty;// GameSystem.Instance.UserSettingManager.ResourseServerBaseAddress + GameSystem.Instance.CommonService.PlatformString + "/" + this.ResourseRelativePath + "?Ver=" + this.MD5;
        }
    }

    private string _md5;

    public string MD5
    {
        get { return _md5; }
        set { _md5 = value; }
    }
  
    public void ReleaseAsset(bool unloadAllLoadedObjects)
    {

       // this.CurAssetLoadState = AssetLoadState.NotLoad;
    
        if(this.AssetBundle==null)
        {
            return;
        }
        else
        {
            this.AssetBundle.Unload(unloadAllLoadedObjects);
        }
      


    }

    public bool IsSame(ResourseItemInfo otherResourceItemInfo)
    {

    
        bool returnValue = true;

        if (otherResourceItemInfo != null)
        {
            if (this.ResourceID != otherResourceItemInfo.ResourceID)
            {
                returnValue = false;
            }
            //else if (this.ResourseRelativePath != otherResourceItemInfo.ResourseRelativePath)//如果只是挪动了 streamingAsset到 ResourceList其他目录，则是可以不用更新的。
            //{
            //    returnValue = false;
            //}
            else if (this.MD5 != otherResourceItemInfo.MD5)
            {
                returnValue = false;
            }
            //else if (this.ResourceName != otherResourceItemInfo.ResourceName)
            //{
            //    returnValue = false;
            //}
        }
        else
        {
            returnValue = false;
        }

        return returnValue;
    }
    //初始化材质球
    private bool _isInitShader = false;
    public bool IsInitShader
    {
        set
        {
            if (!_isInitShader)
            {
                _isInitShader = value;
            }

       
        }
        get {


            return _isInitShader;
        }


    }

}


public class ResourseItemInfoEqualityComparer : IEqualityComparer<ResourseItemInfo>
{
    public bool Equals(ResourseItemInfo x, ResourseItemInfo y)
    {
        if (x == null && y == null)
        {
            return true;
        }
        else if (x != null && y != null)
        {
            return x.ResourseRelativePath == y.ResourseRelativePath;
        }
        else
        {
            return false;
        }
    }

    public int GetHashCode(ResourseItemInfo obj)
    {
        if (obj == null)
        {
            return 0;
        }

        return obj.ResourseRelativePath.GetHashCode();
    }
}

/// <summary>
/// 当前资源 assetBundle 加载状态
/// </summary>
public enum AssetLoadState
{
    None = 0,

    /// <summary>
    /// 还没加载
    /// </summary>
    NotLoad,

    /// <summary>
    /// 正在加载
    /// </summary>
    Loading,

    /// <summary>
    /// 已经加载成功，等待回调中。
    /// 因为IO支持了多任务并发执行。
    /// </summary>
    LoadSuccessWaitCallback,

    /// <summary>
    /// 已经加载
    /// </summary>
    Loaded,
    UnLoadTrue,
    UnLoadFalse


}
