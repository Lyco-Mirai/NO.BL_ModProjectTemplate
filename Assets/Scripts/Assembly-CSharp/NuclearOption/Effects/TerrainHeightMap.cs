using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

namespace NuclearOption.Effects
{
	public class TerrainHeightMap : SceneSingleton<TerrainHeightMap>
	{
		[Serializable]
		public enum EntryType
		{
			Blocker = 0,
			HeightProvider = 1
		}

		[Serializable]
		public struct RenderFilter
		{
			public MeshRenderer Renderer;

			public SubmeshFilter SubmeshFilter;

			public EntryType EntryType;

			public bool BypassHeightCheck;

			public RenderFilter(MeshRenderer renderer)
			{
				Renderer = renderer;
				SubmeshFilter = SubmeshFilter.All;
				EntryType = EntryType.Blocker;
				BypassHeightCheck = false;
			}

			public RenderFilter(MeshRenderer renderer, SubmeshFilter submeshFilter, EntryType entryType)
			{
				Renderer = renderer;
				SubmeshFilter = submeshFilter;
				EntryType = entryType;
				BypassHeightCheck = false;
			}

			public static implicit operator RenderFilter(MeshRenderer renderer)
			{
				return new RenderFilter(renderer);
			}
		}

		[Flags]
		public enum SubmeshFilter
		{
			None = 0,
			Submesh0 = 1,
			Submesh1 = 2,
			Submesh2 = 4,
			Submesh3 = 8,
			Submesh4 = 0x10,
			Submesh5 = 0x20,
			Submesh6 = 0x40,
			Submesh7 = 0x80,
			Submesh8 = 0x100,
			Submesh9 = 0x200,
			All = -1
		}

		public class Entry
		{
			public readonly Renderer renderer;

			public readonly SubmeshFilter submeshFilter;

			public readonly Mesh mesh;

			public readonly EntryType type;

			[NonSerialized]
			public int LastQueryId;

			[NonSerialized]
			public (Vector2Int minCoords, Vector2Int maxCoords) MinMaxCoords;

			public Entry(Renderer renderer, SubmeshFilter submeshFilter, Mesh mesh, EntryType type)
			{
				this.renderer = renderer;
				this.submeshFilter = submeshFilter;
				this.mesh = mesh;
				this.type = type;
			}
		}

		private struct SpatialList
		{
			public List<Entry> List;
		}

		private static readonly ProfilerMarker bakeWindowMarker = new ProfilerMarker("TerrainHeightMap.BakeWindow");

		private static readonly ProfilerMarker getObjectsInBoundsMarker = new ProfilerMarker("TerrainHeightMap.GetObjectsInBounds");

		private static readonly ProfilerMarker registerObjectMarker = new ProfilerMarker("TerrainHeightMap.RegisterObject");

		private static readonly ProfilerMarker unregisterObjectMarker = new ProfilerMarker("TerrainHeightMap.UnegisterObject");

		private static readonly ProfilerMarker onMovePositionMarker = new ProfilerMarker("TerrainHeightMap.OnMovePosition");

		public const int MAX_REASONABLE_VERTS = 1000;

		[Header("Assets")]
		[SerializeField]
		private Shader heightBakeShader;

		[SerializeField]
		private Shader blockerShader;

		[Space]
		[SerializeField]
		public DetailRenderer detailRenderer;

		[SerializeField]
		private MapSettings mapSettings;

		[Header("HeightMap")]
		public MinMax height = new MinMax(0f, 5000f);

		[Tooltip("If vert is above this height, then ignore it")]
		public float blockerHeightVertThreshold = 100f;

		[Tooltip("(for Blockers) If meshBounds.min.y is above this height, then ignore it")]
		public float blockerHeightMeshThreshold = 20f;

		[Tooltip("(for HeightProvider) If meshBounds.min.y is above this height, then ignore it")]
		public float heightProviderMeshProviderThreshold = 20f;

		public float blockerPadding = 1f;

		[Header("Object Grid")]
		[Tooltip("Larger size means less cells need to be checked, but more overdraw if window size is bigger")]
		public float spatialCellSize = 1024f;

		[Tooltip("Extra cells padding to prevent edge errors")]
		public int gridPadding = 4;

		[Header("Auto Find Renderers")]
		public Transform AutoSearchRoot;

		public bool AutoFindTerrain;

		public bool AutoFindBlockers;

		[SerializeField]
		private Material[] terrainMaterials;

		[SerializeField]
		public PhysicMaterial[] terrainPhysicMaterials;

