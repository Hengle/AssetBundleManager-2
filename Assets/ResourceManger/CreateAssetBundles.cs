using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// 自动打包所有资源（设置了Assetbundle Name的资源）
/// </summary>
public class CreateAssetBundles : MonoBehaviour
{

    static string[] m_OutPath = { "Assets/AssetBundles" };
    static string[] m_IntoPath = { "Art/AssetBundle" };

    [MenuItem("AssetBundle/Build AssetBundles")]
    static void BuildAllAssetBundles()
    {
        int _fileCount = 0;
        for (int i = 0; i < m_IntoPath.Length; i++)
        {
            _fileCount = GetAssetBundleConut(m_IntoPath[i]);
            AssetBundleBuild[] _buildMap = new AssetBundleBuild[_fileCount];
            _buildMap[0].assetBundleName = m_IntoPath[0];
            _buildMap[0].assetNames = GetAssetBundleNames(m_IntoPath[i], _fileCount);
            BuildPipeline.BuildAssetBundles(m_OutPath[i], _buildMap, BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.StandaloneWindows);
        }
    }


    [MenuItem("AssetBundle/SetAssetBundleName")]
    public static void SetAssetBundleName()
    {

    }

    /// <summary>
    /// 设置指定的版本目录下所有文件的AssetName
    /// </summary>
    /// <param name="versionDir"></param>
    public static void SetVersionDirAssetName(string versionDir, string assetbundleName)
    {
        var fullPath = Application.dataPath + "/" + versionDir + "/";
        var relativeLen = versionDir.Length + 8; // Assets 长度
        if (Directory.Exists(fullPath))
        {
            EditorUtility.DisplayProgressBar("设置AssetName名称", "正在设置AssetName名称中...", 0f);
            var dir = new DirectoryInfo(fullPath);
            var files = dir.GetFiles("*", SearchOption.AllDirectories);
            for (var i = 0; i < files.Length; ++i)
            {
                var fileInfo = files[i];
                EditorUtility.DisplayProgressBar("设置AssetName名称", "正在设置AssetName名称中...", 1f * i / files.Length);
                if (!fileInfo.Name.EndsWith(".meta"))
                {
                    var basePath = fileInfo.FullName.Substring(fullPath.Length - relativeLen).Replace('\\', '/');
                    var importer = AssetImporter.GetAtPath(basePath);
                    if (importer && importer.assetBundleName != assetbundleName)
                    {
                        importer.assetBundleName = assetbundleName;
                    }
                }
            }
            EditorUtility.ClearProgressBar();
        }
    }

    public static int GetAssetBundleConut(string versionDir)
    {
        int _Count = 0;
        var fullPath = Application.dataPath + "/" + versionDir + "/";
        var relativeLen = versionDir.Length + 8; // Assets 长度
        if (Directory.Exists(fullPath))
        {
            var dir = new DirectoryInfo(fullPath);
            var files = dir.GetFiles("*", SearchOption.AllDirectories);
            for (var i = 0; i < files.Length; ++i)
            {
                var fileInfo = files[i];
                if (!fileInfo.Name.EndsWith(".meta"))
                {
                    _Count++;
                }
            }
        }
        return _Count;
    }

    public static string[] GetAssetBundleNames(string versionDir, int fileCount)
    {
        string[] _fileNames = new string[fileCount];
        var fullPath = Application.dataPath + "/" + versionDir + "/";
        var relativeLen = versionDir.Length + 8; // Assets 长度
        if (Directory.Exists(fullPath))
        {
            var dir = new DirectoryInfo(fullPath);
            var files = dir.GetFiles("*", SearchOption.AllDirectories);
            int _Count = 0;
            for (var i = 0; i < files.Length; ++i)
            {
                var fileInfo = files[i];
                if (!fileInfo.Name.EndsWith(".meta"))
                {
                    string basePath = fileInfo.FullName.Substring(fullPath.Length - relativeLen).Replace('\\', '/');
                    _fileNames[_Count] = basePath;
                    _Count++;
                }
            }
        }
        return _fileNames;
    }

    /// <summary>
    /// 给Asset命名
    /// </summary>
    /// <param name="houName"></param>
    static void SetAssetBundleName(string houName)
    {
        //命名assetBundle名字
        string pathsss = Application.dataPath + "/Asset/";
        Debug.LogError(pathsss);
        var files = Directory.GetFiles(pathsss, "*." + houName + ".meta");
        foreach (var file in files)
        {
            string name = file;
            name = name.Replace(pathsss, "");
            name = name.Replace("." + houName + ".meta", "");
            DoSetAssetBundleName(name, file);
        }
    }

    /// <summary>
    /// 修改bundle名字
    /// </summary>
    /// <param name="path"></param>
    static void DoSetAssetBundleName(string name, string path)
    {
        path = path.Replace("\\", "/");

        StreamReader fs = new StreamReader(path);
        List<string> ret = new List<string>();
        string line;
        while ((line = fs.ReadLine()) != null)
        {
            line = line.Replace("\n", "");
            if (line.IndexOf("assetBundleName:") != -1)
            {
                line = "  assetBundleName: Data/" + name.ToLower();

            }
            if (line.IndexOf("assetBundleVariant:") != -1)
            {
                line = "  assetBundleVariant: " + "bundle".ToLower();

            }
            ret.Add(line);
        }
        fs.Close();

        File.Delete(path);

        StreamWriter writer = new StreamWriter(path + ".tmp");
        foreach (var each in ret)
        {
            writer.WriteLine(each);
        }
        writer.Close();

        File.Copy(path + ".tmp", path);
        File.Delete(path + ".tmp");
    }
}
