using System.Collections.Generic;
using NuclearOption.MissionEditorScripts;
using NuclearOption.Networking;
using NuclearOption.NodeGraph;
using UnityEngine;

namespace NuclearOption.SavedMission.Objectives
{
	public class ReachWaypointsObjective : CompleteOrderObjectiveWithPositions<Waypoint>, IObjectiveWithPosition
	{
		private readonly ValueWrapperBool completeOnEnterRange = new ValueWrapperBool();

		public EmptyDataList DataList { get; private set; }

		public override bool NeedsFaction => true;

		public ReachWaypointsSavedObjective Saved => (ReachWaypointsSavedObjective)SavedObjective;

		public ReachWaypointsObjective(ReachWaypointsSavedObjective savedObjective)
			: base((SavedObjective)savedObjective)
		{
		}

		public override void Load(MissionLookups lookups)
		{
			base.Load(lookups);
			completeOrder = Saved.completeOrder;
			completeSomePercent.SetValue(Saved.completeSomePercent, this);
			completeOnEnterRange.SetValue(Saved.completeOnEnterRange, this);
			allItems = new List<Waypoint>();
			if (Saved.waypoints == null)
			{
				return;
			}
			foreach (SavedWaypoint waypoint in Saved.waypoints)
			{
				Waypoint item = new Waypoint(new GlobalPosition(waypoint.Position), waypoint.Range);
				allItems.Add(item);
			}
		}

		public override void Save()
		{
			base.Save();
			Saved.completeOrder = completeOrder;
			Saved.completeSomePercent = completeSomePercent.Value;
			Saved.completeOnEnterRange = completeOnEnterRange.Value;
			Saved.waypoints.Clear();
			foreach (Waypoint allItem in allItems)
			{
				Saved.waypoints.Add(new SavedWaypoint
				{
					Position = allItem.GlobalPosition.Value.AsVector3(),
					Range = allItem.Range.Value
				});
			}
		}

		protected override void DataReferenceDestroyed(ISaveableReference reference)
		{
		}

		protected override bool CheckComplete(Waypoint item)
		{
			List<Player> players = base.FactionHQ.GetPlayers(sortByScore: false);
			if (players.Count == 0)
			{
				return false;
			}
			foreach (Player item2 in players)
			{
				Aircraft aircraft = item2.Aircraft;
				if (!(aircraft == null) && FastMath.InRange(aircraft.GlobalPosition(), item.GlobalPosition, item.Range) && (completeOnEnterRange.Value || Vector3.Dot(aircraft.rb.velocity, item.GlobalPosition - aircraft.GlobalPosition()) < 0f))
				{
					return true;
				}
			}
			return false;
		}

		protected override bool TryGetPosition(Waypoint item, out ObjectivePosition position)
		{
			position = item.ToObjectivePosition();
			return true;
		}

		public override void DrawData(DataDrawer drawer)
		{
			if (allItems == null)
			{
				allItems = new List<Waypoint>();
			}
			CompleteOrderPercentWrapper.Create(drawer, completeOrder, delegate(CompleteOrder v)
			{
				completeOrder = v;
			}, completeSomePercent);
			drawer.InstantiateWithParent(drawer.Prefabs.BoolFieldPrefab).Setup("Complete on Enter", completeOnEnterRange);
			drawer.Space(10);
			DataList = drawer.DrawList(300, allItems, DrawWaypoint, CreateNewWaypoint);
		}

		public override void AddPins(GraphNodeData data)
		{
			base.AddPins(data);
			data.InputElements.Add(new GraphReadOnlyFieldData
			{
				PinId = new PinId("Waypoints Count"),
				DisplayName = "Waypoints Count",
				GetText = () => (allItems == null) ? "0" : allItems.Count.ToString()
			});
		}

		private static Waypoint CreateNewWaypoint()
		{
			RaycastHit hitInfo;
			Vector3 position = ((!Physics.Raycast(Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2)), out hitInfo, 10000f, PhysicsLayers.StaticsMask)) ? Vector3.zero : hitInfo.point);
			return new Waypoint(position.ToGlobalPosition());
		}

		private static void DrawWaypoint(int _, Waypoint waypoint, DataDrawer drawer)
		{
			drawer.InstantiateWithParent(drawer.Prefabs.FloatFieldPrefab).Setup("Range", waypoint.Range);
			drawer.InstantiateWithParent(drawer.Prefabs.VectorFieldPrefab).Setup("Position", waypoint.GlobalPosition);
		}

		public override IObjectiveEditorUpdate CreateEditorUpdate(Canvas canvas, UIPrefabs prefabs)
		{
			return new WaypointEditor(canvas, prefabs, allItems, this);
		}
	}
}