		[Header("Manual Renderers")]
		[SerializeField]
		private MeshRenderer[] manualHeightProvider;

		[SerializeField]
		private MeshRenderer[] manualHeightBlocker;

		private readonly List<RenderFilter> externalObjects = new List<RenderFilter>();

		private int objectsInBoundsQueryId;

		private Material bakeMaterial;

		private Material blockerMaterial;

		private MaterialPropertyBlock bakeMaterialProps;

		private MaterialPropertyBlock blockerMaterialProps;

		[NonSerialized]
		private SpatialList[] spatialGrid;

		private Vector2Int gridResolution;

		private Vector2Int gridOffset;

		private RenderTargetIdentifier[] heightMapIdentifier;

		private readonly List<Entry> allEntries = new List<Entry>();

		private readonly List<Entry> bakeHeight = new List<Entry>();

		private readonly List<Entry> bakeBlocker = new List<Entry>();

		public bool IsActive { get; private set; }

		public RenderTexture heightMap { get; private set; }

		public RenderTexture normalMap { get; private set; }

		public RenderTexture blockerMap { get; private set; }

		public bool IsDirty { get; private set; }

		public void CommandSetup()
		{
			IsActive = true;
			int windowSize = detailRenderer.windowSize;
			heightMap = new RenderTexture(windowSize, windowSize, 16, RenderTextureFormat.R16);
			heightMap.enableRandomWrite = true;
			heightMap.name = "GrassHeightWindow";
			heightMap.Create();
			normalMap = new RenderTexture(windowSize, windowSize, 0, RenderTextureFormat.ARGB32);
			normalMap.enableRandomWrite = true;
			normalMap.name = "GrassNormalWindow";
			normalMap.Create();
			heightMapIdentifier = new RenderTargetIdentifier[2];
			heightMapIdentifier[0] = heightMap.colorBuffer;
			heightMapIdentifier[1] = normalMap.colorBuffer;
			blockerMap = new RenderTexture(windowSize, windowSize, 0, RenderTextureFormat.R8);
			blockerMap.name = "GrassBlockerWindow";
			blockerMap.Create();
			bakeMaterial = new Material(heightBakeShader);
			blockerMaterial = new Material(blockerShader);
			bakeMaterialProps = new MaterialPropertyBlock();
			blockerMaterialProps = new MaterialPropertyBlock();
			SetBakeMaterialProps();
			SetBlockerMaterialProps();
			InitializeGrid();
			foreach (RenderFilter externalObject in externalObjects)
			{
				RegisterInternal(externalObject);
			}
			MeshRenderer[] array = manualHeightProvider;
			foreach (MeshRenderer r in array)
			{
				RegisterInternal(r, EntryType.HeightProvider);
			}
			array = manualHeightBlocker;
			foreach (MeshRenderer r2 in array)
			{
				RegisterInternal(r2, EntryType.Blocker);
			}
			AutoFindRenderers();
		}

		public void CommandRelease()
		{
			IsActive = false;
			spatialGrid = null;
			allEntries.Clear();
			bakeHeight.Clear();
			bakeBlocker.Clear();
			if (heightMap != null)
			{
				heightMap.Release();
				UnityEngine.Object.Destroy(heightMap);
			}
			if (normalMap != null)
			{
				normalMap.Release();
				UnityEngine.Object.Destroy(normalMap);
			}
			if (blockerMap != null)
			{
				blockerMap.Release();
				UnityEngine.Object.Destroy(blockerMap);
			}
			if (bakeMaterial != null)
			{
				UnityEngine.Object.Destroy(bakeMaterial);
			}
			if (blockerMaterial != null)
			{
				UnityEngine.Object.Destroy(blockerMaterial);
			}
		}

		private void SetBakeMaterialProps()
		{
			bakeMaterialProps.SetVector(ShaderIds.ID_Height, height.ToMinInvRange());
		}

		private void SetBlockerMaterialProps()
		{
			blockerMaterialProps.SetFloat(ShaderIds.ID_BlockerHeightThreshold, blockerHeightVertThreshold);
			blockerMaterialProps.SetFloat(ShaderIds.ID_BlockerPadding, blockerPadding);
			blockerMaterialProps.SetVector(ShaderIds.ID_Height, height.ToMinRange());
			blockerMaterialProps.SetTexture(ShaderIds.ID_HeightMap, heightMap);
		}

		private void OnValidate()
		{
			if (bakeMaterialProps != null)
			{
				SetBakeMaterialProps();
			}
			if (blockerMaterialProps != null)
			{
				SetBlockerMaterialProps();
			}
		}

