using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BL.Blueprinter {
    [System.Serializable]
    public class BlueprinterAsset
    {
        public string name;
        public string locator;
        public string type;
    }
    [System.Serializable]
    public class BlueprinterGameAsset
    {
        public string id;
        public BlueprinterAsset asset;
    }
    [System.Serializable]
    public class BlueprinterPatchLocation
    {
        public string id;
        public BlueprinterAsset asset;
        public string hierarchyPath;
        public string componentType;
        public int componentIndex;
        public string memberPath;
    }
    [System.Serializable]
    public class PatchManifestPatch
    {
        public string helperName = "Patch";
        public BlueprinterGameAsset GameAsset;
        public BlueprinterPatchLocation[] PatchLocations;
    }
    [System.Serializable]
    public enum BlueprinterOpID
    {
        OpAddToEncyclopedia,
        OpAddWeaponMountToWeaponManager
    }
    [System.Serializable]
    public class BlueprinterWeaponManager
    {
        public string helperName = "Vehicle";
        public BlueprinterGameAsset GameAsset;
        public int[] hardpointSetIndices;
    }
    [System.Serializable]
    public class BlueprinterAddToEncyclopediaOperationPayload
    {
        public BlueprinterAsset[] entries;
    }
    [System.Serializable]
    public class BlueprinterAddToWeaponManagerOperationPayload
    {
        public BlueprinterAsset bundleAsset;
        public BlueprinterWeaponManager[] weaponManagers;
    }
    [System.Serializable]
    public class BlueprinterOperationPayload
    {
        public BlueprinterAsset[] entries;
        public BlueprinterAsset bundleAsset;
        public BlueprinterWeaponManager[] weaponManagers;
        public string json(BlueprinterOpID opId) {
            if (opId == BlueprinterOpID.OpAddToEncyclopedia)
            {
                var playload = new BlueprinterAddToEncyclopediaOperationPayload();
                playload.entries = entries;
                return JsonUtility.ToJson(playload, false);
            }
            else if (opId == BlueprinterOpID.OpAddWeaponMountToWeaponManager)
            {
                var playload = new BlueprinterAddToWeaponManagerOperationPayload();
                playload.bundleAsset = bundleAsset;
                playload.weaponManagers = weaponManagers;
                return JsonUtility.ToJson(playload, false);
            }
            else
            {
                return "";
            }
        }
    }
    [System.Serializable]
    public class PatchManifestOperation
    {
        public string helperName = "Operation";
        public BlueprinterOpID opType;
        public BlueprinterOperationPayload payload;
        [HideInInspector]
        public string opId;
        [HideInInspector]
        public string payloadJson;

        public void GeneratePayloadJSON()
        {
            opId = GetOpIDName(opType);
            payloadJson = payload.json(opType);
        }
        public string GetOpIDName(BlueprinterOpID opType)
        {
            string _result = "NoOp";
            if (opType == BlueprinterOpID.OpAddToEncyclopedia) { _result = "OpAddToEncyclopedia"; }
            if (opType == BlueprinterOpID.OpAddWeaponMountToWeaponManager) { _result = "OpAddWeaponMountToWeaponManager"; }
            return _result;
        }
    }

    [System.Serializable]
    public class PatchManifestPatchJSON
    {
        public BlueprinterGameAsset GameAsset;
        public BlueprinterPatchLocation[] PatchLocations;
    }
    [System.Serializable]
    public class PatchManifestOperationJSON
    {
        public string opId;
        public string payloadJson;
    }
    [System.Serializable]
    public class PatchManifestJSON
    {
        public string modName;
        public int schemaVersion;
        public string modVersion;
        public PatchManifestPatchJSON[] Patches;
        public PatchManifestOperationJSON[] Ops;

        public void AddPatch(PatchManifestPatchJSON patch)
        {
            if (Patches != null)
            {
                PatchManifestPatchJSON[] _newArray = new PatchManifestPatchJSON[Patches.Length + 1];
                for (int _x = 0; _x < Patches.Length; _x++) { _newArray[_x] = Patches[_x]; }
                _newArray[_newArray.Length - 1] = patch;
                Patches = _newArray;
            }
            else
            {
                PatchManifestPatchJSON[] _newArray = new PatchManifestPatchJSON[1];
                _newArray[0] = patch;
                Patches = _newArray;
            }
        }

        public void AddOp(PatchManifestOperationJSON op)
        {
            if (Ops != null)
            {
                PatchManifestOperationJSON[] _newArray = new PatchManifestOperationJSON[Ops.Length + 1];
                for (int _x = 0; _x < Ops.Length; _x++) { _newArray[_x] = Ops[_x]; }
                _newArray[_newArray.Length - 1] = op;
                Ops = _newArray;
            }
            else
            {
                PatchManifestOperationJSON[] _newArray = new PatchManifestOperationJSON[1];
                _newArray[0] = op;
                Ops = _newArray;
            }
        }
    }
}