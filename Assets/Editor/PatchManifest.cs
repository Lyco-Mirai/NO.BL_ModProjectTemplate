using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.VisualBasic;
using NaughtyAttributes;
using BL.Blueprinter;
using BL.Common;

namespace BL.Blueprinter
{    
    public enum VehicleModel
    {
        None,
        [InspectorName("Vanilla/Aircraft/CI-22 'Cricket'")] COIN, // CI-22 'Cricket, added in: Playtest
        [InspectorName("Vanilla/Aircraft/SFB-81 'Darkreach'")] SFB, // Playtest
        [InspectorName("Vanilla/Aircraft/FS-12 'Revoker'")] Fighter1, // Playtest
        [InspectorName("Vanilla/Aircraft/T⁄A-30 'Compass'")] Trainer, // Playtest 0.20
        [InspectorName("Vanilla/Aircraft/SAH-46 'Chicane'")] AttackHelo1, // Playtest 0.24
        [InspectorName("Vanilla/Navy/Shard-class Corvette")] Corvette1, // Version 0.26
        [InspectorName("Vanilla/Aircraft/EW-25 'Medusa'")] EW1, // Version 0.27
        [InspectorName("Vanilla/Aircraft/KR-67 'Ifrit'")] Multirole1, // Version 0.28
        [InspectorName("Vanilla/Navy/Hyperion-class Fleet Carrier")] FleetCarrier1, // Version 0.28
        [InspectorName("Vanilla/Aircraft/VL-49 'Tarantula'")] QuadVTOL1, // Version 0.29
        [InspectorName("Vanilla/Navy/Dynamo-class Destroyer")] Destroyer1, // Version 0.29
        [InspectorName("Vanilla/Aircraft/FS-20 'Vortex'")] SmallFighter1, // Version 0.30
        [InspectorName("Vanilla/Navy/Annex-class Assault Carrier")] AssaultCarrier1, // Version 0.30
        [InspectorName("Vanilla/Navy/OTB-31 LCAC")] LandingCraft1, // Version 0.30
        [InspectorName("Vanilla/Aircraft/UH-90 'Ibis'")] UtilityHelo1, // Version 0.31
        [InspectorName("Vanilla/Aircraft/A-19 'Brawler'")] CAS1, // Version 0.32
        [InspectorName("Vanilla/Aircraft/AB-4 'Alkyon'")] FastBomber1, // Version 0.33
        [InspectorName("Vanilla/Navy/Argus-class Missile Frigate")] Frigate1, // Version 0.33
        [InspectorName("Vanilla/Navy/Cursor-class LFD")] SmallCarrier1, // Version 0.33
        [InspectorName("Vanilla/Aircraft/VT-7 'Vagrant'")] VTOLTrainer1, // Version 0.34
        [InspectorName("Vanilla/Navy/Surf-class Patrol Boat")] PatrolBoat1, // Version 0.34

        [InspectorName("Aryx3D/Aircraft/F-16M 'King Viper'")] Aryx_F16M_KingViper,
        [InspectorName("Aryx3D/Aircraft/F-99 'Shrike'")] Aryx_LightFighter1

        //[InspectorName("Aryx/Aircraft/MiG-15 'Fagot'")] PatrolBoat
    }
    [System.Serializable]
    public class VehicleModelType
    {
        public string name;
        public string vehicleUpdate;
        public VehicleModel vehicleModel;
        public BlueprinterAsset blueprinterAsset;   

