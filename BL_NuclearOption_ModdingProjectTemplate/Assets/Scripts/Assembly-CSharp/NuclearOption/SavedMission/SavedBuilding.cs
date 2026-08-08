using System;
using UnityEngine;

namespace NuclearOption.SavedMission
{
	[Serializable]
	public class SavedBuilding : SavedUnit
	{
		[Serializable]
		public class FactoryOptions : ICloneable
		{
			public string productionType = "";

			public float productionTime = 900f;

			public FactoryOptions()
			{
				productionTime = 900f;
				productionType = string.Empty;
			}

			public object Clone()
			{
				return new FactoryOptions
				{
					productionTime = productionTime,
					productionType = productionType
				};
			}
		}

		public bool capturable;

		public string Airbase = "";

		public FactoryOptions factoryOptions;

		public SavedAirbase AirbaseRef { get; private set; }

		public SavedBuilding()
		{
		}

		public SavedBuilding(string uniqueName)
			: base(uniqueName)
		{
		}

		protected override void SetOverrideDefaultValues(Unit unit)
		{
			base.SetOverrideDefaultValues(unit);
			Building building = (Building)unit;
			capturable = building.capturable;
			Airbase = ((building.MapAirbase != null) ? building.MapAirbase.SavedAirbase.UniqueName : "");
			if (building.TryGetComponent<Factory>(out var component))
			{
				if (factoryOptions == null)
				{
					factoryOptions = new FactoryOptions();
				}
				factoryOptions.productionTime = component.ProductionInterval;
				UnitDefinition productionUnit = component.ProductionUnit;
				factoryOptions.productionType = ((productionUnit != null) ? productionUnit.jsonKey : "");
			}
		}

		public void LoadAirbaseRef()
		{
			if (!string.IsNullOrEmpty(Airbase))
			{
				if (MissionManager.TryFindSavedAirbase(Airbase, out var saved))
				{
					SetAirbase(saved);
				}
				else
				{
					Debug.LogError("Failed to find airbase with name " + Airbase);
				}
			}
		}

		public void SaveAirbaseString()
		{
			Airbase = AirbaseRef?.UniqueName;
		}

		public void SetOrRemoveAirbase(SavedAirbase airbase)
		{
			if (airbase != null)
			{
				SetAirbase(airbase);
			}
			else
			{
				RemoveAirbase(airbase);
			}
		}

		public void SetAirbase(SavedAirbase airbase)
		{
			if (AirbaseRef != null && AirbaseRef != airbase)
			{
				bool clearOldAirbaseFields = AirbaseRef.UniqueName != airbase.UniqueName;
				RemoveAirbase(AirbaseRef, clearOldAirbaseFields);
			}
			AirbaseRef = airbase;
			Airbase = airbase.UniqueName;
			if (!airbase.BuildingsRef.Contains(this))
			{
				airbase.BuildingsRef.Add(this);
			}
			faction = airbase.faction;
			if (Unit != null)
			{
				Unit.NetworkHQ = FactionRegistry.HqFromName(faction);
			}
		}

		public void RemoveAirbase(SavedAirbase hint = null, bool clearOldAirbaseFields = true)
		{
			if (AirbaseRef != null && clearOldAirbaseFields)
			{
				if (AirbaseRef.BuildingsRef.Contains(this))
				{
					AirbaseRef.BuildingsRef.Remove(this);
				}
				if (AirbaseRef.TowerRef == this)
				{
					AirbaseRef.TowerRef = null;
					AirbaseRef.Tower = "";
				}
			}
			AirbaseRef = null;
			Airbase = "";
		}

		public void ReferenceReplaced(ISaveableReference oldRef, ISaveableReference newRef)
		{
			if (oldRef is SavedAirbase savedAirbase && newRef is SavedAirbase airbase && AirbaseRef == savedAirbase)
			{
				SetAirbase(airbase);
			}
		}
	}
}
