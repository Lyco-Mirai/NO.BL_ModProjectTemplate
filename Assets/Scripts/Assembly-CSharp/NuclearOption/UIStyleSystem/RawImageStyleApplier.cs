using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.UIStyleSystem
{
	public class RawImageStyleApplier : StyleApplier<ImageStyle>
	{
		private RawImage rawImage;

		private Color initialColor;

		private RawImage RawImage => rawImage ?? (rawImage = GetComponent<RawImage>());

		private void Awake()
		{
			initialColor = RawImage.color;
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
				RawImage.color = color2;
			}
			else
			{
				RawImage.color = initialColor;
			}
		}
	}
}
