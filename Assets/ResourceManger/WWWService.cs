using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TDE;
using UnityEngine;

public class WWWService : SingletonComponent<WWWService>
{
    /// <summary>
    /// 根据url，下载对应的字符串。
    /// </summary>
    /// <param name="url">url</param>
    /// <param name="onComplete">
    /// bool:是否下载成功
    /// string:下载到的字符串
    /// string:下载失败时的错误信息
    /// </param>
    public void LoadString(string url, Action<LoadStringResult> onComplete)
    {
        url = Uri.EscapeUriString(url);
        this.downLoad(url, (w) =>
        {
            LoadStringResult result = new LoadStringResult();
            result.IsSuccess = string.IsNullOrEmpty(w.error);

            if (result.IsSuccess)
            {
                result.Text = w.text;
            }
            else
            {
                result.ErrorText = w.error;
            }

            onComplete(result);
        });
    }

    public void LoadUTF8String(string url, Action<LoadStringResult> onComplete)
    {
        url = Uri.EscapeUriString(url);
        this.downLoad(url, (w) =>
        {
            LoadStringResult result = new LoadStringResult();
            result.IsSuccess = string.IsNullOrEmpty(w.error);

            if (result.IsSuccess)
            {
                var buffer = w.bytes;
                MemoryStream ms = new MemoryStream(w.bytes);
                StreamReader reader = new StreamReader(ms, true);
                var text = reader.ReadToEnd();
                reader.Close();
                ms.Close();

                result.Text = text;

                //Debug.Log(text);
            }
            else
            {
                result.ErrorText = w.error;
            }

            onComplete(result);
        });
    }

    /// <summary>
    /// 根据url,下载二进制数据。
    /// </summary>
    /// <param name="url">url</param>
    /// <param name="onComplete">
    /// bool:是否下载成功
    /// string:错误信息
    /// byte[]:下载到的二进制数据.
    /// </param>
    public void LoadBytes(string url, Action<LoadBytesResult> onComplete)
    {
        url = Uri.EscapeUriString(url);
        this.downLoad(url, (w) =>
        {
            LoadBytesResult result = new LoadBytesResult();
            result.IsSuccess = string.IsNullOrEmpty(w.error);
            if (result.IsSuccess)
            {
                result.Bytes = w.bytes;
            }
            else
            {
                result.ErrorText = w.error;
            }
            onComplete(result);
            w.Dispose();
        });
    }


    public void LoadBytes(string url, Action<LoadBytesResult> onComplete, Action<float> onProgressBytes)
    {
        url = Uri.EscapeUriString(url);
        this.downLoad(url, (w) =>
        {
            LoadBytesResult result = new LoadBytesResult();
            result.IsSuccess = string.IsNullOrEmpty(w.error);
            if (result.IsSuccess)
            {
                result.Bytes = w.bytes;
            }
            else
            {
                result.ErrorText = w.error;
            }
            onComplete(result);
        }, onProgressBytes);
    }

    void downLoad(string url, Action<WWW> onComplete)
    {
        StartCoroutine(this.beginDownLoad(url, onComplete));

    }

    void downLoad(string url, Action<WWW> onComplete, Action<float> onLoadedLength)
    {

        StartCoroutine(this.beginDownLoad(url, onLoadedLength,
             (w) =>
             {
                 onComplete(w);
             }));

    }

    IEnumerator beginDownLoad(string url, Action<WWW> onComplete)
    {
        WWW w = new WWW(url);
        yield return w;
        onComplete(w);
        w.Dispose();
        w = null;
    }

    IEnumerator beginDownLoad(string url, Action<float> onProgress, Action<WWW> onComplete)
    {
        WWW w = new WWW(url);
        //w.threadPriority = ThreadPriority.Low;

        while (!w.isDone)
        {
            //Debug.LogError(w.progress);
            onProgress(w.progress);
            yield return 1;
        }
        onComplete(w);
        w.Dispose();
        w = null;
        yield return null;
    }

    /// <summary>
    /// 只用于www读取.
    /// </summary>
    /// <param name="persistentPath"></param>
    /// <returns></returns>
    string createFullPersistentWWWPath(string persistentPath)
    {
        string returnValue = string.Empty;
        switch (Application.platform)
        {
            case RuntimePlatform.Android:
                returnValue = "file://" + (this.PersistentDataPath + "/" + persistentPath).TrimEnd('/');
                break;
            case RuntimePlatform.IPhonePlayer:
                returnValue = "file://" + (this.PersistentDataPath + "/" + persistentPath).TrimEnd('/');
                break;
            case RuntimePlatform.OSXEditor:
                returnValue = "file://" + (this.PersistentDataPath + "/" + persistentPath).TrimEnd('/');
                break;
            case RuntimePlatform.WindowsPlayer:
            case RuntimePlatform.WindowsEditor:
            default:
                returnValue = "file://" + (this.PersistentDataPath + "/" + persistentPath).TrimEnd('/').Replace('/', '\\');
                break;
        }

        return returnValue;
    }

    private string _persistentDataPath;
    /// <summary>
    /// 每调用一次Application.persistentDataPath，耗费140B内存，花费0.2毫秒左右的CPU时间。
    /// </summary>
    public string PersistentDataPath
    {
        get
        {
            if (string.IsNullOrEmpty(_persistentDataPath))
            {
                _persistentDataPath = Application.persistentDataPath;
            }
            return _persistentDataPath;
        }
    }

