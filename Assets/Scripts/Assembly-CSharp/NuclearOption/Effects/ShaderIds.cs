using UnityEngine;
using UnityEngine.Rendering;

namespace NuclearOption.Effects
{
	public static class ShaderIds
	{
		public static readonly string Kernel_Main = "CSMain";

		public static readonly int ID_Height = Shader.PropertyToID("_Height");

		public static readonly int ID_BlockerHeightThreshold = Shader.PropertyToID("_BlockerHeightThreshold");

		public static readonly int ID_BlockerPadding = Shader.PropertyToID("_BlockerPadding");

		public static readonly int ID_BlastThreshold = Shader.PropertyToID("_BlastThreshold");

		public static readonly int ID_PositionJitter = Shader.PropertyToID("_PositionJitter");

		public static readonly int ID_WindowData = Shader.PropertyToID("_WindowData");

		public static readonly int ID_WindowSize = Shader.PropertyToID("_WindowSize");

		public static readonly int ID_WindowIndex = Shader.PropertyToID("_WindowIndex");

		public static readonly int ID_WindowSnapping = Shader.PropertyToID("_WindowSnapping");

		public static readonly int ID_ViewRadius = Shader.PropertyToID("_ViewRadius");

		public static readonly int ID_MeshRadius = Shader.PropertyToID("_MeshRadius");

		public static readonly int ID_MillimetersPerCell = Shader.PropertyToID("_MillimetersPerCell");

		public static readonly int ID_PositionBuffer = Shader.PropertyToID("_PositionBuffer");

		public static readonly int ID_CountBuffer = Shader.PropertyToID("_CountBuffer");

		public static readonly int ID_CountBufferOffset = Shader.PropertyToID("_CountBufferOffset");

		public static readonly int ID_VisibleBuffer = Shader.PropertyToID("_VisibleBuffer");

		public static readonly int ID_ColorDataBuffer = Shader.PropertyToID("_ColorDataBuffer");

		public static readonly int ID_HeightMap = Shader.PropertyToID("_HeightMap");

		public static readonly int ID_TerrainHeightMap = Shader.PropertyToID("_TerrainHeightMap");

		public static readonly int ID_BlockerMap = Shader.PropertyToID("_BlockerMap");

		public static readonly int ID_DebugMap = Shader.PropertyToID("_DebugMap");

		public static readonly int ID_LushMap = Shader.PropertyToID("_LushMap");

		public static readonly int ID_LushMapArray = Shader.PropertyToID("_LushMapArray");

		public static readonly int ID_LushMapArrayCount = Shader.PropertyToID("_LushMapArrayCount");

		public static readonly int ID_AltitudeRange = Shader.PropertyToID("_AltitudeRange");

		public static readonly int ID_AltIntensity = Shader.PropertyToID("_AltIntensity");

		public static readonly int ID_LushIntensity = Shader.PropertyToID("_LushIntensity");

		public static readonly int ID_LargeNoiseScale = Shader.PropertyToID("_LargeNoiseScale");

		public static readonly int ID_NoiseSmallScale = Shader.PropertyToID("_NoiseSmallScale");

		public static readonly int ID_BlastIntensity = Shader.PropertyToID("_BlastIntensity");

		public static readonly int ID_DetailBlast = Shader.PropertyToID("_DetailBlast");

		public static readonly int ID_DitheringMinDist = Shader.PropertyToID("_DitheringMinDist");

		public static readonly int ID_DitheringMaxDist = Shader.PropertyToID("_DitheringMaxDist");

		public static readonly int ID_DitheringFalloffPowers = Shader.PropertyToID("_DitheringFalloffPowers");

		public static readonly int ID_ConfigBuffer = Shader.PropertyToID("_ConfigBuffer");

		public static readonly int ID_ConfigBufferCount = Shader.PropertyToID("_ConfigBufferCount");

		public static readonly int ID_MainTex = Shader.PropertyToID("_MainTex");

		public static void SetCountBuffer(this CommandBuffer cmd, ComputeShader computeShader, int kernelIndex, DetailRenderer.GroupCountOffsets group)
		{
			cmd.SetComputeBufferParam(computeShader, kernelIndex, ID_CountBuffer, group.Buffer);
			cmd.SetComputeIntParam(computeShader, ID_CountBufferOffset, (int)group.Uint);
		}

		public static void ToggleKeyword(this CommandBuffer cmd, ComputeShader computeShader, bool enabled, LocalKeyword keyword)
		{
			if (enabled)
			{
				cmd.EnableKeyword(computeShader, in keyword);
			}
			else
			{
				cmd.DisableKeyword(computeShader, in keyword);
			}
		}
	}
}
