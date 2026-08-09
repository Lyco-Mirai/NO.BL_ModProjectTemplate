using System.Collections.Generic;
using UnityEngine;

namespace NuclearOption.MissionEditorScripts.Buttons
{
	internal class UnitPreviewGenerator : MonoBehaviour
	{
		[SerializeField]
		private Camera camera;

		[SerializeField]
		private RenderTexture renderTexture;

		[SerializeField]
		private Transform holder;

		[SerializeField]
		public float sizeMultiple = 1f;

		[SerializeField]
		public bool ignoreCache;

		[SerializeField]
		private bool preview;

		[SerializeField]
		private UnitDefinition target;

		private readonly Dictionary<UnitDefinition, Texture2D> previews = new Dictionary<UnitDefinition, Texture2D>();

		private void Awake()
		{
			camera.targetTexture = renderTexture;
		}

		private void Update()
		{
			if (preview)
			{
				preview = false;
				Render(target);
			}
		}

		private void OnDestroy()
		{
			foreach (Texture2D value in previews.Values)
			{
				Object.Destroy(value);
			}
			previews.Clear();
		}

		public Texture2D GetSprite(UnitDefinition unit)
		{
			if (ignoreCache)
			{
				return GenerateSprite(unit);
			}
			if (!previews.TryGetValue(unit, out var value))
			{
				value = GenerateSprite(unit);
				previews.Add(unit, value);
			}
			return value;
		}

		private Texture2D GenerateSprite(UnitDefinition unit)
		{
			RenderTexture active = RenderTexture.active;
			RenderTexture.active = renderTexture;
			try
			{
				Render(unit);
				Texture2D texture2D = new Texture2D(renderTexture.width, renderTexture.height);
				texture2D.ReadPixels(new Rect(0f, 0f, renderTexture.width, renderTexture.height), 0, 0);
				texture2D.Apply();
				return texture2D;
			}
			finally
			{
				RenderTexture.active = active;
			}
		}

		private void Render(UnitDefinition unit)
		{
			using (BenchmarkScope.Create("Preview Render"))
			{
				camera.gameObject.SetActive(value: true);
				holder.gameObject.SetActive(value: true);
				GameObject obj = Object.Instantiate(unit.unitPrefab, holder);
				SetLayerRecursively(obj, holder.gameObject.layer);
				camera.orthographicSize = GetDistance(unit) * sizeMultiple;
				camera.Render();
				Object.Destroy(obj);
				camera.gameObject.SetActive(value: false);
				holder.gameObject.SetActive(value: false);
			}
		}

		private static void SetLayerRecursively(GameObject target, int layer)
		{
			Transform[] componentsInChildren = target.GetComponentsInChildren<Transform>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].gameObject.layer = layer;
			}
		}

		private float GetDistance(UnitDefinition definition)
		{
			float num = (Mathf.Max(definition.length, definition.width * 0.7f) + Mathf.Max(definition.width, definition.length * 0.7f)) * 0.5f;
			if (definition.height > definition.length && definition.height > definition.width)
			{
				num = definition.height * 1.2f;
			}
			float value = Mathf.Pow(num * 0.1f, 0.2f);
			value = Mathf.Clamp(value, 0.6f, 1.5f);
			return num / value;
		}
	}
}
