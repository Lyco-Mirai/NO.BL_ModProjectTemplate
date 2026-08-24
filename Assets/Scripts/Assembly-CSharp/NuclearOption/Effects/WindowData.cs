using UnityEngine;

namespace NuclearOption.Effects
{
	public struct WindowData
	{
		public Vector2Int index;

		public float snapping;

		public float size;

		public Vector4 ToVector4()
		{
			return new Vector4(index.x, index.y, snapping, size);
		}
	}
}
