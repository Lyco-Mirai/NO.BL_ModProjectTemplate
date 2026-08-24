using JamesFrowen.ScriptableVariables.UI;
using NuclearOption.SavedMission;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MissionTagListItem : ListItem<MissionTag>
{
	[SerializeField]
	private LayoutElement layout;

	[SerializeField]
	private TextMeshProUGUI text;

	[SerializeField]
	private Image image;

	[SerializeField]
	private float padding = 6f;

	protected override void SetValue(MissionTag tag)
	{
		text.text = tag.Tag;
		image.color = tag.Color;
		SetSize();
	}

	private void SetSize()
	{
		float num = text.preferredWidth + padding * 2f;
		layout.preferredWidth = num;
		layout.minWidth = num;
	}

	private void OnValidate()
	{
		if (layout != null && text != null)
		{
			SetSize();
		}
	}
}