		private void OnDestroy()
		{
			CommandRelease();
		}

		public void BakeWindow(CommandBuffer cmd, Vector2Int windowIndex)
		{
			using (bakeWindowMarker.Auto())
			{
				IsDirty = false;
				int windowSize = detailRenderer.windowSize;
				int windowSnapping = detailRenderer.windowSnapping;
				GlobalPosition globalPosition = new GlobalPosition(windowIndex.x * windowSnapping, 0f, windowIndex.y * windowSnapping);
				GetObjectsInBounds(globalPosition, windowSize, bakeHeight, bakeBlocker);
				cmd.BeginSample("TerrainHeightMap.BakeWindow");
				Matrix4x4 inverse = Matrix4x4.TRS(globalPosition.ToLocalPosition(), Quaternion.Euler(90f, 0f, 0f), Vector3.one).inverse;
				float num = height.GetRange();
				if (num <= 0.0001f)
				{
					num = 1f;
				}
				Matrix4x4 proj = Matrix4x4.Ortho(-windowSize / 2, windowSize / 2, -windowSize / 2, windowSize / 2, 0f, num);
				cmd.SetViewProjectionMatrices(inverse, proj);
				cmd.SetRenderTarget(heightMap);
				cmd.ClearRenderTarget(clearDepth: true, clearColor: true, Color.black, 0f);
				cmd.SetRenderTarget(normalMap);
				cmd.ClearRenderTarget(clearDepth: true, clearColor: true, new Color(0.5f, 1f, 0.5f, 1f));
				cmd.SetRenderTarget(heightMapIdentifier, heightMap.depthBuffer);
				foreach (Entry item in bakeHeight)
				{
					cmd.BeginSample("TerrainHeightMap.BakeWindow.DrawTerrain");
					for (int i = 0; i < item.mesh.subMeshCount; i++)
					{
						if (CheckSubmeshFilter(i, item.submeshFilter))
						{
							cmd.DrawMesh(item.mesh, item.renderer.transform.localToWorldMatrix, bakeMaterial, i, -1, bakeMaterialProps);
						}
					}
					cmd.EndSample("TerrainHeightMap.BakeWindow.DrawTerrain");
				}
				cmd.SetRenderTarget(blockerMap);
				cmd.ClearRenderTarget(clearDepth: true, clearColor: true, Color.white);
				foreach (Entry item2 in bakeBlocker)
				{
					cmd.BeginSample("TerrainHeightMap.BakeWindow.DrawBlocker");
					for (int j = 0; j < item2.mesh.subMeshCount; j++)
					{
						if (CheckSubmeshFilter(j, item2.submeshFilter))
						{
							cmd.DrawMesh(item2.mesh, item2.renderer.transform.localToWorldMatrix, blockerMaterial, j, -1, blockerMaterialProps);
						}
					}
					cmd.EndSample("TerrainHeightMap.BakeWindow.DrawBlocker");
				}
				cmd.EndSample("TerrainHeightMap.BakeWindow");
			}
		}

		private void InitializeGrid()
		{
			Vector2 mapSize = mapSettings.MapSize;
			int x = Mathf.CeilToInt(mapSize.x / spatialCellSize) + gridPadding * 2;
			int y = Mathf.CeilToInt(mapSize.y / spatialCellSize) + gridPadding * 2;
			gridResolution = new Vector2Int(x, y);
			gridOffset = gridResolution / 2;
			spatialGrid = new SpatialList[gridResolution.x * gridResolution.y];
		}

		private void AutoFindRenderers()
		{
			if (!AutoFindTerrain && !AutoFindBlockers)
			{
				return;
			}
			if (AutoSearchRoot == null)
			{
				AutoSearchRoot = base.transform;
			}
			MeshRenderer[] componentsInChildren = AutoSearchRoot.GetComponentsInChildren<MeshRenderer>();
			foreach (MeshRenderer meshRenderer in componentsInChildren)
			{
				if (meshRenderer.TryGetComponent<TerrainHeightMapBlocker>(out var _) || !meshRenderer.TryGetComponent<MeshFilter>(out var component2) || component2.sharedMesh == null)
				{
					continue;
				}
				if (IsTerrain(meshRenderer))
				{
					if (AutoFindTerrain)
					{
						RegisterInternal(meshRenderer, EntryType.HeightProvider);
					}
				}
				else if (AutoFindBlockers)
				{
					RegisterInternal(meshRenderer, EntryType.Blocker);
				}
			}
		}

