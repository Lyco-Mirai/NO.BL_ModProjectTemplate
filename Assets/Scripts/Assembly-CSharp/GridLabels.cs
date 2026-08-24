using System;
using System.Runtime.CompilerServices;
using NuclearOption.UIStyleSystem;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.UI;

public class GridLabels : MonoBehaviour
{
	private static readonly ProfilerMarker gridLabels_OnMapChangedMarker = new ProfilerMarker("GridLabels_OnMapChanged");

	private static readonly ProfilerMarker updateMinorGridLabelsMarker = new ProfilerMarker("UpdateMinorGridLabels");

	private static readonly ProfilerMarker showGridToolTipMarker = new ProfilerMarker("ShowGridToolTip");

	private static readonly ProfilerMarker showGridAircraftMarker = new ProfilerMarker("ShowGridAircraft");

	private static readonly ProfilerMarker maximizeMarker = new ProfilerMarker("Maximize");

	private static readonly ProfilerMarker showLabelsMarker = new ProfilerMarker("ShowLabels");

	[SerializeField]
	private Text gridToolTip;

	[SerializeField]
	private Text gridAircraft;

	[SerializeField]
	private Font defaultFont;

	[SerializeField]
	private Material defaultMaterial;

	[SerializeField]
	private int offsetX = 80000;

	[SerializeField]
	private int offsetY = 80000;

	private GameObject majorParent;

	private GameObject minorParent;

	private Text[] listHorizontal;

	private Text[] listHorizontalMinor;

	private Text[] listVertical;

	private Text[] listVerticalMinor;

	private static string[] verticalMinorStrings;

	private static string[] horizontalMinorStrings;

	private int previousMinorVertical = int.MinValue;

	private int previousMinorHorizontal = int.MinValue;

	private float previousMapInverseScale = -2.1474836E+09f;

	private float previousPosFixTop = -2.1474836E+09f;

	private float previousPosFixLeft = -2.1474836E+09f;

	public GameObject gridImage_prefab;

	private GameObject[] gridImages;

	public bool LabelShown { get; private set; }

	public bool LabelEnabled { get; private set; } = true;

	public void SetupGrid(int gridSizeX, int gridSizeY, int off_X, int off_Y)
	{
		offsetX = off_X;
		offsetY = off_Y;
		MakeNumberMinorStrings(ref horizontalMinorStrings, gridSizeX);
		MakeLetterMinorStrings(ref verticalMinorStrings, gridSizeY);
		if (majorParent == null)
		{
			majorParent = new GameObject("MajorParent", typeof(RectTransform));
			majorParent.transform.SetParent(base.transform, worldPositionStays: false);
		}
		if (minorParent == null)
		{
			minorParent = new GameObject("MinorParent", typeof(RectTransform));
			minorParent.transform.SetParent(base.transform, worldPositionStays: false);
		}
		if (listHorizontal == null || listHorizontal.Length < gridSizeX)
		{
			listHorizontal = new Text[gridSizeX];
		}
		if (listVertical == null || listVertical.Length < gridSizeY)
		{
			listVertical = new Text[gridSizeY];
		}
		for (int i = 0; i < listHorizontal.Length; i++)
		{
			if (listHorizontal[i] == null)
			{
				string text = $"Top_{i + 1}";
				string text2 = $"{i}";
				listHorizontal[i] = MakeGridText(text, text2, majorParent.transform);
			}
			listHorizontal[i].gameObject.SetActive(i < gridSizeX);
		}
		listVertical = new Text[gridSizeY];
		for (int j = 0; j < listVertical.Length; j++)
		{
			if (listVertical[j] == null)
			{
				string text3 = $"Left_{j + 1}";
				string text4 = $"{(char)(65 + j)}";
				listVertical[j] = MakeGridText(text3, text4, majorParent.transform);
			}
			listVertical[j].gameObject.SetActive(j < gridSizeY);
		}
		if (listHorizontalMinor == null)
		{
			listHorizontalMinor = new Text[10];
			listVerticalMinor = new Text[10];
			for (int k = 0; k < 10; k++)
			{
				string text5 = $"TopMinor_{k + 1}";
				string text6 = $"{k}";
				listHorizontalMinor[k] = MakeGridText(text5, text6, minorParent.transform);
				string text7 = $"LeftMinor_{k + 1}";
				string text8 = $"{(char)(97 + k)}";
				listVerticalMinor[k] = MakeGridText(text7, text8, minorParent.transform);
			}
		}
		int num = gridSizeX / 4;
		int num2 = gridSizeY / 4;
		Vector2 vector = 439.24f * Vector2.one;
		int num3 = num * num2;
		if (gridImages == null)
		{
			gridImages = new GameObject[num3];
		}
		for (int l = num3; l < gridImages.Length; l++)
		{
			UnityEngine.Object.Destroy(gridImages[l]);
		}
		if (gridImages.Length != num3)
		{
			Array.Resize(ref gridImages, num3);
		}
		for (int m = 0; m < num; m++)
		{
			for (int n = 0; n < num2; n++)
			{
				ref GameObject reference = ref gridImages[m * num2 + n];
				if (reference == null)
				{
					reference = UnityEngine.Object.Instantiate(gridImage_prefab, base.transform);
				}
				reference.name = $"mapGrid_{m}_{n}";
				reference.transform.localPosition = new Vector3(((float)(-num / 2 + m) + 0.5f) * vector.x, ((float)(num2 / 2 - n) - 0.5f) * vector.y, 0f);
			}
		}
		LabelShown = false;
	}

