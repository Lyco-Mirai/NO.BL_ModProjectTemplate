using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.UIStyleSystem
{
	public class ImageStyleApplier : StyleApplier<ImageStyle>
	{
		private Image image;

		private Color initialColor;

		private Image Image => image ?? (image = GetComponent<Image>());

		private void Awake()
		{
			initialColor = Image.color;
		}

		protected override void Apply()
		{
			ImageStyle style = GetStyle();
			Color color = style.Color;
			if (color.r != 0f || color.g != 0f || color.b != 0f)
			{
				Color color2 = style.Color;
				if (style.Color.a == 0f)
				{
					color2.a = initialColor.a;
				}
				Image.color = color2;
			}
			else
			{
				Image.color = initialColor;
			}
		}
	}
}