		private bool IsTerrain(MeshRenderer renderer)
		{
			if (renderer.TryGetComponent<MeshCollider>(out var component))
			{
				PhysicMaterial sharedMaterial = component.sharedMaterial;
				PhysicMaterial[] array = terrainPhysicMaterials;
				foreach (PhysicMaterial physicMaterial in array)
				{
					if (sharedMaterial == physicMaterial)
					{
						return true;
					}
				}
			}
			Material[] sharedMaterials = renderer.sharedMaterials;
			foreach (Material material in sharedMaterials)
			{
				Material[] array2 = terrainMaterials;
				foreach (Material material2 in array2)
				{
					if (material == material2)
					{
						return true;
					}
				}
			}
			return false;
		}

		public void RegisterObject(RenderFilter r)
		{
			using (registerObjectMarker.Auto())
			{
				externalObjects.Add(r);
				if (spatialGrid != null)
				{
					RegisterInternal(r);
				}
			}
		}

		private void RegisterInternal(MeshRenderer r, EntryType type, SubmeshFilter filter = SubmeshFilter.All)
		{
			RegisterInternal(new RenderFilter(r, filter, type));
		}

		private void RegisterInternal(RenderFilter r)
		{
			if (!r.Renderer.TryGetComponent<MeshFilter>(out var component))
			{
				return;
			}
			Mesh sharedMesh = component.sharedMesh;
			if (r.Renderer == null || sharedMesh == null)
			{
				if (r.Renderer == null)
				{
					ColorLog<TerrainHeightMap>.LogError("Cannot register null renderer");
				}
				if (sharedMesh == null)
				{
					ColorLog<TerrainHeightMap>.LogError("Cannot register null mesh");
				}
				return;
			}
			foreach (Entry allEntry in allEntries)
			{
				if (allEntry.renderer == r.Renderer)
				{
					ColorLog<TerrainHeightMap>.LogError($"Renderer was already in allEntries list {r}, entity type {allEntry.type}");
					return;
				}
			}
			IsDirty = true;
			Entry entry = new Entry(r.Renderer, r.SubmeshFilter, sharedMesh, r.EntryType);
			allEntries.Add(entry);
			AddToSpatialGrid(entry);
		}

		public void UnregisterObject(Renderer r)
		{
			using (unregisterObjectMarker.Auto())
			{
				for (int i = 0; i < externalObjects.Count; i++)
				{
					if (externalObjects[i].Renderer == r)
					{
						externalObjects.RemoveAt(i);
						break;
					}
				}
				UnregisterInternal(r);
			}
		}

		private void UnregisterInternal(Renderer r)
		{
			if (allEntries.Count == 0)
			{
				return;
			}
			Entry entry = null;
			for (int i = 0; i < allEntries.Count; i++)
			{
				if (allEntries[i].renderer == r)
				{
					entry = allEntries[i];
					allEntries.RemoveAt(i);
					break;
				}
			}
			if (entry != null)
			{
				RemoveFromSpatialGrid(entry);
				IsDirty = true;
			}
		}

		public void SetDirty()
		{
			IsDirty = true;
		}

		public void OnMovePosition(RenderFilter r, bool show)
		{
			using (onMovePositionMarker.Auto())
			{
				IsDirty = true;
				int index = -1;
				Entry entry = null;
				for (int i = 0; i < allEntries.Count; i++)
				{
					if (allEntries[i].renderer == r.Renderer)
					{
						entry = allEntries[i];
						index = i;
						break;
					}
				}
				if (show)
				{
					if (entry == null)
					{
						RegisterObject(r);
						return;
					}
					(Vector2Int, Vector2Int) minMaxCoords = GetMinMaxCoords(entry);
					if (minMaxCoords.Item1 != entry.MinMaxCoords.minCoords || minMaxCoords.Item2 != entry.MinMaxCoords.maxCoords)
					{
						RemoveFromSpatialGrid(entry, entry.MinMaxCoords);
						entry.MinMaxCoords = minMaxCoords;
						AddToSpatialGrid(entry, minMaxCoords);
					}
				}
				else if (entry != null)
				{
					RemoveFromSpatialGrid(entry);
					allEntries.RemoveAt(index);
				}
			}
		}