	private Text MakeGridText(string name, string text, Transform parent)
	{
		GameObject obj = new GameObject();
		obj.transform.SetParent(parent, worldPositionStays: false);
		obj.name = name;
		Text text2 = obj.AddComponent<Text>();
		text2.text = text;
		text2.font = defaultFont;
		text2.material = defaultMaterial;
		text2.supportRichText = false;
		text2.fontSize = 24;
		text2.alignment = TextAnchor.MiddleCenter;
		text2.horizontalOverflow = HorizontalWrapMode.Overflow;
		text2.verticalOverflow = VerticalWrapMode.Overflow;
		return text2;
	}

	private void LateUpdate()
	{
		if (!SceneSingleton<MapOptions>.i.showGridLabels)
		{
			return;
		}
		if (DynamicMap.mapMaximized)
		{
			ShowGridToolTip();
			GridLabels_OnMapChanged(_: true);
		}
		if (SceneSingleton<CombatHUD>.i.aircraft != null)
		{
			if (!gridAircraft.enabled)
			{
				gridAircraft.enabled = true;
			}
			ShowGridAircraft();
		}
		else if (gridAircraft.enabled)
		{
			gridAircraft.enabled = false;
		}
	}

	public void GridLabels_OnMapChanged(bool _)
	{
		using (gridLabels_OnMapChangedMarker.Auto())
		{
			if (!SceneSingleton<MapOptions>.i.showGridLabels)
			{
				return;
			}
			if (DynamicMap.mapMaximized)
			{
				Transform transform = SceneSingleton<DynamicMap>.i.mapImage.transform;
				float num = 1f / transform.localScale.x;
				float num2 = (440f - transform.localPosition.y) * num;
				float num3 = (-440f - transform.localPosition.x) * num;
				bool flag = HasChanged(ref previousMapInverseScale, num);
				flag |= HasChanged(ref previousPosFixTop, num2);
				flag |= HasChanged(ref previousPosFixLeft, num3);
				if (flag)
				{
					for (int i = 0; i < listHorizontal.Length; i++)
					{
						float x = ((float)(-offsetX) + (5000f + (float)i * 10000f)) * SceneSingleton<DynamicMap>.i.mapDisplayFactor;
						listHorizontal[i].transform.localScale = 0.5f * num * Vector3.one;
						listHorizontal[i].transform.localPosition = new Vector3(x, num2, 0f);
					}
				}
				if (flag)
				{
					for (int j = 0; j < listVertical.Length; j++)
					{
						float y = ((float)offsetY - (5000f + (float)j * 10000f)) * SceneSingleton<DynamicMap>.i.mapDisplayFactor;
						listVertical[j].transform.localScale = 0.5f * num * Vector3.one;
						listVertical[j].transform.localPosition = new Vector3(num3, y, 0f);
					}
				}
				bool show = num < 0.33f;
				if (show)
				{
					UpdateMinorGridLabels(ref show, flag);
				}
				if (minorParent.activeSelf != show)
				{
					minorParent.SetActive(show);
				}
			}
			else if (LabelShown)
			{
				ShowLabels(show: false);
			}
		}
	}

