using System;
using System.Collections.Generic;
using Mirage;
using NuclearOption;
using NuclearOption.Networking;
using NuclearOption.UIStyleSystem;
using Rewired;
using UnityEngine;
using UnityEngine.UI;

public class DynamicMap : SceneSingleton<DynamicMap>
{
	private class RadarMapVis
	{
		public Image vectorImage;

		public Unit emitter;

		public float pingTime;

		public float delay = 1f;

		public RadarMapVis(Image vectorImage, Aircraft.OnRadarWarning source)
		{
			this.vectorImage = vectorImage;
			pingTime = Time.timeSinceLevelLoad;
			emitter = source.emitter;
			delay = 1f;
			if (source.detected)
			{
				delay = 2f;
			}
			if (source.isTarget)
			{
				delay = 4f;
			}
		}

		public void Refresh()
		{
			float num = Time.timeSinceLevelLoad - pingTime;
			if (emitter == null || num >= delay || emitter.disabled || SceneSingleton<CombatHUD>.i.aircraft == null || SceneSingleton<CombatHUD>.i.aircraft.disabled)
			{
				UnityEngine.Object.Destroy(vectorImage.gameObject);
				SceneSingleton<DynamicMap>.i.radarVisualizations.Remove(this);
				return;
			}
			Vector3 vector = emitter.GlobalPosition().AsVector3() * SceneSingleton<DynamicMap>.i.mapDisplayFactor;
			vectorImage.transform.localPosition = new Vector3(vector.x, vector.z, 0f);
			Vector3 vector2 = SceneSingleton<DynamicMap>.i.iconLookup[SceneSingleton<CombatHUD>.i.aircraft].transform.position - vectorImage.transform.position;
			float z = (0f - Mathf.Atan2(vector2.x, vector2.y)) * 57.29578f;
			vectorImage.transform.eulerAngles = new Vector3(0f, 0f, z);
			vectorImage.transform.localScale = new Vector3(1f, vector2.magnitude, 1f) / SceneSingleton<DynamicMap>.i.iconLayer.transform.lossyScale.x;
			Color color = vectorImage.color;
			color.a = Mathf.Lerp(color.a, 0f, num * 0.05f);
			vectorImage.color = color;
		}
	}

	public FactionHQ HQ;

	public GameObject viewIndicator;

	public GameObject mapImage;

	public Image mapBackground;

	public float mapScaleMinimized;

	public float mapScaleMaximized;

	public float mapScaleCurrent;

	public float mapLastUpdated;

	private Vector2 mapDimensions;

	public Transform mapScaleCenter;

	public Transform mapScaleProxy;

	public RectTransform mapTransform;

	public RectTransform hudMapAnchor;

	public RectTransform maximizedMapAnchor;

	public Canvas maximizedMapCanvas;

	private RectTransform mapRectTransform;

	private RectTransform backgroundRectTransform;

	public Vector2 mapCenter = Vector2.zero;

	private Vector2 positionOffset;

	private Vector2 stationaryOffset;

	private bool followingCamera;

	public GameObject iconLayer;

	public GameObject infoLayer;

	[SerializeField]
	private GameObject airbaseLayer;

	[SerializeField]
	private GameObject radarVisPrefab;

	[SerializeField]
	private GameObject notchLinePrefab;

	[SerializeField]
	private MapToolTip toolTip;

	private List<RadarMapVis> radarVisualizations = new List<RadarMapVis>();

	public UnitMapIcon unitMapIconPrefab;

	public AirbaseMapIcon airbaseMapIconPrefab;

	public GameObject targetMarker;

	public GameObject jammedMarker;

	private Dictionary<Unit, UnitMapIcon> iconLookup = new Dictionary<Unit, UnitMapIcon>();

	private Dictionary<Airbase, AirbaseMapIcon> airbaseIconLookup = new Dictionary<Airbase, AirbaseMapIcon>();

	[SerializeField]
	private Color backgroundMinimized;

	[SerializeField]
	private Color backgroundMaximized;

	[SerializeField]
	private Transform topRight;

	[SerializeField]
	private Transform bottomLeft;

	public List<MapIcon> selectedIcons = new List<MapIcon>();

	public GameObject mapWaypoint;

	public GameObject mapWaypointVector;

	public List<MapWaypoint> waypoints = new List<MapWaypoint>();

	public List<GlobalPosition> constructWaypoints = new List<GlobalPosition>();

	private Rewired.Player player;

	private float clickTime;

	private int iconIndex;

	public float mapDimension;

	public float mapDisplayFactor;

	private List<PersistentID> queuedIcons = new List<PersistentID>();

	public GridLabels gridLabels;

	private bool isJumping;

	private Vector3 mapTarget;

	[SerializeField]
	private ObjectiveMarkerManager objectiveMarkerManager;

	[Tooltip("When jumping on the map, how fast should the map follow the click")]
	[SerializeField]
	private float mapMoveMaxJumpSpeed = 10f;

	[SerializeField]
	private float jumpCameraFocusDistance = 300f;

