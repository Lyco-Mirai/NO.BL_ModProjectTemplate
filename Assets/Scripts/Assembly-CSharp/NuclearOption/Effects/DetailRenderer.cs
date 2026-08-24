using System.Text;
using NuclearOption.Networking;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

namespace NuclearOption.Effects
{
	[DefaultExecutionOrder(8)]
	public class DetailRenderer : SceneSingleton<DetailRenderer>
	{
		public struct ArgBuffer
		{
			private readonly uint[] array;

			public readonly GraphicsBuffer buffer;

			public ArgBuffer(uint[] array, GraphicsBuffer buffer)
			{
				this.array = array;
				this.buffer = buffer;
			}

			public RendererOffsets GetRenderer(uint renderer)
			{
				return new RendererOffsets(renderer * 3, array, buffer);
			}

			public (uint positionCount, uint visibleCount) GetPreviousInstanceCount(uint renderer)
			{
				return GetRenderer(renderer).GetPreviousInstanceCount();
			}
		}

		public readonly struct RendererOffsets
		{
			public const int STRIDE_PER_RENDERER = 3;

			public const int INT_STRIDE = 5;

			public const int BYTE_STRIDE = 20;

			private readonly uint startStride;

			private readonly uint[] array;

			public readonly GraphicsBuffer Buffer;

			public unsafe T Get<T>(uint strideIndex) where T : unmanaged
			{
				uint num = strideIndex * 5;
				fixed (uint* ptr = &array[num])
				{
					T* ptr2 = (T*)ptr;
					return *ptr2;
				}
			}

			public unsafe void Set<T>(uint strideIndex, T value) where T : unmanaged
			{
				uint num = strideIndex * 5;
				fixed (uint* ptr = &array[num])
				{
					T* ptr2 = (T*)ptr;
					T* ptr3 = &value;
					*ptr2 = *ptr3;
				}
			}

			public RendererOffsets(uint startStride, uint[] array, GraphicsBuffer buffer)
			{
				this.startStride = startStride;
				this.array = array;
				Buffer = buffer;
			}

			public (uint positionCount, uint visibleCount) GetPreviousInstanceCount()
			{
				GroupCount groupCount = Get<GroupCount>(GroupCount_1().Stride);
				GraphicsBuffer.IndirectDrawIndexedArgs indirectDrawIndexedArgs = Get<GraphicsBuffer.IndirectDrawIndexedArgs>(DrawArgs().Stride);
				return (positionCount: groupCount.total, visibleCount: indirectDrawIndexedArgs.instanceCount);
			}

			public GroupCountOffsets GroupCount_1()
			{
				return new GroupCountOffsets(Buffer, startStride);
			}

			public GroupCountOffsets GroupCount_2()
			{
				return new GroupCountOffsets(Buffer, startStride + 1);
			}

			public DrawArgsOffsets DrawArgs()
			{
				return new DrawArgsOffsets(Buffer, startStride + 2);
			}
		}

		public readonly struct GroupCountOffsets
		{
			public readonly GraphicsBuffer Buffer;

			public readonly uint Stride;

			public uint Uint => Stride * 5;

			public uint Byte => Uint * 4;

			public GroupCountOffsets(GraphicsBuffer buffer, uint stride)
			{
				Buffer = buffer;
				Stride = stride;
			}

			public Offset Total()
			{
				return new Offset(Uint + 3);
			}
		}

		public readonly struct DrawArgsOffsets
		{
			public readonly GraphicsBuffer Buffer;

			public readonly uint Stride;

			public uint Uint => Stride * 5;

			public uint Byte => Uint * 4;

			public DrawArgsOffsets(GraphicsBuffer buffer, uint stride)
			{
				Buffer = buffer;
				Stride = stride;
			}

			public Offset InstanceCount()
			{
				return new Offset(Uint + 1);
			}
		}

		public readonly struct Offset
		{
			public readonly uint Uint;

			public uint Byte => Uint * 4;

			public Offset(uint @uint)
			{
				Uint = @uint;
			}
		}

		private static readonly ProfilerMarker SetupMarker = new ProfilerMarker("DetailRenderer.Setup");

		public static readonly ProfilerMarker LateUpdateMarker = new ProfilerMarker("DetailRenderer.LateUpdate");

		private static readonly ProfilerMarker ComputeMarker = new ProfilerMarker("DetailRenderer.Compute");

		private static readonly ProfilerMarker ComputeQueueMarker = new ProfilerMarker("DetailRenderer.Compute.Queue");

		private static readonly ProfilerMarker ComputeDispatchMarker = new ProfilerMarker("DetailRenderer.Compute.Dispatch");

		private static readonly ProfilerMarker RenderMarker = new ProfilerMarker("DetailRenderer.Render");

		public static bool DatumShifted;

		[Tooltip("Snap window to nearest unit, rather than rebaking every frame")]
		public int windowSnapping = 64;

		[Tooltip("Size of the sliding window around the camera to use for the height map and min/max grass bounds")]
		public int windowSize = 1024;

