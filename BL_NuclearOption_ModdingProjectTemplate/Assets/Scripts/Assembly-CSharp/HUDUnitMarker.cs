using NuclearOption.UIStyleSystem;
using UnityEngine;
using UnityEngine.UI;

public class HUDUnitMarker
{
	public Image image;

	private Transform _transform;

	private Sprite icon;

	public Unit unit;

	private TrackingInfo trackingInfo;

	public bool selected;

	private float threat;

	private float timeCreated;

	private bool maximized;

	public bool alwaysMaximized;

	private bool flashing;

	private bool hidden;

	public bool outdated;

	public bool fresh;

	private Color color;

	private float opacity = 1f;

	private float scale = 1f;

	private float customScale;

	private float customRange = 15000f;

	private float distanceScale = 1f;

	public HUDUnitMarker(Unit unit, Image image)
	{
		this.unit = unit;
		_transform = image.transform;
		this.image = image;
		this.image.gameObject.name = $"{unit.unitName}[{unit.persistentID}]";
		this.image.enabled = false;
		unit.onDisableUnit += HUDIcon_OnDisableUnit;
		unit.onChangeFaction += HUDIcon_OnChangeFaction;
		DynamicMap.onShowTypesChanged += HUDUnitMarker_OnMapShowTypesChanged;
		SceneSingleton<HUDOptions>.i.OnApplyOptions += HUDUnitMarker_OnApplyOptions;
		PlayerSettings.OnApplyOptions += HUDUnitMarker_OnApplyOptions;
		SceneSingleton<CombatHUD>.i.aircraft.onSetGear += HUDUnitMarker_OnSetGear;
		ThemeManager.ThemeGroupChanged += HUDUnitMarker_OnThemeGroupChanged;
		alwaysMaximized = unit is Aircraft;
		timeCreated = Time.timeSinceLevelLoad;
		fresh = true;
		trackingInfo = SceneSingleton<DynamicMap>.i.HQ.GetTrackingData(unit.persistentID);
		if (!SceneSingleton<CombatHUD>.i.landingMode)
		{
			scale = unit.definition.iconSize;
			UpdateHidden(SceneSingleton<CombatHUD>.i.aircraft.gearDeployed);
			CustomizeIcon();
			UpdatePosition(SceneSingleton<DynamicMap>.i.HQ, SceneSingleton<CameraStateManager>.i.transform.GlobalPosition(), SceneSingleton<CameraStateManager>.i.transform.forward);
			SetNew();
			UpdateColor();
		}
	}

	private void HUDUnitMarker_OnApplyOptions()
	{
		CustomizeIcon();
	}

	private void HUDUnitMarker_OnSetGear(Aircraft.OnSetGear g)
	{
		UpdateHidden(g.gearState == LandingGear.GearState.Extending || g.gearState == LandingGear.GearState.LockedExtended);
	}

	private void CustomizeIcon()
	{
		customScale = scale * PlayerSettings.hmdIconSize;
		customRange = SceneSingleton<HUDOptions>.i.CheckMaximizeIcon(unit);
		UpdateVisibility(SceneSingleton<DynamicMap>.i.HQ, SceneSingleton<CameraStateManager>.i.transform.GlobalPosition());
	}

	public void AssessThreat(Unit assessor)
	{
		if (!(assessor == null))
		{
			threat = assessor.definition.ThreatPosedBy(unit.definition.roleIdentity);
		}
	}

	public float AssessPriority(Aircraft aircraft, GameObject targetDesignator, WeaponStation weaponStation)
	{
		float num = ((trackingInfo != null) ? FastMath.Distance(aircraft.GlobalPosition(), trackingInfo.GetPosition()) : Vector3.Distance(aircraft.transform.position, unit.transform.position));
		float num2 = FastMath.SquareDistance(targetDesignator.transform.position, _transform.position);
		if (unit.NetworkHQ == aircraft.NetworkHQ || unit.NetworkHQ == null || weaponStation == null)
		{
			return 0.1f / (num * num2);
		}
		OpportunityThreat opportunityThreat = weaponStation.CalcOpportunityThreat(unit.definition, aircraft);
		return Mathf.Max(opportunityThreat.opportunity + opportunityThreat.threat, 0.2f) / (num * num2);
	}

	public void SetNew()
	{
		timeCreated = Time.timeSinceLevelLoad;
		SetFactionIcon();
		SetFactionColor();
		UpdateVisibility(SceneSingleton<DynamicMap>.i.HQ, SceneSingleton<CameraStateManager>.i.transform.GlobalPosition());
	}

	private void SetFactionIcon()
	{
		icon = DynamicMap.GetFactionMode(unit.NetworkHQ) switch
		{
			FactionMode.NoFaction => unit.definition.friendlyIcon, 
			FactionMode.Friendly => unit.definition.friendlyIcon, 
			FactionMode.Enemy => unit.definition.hostileIcon, 
			_ => icon, 
		};
	}

