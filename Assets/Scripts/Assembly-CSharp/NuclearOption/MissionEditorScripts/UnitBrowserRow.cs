using System;
using NuclearOption.SavedMission;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts
{
	public class UnitBrowserRow : MonoBehaviour
	{
		public Toggle Toggle;

		[SerializeField]
		private Image unitIcon;

		[SerializeField]
		private Button focusButton;

		[SerializeField]
		private TextMeshProUGUI label;

		public SavedUnit SavedUnit;

		private Action<SavedUnit, bool> onToggleChanged;

		private Action<SavedUnit> onFocus;

		private void Awake()
		{
			Toggle.onValueChanged.AddListener(Toggle_OnChanged);
			focusButton.onClick.AddListener(FocusButton_OnClick);
		}

		private void OnDestroy()
		{
			Toggle.onValueChanged.RemoveListener(Toggle_OnChanged);
			focusButton.onClick.RemoveListener(FocusButton_OnClick);
		}

		public void Setup(SavedUnit savedUnit, bool isSelected, Action<SavedUnit, bool> onSelectionChanged, Action<SavedUnit> onFocus)
		{
			SavedUnit = savedUnit;
			onToggleChanged = onSelectionChanged;
			this.onFocus = onFocus;
			Toggle.SetIsOnWithoutNotify(isSelected);
			Unit unit = savedUnit.Unit;
			label.text = GetLabel(unit);
			if (unit is Aircraft aircraft)
			{
				unitIcon.sprite = aircraft.definition.mapIcon;
			}
			else
			{
				unitIcon.sprite = unit.definition.friendlyIcon;
			}
			unitIcon.color = ((unit.NetworkHQ != null) ? unit.NetworkHQ.faction.color : Color.white);
			unitIcon.enabled = true;
		}

		public void Clear()
		{
			SavedUnit = null;
			onToggleChanged = null;
			onFocus = null;
			Toggle.SetIsOnWithoutNotify(value: false);
		}

		private void Toggle_OnChanged(bool selected)
		{
			onToggleChanged?.Invoke(SavedUnit, selected);
		}

		private void FocusButton_OnClick()
		{
			onFocus?.Invoke(SavedUnit);
		}

		internal static string GetLabel(Unit unit)
		{
			return unit.unitName + " (" + unit.UniqueName + ")";
		}
	}
}
