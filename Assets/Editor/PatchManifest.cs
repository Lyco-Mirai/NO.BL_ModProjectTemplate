using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.IO;
using System.Collections.Generic;

namespace BL.Blueprinter
{
    [CreateAssetMenu(fileName = "NewItemData", menuName = "ScriptableObjects/Patch Manifest")]
    [System.Serializable]
    public class PatchManifest : ScriptableObject
    {
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

        [Header("Patch Manifest Automation")]
            [SerializeField, Tooltip("Enable to make the additions below take priority and likely overwrite manual changes above and below...")]
            public bool overwriteMode;
            [SerializeField, Tooltip("Enable to add any referance outside the current assetbundle as a patch...")]
            public bool autoAddPatches;
            [SerializeField, Tooltip("Enable to add all entities below to the Encyclopedia Automatically...")]
            public bool autoAddOperations;
            [SerializeField, Tooltip("Enable to copy this scriptableObject into the AssetBundle too, may drag extra referances...")]
            public bool copyDefinitionToAssetbundle = false;
            public UnitDefinition[] AddedUnits;
            public WeaponMount[] AddedWeaponMounts;
            public WeaponInfo[] AddedWeaponInfos;
        //Dependancy listing, not needed for assetbundles, done in the plugin...
            //public PatchManifest[] Dependancies;
    }
}