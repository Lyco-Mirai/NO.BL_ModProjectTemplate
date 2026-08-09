using NuclearOption.Workshop;
using UnityEngine;

namespace NuclearOption.AddressableScripts.ModFoldersImpl
{
	public abstract class Skins : ModLoader<LiveryMetaData, LiveryData>
	{
		public override string Label => "Skin";

		public static bool CanLoad(LiveryKey key, Aircraft aircraft, out string folder)
		{
			string faction = ((aircraft.NetworkHQ != null) ? aircraft.NetworkHQ.faction.factionName : "");
			return CanLoad(key, aircraft.definition, faction, out folder);
		}

		public static bool CanLoad(LiveryKey key, AircraftDefinition aircraft, string faction, out string catalogFolder)
		{
			if (key.Type == LiveryKey.KeyType.Builtin)
			{
				catalogFolder = null;
				return true;
			}
			if (key.Type == LiveryKey.KeyType.AppData)
			{
				catalogFolder = AppDataSkins.GetCatalogFolder(key.AppDataName);
			}
			else
			{
				if (key.Type != LiveryKey.KeyType.Workshop)
				{
					Debug.LogError("Invalid key");
					catalogFolder = null;
					return false;
				}
				if (!SteamWorkshop.TryGetInstallFolder(key.WorkshopId, out catalogFolder))
				{
					return false;
				}
			}
			LiveryMetaData? liveryMetaData = ModLoader.ReadMetaData<LiveryMetaData>(key.WorkshopId, catalogFolder);
			if (!liveryMetaData.HasValue)
			{
				return false;
			}
			return CheckMetaData(liveryMetaData.Value, aircraft, faction);
		}

		private static bool CheckMetaData(LiveryMetaData data, AircraftDefinition aircraft, string faction)
		{
			if (!data.CheckAircraft(aircraft))
			{
				Debug.LogError("Aircraft did not match meta data. data.Aircraft=" + data.Aircraft + ", data.AircraftKey=" + data.AircraftKey + " aircraft=" + aircraft.unitName);
				return false;
			}
			if (!string.IsNullOrEmpty(data.Faction) && !string.IsNullOrEmpty(faction) && data.Faction != faction)
			{
				Debug.LogError("Faction did not match meta data. data=" + data.Faction + " faction=" + faction);
				return false;
			}
			return true;
		}
	}
}
