using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NuclearOption.NodeGraph;
using NuclearOption.SavedMission;
using TMPro;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts
{
	[DefaultExecutionOrder(-10)]
	public class UnitSelection : SceneSingleton<UnitSelection>
	{
		public delegate void SelectionChanged<T>(T details) where T : SelectionDetails;

		private enum DragMode
		{
			None = 0,
			Pending = 1,
			BoxSelect = 2,
			Direct = 3
		}

		private static readonly ProfilerMarker selectUnitsInBoxMarker = new ProfilerMarker("SelectUnitsInBox");

		public static RaycastHit[] hitCache = new RaycastHit[100];

		[SerializeField]
		private LayerMask selectLayer = ~(1 << PhysicsLayers.ExclusionZones);

		[SerializeField]
		private LayerMask placeLayer = ~(1 << PhysicsLayers.ExclusionZones);

		[SerializeField]
		public Transform placementTransform;

		[SerializeField]
		private Transform cursor;

		[SerializeField]
		private bool setPositionTextWidth;

		[SerializeField]
		private TextMeshProUGUI positionText;

		[SerializeField]
		public EditorHandle handle;

		private List<object> disallowSelectionKeys = new List<object>();

		private IPlacingMenu placingMenu;

		private SelectionDetails _selectionDetails;

		private readonly List<SavedUnit> savedUnitCache = new List<SavedUnit>();

		private readonly HashSet<Unit> selectionCache = new HashSet<Unit>();

		private readonly List<SavedUnit> copiedUnits = new List<SavedUnit>();

		private DragMode dragMode;

		private Vector2 dragStartScreen;

		private IEditorSelectable dragSelectable;

		private Collider pointerDownCollider;

		private Vector2 boxStartLocal;

		private Vector2 boxCurrentLocal;

		[SerializeField]
		private RectTransform selectionBox;

		[SerializeField]
		private Color selectionBoxColor;

		[SerializeField]
		private Color selectionBoxOutlineColor;

		[SerializeField]
		private Color additiveSelectionBoxColor;

		[SerializeField]
		private Color additiveSelectionBoxOutlineColor;

		[SerializeField]
		private Image selectionBoxImage;

		[SerializeField]
		private Image selectionBoxOutlineImage;

		private float placementYaw;

		public Func<IEditorSelectable, bool> SelectionFilter { get; set; }

		private bool placeMode => placingMenu != null;

		public SelectionDetails SelectionDetails
		{
			get
			{
				return _selectionDetails;
			}
			private set
			{
				if (_selectionDetails != value)
				{
					if (_selectionDetails is IDisposable disposable)
					{
						disposable.Dispose();
					}
					_selectionDetails = value;
				}
			}
		}

		public SingleSelectionDetails HoverDetails { get; private set; }

		private bool SelectDisallowed => disallowSelectionKeys.Count > 0;

		public event SelectionChanged<SelectionDetails> OnSelect;

		public event SelectionChanged<SingleSelectionDetails> OnHover;

		public SingleSelectionDetails GetSingleOrFirstMultiSelect()
		{
			SelectionDetails selectionDetails = SelectionDetails;
			if (!(selectionDetails is SingleSelectionDetails result))
			{
				if (selectionDetails is MultiSelectSelectionDetails multiSelectSelectionDetails)
				{
					return multiSelectSelectionDetails.Items[0];
				}
				return null;
			}
			return result;
		}

		protected override void Awake()
		{
			base.Awake();
			MissionManager.onMissionLoad += MissionManager_onMissionLoad;
		}

		private void OnDestroy()
		{
			MissionManager.onMissionLoad -= MissionManager_onMissionLoad;
			if (SelectionDetails is IDisposable disposable)
			{
				disposable.Dispose();
			}
		}

		private void MissionManager_onMissionLoad(Mission obj)
		{
			ClearSelection();
			ClearHover();
		}

		public void RefreshSelected()
		{
			RefreshSelectionInternal();
		}

		public void SetSelection(IEditorSelectable value)
		{
			SetSelectionInternal(value);
		}

		public void ClearIfSelected(IEditorSelectable value)
		{
			if (SelectionDetails == null)
			{
				return;
			}
			if (SelectionDetails is SingleSelectionDetails singleSelectionDetails)
			{
				if (singleSelectionDetails?.Source == value)
				{
					ClearSelection();
				}
			}
			else if (SelectionDetails is MultiSelectSelectionDetails multiSelectSelectionDetails)
			{
				multiSelectSelectionDetails.ClearIfSelected(value);
			}
		}

		public void ClearSelection(IEditorSelectable hint)
		{
			if (!(SelectionDetails is SingleSelectionDetails singleSelectionDetails))
			{
				UnityEngine.Debug.LogError("Clear with Hint only supported with SingleSelectionDetails");
			}
			else if (singleSelectionDetails?.Source == hint)
			{
				ClearSelectionInternal();
			}
			else
			{
				UnityEngine.Debug.LogError("ClearSelection called but hint was not selected");
			}
		}

		public void ClearSelection()
		{
			ClearSelectionInternal();
		}

		public void ReplaceSelection<T>(List<T> objects) where T : IEditorSelectable
		{
			List<SingleSelectionDetails> safeList;
			if (objects.Count == 0)
			{
				ClearSelection();
			}
			else if (objects.Count == 1)
			{
				SetSelection(objects[0]);
			}
			else if (CreateSafeDetailsList(objects, out safeList))
			{
				ReplaceSelection(safeList);
			}
		}

		private bool CreateSafeDetailsList<T>(List<T> source, out List<SingleSelectionDetails> safeList) where T : IEditorSelectable
		{
			safeList = new List<SingleSelectionDetails>();
			Type type = null;
			foreach (T item in source)
			{
				SingleSelectionDetails singleSelectionDetails = item.CreateSelectionDetails();
				if (type == null)
				{
					type = singleSelectionDetails.GetType();
				}
				else if (singleSelectionDetails.GetType() != type)
				{
					UnityEngine.Debug.LogError("ReplaceSelection failed because list of object did not create the same SelectionDetails type");
					safeList.Clear();
					return false;
				}
				safeList.Add(singleSelectionDetails);
			}
			return true;
		}

		public void ReplaceSelection(List<SingleSelectionDetails> objects)
		{
			MultiSelectSelectionDetails multiSelectSelectionDetails = new MultiSelectSelectionDetails();
			foreach (SingleSelectionDetails @object in objects)
			{
				multiSelectSelectionDetails.Add(@object);
			}
			SetSelectionInternal(multiSelectSelectionDetails);
		}

		public void ToggleInMultiSelection(IEditorSelectable selectable)
		{
			if (SelectionDetails == null)
			{
				SetSelectionInternal(selectable);
			}
			else if (SelectionDetails is SingleSelectionDetails singleSelectionDetails)
			{
				if (singleSelectionDetails.Source == selectable)
				{
					ClearSelectionInternal();
				}
				else
				{
					AddToMultiSelection(selectable);
				}
			}
			else if (SelectionDetails is MultiSelectSelectionDetails multiSelectSelectionDetails)
			{
				if (multiSelectSelectionDetails.Items.Any((SingleSelectionDetails x) => x.Source == selectable))
				{
					multiSelectSelectionDetails.Remove(selectable);
					RefreshSelected();
				}
				else
				{
					AddToMultiSelection(selectable);
				}
			}
			else
			{
				UnityEngine.Debug.LogError($"Case for {SelectionDetails?.GetType()} not found");
			}
		}

		public void AddToMultiSelection(IEditorSelectable selectable)
		{
			if (SelectionDetails == null)
			{
				SetSelectionInternal(selectable);
				return;
			}
			SingleSelectionDetails singleSelectionDetails = selectable.CreateSelectionDetails();
			SelectionDetails selectionDetails = SelectionDetails;
			if (!(selectionDetails is SingleSelectionDetails singleSelectionDetails2))
			{
				if (selectionDetails is MultiSelectSelectionDetails multiSelectSelectionDetails)
				{
					if (singleSelectionDetails.GetType() == multiSelectSelectionDetails.SelectionType)
					{
						multiSelectSelectionDetails.Add(singleSelectionDetails);
						RefreshSelectionInternal();
					}
					else
					{
						UnityEngine.Debug.LogError("Can't multiselect " + singleSelectionDetails.GetType().Name + " with " + multiSelectSelectionDetails.SelectionType.Name);
					}
				}
				else
				{
					UnityEngine.Debug.LogError($"Case for {SelectionDetails?.GetType()} not found");
				}
			}
			else if (singleSelectionDetails.GetType() == singleSelectionDetails2.GetType())
			{
				MultiSelectSelectionDetails multiSelectSelectionDetails2 = new MultiSelectSelectionDetails();
				multiSelectSelectionDetails2.Add(singleSelectionDetails2);
				multiSelectSelectionDetails2.Add(singleSelectionDetails);
				SetSelectionInternal(multiSelectSelectionDetails2);
			}
			else
			{
				UnityEngine.Debug.LogError("Can't multiselect " + singleSelectionDetails.GetType().Name + " with " + singleSelectionDetails2.GetType().Name);
			}
		}

		public void AddToMultiSelection<T>(List<T> objects) where T : IEditorSelectable
		{
			if (SelectionDetails == null)
			{
				ReplaceSelection(objects);
				return;
			}
			objects = objects.Where((T x) => !IsSelected(x)).ToList();
			if (objects.Count == 0 || !CreateSafeDetailsList(objects, out var safeList))
			{
				return;
			}
			SelectionDetails selectionDetails = SelectionDetails;
			if (!(selectionDetails is SingleSelectionDetails singleSelectionDetails))
			{
				if (selectionDetails is MultiSelectSelectionDetails multiSelectSelectionDetails)
				{
					if (safeList[0].GetType() == multiSelectSelectionDetails.SelectionType)
					{
						foreach (SingleSelectionDetails item in safeList)
						{
							multiSelectSelectionDetails.Add(item);
						}
						RefreshSelectionInternal();
					}
					else
					{
						UnityEngine.Debug.LogError("Can't multiselect " + safeList[0].GetType().Name + " with " + multiSelectSelectionDetails.SelectionType.Name);
					}
				}
				else
				{
					UnityEngine.Debug.LogError($"Case for {SelectionDetails?.GetType()} not found");
				}
			}
			else if (safeList[0].GetType() == singleSelectionDetails.GetType())
			{
				MultiSelectSelectionDetails multiSelectSelectionDetails2 = new MultiSelectSelectionDetails();
				multiSelectSelectionDetails2.Add(singleSelectionDetails);
				foreach (SingleSelectionDetails item2 in safeList)
				{
					multiSelectSelectionDetails2.Add(item2);
				}
				SetSelectionInternal(multiSelectSelectionDetails2);
			}
			else
			{
				UnityEngine.Debug.LogError("Can't multiselect " + safeList[0].GetType().Name + " with " + singleSelectionDetails.GetType().Name);
			}
		}

		public void RemoveFromMultiSelection(IEditorSelectable obj)
		{
			if (SelectionDetails is MultiSelectSelectionDetails multiSelectSelectionDetails)
			{
				multiSelectSelectionDetails.Remove(obj);
				if (SelectionDetails == multiSelectSelectionDetails)
				{
					RefreshSelected();
				}
			}
			else
			{
				UnityEngine.Debug.LogError("Can't use RemoveFromMultiSelection when not using MultiSelectSelectionDetails");
			}
		}

		public void RemoveFromMultiSelection<T>(List<T> objects, bool errorIfNotSeleted) where T : IEditorSelectable
		{
			if (SelectionDetails is MultiSelectSelectionDetails multiSelectSelectionDetails)
			{
				multiSelectSelectionDetails.RemoveAll(objects, errorIfNotSeleted);
				if (SelectionDetails == multiSelectSelectionDetails)
				{
					RefreshSelected();
				}
			}
			else
			{
				UnityEngine.Debug.LogError("Can't use RemoveFromMultiSelection when not using MultiSelectSelectionDetails");
			}
		}

		public void ReplaceMultiSelection(MultiSelectSelectionDetails hint, SingleSelectionDetails first)
		{
			if (SelectionDetails == hint)
			{
				SetSelectionInternal(first);
			}
			else
			{
				UnityEngine.Debug.LogError("ReplaceMultiSelection called but hint was not selected");
			}
		}

		public void ClearMultiSelection(MultiSelectSelectionDetails hint)
		{
			if (SelectionDetails == hint)
			{
				ClearSelectionInternal();
			}
			else
			{
				UnityEngine.Debug.LogError("ClearMultiSelection called but hint was not selected");
			}
		}

		private void RefreshSelectionInternal()
		{
			if (SelectionDetails != null && SelectionDetails.TryGetFaction(out var faction))
			{
				SceneSingleton<MissionEditor>.i.stickyFaction = ((faction != null) ? faction.factionName : "");
			}
			this.OnSelect?.Invoke(SelectionDetails);
		}

		private void SetSelectionInternal(IEditorSelectable selectable)
		{
			SingleSelectionDetails selectionInternal = selectable.CreateSelectionDetails();
			SetSelectionInternal(selectionInternal);
		}

		private void SetSelectionInternal(SelectionDetails details)
		{
			SelectionDetails = details;
			if (details.TryGetFaction(out var faction))
			{
				SceneSingleton<MissionEditor>.i.stickyFaction = ((faction != null) ? faction.factionName : "");
			}
			this.OnSelect?.Invoke(details);
		}

		private void ClearSelectionInternal()
		{
			SelectionDetails = null;
			this.OnSelect?.Invoke(null);
		}

		public void SetHover(IEditorSelectable value)
		{
			SetHoverInternal(value);
		}

		public void ClearHover(IEditorSelectable hint)
		{
			if (HoverDetails?.Source == hint)
			{
				SetHoverInternal(null);
			}
		}

		private void ClearHover()
		{
			SetHoverInternal(null);
		}

		private void SetHoverInternal(IEditorSelectable selectable)
		{
			if (HoverDetails?.Source != selectable)
			{
				SingleSelectionDetails details = (HoverDetails = selectable?.CreateSelectionDetails());
				this.OnHover?.Invoke(details);
			}
		}

		private void Update()
		{
			if (!DynamicMap.mapMaximized && !(SceneSingleton<CameraStateManager>.i.mainCamera == null) && !(GraphEditor.i != null) && !UpdateUnitCopyPaste() && !UpdateDrag())
			{
				CheckRaycast();
				if (placeMode && MissionEditorInput.IsEscapeDown)
				{
					placingMenu.CancelPlace();
				}
				Physics.SyncTransforms();
			}
		}

		private bool UpdateUnitCopyPaste()
		{
			if (placeMode || SelectDisallowed || SelectionFilter != null || InputFieldChecker.InsideInputField)
			{
				return false;
			}
			if (!MissionEditorInput.Isctrl)
			{
				return false;
			}
			if (MissionEditorInput.IsCDown)
			{
				copiedUnits.Clear();
				copiedUnits.AddRange(GetSelectedSavedUnits());
				return true;
			}
			if (MissionEditorInput.IsVDown)
			{
				if (copiedUnits.Count == 0)
				{
					return true;
				}
				Vector3 offset = placementTransform.GlobalPosition().AsVector3() - copiedUnits[0].Unit.transform.GlobalPosition().AsVector3();
				List<Unit> list = SceneSingleton<MissionEditor>.i.DuplicateUnits(copiedUnits, offset);
				if (list.Count > 0)
				{
					ReplaceSelection(list);
				}
				return true;
			}
			return false;
		}

		public IEnumerable<SavedUnit> GetSelectedSavedUnits()
		{
			if (SelectionDetails is UnitSelectionDetails unitSelectionDetails)
			{
				if (unitSelectionDetails.SavedUnit?.Unit != null)
				{
					yield return unitSelectionDetails.SavedUnit;
				}
			}
			else
			{
				if (!(SelectionDetails is MultiSelectSelectionDetails multiSelectSelectionDetails))
				{
					yield break;
				}
				foreach (SingleSelectionDetails item in multiSelectSelectionDetails.Items)
				{
					if (item is UnitSelectionDetails unitSelectionDetails2 && unitSelectionDetails2.SavedUnit?.Unit != null)
					{
						yield return unitSelectionDetails2.SavedUnit;
					}
				}
			}
		}

		private bool UpdateDrag()
		{
			if (dragMode == DragMode.None)
			{
				return false;
			}
			if (MissionEditorInput.IsEscapeDown)
			{
				ClearDrag();
				return true;
			}
			if (!MissionEditorInput.IsMouse0)
			{
				EndDrag();
				return true;
			}
			switch (dragMode)
			{
			case DragMode.Pending:
				UpdatePendingDrag();
				return true;
			case DragMode.BoxSelect:
				UpdateBoxSelect(MissionEditorInput.MousePosition);
				return true;
			case DragMode.Direct:
				UpdateDirectDrag();
				return true;
			default:
				ClearDrag();
				return true;
			}
		}

		private void EndDrag()
		{
			switch (dragMode)
			{
			case DragMode.Pending:
				CommitClick();
				break;
			case DragMode.BoxSelect:
				EndBoxSelect();
				break;
			case DragMode.Direct:
				handle.EndDirectManipulation();
				break;
			}
			ClearDrag();
		}

		private void ClearDrag()
		{
			if (dragMode == DragMode.Direct)
			{
				handle.EndDirectManipulation();
			}
			dragMode = DragMode.None;
			dragSelectable = null;
			pointerDownCollider = null;
			if (selectionBox != null)
			{
				selectionBox.gameObject.SetActive(value: false);
			}
		}

		private void BeginPendingDrag(IEditorSelectable selectable, Collider hitCollider = null)
		{
			dragMode = DragMode.Pending;
			dragStartScreen = MissionEditorInput.MousePosition;
			dragSelectable = selectable;
			pointerDownCollider = hitCollider;
			boxStartLocal = (boxCurrentLocal = ScreenToLocal(MissionEditorInput.MousePosition));
		}

		private void UpdatePendingDrag()
		{
			if (!DragThresholdPassed(dragStartScreen))
			{
				return;
			}
			if (dragSelectable != null)
			{
				if (SelectionFilter != null && !MissionEditorInput.IsShift && !IsSelected(dragSelectable))
				{
					SetSelection(dragSelectable);
				}
				if (IsSelected(dragSelectable) && TryBeginDirectDrag())
				{
					dragMode = DragMode.Direct;
				}
				else if (SelectionFilter == null)
				{
					BeginBoxSelect();
				}
				else
				{
					ClearDrag();
				}
			}
			else if (SelectionFilter == null)
			{
				BeginBoxSelect();
			}
			else
			{
				ClearDrag();
			}
		}

		private bool TryBeginDirectDrag()
		{
			if (dragSelectable == null || !IsSelected(dragSelectable))
			{
				return false;
			}
			bool isShift = MissionEditorInput.IsShift;
			bool isAlt = MissionEditorInput.IsAlt;
			return handle.BeginDirectManipulation(isShift, isAlt, MissionEditorInput.MousePosition);
		}

		private void UpdateDirectDrag()
		{
			bool isShift = MissionEditorInput.IsShift;
			bool isAlt = MissionEditorInput.IsAlt;
			if (!handle.UpdateDirectManipulation(isShift, isAlt, MissionEditorInput.MousePosition))
			{
				handle.EndDirectManipulation();
				ClearDrag();
			}
			else
			{
				Vector3 position = handle.proxyTransform.position;
				position.y = Mathf.Max(position.y, Datum.LocalSeaY);
				SetCursorVisual(position, handle.proxyTransform.position);
			}
		}

		private void CommitClick()
		{
			if (dragSelectable != null)
			{
				if (MissionEditorInput.Isctrl && dragSelectable is RoadView roadView)
				{
					roadView.InsertPoint(pointerDownCollider);
				}
				else if (MissionEditorInput.IsShift)
				{
					ToggleInMultiSelection(dragSelectable);
				}
				else if (IsSelected(dragSelectable))
				{
					if (SelectionDetails is MultiSelectSelectionDetails multiSelectSelectionDetails)
					{
						multiSelectSelectionDetails.SetPivot(dragSelectable);
						RefreshSelected();
					}
				}
				else
				{
					SetSelection(dragSelectable);
				}
			}
			else if (!MissionEditorInput.IsShift)
			{
				TryDeselect();
			}
		}

		private void BeginBoxSelect()
		{
			dragMode = DragMode.BoxSelect;
			dragSelectable = null;
			MissionManager.GetAllSavedUnitsNonAlloc(savedUnitCache, includeBuiltIn: true);
			selectionCache.Clear();
			cursor.gameObject.SetActive(value: false);
			selectionBox.gameObject.SetActive(value: true);
			UpdateBoxSelect(MissionEditorInput.MousePosition);
		}

		private void UpdateBoxSelect(Vector2 mousePosition)
		{
			boxCurrentLocal = ScreenToLocal(mousePosition);
			UpdateBoxVisual();
		}

		private void EndBoxSelect()
		{
			Stopwatch stopwatch = Stopwatch.StartNew();
			SelectUnitsInBox(out var count);
			stopwatch.Stop();
			int num = Mathf.Max(count, 1);
			UnityEngine.Debug.Log($"[UnitSelection.SelectUnitsInBox] Units:{count} |Time:{stopwatch.Elapsed.TotalMilliseconds:F2} ms |Norm:{stopwatch.Elapsed.TotalMilliseconds / (double)num:F2} ms");
			selectionBox.gameObject.SetActive(value: false);
		}

		private void UpdateBoxVisual()
		{
			Vector2 vector = Vector2.Min(boxStartLocal, boxCurrentLocal);
			Vector2 vector2 = Vector2.Max(boxStartLocal, boxCurrentLocal);
			selectionBox.anchoredPosition = vector;
			selectionBox.sizeDelta = vector2 - vector;
			bool isShift = MissionEditorInput.IsShift;
			selectionBoxImage.color = (isShift ? additiveSelectionBoxColor : selectionBoxColor);
			selectionBoxOutlineImage.color = (isShift ? additiveSelectionBoxOutlineColor : selectionBoxOutlineColor);
		}

		private void SelectUnitsInBox(out int count)
		{
			using (selectUnitsInBoxMarker.Auto())
			{
				Vector2 vector = Vector2.Min(boxStartLocal, boxCurrentLocal);
				Vector2 vector2 = Vector2.Max(boxStartLocal, boxCurrentLocal);
				Rect rect = Rect.MinMaxRect(vector.x, vector.y, vector2.x, vector2.y);
				foreach (SavedUnit item in savedUnitCache)
				{
					if (item == null || (UnitBrowser.I != null && !UnitBrowser.I.PassesFilters(item)))
					{
						continue;
					}
					Vector3 vector3 = SceneSingleton<CameraStateManager>.i.mainCamera.WorldToScreenPoint(item.globalPosition.ToLocalPosition());
					if (!(vector3.z <= 0f) && !(vector3.z > 10000f) && rect.Contains(ScreenToLocal(vector3)))
					{
						RaycastHit hit;
						IEditorSelectable selectable;
						if (SceneSingleton<MissionEditor>.i.allowCameraClip)
						{
							selectionCache.Add(item.Unit);
						}
						else if (DoRayCast(placeMode: false, vector3, out hit) && TryGetSelectable(hit, out selectable) && selectable as Unit == item.Unit)
						{
							selectionCache.Add(item.Unit);
						}
					}
				}
				count = selectionCache.Count;
				if (MissionEditorInput.IsShift)
				{
					if (selectionCache.Count > 0)
					{
						AddToMultiSelection(selectionCache.ToList());
					}
				}
				else
				{
					ReplaceSelection(selectionCache.ToList());
				}
			}
		}

		private void CheckRaycast()
		{
			bool flag = EventSystem.current.IsPointerOverGameObject();
			bool flag2 = MissionEditorInput.IsMouse0Down && !flag;
			if (handle.MouseHoverOrInteract)
			{
				cursor.gameObject.SetActive(value: false);
				return;
			}
			bool flag3 = false;
			RaycastHit hit = default(RaycastHit);
			if (!flag)
			{
				flag3 = DoRayCast(placeMode, MissionEditorInput.MousePosition, out hit);
			}
			cursor.gameObject.SetActive(flag3);
			if (!flag3)
			{
				if (!placeMode)
				{
					if (flag2)
					{
						BeginPendingDrag(null);
					}
					else
					{
						ClearHover();
					}
				}
				return;
			}
			MoveCursorTransforms(hit);
			if (placeMode)
			{
				if (!flag2)
				{
					return;
				}
				var (flag4, editorSelectable) = placingMenu.Place(MissionEditorInput.IsShift);
				if (!flag4)
				{
					placingMenu = null;
					if (editorSelectable != null)
					{
						SetSelection(editorSelectable);
					}
				}
				return;
			}
			IEditorSelectable selectable;
			bool flag5 = TryGetSelectable(hit, out selectable);
			if (flag2)
			{
				BeginPendingDrag(flag5 ? selectable : null, flag5 ? hit.collider : null);
				return;
			}
			if (flag5)
			{
				SetHover(selectable);
				return;
			}
			SingleSelectionDetails hoverDetails = HoverDetails;
			if (hoverDetails != null && hoverDetails.AutoUnhover)
			{
				ClearHover();
			}
		}

		private bool DoRayCast(bool placeMode, Vector3 rayPos, out RaycastHit hit)
		{
			Ray ray = SceneSingleton<CameraStateManager>.i.mainCamera.ScreenPointToRay(rayPos);
			LayerMask layerMask = (placeMode ? placeLayer : selectLayer);
			bool flag = Physics.Raycast(ray, out hit, 100000f, layerMask);
			if (Datum.WaterPlane().Raycast(ray, out var enter) && enter < 100000f)
			{
				if (!flag || enter < hit.distance)
				{
					hit.point = SceneSingleton<CameraStateManager>.i.mainCamera.transform.position + ray.direction * enter;
					hit.normal = Vector3.up;
				}
				flag = true;
			}
			return flag;
		}

		private RaycastHit MoveCursorTransforms(RaycastHit hit)
		{
			Vector3 point = hit.point;
			point.y = Mathf.Max(point.y, Datum.LocalSeaY);
			Vector3 vector = handle.SnapPlacementPosition(point, !SceneSingleton<MissionEditor>.i.allowCameraClip);
			float y = MissionEditorInput.MouseScrollDelta.y;
			if (y != 0f)
			{
				placementYaw += y * Mathf.Max(handle.scrollStep, handle.AngleSnapStep);
			}
			Vector3 axis = Vector3.up;
			Quaternion quaternion = Quaternion.identity;
			if (SceneSingleton<MissionEditor>.i.allowTerrainFollowing)
			{
				axis = hit.normal;
				quaternion = Quaternion.LookRotation(Vector3.Cross(hit.normal, SceneSingleton<CameraStateManager>.i.transform.right), hit.normal);
			}
			quaternion = Quaternion.AngleAxis(handle.SnapAngle(placementYaw), axis) * quaternion;
			placementTransform.SetPositionAndRotation(vector, quaternion);
			SetCursorVisual(point, vector);
			placingMenu?.MoveCursor(placementTransform);
			return hit;
		}

		private void SetCursorVisual(Vector3 cursorTarget, Vector3 positionTextTarget)
		{
			cursor.gameObject.SetActive(value: true);
			float num = Vector3.Distance(SceneSingleton<CameraStateManager>.i.transform.position, cursorTarget);
			cursor.transform.position = cursorTarget + placementTransform.up * 0.2f * num * 0.025f;
			cursor.transform.localScale = new Vector3(num * 0.025f, 1f, num * 0.025f);
			positionText.text = positionTextTarget.ToGlobalPosition().AsVector3().ToString("0.0");
			if (setPositionTextWidth)
			{
				positionText.SetRectWidth(positionText.preferredWidth);
			}
		}

		private bool TryGetSelectable(RaycastHit hit, out IEditorSelectable selectable)
		{
			if (SelectDisallowed)
			{
				selectable = null;
				return false;
			}
			Collider collider = hit.collider;
			if (collider == null)
			{
				selectable = null;
				return false;
			}
			selectable = collider.GetComponentInParent<IEditorSelectable>();
			if (selectable == null && collider.TryGetComponent<IDamageable>(out var component))
			{
				selectable = component.GetUnit();
			}
			if (selectable is Unit unit && !UnitBrowser.I.PassesFilters(unit.SavedUnit))
			{
				selectable = null;
				return false;
			}
			if (selectable != null && SelectionFilter != null && !SelectionFilter(selectable))
			{
				selectable = null;
				return false;
			}
			return selectable != null;
		}

		private void TryDeselect()
		{
			if (SelectionDetails == null)
			{
				return;
			}
			if (handle.MouseHoverOrInteract)
			{
				UnityEngine.Debug.LogWarning("Mouse over handle, no de-select");
			}
			else if (SelectionDetails is SingleSelectionDetails details)
			{
				if (TryDeselect(details))
				{
					ClearSelection();
				}
			}
			else if (SelectionDetails is MultiSelectSelectionDetails multiSelectSelectionDetails && multiSelectSelectionDetails.Items.All(TryDeselect))
			{
				ClearSelection();
			}
		}

		private static bool TryDeselect(SingleSelectionDetails details)
		{
			if (details is UnitSelectionDetails unitSelectionDetails)
			{
				return CheckDistanceClear(unitSelectionDetails.Unit.transform.position);
			}
			if (details is WaypointSelectionDetails waypointSelectionDetails)
			{
				Rect worldRect = waypointSelectionDetails.Handle.GetWorldRect();
				if (!new Rect(worldRect.x - 150f, worldRect.y - 150f, worldRect.width + 300f, worldRect.height + 300f).Contains(MissionEditorInput.MousePosition))
				{
					return true;
				}
				return false;
			}
			if (details.PositionWrapper != null)
			{
				return CheckDistanceClear(details.PositionWrapper.Value.ToLocalPosition());
			}
			return true;
			static bool CheckDistanceClear(Vector3 position)
			{
				Vector3 b = SceneSingleton<CameraStateManager>.i.mainCamera.WorldToScreenPoint(position);
				b.z = 0f;
				return Vector3.Distance(MissionEditorInput.MousePosition, b) > 200f;
			}
		}

		public void StartPlaceUnit(IPlacingMenu placingMenu)
		{
			this.placingMenu = placingMenu;
			placingMenu.MoveCursor(placementTransform);
			ClearSelection();
		}

		public void StopPlacingUnit(IPlacingMenu placingMenu)
		{
			this.placingMenu = null;
		}

		public Vector3 GetPlacementUpAxis(UnitDefinition placingDefinition)
		{
			Vector3 up = Vector3.up;
			if (!(placingDefinition is BuildingDefinition))
			{
				up = placementTransform.up;
			}
			return up;
		}

		public static void DisallowSelection(object key, bool disallow)
		{
			if (disallow)
			{
				SceneSingleton<UnitSelection>.i.disallowSelectionKeys.Add(key);
			}
			else
			{
				SceneSingleton<UnitSelection>.i.disallowSelectionKeys.Remove(key);
			}
		}

		private Vector2 ScreenToLocal(Vector2 screenPos)
		{
			RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)base.transform, screenPos, null, out var localPoint);
			return localPoint;
		}

		private bool IsSelected(IEditorSelectable selectable)
		{
			if (selectable == null)
			{
				return false;
			}
			SelectionDetails selectionDetails = SelectionDetails;
			if (!(selectionDetails is SingleSelectionDetails singleSelectionDetails))
			{
				if (selectionDetails is MultiSelectSelectionDetails multiSelectSelectionDetails)
				{
					return multiSelectSelectionDetails.Items.Any((SingleSelectionDetails item) => item.Source == selectable);
				}
				return false;
			}
			return singleSelectionDetails.Source == selectable;
		}

		public static bool DragThresholdPassed(Vector2 startMousePosition)
		{
			int num = ((EventSystem.current != null) ? EventSystem.current.pixelDragThreshold : 5);
			return (MissionEditorInput.MousePosition - startMousePosition).sqrMagnitude >= (float)(num * num);
		}
	}
}