        public VehicleModelType(){}
        public VehicleModelType(VehicleModel initVehicleModel, BlueprinterAsset initBlueprinterAsset)
        {
            vehicleModel = initVehicleModel;
            blueprinterAsset = initBlueprinterAsset;
        }
        public VehicleModelType(string initVehicleUpdate, VehicleModel initVehicleModel, BlueprinterAsset initBlueprinterAsset)
        {
            vehicleUpdate = initVehicleUpdate;
            vehicleModel = initVehicleModel;
            blueprinterAsset = initBlueprinterAsset;
        }        
        public VehicleModelType(string initInpectorName, string initVehicleUpdate, VehicleModel initVehicleModel, BlueprinterAsset initBlueprinterAsset)
        {
            name = initInpectorName;
            vehicleUpdate = initVehicleUpdate;
            vehicleModel = initVehicleModel;
            blueprinterAsset = initBlueprinterAsset;
        }  
    }
    public static class VehicleModelTypeDatabase
    {
        public static VehicleModelType GetVehicleModelInfo(VehicleModel vehicle)
        {
            string type = "";
            switch(vehicle) 
            {
                // Vanilla Aircraft
                case VehicleModel.COIN: return new VehicleModelType("CI-22 'Cricket'", "0.19.0", vehicle, new BlueprinterAsset("COIN", "BaseGame", type));
                case VehicleModel.SFB: return new VehicleModelType("SFB-81 'Darkreach'", "0.19.0", vehicle, new BlueprinterAsset("SFB", "BaseGame", type));
                case VehicleModel.Fighter1: return new VehicleModelType("FS-12 'Revoker'", "0.19.0", vehicle, new BlueprinterAsset("Fighter1", "BaseGame", type));
                case VehicleModel.Trainer: return new VehicleModelType("T⁄A-30 'Compass'", "0.20.0", vehicle, new BlueprinterAsset("Trainer", "BaseGame", type));
                case VehicleModel.AttackHelo1: return new VehicleModelType("SAH-46 'Chicane'", "0.24.0", vehicle, new BlueprinterAsset("AttackHelo1", "BaseGame", type));
                case VehicleModel.EW1: return new VehicleModelType("EW-25 'Medusa'", "0.27.0", vehicle, new BlueprinterAsset("EW1", "BaseGame", type));
                case VehicleModel.Multirole1: return new VehicleModelType("KR-67 'Ifrit'", "0.28.0", vehicle, new BlueprinterAsset("Multirole1", "BaseGame", type));
                case VehicleModel.QuadVTOL1: return new VehicleModelType("VL-49 'Tarantula'", "0.29.0", vehicle, new BlueprinterAsset("QuadVTOL1", "BaseGame", type));
                case VehicleModel.SmallFighter1: return new VehicleModelType("FS-20 'Vortex'", "0.30.0", vehicle, new BlueprinterAsset("SmallFighter1", "BaseGame", type));
                case VehicleModel.UtilityHelo1: return new VehicleModelType("UH-90 'Ibis'", "0.31.0", vehicle, new BlueprinterAsset("UtilityHelo1", "BaseGame", type));
                case VehicleModel.CAS1: return new VehicleModelType("A-19 'Brawler'", "0.32.0", vehicle, new BlueprinterAsset("CAS1", "BaseGame", type));
                case VehicleModel.FastBomber1: return new VehicleModelType("AB-4 'Alkyon'", "0.33.0", vehicle, new BlueprinterAsset("FastBomber1", "BaseGame", type));
                case VehicleModel.VTOLTrainer1: return new VehicleModelType("VT-7 'Vagrant'", "0.34.0", vehicle, new BlueprinterAsset("VTOLTrainer1", "BaseGame", type));
                // Vanilla Seacraft
                case VehicleModel.Corvette1: return new VehicleModelType("Shard-class Corvette", "0.26.0", vehicle, new BlueprinterAsset("Corvette1", "BaseGame", type));
                case VehicleModel.FleetCarrier1: return new VehicleModelType("Hyperion-class Fleet Carrier", "0.28.0", vehicle, new BlueprinterAsset("FleetCarrier1", "BaseGame", type));
                case VehicleModel.Destroyer1: return new VehicleModelType("Dynamo-class Destroyer", "0.29.0", vehicle, new BlueprinterAsset("Destroyer1", "BaseGame", type));
                case VehicleModel.AssaultCarrier1: return new VehicleModelType("Annex-class Assault Carrier", "0.30.0", vehicle, new BlueprinterAsset("AssaultCarrier1", "BaseGame", type));
                case VehicleModel.LandingCraft1: return new VehicleModelType("OTB-31 LCAC", "0.30.0", vehicle, new BlueprinterAsset("LandingCraft1", "BaseGame", type));
                case VehicleModel.Frigate1: return new VehicleModelType("Argus-class Missile Frigate", "0.33.0", vehicle, new BlueprinterAsset("Frigate1", "BaseGame", type));
                case VehicleModel.SmallCarrier1: return new VehicleModelType("Cursor-class LFD", "0.33.0", vehicle, new BlueprinterAsset("SmallCarrier1", "BaseGame", type));
                case VehicleModel.PatrolBoat1: return new VehicleModelType("Surf-class Patrol Boat", "0.34.0", vehicle, new BlueprinterAsset("PatrolBoat1", "BaseGame", type));
                // Aryx3D
                case VehicleModel.Aryx_F16M_KingViper: return new VehicleModelType("F-16M 'King Viper'", "0.0.0", vehicle, new BlueprinterAsset("Aryx_F16M_KingViper", "BaseGame", type));
                case VehicleModel.Aryx_LightFighter1: return new VehicleModelType("F-99 'Shrike'", "0.0.0", vehicle, new BlueprinterAsset("Aryx_LightFighter1", "BaseGame", type));
                // Default
                case VehicleModel.None: return new VehicleModelType("ERROR: Null Vehicle!", "0.0.0", vehicle, new BlueprinterAsset("ERROR_NULL_VEHICLE", "BaseGame", type));
                default: return new VehicleModelType("ERROR: No Vehicle!", "0.0.0", VehicleModel.None, new BlueprinterAsset("ERROR_NO_VEHICLE", "ERROR", type));
            }
        }
    }
    
