using System.Collections.Generic;
using NuclearOption.MissionEditorScripts;
using UnityEngine;

namespace NuclearOption.SavedMission.Objectives
{
	public class WaypointEditor : IObjectiveEditorUpdate
	{
		private readonly UIPrefabs prefabs;

		private readonly ReachWaypointsObjective objective;

		private readonly List<Waypoint> waypoints;

		private readonly List<WaypointObjectiveHandle> waypointHandles = new List<WaypointObjectiveHandle>();

		private GameObject parent;

		private int listHash;

		public WaypointEditor(Canvas canvas, UIPrefabs prefabs, List<Waypoint> waypoints, ReachWaypointsObjective objective)
		{
			this.prefabs = prefabs;
			this.objective = objective;
			this.waypoints = waypoints;
			parent = new GameObject("Waypoint_Editor", typeof(RectTransform));
			parent.transform.SetParent(canvas.transform);
			parent.transform.SetAsFirstSibling();
		}

		public void Destroy()
		{
			Object.Destroy(parent);
			waypointHandles.Clear();
			listHash = 0;
		}

		public void DeleteWaypoint(int index)
		{
			waypoints.RemoveAt(index);
			Update();
			if (objective.DataList != null)
			{
				objective.DataList.RefreshList();
			}
		}

		public void Update()
		{
			int num = HashList(waypoints);
			if (num == listHash)
			{
				return;
			}
			listHash = num;
			while (waypointHandles.Count < waypoints.Count)
			{
				WaypointObjectiveHandle item = Object.Instantiate(prefabs.WaypointEditor, parent.transform);
				waypointHandles.Add(item);
			}
			for (int i = 0; i < waypointHandles.Count; i++)
			{
				WaypointObjectiveHandle waypointObjectiveHandle = waypointHandles[i];
				if (i < waypoints.Count)
				{
					waypointObjectiveHandle.SetWaypoint(this, i, waypoints[i], objective);
				}
				else
				{
					waypointObjectiveHandle.Hide();
				}
			}
		}

		private static int HashList(List<Waypoint> waypoints)
		{
			int num = 7;
			foreach (Waypoint waypoint in waypoints)
			{
				num = num * 31 + waypoint.GetHashCode();
			}
			return num;
		}
	}
}
