using UnityEditor;
using UnityEngine;
using System.IO;

public class LoadBundleEditor : EditorWindow
{
    [MenuItem("Tools/Unpack AssetBundle (Preserve Data)")]
    public static void UnpackBundleWithData()
    {
        // 1. Prompt file selection browser for the bundle
        string filePath = EditorUtility.OpenFilePanel("Select AssetBundle File", "", "");
        if (string.IsNullOrEmpty(filePath))
        {
            Debug.LogWarning("Unpacking cancelled: No file selected.");
            return;
        }

        // 2. Generate a clean destination folder
        string bundleName = Path.GetFileNameWithoutExtension(filePath);
        string relativeTargetFolder = $"Assets/Unpacked_{bundleName}";
        string absoluteTargetFolder = Path.Combine(Application.dataPath, $"Unpacked_{bundleName}");

        if (!Directory.Exists(absoluteTargetFolder))
        {
            Directory.CreateDirectory(absoluteTargetFolder);
        }

        // 3. Load the bundle directly into memory
        AssetBundle bundle = AssetBundle.LoadFromFile(filePath);
        if (bundle == null)
        {
            Debug.LogError($"Failed to load AssetBundle from path: {filePath}");
            return;
        }

        // 4. Read internal layout without spawning copies into a scene
        string[] assetNames = bundle.GetAllAssetNames();
        int unpackedCount = 0;

        foreach (string assetPath in assetNames)
        {
            // Only attempt to process GameObject prefabs to prevent corrupting textures/audio
            if (!assetPath.EndsWith(".prefab")) continue;

            GameObject prefabAsset = bundle.LoadAsset<GameObject>(assetPath);
            if (prefabAsset == null) continue;

            string fileName = Path.GetFileName(assetPath);
            string savePath = Path.Combine(relativeTargetFolder, fileName);

            // Bypasses 'Instantiate' to retain raw file data, references, and serialized variables
            PrefabUtility.SaveAsPrefabAsset(prefabAsset, savePath);
            unpackedCount++;
        }

        // 5. Unload binary memory structures and notify the AssetDatabase engine
        bundle.Unload(false); // False keeps the newly written project links intact
        AssetDatabase.Refresh();

        Debug.Log($"🧼 Successfully unpacked {unpackedCount} prefabs with script data intact into: {relativeTargetFolder}");
    }
}