	[SerializeField]
	private float jumpCameraFocusHeight = 300f;

	[SerializeField]
	private float jumpCameraFocusAngleDown = 30f;

	public static bool AllowedToOpen { get; set; }

	public List<MapIcon> mapIcons { get; private set; } = new List<MapIcon>();

	public List<MapMarker> mapMarkers { get; private set; } = new List<MapMarker>();

	public static bool mapMaximized { get; private set; }

	public static event Action onMapChanged;

	public event Action onMapGenerated;

	public event Action onMapMaximized;

	public event Action onMapMinimized;

	public event Action onAllDeselected;

	public event Action<Unit> onUnitSelected;

	public event Action<Unit> onUnitDeselected;

	public static event Action onShowTypesChanged;

	public static void EnableCanvas(bool enable)
	{
		SceneSingleton<DynamicMap>.i.gameObject.SetActive(enable);
	}

	public static void LoadMapImage(MapSettings mapSettings)
	{
		if (!(SceneSingleton<DynamicMap>.i == null))
		{
			Image component = SceneSingleton<DynamicMap>.i.mapImage.GetComponent<Image>();
			SceneSingleton<DynamicMap>.i.mapImage.GetComponent<RectTransform>().sizeDelta = Vector2.one * (mapSettings.MapSize / 81920f) * 900f;
			component.sprite = mapSettings.MapImage;
			SceneSingleton<DynamicMap>.i.mapDimensions = mapSettings.MapSize;
			SceneSingleton<DynamicMap>.i.gridLabels.SetupGrid(mapSettings.GridSizeX, mapSettings.GridSizeY, mapSettings.OffsetX, mapSettings.OffsetY);
			component.color = ThemeManager.Active.ColorTheme.MapBackground;
			SceneSingleton<DynamicMap>.i.gridLabels.UpdateGridColor();
		}
	}

	public static void ClampToMapEdge(Transform clampedTransform)
	{
		if (SceneSingleton<CombatHUD>.i.aircraft == null || !TryGetMapIcon(SceneSingleton<CombatHUD>.i.aircraft, out var mapIcon))
		{
			return;
		}
		Transform transform = mapIcon.transform;
		float num = SceneSingleton<DynamicMap>.i.mapScaleCurrent * 0.48f * (float)(Screen.height / 1080);
		Vector3 vector = clampedTransform.position - SceneSingleton<DynamicMap>.i.mapBackground.transform.position;
		Vector3 vector2 = clampedTransform.position - transform.position;
		Vector3 vector3 = transform.position - SceneSingleton<DynamicMap>.i.mapBackground.transform.position;
		float num2 = num + vector3.x;
		float num3 = num - vector3.x;
		float num4 = num - vector3.y;
		float num5 = num + vector3.y;
		if (!(Mathf.Abs(vector.x) < num) || !(Mathf.Abs(vector.y) < num))
		{
			float num6 = ((Mathf.Abs(vector2.x) > 0f) ? (vector2.y / vector2.x) : 1000000f);
			if (num6 == 0f)
			{
				num6 = 1E-06f;
			}
			vector2 = ((vector2.x >= 0f) ? ((!(num6 < num4 / num3) || !(num6 > 0f - num5 / num3)) ? ((vector2.y > 0f) ? new Vector3(num4 / num6, num4, 0f) : new Vector3(0f - num5 / num6, 0f - num5, 0f)) : new Vector3(num3, num3 * num6, 0f)) : ((!(num6 < num5 / num2) || !(num6 > (0f - num4) / num2)) ? ((vector2.y > 0f) ? new Vector3(num4 / num6, num4, 0f) : new Vector3(0f - num5 / num6, 0f - num5, 0f)) : new Vector3(0f - num2, (0f - num2) * num6, 0f)));
			clampedTransform.position = transform.position + vector2;
		}
	}

	public static bool TryGetMapIcon(Unit unit, out UnitMapIcon mapIcon)
	{
		return SceneSingleton<DynamicMap>.i.iconLookup.TryGetValue(unit, out mapIcon);
	}

	protected override void Awake()
	{
		base.Awake();
		AllowedToOpen = true;
		player = ReInput.players.GetPlayer(0);
		ThemeManager.ThemeGroupChanged += DynamicMap_OnThemeGroupChanged;
		EnableCanvas(enable: false);
		mapMaximized = false;
		mapRectTransform = GetComponent<RectTransform>();
		backgroundRectTransform = mapBackground.GetComponent<RectTransform>();
		mapBackground.color = backgroundMaximized;
		HideTooltip();
	}

	private void OnEnable()
	{
		NetworkManagerNuclearOption.i.Client.Disconnected.AddListener(DynamicMap_OnStopClient);
	}

	public GameObject ShowNotchLine()
	{
		if (!TryGetMapIcon(SceneSingleton<CombatHUD>.i.aircraft, out var _))
		{
			return null;
		}
		return UnityEngine.Object.Instantiate(notchLinePrefab, iconLayer.transform);
	}

