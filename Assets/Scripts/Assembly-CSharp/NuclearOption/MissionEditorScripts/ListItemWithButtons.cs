using JamesFrowen.ScriptableVariables.UI;
using TMPro;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts
{
	public class ListItemWithButtons : ListItem<ItemWithButtonsWrapper>
	{
		private static readonly ProfilerMarker setValueMarker = new ProfilerMarker("ListItemWithButtons SetValue");

		[Header("Main Text")]
		[SerializeField]
		protected TextMeshProUGUI text;

		[Header("Right buttons")]
		[SerializeField]
		private Button editButton;

		[Space]
		[SerializeField]
		private Button removeButton;

		[SerializeField]
		protected TextMeshProUGUI remoteText;

		[Header("Left buttons")]
		[SerializeField]
		private Button moveUpButton;

		[SerializeField]
		private Button moveDownButton;

		[SerializeField]
		private GameObject buttonGroup;

		[Header("Layout")]
		[SerializeField]
		private int heightPerLine;

		[SerializeField]
		private int padding;

		[SerializeField]
		private int minHeight;

		private RectTransform RectTransform => (RectTransform)base.transform;

		protected override void Awake()
		{
			base.Awake();
			editButton.onClick.AddListener(EditClicked);
			removeButton.onClick.AddListener(RemoveClicked);
			moveUpButton.onClick.AddListener(MoveUpClicked);
			moveDownButton.onClick.AddListener(MoveDownClicked);
		}

		private void EditClicked()
		{
			base.Value.EditClicked?.Invoke(base.Value.Index);
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

		protected sealed override void SetValue(ItemWithButtonsWrapper value)
		{
			using (setValueMarker.Auto())
			{
				if (!value.Enabled)
				{
					SetActive(active: false);
					return;
				}
				if (editButton.gameObject != base.gameObject)
				{
					editButton.gameObject.SetActive(value.EditClicked != null);
				}
				removeButton.gameObject.SetActive(value.DeleteClicked != null);
				buttonGroup.SetActive(value.MoveClicked != null);
				moveUpButton.gameObject.SetActive(value.Index > 0);
				moveDownButton.gameObject.SetActive(value.Index < value.Count - 1);
				if (!string.IsNullOrEmpty(value.DeleteButtonText))
				{
					remoteText.text = value.DeleteButtonText;
				}
				Vector2 sizeDelta = RectTransform.sizeDelta;
				sizeDelta.y = CalculateHeight(value);
				RectTransform.sizeDelta = sizeDelta;
				if (text != null)
				{
					text.text = value.Text;
				}
			}
		}

		public override float CalculateHeight(ItemWithButtonsWrapper value)
		{
			int num = value.Text.CountLines();
			return Mathf.Max(padding + num * heightPerLine, minHeight);
		}
	}
}
