using System.Threading;
using Cysharp.Threading.Tasks;
using NuclearOption.SavedMission;
using Rewired;
using RuntimeHandle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts
{
	[DefaultExecutionOrder(10)]
	public class EditorHandle : MonoBehaviour
	{
		private enum DirectMode
		{
			None = 0,
			MoveHorizontal = 1,
			MoveVertical = 2,
			LookAt = 3
		}

		[SerializeField]
		private UnitSelection unitSelection;

		[SerializeField]
		private GameObject handlePrefab;

		[Header("Buttons")]
		[SerializeField]
		private GameObject holder;

		[Space]
		[SerializeField]
		private Button selectMode;

		[SerializeField]
		private Button positionMode;

		[SerializeField]
		private Button rotationMode;

		[Space]
		[SerializeField]
		private Button worldMode;

		[SerializeField]
		private Button localMode;

		[Space]
		[SerializeField]
		private GameObject groupHolder;

		[SerializeField]
		private Button groupCenterMode;

		[SerializeField]
		private Button groupLocalMode;

		[Space]
		[SerializeField]
		private Color activeColor;

		[SerializeField]
		private Color deactiveColor;

		[Tooltip("default mode")]
		[SerializeField]
		private HandleType type;

		[SerializeField]
		private HandleType typeLastSelected;

		[SerializeField]
		private HandleSpace space = HandleSpace.LOCAL;

		[SerializeField]
		private Bounds globalBounds = new Bounds(Vector3.zero, Vector3.one * 100000f);

		[Header("Position Snapping")]
		[SerializeField]
		private Button positionSnappingButton;

		[SerializeField]
		private GameObject positionSnappingField;

		[SerializeField]
		private TMP_InputField positionSnappingValue;

		[SerializeField]
		private bool positionSnapping;

		public float PositionSnapStep;

		[Header("Angle Snapping")]
		[SerializeField]
		private Button angleSnappingButton;

		[SerializeField]
		private GameObject angleSnappingField;

		[SerializeField]
		private TMP_InputField angleSnappingValue;

		[SerializeField]
		private bool angleSnapping;

		[SerializeField]
		public float scrollStep = 15f;

		public float AngleSnapStep;

		private GlobalPosition editStartPosition;

		private Quaternion editStartRotation;

		public static bool DraggingHandle;

		private RuntimeTransformHandle handle;

		private SelectionDetails selection;

		private IValueWrapper<GlobalPosition> positionWrapper;

		private IValueWrapper<Quaternion> rotationWrapper;

		public Transform proxyTransform;

		private bool draggingRuntimeHandle;

		private DirectMode directMode;

		private Vector3 directStartPosition;

		private Vector3 directStartPoint;

		private Plane directPlane;

		public bool MouseHoverOrInteract => handle.MouseHoverOrInteract;

		private void OnDrawGizmosSelected()
		{
			Gizmos.color = Color.red;
			Vector3 center = globalBounds.center;
			if (Datum.origin != null)
			{
				center += Datum.originPosition;
			}
			Gizmos.DrawWireCube(center, globalBounds.size);
		}

		private void Awake()
		{
			Player player = ReInput.players.GetPlayer(0);
			ActionElementMap firstElementMapWithAction = player.controllers.maps.GetFirstElementMapWithAction("SelectUnitMode", skipDisabledMaps: false);
			TextMeshProUGUI componentInChildren = selectMode.GetComponentInChildren<TextMeshProUGUI>();
			if (firstElementMapWithAction != null && componentInChildren != null)
			{
				componentInChildren.text = firstElementMapWithAction.elementIdentifierName;
			}
			ActionElementMap firstElementMapWithAction2 = player.controllers.maps.GetFirstElementMapWithAction("TranslateUnitMode", skipDisabledMaps: false);
			TextMeshProUGUI componentInChildren2 = positionMode.GetComponentInChildren<TextMeshProUGUI>();
			if (firstElementMapWithAction2 != null && componentInChildren2 != null)
			{
				componentInChildren2.text = firstElementMapWithAction2.elementIdentifierName;
			}
			ActionElementMap firstElementMapWithAction3 = player.controllers.maps.GetFirstElementMapWithAction("RotateUnitMode", skipDisabledMaps: false);
			TextMeshProUGUI componentInChildren3 = rotationMode.GetComponentInChildren<TextMeshProUGUI>();
			if (firstElementMapWithAction3 != null && componentInChildren3 != null)
			{
				componentInChildren3.text = firstElementMapWithAction3.elementIdentifierName;
			}
			selectMode.onClick.AddListener(delegate
			{
				SetMode(HandleType.NONE);
			});
			positionMode.onClick.AddListener(delegate
			{
				SetMode(HandleType.POSITION);
			});
			rotationMode.onClick.AddListener(delegate
			{
				SetMode(HandleType.ROTATION);
			});
			worldMode.onClick.AddListener(delegate
			{
				SetSpace(HandleSpace.WORLD);
			});
			localMode.onClick.AddListener(delegate
			{
				SetSpace(HandleSpace.LOCAL);
			});
			groupCenterMode.onClick.AddListener(delegate
			{
				SetGroupRotate(GroupRotationMode.Center);
			});
			groupLocalMode.onClick.AddListener(delegate
			{
				SetGroupRotate(GroupRotationMode.Local);
			});
			positionSnappingButton.onClick.AddListener(TogglePositionSnapping);
			angleSnappingButton.onClick.AddListener(ToggleAngleSnapping);
			positionSnappingValue.onEndEdit.AddListener(delegate
			{
				RefreshSnapping();
			});
			angleSnappingValue.onEndEdit.AddListener(delegate
			{
				RefreshSnapping();
			});
			handle = Object.Instantiate(handlePrefab).GetComponent<RuntimeTransformHandle>();
			handle.startedDraggingHandle.AddListener(OnDragStart);
			handle.endedDraggingHandle.AddListener(OnDragEnd);
			proxyTransform = new GameObject("EditorHandleProxy").transform;
			handle.target = proxyTransform;
			unitSelection.OnSelect += UnitSelection_OnSelect;
			UnitSelection_OnSelect(unitSelection.SelectionDetails);
			SetMode(type, forceRefresh: true);
			SetSpace(space, forceRefresh: true);
			SetGroupRotate(MultiSelectSelectionDetails.RotationMode);
			type = HandleType.NONE;
			RefreshHandle();
			RefreshSnapping();
			RunEarlyUpdate().Forget();
		}

		private void OnDragStart()
		{
			draggingRuntimeHandle = true;
			DraggingHandle = true;
			CaptureEditStart();
		}

		private void OnDragEnd()
		{
			draggingRuntimeHandle = false;
			DraggingHandle = false;
			RecalculateMultiSelect();
		}

		private void OnDestroy()
		{
			DraggingHandle = false;
			unitSelection.OnSelect -= UnitSelection_OnSelect;
			ClearWrapper();
			if (proxyTransform != null)
			{
				Object.Destroy(proxyTransform.gameObject);
			}
		}

		private void UnitSelection_OnSelect(SelectionDetails selectionDetails)
		{
			selection = selectionDetails;
			ClearWrapper();
			bool flag = false;
			bool flag2 = false;
			if (selection != null)
			{
				positionWrapper = selection.PositionWrapper;
				rotationWrapper = selection.RotationWrapper;
				if (positionWrapper != null)
				{
					positionWrapper.RegisterOnChange(this, OnPositionChanged);
					OnPositionChanged(positionWrapper.Value);
				}
				if (rotationWrapper != null)
				{
					rotationWrapper.RegisterOnChange(this, OnRotationChanged);
					OnRotationChanged(rotationWrapper.Value);
				}
				flag = positionWrapper != null && selection.PositionHandleAllowed;
				flag2 = rotationWrapper != null && selection.RotationHandleAllowed;
			}
			type = typeLastSelected;
			if (type == HandleType.POSITION && !flag)
			{
				type = (flag2 ? HandleType.ROTATION : HandleType.NONE);
			}
			if (type == HandleType.ROTATION && !flag2)
			{
				type = ((!flag) ? HandleType.NONE : HandleType.POSITION);
			}
			RefreshHandle();
		}

		private void ClearSelection()
		{
			UnitSelection_OnSelect(null);
		}

		private void ClearWrapper()
		{
			positionWrapper?.UnregisterOnChange(this);
			rotationWrapper?.UnregisterOnChange(this);
			positionWrapper = null;
			rotationWrapper = null;
		}

		private void OnPositionChanged(GlobalPosition newValue)
		{
			proxyTransform.position = newValue.ToLocalPosition();
		}

		private void OnRotationChanged(Quaternion newValue)
		{
			proxyTransform.rotation = newValue;
		}

		public void SetMode(HandleType value, bool forceRefresh = false)
		{
			if (type != value || forceRefresh)
			{
				type = value;
				typeLastSelected = value;
				RefreshHandle();
				SetButtonColor(selectMode, type == HandleType.NONE);
				SetButtonColor(positionMode, type == HandleType.POSITION);
				SetButtonColor(rotationMode, type == HandleType.ROTATION);
			}
		}

		private void SetSpace(HandleSpace value, bool forceRefresh = false)
		{
			if (space != value || forceRefresh)
			{
				space = value;
				RefreshHandle();
				SetButtonColor(worldMode, space == HandleSpace.WORLD);
				SetButtonColor(localMode, space == HandleSpace.LOCAL);
			}
		}

		private void SetGroupRotate(GroupRotationMode mode)
		{
			MultiSelectSelectionDetails.RotationMode = mode;
			SetButtonColor(groupCenterMode, mode == GroupRotationMode.Center);
			SetButtonColor(groupLocalMode, mode == GroupRotationMode.Local);
			RecalculateMultiSelect();
		}

		private void RefreshHandle()
		{
			bool activeSelf = handle.gameObject.activeSelf;
			bool flag = selection != null && type != HandleType.NONE;
			if (activeSelf != flag)
			{
				handle.gameObject.SetActive(flag);
			}
			if (selection != null && type != HandleType.NONE)
			{
				handle.type = type;
				handle.axes = HandleAxes.XYZ;
				handle.space = space;
				if (type == HandleType.POSITION)
				{
					handle.axes = selection.AllowedPositionAxes;
				}
				handle.ForceRefresh();
			}
		}

		private async UniTask RunEarlyUpdate()
		{
			CancellationToken cancel = base.destroyCancellationToken;
			await UniTask.Yield(PlayerLoopTiming.LastEarlyUpdate);
			while (!cancel.IsCancellationRequested)
			{
				EarlyUpdate();
				await UniTask.Yield(PlayerLoopTiming.LastEarlyUpdate);
			}
		}

		private void EarlyUpdate()
		{
			if (selection != null && selection.IsDestroyed)
			{
				ClearSelection();
			}
		}

		private void Update()
		{
			if (draggingRuntimeHandle && type != HandleType.NONE)
			{
				if (type == HandleType.POSITION)
				{
					ApplyProxyPosition();
				}
				else if (type == HandleType.ROTATION)
				{
					ApplyProxyRotation();
				}
				handle.ForceRefresh();
				Physics.SyncTransforms();
			}
		}

		private GlobalPosition ClampBounds(GlobalPosition pos)
		{
			return pos;
		}

		private void TogglePositionSnapping()
		{
			positionSnapping = !positionSnapping;
			positionSnappingField.SetActive(positionSnapping);
			RefreshSnapping();
		}

		private void ToggleAngleSnapping()
		{
			angleSnapping = !angleSnapping;
			angleSnappingField.SetActive(angleSnapping);
			RefreshSnapping();
		}

		private void SetButtonColor(Button button, bool active)
		{
			if (button != null)
			{
				button.GetComponent<MaskableGraphic>().color = (active ? activeColor : deactiveColor);
			}
		}

		private void RefreshSnapping()
		{
			PositionSnapStep = 0f;
			AngleSnapStep = 0f;
			if (positionSnapping && float.TryParse(positionSnappingValue.text, out var result) && result > 0f)
			{
				PositionSnapStep = result;
			}
			if (angleSnapping && float.TryParse(angleSnappingValue.text, out var result2) && result2 > 0f)
			{
				AngleSnapStep = result2;
			}
			SetButtonColor(positionSnappingButton, PositionSnapStep > 0f);
			SetButtonColor(angleSnappingButton, AngleSnapStep > 0f);
		}

		public GlobalPosition SnapPosition(GlobalPosition position, HandleSpace? snapSpace = null)
		{
			if (PositionSnapStep <= 0f)
			{
				return position;
			}
			Vector3 v;
			if ((snapSpace ?? space) == HandleSpace.WORLD)
			{
				v = SnapVector3(position.AsVector3(), PositionSnapStep);
			}
			else
			{
				Vector3 vector = editStartPosition.AsVector3();
				Vector3 vector2 = position.AsVector3() - vector;
				Vector3 vector3 = SnapVector3(Quaternion.Inverse(editStartRotation) * vector2, PositionSnapStep);
				v = vector + editStartRotation * vector3;
			}
			return new GlobalPosition(v);
		}

		public Quaternion SnapRotation(Quaternion rotation, HandleSpace? snapSpace = null)
		{
			if (AngleSnapStep <= 0f)
			{
				return rotation;
			}
			if ((snapSpace ?? space) == HandleSpace.WORLD)
			{
				return Quaternion.Euler(SnapVector3(rotation.eulerAngles, AngleSnapStep));
			}
			Quaternion quaternion = Quaternion.Euler(SnapVector3((Quaternion.Inverse(editStartRotation) * rotation).eulerAngles, AngleSnapStep));
			return editStartRotation * quaternion;
		}

		private void ApplyProxyPosition()
		{
			if (positionWrapper == null)
			{
				return;
			}
			SelectionDetails selectionDetails = selection;
			if (selectionDetails != null && selectionDetails.PositionHandleAllowed)
			{
				GlobalPosition globalPosition = SnapPosition(proxyTransform.GlobalPosition());
				if (!SceneSingleton<MissionEditor>.i.allowCameraClip && selection is UnitSelectionDetails unitSelectionDetails)
				{
					globalPosition = unitSelectionDetails.ClampPosition(globalPosition);
				}
				globalPosition = ClampBounds(globalPosition);
				proxyTransform.position = globalPosition.ToLocalPosition();
				positionWrapper.SetValue(globalPosition, this);
			}
		}

		private void ApplyProxyRotation()
		{
			if (rotationWrapper != null)
			{
				SelectionDetails selectionDetails = selection;
				if (selectionDetails != null && selectionDetails.RotationHandleAllowed)
				{
					Quaternion quaternion = SnapRotation(proxyTransform.rotation);
					proxyTransform.rotation = quaternion;
					rotationWrapper.SetValue(quaternion, this);
				}
			}
		}

		private static Vector3 SnapVector3(Vector3 value, float step)
		{
			return new Vector3(Snap(value.x, step), Snap(value.y, step), Snap(value.z, step));
		}

		private static float Snap(float value, float step)
		{
			return Mathf.Round(value / step) * step;
		}

		public Vector3 SnapPlacementPosition(Vector3 localPosition, bool snapY)
		{
			if (PositionSnapStep <= 0f)
			{
				return localPosition;
			}
			GlobalPosition position = localPosition.ToGlobalPosition();
			position.x = Snap(position.x, PositionSnapStep);
			position.z = Snap(position.z, PositionSnapStep);
			if (snapY)
			{
				position.y = Mathf.Ceil(position.y / PositionSnapStep) * PositionSnapStep;
			}
			return position.ToLocalPosition();
		}

		public float SnapAngle(float angle)
		{
			if (!(AngleSnapStep > 0f))
			{
				return angle;
			}
			return Snap(angle, AngleSnapStep);
		}

		public bool CanDirectManipulate(SelectionDetails target, bool rotate)
		{
			if (target == null)
			{
				return false;
			}
			if (!rotate)
			{
				return target.PositionHandleAllowed;
			}
			return target.RotationHandleAllowed;
		}

		public bool BeginDirectManipulation(bool rotate, bool vertical, Vector2 mousePosition)
		{
			if (!CanDirectManipulate(selection, rotate))
			{
				return false;
			}
			directMode = DirectMode.None;
			if (!UpdateDirectManipulation(rotate, vertical, mousePosition))
			{
				return false;
			}
			DraggingHandle = true;
			return true;
		}

		public bool UpdateDirectManipulation(bool rotate, bool vertical, Vector2 mousePosition)
		{
			bool flag = SceneSingleton<CameraStateManager>.i.mainCamera.velocity.sqrMagnitude > 0.01f;
			DirectMode directMode = ((rotate && !flag) ? DirectMode.LookAt : ((!vertical) ? DirectMode.MoveHorizontal : DirectMode.MoveVertical));
			if (!CanDirectManipulate(selection, directMode == DirectMode.LookAt))
			{
				return false;
			}
			if (!SetDirectMode(directMode, mousePosition))
			{
				return false;
			}
			if (this.directMode == DirectMode.LookAt)
			{
				return UpdateDirectLookAt(mousePosition);
			}
			return UpdateDirectMove(mousePosition);
		}

		public void EndDirectManipulation()
		{
			if (directMode != DirectMode.None)
			{
				if (directMode == DirectMode.LookAt)
				{
					RecalculateMultiSelect();
				}
				directMode = DirectMode.None;
				DraggingHandle = false;
			}
		}

		private bool SetDirectMode(DirectMode mode, Vector2 mousePosition)
		{
			if (directMode == mode)
			{
				return true;
			}
			if (directMode == DirectMode.LookAt)
			{
				RecalculateMultiSelect();
			}
			directMode = DirectMode.None;
			CaptureEditStart();
			switch (mode)
			{
			case DirectMode.MoveHorizontal:
			case DirectMode.MoveVertical:
				directStartPosition = editStartPosition.ToLocalPosition();
				directPlane = ((mode == DirectMode.MoveVertical) ? CreateVerticalDragPlane(directStartPosition) : new Plane(Vector3.up, directStartPosition));
				if (!TryGetMousePlanePoint(directPlane, mousePosition, out directStartPoint))
				{
					return false;
				}
				break;
			}
			directMode = mode;
			return true;
		}

		private bool UpdateDirectMove(Vector2 mousePosition)
		{
			if (!TryGetMousePlanePoint(directPlane, mousePosition, out var point))
			{
				return false;
			}
			Vector3 vector = ((directMode == DirectMode.MoveVertical) ? (Vector3.up * (point.y - directStartPoint.y)) : (point - directStartPoint));
			proxyTransform.position = directStartPosition + vector;
			ApplyProxyPosition();
			return true;
		}

		private bool UpdateDirectLookAt(Vector2 mousePosition)
		{
			Vector3 position = proxyTransform.position;
			if (!TryGetMousePlanePoint(new Plane(Vector3.up, position), mousePosition, out var point))
			{
				return false;
			}
			Vector3 vector = point - position;
			vector.y = 0f;
			if (vector.sqrMagnitude < 0.001f)
			{
				return true;
			}
			if (SceneSingleton<MissionEditor>.i.allowTerrainFollowing)
			{
				Vector3 eulerAngles = editStartRotation.eulerAngles;
				eulerAngles.y = Quaternion.LookRotation(vector.normalized, Vector3.up).eulerAngles.y;
				proxyTransform.rotation = Quaternion.Euler(eulerAngles);
			}
			else
			{
				proxyTransform.rotation = Quaternion.LookRotation(vector.normalized, Vector3.up);
			}
			ApplyProxyRotation();
			return true;
		}

		private static Plane CreateVerticalDragPlane(Vector3 point)
		{
			Transform transform = Camera.main.transform;
			Vector3 vector = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
			if (vector.sqrMagnitude < 0.001f)
			{
				vector = Vector3.ProjectOnPlane(transform.up, Vector3.up);
			}
			if (vector.sqrMagnitude < 0.001f)
			{
				vector = Vector3.forward;
			}
			return new Plane(vector.normalized, point);
		}

		private static bool TryGetMousePlanePoint(Plane plane, Vector2 mousePosition, out Vector3 point)
		{
			Ray ray = Camera.main.ScreenPointToRay(mousePosition);
			if (plane.Raycast(ray, out var enter))
			{
				point = ray.GetPoint(enter);
				return true;
			}
			point = default(Vector3);
			return false;
		}

		private void CaptureEditStart()
		{
			editStartPosition = ((positionWrapper != null) ? positionWrapper.Value : proxyTransform.GlobalPosition());
			editStartRotation = ((rotationWrapper != null) ? rotationWrapper.Value : proxyTransform.rotation);
		}

		private void RecalculateMultiSelect()
		{
			if (selection is MultiSelectSelectionDetails multiSelectSelectionDetails)
			{
				multiSelectSelectionDetails.RecalculatePositionsAndRotation();
			}
		}
	}
}
