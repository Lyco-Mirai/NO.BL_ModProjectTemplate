using System;
using JamesFrowen.ScriptableVariables.UI;
using NuclearOption.SavedMission;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TagFilterListItem : ListItem<TagFilterListItem.Item>, IPointerClickHandler, IEventSystemHandler
{
	public readonly struct Item
	{
		public readonly MissionTag Tag;

		public readonly Action<MissionTag> OnClick;

		public readonly bool Enabled;

		public readonly int RefCount;

		public Item(MissionTag tag, Action<MissionTag> onClick, bool enabled, int refCount)
		{
			Tag = tag;
			OnClick = onClick;
			Enabled = enabled;
			RefCount = refCount;
		}
	}

	[SerializeField]
	private LayoutElement layout;

	[SerializeField]
	private TextMeshProUGUI text;

	[SerializeField]
	private Image image;

	[SerializeField]
	private Image boarder;

	[SerializeField]
	private float padding = 6f;

	[Header("Colors")]
	[SerializeField]
	private float enabledColor = 1.2f;

	[SerializeField]
	private float normalColor = 1f;

	[SerializeField]
	private float disableColor = 0.4f;

	[Space]
	[SerializeField]
	private float boarderColor = 1.3f;

	protected override void SetValue(Item value)
	{
		MissionTag missionTag = value.Tag;
		if (!value.Enabled && value.RefCount > 0)
		{
			text.text = $"{missionTag.Tag} ({value.RefCount})";
		}
		else
		{
			text.text = missionTag.Tag;
		}
		if (value.Enabled)
		{
			SetColor(missionTag.Color * enabledColor);
		}
		else if (value.RefCount > 0)
		{
			SetColor(missionTag.Color * normalColor);
		}
		else
		{
			SetColor(missionTag.Color * disableColor);
		}
		SetSize();
	}

	private void SetColor(Color color)
	{
		image.color = color;
		boarder.color = color * boarderColor;
	}

	private void SetSize()
	{
		float num = text.preferredWidth + padding * 2f;
		if (layout.preferredWidth != num)
		{
			layout.preferredWidth = num;
			layout.minWidth = num;
		}
	}

	private void OnValidate()
	{
		if (layout != null && text != null)
		{
			SetSize();
		}
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		if (base.Value.Enabled || base.Value.RefCount > 0)
		{
			base.Value.OnClick?.Invoke(base.Value.Tag);
		}
	}
}
