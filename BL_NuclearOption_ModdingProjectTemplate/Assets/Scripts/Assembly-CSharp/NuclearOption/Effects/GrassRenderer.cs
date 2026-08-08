using System;
using JamesFrowen.ScriptableVariables;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

namespace NuclearOption.Effects
{
	[DefaultExecutionOrder(8)]
	public class GrassRenderer : MonoBehaviour
	{
		[Serializable]
		public struct GrassConfig
		{
			[Header("Vertex Rendering")]
			public float ScaleMin;

			public float ScaleMax;

			public float Y_Scale;

			public float Y_Offset;

			[Header("Fragment Rendering")]
			public Color BaseColor;

			public uint MainTextureIndex;

			public float Cutoff;

			public float ViewRadius;

			[NonSerialized]
			private float _pad0;

			[Header("Lush/Positioning")]
			public uint LushTextureIndex;

			public float LushMultiplier;

			public float LushExponent;

			public float LushThreshold;

			public unsafe static int Size => sizeof(GrassConfig);

			public static void AssertSize()
			{
			}
		}

		private static readonly ProfilerMarker ComputeMarker = new ProfilerMarker("GrassRenderer.Compute");

		private static readonly ProfilerMarker RenderMarker = new ProfilerMarker("GrassRenderer.Render");

		private const int MAX_CONFIG_COUNT = 16;

		public DetailEnabled EnabledSetting;

		[Header("Assets")]
		[SerializeField]
		private ComputeShader grassCompute;

		[SerializeField]
		private Mesh grassMesh;

		[SerializeField]
		private Material grassMaterial;

		[Tooltip("how big is mesh for frustum culling")]
		public float meshRadius = 5f;

		[Space]
		[SerializeField]
		public DetailRenderer detailRenderer;

		[SerializeField]
		private TerrainHeightMap terrainHeightMap;

		[Header("Grass Positions")]
		public int windowSize = 1024;

		[Tooltip("what percent of windowSize to use for viewRadius. 1 = round, 1.4 = square")]
		[Range(0.9f, 1.5f)]
		public float viewRadiusEdge = 1f;

		[Tooltip("spacing of grass grid to use on compute shader, eg grass density")]
		public float grassGridSpacing = 0.5f;

		[Space]
		[Tooltip("How much grass will be moved from grid position ")]
		public float PositionJitter = 1f;

		[Space]
		public MinMax BlastThreshold = new MinMax(0.2f, 0.4f);

		[SerializeField]
		private Texture2D[] lushMaps;

		[SerializeField]
		private Texture2D[] mainTextures;

		public GrassConfig[] config;

		[Header("Dithering culling")]
		public float DitheringMinDist = 0.1f;

		public float DitheringMaxDist = 1f;

		public float DitheringFalloffPower = 2f;

		[Header("Memory")]
		[Tooltip("0.5 means we allocate for 50% of the possible grass blades. Lower = less VRAM, higher = less chance of holes.")]
		[Range(0f, 1f)]
		public float PositionBufferSizePercent = 0.5f;

		[Tooltip("0.5 means we allocate for 50% of the possible grass blades. Lower = less VRAM, higher = less chance of holes.")]
		[Range(0f, 1f)]
		public float VisibleBufferSizePercent = 0.1f;

		[Header("Runtime Textures")]
		[ReadOnly]
		[SerializeField]
		private RenderTexture lushMapArray;

		[ReadOnly]
		[SerializeField]
		private RenderTexture mainTextureArray;

		private GraphicsBuffer grassPositionBuffer;

		private GraphicsBuffer configBuffer;

		private GraphicsBuffer visiblePositionBuffer;

		public MaterialPropertyBlock grassMaterialProps;

		private bool _configIsDirty;

		public bool IsActive { get; private set; }

		public uint ArgIndex { get; set; }

		public Texture2D LushMapIndex0 => lushMaps[0];

		private void OnValidate()
		{
			GrassConfig.AssertSize();
			if (detailRenderer != null && detailRenderer.windowSize != windowSize)
			{
				windowSize = detailRenderer.windowSize;
			}
			_configIsDirty = true;
			if (config.Length > 16)
			{
				Debug.LogError($"Max config count is {16}");
				Array.Resize(ref config, 16);
			}
			if (lushMaps.Length != 0)
			{
				TextureArrayHelper.GetMaxDimensions(lushMaps, TextureArrayHelper.DimensionsMode.AssertAllSame);
			}
			AssertTexturedAreUsed();
			for (int i = 0; i < mainTextures.Length; i++)
			{
				if (mainTextures[i].wrapMode != TextureWrapMode.Clamp)
				{
					Debug.LogWarning($"[GrassRenderer] Main Texture at index {i} ({mainTextures[i].name}) is not using TextureWrapMode.Clamp");
				}
			}
		}

