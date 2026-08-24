using TMPro;
using UnityEngine;

namespace NuclearOption.UIStyleSystem
{
	public class TextStyleApplier : StyleApplier<TextStyle>
	{
		private TextMeshProUGUI text;

		private Color initialColor;

		private float initialFontSize;

		private TMP_FontAsset initialFont;

		private TextMeshProUGUI Text => text ?? (text = GetComponent<TextMeshProUGUI>());

		private void Awake()
		{
			initialColor = Text.color;
			initialFontSize = Text.fontSize;
			initialFont = Text.font;
		}

		protected override void Apply()
		{
			TextStyle style = GetStyle();
			if (style.Size > 0f || !Mathf.Approximately(style.FontScaling, 1f) || style.FontScaling != 0f || Context != ThemeManager.ThemeContext.HUD)
			{
				Text.fontSize = ((style.Size > 0f) ? style.Size : (initialFontSize * style.FontScaling));
			}
			Text.font = ((style.Font != null) ? style.Font : initialFont);
			Color color = style.Color;
			if (color.r != 0f || color.g != 0f || color.b != 0f)
			{
				Color color2 = style.Color;
				if (style.Color.a == 0f)
				{
					color2.a = initialColor.a;
				}
				Text.color = color2;
			}
			else
			{
				Text.color = initialColor;
			}
		}
	}
}