		private (Vector2Int minCoords, Vector2Int maxCoords) GetMinMaxCoords(Entry entry)
		{
			Bounds bounds = entry.renderer.bounds;
			GlobalPosition pos = bounds.min.ToGlobalPosition();
			GlobalPosition pos2 = bounds.max.ToGlobalPosition();
			Vector2Int item = WorldToGridCoords(pos);
			Vector2Int item2 = WorldToGridCoords(pos2);
			return (minCoords: item, maxCoords: item2);
		}

		private void RemoveFromSpatialGrid(Entry entry)
		{
			RemoveFromSpatialGrid(entry, entry.MinMaxCoords);
		}

		private void RemoveFromSpatialGrid(Entry entry, (Vector2Int minCoords, Vector2Int maxCoords) minMaxCoords)
		{
			(Vector2Int minCoords, Vector2Int maxCoords) tuple = minMaxCoords;
			Vector2Int item = tuple.minCoords;
			Vector2Int item2 = tuple.maxCoords;
			for (int i = item.x; i <= item2.x; i++)
			{
				for (int j = item.y; j <= item2.y; j++)
				{
					int gridIndex = GetGridIndex(i, j);
					if (gridIndex != -1)
					{
						spatialGrid[gridIndex].List?.Remove(entry);
					}
				}
			}
		}

		private void AddToSpatialGrid(Entry entry)
		{
			entry.MinMaxCoords = GetMinMaxCoords(entry);
			AddToSpatialGrid(entry, entry.MinMaxCoords);
		}

		private void AddToSpatialGrid(Entry entry, (Vector2Int minCoords, Vector2Int maxCoords) minMaxCoords)
		{
			(Vector2Int minCoords, Vector2Int maxCoords) tuple = minMaxCoords;
			Vector2Int item = tuple.minCoords;
			Vector2Int item2 = tuple.maxCoords;
			for (int i = item.x; i <= item2.x; i++)
			{
				for (int j = item.y; j <= item2.y; j++)
				{
					int gridIndex = GetGridIndex(i, j);
					if (gridIndex == -1)
					{
						ColorLog<TerrainHeightMap>.Info($"Grid Index Out of Bounds: ({i}, {j}). Object: {entry.renderer.name} Coords: {item} to {item2}");
					}
					else
					{
						List<Entry> list = spatialGrid[gridIndex].List;
						if (list == null)
						{
							list = new List<Entry>();
							spatialGrid[gridIndex].List = list;
						}
						list.Add(entry);
					}
				}
			}
		}

		private Vector2Int WorldToGridCoords(GlobalPosition pos)
		{
			return new Vector2Int(Mathf.FloorToInt(pos.x / spatialCellSize) + gridOffset.x, Mathf.FloorToInt(pos.z / spatialCellSize) + gridOffset.y);
		}

		private int GetGridIndex(int x, int y)
		{
			if (x < 0 || x >= gridResolution.x || y < 0 || y >= gridResolution.y)
			{
				return -1;
			}
			return x + y * gridResolution.x;
		}

		public void GetObjectsInBounds(GlobalPosition center, float size, List<Entry> providers, List<Entry> blockers)
		{
			using (getObjectsInBoundsMarker.Auto())
			{
				providers.Clear();
				blockers.Clear();
				objectsInBoundsQueryId++;
				Vector3 vector = new Vector3(size / 2f, 10000f, size / 2f);
				GlobalPosition pos = center - vector;
				GlobalPosition pos2 = center + vector;
				Vector2Int vector2Int = WorldToGridCoords(pos);
				Vector2Int vector2Int2 = WorldToGridCoords(pos2);
				for (int i = vector2Int.x; i <= vector2Int2.x; i++)
				{
					for (int j = vector2Int.y; j <= vector2Int2.y; j++)
					{
						int gridIndex = GetGridIndex(i, j);
						if (gridIndex == -1)
						{
							continue;
						}
						List<Entry> list = spatialGrid[gridIndex].List;
						if (list == null)
						{
							continue;
						}
						for (int k = 0; k < list.Count; k++)
						{
							Entry entry = list[k];
							if (entry.LastQueryId != objectsInBoundsQueryId)
							{
								entry.LastQueryId = objectsInBoundsQueryId;
								if (entry.type == EntryType.HeightProvider)
								{
									providers.Add(entry);
								}
								else
								{
									blockers.Add(entry);
								}
							}
						}
					}
				}
			}
		}

		public static bool CheckSubmeshFilter(int index, SubmeshFilter filter)
		{
			if (index > 9)
			{
				Debug.LogWarning("CheckSubmeshFilter does not with mesh with over 10 submesh");
			}
			return ((uint)filter & (uint)(1 << index)) != 0;
		}
	}
}
