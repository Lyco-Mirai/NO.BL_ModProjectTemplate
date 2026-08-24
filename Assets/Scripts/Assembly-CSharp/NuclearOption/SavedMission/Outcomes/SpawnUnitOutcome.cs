using System;
using System.Collections.Generic;
using NuclearOption.MissionEditorScripts;
using NuclearOption.NodeGraph;
using UnityEngine;

namespace NuclearOption.SavedMission.Outcomes
{
	internal class SpawnUnitOutcome : Outcome
	{
		private static readonly List<SavedUnit> Ordered = new List<SavedUnit>();

		public List<SavedUnit> UnitsToSpawn;

		private static readonly Type[] TypeOrder = new Type[7]
		{
			typeof(SavedBuilding),
			typeof(SavedContainer),
			typeof(SavedPilot),
			typeof(SavedShip),
			typeof(SavedVehicle),
			typeof(SavedMissile),
			typeof(SavedAircraft)
		};

		public SpawnUnitSavedOutcome Saved => (SpawnUnitSavedOutcome)SavedOutcome;

		public SpawnUnitOutcome(SpawnUnitSavedOutcome savedOutcome)
			: base(savedOutcome)
		{
		}

		public override void CopyFrom(Outcome original)
		{
			base.CopyFrom(original);
			SpawnUnitOutcome spawnUnitOutcome = (SpawnUnitOutcome)original;
			if (spawnUnitOutcome.UnitsToSpawn != null)
			{
				UnitsToSpawn = new List<SavedUnit>(spawnUnitOutcome.UnitsToSpawn);
			}
		}

		public override void Load(MissionLookups lookups)
		{
			base.Load(lookups);
			UnitsToSpawn = new List<SavedUnit>();
			foreach (string item in Saved.UnitsToSpawn)
			{
				if (lookups.SavedUnits.TryGetValue(item, out var value))
				{
					UnitsToSpawn.Add(value);
				}
				else
				{
					lookups.LoadErrors.AddWarn("'" + item + "' was not found in lookup for SavedUnit");
				}
			}
		}

		public override void Save()
		{
			base.Save();
			Saved.UnitsToSpawn.Clear();
			foreach (SavedUnit item in UnitsToSpawn)
			{
				Saved.UnitsToSpawn.Add(item.GetNameSavedCheckDestroyed());
			}
		}

		public override void ReferenceDestroyed(ISaveableReference reference)
		{
			UnitsToSpawn.RemoveAll((SavedUnit x) => x.SavedReferenceEquals(reference));
		}

		public override void Complete(Objective completedObjective)
		{
			Spawner i = NetworkSceneSingleton<Spawner>.i;
			Ordered.Clear();
			Ordered.AddRange(UnitsToSpawn);
			Ordered.Sort(Comparison);
			foreach (SavedUnit item in Ordered)
			{
				if (!item.HasSpawned)
				{
					item.HasSpawned = true;
					i.SpawnSavedUnit(item);
				}
			}
			Physics.SyncTransforms();
			Ordered.Clear();
		}

		private int Comparison(SavedUnit x, SavedUnit y)
		{
			int num = Array.IndexOf(TypeOrder, x.GetType());
			int num2 = Array.IndexOf(TypeOrder, y.GetType());
			return num - num2;
		}

		public override void DrawData(DataDrawer drawer)
		{
			if (UnitsToSpawn == null)
			{
				UnitsToSpawn = new List<SavedUnit>();
			}
			drawer.DrawList(300, UnitsToSpawn, includeBuiltIn: false);
		}

		public override void AddPins(GraphNodeData data)
		{
			data.InputElements.Add(new GraphReadOnlyFieldData
			{
				PinId = new PinId("Units Count"),
				DisplayName = "Units Count",
				GetText = () => (UnitsToSpawn == null) ? "0" : UnitsToSpawn.Count.ToString()
			});
		}
	}
}