    [System.Serializable]
    public class AssetReferanceDependant
    {
        public UnityEngine.Object dependentObject;
        public BlueprinterPatchLocation[] dependentLocations;
    }
    [System.Serializable]
    public class AssetReferance
    {
        public string assetName;
        public UnityEngine.Object referance;
        public AssetReferanceDependant[] dependents;
        //public string 
    }
    [System.Serializable]
    public class WeaponMountEntry
    {   
        public bool manualMode;
        [HideIf("manualMode"), AllowNesting, Tooltip("Vehicle to add the weapon mounts to, arranged as a fast, easy-to-use list dictionary...")]
        public VehicleModel vehicle;
        [ShowIf("manualMode"), AllowNesting]
        public VehicleModelType vehicleInfo;
        public bool isEnabled;
        public bool isEventContent;
        public int[] pylonIndexes;
    }
    [System.Serializable]
    public class WeaponPylonAddition
    {
        public string helperName;
        public WeaponMount weaponMount;
        public WeaponMountEntry[] addMountToVehicles;
    }

    [CreateAssetMenu(fileName = "NewItemData", menuName = "ScriptableObjects/Patch Manifest")]
    [System.Serializable]
    public class PatchManifest : ScriptableObject
    {

        [Header("Patch Manifest - Manual Stuff")]

        public WeaponPylonAddition[] AddedPylonsToVehicle;

        [Header("Patch Manifest - Automation")]
        [SerializeField, Tooltip("Enable to make the additions below take priority and likely overwrite manual changes above and below...")]
        public bool overwriteMode;
        [SerializeField, ShowIf("overwriteMode"), AllowNesting, Tooltip("Enable to add any referance outside the current assetbundle as a patch...")]
        public bool autoAddPatches;
        [SerializeField, ShowIf("overwriteMode"), AllowNesting, Tooltip("Enable to add all entities below to the Encyclopedia Automatically...")]
        public bool autoAddOperations;
        [SerializeField, ShowIf("overwriteMode"), AllowNesting, Tooltip("Enable to copy this scriptableObject into the AssetBundle too, may drag extra referances...")]
        public bool copyPatchDefinitionToAssetBundle = false;

        [ShowIf("overwriteMode"), AllowNesting]
        public UnityEngine.Object[] AddedAssets;
        [ShowIf("overwriteMode"), AllowNesting]
        public Material[] AddedMaterials;
        [ShowIf("overwriteMode"), AllowNesting]
        public GameObject[] AddedPrefabs;
        [ShowIf("overwriteMode"), AllowNesting]
        public UnitDefinition[] AddedUnits;
        [ShowIf("overwriteMode"), AllowNesting]
        public WeaponInfo[] AddedWeaponInfos;
        [ShowIf("overwriteMode"), AllowNesting]
        public WeaponMount[] AddedWeaponMounts;
        [ShowIf("overwriteMode"), AllowNesting]
        public Faction[] AddedFactions;

        // Patcher Stuff
        // - Grab everything that has been referanced outside the current assetbundle and save it here for later...
        [Header("Patch Manifest - External Referances")]

        [ShowIf("overwriteMode"), AllowNesting]
        public AssetReferance[] ReferancesDatabase;
        [ShowIf("overwriteMode"), AllowNesting]
        public UnityEngine.Object[] ExternalReferances;

        [Header("Patch Manifest - Manual Modification")]

