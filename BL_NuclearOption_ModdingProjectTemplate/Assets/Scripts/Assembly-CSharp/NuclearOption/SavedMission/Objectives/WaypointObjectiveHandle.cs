using NuclearOption.MissionEditorScripts;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NuclearOption.SavedMission.Objectives
{
	public class WaypointObjectiveHandle : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler, IPointerClickHandler, IEditorSelectable
	{
		private static readonly Vector3[] worldCorners = new Vector3[4];

		[SerializeField]
		private ObjectiveOverlay overlay;

		[SerializeField]
		private RectTransform selectRect;

		[SerializeField]
		private Color normalColor;

		[SerializeField]
		private Color hoverColor;

		[SerializeField]
		private Color selectColor;

		private Transform proxy;

		private Waypoint waypoint;

		private Objective objective;

		private bool _selected;

		public WaypointEditor Editor { get; private set; }

		public int Index { get; private set; }

		public bool Selected => _selected;

		public bool Hover { get; private set; }

		public Waypoint Waypoint => waypoint;

		public void SetSelected(bool selected)
		{
			_selected = selected;
			overlay.SetRaycastTarget(!selected);
		}

		public Transform GetProxy()
		{
			if (proxy == null)
			{
				proxy = new GameObject("WaypointProxy").transform;
			}
			proxy.position = waypoint.GlobalPosition.Value.ToLocalPosition();
			return proxy;
		}

		public Rect GetWorldRect()
		{
			selectRect.GetWorldCorners(worldCorners);
			return new Rect(worldCorners[0].x, worldCorners[0].y, worldCorners[2].x - worldCorners[0].x, worldCorners[2].y - worldCorners[0].y);
		}

		public void SetWaypoint(WaypointEditor editor, int index, Waypoint waypoint, Objective objective)
		{
			if (this.waypoint != waypoint && Selected)
			{
				SceneSingleton<UnitSelection>.i.ClearSelection(this);
			}
			Editor = editor;
			Index = index;
			this.waypoint = waypoint;
			this.objective = objective;
			base.gameObject.SetActive(value: true);
		}

		public void Hide()
		{
			Editor = null;
			Index = 0;
			waypoint = null;
			base.gameObject.SetActive(value: false);
			overlay.HideOverlay();
			if (Selected)
			{
				SceneSingleton<UnitSelection>.i.ClearSelection(this);
			}
		}

		public void DeleteWaypoint()
		{
			if (Editor != null)
			{
				Editor.DeleteWaypoint(Index);
			}
		}

		private void Update()
		{
			GlobalPosition globalPosition = SceneSingleton<CameraStateManager>.i.mainCamera.transform.position.ToGlobalPosition();
			MissionPosition.PositionResult result = MissionPosition.ResultForPosition(waypoint.ToObjectivePosition(), globalPosition);
			overlay.UpdateOverlay(result);
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			Hover = true;
			if (!Selected)
			{
				overlay.SetColor(hoverColor);
				SceneSingleton<UnitSelection>.i.SetHover(this);
			}
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			Hover = false;
			if (!Selected)
			{
				overlay.SetColor(normalColor);
				SceneSingleton<UnitSelection>.i.ClearHover(this);
			}
		}

		public void OnPointerClick(PointerEventData eventData)
		{
			if (!Selected)
			{
				overlay.SetColor(selectColor);
				SceneSingleton<UnitSelection>.i.SetSelection(this);
				SetSelected(selected: true);
				SceneSingleton<UnitSelection>.i.OnSelect += SelectionChanged;
			}
		}

		private void SelectionChanged(SelectionDetails details)
		{
			if (!(details is WaypointSelectionDetails waypointSelectionDetails) || !(waypointSelectionDetails.Handle == this))
			{
				SetSelected(selected: false);
				overlay.SetColor(normalColor);
				SceneSingleton<UnitSelection>.i.OnSelect -= SelectionChanged;
			}
		}

		private void OnDestroy()
		{
			if (Selected)
			{
				SceneSingleton<UnitSelection>.i.ClearSelection(this);
			}
			if (proxy != null)
			{
				Object.Destroy(proxy.gameObject);
			}
		}

		public string GetDisplayName()
		{
			string displayName = objective.SavedObjective.DisplayName;
			ValueWrapperGlobalPosition globalPosition = Waypoint.GlobalPosition;
			return $"{displayName} - {globalPosition}";
		}

		SingleSelectionDetails IEditorSelectable.CreateSelectionDetails()
		{
			return new WaypointSelectionDetails(this);
		}
	}
}
