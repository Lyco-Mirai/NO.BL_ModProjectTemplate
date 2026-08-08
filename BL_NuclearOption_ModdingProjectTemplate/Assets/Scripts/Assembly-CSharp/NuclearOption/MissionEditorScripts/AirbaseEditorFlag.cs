using System.Collections.Generic;
using UnityEngine;

namespace NuclearOption.MissionEditorScripts
{
	public class AirbaseEditorFlag : MonoBehaviour
	{
		private static readonly List<AirbaseEditorFlag> cache = new List<AirbaseEditorFlag>();

		private Material material;

		public void SetColor(Color color)
		{
			material.SetColor("_Color", color);
		}

		private void OnDestroy()
		{
			Object.Destroy(material);
		}

		public static AirbaseEditorFlag Create(Transform parent, GameObject prefab, Color color, float extraScale)
		{
			Vector3 localScale = Vector3.one * extraScale;
			AirbaseEditorFlag airbaseEditorFlag = Object.Instantiate(prefab, parent).AddComponent<AirbaseEditorFlag>();
			airbaseEditorFlag.transform.localScale = localScale;
			color.a = 0.5f;
			Renderer component = airbaseEditorFlag.GetComponent<Renderer>();
			airbaseEditorFlag.material = component.material;
			airbaseEditorFlag.SetColor(color);
			return airbaseEditorFlag;
		}

		public static AirbaseEditorFlag Find(Transform directParent)
		{
			cache.Clear();
			directParent.GetComponentsInChildren(cache);
			foreach (AirbaseEditorFlag item in cache)
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