        [Tooltip("Enable to make the JSON file actually legable to human eyes...")]
        public bool fancyFormating;
        [Tooltip("Name of the assetbundle the 'patch_manifest.json' gets assigned to once created...")]
        public string assetBundleName;
        [Tooltip("Name of the mod")]
        public string modName;
        [HideInInspector]
        public int schemaVersion = 3;
        [Tooltip("Version of the Mod")]
        public string modVersion;
        [Tooltip("Base-Game Vanilla or External Referances to Units or other mods")]
        public PatchManifestPatch[] Patches;
        [Tooltip("Additions into the game...")]
        public PatchManifestOperation[] Ops;

        public void CleanPatches() {                
            Patches = new PatchManifestPatch[0];
        }        
        public void CleanOps() {
            Ops = new PatchManifestOperation[0];
        }        
        public void Clean() {
            AddedAssets = new UnityEngine.Object[0];
            AddedMaterials = new Material[0];
            AddedPrefabs = new GameObject[0];
            AddedUnits = new UnitDefinition[0];
            AddedWeaponInfos = new WeaponInfo[0];
            AddedWeaponMounts = new WeaponMount[0];
            AddedFactions = new Faction[0];
            ReferancesDatabase = new AssetReferance[0];
            ExternalReferances = new UnityEngine.Object[0];

            if (overwriteMode && autoAddPatches) { CleanPatches(); }
            if (overwriteMode && autoAddOperations) { CleanOps(); }
        }


        public void FindAddedAssets() {
            if (assetBundleName == null) { assetBundleName = ""; } // Sanity Check
            string assetBundleIdentifierName = $"{assetBundleName}.nobp";

            List<UnityEngine.Object> allAddedAssets = new List<UnityEngine.Object>();

            // Debug Check to see if the PatchManifestDefinition's bundle even exists 
            bool bundleExists = AssetDatabase.GetAllAssetBundleNames().Contains(assetBundleIdentifierName);
            if (bundleExists) { BL.Common.Debug.Log($"PatchManifest is a member of this AssetBundle: {assetBundleIdentifierName}"); }
            else { BL.Common.Debug.LogWarning($"PatchManifest is a member of this AssetBundle: {assetBundleIdentifierName}, Which does not exist!"); }

            // Find and list all definitions not already added to the PatchManifestDefinition
            string[] assetBundleContentPaths = AssetDatabase.GetAssetPathsFromAssetBundle(assetBundleIdentifierName);
            string[] assetBundleDependancyPaths = AssetDatabase.GetDependencies(assetBundleContentPaths, true);
            string[] trueAssetDependancyPaths = assetBundleDependancyPaths.Except(assetBundleContentPaths).ToArray(); ;
            BL.Common.Debug.Log($"AssetBundle {assetBundleIdentifierName} contains the following '{assetBundleContentPaths.Length}' asset paths: {string.Join(", ", assetBundleContentPaths)}");
            BL.Common.Debug.Log($"AssetBundle {assetBundleIdentifierName} referances the following '{trueAssetDependancyPaths.Length}' asset paths: {string.Join(", ", trueAssetDependancyPaths)}");
            
            foreach (string assetPath in assetBundleContentPaths) {
                if (assetPath != null && assetPath != "") { // Sanity Check
                    string assetFileName = Path.GetFileName(assetPath);
                    UnityEngine.Object internalObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                    if (internalObject != null) { 
                        if (!AddedAssets.Contains(internalObject)) { AddedAssets = T.Add(AddedAssets, internalObject); }
                        switch (internalObject) {
                            case Material _ref: if ((_ref != null) && (!AddedMaterials.Contains(_ref))) { AddedMaterials = T.Add(AddedMaterials, _ref); } break;
                            case GameObject _ref: if ((_ref != null) && (!AddedPrefabs.Contains(_ref))) {  AddedPrefabs = T.Add(AddedPrefabs, _ref); } break;
                            case UnitDefinition _ref: if ((_ref != null) && (!AddedUnits.Contains(_ref))) { AddedUnits = T.Add(AddedUnits, _ref); } break;
                            case WeaponInfo _ref: if ((_ref != null) && (!AddedWeaponInfos.Contains(_ref))) { AddedWeaponInfos = T.Add(AddedWeaponInfos, _ref); } break;
                            case WeaponMount _ref: if ((_ref != null) && (!AddedWeaponMounts.Contains(_ref))) { AddedWeaponMounts = T.Add(AddedWeaponMounts, _ref); } break;
                            case Faction _ref: if ((_ref != null) && (!AddedFactions.Contains(_ref))) { AddedFactions = T.Add(AddedFactions, _ref); } break;
                            default: break;        
                        }
                    }
                }
            }
        }