		private void AssertTexturedAreUsed()
		{
			Span<bool> span = stackalloc bool[lushMaps.Length];
			Span<bool> span2 = stackalloc bool[mainTextures.Length];
			GrassConfig[] array = config;
			for (int i = 0; i < array.Length; i++)
			{
				GrassConfig grassConfig = array[i];
				if (lushMaps.Length != 0)
				{
					span[(int)grassConfig.LushTextureIndex] = true;
				}
				span2[(int)grassConfig.MainTextureIndex] = true;
			}
			for (int j = 0; j < span.Length; j++)
			{
				if (!span[j])
				{
					Debug.LogError($"[GrassRenderer] Lush Map at index {j} ({lushMaps[j].name}) is not used by any GrassConfig!");
				}
			}
			for (int k = 0; k < span2.Length; k++)
			{
				if (!span2[k])
				{
					Debug.LogError($"[GrassRenderer] Main Texture at index {k} ({mainTextures[k].name}) is not used by any GrassConfig!");
				}
			}
		}

		private void UpdateConfigBuffer()
		{
			_configIsDirty = false;
			if (config != null && config.Length != 0)
			{
				if (configBuffer == null || configBuffer.count != config.Length)
				{
					configBuffer?.Release();
					configBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, config.Length, GrassConfig.Size);
					grassMaterialProps.SetBuffer(ShaderIds.ID_ConfigBuffer, configBuffer);
				}
				configBuffer.SetData(config);
			}
		}

