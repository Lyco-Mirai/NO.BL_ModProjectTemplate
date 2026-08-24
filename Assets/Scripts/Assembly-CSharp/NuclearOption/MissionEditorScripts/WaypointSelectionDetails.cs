using NuclearOption.SavedMission.Objectives;
using UnityEngine;

namespace NuclearOption.MissionEditorScripts
{
	public class WaypointSelectionDetails : SingleSelectionDetails
	{
		public readonly WaypointObjectiveHandle Handle;

		public override string DisplayName => Handle.GetDisplayName();

		public override bool IsDestroyed => Handle == null;

		public override bool AutoUnhover => false;

		public WaypointSelectionDetails(WaypointObjectiveHandle handle)
			: base(handle, handle.Waypoint.GlobalPosition, null)
		{
			Handle = handle;
		}

		public override void Focus()
		{
			Vector3 position = base.PositionWrapper.Value.ToLocalPosition();
			float distance = (float)Handle.Waypoint.Range * 1.3f;
			SceneSingleton<CameraStateManager>.i.FocusPosition(position, null, distance);
		}

		public override bool Delete()
		{
			Handle.DeleteWaypoint();
			return true;
		}

		public override string ToString()
		{
			return $"Waypoint({base.PositionWrapper.Value})";
		}
	}
}
