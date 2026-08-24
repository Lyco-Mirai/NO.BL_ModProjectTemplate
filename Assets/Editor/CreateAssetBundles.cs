using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.VisualBasic;
using BL.Blueprinter;
using BL.Common;

public class CreateAssetBundles : MonoBehaviour
{
    public static List<PatchManifest> GetAllPackManifests()
    {
        Debug.Log($"Attempting to gather all Patch manifests!");

        string[] guids = AssetDatabase.FindAssets("t:PatchManifest a:assets");
        List<PatchManifest> patchManifests = new List<PatchManifest>();

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            Debug.Log($"Found PatchManifest asset at: {assetPath}");
            PatchManifest patchManifest = AssetDatabase.LoadAssetAtPath<PatchManifest>(assetPath);
            if (patchManifest != null) { patchManifests.Add(patchManifest); }
        }

        return patchManifests;
    }

    public static void ClearConsole()
    {
        // Clear the console
        var assembly = Assembly.GetAssembly(typeof(EditorWindow));
        var type = assembly.GetType("UnityEditor.LogEntries");
        var method = type.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public);
        method.Invoke(null, null);
    }

    [MenuItem("Assets/Pack/Manual/Generate Patch Manifests")]
    public static void GenerateAllPackManifests()
    {
        ClearConsole();

        Debug.Log($"Attempting to generate Patch manifests!");

        List<PatchManifest> patchManifests = GetAllPackManifests();

        Debug.Log($"Found {patchManifests.Count} Patch Manifest(s)!");

        foreach (PatchManifest patchManifest in patchManifests)
        {
            string assetBundleName = patchManifest.assetBundleName;
            string assetPath = AssetDatabase.GetAssetPath(patchManifest);

            Debug.Log($"Attempting to format JSON file ...");

            PatchManifestJSON patchManifestJSON = new PatchManifestJSON();
            patchManifestJSON.modName = patchManifest.modName;
            patchManifestJSON.schemaVersion = patchManifest.schemaVersion;
            patchManifestJSON.modVersion = patchManifest.modVersion;

            Debug.Log($"Attempting to add {patchManifest.Patches.Length} Patches ...");
            for (int _x = 0; _x < patchManifest.Patches.Length; _x++)
            {
                PatchManifestPatch patch = patchManifest.Patches[_x];
                PatchManifestPatchJSON patchJSON = new PatchManifestPatchJSON();
                patchJSON.GameAsset = patch.GameAsset;
                patchJSON.PatchLocations = patch.PatchLocations;
                Debug.Log($"Attempting to add Patch {patchJSON} ...");
                patchManifestJSON.AddPatch(patchJSON);
            }
            for (int _x = 0; _x < patchManifest.Ops.Length; _x++)
            {
                PatchManifestOperation op = patchManifest.Ops[_x];
                op.GeneratePayloadJSON();
                PatchManifestOperationJSON opJSON = new PatchManifestOperationJSON();
                opJSON.opId = op.opId;
                opJSON.payloadJson = op.payloadJson;
                patchManifestJSON.AddOp(opJSON);
            }

            string fileName = "patch_manifest.json";

            string directoryPath = Path.GetDirectoryName(assetPath);
            string projectPath = Path.GetDirectoryName(Application.dataPath);
            string absolutePath = Path.Combine(projectPath, directoryPath);
            string absoluteFilePath = Path.Combine(absolutePath, fileName);
            string relativeFilePath = Path.Combine(directoryPath, fileName);
            string jsonString = JsonUtility.ToJson(patchManifestJSON, patchManifest.fancyFormating);

            Debug.Log($"Attempting to create PatchManifest at: '{absoluteFilePath}'");
            File.WriteAllText(absoluteFilePath, jsonString);
            Debug.Log($"Attempting to register PatchManifest at: {relativeFilePath} ..."); 
            AssetDatabase.Refresh();
            AssetImporter patchManifestImporter = AssetImporter.GetAtPath(relativeFilePath);
            Debug.Log($"Attempting to assign PatchManifest to bundle {assetBundleName}.nobp ...");
            patchManifestImporter.SetAssetBundleNameAndVariant(assetBundleName, "nobp");
            patchManifestImporter.SaveAndReimport();
        }
    }

    public static void AutomatePackManifest(PatchManifest patchManifest)
    {
        string assetBundleName = patchManifest.assetBundleName;
        bool overwriteMode = patchManifest.overwriteMode;
        bool addPatches = patchManifest.autoAddPatches;
        bool addOperations = patchManifest.autoAddOperations;

        if (assetBundleName == "")
        {
            Debug.Log($"PatchManifest has no assetBundle assignment!");
            string assetPath = AssetDatabase.GetAssetPath(patchManifest);
            Debug.Log($"PatchManifest located in path: {assetPath}");
            AssetImporter importer = AssetImporter.GetAtPath(assetPath);
            if (importer != null) {
                string importerAssetBundleName = importer.assetBundleName; 
                if (importerAssetBundleName != "") {
                    assetBundleName = importerAssetBundleName;
                    Debug.Log($"PatchManifest importer assetBundle not null/empty: {assetBundleName}");
                }
                else
                {
                    if (patchManifest.modName != "") {
                        assetBundleName = patchManifest.modName;
                    }
                }
            }
            else
            {
                if (patchManifest.modName != "") {
                    assetBundleName = patchManifest.modName;
                }
            }
        }
        if (overwriteMode)
        {
            patchManifest.assetBundleName = assetBundleName;            
            // Toggle to allow for the PatchManifestDefinition to also be packaged if wanted ...
            string assetPath = AssetDatabase.GetAssetPath(patchManifest);
            AssetImporter patchDefinitionImporter = AssetImporter.GetAtPath(assetPath);
            if (!patchManifest.copyDefinitionToAssetbundle) {
                Debug.Log($"Attempting to wipe PatchManifest Definition from all bundles ...");
                patchDefinitionImporter.SetAssetBundleNameAndVariant("", "");
                patchDefinitionImporter.SaveAndReimport();
            }
        }

        UnitDefinition[] addedUnits = patchManifest.AddedUnits;
        WeaponMount[] addedWeaponMounts = patchManifest.AddedWeaponMounts;
        WeaponInfo[] addedWeaponInfos = patchManifest.AddedWeaponInfos;

        // Stop automation from poetentially wiping hand-written referances
        if (overwriteMode)
        {
            Debug.Log($"PatchManifest has overwrite mode enabled ...");

            AssetDatabase.Refresh();
            AssetDatabase.RemoveUnusedAssetBundleNames();
            AssetDatabase.SaveAssets();
            string assetBundleIdentifierName = $"{assetBundleName}.nobp";

            List<UnitDefinition> allBundleUnitDefinitions = new List<UnitDefinition>();
            List<WeaponMount> allBundleWeaponMounts = new List<WeaponMount>();
            List<WeaponInfo> allBundleWeaponInfos = new List<WeaponInfo>();

            // Debug Check to see if the bundle even exists 
            bool bundleExists = AssetDatabase.GetAllAssetBundleNames().Contains(assetBundleIdentifierName);
            if (bundleExists) { Debug.Log($"PatchManifest is a member of this AssetBundle: {assetBundleIdentifierName}"); }
            else { Debug.LogWarning($"PatchManifest is a member of this AssetBundle: {assetBundleIdentifierName}, Which does not exist!"); }

            string[] assetBundleContentPaths = AssetDatabase.GetAssetPathsFromAssetBundle(assetBundleIdentifierName);
            Debug.Log($"AssetBundle contains the following '{assetBundleContentPaths.Length}' asset paths: {string.Join(", ", assetBundleContentPaths)}");
            foreach (string assetPath in assetBundleContentPaths)
            {
                UnitDefinition unit = AssetDatabase.LoadAssetAtPath<UnitDefinition>(assetPath);
                WeaponMount weaponMount = AssetDatabase.LoadAssetAtPath<WeaponMount>(assetPath);
                WeaponInfo weaponInfo = AssetDatabase.LoadAssetAtPath<WeaponInfo>(assetPath);
                if (unit != null) { allBundleUnitDefinitions.Add(unit); }
                if (weaponMount != null) { allBundleWeaponMounts.Add(weaponMount); }
                if (weaponInfo != null) { allBundleWeaponInfos.Add(weaponInfo); }
            }

            foreach (UnitDefinition unit in allBundleUnitDefinitions)
            {
                if (!addedUnits.Contains(unit))
                {
                    Debug.Log($"PatchManifest does not referance added Definition of '{unit}' ...");
                    UnitDefinition[] newAddedUnits = T.Add(addedUnits, unit);
                    patchManifest.AddedUnits = newAddedUnits;
                    addedUnits = patchManifest.AddedUnits;
                }
            }
            foreach (WeaponMount weaponMount in allBundleWeaponMounts)
            {
                if (!addedWeaponMounts.Contains(weaponMount))
                {
                    Debug.Log($"PatchManifest does not referance added Definition of '{weaponMount}' ...");
                    WeaponMount[] newAddedWeaponMounts = T.Add(addedWeaponMounts, weaponMount);
                    patchManifest.AddedWeaponMounts = newAddedWeaponMounts;
                    addedWeaponMounts = patchManifest.AddedWeaponMounts;
                }
            }
            foreach (WeaponInfo weaponInfo in allBundleWeaponInfos)
            {
                if (!addedWeaponInfos.Contains(weaponInfo))
                {
                    Debug.Log($"PatchManifest does not referance added Definition of '{weaponInfo}' ...");
                    WeaponInfo[] newWeaponInfos = T.Add(addedWeaponInfos, weaponInfo);
                    patchManifest.AddedWeaponInfos = newWeaponInfos;
                    addedWeaponInfos = patchManifest.AddedWeaponInfos;
                }
            }
        }
    }
    [MenuItem("Assets/Pack/Manual/Automate Patch Manifests")]
    public static void AutomateAllPackManifests()
    {
        ClearConsole();

        Debug.Log($"Attempting to automate Patch manifests!");

        List<PatchManifest> patchManifests = GetAllPackManifests();
        foreach (PatchManifest patchManifest in patchManifests)
        {
            AutomatePackManifest(patchManifest);
        }
    }
    [MenuItem("Assets/Pack/Build AssetBundles")]
    public static void BuildAllAssetBundles()
    {
        AutomateAllPackManifests();
        GenerateAllPackManifests();

        string assetBundleDirectory = "Assets/StreamingAssets";
        if (!Directory.Exists(assetBundleDirectory))
        {
            Directory.CreateDirectory(assetBundleDirectory);
        }
        
        BuildPipeline.BuildAssetBundles(
            assetBundleDirectory, 
            BuildAssetBundleOptions.None, 
            BuildTarget.StandaloneWindows64);
    }
}