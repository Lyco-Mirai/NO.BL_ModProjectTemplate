using System.Collections.Generic;
using System.Linq;
using JamesFrowen.ScriptableVariables;
using UnityEngine;

namespace NuclearOption.Effects
{
	public class TerrainHeightMapBlocker : MonoBehaviour
	{
		private static RaycastHit[] hitCache = new RaycastHit[100];

		public List<TerrainHeightMap.RenderFilter> Renderers;

		public bool IgnoreHighVertWarning;

		private void CheckVertCount()
		{
			if (!IgnoreHighVertWarning)
			{
				GetTotalVertexCount(Renderers);
				_ = 1000;
			}
		}

		private void OnValidate()
		{
			for (int i = 0; i < Renderers.Count; i++)
			{
				if (Renderers[i].Renderer == null)
				{
					ColorLog<TerrainHeightMap>.LogError($"{base.name} has a null renderer at index {i}");
				}
			}
		}

		public void Start()
		{
			for (int num = Renderers.Count - 1; num >= 0; num--)
			{
				if (Renderers[num].Renderer == null)
				{
					Renderers.RemoveAt(num);
				}
			}
			foreach (TerrainHeightMap.RenderFilter renderer in Renderers)
			{
				if (RendererIsCloseToGround(renderer))
				{
					SceneSingleton<TerrainHeightMap>.i.RegisterObject(renderer);
				}
			}
		}

		private bool RendererIsCloseToGround(TerrainHeightMap.RenderFilter r)
		{
			if (r.BypassHeightCheck)
			{
				return true;
			}
			Bounds bounds = r.Renderer.bounds;
			float y = bounds.min.y;
			Vector3 origin = new Vector3(bounds.center.x, Datum.LocalSeaY + 5000f, bounds.center.z);
			float num = 0f;
			int num2 = Physics.RaycastNonAlloc(origin, Vector2.down, hitCache, 5000f, PhysicsLayers.StaticsMask);
			for (int i = 0; i < num2; i++)
			{
				RaycastHit raycastHit = hitCache[i];
				if (raycastHit.collider is MeshCollider meshCollider && SceneSingleton<TerrainHeightMap>.i.terrainPhysicMaterials.Contains(meshCollider.sharedMaterial) && !meshCollider.TryGetComponent<Unit>(out var _))
				{
					num = raycastHit.point.y;
				}
			}
			float num3 = ((r.EntryType == TerrainHeightMap.EntryType.Blocker) ? SceneSingleton<TerrainHeightMap>.i.blockerHeightMeshThreshold : SceneSingleton<TerrainHeightMap>.i.heightProviderMeshProviderThreshold);
			return y < num + num3;
		}

		private void OnDestroy()
		{
			foreach (TerrainHeightMap.RenderFilter renderer in Renderers)
			{
				SceneSingleton<TerrainHeightMap>.i.UnregisterObject(renderer.Renderer);
			}
		}

		public MeshRenderer[] FindRenderers()
		{
			return GetComponentsInChildren<MeshRenderer>();
		}

		private void Reset()
		{
			Renderers = (from x in FindRenderers()
				select new TerrainHeightMap.RenderFilter(x)).ToList();
		}

		public static int GetTotalVertexCount(List<TerrainHeightMap.RenderFilter> renderers)
		{
			if (renderers == null)
			{
				return 0;
			}
			int num = 0;
			foreach (TerrainHeightMap.RenderFilter renderer in renderers)
			{
				if (renderer.Renderer == null)
				{
					continue;
				}
				MeshFilter component = renderer.Renderer.GetComponent<MeshFilter>();
				Mesh mesh = component?.sharedMesh;
				if (!(mesh != null))
				{
					continue;
				}
				for (int i = 0; i < mesh.subMeshCount; i++)
				{
					if (TerrainHeightMap.CheckSubmeshFilter(i, renderer.SubmeshFilter))
					{
						num += component.sharedMesh.GetSubMesh(i).vertexCount;
					}
				}
			}
			return num;
		}

		public static int GetTotalSubMeshCount(List<TerrainHeightMap.RenderFilter> renderers)
		{
			if (renderers == null)
			{
				return 0;
			}
			int num = 0;
			foreach (TerrainHeightMap.RenderFilter renderer in renderers)
			{
				if (renderer.Renderer == null)
				{
					continue;
				}
				Mesh mesh = renderer.Renderer.GetComponent<MeshFilter>()?.sharedMesh;
				if (!(mesh != null))
				{
					continue;
				}
				for (int i = 0; i < mesh.subMeshCount; i++)
				{
					if (TerrainHeightMap.CheckSubmeshFilter(i, renderer.SubmeshFilter))
					{
						num++;
					}
				}
			}
			return num;
		}

		public void EditorUpdatePosition()
		{
			foreach (TerrainHeightMap.RenderFilter renderer in Renderers)
			{
				SceneSingleton<TerrainHeightMap>.i.OnMovePosition(renderer, RendererIsCloseToGround(renderer));
			}
		}
	}
}
