using NuclearOption.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts
{
	public class ButtonStyleController : MonoBehaviour
	{
		[SerializeField]
		private TextMeshProUGUI text;

		[SerializeField]
		private MaskableGraphic image;

		[SerializeField]
		private MaskableGraphic boarder;

		public ButtonStyle GetCurrentStyle()
		{
			return new ButtonStyle
			{
				TextColor = ((text != null) ? text.color : Color.white),
				ImageColor = ((image != null) ? image.color : Color.white),
				BorderColor = ((boarder != null) ? boarder.color : Color.white)
			};
		}

		public void ApplyStyle(ButtonStyle style)
		{
			if (text != null)
			{
				text.color = style.TextColor;
			}
			if (image != null)
			{
				image.color = style.ImageColor;
			}
			if (boarder != null)
			{
				boarder.color = style.BorderColor;
			}
		}

		private void OnValidate()
		{
			if (text == null)
			{
				text = GetComponentInChildren<TextMeshProUGUI>();
			}
			if (image == null)
			{
				image = GetComponent<Image>();
			}
			if (boarder == null)
			{
				boarder = GetComponentInChildren<BetterBorder>();
			}
		}
	}
}
