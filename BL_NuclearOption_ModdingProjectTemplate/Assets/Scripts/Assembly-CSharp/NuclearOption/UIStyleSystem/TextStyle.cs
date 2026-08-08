using System;
using TMPro;
using UnityEngine;

namespace NuclearOption.UIStyleSystem
{
	[Serializable]
	public class TextStyle
	{
		public Color Color;

		public float Size;

		[Range(0f, 2f)]
		public float FontScaling = 1f;

		public TMP_FontAsset Font;
	}
}
