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
        [InspectorName("Vanilla/Navy/Surf-class Patrol Boat")] PatrolBoat1 // Version 0.34

        //[InspectorName("Aryx/Aircraft/MiG-15 'Fagot'")] PatrolBoat
    }
    [System.Serializable]
    public class VehicleModelType
    {
        public string inspectorName;
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
            inspectorName = initInpectorName;
            vehicleUpdate = initVehicleUpdate;
            vehicleModel = initVehicleModel;
            blueprinterAsset = initBlueprinterAsset;
        }  
    }
    public static class VehicleModelTypeDatabase
    {
        // 1. Define individual pre-made static configurations
        static string type = "";
        // Vanilla Aircraft
        /*
        public static readonly VehicleModelType COIN = new VehicleModelType("Vanilla/Aircraft/CI-22 'Cricket'", "0.19.0", VehicleModel.COIN, new BlueprinterAsset("COIN","BaseGame",type));
        public static readonly VehicleModelType SFB = new VehicleModelType("Vanilla/Aircraft/SFB-81 'Darkreach'", "0.19.0", VehicleModel.SFB, new BlueprinterAsset("SFB","BaseGame",type));
        public static readonly VehicleModelType Trainer = new VehicleModelType("Vanilla/Aircraft/T⁄A-30 'Compass'", "0.20.0", VehicleModel.Trainer, new BlueprinterAsset("Trainer","BaseGame",type));
        public static readonly VehicleModelType AttackHelo1 = new VehicleModelType("Vanilla/Aircraft/SAH-46 'Chicane'", "0.24.0", VehicleModel.AttackHelo1, new BlueprinterAsset("AttackHelo1","BaseGame",type));
        public static readonly VehicleModelType EW1 = new VehicleModelType("Vanilla/Aircraft/EW-25 'Medusa'", "0.27.0", VehicleModel.EW1, new BlueprinterAsset("EW1","BaseGame",type));
        public static readonly VehicleModelType Multirole1 = new VehicleModelType("Vanilla/Aircraft/KR-67 'Ifrit'", "0.28.0", VehicleModel.Multirole1, new BlueprinterAsset("Multirole1","BaseGame",type));
        public static readonly VehicleModelType QuadVTOL1 = new VehicleModelType("Vanilla/Aircraft/VL-49 'Tarantula'", "0.29.0", VehicleModel.QuadVTOL1, new BlueprinterAsset("QuadVTOL1","BaseGame",type));
        public static readonly VehicleModelType SmallFighter1 = new VehicleModelType("Vanilla/Aircraft/FS-20 'Vortex'", "0.30.0", VehicleModel.SmallFighter1, new BlueprinterAsset("SmallFighter1","BaseGame",type));
        public static readonly VehicleModelType UtilityHelo1 = new VehicleModelType("Vanilla/Aircraft/UH-90 'Ibis'", "0.31.0", VehicleModel.UtilityHelo1, new BlueprinterAsset("UtilityHelo1","BaseGame",type));
        public static readonly VehicleModelType CAS1 = new VehicleModelType("Vanilla/Aircraft/A-19 'Brawler'", "0.32.0", VehicleModel.CAS1, new BlueprinterAsset("CAS1","BaseGame",type));
        public static readonly VehicleModelType FastBomber1 = new VehicleModelType("Vanilla/Aircraft/AB-4 'Alkyon'", "0.33.0", VehicleModel.FastBomber1, new BlueprinterAsset("FastBomber1","BaseGame",type));
        public static readonly VehicleModelType VTOLTrainer1 = new VehicleModelType("Vanilla/Aircraft/VT-7 'Vagrant'", "0.34.0", VehicleModel.VTOLTrainer1, new BlueprinterAsset("VTOLTrainer1","BaseGame",type));
        */

        public static DropdownList<VehicleModelType> GetVehicles()
        {
            var dropdown = new DropdownList<VehicleModelType>();
            string type = "";
            string inspectorName = "";

            // Vanilla
                // Vanilla Aircraft
                inspectorName = "Vanilla/Aircraft/CI-22 'Cricket'"; dropdown.Add(inspectorName, new VehicleModelType(inspectorName, "0.19.0", VehicleModel.COIN, new BlueprinterAsset("COIN","BaseGame",type)));
                inspectorName = "Vanilla/Aircraft/SFB-81 'Darkreach'"; dropdown.Add(inspectorName, new VehicleModelType(inspectorName, "0.19.0", VehicleModel.SFB, new BlueprinterAsset("SFB","BaseGame",type)));
                inspectorName = "Vanilla/Aircraft/FS-12 'Revoker'"; dropdown.Add(inspectorName, new VehicleModelType(inspectorName, "0.19.0", VehicleModel.Fighter1, new BlueprinterAsset("Fighter1","BaseGame",type)));
                inspectorName = "Vanilla/Aircraft/T⁄A-30 'Compass'"; dropdown.Add(inspectorName, new VehicleModelType(inspectorName, "0.20.0", VehicleModel.Trainer, new BlueprinterAsset("Trainer","BaseGame",type)));
                // Vanilla Sea
                inspectorName = "Vanilla/Watercraft/Shard-class Corvette"; dropdown.Add(inspectorName, new VehicleModelType(inspectorName, "0.24.0", VehicleModel.Corvette1, new BlueprinterAsset("Corvette1","BaseGame",type)));
                dropdown.Add("Vanilla/Watercraft/Hyperion-class Fleet Carrier", new VehicleModelType("0.28.0", VehicleModel.FleetCarrier1, new BlueprinterAsset("FleetCarrier1","BaseGame",type)));
                dropdown.Add("Vanilla/Watercraft/Dynamo-class Destroyer", new VehicleModelType("0.29.0", VehicleModel.Destroyer1, new BlueprinterAsset("Destroyer1","BaseGame",type)));
                dropdown.Add("Vanilla/Watercraft/Annex-class Assault Carrier", new VehicleModelType("0.30.0", VehicleModel.AssaultCarrier1, new BlueprinterAsset("AssaultCarrier1","BaseGame",type)));
                dropdown.Add("Vanilla/Watercraft/OTB-31 LCAC", new VehicleModelType("0.30.0", VehicleModel.LandingCraft1, new BlueprinterAsset("LandingCraft1","BaseGame",type)));
                dropdown.Add("Vanilla/Watercraft/Argus-class Missile Frigate", new VehicleModelType("0.33.0", VehicleModel.Frigate1, new BlueprinterAsset("Frigate1","BaseGame",type)));
                dropdown.Add("Vanilla/Watercraft/Cursor-class LFD", new VehicleModelType("0.33.0", VehicleModel.SmallCarrier1, new BlueprinterAsset("SmallCarrier1","BaseGame",type)));
                dropdown.Add("Vanilla/Watercraft/Surf-class Patrol Boat", new VehicleModelType("0.34.0", VehicleModel.PatrolBoat1, new BlueprinterAsset("PatrolBoat1","BaseGame",type)));
                // Vanilla Land
                dropdown.Add("Vanilla/Land/LCV-45 Recon Truck", new VehicleModelType("0.20.0", VehicleModel.None, new BlueprinterAsset("","BaseGame",type)));
                dropdown.Add("Vanilla/Land/HLT Truck (Munitions)", new VehicleModelType("0.20.0", VehicleModel.None, new BlueprinterAsset("","BaseGame",type)));
                dropdown.Add("Vanilla/Land/HLT Truck (Fuel)", new VehicleModelType("0.20.0", VehicleModel.None, new BlueprinterAsset("","BaseGame",type)));
                dropdown.Add("Vanilla/Land/HLT Truck (Radar)", new VehicleModelType("0.20.0", VehicleModel.None, new BlueprinterAsset("","BaseGame",type)));
                dropdown.Add("Vanilla/Land/AeroSentry SPAAG", new VehicleModelType("0.20.0", VehicleModel.None, new BlueprinterAsset("","BaseGame",type)));
                dropdown.Add("Vanilla/Land/Anvil SPAAG", new VehicleModelType("0.21.0", VehicleModel.None, new BlueprinterAsset("","BaseGame",type)));
                dropdown.Add("Vanilla/Land/AFV8 IFV", new VehicleModelType("0.25.0", VehicleModel.None, new BlueprinterAsset("","BaseGame",type)));
                dropdown.Add("Vanilla/Land/HLT Truck (Tractor)", new VehicleModelType("0.26.0", VehicleModel.None, new BlueprinterAsset("","BaseGame",type)));
                dropdown.Add("Vanilla/Land/HLT Truck (Flatbed)", new VehicleModelType("0.26.0", VehicleModel.None, new BlueprinterAsset("","BaseGame",type)));
                dropdown.Add("Vanilla/Land/LCV-25 (AA)", new VehicleModelType("0.29.0", VehicleModel.None, new BlueprinterAsset("","BaseGame",type)));
                dropdown.Add("Vanilla/Land/LCV-25 (AT)", new VehicleModelType("0.29.0", VehicleModel.None, new BlueprinterAsset("","BaseGame",type)));
                dropdown.Add("Vanilla/Land/AFV6 (AA)", new VehicleModelType("0.29.0", VehicleModel.None, new BlueprinterAsset("","BaseGame",type)));
                dropdown.Add("Vanilla/Land/AFV6 (AT)", new VehicleModelType("0.29.0", VehicleModel.None, new BlueprinterAsset("","BaseGame",type)));
                dropdown.Add("Vanilla/Land/AFV6 (IFV)", new VehicleModelType("0.29.0", VehicleModel.None, new BlueprinterAsset("","BaseGame",type)));
                dropdown.Add("Vanilla/Land/AFV6 (APC)", new VehicleModelType("0.29.0", VehicleModel.None, new BlueprinterAsset("","BaseGame",type)));
                // Vanilla Static
                dropdown.Add("Vanilla/Static/StratoLance R9 Launcher", new VehicleModelType("0.25.0", VehicleModel.None, new BlueprinterAsset("","BaseGame",type)));
            // BL
                // BL Aircraft
                dropdown.Add("BL/Aircraft/RAH-66 'Commanche'", new VehicleModelType(VehicleModel.None, new BlueprinterAsset("","BaseGame",type)));
            // Aryx
                // Aryx Aircraft
                dropdown.Add("Aryx3D/Aircraft/MiG-15 'Fagot'", new VehicleModelType(VehicleModel.None, new BlueprinterAsset("","BaseGame",type)));
                dropdown.Add("Aryx3D/Aircraft/F-16M 'Fighting Falcon'", new VehicleModelType(VehicleModel.None, new BlueprinterAsset("","BaseGame",type)));
                dropdown.Add("Aryx3D/Aircraft/MC-260 'Chimera'", new VehicleModelType(VehicleModel.None, new BlueprinterAsset("","BaseGame",type)));
                dropdown.Add("Aryx3D/Aircraft/RAH-72 'Knockout'", new VehicleModelType(VehicleModel.None, new BlueprinterAsset("","BaseGame",type)));
                dropdown.Add("Aryx3D/Aircraft/F-99 'Shrike'", new VehicleModelType(VehicleModel.None, new BlueprinterAsset("","BaseGame",type)));
                dropdown.Add("Aryx3D/Aircraft/FS-41 'Eclipse'", new VehicleModelType(VehicleModel.None, new BlueprinterAsset("","BaseGame",type)));
            
            return dropdown;
        }
    }
    [System.Serializable]
    public class WeaponMountEntry
    {        
        [Tooltip("Vehicle to add the weapon mounts to, arranged as a fast, easy-to-use list dictionary...")]
        public static readonly DropdownList<VehicleModelType> VehicleModelDictionary = VehicleModelTypeDatabase.GetVehicles();
        [Dropdown("VehicleModelDictionary")]
        public VehicleModelType vehicle;
        public bool isEnabled;
        public bool isEventContent;
        public int[] pylonIndexes;
    }
    [System.Serializable]
    public class WeaponPylonAddition
    {
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