	private void SetFactionColor()
	{
		color = DynamicMap.GetFactionMode(unit.NetworkHQ) switch
		{
			FactionMode.NoFaction => ThemeManager.Active.ColorTheme.HudUnitNeutral, 
			FactionMode.Friendly => ThemeManager.Active.ColorTheme.HudUnitFriendly, 
			FactionMode.Enemy => ThemeManager.Active.ColorTheme.HudUnitHostile, 
			_ => color, 
		};
	}

	public void SetFlashing(bool flashing)
	{
		this.flashing = flashing;
		if (!selected && !flashing)
		{
			UpdateVisibility(SceneSingleton<DynamicMap>.i.HQ, SceneSingleton<CameraStateManager>.i.transform.GlobalPosition());
			UpdateColor();
		}
	}

	public void SetNearestAlly(bool nearest)
	{
		if (unit is Aircraft)
		{
			icon = (nearest ? GameAssets.i.closestAircraftSprite : GameAssets.i.aircraftSprite);
		}
	}

	private void HUDIcon_OnDisableUnit(Unit unit)
	{
		selected = false;
		SceneSingleton<CombatHUD>.i.RemoveMarker(this);
		RemoveIcon();
	}

	public Vector3 GetLocalPosition()
	{
		return _transform.localPosition;
	}

	public void RemoveIcon()
	{
		unit.onDisableUnit -= HUDIcon_OnDisableUnit;
		unit.onChangeFaction -= HUDIcon_OnChangeFaction;
		DynamicMap.onShowTypesChanged -= HUDUnitMarker_OnMapShowTypesChanged;
		SceneSingleton<HUDOptions>.i.OnApplyOptions -= HUDUnitMarker_OnApplyOptions;
		PlayerSettings.OnApplyOptions -= HUDUnitMarker_OnApplyOptions;
		ThemeManager.ThemeGroupChanged -= HUDUnitMarker_OnThemeGroupChanged;
		if (SceneSingleton<CombatHUD>.i.aircraft != null)
		{
			SceneSingleton<CombatHUD>.i.aircraft.onSetGear -= HUDUnitMarker_OnSetGear;
		}
		if (trackingInfo != null && outdated)
		{
			trackingInfo.OnSpotted -= HUDUnitMarker_OnSpotted;
		}
		NetworkSceneSingleton<Spawner>.i.DestroyLocal(image.gameObject, 0f);
	}

	private void HUDIcon_OnChangeFaction(Unit unit)
	{
		SetNew();
		UpdateColor();
	}

	private void HUDUnitMarker_OnThemeGroupChanged()
	{
		SetFactionColor();
		UpdateColor();
	}

	public void SetLandingMode()
	{
		if (!(unit is Aircraft))
		{
			image.enabled = false;
		}
	}

	public void UpdatePosition(FactionHQ hq, GlobalPosition viewPosition, Vector3 cameraForward)
	{
		if (hidden)
		{
			return;
		}
		GlobalPosition knownPosition = unit.GlobalPosition();
		if (outdated && !hq.TryGetKnownPosition(unit, out knownPosition))
		{
			return;
		}
		if (selected)
		{
			if (HUDFunctions.PinToScreenEdge(knownPosition.ToLocalPosition(), out var rayToScreen, out var arrowAngle))
			{
				image.enabled = false;
				SceneSingleton<CombatHUD>.i.SetTargetArrow(enabled: true, rayToScreen, new Vector3(0f, 0f, arrowAngle * 57.29578f - 90f));
			}
			else
			{
				image.enabled = true;
				_transform.position = rayToScreen;
				SceneSingleton<CombatHUD>.i.SetTargetArrow(enabled: false, Vector3.zero, Vector3.zero);
			}
			if (!unit.HasRadarEmission())
			{
				return;
			}
			if ((unit.radar as Radar).IsJammed())
			{
				if (image.sprite != GameAssets.i.targetUnitSpriteJammed)
				{
					image.sprite = GameAssets.i.targetUnitSpriteJammed;
				}
			}
			else if (image.sprite == GameAssets.i.targetUnitSpriteJammed)
			{
				image.sprite = ((DynamicMap.GetFactionMode(unit.NetworkHQ) == FactionMode.Friendly) ? GameAssets.i.targetUnitSpriteFriendly : icon);
			}
			return;
		}
		if (Vector3.Dot(knownPosition - viewPosition, cameraForward) < 0f)
		{
			if (image.enabled)
			{
				image.enabled = false;
			}
			return;
		}
		if (!image.enabled)
		{
			image.enabled = true;
		}
		_transform.position = Vector3.Scale(SceneSingleton<CameraStateManager>.i.mainCamera.WorldToScreenPoint(knownPosition.ToLocalPosition()), new Vector3(1f, 1f, 0f));
		if (fresh)
		{
			float num = Time.timeSinceLevelLoad - timeCreated;
			image.color = Color.Lerp(color + ThemeManager.Active.ColorTheme.Warning, color, num);
			if (num > 1f)
			{
				fresh = false;
			}
		}
		if (flashing)
		{
			image.color = Color.Lerp(color + ThemeManager.Active.ColorTheme.Warning, color, Mathf.Sin(Time.timeSinceLevelLoad * 20f) + 0.5f);
		}
	}

