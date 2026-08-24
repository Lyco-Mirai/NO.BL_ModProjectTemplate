using TMPro;
using UnityEngine;

namespace NuclearOption.MissionEditorScripts.Buttons
{
	public class HoverText : MonoBehaviour
	{
		[SerializeField]
		private GameObject hover;

		[SerializeField]
		private TextMeshProUGUI text;

		[SerializeField]
		private float offset;

		private RectTransform rectTransform;

		private object key;

		private void Awake()
		{
			rectTransform = GetComponent<RectTransform>();
			Hide(null);
		}

		public void Show(object key, string showText)
		{
			this.key = key;
			if (text == null)
			{
				text = hover.GetComponentInChildren<TextMeshProUGUI>();
			}
			if (showText != null)
			{
				text.text = showText;
			}
			hover.SetActive(value: true);
			FixLayout.ForceRebuildRecursive((RectTransform)hover.transform);
		}

		public void Hide(object key)
		{
			if (this.key == key)
			{
				hover.SetActive(value: false);
			}
		}

		public void Refresh(string showText)
		{
			if (text != null && hover.activeSelf)
			{
				if (showText != null)
				{
					text.text = showText;
				}
				FixLayout.ForceRebuildRecursive((RectTransform)hover.transform);
			}
		}

		public void Move(object key, Vector2 pos)
		{
			if (hover.activeSelf && this.key == key)
			{
				Vector2 vector = offset * new Vector2(1f, -1f);
				float width = rectTransform.rect.width;
				if (pos.x > (float)Screen.width - width - offset)
				{
					rectTransform.pivot = new Vector2(1f, 1f);
					vector = offset * new Vector2(-1f, -1f);
				}
				else
				{
					rectTransform.pivot = new Vector2(0f, 1f);
					vector = offset * new Vector2(1f, -1f);
				}
				Vector2 vector2 = pos + vector;
				hover.transform.position = vector2;
			}
		}
	}
}
