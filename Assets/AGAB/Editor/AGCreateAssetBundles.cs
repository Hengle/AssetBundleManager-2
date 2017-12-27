using UnityEditor;

public class AGCreateAssetBundles
{
    [MenuItem("Dajia Game/Build AssetBundles")]
    static void BuildAllAssetBundles()
    {
        BuildPipeline.BuildAssetBundles("Assets/AGAssetBundles", BuildAssetBundleOptions.None, BuildTarget.Android);
    }
}