		public void CommandSetup(DetailRenderer.RendererOffsets argHelper)
		{
			IsActive = true;
			CalculateMaxGrass(out var sampleCount, out var positionBufferSize, out var indexBufferSize);
			grassPositionBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured | GraphicsBuffer.Target.Append, positionBufferSize, 16);
			grassPositionBuffer.name = "GrassPositionBuffer";
			visiblePositionBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured | GraphicsBuffer.Target.Append, indexBufferSize, 16);
			visiblePositionBuffer.name = "grassVisiblePositionBuffer";
			argHelper.Set(argHelper.GroupCount_1().Stride, default(GroupCount));
			argHelper.Set(argHelper.DrawArgs().Stride, new GraphicsBuffer.IndirectDrawIndexedArgs
			{
				indexCountPerInstance = grassMesh.GetIndexCount(0),
				instanceCount = 0u,
				baseVertexIndex = grassMesh.GetBaseVertex(0),
				startIndex = grassMesh.GetIndexStart(0),
				startInstance = 0u
			});
			AssertTexturedAreUsed();
			Texture2D[] array = lushMaps;
			for (sampleCount = 0; sampleCount < array.Length; sampleCount++)
			{
				_ = array[sampleCount];
			}
			Vector2Int maxDimensions = TextureArrayHelper.GetMaxDimensions(lushMaps, TextureArrayHelper.DimensionsMode.AssertAllSame);
			lushMapArray = TextureArrayHelper.CreateTextureArray(maxDimensions, lushMaps.Length, RenderTextureFormat.R8, mips: false);
			array = mainTextures;
			for (sampleCount = 0; sampleCount < array.Length; sampleCount++)
			{
				_ = array[sampleCount];
			}
			Vector2Int maxDimensions2 = TextureArrayHelper.GetMaxDimensions(mainTextures, TextureArrayHelper.DimensionsMode.ForceSquare);
			mainTextureArray = TextureArrayHelper.CreateTextureArray(maxDimensions2, mainTextures.Length, RenderTextureFormat.ARGB32, mips: true);
			CommandBuffer commandBuffer = new CommandBuffer();
			TextureArrayHelper.BakeToTextureArray(commandBuffer, lushMaps, lushMapArray);
			TextureArrayHelper.BakeToTextureArray(commandBuffer, mainTextures, mainTextureArray);
			Graphics.ExecuteCommandBuffer(commandBuffer);
			commandBuffer.Release();
			configBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, config.Length, GrassConfig.Size);
			configBuffer.name = "GrassConfigBuffer";
			configBuffer.SetData(config);
			grassMaterialProps = new MaterialPropertyBlock();
			grassMaterialProps.SetBuffer(ShaderIds.ID_VisibleBuffer, visiblePositionBuffer);
			grassMaterialProps.SetBuffer(ShaderIds.ID_ConfigBuffer, configBuffer);
			grassMaterialProps.SetTexture(ShaderIds.ID_MainTex, mainTextureArray);
			grassMaterialProps.SetTexture(ShaderIds.ID_TerrainHeightMap, terrainHeightMap.normalMap);
		}

		public void CommandRelease()
		{
			IsActive = false;
			grassPositionBuffer?.Release();
			visiblePositionBuffer?.Release();
			configBuffer?.Release();
			if (lushMapArray != null)
			{
				lushMapArray.Release();
				UnityEngine.Object.Destroy(lushMapArray);
			}
			if (mainTextureArray != null)
			{
				mainTextureArray.Release();
				UnityEngine.Object.Destroy(mainTextureArray);
			}
		}

		private void OnDestroy()
		{
			CommandRelease();
		}

		private void CalculateGrassGroups(out int groups)
		{
			groups = Mathf.CeilToInt((float)windowSize / grassGridSpacing / 8f);
		}

		public void CalculateMaxGrass(out int sampleCount, out int positionBufferSize, out int indexBufferSize)
		{
			CalculateGrassGroups(out var groups);
			checked
			{
				int num = groups * 8;
				sampleCount = num * num;
				positionBufferSize = Mathf.Max(1024, Mathf.CeilToInt((float)sampleCount * PositionBufferSizePercent));
				indexBufferSize = Mathf.Max(1024, Mathf.CeilToInt((float)sampleCount * VisibleBufferSizePercent));
			}
		}

		public void UpdatePositions(CommandBuffer cmd, DetailRenderer.RendererOffsets argHelper, bool windowMoved, Vector2Int windowIndex)
		{
			cmd.BeginSample("GrassRenderer");
			using (ComputeMarker.Auto())
			{
				if (windowMoved)
				{
					cmd.BeginSample("GrassRenderer.DispatchGrassCompute");
					int kernelIndex = grassCompute.FindKernel("CSMain");
					cmd.SetBufferCounterValue(grassPositionBuffer, 0u);
					cmd.SetComputeBufferParam(grassCompute, kernelIndex, ShaderIds.ID_PositionBuffer, grassPositionBuffer);
					cmd.SetComputeTextureParam(grassCompute, kernelIndex, ShaderIds.ID_HeightMap, terrainHeightMap.heightMap);
					cmd.SetComputeTextureParam(grassCompute, kernelIndex, ShaderIds.ID_BlockerMap, terrainHeightMap.blockerMap);
					cmd.SetComputeBufferParam(grassCompute, kernelIndex, ShaderIds.ID_ConfigBuffer, configBuffer);
					cmd.SetComputeIntParam(grassCompute, ShaderIds.ID_ConfigBufferCount, config.Length);
					cmd.SetComputeTextureParam(grassCompute, kernelIndex, ShaderIds.ID_LushMapArray, lushMapArray);
					cmd.SetComputeVectorParam(grassCompute, ShaderIds.ID_Height, terrainHeightMap.height.ToMinRange());
					cmd.SetComputeVectorParam(grassCompute, ShaderIds.ID_BlastThreshold, BlastThreshold.ToMinInvRange());
					cmd.SetComputeFloatParam(grassCompute, ShaderIds.ID_PositionJitter, PositionJitter);
					cmd.SetComputeFloatParam(grassCompute, ShaderIds.ID_DitheringMinDist, DitheringMinDist);
					cmd.SetComputeFloatParam(grassCompute, ShaderIds.ID_DitheringMaxDist, DitheringMaxDist);
					cmd.SetComputeFloatParam(grassCompute, ShaderIds.ID_DitheringFalloffPowers, DitheringFalloffPower);
					cmd.SetComputeIntParam(grassCompute, ShaderIds.ID_WindowSize, detailRenderer.windowSize);
					cmd.SetComputeIntParam(grassCompute, ShaderIds.ID_WindowSnapping, detailRenderer.windowSnapping);
					cmd.SetComputeFloatParam(grassCompute, ShaderIds.ID_MeshRadius, meshRadius);
					cmd.SetComputeIntParam(grassCompute, ShaderIds.ID_MillimetersPerCell, Mathf.RoundToInt(grassGridSpacing * 1000f));
					cmd.SetComputeVectorParam(grassCompute, ShaderIds.ID_WindowIndex, new Vector4(windowIndex.x, windowIndex.y, 0f, 0f));
					CalculateGrassGroups(out var groups);
					cmd.DispatchCompute(grassCompute, kernelIndex, groups, groups, 1);
					cmd.CopyCounterValue(grassPositionBuffer, argHelper.Buffer, argHelper.GroupCount_1().Total().Byte);
					cmd.EndSample("GrassRenderer.DispatchGrassCompute");
					ComputeGroupCount.Run(cmd, argHelper.GroupCount_1());
				}
				ComputeFrustumCulling.RunCull(cmd, ComputeFrustumCulling.RunFlags.UseFloat4, argHelper.GroupCount_1(), grassPositionBuffer, visiblePositionBuffer, meshRadius, (float)detailRenderer.windowSize * viewRadiusEdge);
				cmd.CopyCounterValue(visiblePositionBuffer, argHelper.Buffer, argHelper.DrawArgs().InstanceCount().Byte);
				cmd.EndSample("GrassRenderer");
			}
		}

		public void Render(DetailRenderer.RendererOffsets argHelper)
		{
			using (RenderMarker.Auto())
			{
				Bounds worldBounds = new Bounds(Vector3.zero, 10 * detailRenderer.windowSize * Vector3.one);
				RenderParams renderParams = new RenderParams(grassMaterial);
				renderParams.matProps = grassMaterialProps;
				renderParams.worldBounds = worldBounds;
				renderParams.shadowCastingMode = ShadowCastingMode.Off;
				renderParams.receiveShadows = true;
				RenderParams rparams = renderParams;
				Graphics.RenderMeshIndirect(in rparams, grassMesh, argHelper.Buffer, 1, (int)argHelper.DrawArgs().Stride);
			}
		}
	}
}