	private static void DynamicMap_OnThemeGroupChanged()
	{
		SceneSingleton<DynamicMap>.i.mapImage.GetComponent<Image>().color = ThemeManager.Active.ColorTheme.MapBackground;
		SceneSingleton<DynamicMap>.i.gridLabels.UpdateGridColor();
	}

	public void UnselectAll()
	{
		selectedIcons.Clear();
		ClearWaypoints();
		foreach (MapIcon mapIcon in mapIcons)
		{
			if (mapIcon != null)
			{
				mapIcon.DeselectIcon();
			}
		}
		foreach (KeyValuePair<Airbase, AirbaseMapIcon> item in airbaseIconLookup)
		{
			if (item.Value != null)
			{
				item.Value.DeselectIcon();
			}
		}
		this.onAllDeselected?.Invoke();
	}

	public void Maximize()
	{
		if (!AllowedToOpen)
		{
			return;
		}
		EnableCanvas(enable: true);
		if (!mapMaximized)
		{
			followingCamera = true;
			positionOffset = Vector2.zero;
			Vector3 vector = SceneSingleton<CameraStateManager>.i.transform.position.ToGlobalPosition().AsVector3() * mapDisplayFactor;
			stationaryOffset = new Vector2(vector.x, vector.z);
			maximizedMapCanvas.gameObject.SetActive(value: true);
			base.transform.SetParent(maximizedMapAnchor);
			base.transform.localScale = Vector3.one;
			base.transform.position = maximizedMapAnchor.position;
			mapRectTransform.sizeDelta = Vector2.one * mapScaleMaximized;
			backgroundRectTransform.sizeDelta = mapRectTransform.sizeDelta + new Vector2(20f, 20f);
			mapMaximized = true;
			mapBackground.enabled = true;
			mapImage.transform.SetParent(mapBackground.transform);
			if (SceneSingleton<CameraStateManager>.i.currentState == SceneSingleton<CameraStateManager>.i.cockpitState)
			{
				FlightHud.EnableCanvas(enable: false);
			}
			if (ShouldShowAirbase())
			{
				ShowAirbases();
			}
			else
			{
				HideAirbases();
			}
			CursorManager.SetFlag(CursorFlags.Map, value: true);
			if (GameManager.gameState != GameState.Editor)
			{
				SceneSingleton<GameplayUI>.i.CheckShowSpectatorPanel();
				if (GameManager.GetLocalHQ(out var _))
				{
					SceneSingleton<GameplayUI>.i.ShowSelectAirbase();
				}
			}
			if (SceneSingleton<MapOptions>.i.showGridLabels && !gridLabels.LabelShown)
			{
				gridLabels.Maximize(maximized: true);
			}
			this.onMapMaximized?.Invoke();
		}
		mapScaleCurrent = mapScaleMaximized;
		mapDisplayFactor = mapScaleMaximized / mapDimension;
		CenterMinimizedMap();
	}

	private bool ShouldShowAirbase()
	{
		if (!GameManager.GetLocalPlayer<NuclearOption.Networking.Player>(out var localPlayer))
		{
			return false;
		}
		Aircraft aircraft = localPlayer.Aircraft;
		if (aircraft != null && !aircraft.disabled)
		{
			return false;
		}
		if (localPlayer.AircraftSpawnPending)
		{
			return false;
		}
		return true;
	}

	public void Minimize()
	{
		if (mapMaximized)
		{
			ClearWaypoints();
			SceneSingleton<GameplayUI>.i.HideSpectatorPanel();
			base.transform.SetParent(hudMapAnchor);
			maximizedMapCanvas.gameObject.SetActive(value: false);
			base.transform.localScale = Vector3.one;
			base.transform.localPosition = Vector3.zero;
			mapImage.transform.SetParent(mapScaleCenter.transform);
			mapScaleCenter.transform.localScale = new Vector3(2f, 2f, 2f);
			mapImage.transform.SetParent(mapBackground.transform);
			mapRectTransform.sizeDelta = Vector2.one * mapScaleMinimized;
			backgroundRectTransform.sizeDelta = mapRectTransform.sizeDelta + new Vector2(20f, 20f);
			mapBackground.enabled = false;
			mapMaximized = false;
			HideTooltip();
			if (SceneSingleton<CameraStateManager>.i.currentState == SceneSingleton<CameraStateManager>.i.cockpitState)
			{
				FlightHud.EnableCanvas(enable: true);
			}
			CursorManager.SetFlag(CursorFlags.Map, value: false);
			if (gridLabels.LabelShown)
			{
				gridLabels.Maximize(maximized: false);
			}
			this.onMapMinimized?.Invoke();
		}
		SceneSingleton<GameplayUI>.i.HideSelectAirbase();
		EnableCanvas(SceneSingleton<CombatHUD>.i.aircraft != null && CameraStateManager.cameraMode == CameraMode.cockpit);
		mapBackground.color = backgroundMinimized;
		mapScaleCurrent = mapScaleMinimized;
		mapDisplayFactor = mapScaleMaximized / mapDimension;
	}

