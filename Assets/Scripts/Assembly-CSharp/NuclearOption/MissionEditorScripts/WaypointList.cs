using System;
using System.Collections.Generic;
using NuclearOption.MissionEditorScripts.MultiSelect;
using NuclearOption.SavedMission;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts
{
	public class WaypointList : MonoBehaviour
	{
		[SerializeField]
		private Button addWaypointButton;

		[SerializeField]
		private WaypointEntry waypointEntryPrefab;

		[SerializeField]
		private GameObject waypointsNotSameOverlay;

		[SerializeField]
		private Transform waypointsPanel;

		private List<WaypointEntry> entries = new List<WaypointEntry>();

		private List<string> objectives;

		private MultiSelect<SavedUnit> targets;

		private void Awake()
		{
			addWaypointButton.onClick.AddListener(AddWaypoint);
		}

		public void Setup(MultiSelect<SavedUnit> targets)
		{
			this.targets = targets;
			bool flag = targets.AllTheSame((MultiSelect<SavedUnit>.GetField<IReadOnlyList<VehicleWaypoint>>)GetWaypoints);
			waypointsNotSameOverlay.SetActive(!flag);
			IReadOnlyList<VehicleWaypoint> waypoints;
			if (!flag)
			{
				IReadOnlyList<VehicleWaypoint> readOnlyList = Array.Empty<VehicleWaypoint>();
				waypoints = readOnlyList;
			}
			else
			{
				IReadOnlyList<VehicleWaypoint> readOnlyList = GetWaypoints(targets.Targets[0]);
				waypoints = readOnlyList;
			}
			Setup(waypoints);
		}

		private List<VehicleWaypoint> GetWaypoints(SavedUnit unit)
		{
			if (!(unit is SavedVehicle { waypoints: var waypoints }))
			{
				if (!(unit is SavedShip { waypoints: var waypoints2 }))
				{
					throw new NotSupportedException($"{unit.GetType()} is not supported for GetWaypoints");
				}
				return waypoints2;
			}
			return waypoints;
		}

		public void Setup(IReadOnlyList<VehicleWaypoint> waypoints)
		{
			foreach (WaypointEntry entry in entries)
			{
				UnityEngine.Object.Destroy(entry.gameObject);
			}
			entries.Clear();
			objectives = GetObjectives();
			for (int i = 0; i < waypoints.Count; i++)
			{
				WaypointEntry item = UnityEngine.Object.Instantiate(waypointEntryPrefab, waypointsPanel);
				entries.Add(item);
			}
			SetupAllWithIndex();
		}

		public List<string> GetObjectives()
		{
			List<string> list = new List<string>();
			list.Add("Unit Spawn");
			foreach (Objective allObjective in MissionManager.Objectives.AllObjectives)
			{
				string nameSavedCheckDestroyed = allObjective.GetNameSavedCheckDestroyed();
				list.Add(nameSavedCheckDestroyed);
			}
			return list;
		}

		private void SetupAllWithIndex()
		{
			List<VehicleWaypoint> waypoints = GetWaypoints(targets.Targets[0]);
			for (int i = 0; i < entries.Count; i++)
			{
				int i2 = i;
				VehicleWaypoint waypoint = waypoints[i2];
				MultiField<GlobalPosition> positionField = new MultiField<GlobalPosition>(() => waypoint.position, delegate(GlobalPosition v)
				{
					targets.SetSameValue((SavedUnit x) => ref GetWaypoints(x)[i2].position, v);
				});
				MultiField<string> objectiveField = new MultiField<string>(() => waypoint.objective, delegate(string v)
				{
					targets.SetSameValue((SavedUnit x) => ref GetWaypoints(x)[i2].objective, v);
				});
				entries[i2].SetEntry(this, objectives, i2, positionField, objectiveField);
			}
		}

		public void AddWaypoint()
		{
			foreach (SavedUnit target in targets.Targets)
			{
				VehicleWaypoint vehicleWaypoint = new VehicleWaypoint();
				vehicleWaypoint.position = SceneSingleton<CameraStateManager>.i.transform.position.ToGlobalPosition();
				vehicleWaypoint.objective = "Unit Spawn";
				GetWaypoints(target).Add(vehicleWaypoint);
			}
			WaypointEntry item = UnityEngine.Object.Instantiate(waypointEntryPrefab, waypointsPanel);
			entries.Add(item);
			FixLayout.RebuildRoot(base.gameObject);
			SetupAllWithIndex();
		}

		public void RemoveWayPoint(WaypointEntry entry)
		{
			foreach (SavedUnit target in targets.Targets)
			{
				GetWaypoints(target).RemoveAt(entry.Index);
			}
			entries.RemoveAt(entry.Index);
			UnityEngine.Object.Destroy(entry.gameObject);
			FixLayout.RebuildRootEndOfFrame(base.gameObject);
			SetupAllWithIndex();
		}
	}
}
