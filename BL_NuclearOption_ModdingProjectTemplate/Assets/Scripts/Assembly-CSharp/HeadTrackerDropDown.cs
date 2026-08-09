using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HeadTrackerDropDown : MonoBehaviour
{
	[Header("UI References")]
	[Tooltip("Drag your TextMeshPro Dropdown here")]
	public TMP_Dropdown selectedHeadTrackerDropDown;

	public ControlsMenu controlsMenu;

	private void Start()
	{
		if (selectedHeadTrackerDropDown == null)
		{
			selectedHeadTrackerDropDown = GetComponent<TMP_Dropdown>();
		}
		if (controlsMenu == null)
		{
			controlsMenu = GetComponentInParent<ControlsMenu>();
		}
		if (controlsMenu != null && controlsMenu.useHeadTracker != null)
		{
			controlsMenu.useHeadTracker.onValueChanged.AddListener(OnUseHeadTrackerChanged);
			OnUseHeadTrackerChanged(controlsMenu.useHeadTracker.isOn);
		}
		PopulateDropdown();
		selectedHeadTrackerDropDown.SetValueWithoutNotify((int)PlayerSettings.headTrackerType);
		selectedHeadTrackerDropDown.onValueChanged.AddListener(OnDropdownValueChanged);
	}

	private void OnUseHeadTrackerChanged(bool isOn)
	{
		if (!isOn)
		{
			base.gameObject.SetActive(value: false);
		}
		else
		{
			base.gameObject.SetActive(value: true);
		}
	}

	private void PopulateDropdown()
	{
		selectedHeadTrackerDropDown.ClearOptions();
		List<string> options = new List<string>(Enum.GetNames(typeof(HeadTrackerType)));
		selectedHeadTrackerDropDown.AddOptions(options);
		selectedHeadTrackerDropDown.RefreshShownValue();
	}

	private void OnDropdownValueChanged(int index)
	{
		HeadTrackerType headTrackerType = (HeadTrackerType)index;
		Debug.Log("User selected: " + headTrackerType);
		if (controlsMenu != null)
		{
			controlsMenu.SavePlayerSettings();
		}
	}
}