        public void TryAddInternalAssetReferance(UnityEngine.Object referanceObject) { AddInternalAssetReferance(referanceObject); } // Poetentially needed for filtering
        internal void AddInternalAssetReferance(UnityEngine.Object referanceObject)
        {
            if (!AddedAssets.Contains(referanceObject))
            {
                BL.Common.Debug.Log($"PatchManifest does not referance added asset of '{referanceObject}' ...");
                AddedAssets = T.Add(AddedAssets, referanceObject);
            }
            switch (referanceObject)
            {
                case UnityEngine.Material material: if (!AddedMaterials.Contains(material)) { AddedMaterials = T.Add(AddedMaterials, material); } break;
                case GameObject prefab: if (!AddedPrefabs.Contains(prefab)) { AddedPrefabs = T.Add(AddedPrefabs, prefab); } break;
                case UnitDefinition unit: if (!AddedUnits.Contains(unit)) { AddedUnits = T.Add(AddedUnits, unit); } break;
                case WeaponInfo weaponInfo: if (!AddedWeaponInfos.Contains(weaponInfo)) { AddedWeaponInfos = T.Add(AddedWeaponInfos, weaponInfo); } break;
                case WeaponMount weaponMount: if (!AddedWeaponMounts.Contains(weaponMount)) { AddedWeaponMounts = T.Add(AddedWeaponMounts, weaponMount); } break;
                case Faction faction: if (!AddedFactions.Contains(faction)) { AddedFactions = T.Add(AddedFactions, faction); } break;
                default: break;
            }
        }
        
        public static BlueprinterPatchLocation[] GetDepencancyPatchLocations(UnityEngine.Object dependant, UnityEngine.Object dependency)
        {
            string path = AssetDatabase.GetAssetPath(dependant);
            string fileName = Path.GetFileNameWithoutExtension(path);
            BlueprinterPatchLocation[] results = new BlueprinterPatchLocation[0];

            if (dependant == null || dependency == null)
                return results;
            if (dependant is GameObject rootGo) {
                Component[] allComponents = rootGo.GetComponentsInChildren<Component>(true);

                foreach (Component comp in allComponents)
                {
                    if (comp == null) continue; // Skip broken or missing script components
                    List<string> foundPropertyPaths = ScanSerializedFields(comp, dependency);

                    if (foundPropertyPaths.Count > 0)
                    {
                        string cleanHierarchyPath = GetCleanHierarchyPath(rootGo, comp.gameObject);
                        string assemblyQualifiedName = GetFormattedTypeName(comp);
                        Component[] siblingsOfSameType = comp.gameObject.GetComponents(comp.GetType());
                        int index = Array.IndexOf(siblingsOfSameType, comp);
                        int componentIndex = siblingsOfSameType.Length > 1 ? index : 0;
                        foreach (string propPath in foundPropertyPaths)
                        {
                            BlueprinterPatchLocation patchLocation = new BlueprinterPatchLocation();
                            patchLocation.id = $"{fileName} | {assemblyQualifiedName}.{FormatMemberPath(propPath)}";
                            patchLocation.asset = new BlueprinterAsset();
                            patchLocation.asset.name = $"{fileName}";
                            patchLocation.asset.locator = $"{path}";
                            patchLocation.asset.type = GetFormattedTypeName(dependant);
                            patchLocation.hierarchyPath = cleanHierarchyPath;
                            patchLocation.componentType = assemblyQualifiedName;
                            patchLocation.componentIndex = componentIndex;
                            patchLocation.memberPath = FormatMemberPath(propPath);

                            results = T.Add(results, patchLocation);
                        }
                    }
                }
            } else {
                List<string> foundPropertyPaths = ScanSerializedFields(dependant, dependency);
                string assemblyQualifiedName = GetFormattedTypeName(dependant);

                foreach (string propPath in foundPropertyPaths)
                {
                    BlueprinterPatchLocation patchLocation = new BlueprinterPatchLocation();
                    patchLocation.id = $"{fileName} | {assemblyQualifiedName}.{FormatMemberPath(propPath)}";
                    patchLocation.asset = new BlueprinterAsset();
                    patchLocation.asset.name = $"{fileName}";
                    patchLocation.asset.locator = $"{path}";
                    patchLocation.asset.type = GetFormattedTypeName(dependant);
                    patchLocation.hierarchyPath = "";
                    patchLocation.componentType = assemblyQualifiedName;
                    patchLocation.componentIndex = 0;
                    patchLocation.memberPath = FormatMemberPath(propPath);

                    results = T.Add(results, patchLocation);
                }
            }

            return results;
        }
        public void TryAddNewAssetReferance(UnityEngine.Object dependancy, UnityEngine.Object dependant)
        {
            string dependancyPath = AssetDatabase.GetAssetPath(dependancy);
            string dependancyFileName = Path.GetFileNameWithoutExtension(dependancyPath);
            BlueprinterPatchLocation[] dependantPatchLocations = GetDepencancyPatchLocations(dependant, dependancy);

            AssetReferance assetReferance = null;
            int referanceIndex = Array.FindIndex(ReferancesDatabase, entry => entry != null && entry.referance == dependancy);
            if (referanceIndex == -1) {
                assetReferance = new AssetReferance();
                assetReferance.assetName = dependancyFileName;
                assetReferance.referance = dependancy;
                assetReferance.dependents = new AssetReferanceDependant[1];
                assetReferance.dependents[0] = new AssetReferanceDependant();
                assetReferance.dependents[0].dependentObject = dependant;
                assetReferance.dependents[0].dependentLocations = dependantPatchLocations;

                ReferancesDatabase = T.Add(ReferancesDatabase, assetReferance);
            } else {
                assetReferance = ReferancesDatabase[referanceIndex];
                AssetReferanceDependant referanceDependant = null;
                int dependantIndex = Array.FindIndex(assetReferance.dependents, dependantInfo => dependantInfo != null && dependantInfo.dependentObject == dependant);            
                if (dependantIndex == -1) {
                    referanceDependant = new AssetReferanceDependant();
                    referanceDependant.dependentObject = dependant;
                    referanceDependant.dependentLocations = dependantPatchLocations;
                    assetReferance.dependents = T.Add(assetReferance.dependents, referanceDependant);
                } else {
                    referanceDependant = assetReferance.dependents[dependantIndex];
                    foreach (BlueprinterPatchLocation patchLocation in dependantPatchLocations)
                    {
                        referanceDependant.dependentLocations = T.Add(referanceDependant.dependentLocations, patchLocation);
                    }
                }
            }
        }