	public void UpdateGridColor()
	{
		GameObject[] array = gridImages;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].GetComponent<Image>().color = ThemeManager.Active.ColorTheme.MapBackground;
		}
	}

	private void UpdateMinorGridLabels(ref bool show, bool changed)
	{
		using (updateMinorGridLabelsMarker.Auto())
		{
			Transform transform = SceneSingleton<DynamicMap>.i.mapImage.transform;
			float num = 1f / transform.localScale.x;
			Vector3 vector = Input.mousePosition - transform.position;
			vector *= SceneSingleton<DynamicMap>.i.mapDimension / (900f * transform.lossyScale.x);
			Vector3 vector2 = new GlobalPosition(vector.x, 0f, vector.y).AsVector3();
			vector2 = new Vector3((float)offsetX + vector2.x, 0f, (float)offsetY - vector2.z);
			int num2 = Mathf.FloorToInt(vector2.x / 10000f);
			int num3 = Mathf.FloorToInt(vector2.z / 10000f);
			if (!InBounds(num2, num3))
			{
				show = false;
				return;
			}
			changed |= HasChanged(ref previousMinorVertical, num3);
			changed |= HasChanged(ref previousMinorHorizontal, num2);
			if (changed)
			{
				float y = (420f - transform.localPosition.y) * num;
				float x = (-420f - transform.localPosition.x) * num;
				Vector3 localScale = 0.5f * num * Vector3.one;
				for (int i = 0; i < 10; i++)
				{
					listHorizontalMinor[i].text = GetHorizontalMinorLabel(num2, i);
					listVerticalMinor[i].text = GetVerticalMinorLabel(num3, i);
					listHorizontalMinor[i].transform.localScale = localScale;
					listVerticalMinor[i].transform.localScale = localScale;
					float x2 = ((float)(-offsetX) + (500f + (float)i * 1000f + (float)num2 * 10000f)) * SceneSingleton<DynamicMap>.i.mapDisplayFactor;
					float y2 = ((float)offsetY - (500f + (float)i * 1000f + (float)num3 * 10000f)) * SceneSingleton<DynamicMap>.i.mapDisplayFactor;
					listHorizontalMinor[i].transform.localPosition = new Vector3(x2, y, 0f);
					listVerticalMinor[i].transform.localPosition = new Vector3(x, y2, 0f);
				}
			}
		}
	}

	private void ShowGridToolTip()
	{
		using (showGridToolTipMarker.Auto())
		{
			gridToolTip.text = GetGridPosition(SceneSingleton<DynamicMap>.i.GetCursorCoordinates());
		}
	}

	private void ShowGridAircraft()
	{
		using (showGridAircraftMarker.Auto())
		{
			string gridPosition = GetGridPosition(SceneSingleton<CombatHUD>.i.aircraft.GlobalPosition());
			gridAircraft.text = (DynamicMap.mapMaximized ? ("Current: " + gridPosition) : (gridPosition ?? ""));
		}
	}

	public string GetGridPosition(GlobalPosition globalPosition)
	{
		Vector3 vector = new Vector3((float)offsetX + globalPosition.x, 0f, (float)offsetY - globalPosition.z);
		int num = Mathf.FloorToInt(vector.x / 10000f);
		int minor = Mathf.FloorToInt((vector.x - 10000f * (float)num) / 1000f);
		int num2 = Mathf.FloorToInt(vector.z / 10000f);
		int minor2 = Mathf.FloorToInt((vector.z - 10000f * (float)num2) / 1000f);
		if (!InBounds(num, num2))
		{
			return "";
		}
		string horizontalMinorLabel = GetHorizontalMinorLabel(num, minor);
		return GetVerticalMinorLabel(num2, minor2) + horizontalMinorLabel;
	}

	public void Maximize(bool maximized)
	{
		using (maximizeMarker.Auto())
		{
			majorParent.SetActive(maximized);
			minorParent.SetActive(maximized);
			gridToolTip.enabled = maximized;
			LabelShown = maximized;
			if (maximized)
			{
				gridToolTip.transform.SetParent(SceneSingleton<DynamicMap>.i.mapImage.transform.parent, worldPositionStays: false);
				gridToolTip.transform.localPosition = new Vector3(430f, -390f, 0f);
				gridToolTip.transform.localEulerAngles = Vector3.zero;
				gridAircraft.transform.SetParent(SceneSingleton<DynamicMap>.i.mapImage.transform.parent, worldPositionStays: false);
				gridAircraft.transform.localPosition = new Vector3(430f, -420f, 0f);
				gridAircraft.transform.localEulerAngles = Vector3.zero;
			}
			else
			{
				gridAircraft.transform.SetParent(SceneSingleton<DynamicMap>.i.hudMapAnchor, worldPositionStays: false);
				gridAircraft.transform.localPosition = new Vector3(140f, -130f, 0f);
				gridAircraft.transform.localEulerAngles = Vector3.zero;
			}
		}
	}

	public void ShowLabels(bool show)
	{
		using (showLabelsMarker.Auto())
		{
			majorParent.SetActive(show);
			minorParent.SetActive(show);
			gridToolTip.enabled = show;
			gridAircraft.enabled = show;
			LabelEnabled = show;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool InBounds(int x, int y)
	{
		if (x >= 0 && x < listHorizontal.Length && y >= 0)
		{
			return y < listVertical.Length;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetHorizontalMinorLabel(int major, int minor)
	{
		if (0 <= major && major < listHorizontal.Length)
		{
			int num = major * 10 + minor;
			return horizontalMinorStrings[num];
		}
		return "";
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetVerticalMinorLabel(int major, int minor)
	{
		if (0 <= major && major < listVertical.Length)
		{
			int num = major * 10 + minor;
			return verticalMinorStrings[num];
		}
		return "";
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool HasChanged(ref int previous, int current)
	{
		bool result = previous != current;
		previous = current;
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool HasChanged(ref float previous, float current)
	{
		bool result = previous != current;
		previous = current;
		return result;
	}

	private static void MakeNumberMinorStrings(ref string[] array, int gridSize)
	{
		if (array == null || array.Length <= gridSize * 10)
		{
			array = new string[gridSize * 10];
			for (int i = 0; i < array.Length; i++)
			{
				int num = i / 10;
				int num2 = i % 10;
				array[i] = $"{num}{num2}";
			}
		}
	}

	private static void MakeLetterMinorStrings(ref string[] array, int gridSize)
	{
		if (array == null || array.Length <= gridSize * 10)
		{
			array = new string[gridSize * 10];
			for (int i = 0; i < array.Length; i++)
			{
				char c = (char)(65 + i / 10);
				char c2 = (char)(97 + i % 10);
				array[i] = $"{c}{c2}";
			}
		}
	}
}
