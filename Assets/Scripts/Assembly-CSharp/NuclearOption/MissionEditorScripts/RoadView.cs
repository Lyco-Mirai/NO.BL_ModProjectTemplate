using RoadPathfinding;
using UnityEngine;

namespace NuclearOption.MissionEditorScripts
{
	public class RoadView : MonoBehaviour, IEditorSelectable
	{
		private sealed class RoadSelectionDetails : SingleSelectionDetails
		{
			private readonly RoadView view;

			public override string DisplayName => "Road";

			public override bool IsDestroyed => view == null;

			public RoadSelectionDetails(RoadView view)
				: base(view, null, null)
			{
				this.view = view;
			}

			public override void Focus()
			{
				Road road = view.Road;
				SceneSingleton<CameraStateManager>.i.FocusPosition(road.points[road.points.Count / 2].ToLocalPosition(), null, 50f);
			}

			public override bool Delete()
			{
				return false;
			}
		}

		public RoadEditor Editor { get; private set; }

		public Road Road { get; private set; }

		public void Setup(RoadEditor editor, Road road)
		{
			Editor = editor;
			Road = road;
		}

		public void InsertPoint(Collider segment)
		{
			Editor.InsertPoint(Road, segment.transform.GetSiblingIndex());
		}

		public SingleSelectionDetails CreateSelectionDetails()
		{
			return new RoadSelectionDetails(this);
		}
	}
}
