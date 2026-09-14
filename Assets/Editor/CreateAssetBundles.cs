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

namespace BL.UnityEditor
{
    public class CreateAssetBundles : MonoBehaviour
    {
        public static List<PatchManifest> GetAllPackManifests()
        {
            BL.Common.Debug.Log($"Attempting to gather all Patch manifests!");

            string[] guids = AssetDatabase.FindAssets("t:PatchManifest a:assets");
            List<PatchManifest> patchManifests = new List<PatchManifest>();

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                BL.Common.Debug.Log($"Found PatchManifest asset at: {assetPath}");
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
            AutomateAllPackManifests();

            ClearConsole();

            BL.Common.Debug.Log($"Attempting to generate Patch manifests!");

            List<PatchManifest> patchManifests = GetAllPackManifests();

            BL.Common.Debug.Log($"Found {patchManifests.Count} Patch Manifest(s)!");

            foreach (PatchManifest patchManifest in patchManifests)
            {
                string assetBundleName = patchManifest.assetBundleName;
                string assetPath = AssetDatabase.GetAssetPath(patchManifest);

                BL.Common.Debug.Log($"Attempting to format JSON file ...");

                PatchManifestJSON patchManifestJSON = new PatchManifestJSON();
                patchManifestJSON.modName = patchManifest.modName;
                patchManifestJSON.schemaVersion = patchManifest.schemaVersion;
                patchManifestJSON.modVersion = patchManifest.modVersion;

                BL.Common.Debug.Log($"Attempting to add {patchManifest.Patches.Length} Patches ...");
                for (int _x = 0; _x < patchManifest.Patches.Length; _x++)
                {
                    PatchManifestPatch patch = patchManifest.Patches[_x];
                    PatchManifestPatchJSON patchJSON = new PatchManifestPatchJSON();
                    patchJSON.GameAsset = patch.GameAsset;
                    patchJSON.PatchLocations = patch.PatchLocations;
                    BL.Common.Debug.Log($"Attempting to add Patch {patchJSON} ...");
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

                BL.Common.Debug.Log($"Attempting to create PatchManifest at: '{absoluteFilePath}'");
                File.WriteAllText(absoluteFilePath, jsonString);
                BL.Common.Debug.Log($"Attempting to register PatchManifest at: {relativeFilePath} ..."); 
                AssetDatabase.Refresh();
                AssetImporter patchManifestImporter = AssetImporter.GetAtPath(relativeFilePath);
                BL.Common.Debug.Log($"Attempting to assign PatchManifest to bundle {assetBundleName}.nobp ...");
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
                BL.Common.Debug.Log($"PatchManifest has no assetBundle assignment!");
                string assetPath = AssetDatabase.GetAssetPath(patchManifest);
                BL.Common.Debug.Log($"PatchManifest located in path: {assetPath}");
                AssetImporter importer = AssetImporter.GetAtPath(assetPath);
                if (importer != null) {
                    string importerAssetBundleName = importer.assetBundleName; 
                    if (importerAssetBundleName != "") {
                        assetBundleName = importerAssetBundleName;
                        BL.Common.Debug.Log($"PatchManifest importer assetBundle not null/empty: {assetBundleName}");
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
                if (!patchManifest.copyPatchDefinitionToAssetBundle) {
                    BL.Common.Debug.Log($"Attempting to wipe PatchManifest Definition from all bundles ...");
                    patchDefinitionImporter.SetAssetBundleNameAndVariant("", "");
                    patchDefinitionImporter.SaveAndReimport();
                }
            }

            // Stop automation from poetentially wiping hand-written referances
            if (overwriteMode)
            {
                BL.Common.Debug.Log($"PatchManifest has overwrite mode enabled ...");
                string assetBundleIdentifierName = $"{assetBundleName}.nobp";

                patchManifest.Generate();
            }
        }
        
        [MenuItem("Assets/Pack/Manual/Automate Patch Manifests")]
        public static void AutomateAllPackManifests()
        {
            ClearConsole();
            // Sanity Check for ensuring the patches are ready 
            AssetDatabase.Refresh();
            AssetDatabase.RemoveUnusedAssetBundleNames();
            AssetDatabase.SaveAssets();
            BL.Common.Debug.Log($"Attempting to automate Patch manifests!");
            // Get and process all PatchManifestDefinitions
            List<PatchManifest> patchManifests = GetAllPackManifests();
            foreach (PatchManifest patchManifest in patchManifests)
            {
                AutomatePackManifest(patchManifest);
            }
        }
        [MenuItem("Assets/Pack/Build AssetBundles")]
        public static void BuildAllAssetBundles()
        {
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
}