	private void OnDestroy()
	{
		ThemeManager.ThemeGroupChanged -= DynamicMap_OnThemeGroupChanged;
		mapMaximized = false;
		CursorManager.SetFlag(CursorFlags.Map, value: false);
	}

	public void CenterMap()
	{
		Vector3 vector = SceneSingleton<CameraStateManager>.i.transform.GlobalPosition().AsVector3() * mapDisplayFactor;
		mapImage.transform.localPosition = (mapImage.transform.right * (0f - vector.x) + mapImage.transform.up * (0f - vector.z)) * mapImage.transform.localScale.x * mapBackground.transform.localScale.x;
	}

	public void ClearMap()
	{
		UnselectAll();
		foreach (MapIcon mapIcon in mapIcons)
		{
			mapIcon.RemoveIcon();
		}
		foreach (KeyValuePair<Airbase, AirbaseMapIcon> item in airbaseIconLookup)
		{
			item.Value.RemoveIcon();
		}
		foreach (MapMarker mapMarker in mapMarkers)
		{
			mapMarker.Remove();
		}
		mapIcons.Clear();
		airbaseIconLookup.Clear();
		iconLookup.Clear();
		queuedIcons.Clear();
		mapMarkers.Clear();
	}

	public void GenerateMap(FactionHQ HQ, bool showAirbases)
	{
		if (HQ == null)
		{
			foreach (Unit allUnit in UnitRegistry.allUnits)
			{
				AddIcon(allUnit.persistentID);
			}
			return;
		}
		foreach (KeyValuePair<PersistentID, TrackingInfo> item in HQ.trackingDatabase)
		{
			AddIcon(item.Key);
		}
		foreach (PersistentID factionUnit in HQ.factionUnits)
		{
			AddIcon(factionUnit);
		}
		if (!showAirbases)
		{
			return;
		}
		foreach (Airbase airbasis in HQ.GetAirbases())
		{
			AddIcon(airbasis);
		}
	}

	public void RefreshAirbases()
	{
		foreach (KeyValuePair<Airbase, AirbaseMapIcon> item in airbaseIconLookup)
		{
			UnityEngine.Object.Destroy(item.Value.gameObject);
		}
		airbaseIconLookup.Clear();
		if (HQ == null)
		{
			return;
		}
		foreach (Airbase airbasis in HQ.GetAirbases())
		{
			AddIcon(airbasis);
		}
	}

	public void ShowAirbases()
	{
		foreach (KeyValuePair<Airbase, AirbaseMapIcon> item in airbaseIconLookup)
		{
			item.Value.iconImage.enabled = true;
		}
	}

	public void HideAirbases()
	{
		foreach (KeyValuePair<Airbase, AirbaseMapIcon> item in airbaseIconLookup)
		{
			item.Value.iconImage.enabled = false;
		}
	}

	public void ShowRadarPing(Aircraft.OnRadarWarning source)
	{
		Image component = UnityEngine.Object.Instantiate(radarVisPrefab, iconLayer.transform).GetComponent<Image>();
		Color color = ThemeManager.Active.ColorTheme.TargetPing.WithAlpha(0.5f);
		Color color2 = ThemeManager.Active.ColorTheme.DetectedPing.WithAlpha(0.25f);
		Color color3 = ThemeManager.Active.ColorTheme.PassivePing.WithAlpha(0.125f);
		Color color4 = (source.detected ? color2 : color3);
		if (source.isTarget)
		{
			color4 = color;
		}
		component.color = color4;
		radarVisualizations.Add(new RadarMapVis(component, source));
	}

	public GlobalPosition GetCursorCoordinates()
	{
		Vector3 vector = Input.mousePosition - mapImage.transform.position;
		vector *= mapDimension / (900f * mapImage.transform.lossyScale.x);
		return new GlobalPosition(vector.x, 0f, vector.y);
	}

	public bool TryGetCursorCoordinates(out GlobalPosition position)
	{
		bool flag = IsCursorInMapRectangle();
		position = (flag ? GetCursorCoordinates() : default(GlobalPosition));
		return flag;
	}

	public bool IsCursorInMapRectangle()
	{
		return RectTransformUtility.RectangleContainsScreenPoint(mapBackground.rectTransform, Input.mousePosition, null);
	}

	public void SetFaction(FactionHQ HQ)
	{
		this.onMapGenerated?.Invoke();
		if (this.HQ == HQ)
		{
			return;
		}
		this.HQ = HQ;
		ClearMap();
		if (HQ != null)
		{
			GenerateMap(HQ, showAirbases: true);
			objectiveMarkerManager.Initialize(iconLayer.transform);
			return;
		}
		foreach (FactionHQ allHQ in FactionRegistry.GetAllHQs())
		{
			GenerateMap(allHQ, showAirbases: false);
		}
		GenerateMap(null, showAirbases: false);
	}

	private void DynamicMap_OnFactionChanged(Unit unit)
	{
		if (iconLookup.TryGetValue(unit, out var value))
		{
			value.UpdateColor();
		}
	}

