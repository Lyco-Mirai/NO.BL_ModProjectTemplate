using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.UIStyleSystem
{
	public class ButtonStyleApplier : StyleApplier<ButtonStyle>
	{
		private TextMeshProUGUI text;

		private Button button;

		private Color initialNormalColor;

		private Color initialHighlightedColor;

		private Button Button => button ?? (button = GetComponent<Button>());

		private void Awake()
		{
			initialNormalColor = Button.colors.normalColor;
			initialHighlightedColor = Button.colors.highlightedColor;
		}

		protected override void Apply()
		{
			ButtonStyle style = GetStyle();
			Color color = style.Color;
			if (color.r != 0f || color.g != 0f || color.b != 0f)
			{
				Color color2 = style.Color;
				Color.RGBToHSV(color2, out var H, out var S, out var V);
				Color normalColor = Color.HSVToRGB(H, S / 2f, V / 2f);
				if (style.Color.a == 0f)
				{
					normalColor.a = initialNormalColor.a;
					color2.a = initialHighlightedColor.a;
				}
				Button.colors = new ColorBlock
				{
					normalColor = normalColor,
					highlightedColor = color2,
					pressedColor = Button.colors.pressedColor,
					selectedColor = Button.colors.selectedColor,
					disabledColor = Button.colors.disabledColor,
					colorMultiplier = Button.colors.colorMultiplier,
					fadeDuration = Button.colors.fadeDuration
				};
			}
			else
			{
				Button.colors = new ColorBlock
				{
					normalColor = initialNormalColor,
					highlightedColor = initialHighlightedColor,
					pressedColor = Button.colors.pressedColor,
					selectedColor = Button.colors.selectedColor,
					disabledColor = Button.colors.disabledColor,
					colorMultiplier = Button.colors.colorMultiplier,
					fadeDuration = Button.colors.fadeDuration
				};
			}
		}
	}
}
