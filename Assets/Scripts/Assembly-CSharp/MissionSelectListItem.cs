using System.Collections.Generic;
using JamesFrowen.ScriptableVariables.UI;
using NuclearOption.SavedMission;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MissionSelectListItem : ListItem<MissionSelectListItem.Item>
{
	public readonly struct Item
	{
		public readonly MissionSelectPanel Menu;

		public readonly MissionKey Key;

		public readonly MissionQuickLoad Mission;

		public Item(MissionSelectPanel menu, MissionKey key, MissionQuickLoad mission)
		{
			Menu = menu;
			Key = key;
			Mission = mission;
		}
	}

	public Button Button;

	[SerializeField]
	private TextMeshProUGUI text;

	[SerializeField]
	private MissionTagList tagList;

	protected override void Awake()
	{
		Button.onClick.AddListener(OnClick);
	}

	private void OnClick()
	{
		base.Value.Menu.SetSelectedMission(base.Value.Key);
	}

	protected override void SetValue(Item value)
	{
		text.text = value.Key.Name;
		bool flag = value.Key.Equals(value.Menu.SelectedMission);
		Button.interactable = !flag;
		List<MissionTag> tags = value.Mission.missionSettings.Tags;
		tagList.UpdateList(tags);
	}
}