	public void UpdateUnitFaction(Unit unit)
	{
		if (iconLookup.TryGetValue(unit, out var value))
		{
			value.UpdateColor();
		}
	}

	public float MetersToPixels()
	{
		return mapTransform.rect.width / mapDimension;
	}

	public void FlagIncomingMissile(Unit missileUnit)
	{
		GetOrAddIcon(missileUnit).SetMissileWarning();
	}

	public void ClearIncomingMissile(Unit missileUnit)
	{
		if (missileUnit != null && iconLookup.ContainsKey(missileUnit))
		{
			iconLookup[missileUnit].ClearMissileWarning();
		}
	}

	public bool TryGetIcon(Unit unit, out UnitMapIcon icon)
	{
		return iconLookup.TryGetValue(unit, out icon);
	}

	public UnitMapIcon GetOrAddIcon(Unit unit)
	{
		if (iconLookup.TryGetValue(unit, out var value))
		{
			return value;
		}
		AddIcon(unit.persistentID);
		return iconLookup[unit];
	}

	public void AddIcon(PersistentID id)
	{
		queuedIcons.Add(id);
		SpawnQueuedIcons();
	}

	public void AddIcon(Airbase airbase)
	{
		AirbaseMapIcon airbaseMapIcon = UnityEngine.Object.Instantiate(airbaseMapIconPrefab);
		airbaseMapIcon.transform.SetParent(airbaseLayer.transform);
		airbaseMapIcon.SetIcon(airbase);
		airbaseIconLookup.Add(airbase, airbaseMapIcon);
	}

	public void SelectIcon(Unit unit)
	{
		if (!SceneSingleton<TargetListSelector>.i.CheckExclusions(unit))
		{
			if (!iconLookup.ContainsKey(unit))
			{
				SpawnQueuedIcons();
			}
			if (iconLookup.TryGetValue(unit, out var value) && !selectedIcons.Contains(value))
			{
				value.SelectIcon();
				selectedIcons.Add(iconLookup[unit]);
				this.onUnitSelected?.Invoke(unit);
			}
			else
			{
				ColorLog<DynamicMap>.LogError($"No icon found for unit:{unit}");
			}
		}
	}

	public void DeselectIcon(Unit unit)
	{
		if (iconLookup.TryGetValue(unit, out var value))
		{
			value.DeselectIcon();
			selectedIcons.Remove(value);
			this.onUnitDeselected?.Invoke(unit);
		}
		else if (unit != null)
		{
			ColorLog<DynamicMap>.LogError($"No icon found for unit:{unit}");
		}
	}

	public void SelectIcon(Airbase airbase)
	{
		if (airbaseIconLookup.TryGetValue(airbase, out var value))
		{
			value.SelectIcon();
			selectedIcons.Add(value);
		}
		else
		{
			ColorLog<DynamicMap>.LogError($"No icon found for airbase:{airbase}");
		}
	}

	public void DeselectAllIcons()
	{
		foreach (KeyValuePair<Unit, UnitMapIcon> item in iconLookup)
		{
			item.Value.DeselectIcon();
		}
		foreach (KeyValuePair<Airbase, AirbaseMapIcon> item2 in airbaseIconLookup)
		{
			item2.Value.DeselectIcon();
		}
		foreach (MapMarker mapMarker in mapMarkers)
		{
			if (mapMarker is TargetMarker)
			{
				mapMarker.Remove();
			}
		}
		selectedIcons.Clear();
	}

	public void HighlightIcon(Unit unit)
	{
		if (iconLookup.ContainsKey(unit))
		{
			iconLookup[unit].HighlightIcon();
		}
	}

	public void RemoveIcon(MapIcon mapIcon)
	{
		if (selectedIcons.Contains(mapIcon))
		{
			selectedIcons.Remove(mapIcon);
		}
		mapIcons.Remove(mapIcon);
		if (mapIcon is UnitMapIcon unitMapIcon)
		{
			iconLookup.Remove(unitMapIcon.unit);
		}
	}

	public void DisplayExclusionZone(ExclusionZone exclusionZone)
	{
		GameObject icon = UnityEngine.Object.Instantiate(GameAssets.i.exclusionZoneDisplay, iconLayer.transform);
		icon.transform.localPosition = new Vector3(exclusionZone.position.x, exclusionZone.position.z, 0f) * SceneSingleton<DynamicMap>.i.mapDisplayFactor;
		icon.transform.localScale = Vector3.one * (exclusionZone.radius * SceneSingleton<DynamicMap>.i.mapDisplayFactor);
		if (UnitRegistry.TryGetUnit(exclusionZone.sourceId, out var unit))
		{
			unit.onDisableUnit += delegate
			{
				UnityEngine.Object.Destroy(icon);
			};
		}
		else
		{
			UnityEngine.Object.Destroy(icon, 30f);
		}
	}