		[SerializeField]
		private BlastManager blastManager;

		[SerializeField]
		private TerrainHeightMap terrainHeight;

		[SerializeField]
		private GrassRenderer[] grassRenderers;

		[SerializeField]
		private TreeRenderer[] treeRenderers;

		[Header("Debug")]
		public bool DebugAlwaysBakeHeight;

		public bool DebugDrawWindowBounds;

		public bool DebugLogCounts;

		private CommandBuffer cmd;

		private GraphicsBuffer argBuffer;

		private uint[] argArray;

		private ArgBuffer argHelper;

		private Camera camera;

		private StringBuilder LogCountsBuilder;

		private Vector2Int windowIndex;

		public Vector2Int WindowIndex => windowIndex;

		public static bool IsSupported()
		{
			return SystemInfo.graphicsShaderLevel >= 45;
		}

		protected override void Awake()
		{
			base.Awake();
			bool flag = !IsSupported();
			if (GameManager.IsHeadless || flag)
			{
				if (flag)
				{
					ColorLog<DetailRenderer>.InfoWarn("GPU does not support Target 4.5. Disabling Grass and other detail");
				}
				base.gameObject.SetActive(value: false);
			}
		}

		private void Start()
		{
			if (GameManager.IsHeadless || !IsSupported())
			{
				return;
			}
			using (SetupMarker.Auto())
			{
				camera = Camera.main;
				cmd = new CommandBuffer();
				int num = (grassRenderers.Length + treeRenderers.Length) * 3;
				argBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured | GraphicsBuffer.Target.IndirectArguments, num, 20);
				argBuffer.name = "ArgBuffer";
				argArray = new uint[num * 5];
				argHelper = new ArgBuffer(argArray, argBuffer);
				blastManager.CommandSetup();
				uint num2 = 0u;
				GrassRenderer[] array = grassRenderers;
				for (int i = 0; i < array.Length; i++)
				{
					array[i].ArgIndex = num2++;
				}
				TreeRenderer[] array2 = treeRenderers;
				for (int i = 0; i < array2.Length; i++)
				{
					array2[i].ArgIndex = num2++;
				}
				CheckSettings();
				array2 = treeRenderers;
				foreach (TreeRenderer treeRenderer in array2)
				{
					treeRenderer.CommandSetup(argHelper.GetRenderer(treeRenderer.ArgIndex));
				}
				argBuffer.SetData(argArray);
				windowIndex = new Vector2Int(int.MaxValue, int.MaxValue);
			}
		}

		private void OnDestroy()
		{
			cmd?.Dispose();
			argBuffer?.Release();
		}

		private void LateUpdate()
		{
			if (GameManager.IsHeadless || !IsSupported() || NetworkManagerNuclearOption.IsLoadingScene)
			{
				return;
			}
			using (LateUpdateMarker.Auto())
			{
				if (DebugLogCounts)
				{
					LogCounts();
				}
				if (DebugDrawWindowBounds)
				{
					DrawWindowRays();
				}
				CheckSettings();
				int num = Mathf.Max(0, windowSize / 2 - windowSnapping * 2);
				ShaderGlobalManager.SetCameraPlanes(camera, num, out var cameraTarget);
				bool flag = WindowMoved(cameraTarget.ToGlobalPosition()) || DatumShifted || terrainHeight.IsDirty || DebugAlwaysBakeHeight;
				using (ComputeMarker.Auto())
				{
					using (ComputeQueueMarker.Auto())
					{
						cmd.Clear();
						cmd.BeginSample("DetailRenderer");
						BlastManager.DetailBlast result;
						TreeRenderer[] array;
						while (blastManager.Blasts.TryDequeue(out result))
						{
							flag = true;
							blastManager.DrawBlast(cmd, result);
							array = treeRenderers;
							foreach (TreeRenderer treeRenderer in array)
							{
								treeRenderer.ClearTrees(cmd, argHelper.GetRenderer(treeRenderer.ArgIndex), result);
							}
						}
						if (terrainHeight.IsActive)
						{
							if (flag)
							{
								terrainHeight.BakeWindow(cmd, windowIndex);
								DatumShifted = false;
							}
							GrassRenderer[] array2 = grassRenderers;
							foreach (GrassRenderer grassRenderer in array2)
							{
								if (grassRenderer.IsActive)
								{
									grassRenderer.UpdatePositions(cmd, argHelper.GetRenderer(grassRenderer.ArgIndex), flag, windowIndex);
								}
							}
						}
						array = treeRenderers;
						foreach (TreeRenderer treeRenderer2 in array)
						{
							treeRenderer2.UpdatePositions(cmd, argHelper.GetRenderer(treeRenderer2.ArgIndex));
						}
						cmd.EndSample("DetailRenderer");
					}
					using (ComputeDispatchMarker.Auto())
					{
						Graphics.ExecuteCommandBuffer(cmd);
					}
				}
				using (RenderMarker.Auto())
				{
					GrassRenderer[] array2 = grassRenderers;
					foreach (GrassRenderer grassRenderer2 in array2)
					{
						if (grassRenderer2.IsActive)
						{
							if (flag)
							{
								WindowData windowData = new WindowData
								{
									index = windowIndex,
									size = windowSize,
									snapping = windowSnapping
								};
								grassRenderer2.grassMaterialProps.SetVector(ShaderIds.ID_WindowData, windowData.ToVector4());
							}
							grassRenderer2.Render(argHelper.GetRenderer(grassRenderer2.ArgIndex));
						}
					}
					TreeRenderer[] array = treeRenderers;
					foreach (TreeRenderer treeRenderer3 in array)
					{
						treeRenderer3.Render(argHelper.GetRenderer(treeRenderer3.ArgIndex));
					}
				}
			}
		}

