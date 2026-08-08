using System;
using System.Collections.Generic;

namespace NuclearOption.SavedMission.ConvertVersions
{
	[Serializable]
	[Obsolete("V5", true)]
	public class Mission_V5_OLD
	{
		public int JsonVersion;

		[Obsolete("WorkshopId has been moved to workshop.json", true)]
		public ulong WorkshopId;

		public MapKey_V5_OLD MapKey;

		public MissionSettings_V5_OLD missionSettings = new MissionSettings_V5_OLD();

		public MissionEnvironment_V5_OLD environment = new MissionEnvironment_V5_OLD();

		public List<SavedAircraft_V5_OLD> aircraft = new List<SavedAircraft_V5_OLD>();

		public List<SavedVehicle_V5_OLD> vehicles = new List<SavedVehicle_V5_OLD>();

		public List<SavedShip_V5_OLD> ships = new List<SavedShip_V5_OLD>();

		public List<SavedBuilding_V5_OLD> buildings = new List<SavedBuilding_V5_OLD>();

		public List<SavedScenery_V5_OLD> scenery = new List<SavedScenery_V5_OLD>();

		public List<SavedContainer_V5_OLD> containers = new List<SavedContainer_V5_OLD>();

		public List<SavedMissile_V5_OLD> missiles = new List<SavedMissile_V5_OLD>();

		public List<SavedPilot_V5_OLD> pilots = new List<SavedPilot_V5_OLD>();

		public List<MissionFaction_V5_OLD> factions = new List<MissionFaction_V5_OLD>();

		public List<SavedAirbase_V5_OLD> airbases = new List<SavedAirbase_V5_OLD>();

		public List<UnitInventory_V5_OLD> unitInventories = new List<UnitInventory_V5_OLD>();

		public SavedMissionObjectives_V5_OLD objectives;
	}
}