	private void SpawnQueuedIcons()
	{
		for (int num = queuedIcons.Count - 1; num >= 0; num--)
		{
			if (UnitRegistry.TryGetUnit(queuedIcons[num], out var unit))
			{
				if (!unit.disabled && unit.definition.mapIconSize > 0f && !iconLookup.ContainsKey(unit))
				{
					SpawnIconForUnit(unit);
				}
				queuedIcons.RemoveAt(num);
			}
		}
	}

	private UnitMapIcon SpawnIconForUnit(Unit unit)
	{
		UnitMapIcon unitMapIcon = UnityEngine.Object.Instantiate(unitMapIconPrefab);
		unitMapIcon.transform.SetParent(iconLayer.transform);
		unitMapIcon.transform.localScale = Vector3.one;
		unitMapIcon.SetIcon(unit);
		mapIcons.Add(unitMapIcon);
		iconLookup.Add(unit, unitMapIcon);
		SceneSingleton<CombatHUD>.i.CreateMarker(unit.persistentID);
		return unitMapIcon;
	}

	private void DynamicMap_OnStopClient(ClientStoppedReason _)
	{
		NetworkManagerNuclearOption.i.Client.Disconnected.RemoveListener(DynamicMap_OnStopClient);
		if (this != null)
		{
			base.enabled = false;
		}
	}

	public static void ClearJammingEffects()
	{
		SceneSingleton<CombatHUD>.i.jamAccumulation = 0f;
		foreach (MapIcon mapIcon in SceneSingleton<DynamicMap>.i.mapIcons)
		{
			if (mapIcon is UnitMapIcon unitMapIcon)
			{
				unitMapIcon.ClearJammingDistortion();
			}
		}
	}

	private void UpdateIcons()
	{
		float mapInverseScale = 1f / mapImage.transform.localScale.x;
		int num = Mathf.Max(1, (int)((float)mapIcons.Count * 0.2f));
		int num2 = Mathf.Min(iconIndex + num, mapIcons.Count);
		for (int i = iconIndex; i < num2; i++)
		{
			mapIcons[i].UpdateIcon(mapDisplayFactor, mapInverseScale, mapImage.transform, mapMaximized);
		}
		iconIndex = ((num2 < mapIcons.Count) ? num2 : 0);
		if (!(SceneSingleton<CombatHUD>.i.aircraft != null) || SceneSingleton<CombatHUD>.i.aircraft.disabled || !(SceneSingleton<CombatHUD>.i.jamAccumulation > 0f))
		{
			return;
		}
		float jamAccumulation = SceneSingleton<CombatHUD>.i.jamAccumulation;
		for (int j = 0; j < mapIcons.Count; j++)
		{
			if (mapIcons[j] is UnitMapIcon unitMapIcon)
			{
				unitMapIcon.JammingDistortion(jamAccumulation);
			}
		}
	}

	private void UpdateMap()
	{
		mapLastUpdated = Time.realtimeSinceStartup;
		float mapInverseScale = 1f / mapImage.transform.localScale.x;
		foreach (KeyValuePair<Airbase, AirbaseMapIcon> item in airbaseIconLookup)
		{
			item.Value.UpdateIcon(mapDisplayFactor, mapInverseScale, mapImage.transform, mapMaximized);
		}
		for (int num = radarVisualizations.Count - 1; num >= 0; num--)
		{
			radarVisualizations[num].Refresh();
		}
		if (SceneSingleton<MapOptions>.i.showGridLabels != gridLabels.LabelEnabled)
		{
			gridLabels.ShowLabels(SceneSingleton<MapOptions>.i.showGridLabels);
		}
		if (waypoints.Count > 0)
		{
			foreach (MapWaypoint waypoint in waypoints)
			{
				waypoint.UpdateMarker();
			}
		}
		DynamicMap.onMapChanged?.Invoke();
	}

	public void DisplayTooltip(MapIcon icon)
	{
		toolTip.ShowTooltip(icon);
	}

	public void HideTooltip()
	{
		toolTip.ShowTooltip(null);
	}

	public void ClearWaypoints()
	{
		foreach (MapWaypoint waypoint in waypoints)
		{
			UnityEngine.Object.Destroy(waypoint.marker);
			UnityEngine.Object.Destroy(waypoint.vector);
		}
		waypoints.Clear();
		constructWaypoints.Clear();
	}

	private void Update()
	{
		SpawnQueuedIcons();
		UpdateIcons();
		if (Time.realtimeSinceStartup - mapLastUpdated >= 0.1f)
		{
			UpdateMap();
		}
		if (isJumping)
		{
			JumptoTarget();
		}
		else if (mapMaximized)
		{
			MapControls();
		}
		else
		{
			CenterMinimizedMap();
		}
	}

