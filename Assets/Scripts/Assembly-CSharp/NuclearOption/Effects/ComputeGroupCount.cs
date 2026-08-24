using UnityEngine;
using UnityEngine.Rendering;

namespace NuclearOption.Effects
{
	public static class ComputeGroupCount
	{
		private static bool HasKernelId;

		private static int kernel;

		private static ComputeShader compute => GameAssets.i.GroupCountCompute;

		private static void GetKernelIds()
		{
			kernel = compute.FindKernel(ShaderIds.Kernel_Main);
			HasKernelId = true;
		}

		public static void Run(CommandBuffer cmd, DetailRenderer.GroupCountOffsets argOffsets)
		{
			if (!HasKernelId)
			{
				GetKernelIds();
			}
			cmd.BeginSample("FrustumCulling.GroupCount");
			cmd.SetCountBuffer(compute, kernel, argOffsets);
			cmd.DispatchCompute(compute, kernel, 1, 1, 1);
			cmd.EndSample("FrustumCulling.GroupCount");
		}
	}
}
