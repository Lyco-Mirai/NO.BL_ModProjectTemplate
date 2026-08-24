using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace NuclearOption.MissionEditorScripts
{
	public class AirbaseEditorRadius : MonoBehaviour
	{
		private static readonly List<AirbaseEditorRadius> cache = new List<AirbaseEditorRadius>();

		[SerializeField]
		private DecalProjector projector;

		[SerializeField]
		private float colorAlpha = 0.5f;

		private Material material;

		private void Awake()
		{
			material = Object.Instantiate(projector.material);
			projector.material = material;
		}

		private void OnDestroy()
		{
			if (material != null)
			{
				Object.Destroy(material);
			}
		}

		public void Setup(Color color, float radius)
		{
			Vector3 size = projector.size;
			size.x = radius * 2f;
			size.y = radius * 2f;
			projector.size = size;
			color.a = colorAlpha;
			material.SetColor("_Color", color);
		}

		public static AirbaseEditorRadius Create(Transform parent, GameObject prefab)
		{
			return Object.Instantiate(prefab, parent).GetComponent<AirbaseEditorRadius>();
		}

		public static AirbaseEditorRadius Find(Transform directParent)
		{
			cache.Clear();
			directParent.GetComponentsInChildren(cache);
			foreach (AirbaseEditorRadius item in cache)
			{
				if (item.transform.parent == directParent)
				{
					return item;
				}
			}
			return null;
		}
	}
}