	private void CenterMinimizedMap()
	{
		Vector3 vector = SceneSingleton<CameraStateManager>.i.transform.position.ToGlobalPosition().AsVector3() * mapDisplayFactor;
		if (SceneSingleton<CombatHUD>.i != null && SceneSingleton<CombatHUD>.i.aircraft != null)
		{
			Vector3 forward = SceneSingleton<CombatHUD>.i.aircraft.transform.forward;
			forward.y = 0f;
			Vector3 vector2 = vector + forward.normalized * mapDisplayFactor * 4000f;
			mapImage.transform.eulerAngles = new Vector3(0f, 0f, SceneSingleton<CombatHUD>.i.aircraft.transform.eulerAngles.y);
			mapImage.transform.localPosition = (mapImage.transform.right * (0f - vector2.x) + mapImage.transform.up * (0f - vector2.z)) * mapImage.transform.localScale.x * mapBackground.transform.localScale.x;
			viewIndicator.transform.localPosition = new Vector3(vector.x, vector.z, 0f);
			viewIndicator.transform.eulerAngles = new Vector3(0f, 0f, mapImage.transform.eulerAngles.z - SceneSingleton<CameraStateManager>.i.transform.eulerAngles.y);
		}
	}

	private void SelectFromMap()
	{
		float num = 10000f;
		MapIcon mapIcon = null;
		Vector3 mousePosition = Input.mousePosition;
		foreach (UnitMapIcon value in iconLookup.Values)
		{
			float num2 = FastMath.SquareDistance(mousePosition, value.transform.position);
			if (!(num2 > num) && value.iconImage.raycastTarget && !SceneSingleton<TargetListSelector>.i.CheckExclusions(value.unit) && num2 < num && value.iconImage.raycastTarget)
			{
				num = num2;
				mapIcon = value;
			}
		}
		if (mapIcon != null && mapIcon.gameObject.activeSelf)
		{
			mapIcon.ClickIcon(MapIcon.ClickSource.Controller);
		}
	}

	private void MapControls()
	{
		float num = player.GetAxis("Zoom View") * 0.05f;
		if (num != 0f)
		{
			float x = mapScaleCenter.transform.localScale.x;
			x *= num + 1f;
			x = Mathf.Clamp(x, 1f, 20f);
			SetZoomLevel(x);
		}
		float axis = player.GetAxis("Move Map Horizontal");
		float axis2 = player.GetAxis("Move Map Vertical");
		if (axis != 0f || axis2 != 0f)
		{
			float num2 = 300f * Time.unscaledDeltaTime / mapScaleCenter.localScale.x;
			positionOffset += new Vector2(axis * num2, axis2 * num2);
			DynamicMap.onMapChanged?.Invoke();
		}
		if (Input.GetMouseButton(0))
		{
			clickTime += Time.deltaTime;
			float num3 = player.GetAxis("Pan View") * -1f;
			float axis3 = player.GetAxis("Tilt View");
			float num4 = 150f * Mathf.Min(Time.unscaledDeltaTime, 0.03f) / mapScaleCenter.localScale.x;
			positionOffset += new Vector2(num3 * num4, axis3 * num4);
			if (positionOffset.magnitude > 0f)
			{
				DynamicMap.onMapChanged?.Invoke();
			}
		}
		else
		{
			clickTime = 0f;
		}
		if (player.GetButtonDown("Select"))
		{
			SelectFromMap();
		}
		if (mapMaximized && player.GetButtonDown("Jump Map") && !GameManager.GetLocalAircraft(out var _) && TryGetCursorCoordinates(out var position))
		{
			JumpCameraTo(position);
		}
		followingCamera = positionOffset == Vector2.zero;
		if (mapMaximized && selectedIcons.Count > 0 && Input.GetMouseButtonDown(1) && GameManager.gameState != GameState.Editor)
		{
			if (!Input.GetKey(KeyCode.LeftShift))
			{
				ClearWaypoints();
			}
			MapIcon mapIcon = selectedIcons[0];
			if (mapIcon is UnitMapIcon unitMapIcon && GetFactionMode(unitMapIcon.unit.NetworkHQ) == FactionMode.Friendly && unitMapIcon.unit is ICommandable)
			{
				Vector3 previousWaypoint = ((waypoints.Count > 0) ? waypoints[waypoints.Count - 1].marker.transform.localPosition : mapIcon.transform.localPosition);
				constructWaypoints.Add(GetCursorCoordinates());
				MapWaypoint item = new MapWaypoint(Input.mousePosition, previousWaypoint, UnityEngine.Object.Instantiate(mapWaypoint, iconLayer.transform), UnityEngine.Object.Instantiate(mapWaypointVector, iconLayer.transform));
				if (waypoints.Count > 0)
				{
					UnityEngine.Object.Destroy(waypoints[waypoints.Count - 1].marker);
				}
				waypoints.Add(item);
				foreach (MapIcon selectedIcon in selectedIcons)
				{
					if (selectedIcon is UnitMapIcon { unit: var unit } && !(unit == null) && !unit.disabled && unit is ICommandable commandable && !(commandable.UnitCommand == null) && GetFactionMode(unit.NetworkHQ) == FactionMode.Friendly)
					{
						UnitCommand unitCommand = commandable.UnitCommand;
						List<GlobalPosition> list = constructWaypoints;
						unitCommand.SetDestination(list[list.Count - 1], playerCommand: true);
					}
				}
			}
		}
		if (!(SceneSingleton<CameraStateManager>.i != null))
		{
			return;
		}
		Vector3 vector = SceneSingleton<CameraStateManager>.i.transform.position.ToGlobalPosition().AsVector3() * mapDisplayFactor;
		float x2 = mapImage.transform.localScale.x;
		if (followingCamera)
		{
			Vector2 vector2 = new Vector2(vector.x, vector.z);
			if (SceneSingleton<CombatHUD>.i.aircraft != null && !SceneSingleton<CombatHUD>.i.aircraft.disabled)
			{
				Vector3 forward = SceneSingleton<CombatHUD>.i.aircraft.transform.forward;
				Vector2 vector3 = new Vector2(forward.x, forward.z);
				stationaryOffset = vector2 + 5000f * mapDisplayFactor * vector3;
			}
			else
			{
				stationaryOffset = Vector2.MoveTowards(stationaryOffset, vector2, mapMoveMaxJumpSpeed);
			}
		}
		mapImage.transform.localEulerAngles = Vector3.zero;
		mapScaleCenter.transform.localEulerAngles = Vector3.zero;
		Vector2 pos = -stationaryOffset - positionOffset;
		((RectTransform)mapBackground.transform).rect.ClampPos(ref pos, 2f);
		positionOffset = -stationaryOffset - pos;
		mapImage.transform.localPosition = pos * x2;
		viewIndicator.transform.localPosition = new Vector3(vector.x, vector.z, 0f);
		viewIndicator.transform.eulerAngles = new Vector3(0f, 0f, mapImage.transform.eulerAngles.z - SceneSingleton<CameraStateManager>.i.transform.eulerAngles.y);
	}

