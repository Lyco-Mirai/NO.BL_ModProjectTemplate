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

            UnitDefinition[] addedUnits = patchManifest.AddedUnits;
            WeaponMount[] addedWeaponMounts = patchManifest.AddedWeaponMounts;
            WeaponInfo[] addedWeaponInfos = patchManifest.AddedWeaponInfos;

            // Stop automation from poetentially wiping hand-written referances
            if (overwriteMode)
            {
                BL.Common.Debug.Log($"PatchManifest has overwrite mode enabled ...");
                string assetBundleIdentifierName = $"{assetBundleName}.nobp";

                List<UnitDefinition> allBundleUnitDefinitions = new List<UnitDefinition>();
                List<WeaponMount> allBundleWeaponMounts = new List<WeaponMount>();
                List<WeaponInfo> allBundleWeaponInfos = new List<WeaponInfo>();
                // Debug Check to see if the PatchManifestDefinition's bundle even exists 
                bool bundleExists = AssetDatabase.GetAllAssetBundleNames().Contains(assetBundleIdentifierName);
                if (bundleExists) { BL.Common.Debug.Log($"PatchManifest is a member of this AssetBundle: {assetBundleIdentifierName}"); }
                else { BL.Common.Debug.LogWarning($"PatchManifest is a member of this AssetBundle: {assetBundleIdentifierName}, Which does not exist!"); }
                // Find and list all definitions not already added to the PatchManifestDefinition
                string[] assetBundleContentPaths = AssetDatabase.GetAssetPathsFromAssetBundle(assetBundleIdentifierName);
                BL.Common.Debug.Log($"AssetBundle contains the following '{assetBundleContentPaths.Length}' asset paths: {string.Join(", ", assetBundleContentPaths)}");
                foreach (string assetPath in assetBundleContentPaths)
                {
                    UnitDefinition unit = AssetDatabase.LoadAssetAtPath<UnitDefinition>(assetPath);
                    WeaponMount weaponMount = AssetDatabase.LoadAssetAtPath<WeaponMount>(assetPath);
                    WeaponInfo weaponInfo = AssetDatabase.LoadAssetAtPath<WeaponInfo>(assetPath);
                    if (unit != null) { allBundleUnitDefinitions.Add(unit); }
                    if (weaponMount != null) { allBundleWeaponMounts.Add(weaponMount); }
                    if (weaponInfo != null) { allBundleWeaponInfos.Add(weaponInfo); }
                }
                // Add new found definitions to the PatchManifestDefinition
                foreach (UnitDefinition unit in allBundleUnitDefinitions)
                {
                    if (!addedUnits.Contains(unit))
                    {
                        BL.Common.Debug.Log($"PatchManifest does not referance added Definition of '{unit}' ...");
                        UnitDefinition[] newAddedUnits = T.Add(addedUnits, unit);
                        patchManifest.AddedUnits = newAddedUnits;
                        addedUnits = patchManifest.AddedUnits;
                    }
                }
                foreach (WeaponMount weaponMount in allBundleWeaponMounts)
                {
                    if (!addedWeaponMounts.Contains(weaponMount))
                    {
                        BL.Common.Debug.Log($"PatchManifest does not referance added Definition of '{weaponMount}' ...");
                        WeaponMount[] newAddedWeaponMounts = T.Add(addedWeaponMounts, weaponMount);
                        patchManifest.AddedWeaponMounts = newAddedWeaponMounts;
                        addedWeaponMounts = patchManifest.AddedWeaponMounts;
                    }
                }
                foreach (WeaponInfo weaponInfo in allBundleWeaponInfos)
                {
                    if (!addedWeaponInfos.Contains(weaponInfo))
                    {
                        BL.Common.Debug.Log($"PatchManifest does not referance added Definition of '{weaponInfo}' ...");
                        WeaponInfo[] newWeaponInfos = T.Add(addedWeaponInfos, weaponInfo);
                        patchManifest.AddedWeaponInfos = newWeaponInfos;
                        addedWeaponInfos = patchManifest.AddedWeaponInfos;
                    }
                }
                // Generate Patches
                // Generate Operations
                if (patchManifest.autoAddOperations)
                {
                    List<PatchManifestOperation> generatedOperations = new List<PatchManifestOperation>(); 
                    // Add Units to Encyclopedia
                        if (patchManifest.AddedUnits.Length > 0) { // Sanity check to ensure operation is only added when there is something to add...
                        PatchManifestOperation AddUnitsToEncyclopediaOperation = new PatchManifestOperation();
                        AddUnitsToEncyclopediaOperation.helperName = "Add Units to Encyclopedia";
                        AddUnitsToEncyclopediaOperation.opType = BlueprinterOpID.OpAddToEncyclopedia;
                        BlueprinterOperationPayload AddUnitsToEncyclopediaOperationPayload = new BlueprinterOperationPayload();
                        BlueprinterAsset[] AddUnitsToEncyclopediaOperationPayloadEntries = new BlueprinterAsset[patchManifest.AddedUnits.Length];
                        for (int _x = 0; _x < patchManifest.AddedUnits.Length; _x++)
                        {
                            UnitDefinition unit = patchManifest.AddedUnits[_x];
                            string entryNamespace = unit.GetType().Namespace;
                            if ((entryNamespace == "") || (entryNamespace == null)) {entryNamespace = "Assembly-CSharp"; }
                            string path = AssetDatabase.GetAssetPath(unit);
                            BlueprinterAsset assetReferance = new BlueprinterAsset($"{unit.name}", $"{path}", $"{unit.GetType().Name}, {entryNamespace}");
                            AddUnitsToEncyclopediaOperationPayloadEntries[_x] = assetReferance;
                        }
                        AddUnitsToEncyclopediaOperationPayload.entries = AddUnitsToEncyclopediaOperationPayloadEntries;
                        AddUnitsToEncyclopediaOperation.payload = AddUnitsToEncyclopediaOperationPayload;
                        generatedOperations.Add(AddUnitsToEncyclopediaOperation);
                        }
                    // Add WeaponMounts to Encyclopedia
                        if (patchManifest.AddedWeaponMounts.Length > 0) { // Sanity check to ensure operation is only added when there is something to add...
                        PatchManifestOperation AddWeaponMountsToEncyclopediaOperation = new PatchManifestOperation();
                        AddWeaponMountsToEncyclopediaOperation.helperName = "Add Weapon Mounts to Encyclopedia";
                        AddWeaponMountsToEncyclopediaOperation.opType = BlueprinterOpID.OpAddToEncyclopedia;
                        BlueprinterOperationPayload AddWeaponMountsToEncyclopediaOperationPayload = new BlueprinterOperationPayload();
                        BlueprinterAsset[] AddWeaponMountsToEncyclopediaOperationPayloadEntries = new BlueprinterAsset[patchManifest.AddedWeaponMounts.Length];
                        for (int _x = 0; _x < patchManifest.AddedWeaponMounts.Length; _x++)
                        {
                            WeaponMount weaponMount = patchManifest.AddedWeaponMounts[_x];
                            string entryNamespace = weaponMount.GetType().Namespace;
                            if ((entryNamespace == "") || (entryNamespace == null)) {entryNamespace = "Assembly-CSharp"; }
                            string path = AssetDatabase.GetAssetPath(weaponMount);
                            BlueprinterAsset assetReferance = new BlueprinterAsset($"{weaponMount.name}", $"{path}", $"{weaponMount.GetType().Name}, {entryNamespace}");
                            AddWeaponMountsToEncyclopediaOperationPayloadEntries[_x] = assetReferance;
                        }
                        AddWeaponMountsToEncyclopediaOperationPayload.entries = AddWeaponMountsToEncyclopediaOperationPayloadEntries;
                        AddWeaponMountsToEncyclopediaOperation.payload = AddWeaponMountsToEncyclopediaOperationPayload;
                        generatedOperations.Add(AddWeaponMountsToEncyclopediaOperation);
                        }
                    // Add WeaponMounts to Vehicles

                        BL.Common.Debug.Log($"Found {patchManifest.AddedPylonsToVehicle.Length} pylons to add to weapon managers ...");
                        if (patchManifest.AddedPylonsToVehicle.Length > 0) { // Sanity check to ensure no errors are produced
                        for (int _x = 0; _x < patchManifest.AddedPylonsToVehicle.Length; _x++)
                        {
                            WeaponPylonAddition weaponPylonAddition = patchManifest.AddedPylonsToVehicle[_x];   
                            WeaponMount weaponMount = weaponPylonAddition.weaponMount;        
                            string entryNamespace = weaponMount.GetType().Namespace;
                            if ((entryNamespace == "") || (entryNamespace == null)) { entryNamespace = "Assembly-CSharp"; }
                            string path = AssetDatabase.GetAssetPath(weaponMount);
                            
                            PatchManifestOperation operation = new PatchManifestOperation();
                            operation.helperName = $"Add WeaponMount '{weaponMount.name}' to Vehicles";
                            operation.opType = BlueprinterOpID.OpAddWeaponMountToWeaponManager;
                            BlueprinterOperationPayload operationPayload = new BlueprinterOperationPayload();

                            
                            BL.Common.Debug.Log($"Attempting to pass bundle asset to weapon manager: {weaponMount.name} ...");
                            operationPayload.bundleAsset = new BlueprinterAsset($"{weaponMount.name}", $"{path}", $"{weaponMount.GetType().Name}, {entryNamespace}");
                            BL.Common.Debug.Log($"{operationPayload.bundleAsset} ...");
                            
                            int weaponPylonAdditionSize = weaponPylonAddition.addMountToVehicles.Length;
                            BlueprinterWeaponManager[] vehicleWeaponManagers = new BlueprinterWeaponManager[weaponPylonAdditionSize];
                            for (int _y = 0; _y < weaponPylonAdditionSize; _y++)
                            {
                                WeaponMountEntry vehicleEntry = weaponPylonAddition.addMountToVehicles[_y];   
                                BlueprinterWeaponManager vehicleWeaponManager = new BlueprinterWeaponManager();
                                VehicleModelType vehicleInfo = VehicleModelTypeDatabase.GetVehicleModelInfo(vehicleEntry.vehicle);
                                vehicleWeaponManager.helperName = vehicleInfo.name;
                                vehicleWeaponManager.gameAsset = new BlueprinterAsset();
                                vehicleWeaponManager.gameAsset = vehicleInfo.blueprinterAsset;
                                vehicleWeaponManager.gameAsset.type = "WeaponManager, Assembly-CSharp";
                                vehicleWeaponManager.hardpointSetIndices = vehicleEntry.pylonIndexes;
                                vehicleWeaponManagers[_y] = vehicleWeaponManager;
                            }

                            operationPayload.weaponManagers = vehicleWeaponManagers;
                            operation.payload = operationPayload;
                            generatedOperations.Add(operation);
                        }}
                    // Combine and push Operations to the Patch Manifest
                        patchManifest.Ops = new PatchManifestOperation[generatedOperations.Count()];
                        for (int _x = 0; _x < generatedOperations.Count(); _x++)
                        {
                            patchManifest.Ops[_x] = generatedOperations[_x];
                        }
                }
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