using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.IO;
using System.Collections.Generic;

namespace BL.Blueprinter
{    
    // Adds all common vehicles for quick access ...
    public enum VehicleModel
    {
        None,
        [InspectorName("Vanilla/Aircraft/CI-22 'Cricket'")] COIN, // Playtest
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
    public class WeaponMountEntry
    {        
        [Tooltip("Vehicle to add the weapon mounts to, arranged as a fast, easy-to-use list dictionary...")]
        public VehicleModel vehicle;
        [Tooltip("Specific unit definition for when then 'vehicle' dropdown does not contain the desired target vehicle...")]
        public UnitDefinition? vehicleDefinition;
        [Tooltip("Specific Blueprinter asset reference for when both the 'vehicle' dropdown and specific 'vehicleDefinition' cannot be used...")]
        public BlueprinterAsset? vehicleAssetReferance;
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