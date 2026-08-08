using System.Collections.Generic;
using NuclearOption.SavedMission;
using TMPro;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.UI;

public class MissionSelectGroupButtons : MonoBehaviour
{
	private static readonly ProfilerMarker createTabButtonsMarker = new ProfilerMarker("MissionSelectGroupButtons.CreateTabButtons");

	private static readonly ProfilerMarker tabButtonOnClickMarker = new ProfilerMarker("MissionSelectGroupButtons.TabButtonOnClick");

	private static readonly ProfilerMarker setPickerFilterMarker = new ProfilerMarker("MissionSelectGroupButtons.SetPickerFilter");

	[SerializeField]
	private Button tabButtonPrefab;

	[SerializeField]
	private RectTransform buttonHolder;

	[SerializeField]
	private MissionSelectPanel panel;

	private Button activeButton;

	private readonly List<MissionGroup> disallowedGroups = new List<MissionGroup>();

	private readonly Dictionary<MissionGroup, Button> buttons = new Dictionary<MissionGroup, Button>();

	private void Awake()
	{
		MissionGroup.AllGroup all = MissionGroup.All;
		Button button = CreateTabButtons(all);
		TabButtonOnClick(button, all);
	}

	private Button CreateTabButtons(MissionGroup.AllGroup startingGroup)
	{
		using (createTabButtonsMarker.Auto())
		{
			Button result = null;
			MissionGroup[] allGroups = MissionGroup.AllGroups;
			foreach (MissionGroup group in allGroups)
			{
				Button button = Object.Instantiate(tabButtonPrefab, buttonHolder);
				buttons[group] = button;
				button.GetComponentInChildren<TextMeshProUGUI>().text = group.Name;
				button.onClick.AddListener(delegate
				{
					TabButtonOnClick(button, group);
				});
				if (group == startingGroup)
				{
					result = button;
				}
				if (disallowedGroups.Contains(group))
				{
					button.gameObject.SetActive(value: false);
				}
			}
			return result;
		}
	}

	private void TabButtonOnClick(Button button, MissionGroup group)
	{
		using (tabButtonOnClickMarker.Auto())
		{
			SetButtonColor(ref activeButton, button);
			panel.SetActiveGroup(group);
		}
	}

	private static void SetButtonColor(ref Button activeButton, Button button)
	{
		if (activeButton != null)
		{
			activeButton.interactable = true;
		}
		activeButton = button;
		if (activeButton != null)
		{
			activeButton.interactable = false;
		}
	}

	public void SetPickerFilter(MissionsPicker.Filter filter)
	{
		using (setPickerFilterMarker.Auto())
		{
			if (filter.DisallowedGroups == null)
			{
				return;
			}
			disallowedGroups.AddRange(filter.DisallowedGroups);
			foreach (MissionGroup disallowedGroup in filter.DisallowedGroups)
			{
				if (buttons.TryGetValue(disallowedGroup, out var value))
				{
					value.gameObject.SetActive(value: false);
				}
			}
		}
	}
}