		private void CheckSettings()
		{
			bool anyGrassShouldBeActive = false;
			GrassRenderer[] array = grassRenderers;
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i].EnabledSetting switch
				{
					DetailEnabled.Disable => false, 
					DetailEnabled.UsePlayerSettings => PlayerSettings.DetailSettings.GrassEnabled, 
					_ => true, 
				})
				{
					anyGrassShouldBeActive = true;
					break;
				}
			}
			CheckTerrainHeightActive(anyGrassShouldBeActive);
			array = grassRenderers;
			foreach (GrassRenderer grassRenderer in array)
			{
				bool flag = grassRenderer.EnabledSetting switch
				{
					DetailEnabled.Disable => false, 
					DetailEnabled.UsePlayerSettings => PlayerSettings.DetailSettings.GrassEnabled, 
					_ => true, 
				};
				if (flag)
				{
					anyGrassShouldBeActive = true;
				}
				CheckGrassActive(grassRenderer, flag);
			}
			TreeRenderer[] array2 = treeRenderers;
			for (int i = 0; i < array2.Length; i++)
			{
				array2[i].TreeRangeMultiplier = PlayerSettings.DetailSettings.TreeRangeMultiplier;
			}
		}

		private void CheckTerrainHeightActive(bool anyGrassShouldBeActive)
		{
			if (anyGrassShouldBeActive != terrainHeight.IsActive)
			{
				if (anyGrassShouldBeActive)
				{
					terrainHeight.CommandSetup();
				}
				else
				{
					terrainHeight.CommandRelease();
				}
			}
		}

		private void CheckGrassActive(GrassRenderer grass, bool grassShouldBeActive)
		{
			if (grassShouldBeActive != grass.IsActive)
			{
				if (grassShouldBeActive)
				{
					grass.CommandSetup(argHelper.GetRenderer(grass.ArgIndex));
				}
				else
				{
					grass.CommandRelease();
				}
			}
		}

		private void LogCounts()
		{
			if (LogCountsBuilder == null)
			{
				LogCountsBuilder = new StringBuilder();
			}
			LogCountsBuilder.Clear();
			argBuffer.GetData(argArray);
			GrassRenderer[] array = grassRenderers;
			foreach (GrassRenderer grassRenderer in array)
			{
				var (num, num2) = argHelper.GetPreviousInstanceCount(grassRenderer.ArgIndex);
				LogCountsBuilder.Append($"Grass[{grassRenderer.ArgIndex}]: {num2}/{num}, ");
			}
			TreeRenderer[] array2 = treeRenderers;
			foreach (TreeRenderer treeRenderer in array2)
			{
				var (num3, num4) = argHelper.GetPreviousInstanceCount(treeRenderer.ArgIndex);
				LogCountsBuilder.Append($"Tree[{treeRenderer.ArgIndex}]: {num4}/{num3}, ");
			}
			if (LogCountsBuilder.Length > 2)
			{
				LogCountsBuilder.Length -= 2;
			}
		}

		private bool WindowMoved(GlobalPosition cameraPos)
		{
			Vector2Int vector2Int = new Vector2Int(Mathf.FloorToInt(cameraPos.x / (float)windowSnapping), Mathf.FloorToInt(cameraPos.z / (float)windowSnapping));
			if (vector2Int != windowIndex)
			{
				windowIndex = vector2Int;
				return true;
			}
			return false;
		}

		private void DrawWindowRays()
		{
			Vector3 vector = new GlobalPosition(windowIndex.x * windowSnapping, 0f, windowIndex.y * windowSnapping).ToLocalPosition();
			Debug.DrawRay(vector + new Vector3(windowSize / 2, 0f, windowSize / 2) + Vector3.down * 1000f, Vector3.up * 5000f, Color.blue);
			Debug.DrawRay(vector + new Vector3(-windowSize / 2, 0f, windowSize / 2) + Vector3.down * 1000f, Vector3.up * 5000f, Color.blue);
			Debug.DrawRay(vector + new Vector3(-windowSize / 2, 0f, -windowSize / 2) + Vector3.down * 1000f, Vector3.up * 5000f, Color.blue);
			Debug.DrawRay(vector + new Vector3(windowSize / 2, 0f, -windowSize / 2) + Vector3.down * 1000f, Vector3.up * 5000f, Color.blue);
		}
	}
}