    public string CreateFullStreamingWWWPath(string resourcePath)
    {
        var streamingPath = resourcePath;
        if (streamingPath.StartsWith("StreamingAssets/"))
        {
            streamingPath = streamingPath.Substring("StreamingAssets/".Length);
        }

        string returnValue = string.Empty;
        switch (Application.platform)
        {
            case RuntimePlatform.Android:
                returnValue = Application.streamingAssetsPath + "/" + streamingPath;
                break;
            case RuntimePlatform.IPhonePlayer:
                //returnValue = Application.streamingAssetsPath + "/" + streamingPath;
                returnValue = "file:///" + Application.streamingAssetsPath + "/" + streamingPath;
                break;
            case RuntimePlatform.OSXEditor:
            case RuntimePlatform.WindowsPlayer:
            case RuntimePlatform.WindowsEditor:
            default:
                returnValue = "file:///" + Application.streamingAssetsPath + "/" + streamingPath;
                break;
        }
        return returnValue;
    }

    public void CreateAssetBundleRequest(byte[] buffer, Action<AssetBundleCreateRequest> onComplete)
    {
        StartCoroutine(this.beginCreateAssetBundleRequest(buffer, (rqt) =>
        {
            onComplete(rqt);
        }));
    }
    public void LoadStreamAssetBundle(string name, string path, Action<bool, string, AssetBundle> onComplete)
    {
        ResourseItemInfo info = new ResourseItemInfo();
        info.ResoursePath = path;
        info.ResourceName = name;
        this.LoadStreamingAssetsBundle(info, onComplete);
    }

    public void LoadAssetBundle(ResourseItemInfo resourseItemInfo, Action<bool, string, AssetBundle> onComplete)
    {

        this.LoadStreamingAssetsBundle(resourseItemInfo, onComplete);
    }

    public void LoadPersistentAssetBundle(ResourseItemInfo resourseItemInfo, Action<bool, string, AssetBundle> onComplete)
    {
        var wwwPersistentPath = this.createFullPersistentWWWPath(resourseItemInfo.ResourseRelativePath);

        var time = DateTime.Now;
        string s1 = string.Empty;
        string s2 = string.Empty;
        string s3 = string.Empty;
        string s4 = string.Empty;

        /*if (GameBaseController.IsLogLoadAssetInfo)
        {
            s1 = "开始加载文件：" + wwwPersistentPath;
        }*/

        StartCoroutine(this.beginDownLoad(wwwPersistentPath, (w) =>
        {
            if (string.IsNullOrEmpty(w.error))
            {
                var buffer = w.bytes;
                StartCoroutine(this.beginCreateAssetBundleRequest(buffer, (rqt) =>
                {
                    onComplete(true, string.Empty, rqt.assetBundle);
                    //GameSystem.Instance.netMessageController.WaitForEndOfFame(() => { rqt.assetBundle.Unload(false); });
                }));
            }
            else
            {
                onComplete(false, w.error, null);
            }
        }));
    }

    public void LoadStreamingAssetsString(string streammingAssetPath, Action<LoadStringResult> onComplete)
    {
        var wwwPath = this.CreateFullStreamingWWWPath(streammingAssetPath);

        this.LoadUTF8String(wwwPath, onComplete);
    }
    public void LoadStreamingAssetsBundle(ResourseItemInfo resourseItemInfo, Action<bool, string, AssetBundle> onComplete)
    {
        var wwwPath = this.CreateFullStreamingWWWPath(resourseItemInfo.ResoursePath);

        StartCoroutine(this.beginDownLoad(wwwPath, (w) =>
        {

            if (string.IsNullOrEmpty(w.error))
            {


                onComplete(true, string.Empty, w.assetBundle);


            }
            else
            {
                Debug.LogError(w.error);
                onComplete(false, w.error, null);
            }
        }));
    }

    IEnumerator beginCreateAssetBundleRequest(byte[] buffer, Action<AssetBundleCreateRequest> onComplete)
    {

        var request = AssetBundle.LoadFromMemoryAsync(buffer);
        yield return request;
        onComplete(request);
    }
}

public struct LoadStringResult
{
    private bool _isSuccess;
    public bool IsSuccess
    {
        get { return _isSuccess; }
        set { _isSuccess = value; }
    }

    private string _errorText;
    public string ErrorText
    {
        get { return _errorText; }
        set { _errorText = value; }
    }

    private string _text;

    public string Text
    {
        get { return _text; }
        set { _text = value; }
    }
}

public struct LoadBytesResult
{
    private bool _isSuccess;

    public bool IsSuccess
    {
        get { return _isSuccess; }
        set { _isSuccess = value; }
    }

    private string _errorText;

    public string ErrorText
    {
        get { return _errorText; }
        set { _errorText = value; }
    }

    private byte[] _bytes;

    internal byte[] Bytes
    {
        get { return _bytes; }
        set { _bytes = value; }
    }
}

public struct LoadImageResult
{
    private bool _isSuccess;

    public bool IsSuccess
    {
        get { return _isSuccess; }
        set { _isSuccess = value; }
    }

    private string _errorText;

    public string ErrorText
    {
        get { return _errorText; }
        set { _errorText = value; }
    }

    private Texture2D _texture2D;

    internal Texture2D Texture2D
    {
        get { return _texture2D; }
        set { _texture2D = value; }
    }
}
