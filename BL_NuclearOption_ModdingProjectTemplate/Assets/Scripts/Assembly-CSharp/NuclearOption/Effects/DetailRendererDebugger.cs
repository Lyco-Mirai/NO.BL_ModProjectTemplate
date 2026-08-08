using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace NuclearOption.Effects
{
	[DefaultExecutionOrder(9)]
	public class DetailRendererDebugger : MonoBehaviour
	{
		[Serializable]
		public enum MapType
		{
			Height = 0,
			Normal = 1,
			Blocker = 2,
			Blast = 3,
			Lush_Index0 = 4
		}

		[SerializeField]
		private MapType mapType;

		[SerializeField]
		private DetailRenderer detailRenderer;

		[SerializeField]
		private TerrainHeightMap heightMap;

		[SerializeField]
		private GrassRenderer grassRenderer;

		[SerializeField]
		private BlastManager blastManager;

		[SerializeField]
		private Material realTerrainMaterialToReplace;

		[SerializeField]
		private Shader debugShader;

		private Material mat;

		private List<MeshRenderer> renderers;

		private MapType _currentType;

		private void OnEnable()
		{
			renderers = new List<MeshRenderer>();
			GameObject[] rootGameObjects = SceneManager.GetActiveScene().GetRootGameObjects();
			for (int i = 0; i < rootGameObjects.Length; i++)
			{
				MeshRenderer[] componentsInChildren = rootGameObjects[i].GetComponentsInChildren<MeshRenderer>();
				foreach (MeshRenderer meshRenderer in componentsInChildren)
				{
					if (meshRenderer.sharedMaterial == realTerrainMaterialToReplace)
					{
						renderers.Add(meshRenderer);
					}
				}
			}
			Setup(renderers);
		}

		private void OnDisable()
		{
			if (renderers == null)
			{
				return;
			}
			foreach (MeshRenderer renderer in renderers)
			{
				renderer.sharedMaterial = realTerrainMaterialToReplace;
			}
		}

		public void Setup(List<MeshRenderer> renderers)
		{
			mat = new Material(debugShader);
			SetMapTexture();
			mat.SetInt(ShaderIds.ID_WindowSnapping, detailRenderer.windowSnapping);
			mat.SetInt(ShaderIds.ID_WindowSize, detailRenderer.windowSize);
			foreach (MeshRenderer renderer in renderers)
			{
				renderer.sharedMaterial = mat;
			}
		}

		private void SetMapTexture()
		{
			Texture value = mapType switch
			{
				MapType.Height => heightMap.heightMap, 
				MapType.Normal => heightMap.normalMap, 
				MapType.Blocker => heightMap.blockerMap, 
				MapType.Blast => blastManager.Texture, 
				MapType.Lush_Index0 => grassRenderer.LushMapIndex0, 
				_ => null, 
			};
			bool value2 = mapType switch
			{
				MapType.Height => true, 
				MapType.Normal => true, 
				MapType.Blocker => true, 
				MapType.Blast => false, 
				MapType.Lush_Index0 => false, 
				_ => false, 
			};
			_currentType = mapType;
			mat.SetTexture(ShaderIds.ID_DebugMap, value);
			mat.SetKeyword(new LocalKeyword(debugShader, "USE_WINDOW"), value2);
			mat.SetKeyword(new LocalKeyword(debugShader, "TEXTURE_COMPRESSED_NORMALS"), mapType == MapType.Normal);
		}

		private void LateUpdate()
		{
			mat.SetVector(ShaderIds.ID_WindowIndex, new Vector4(detailRenderer.WindowIndex.x, detailRenderer.WindowIndex.y, 0f, 0f));
			if (_currentType != mapType)
			{
				SetMapTexture();
			}
		}
	}
}
