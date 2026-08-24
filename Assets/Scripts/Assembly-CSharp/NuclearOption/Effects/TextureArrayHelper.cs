using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NuclearOption.Effects
{
	public static class TextureArrayHelper
	{
		public enum DimensionsMode
		{
			ForceSquare = 0,
			AssertAllSame = 1
		}

		public static Vector2Int GetMaxDimensions(IReadOnlyList<Texture2D> textures, DimensionsMode mode)
		{
			if (mode == DimensionsMode.AssertAllSame)
			{
				int width = textures[0].width;
				int height = textures[0].height;
				for (int i = 1; i < textures.Count; i++)
				{
					if (textures[i].width != width || textures[i].height != height)
					{
						Debug.LogError("[TextureArrayHelper] Dimension Mismatch! " + $"Texture at index {i} ({textures[i].name}) is {textures[i].width}x{textures[i].height}, " + $"but reference texture ({textures[0].name}) is {width}x{height}.");
					}
				}
				return new Vector2Int(width, height);
			}
			int num = 0;
			for (int j = 0; j < textures.Count; j++)
			{
				num = Mathf.Max(num, Mathf.Max(textures[j].width, textures[j].height));
			}
			return new Vector2Int(num, num);
		}

		public static RenderTexture CreateTextureArray(Vector2Int resolution, int textureCount, RenderTextureFormat format, bool mips)
		{
			RenderTextureDescriptor desc = new RenderTextureDescriptor(resolution.x, resolution.y, format, 0);
			desc.dimension = TextureDimension.Tex2DArray;
			desc.volumeDepth = textureCount;
			desc.useMipMap = mips;
			desc.autoGenerateMips = false;
			RenderTexture renderTexture = new RenderTexture(desc);
			renderTexture.filterMode = FilterMode.Bilinear;
			renderTexture.wrapMode = TextureWrapMode.Clamp;
			renderTexture.Create();
			return renderTexture;
		}

		public static void BakeToTextureArray(CommandBuffer cb, IReadOnlyList<Texture2D> textures, RenderTexture targetArray)
		{
			for (int i = 0; i < textures.Count; i++)
			{
				cb.SetRenderTarget(targetArray, 0, CubemapFace.Unknown, i);
				cb.ClearRenderTarget(clearDepth: true, clearColor: true, Color.clear);
			}
			for (int j = 0; j < textures.Count; j++)
			{
				Texture2D texture2D = textures[j];
				texture2D.wrapMode = TextureWrapMode.Clamp;
				cb.Blit(texture2D, targetArray, Vector2.one, Vector2.zero, 0, j);
			}
			if (targetArray.useMipMap)
			{
				cb.GenerateMips(targetArray);
			}
		}
	}
}
