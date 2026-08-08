using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

namespace NuclearOption.Effects
{
	public class BlastManager : SceneSingleton<BlastManager>
	{
		public struct DetailBlast
		{
			public GlobalPosition Position;

			public float Radius;

			public DetailBlast(GlobalPosition position, float radius)
			{
				Position = position;
				Radius = radius;
			}

			public readonly Vector4 ToVector4()
			{
				return new Vector4(Position.x, Position.y, Position.z, Radius);
			}

			public override readonly string ToString()
			{
				return $"({Position}, {Radius}m)";
			}
		}

		private static readonly ProfilerMarker AddBlastMarker = new ProfilerMarker("BlastManager.AddBlast");

		[SerializeField]
		public MapSettings mapSettings;

		[SerializeField]
		private Mesh quadMesh;

		[SerializeField]
		private Shader blastShader;

		[SerializeField]
		public float worldSizeToResolution = 160f;

		[Tooltip("How much to multiply BlastRadius by to draw extra blast onto the texture, draws based on 1/r^2")]
		[SerializeField]
		private float radiusMultiplier = 2f;

		private Material drawMaterial;

		private Vector2 mapSize;

		public readonly Queue<DetailBlast> Blasts = new Queue<DetailBlast>();

		public RenderTexture Texture { get; private set; }

		public void CommandSetup()
		{
			drawMaterial = new Material(blastShader);
			mapSize = mapSettings.MapSize;
			Vector2Int vector2Int = CaculateResolution(mapSettings.MapSize, worldSizeToResolution);
			Texture = new RenderTexture(vector2Int.x, vector2Int.y, 0, RenderTextureFormat.R8);
			Texture.filterMode = FilterMode.Bilinear;
			Texture.wrapMode = TextureWrapMode.Clamp;
			Texture.Create();
			ClearMap();
			Shader.SetGlobalTexture(ShaderGlobalManager.ID_Global_BlastMap, Texture);
		}

		public static Vector2Int CaculateResolution(Vector2 mapWorldSize, float sizeToResolution)
		{
			return new Vector2Int
			{
				x = Mathf.NextPowerOfTwo(Mathf.RoundToInt(mapWorldSize.x / sizeToResolution)),
				y = Mathf.NextPowerOfTwo(Mathf.RoundToInt(mapWorldSize.y / sizeToResolution))
			};
		}

		public void OnDestroy()
		{
			if (Texture != null)
			{
				Texture.Release();
				Object.Destroy(Texture);
				Texture = null;
			}
			if (drawMaterial != null)
			{
				Object.Destroy(drawMaterial);
				drawMaterial = null;
			}
		}

		public void ClearMap()
		{
			CommandBuffer commandBuffer = CommandBufferPool.Get("ClearBlastMap");
			commandBuffer.SetRenderTarget(Texture);
			commandBuffer.ClearRenderTarget(clearDepth: true, clearColor: true, Color.black);
			Graphics.ExecuteCommandBuffer(commandBuffer);
			CommandBufferPool.Release(commandBuffer);
		}

		public void AddBlast(GlobalPosition position, float radius)
		{
			DetailBlast detailBlast = new DetailBlast(position, radius * radiusMultiplier);
			ColorLog<BlastManager>.Info($"Queuing blast {detailBlast}");
			Blasts.Enqueue(detailBlast);
		}

		public void DrawBlast(CommandBuffer cmd, DetailBlast blast)
		{
			GlobalPosition position = blast.Position;
			float radius = blast.Radius;
			float num = worldSizeToResolution / 2f;
			if (radius < num)
			{
				return;
			}
			using (AddBlastMarker.Auto())
			{
				ColorLog<BlastManager>.Info($"Drawing blast of radius {radius}");
				float x = (position.x + mapSize.x / 2f) / mapSize.x;
				float y = (position.z + mapSize.y / 2f) / mapSize.y;
				float num2 = radius / mapSize.x;
				float num3 = radius / mapSize.y;
				Matrix4x4 matrix = Matrix4x4.TRS(new Vector3(x, y, 0f), s: new Vector3(num2 * 2f, num3 * 2f, 1f), q: Quaternion.identity);
				cmd.BeginSample("BlastManager.AddBlast");
				cmd.SetRenderTarget(Texture);
				cmd.SetProjectionMatrix(Matrix4x4.Ortho(0f, 1f, 0f, 1f, -1f, 1f));
				cmd.SetViewMatrix(Matrix4x4.identity);
				cmd.DrawMesh(quadMesh, matrix, drawMaterial);
				cmd.EndSample("BlastManager.AddBlast");
			}
		}
	}
}
