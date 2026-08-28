using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using NaughtyAttributes;

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
    public class WeaponMountEntry
    {        
        [Tooltip("Vehicle to add the weapon mounts to, arranged as a fast, easy-to-use list dictionary...")]
        public VehicleModel vehicle;
        [HideInInspector]
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
        [Header("Patch Manifest Automation")]
            [SerializeField, Tooltip("Enable to make the additions below take priority and likely overwrite manual changes above and below...")]
            public bool overwriteMode;
            [SerializeField, Tooltip("Enable to add any referance outside the current assetbundle as a patch...")]
            public bool autoAddPatches;
            [SerializeField, Tooltip("Enable to add all entities below to the Encyclopedia Automatically...")]
            public bool autoAddOperations;
            [SerializeField, Tooltip("Enable to copy this scriptableObject into the AssetBundle too, may drag extra referances...")]
            public bool copyPatchDefinitionToAssetBundle = false;
            public UnitDefinition[] AddedUnits;
            public WeaponMount[] AddedWeaponMounts;
            public WeaponInfo[] AddedWeaponInfos;
            public WeaponPylonAddition[] AddedPylonsToVehicle;
        //Dependancy listing, not needed for assetbundles, done in the plugin...
            //public PatchManifest[] Dependancies;

        [Header("Manual Patch Manifest Modification")]
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
    }
}