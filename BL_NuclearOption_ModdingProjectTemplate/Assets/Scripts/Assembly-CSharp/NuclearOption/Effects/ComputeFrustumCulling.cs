using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace NuclearOption.Effects
{
	public static class ComputeFrustumCulling
	{
		[Flags]
		public enum RunFlags
		{
			None = 0,
			UseIndex = 1,
			UseFloat4 = 2,
			UseGlobal = 4
		}

		private static bool hasKernelId;

		private static int kernel;

		private static LocalKeyword keyword_float4;

		private static LocalKeyword keyword_global;

		private static LocalKeyword keyword_toIndex;

		private static ComputeShader Compute => GameAssets.i.FrustumCullingCompute;

		private static void GetKernelIds()
		{
			kernel = Compute.FindKernel(ShaderIds.Kernel_Main);
			keyword_float4 = new LocalKeyword(Compute, "FRUSTUM_CULLING_FLOAT4");
			keyword_global = new LocalKeyword(Compute, "FRUSTUM_CULLING_GLOBAL");
			keyword_toIndex = new LocalKeyword(Compute, "FRUSTUM_CULLING_TO_INDEX");
			hasKernelId = true;
		}

		public static void RunCull(CommandBuffer cmd, RunFlags runFlags, DetailRenderer.GroupCountOffsets argOffsets, GraphicsBuffer allPositionBuffer, GraphicsBuffer visibleBuffer, float meshRadius, float viewRadius)
		{
			cmd.BeginSample("FrustumCulling");
			if (!hasKernelId)
			{
				GetKernelIds();
			}
			bool enabled = (runFlags & RunFlags.UseIndex) != 0;
			bool enabled2 = (runFlags & RunFlags.UseFloat4) != 0;
			bool enabled3 = (runFlags & RunFlags.UseGlobal) != 0;
			cmd.ToggleKeyword(Compute, enabled, keyword_toIndex);
			cmd.ToggleKeyword(Compute, enabled2, keyword_float4);
			cmd.ToggleKeyword(Compute, enabled3, keyword_global);
			cmd.SetBufferCounterValue(visibleBuffer, 0u);
			cmd.SetComputeBufferParam(Compute, kernel, ShaderIds.ID_PositionBuffer, allPositionBuffer);
			cmd.SetComputeBufferParam(Compute, kernel, ShaderIds.ID_VisibleBuffer, visibleBuffer);
			cmd.SetCountBuffer(Compute, kernel, argOffsets);
			cmd.SetComputeFloatParam(Compute, ShaderIds.ID_MeshRadius, meshRadius);
			cmd.SetComputeFloatParam(Compute, ShaderIds.ID_ViewRadius, viewRadius);
			cmd.DispatchCompute(Compute, kernel, argOffsets.Buffer, argOffsets.Byte);
			cmd.EndSample("FrustumCulling");
		}
	}
}