        public void FindDependancies()
        {
            if (assetBundleName == null) { assetBundleName = ""; } // Sanity Check
            string assetBundleIdentifierName = $"{assetBundleName}.nobp";

            // Debug Check to see if the PatchManifestDefinition's bundle even exists 
            bool bundleExists = AssetDatabase.GetAllAssetBundleNames().Contains(assetBundleIdentifierName);
            if (bundleExists) { BL.Common.Debug.Log($"PatchManifest is a member of this AssetBundle: {assetBundleIdentifierName}"); }
            else { BL.Common.Debug.LogWarning($"PatchManifest is a member of this AssetBundle: {assetBundleIdentifierName}, Which does not exist!"); }

            // Find and list all definitions not already added to the PatchManifestDefinition
            string[] assetBundleContentPaths = AssetDatabase.GetAssetPathsFromAssetBundle(assetBundleIdentifierName);
            string[] assetBundleDependancyPaths = AssetDatabase.GetDependencies(assetBundleContentPaths, true);
            string[] trueAssetDependancyPaths = assetBundleDependancyPaths.Except(assetBundleContentPaths).ToArray(); ;
            BL.Common.Debug.Log($"AssetBundle {assetBundleIdentifierName} contains the following '{assetBundleContentPaths.Length}' asset paths: {string.Join(", ", assetBundleContentPaths)}");
            BL.Common.Debug.Log($"AssetBundle {assetBundleIdentifierName} referances the following '{trueAssetDependancyPaths.Length}' asset paths: {string.Join(", ", trueAssetDependancyPaths)}");
            
            foreach (UnityEngine.Object asset in AddedAssets) {
                string assetPath = AssetDatabase.GetAssetPath(asset);
                string[] directDependencies = AssetDatabase.GetDependencies(assetPath, false);
                foreach (string referancePath in directDependencies) {
                    if (referancePath != null && referancePath != "") { // Sanity Check
                        AssetImporter importer = AssetImporter.GetAtPath(referancePath);
                        BL.Common.Debug.Log($"AssetBundle referance for {importer.assetBundleName}...");

                        if (importer != null) { if (importer.assetBundleName == assetBundleName) continue; } // Same Bundle Sanity Check

                        UnityEngine.Object dependancy = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(referancePath);
                        if (dependancy != null) {            
                            string projectPath = AssetDatabase.GetAssetPath(dependancy);
                            string extension = Path.GetExtension(projectPath);
                            switch (dependancy) {
                                case MonoScript cs: break;
                                case UnityEngine.Object obj when extension == ".cs": break;
                                case UnityEngine.Object obj when extension == ".dll": break;
                                case UnityEngine.Object obj when extension == ".shader": break;
                                case UnityEngine.Object obj when extension == ".png": break;
                                case UnityEngine.Object obj when extension == ".jpg": break;
                                case UnityEngine.Object obj when extension == ".mixer": break;
                                case AudioClip ac: break;
                                default: 
                                    if (!ExternalReferances.Contains(dependancy)) { ExternalReferances = T.Add(ExternalReferances, dependancy); }
                                    TryAddNewAssetReferance(dependancy, asset);
                                break;
                            }
                        }
                    }
                }
            }
        }
        public void GeneratePatches()
        {
            FindAddedAssets();
            FindDependancies();

            PatchManifestPatch[] generatedPatches = new PatchManifestPatch[0];

            foreach (AssetReferance assetReferance in ReferancesDatabase)
            {
                UnityEngine.Object assetReferanceObject = assetReferance.referance;            
                string assetReferancePath = AssetDatabase.GetAssetPath(assetReferanceObject);
                string assetReferanceFileName = Path.GetFileNameWithoutExtension(assetReferancePath);

                BlueprinterPatchLocation[] allPatchLocations = new BlueprinterPatchLocation[0];
                foreach (AssetReferanceDependant dependant in assetReferance.dependents) {
                    foreach (BlueprinterPatchLocation patchLocation in dependant.dependentLocations) { allPatchLocations = T.Add(allPatchLocations, patchLocation); }
                }

                PatchManifestPatch generatedPatch = new PatchManifestPatch();
                generatedPatch.helperName = $"Auto Patch Referance for '{assetReferanceFileName}'";
                generatedPatch.GameAsset = new BlueprinterGameAsset();
                generatedPatch.GameAsset.id = $"'{assetReferanceFileName}' | {GetFormattedTypeName(assetReferanceObject)}";
                generatedPatch.GameAsset.asset = new BlueprinterAsset();
                generatedPatch.GameAsset.asset.name = assetReferanceFileName;
                generatedPatch.GameAsset.asset.locator = assetReferancePath;
                generatedPatch.GameAsset.asset.type = GetFormattedTypeName(assetReferanceObject);
                generatedPatch.PatchLocations = allPatchLocations;
                generatedPatches = T.Add(generatedPatches, generatedPatch);
            }

            Patches = generatedPatches;
        }

