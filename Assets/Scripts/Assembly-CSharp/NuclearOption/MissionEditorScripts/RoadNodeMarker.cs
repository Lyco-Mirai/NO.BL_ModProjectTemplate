using NuclearOption.SavedMission;
using RoadPathfinding;
using UnityEngine;

namespace NuclearOption.MissionEditorScripts
{
	public class RoadNodeMarker : MonoBehaviour, IEditorSelectable
	{
		private sealed class RoadPointSelectionDetails : SingleSelectionDetails
		{
			private readonly RoadNodeMarker marker;

			private readonly Road road;

			private readonly int pointIndex;

			public override string DisplayName => "Road Point";

			public override bool IsDestroyed
			{
				get
				{
					if (!(marker == null) && marker.Road == road)
					{
						return marker.PointIndex != pointIndex;
					}
					return true;
				}
			}

			public RoadPointSelectionDetails(RoadNodeMarker marker, ValueWrapperGlobalPosition position)
				: base(marker, position, null)
			{
				this.marker = marker;
				road = marker.Road;
				pointIndex = marker.PointIndex;
			}

			public override void Focus()
			{
				SceneSingleton<CameraStateManager>.i.FocusPosition(base.PositionWrapper.Value.ToLocalPosition(), null, 50f);
			}

			public override bool Delete()
			{
				return false;
			}
		}

		public Node node;

		private ValueWrapperGlobalPosition position;

		public RoadEditor Editor { get; private set; }

		public Road Road { get; private set; }

		public int PointIndex { get; private set; } = -1;

		public bool IsRoadPoint
		{
			get
			{
				if (Road != null)
				{
					return PointIndex >= 0;
				}
				return false;
			}
		}

		public void SetupNode(RoadEditor editor, Node node)
		{
			Editor = editor;
			this.node = node;
			ClearPoint();
		}

		public void SetupPoint(RoadEditor editor, Road road, int pointIndex, Node node = null)
		{
			Editor = editor;
			this.node = node;
			Road = road;
			PointIndex = pointIndex;
			position = new ValueWrapperGlobalPosition(road.points[pointIndex]);
			position.RegisterOnChange(this, delegate(GlobalPosition value)
			{
				Editor.MoveRoadPoint(this, value);
			});
		}

		public void ClearPoint()
		{
			Road = null;
			PointIndex = -1;
			position = null;
		}

		public SingleSelectionDetails CreateSelectionDetails()
		{
			return new RoadPointSelectionDetails(this, position);
		}
	}
}
