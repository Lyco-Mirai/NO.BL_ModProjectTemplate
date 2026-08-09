using System;
using System.Collections.Generic;
using NuclearOption.Networking;
using NuclearOption.UIStyleSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CombatHUD : SceneSingleton<CombatHUD>
{
	private class HitMarker
	{
		public float hitTime;

		public GameObject marker;

		public Unit hitUnit;

		public Vector3 hitOffset;

		private Vector3 scaleVector = new Vector3(1f, 1f, 0f);

		public HitMarker(float hitTime, GameObject marker, Unit hitUnit, Vector3 hitOffset)
		{
			this.hitTime = hitTime;
			this.marker = marker;
			this.hitUnit = hitUnit;
			this.hitOffset = hitOffset;
			marker.SetActive(value: false);
		}

		public void SetMarker(GlobalPosition globalPosition, Unit hitUnit)
		{
			hitTime = Time.timeSinceLevelLoad;
			this.hitUnit = hitUnit;
			hitOffset = globalPosition - hitUnit.GlobalPosition();
			Position();
		}

		public void Position()
		{
			if (hitUnit == null || Time.timeSinceLevelLoad - hitTime > 0.2f)
			{
				marker.SetActive(value: false);
				return;
			}
			Vector3 lhs = hitUnit.transform.position + hitOffset - SceneSingleton<CameraStateManager>.i.transform.position;
			marker.SetActive(Vector3.Dot(lhs, SceneSingleton<CameraStateManager>.i.transform.forward) > 0f);
			if (marker.activeSelf)
			{
				marker.transform.position = Vector3.Scale(SceneSingleton<CameraStateManager>.i.mainCamera.WorldToScreenPoint(hitUnit.transform.position + hitOffset), scaleVector);
			}
		}
	}

	public Transform iconLayer;

	public Aircraft aircraft;

	[SerializeField]
	private GameObject unitMarker;

	public Image targetDesignator;

	[SerializeField]
	private GameObject MissileUI;

	[SerializeField]
	private GameObject BoresightUI;

	[SerializeField]
	private GameObject BombingUI;

	[SerializeField]
	private GameObject TurretUI;

	[SerializeField]
	private GameObject CargoUI;

	[SerializeField]
	private GameObject LaserGuidedUI;

	[SerializeField]
	private GameObject SlingUI;

	[SerializeField]
	private GameObject NoWeaponUI;

	[SerializeField]
	private GameObject topRightPanel;

	[SerializeField]
	private WeaponStatus weaponStatus;

	[SerializeField]
	private GameObject countermeasureBackground;

	[SerializeField]
	private Image countermeasureImage;

	[SerializeField]
	private TextMeshProUGUI countermeasureName;

	[SerializeField]
	private TextMeshProUGUI countermeasureAmmo;

	[SerializeField]
	private ThreatList threatList;

	[SerializeField]
	private Image targetArrow;

	public Sprite minimizedFriendly;

	public Sprite minimizedHostile;

	[SerializeField]
	private Transform targetArrowTail;

	[SerializeField]
	private TextMeshProUGUI targetText;

	[SerializeField]
	private TextMeshProUGUI targetInfo;

	[SerializeField]
	private Transform aircraftActionsReportAnchor;

	[SerializeField]
	private AudioClip selectSound;

	[SerializeField]
	private AudioClip deselectSound;

	[SerializeField]
	private AudioClip deselectAllSound;

	[SerializeField]
	private AudioClip weaponSwitchSound;

	[SerializeField]
	private AudioClip jammedSound;

	[SerializeField]
	private ObjectiveOverlayManager objectiveOverlay;

	[SerializeField]
	private AudioSource jammedSource;

	[SerializeField]
	[Range(0f, 1f)]
	private float jammedVolumeMultiplier;

	private List<HUDUnitMarker> markers = new List<HUDUnitMarker>();

	private Dictionary<Unit, HUDUnitMarker> markerLookup = new Dictionary<Unit, HUDUnitMarker>();

	private List<Unit> targetList;

	private float smoothVel;

	[SerializeField]
	private GameObject hitMarker;

	private List<HitMarker> hitMarkers = new List<HitMarker>();

	private float targetSelectTimer;

	private int iconIndex;

	private float lastHitMarker;

	[HideInInspector]
	public float jamAccumulation;

	private HUDWeaponState weaponState;

	private WeaponStation currentWeaponStation;

	public GameObject notchIndicatorPrefab;

	public GameObject rearmerDisplay;

	public bool landingMode { get; private set; }

	public bool HasTargets
	{
		get
		{
			List<Unit> list = targetList;
			if (list == null)
			{
				return false;
			}
			return list.Count > 0;
		}
	}

	public bool turretAutoControl { get; private set; }

	public static event Action onSetTurretAuto;

	public static event Action<CombatHUD> onSetAircraft;

	protected override void Awake()
	{
		base.Awake();
		hitMarkers.Add(new HitMarker(0f, hitMarker, null, Vector3.zero));
		for (int i = 0; i < 19; i++)
		{
			hitMarkers.Add(new HitMarker(0f, UnityEngine.Object.Instantiate(hitMarker, iconLayer), null, Vector3.zero));
		}
		PlayerSettings.OnApplyOptions += RefreshSettings;
	}

	private void OnDestroy()
	{
		PlayerSettings.OnApplyOptions -= RefreshSettings;
		ClearIcons();
	}

	public List<Unit> GetTargetList()
	{
		return targetList;
	}

	public void RefreshSettings()
	{
		targetText.fontSize = (int)PlayerSettings.overlayTextSize;
		targetInfo.fontSize = (int)PlayerSettings.hmdTextSize;
	}

	public void CreateMarker(PersistentID id)
	{
		if (!(aircraft == null) && UnitRegistry.TryGetUnit(id, out var unit) && aircraft != unit && !unit.disabled && !markerLookup.ContainsKey(unit) && !(unit is Scenery))
		{
			Image component = UnityEngine.Object.Instantiate(unitMarker, iconLayer).GetComponent<Image>();
			HUDUnitMarker hUDUnitMarker = new HUDUnitMarker(unit, component);
			markers.Add(hUDUnitMarker);
			markerLookup.Add(unit, hUDUnitMarker);
			hUDUnitMarker.AssessThreat(aircraft);
		}
	}

	public void RemoveMarker(HUDUnitMarker marker)
	{
		if (aircraft != null && !aircraft.disabled)
		{
			aircraft.weaponManager.RemoveTargetList(marker.unit);
		}
		markers.Remove(marker);
		markerLookup.Remove(marker.unit);
	}

	public void DisplayHit(GlobalPosition hitPosition, Unit hitUnit)
	{
		if (PlayerSettings.showHitMarkers && SceneSingleton<CameraStateManager>.i.currentState == SceneSingleton<CameraStateManager>.i.cockpitState && (hitUnit is GroundVehicle || hitUnit is Aircraft || !(hitUnit.maxRadius > 8f)))
		{
			HitMarker hitMarker = hitMarkers[0];
			hitMarkers.RemoveAt(0);
			hitMarker.SetMarker(hitPosition, hitUnit);
			hitMarkers.Add(hitMarker);
			if (!((double)(Time.timeSinceLevelLoad - lastHitMarker) < 0.05))
			{
				lastHitMarker = Time.timeSinceLevelLoad;
				SoundManager.PlayInterfaceOneShot(GameAssets.i.hitMarkerSound);
			}
		}
	}

	public void ClearIcons()
	{
		foreach (HUDUnitMarker marker in markers)
		{
			marker.RemoveIcon();
		}
		markerLookup.Clear();
		markers.Clear();
		targetArrow.enabled = false;
		targetText.enabled = false;
		jamAccumulation = 0f;
	}

	public void SetAircraft(Aircraft aircraft)
	{
		ClearIcons();
		this.aircraft = aircraft;
		landingMode = false;
		threatList.SetAircraft(aircraft);
		targetList = ((aircraft.weaponManager != null) ? aircraft.weaponManager.GetTargetList() : new List<Unit>());
		for (int i = 0; i < aircraft.NetworkHQ.factionUnits.Count; i++)
		{
			CreateMarker(aircraft.NetworkHQ.factionUnits[i]);
		}
		foreach (KeyValuePair<PersistentID, TrackingInfo> item in aircraft.NetworkHQ.trackingDatabase)
		{
			CreateMarker(item.Key);
		}
		SetPlayerFaction();
		UnityEngine.Object.Instantiate(GameAssets.i.aircraftActionsReport, aircraftActionsReportAnchor).GetComponent<AircraftActionsReport>().Initialize(aircraft);
		objectiveOverlay.Initialize(aircraft, iconLayer);
		CombatHUD.onSetAircraft?.Invoke(this);
		jamAccumulation = 0f;
		aircraft.onJam += CombatHUD_OnJam;
		turretAutoControl = true;
		FlightHud.ResetAircraft();
		PlayerSettings.OnApplyOptions += RefreshSettings;
	}

	public void RemoveAircraft()
	{
		if (aircraft != null)
		{
			aircraft.onJam -= CombatHUD_OnJam;
		}
		jamAccumulation = 0f;
		DynamicMap.ClearJammingEffects();
		aircraft = null;
		DeselectAll();
		PlayerSettings.OnApplyOptions -= RefreshSettings;
	}

	private void CombatHUD_OnJam(Unit.JamEventArgs e)
	{
		jamAccumulation += e.jamAmount * 0.5f;
	}

	public void SetPlayerFaction()
	{
		foreach (HUDUnitMarker marker in markers)
		{
			marker.AssessThreat(aircraft);
		}
	}

	public bool MarkerExists(Unit unit)
	{
		return markerLookup.ContainsKey(unit);
	}

	public void HighlightMarker(Unit unit)
	{
		markerLookup[unit].SetNew();
	}

	public void FlashMarker(Unit unit, bool flash)
	{
		if (!landingMode && !(unit == null) && !(aircraft == null))
		{
			if (flash)
			{
				CreateMarker(unit.persistentID);
			}
			if (markerLookup.TryGetValue(unit, out var value))
			{
				value.SetFlashing(flash);
			}
		}
	}

	public bool TryGetMarker(Unit unit, out HUDUnitMarker marker)
	{
		return markerLookup.TryGetValue(unit, out marker);
	}

	public void DisplayWeaponSafety(bool safetyEnabled)
	{
		SceneSingleton<HUDOptions>.i.AutomaticToggle(currentWeaponStation, safetyEnabled);
	}

	public void ToggleAutoControl()
	{
		if (aircraft.weaponManager.StationsWithTurrets() != 0)
		{
			turretAutoControl = !turretAutoControl;
			CombatHUD.onSetTurretAuto?.Invoke();
			string text = (turretAutoControl ? "engage at will" : "hold fire");
			SceneSingleton<AircraftActionsReport>.i.ReportText("Turrets set to " + text, 4f);
		}
	}

	public void ShowWeaponStation(WeaponStation weaponStation)
	{
		weaponStatus.SetCurrentStation(aircraft, weaponStation);
		if (weaponState != null)
		{
			UnityEngine.Object.Destroy(weaponState.gameObject);
		}
		weaponStatus.SetVisible(weaponStation != null);
		currentWeaponStation = weaponStation;
		if (weaponStation == null)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(NoWeaponUI, SceneSingleton<FlightHud>.i.GetHUDCenter());
			weaponState = gameObject.GetComponent<HUDWeaponState>();
			weaponState.SetHUDWeaponState(targetDesignator, aircraft, weaponStation);
		}
		else
		{
			GameObject gameObject = (weaponStation.HasTurret() ? UnityEngine.Object.Instantiate(TurretUI, SceneSingleton<FlightHud>.i.GetHUDCenter()) : (weaponStation.WeaponInfo.boresight ? UnityEngine.Object.Instantiate(BoresightUI, SceneSingleton<FlightHud>.i.GetHUDCenter()) : (weaponStation.WeaponInfo.bomb ? UnityEngine.Object.Instantiate(BombingUI, SceneSingleton<FlightHud>.i.GetHUDCenter()) : ((weaponStation.WeaponInfo.cargo || weaponStation.WeaponInfo.troops) ? UnityEngine.Object.Instantiate(CargoUI, SceneSingleton<FlightHud>.i.HMDCenter) : (weaponStation.WeaponInfo.sling ? UnityEngine.Object.Instantiate(SlingUI, SceneSingleton<FlightHud>.i.GetHUDCenter()) : ((!weaponStation.WeaponInfo.laserGuided) ? UnityEngine.Object.Instantiate(MissileUI, SceneSingleton<FlightHud>.i.GetHUDCenter()) : UnityEngine.Object.Instantiate(LaserGuidedUI, SceneSingleton<FlightHud>.i.GetHUDCenter())))))));
			weaponState = gameObject.GetComponent<HUDWeaponState>();
			weaponState.SetHUDWeaponState(targetDesignator, aircraft, weaponStation);
			SoundManager.PlayInterfaceOneShot(weaponSwitchSound);
			SceneSingleton<HUDOptions>.i.AutomaticToggle(currentWeaponStation, currentWeaponStation.SafetyIsOn(aircraft));
		}
	}

	public WeaponStation GetWeaponStation()
	{
		return currentWeaponStation;
	}

	public void DisplayCountermeasureAmmo(int ammo)
	{
		countermeasureAmmo.text = ammo.ToString();
		countermeasureAmmo.enabled = ammo > 1;
	}

	public void DisplayCountermeasures(string countermeasureName, Sprite displayImage, bool inUse)
	{
		Color color = (inUse ? (ThemeManager.Active.ColorTheme.Alert + ThemeManager.Active.ColorTheme.AllClear * 0.5f) : ThemeManager.Active.ColorTheme.AllClear);
		this.countermeasureName.color = color;
		if (this.countermeasureName.text != countermeasureName)
		{
			this.countermeasureName.text = countermeasureName;
		}
		countermeasureImage.color = color;
		if (countermeasureImage.sprite != displayImage)
		{
			countermeasureImage.sprite = displayImage;
		}
	}

	public void DisplayCountermeasures(string countermeasureName, Sprite displayImage, int ammo)
	{
		Color color = ((ammo > 0) ? ThemeManager.Active.ColorTheme.AllClear : ThemeManager.Active.ColorTheme.Alert);
		if (this.countermeasureName.text != countermeasureName)
		{
			this.countermeasureName.text = countermeasureName;
		}
		if (countermeasureImage.sprite != displayImage)
		{
			countermeasureImage.sprite = displayImage;
		}
		countermeasureImage.color = color;
		countermeasureAmmo.color = color;
		this.countermeasureName.color = color;
		countermeasureAmmo.text = ammo.ToString();
		if (!countermeasureAmmo.enabled)
		{
			countermeasureAmmo.enabled = true;
		}
	}

	public void SelectUnit(Unit unit)
	{
		if (markerLookup.ContainsKey(unit))
		{
			markerLookup[unit].SelectMarker();
			aircraft.weaponManager.AddTargetList(unit);
		}
		SoundManager.PlayInterfaceOneShot(selectSound);
	}

	public void DeSelectUnit(Unit unit)
	{
		if (targetList.Count != 0)
		{
			if (markerLookup.ContainsKey(unit))
			{
				markerLookup[unit]?.DeselectMarker();
			}
			SoundManager.PlayInterfaceOneShot(deselectSound);
			SceneSingleton<DynamicMap>.i.DeselectIcon(unit);
			targetList.Remove(unit);
			aircraft.weaponManager.TargetListChanged();
		}
	}

	private void TargetSelect(bool paint)
	{
		bool flag = false;
		List<HUDUnitMarker> list = new List<HUDUnitMarker>();
		for (int i = 0; i < markers.Count; i++)
		{
			if (FastMath.InRange(targetDesignator.gameObject.transform.position, markers[i].image.transform.position, 100f) && (paint || !markers[i].selected) && markers[i].image.enabled && !SceneSingleton<TargetListSelector>.i.CheckExclusions(markers[i].unit))
			{
				list.Add(markers[i]);
			}
		}
		if (paint)
		{
			for (int j = 0; j < list.Count; j++)
			{
				if (list[j].unit != null && list[j].unit.NetworkHQ != null && list[j].unit.NetworkHQ != aircraft.NetworkHQ && !targetList.Contains(list[j].unit) && currentWeaponStation != null && CombatAI.AnalyzeTarget(currentWeaponStation, aircraft, aircraft.NetworkHQ.GetTrackingData(list[j].unit.persistentID), 0f, 1000f, 100f).opportunity != 0f)
				{
					targetList.Insert(0, list[j].unit);
					flag = true;
					list[j].SelectMarker();
				}
			}
		}
		else if (list.Count > 0)
		{
			list.Sort((HUDUnitMarker a, HUDUnitMarker b) => b.AssessPriority(aircraft, targetDesignator.gameObject, currentWeaponStation).CompareTo(a.AssessPriority(aircraft, targetDesignator.gameObject, currentWeaponStation)));
			if (!targetList.Contains(list[0].unit))
			{
				targetList.Insert(0, list[0].unit);
				flag = true;
				list[0].SelectMarker();
			}
		}
		if (flag)
		{
			SoundManager.PlayInterfaceOneShot(selectSound);
			aircraft.weaponManager.TargetListChanged();
		}
	}

	public void DeselectAll(bool withAudio = false)
	{
		if (targetList.Count == 0)
		{
			return;
		}
		foreach (Unit target in targetList)
		{
			if (markerLookup.ContainsKey(target))
			{
				markerLookup[target]?.DeselectMarker();
			}
		}
		SceneSingleton<DynamicMap>.i.DeselectAllIcons();
		targetList.Clear();
		targetArrow.enabled = false;
		targetText.enabled = false;
		if (aircraft != null)
		{
			aircraft.weaponManager.TargetListChanged();
		}
		if (withAudio)
		{
			SoundManager.PlayInterfaceOneShot(deselectAllSound);
		}
	}

	public void DeselectLast()
	{
		if (targetList.Count != 0)
		{
			Unit unit = targetList[0];
			if (markerLookup.ContainsKey(unit))
			{
				markerLookup[unit]?.DeselectMarker();
			}
			SoundManager.PlayInterfaceOneShot(deselectSound);
			SceneSingleton<DynamicMap>.i.DeselectIcon(unit);
			targetList.Remove(unit);
			aircraft.weaponManager.TargetListChanged();
		}
	}

	private bool ShowTargetInfo()
	{
		if (targetList.Count == 0 || targetArrow.enabled)
		{
			return false;
		}
		if (!markerLookup.TryGetValue(targetList[0], out var value))
		{
			return false;
		}
		if (!this.aircraft.NetworkHQ.TryGetKnownPosition(value.unit, out var knownPosition))
		{
			return false;
		}
		targetInfo.transform.position = value.image.transform.position;
		float distance = FastMath.Distance(knownPosition, this.aircraft.GlobalPosition());
		targetInfo.text = "";
		if (value.unit is Aircraft)
		{
			Aircraft aircraft = value.unit as Aircraft;
			if (aircraft.pilots[0] != null && aircraft.Player != null)
			{
				TextMeshProUGUI textMeshProUGUI = targetInfo;
				textMeshProUGUI.text = textMeshProUGUI.text + aircraft.Player.GetDisplayName(PlayerNameContext.Other) + "\n";
			}
		}
		TextMeshProUGUI textMeshProUGUI2 = targetInfo;
		textMeshProUGUI2.text = textMeshProUGUI2.text + value.unit.definition.code + "\n\n\n" + UnitConverter.DistanceReading(distance);
		value.image.color = ThemeManager.Active.ColorTheme.HudUnitSelected;
		return true;
	}

	private void UpdateMarkers()
	{
		FactionHQ networkHQ = aircraft.NetworkHQ;
		GlobalPosition viewPosition = SceneSingleton<CameraStateManager>.i.transform.GlobalPosition();
		if (jammedSource == null)
		{
			if (jamAccumulation > 0f)
			{
				jammedSource = base.gameObject.AddComponent<AudioSource>();
				jammedSource.spatialBlend = 0f;
				jammedSource.loop = true;
				jammedSource.dopplerLevel = 0f;
				jammedSource.outputAudioMixerGroup = SoundManager.i.JammedNoiseMixer;
				jammedSource.clip = jammedSound;
				jammedSource.volume = 0f;
				jammedSource.Play();
			}
		}
		else
		{
			if (jamAccumulation == 0f)
			{
				jammedSource.Stop();
			}
			if (jamAccumulation > 0f)
			{
				jammedSource.volume = FastMath.SmoothDamp(jammedSource.volume, jamAccumulation, ref smoothVel, 0.5f) * jammedVolumeMultiplier;
				if (!jammedSource.isPlaying)
				{
					jammedSource.Play();
					jammedSource.time = UnityEngine.Random.Range(0f, jammedSound.length);
				}
			}
		}
		if (aircraft != null && markers.Count > 0)
		{
			Vector3 forward = SceneSingleton<CameraStateManager>.i.transform.forward;
			foreach (HUDUnitMarker marker in markers)
			{
				marker.UpdatePosition(networkHQ, viewPosition, forward);
				if (jamAccumulation > 0f)
				{
					marker.JammingDistortion(jamAccumulation);
				}
			}
			if (iconIndex >= markers.Count)
			{
				iconIndex = 0;
			}
			markers[iconIndex].UpdateVisibility(networkHQ, viewPosition);
			iconIndex++;
		}
		jamAccumulation = Mathf.Clamp01(jamAccumulation - Mathf.Max(jamAccumulation, 0.25f) * Time.deltaTime);
	}

	private void UpdateHitMarkers()
	{
		for (int i = 0; i < hitMarkers.Count; i++)
		{
			hitMarkers[i].Position();
		}
	}

	public void SetTargetArrow(bool enabled, Vector3 position, Vector3 angles)
	{
		targetArrow.enabled = enabled;
		targetText.enabled = enabled;
		targetText.transform.position = targetArrowTail.position;
		if (enabled)
		{
			targetArrow.transform.position = position;
			targetArrow.transform.localEulerAngles = angles;
		}
	}

	private void FixedUpdate()
	{
		if (weaponState != null)
		{
			weaponState.HUDFixedUpdate(aircraft, targetList);
		}
	}

	private void LateUpdate()
	{
		if (aircraft == null)
		{
			return;
		}
		UpdateMarkers();
		UpdateHitMarkers();
		if (GameManager.playerInput.GetButtonTimedPressUp("Select", 0f, PlayerSettings.clickDelay))
		{
			TargetSelect(paint: false);
		}
		else if (GameManager.playerInput.GetButtonTimedPressDown("Select", PlayerSettings.pressDelay) && !DynamicMap.mapMaximized)
		{
			TargetSelect(paint: true);
		}
		if (targetList.Count > 0)
		{
			if (aircraft.targetCam != null)
			{
				aircraft.targetCam.SetTargetCam();
			}
		}
		else if (targetArrow.enabled)
		{
			targetArrow.enabled = false;
			targetText.enabled = false;
		}
		bool num = ShowTargetInfo();
		if (num && !targetInfo.enabled)
		{
			targetInfo.enabled = true;
		}
		if (!num && targetInfo.enabled)
		{
			targetInfo.enabled = false;
		}
		if (weaponState != null)
		{
			weaponState.UpdateWeaponDisplay(aircraft, targetList);
		}
	}

	public GameObject ShowNotchIndicator()
	{
		return UnityEngine.Object.Instantiate(notchIndicatorPrefab, iconLayer.transform);
	}
}
