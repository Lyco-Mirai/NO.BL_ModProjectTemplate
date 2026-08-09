using System.Collections.Generic;
using JamesFrowen.ScriptableVariables.UI;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts
{
	public class EmptyDataListItem : ListItem<EmptyDataItemWrapper>
	{
		[Header("Right buttons")]
		[SerializeField]
		private Button removeButton;

		[Header("Left buttons")]
		[SerializeField]
		private int buttonLeftPad;

		[SerializeField]
		private Button moveUpButton;

		[SerializeField]
		private Button moveDownButton;

		[SerializeField]
		private GameObject buttonGroup;

		[SerializeField]
		private VerticalLayoutGroup layout;

		[SerializeField]
		private RectTransform content;

		private static readonly List<Transform> tmp = new List<Transform>();

		protected override void Awake()
		{
			removeButton.onClick.AddListener(RemoveClicked);
			moveUpButton.onClick.AddListener(MoveUpClicked);
			moveDownButton.onClick.AddListener(MoveDownClicked);
		}

		private void RemoveClicked()
		{
			base.Value.DeleteClicked?.Invoke(base.Value.Index);
		}

		private void MoveUpClicked()
		{
			base.Value.MoveClicked?.Invoke(base.Value.Index, base.Value.Index - 1);
		}

		private void MoveDownClicked()
		{
			base.Value.MoveClicked?.Invoke(base.Value.Index, base.Value.Index + 1);
		}

		protected sealed override void SetValue(EmptyDataItemWrapper value)
		{
			removeButton.gameObject.SetActive(base.Value.DeleteClicked != null);
			buttonGroup.SetActive(value.MoveClicked != null);
			moveUpButton.gameObject.SetActive(base.Value.Index > 0);
			moveDownButton.gameObject.SetActive(base.Value.Index < base.Value.Count - 1);
			layout.padding.left = ((value.MoveClicked != null) ? buttonLeftPad : 0);
			float x = ((RectTransform)base.transform).sizeDelta.x;
			Vector2 sizeDelta = content.sizeDelta;
			sizeDelta.x = x - (float)layout.padding.left - (float)layout.padding.right;
			content.sizeDelta = sizeDelta;
			foreach (object item in content)
			{
				tmp.Add((Transform)item);
			}
			foreach (Transform item2 in tmp)
			{
				Object.Destroy(item2.gameObject);
			}
			tmp.Clear();
			value.DrawContent(value.Index, value.Value, content);
		}
	}
}