        public void GenerateOperations()
        {
            List<PatchManifestOperation> generatedOperations = new List<PatchManifestOperation>(); 
            // Add Units to Encyclopedia
            if (AddedUnits != null && AddedUnits.Length > 0) { // Sanity check to ensure operation is only added when there is something to add...
            PatchManifestOperation AddUnitsToEncyclopediaOperation = new PatchManifestOperation();
            AddUnitsToEncyclopediaOperation.helperName = "Add Units to Encyclopedia";
            AddUnitsToEncyclopediaOperation.opType = BlueprinterOpID.OpAddToEncyclopedia;
            BlueprinterOperationPayload AddUnitsToEncyclopediaOperationPayload = new BlueprinterOperationPayload();
            BlueprinterAsset[] AddUnitsToEncyclopediaOperationPayloadEntries = new BlueprinterAsset[AddedUnits.Length];
            for (int _x = 0; _x < AddedUnits.Length; _x++)
            {
                UnitDefinition unit = AddedUnits[_x];
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
        // Add Factions to Encyclopedia
            if (AddedFactions != null && AddedFactions.Length > 0)
            { // Sanity check to ensure operation is only added when there is something to add...
                PatchManifestOperation AddFactionsToEncyclopediaOperation = new PatchManifestOperation();
                AddFactionsToEncyclopediaOperation.helperName = "Add Factions to Encyclopedia";
                AddFactionsToEncyclopediaOperation.opType = BlueprinterOpID.OpAddToEncyclopedia;
                BlueprinterOperationPayload AddFactionsToEncyclopediaOperationPayload = new BlueprinterOperationPayload();
                BlueprinterAsset[] AddFactionsToEncyclopediaOperationPayloadEntries = new BlueprinterAsset[AddedFactions.Length];
                for (int _x = 0; _x < AddedFactions.Length; _x++)
                {
                    Faction faction = AddedFactions[_x];
                    string entryNamespace = faction.GetType().Namespace;
                    if ((entryNamespace == "") || (entryNamespace == null)) { entryNamespace = "Assembly-CSharp"; }
                    string path = AssetDatabase.GetAssetPath(faction);
                    string factionClassName = Path.GetFileNameWithoutExtension(path);
                    BlueprinterAsset assetReferance = new BlueprinterAsset($"{factionClassName}", $"{path}", $"{faction.GetType().Name}, {entryNamespace}");
                    AddFactionsToEncyclopediaOperationPayloadEntries[_x] = assetReferance;
                }
                AddFactionsToEncyclopediaOperationPayload.entries = AddFactionsToEncyclopediaOperationPayloadEntries;
                AddFactionsToEncyclopediaOperation.payload = AddFactionsToEncyclopediaOperationPayload;
                generatedOperations.Add(AddFactionsToEncyclopediaOperation);
            }
        // Add WeaponMounts to Encyclopedia
        if (AddedWeaponMounts != null && AddedWeaponMounts.Length > 0) { // Sanity check to ensure operation is only added when there is something to add...
            PatchManifestOperation AddWeaponMountsToEncyclopediaOperation = new PatchManifestOperation();
            AddWeaponMountsToEncyclopediaOperation.helperName = "Add Weapon Mounts to Encyclopedia";
            AddWeaponMountsToEncyclopediaOperation.opType = BlueprinterOpID.OpAddToEncyclopedia;
            BlueprinterOperationPayload AddWeaponMountsToEncyclopediaOperationPayload = new BlueprinterOperationPayload();
            BlueprinterAsset[] AddWeaponMountsToEncyclopediaOperationPayloadEntries = new BlueprinterAsset[AddedWeaponMounts.Length];
            for (int _x = 0; _x < AddedWeaponMounts.Length; _x++)
            {
                WeaponMount weaponMount = AddedWeaponMounts[_x];
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
            BL.Common.Debug.Log($"Found {AddedPylonsToVehicle.Length} pylons to add to weapon managers ...");
            if (AddedPylonsToVehicle.Length > 0) { // Sanity check to ensure no errors are produced
            for (int _x = 0; _x < AddedPylonsToVehicle.Length; _x++)
            {
                WeaponPylonAddition weaponPylonAddition = AddedPylonsToVehicle[_x];   
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
            Ops = new PatchManifestOperation[generatedOperations.Count()];
            for (int _x = 0; _x < generatedOperations.Count(); _x++)
            {
                Ops[_x] = generatedOperations[_x];
            }
        }

        public void Generate()
        {
            Clean();

            FindAddedAssets();
            FindDependancies();

            if (overwriteMode && autoAddPatches) { GeneratePatches(); }
            if (overwriteMode && autoAddOperations) { GenerateOperations(); }
        }

        private static List<string> ScanSerializedFields(UnityEngine.Object obj, UnityEngine.Object dependency)
        {
            List<string> paths = new List<string>();
            using (SerializedObject serializedObj = new SerializedObject(obj))
            {
                SerializedProperty iterator = serializedObj.GetIterator();
                while (iterator.NextVisible(true))
                {
                    if (iterator.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        if (iterator.objectReferenceValue == dependency)
                        {
                            paths.Add(iterator.propertyPath);
                        }
                    }
                }
            }
            return paths;
        }
        private static string GetCleanHierarchyPath(GameObject root, GameObject target)
        {
            if (root == target) return string.Empty;

            List<string> pathSegments = new List<string>();
            Transform current = target.transform;

            while (current != null && current.gameObject != root)
            {
                pathSegments.Insert(0, current.name);
                current = current.parent;
            }

            return string.Join("/", pathSegments);
        }
        private static string GetFormattedTypeName(UnityEngine.Object obj)
        {
            Type type = obj.GetType();
            string assemblyName = type.Assembly.GetName().Name;
            return $"{type.FullName}, {assemblyName}";
        }
        private static string FormatMemberPath(string rawPath)
        {
            if (rawPath.Contains(".Array.data["))
            {
                return rawPath.Replace(".Array.data[", "[");
            }
            return rawPath;
        }
        //public PatchManifestOperation GenerateOperation(BlueprinterOpID opID, BlueprinterOperationPayload payload) {  }
    }
}