	private void JumpCameraTo(GlobalPosition pos)
	{
		Vector3 vector = pos.ToLocalPosition();
		float num = Datum.LocalSeaY;
		if (Physics.Raycast(vector + new Vector3(0f, 10000f, 0f), Vector3.down, out var hitInfo, 10000f, PhysicsLayers.Everything))
		{
			num = hitInfo.point.y;
		}
		float y = num + jumpCameraFocusHeight;
		Vector3 eulerAngles = SceneSingleton<CameraStateManager>.i.transform.rotation.eulerAngles;
		Quaternion value = Quaternion.Euler(jumpCameraFocusAngleDown, eulerAngles.y, 0f);
		Vector3 position = new Vector3(vector.x, y, vector.z);
		SceneSingleton<CameraStateManager>.i.FocusPosition(position, value, jumpCameraFocusDistance);
	}

	public void SetMapTarget(GlobalPosition newTarget)
	{
		mapTarget = newTarget.AsVector3() * mapDisplayFactor;
		isJumping = true;
	}

	private void JumptoTarget()
	{
		Vector3 localPosition = mapImage.transform.localPosition;
		Vector3 b = (mapImage.transform.right * (0f - mapTarget.x) + mapImage.transform.up * (0f - mapTarget.z)) * mapImage.transform.localScale.x * mapBackground.transform.localScale.x;
		mapImage.transform.localPosition = Vector3.Lerp(localPosition, b, 0.05f);
		if (Vector3.Distance(localPosition, b) < 0.1f)
		{
			mapTarget = Vector3.zero;
			isJumping = false;
		}
	}

	public void SetZoomLevel(float zoomLevel)
	{
		mapScaleCenter.position = Input.mousePosition;
		mapScaleProxy.localScale = mapScaleCenter.localScale;
		mapScaleProxy.position = mapImage.transform.position;
		mapScaleProxy.transform.SetParent(mapScaleCenter);
		mapScaleCenter.localScale = Vector3.one * zoomLevel;
		mapScaleProxy.SetParent(mapBackground.transform);
		mapImage.transform.localScale = mapScaleProxy.localScale;
		mapImage.transform.position = mapScaleProxy.position;
		DynamicMap.onMapChanged?.Invoke();
	}

	public float GetZoomLevel()
	{
		return mapScaleCenter.localScale.x;
	}

	public void ShowTypeChanged()
	{
		DynamicMap.onShowTypesChanged?.Invoke();
	}

	public static FactionMode GetFactionMode(FactionHQ hq = null, bool checkNoFactionBeforeSpectator = false)
	{
		if (checkNoFactionBeforeSpectator)
		{
			if (hq == null)
			{
				return FactionMode.NoFaction;
			}
			if (SceneSingleton<DynamicMap>.i.HQ == null)
			{
				return FactionMode.Spectator;
			}
		}
		else
		{
			if (SceneSingleton<DynamicMap>.i.HQ == null)
			{
				return FactionMode.Spectator;
			}
			if (hq == null)
			{
				return FactionMode.NoFaction;
			}
		}
		if (!(SceneSingleton<DynamicMap>.i.HQ == hq))
		{
			return FactionMode.Enemy;
		}
		return FactionMode.Friendly;
	}

	public static bool IsFactionMode(FactionHQ hq, FactionMode flags)
	{
		return (GetFactionMode(hq) & flags) != 0;
	}
}