	public void JammingDistortion(float jammingStrength)
	{
		if (!hidden)
		{
			jammingStrength = Mathf.Min(jammingStrength, 4f);
			Vector3 vector = Random.insideUnitSphere * jammingStrength * 3f;
			_transform.position += new Vector3(vector.x, vector.y, 0f);
			image.color = image.color.WithAlpha(1f - jammingStrength * Random.value);
		}
	}

	private void SetOutdated(bool newState)
	{
		if (!(image == null) && trackingInfo != null)
		{
			outdated = newState;
			if (selected)
			{
				image.sprite = (outdated ? GameAssets.i.targetUnitSpriteOld : icon);
			}
			else
			{
				UpdateColor();
			}
			if (outdated)
			{
				trackingInfo.OnSpotted += HUDUnitMarker_OnSpotted;
			}
			else
			{
				trackingInfo.OnSpotted -= HUDUnitMarker_OnSpotted;
			}
		}
	}

	public void UpdateVisibility(FactionHQ hq, GlobalPosition viewPosition)
	{
		if (!outdated && !hq.IsTargetPositionAccurate(unit, 20f))
		{
			SetOutdated(newState: true);
		}
		if (!selected && hq.TryGetKnownPosition(unit, out var knownPosition))
		{
			UpdateMaximized(knownPosition, viewPosition, hq != null && hq != unit.NetworkHQ);
		}
	}

	private void UpdateMaximized(GlobalPosition knownPosition, GlobalPosition viewPosition, bool enemy)
	{
		if (alwaysMaximized)
		{
			maximized = true;
			float num = FastMath.Distance(viewPosition, knownPosition);
			distanceScale = Mathf.Lerp(1f, 0.45f, num * 4E-05f - 0.5f);
			_transform.localScale = customScale * distanceScale * Vector3.one;
			image.sprite = icon;
			return;
		}
		if (flashing || Time.timeSinceLevelLoad - timeCreated <= 4f)
		{
			maximized = true;
		}
		else
		{
			maximized = FastMath.InRange(knownPosition, viewPosition, (0.1f + threat) * customRange);
		}
		if (maximized)
		{
			_transform.localScale = customScale * distanceScale * Vector3.one;
			image.sprite = icon;
		}
		else if (enemy)
		{
			_transform.localScale = 6f * Vector3.one;
			image.sprite = SceneSingleton<CombatHUD>.i.minimizedHostile;
		}
		else
		{
			_transform.localScale = 3f * Vector3.one;
			image.sprite = null;
		}
	}

	private void UpdateColor()
	{
		if (selected)
		{
			image.color = ThemeManager.Active.ColorTheme.HudUnitSelected;
		}
		else
		{
			image.color = color.WithAlpha(opacity * ((outdated && maximized) ? 0.5f : 1f));
		}
	}

	public void HUDUnitMarker_OnSpotted()
	{
		if (outdated)
		{
			SetOutdated(newState: false);
		}
	}

	private void UpdateHidden(bool gearExtended)
	{
		if (gearExtended)
		{
			hidden = !alwaysMaximized;
		}
		else
		{
			hidden = false;
			if (unit is PilotDismounted)
			{
				hidden = !SceneSingleton<MapOptions>.i.showPilotIcons;
			}
		}
		if (hidden)
		{
			image.enabled = false;
		}
		else
		{
			image.sprite = icon;
		}
	}

	public void HUDUnitMarker_OnMapShowTypesChanged()
	{
		UpdateHidden(SceneSingleton<CombatHUD>.i.aircraft.gearDeployed);
	}

	public void SelectMarker()
	{
		selected = true;
		if (unit.NetworkHQ == SceneSingleton<CombatHUD>.i.aircraft.NetworkHQ && unit.TryGetComponent<Rearmer>(out var component))
		{
			GameObject gameObject = Object.Instantiate(SceneSingleton<CombatHUD>.i.rearmerDisplay, _transform);
			gameObject.transform.localScale = Vector3.one * 0.05f;
			gameObject.transform.localPosition = Vector3.zero;
			gameObject.GetComponent<RearmerDisplay>().Initialize(this, null, component);
		}
		image.sprite = (outdated ? GameAssets.i.targetUnitSpriteOld : icon);
		if (unit.NetworkHQ == SceneSingleton<CombatHUD>.i.aircraft.NetworkHQ)
		{
			image.sprite = GameAssets.i.targetUnitSpriteFriendly;
		}
		_transform.localScale = Vector3.one * 20f;
		UpdateColor();
		SceneSingleton<DynamicMap>.i.SelectIcon(unit);
	}

	public void DeselectMarker()
	{
		selected = false;
		if (SceneSingleton<CombatHUD>.i.aircraft != null)
		{
			UpdateVisibility(SceneSingleton<CombatHUD>.i.aircraft.NetworkHQ, SceneSingleton<CameraStateManager>.i.transform.GlobalPosition());
		}
		UpdateColor();
		SceneSingleton<DynamicMap>.i.DeselectIcon(unit);
	}
}
