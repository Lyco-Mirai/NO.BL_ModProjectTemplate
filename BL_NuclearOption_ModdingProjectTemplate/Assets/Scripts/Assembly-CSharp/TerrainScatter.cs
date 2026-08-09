using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

public class TerrainScatter : MonoBehaviour
{
	private static readonly ProfilerMarker GenerateScattersDensity = new ProfilerMarker("GenerateScattersDensity");

	private static readonly ProfilerMarker GenerateScattersLineCast = new ProfilerMarker("GenerateScattersLineCast");

	private static readonly ProfilerMarker GenerateScattersCreatePoint = new ProfilerMarker("GenerateScattersCreatePoint");

	[SerializeField]
	private MapSettings mapSettings;

	[SerializeField]
	private float globalDensity;

	private Vector2Int sectorCount;

	private Vector2 terrainSize;

	[SerializeField]
	private float sectorSize;

	[SerializeField]
	private GameObject sectorVis;

	[SerializeField]
	private Transform datum;

	[SerializeField]
	public ScatterType[] scatterTypes;

	public void GenerateScatters(GameAssets gameAssets)
	{
		using (BenchmarkScope.Create("GenerateScatters"))
		{
			terrainSize = mapSettings.MapSize;
			sectorCount.x = (int)(mapSettings.MapSize.x / sectorSize);
			sectorCount.y = (int)(mapSettings.MapSize.y / sectorSize);
			UnityEngine.Random.InitState(42);
			int totalScatters = 0;
			ScatterType[] array = scatterTypes;
			foreach (ScatterType scatter in array)
			{
				GenerateScatter(ref totalScatters, scatter, gameAssets);
			}
		}
	}

	private void GenerateScatter(ref int totalScatters, ScatterType scatter, GameAssets gameAssets)
	{
		List<GlobalPosition> list = (scatter.GeneratedPositions = new List<GlobalPosition>());
		Vector3 position = datum.position;
		Vector3 zero = Vector3.zero;
		(byte[] redPixel, int width, int height) redPixels = GetRedPixels(scatter.densityMap);
		byte[] item = redPixels.redPixel;
		int item2 = redPixels.width;
		int item3 = redPixels.height;
		for (int i = 0; i < sectorCount.x * sectorCount.y; i++)
		{
			int num = i % sectorCount.x;
			int num2 = Mathf.FloorToInt(i / sectorCount.x);
			for (int j = 0; (float)j < scatter.maxDensity * globalDensity; j++)
			{
				Vector3 vector = new Vector3((float)num * sectorSize, 400f, (float)num2 * sectorSize) + new Vector3(sectorSize, 0f, sectorSize) * 0.5f - new Vector3(terrainSize.x, 0f, terrainSize.y) * 0.5f + new Vector3(UnityEngine.Random.Range((0f - sectorSize) * 0.5f, sectorSize * 0.5f), 0f, UnityEngine.Random.Range((0f - sectorSize) * 0.5f, sectorSize * 0.5f));
				zero.x = (float)item2 * (vector.x + terrainSize.x * 0.5f) / terrainSize.x;
				zero.z = (float)item3 * (vector.z + terrainSize.y * 0.5f) / terrainSize.y;
				int num3 = ((int)zero.x + item2) % item2;
				int num4 = ((int)zero.z + item3) % item3;
				int num5 = num3 + num4 * item2;
				if (!((float)(int)item[num5] > UnityEngine.Random.value * 255f))
				{
					continue;
				}
				Vector3 v = vector + Vector3.up * 3000f;
				int num6 = 0;
				for (int k = 0; k < scatter.samplePoints; k++)
				{
					Vector3 vector2 = vector;
					if (scatter.samplePoints > 0)
					{
						float f = MathF.PI * 2f * (float)k / (float)scatter.samplePoints;
						vector2 += (Vector3.right * Mathf.Sin(f) + Vector3.forward * Mathf.Cos(f)) * scatter.sampleRadius;
					}
					if (Physics.Linecast(vector2 + position, vector2 + position - Vector3.up * 5000f, out var hitInfo, PhysicsLayers.StaticsMask) && hitInfo.point.y > position.y && hitInfo.collider.sharedMaterial == gameAssets.terrainMaterial)
					{
						v.y = Mathf.Min(v.y, hitInfo.point.y - position.y);
						num6++;
					}
				}
				if (num6 == scatter.samplePoints)
				{
					v -= UnityEngine.Random.value * scatter.heightVariation * Vector3.up;
					list.Add(new GlobalPosition(v));
					totalScatters++;
				}
			}
		}
	}

	private static (byte[] redPixel, int width, int height) GetRedPixels(Texture2D map)
	{
		Color32[] pixels = map.GetPixels32();
		byte[] array = new byte[pixels.Length];
		for (int i = 0; i < pixels.Length; i++)
		{
			array[i] = pixels[i].r;
		}
		return (redPixel: array, width: map.width, height: map.height);
	}
}
