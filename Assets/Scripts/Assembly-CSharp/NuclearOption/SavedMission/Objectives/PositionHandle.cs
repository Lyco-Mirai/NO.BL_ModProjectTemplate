using System;
using System.Linq;
using NuclearOption.MissionEditorScripts;
using UnityEngine;

namespace NuclearOption.SavedMission.Objectives
{
	public class PositionHandle : MonoBehaviour, IEditorSelectable
	{
		[SerializeField]
		private MeshRenderer renderer;

		[SerializeField]
		private bool scaleWithDistance;

		[SerializeField]
		private AnimationCurve scaleCurve;

		[SerializeField]
		private Color normalColor;

		[SerializeField]
		private Color hoverColor;

		[SerializeField]
		private Color selectColor;

		private Func<string> getParentName;

		private float oldScale;

		private Action destroyCallback;

		public IValueWrapper<GlobalPosition> PositionWrapper { get; private set; }

		public bool Selected { get; private set; }

		public bool Hover { get; private set; }

		public void SetHue(Color hueColor)
		{
			Color.RGBToHSV(hueColor, out var H, out var _, out var _);
			normalColor = normalColor.ChangeHue(H);
			hoverColor = hoverColor.ChangeHue(H);
			selectColor = selectColor.ChangeHue(H);
			SetColor(normalColor);
		}

		public void Setup(IValueWrapper<GlobalPosition> wrapper, Func<string> getParentName, Action destroyCallback)
		{
			if (PositionWrapper != wrapper && Selected)
			{
				SceneSingleton<UnitSelection>.i.ClearSelection(this);
			}
			this.destroyCallback = destroyCallback;
			PositionWrapper = wrapper;
			wrapper.RegisterOnChange(this, PositionChanged);
			PositionChanged(wrapper.Value);
			this.getParentName = getParentName;
			base.gameObject.SetActive(value: true);
			SetColor(normalColor);
		}

		private void Awake()
		{
			SceneSingleton<UnitSelection>.i.OnSelect += SelectionChanged;
			SceneSingleton<UnitSelection>.i.OnHover += HoverChanged;
		}

		private void OnDestroy()
		{
			SceneSingleton<UnitSelection>.i.OnSelect -= SelectionChanged;
			SceneSingleton<UnitSelection>.i.OnHover -= HoverChanged;
			if (PositionWrapper != null)
			{
				PositionWrapper.UnregisterOnChange(this);
			}
			if (Selected)
			{
				SceneSingleton<UnitSelection>.i.ClearSelection(this);
			}
		}

		private void PositionChanged(GlobalPosition newValue)
		{
			base.transform.position = newValue.ToLocalPosition();
			Physics.SyncTransforms();
		}

		public void Hide()
		{
			if (PositionWrapper != null)
			{
				PositionWrapper.UnregisterOnChange(this);
			}
			PositionWrapper = null;
			base.gameObject.SetActive(value: false);
			if (Selected)
			{
				SceneSingleton<UnitSelection>.i.ClearSelection(this);
			}
		}

		private void Update()
		{
			if (scaleWithDistance)
			{
				GlobalPosition b = SceneSingleton<CameraStateManager>.i.mainCamera.transform.position.ToGlobalPosition();
				float time = FastMath.Distance(PositionWrapper.Value, b);
				float num = scaleCurve.Evaluate(time);
				if (num != oldScale)
				{
					base.transform.localScale = Vector3.one * num;
					Physics.SyncTransforms();
					oldScale = num;
				}
			}
		}

		private void SelectionChanged(SelectionDetails details)
		{
			if ((!(details is SingleSelectionDetails singleSelectionDetails)) ? (details is MultiSelectSelectionDetails multiSelectSelectionDetails && multiSelectSelectionDetails.Items.Any((SingleSelectionDetails x) => x.Source == this)) : (singleSelectionDetails?.Source == this))
			{
				Selected = true;
				SetColor(selectColor);
			}
			else
			{
				Selected = false;
				SetColor(normalColor);
			}
		}

		private void HoverChanged(SingleSelectionDetails details)
		{
			if (!Selected)
			{
				if (details?.Source == this)
				{
					SetColor(hoverColor);
				}
				else
				{
					SetColor(normalColor);
				}
			}
		}

		private void SetColor(Color color)
		{
			renderer.material.color = color;
		}

		public string GetDisplayName()
		{
			string arg = getParentName();
			Vector3 vector = PositionWrapper.Value.AsVector3();
			return $"{arg} - {vector}";
		}

		SingleSelectionDetails IEditorSelectable.CreateSelectionDetails()
		{
			return new PositionSelectionDetails(this, destroyCallback);
		}
	}
}
