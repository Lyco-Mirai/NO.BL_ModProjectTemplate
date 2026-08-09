using NuclearOption.MissionEditorScripts;
using NuclearOption.Networking;
using NuclearOption.UIStyleSystem;
using UnityEngine;

public class UnitMapIcon : MapIcon
{
	private bool orient;

	private bool flashing;

	private bool scale;

	private Vector3 iconProportions = Vector3.one;

	private Vector3 unitTrueSize;

	private float unitMaxDimension;

	private TrackingInfo trackingInfo;

	private float unitSizeFactor = 1f;

	private TargetMarker targetMarker;

	private JammedMarker jammedMarker;

	public Unit unit { get; private set; }

	public Factory Factory { get; private set; }

	protected override FactionHQ GetHQ()
	{
		return unit.NetworkHQ;
	}

	protected override bool IsLocalPlayerAircraft()
	{
		return SceneSingleton<CombatHUD>.i.aircraft == unit;
	}

	public void SetIcon(Unit unit)
	{
		base.transform.localScale = Vector3.one * SceneSingleton<MapOptions>.i.iconSize;
		base.transform.eulerAngles = Vector3.zero;
		if (SceneSingleton<DynamicMap>.i.HQ != null)
		{
			trackingInfo = SceneSingleton<DynamicMap>.i.HQ.GetTrackingData(unit.persistentID);
		}
		base.gameObject.name = $"{unit.unitName}[{unit.persistentID}]";
		this.unit = unit;
		Factory = ((unit is Building) ? unit.GetComponent<Factory>() : null);
		iconImage.sprite = unit.definition.mapIcon;
		orient = unit.definition.mapOrient;
		unit.onChangeFaction += UnitMapIcon_OnFactionChanged;
		unit.onDisableUnit += UnitMapIcon_OnUnitDisabled;
		unit.onJam += UnitMapIcon_OnUnitJammed;
		ThemeManager.ThemeGroupChanged += UnitMapIcon_UpdateColor;
		float num = 1f / SceneSingleton<DynamicMap>.i.mapImage.transform.localScale.x;
		if (unit is Building)
		{
			scale = unit.maxRadius > 10f;
			unitTrueSize = new Vector3(unit.definition.width, unit.definition.length, 1f) * SceneSingleton<DynamicMap>.i.MetersToPixels();
			unitMaxDimension = Mathf.Max(unitTrueSize.x, unitTrueSize.y);
			iconProportions = unitTrueSize.normalized;
			float num2 = num * 10f;
			if (unitMaxDimension < num2)
			{
				iconImage.transform.localScale = iconProportions * num2;
			}
			else
			{
				iconImage.transform.localScale = unitTrueSize;
			}
		}
		else
		{
			iconImage.transform.localScale = num * 15f * unit.definition.mapIconSize * unitSizeFactor * Vector3.one;
		}
		UnitMapIcon_UpdateColor();
		if (unit is PilotDismounted && !SceneSingleton<MapOptions>.i.showPilotIcons && base.gameObject.activeSelf)
		{
			base.gameObject.SetActive(value: false);
		}
	}

	public void UnitMapIcon_UpdateColor()
	{
		UpdateColor();
		if (!(unit == null))
		{
			bool num = IsLocalPlayerAircraft();
			if (!num && SceneSingleton<TargetListSelector>.i.CheckExclusions(unit))
			{
				iconImage.color *= 0.67f;
			}
			if (num && iconImage.raycastTarget)
			{
				HighlightIcon();
			}
		}
	}

	private void UnitMapIcon_OnUnitDisabled(Unit unit)
	{
		SceneSingleton<DynamicMap>.i.RemoveIcon(this);
		RemoveIcon();
	}

	private void UnitMapIcon_OnFactionChanged(Unit unit)
	{
		UnitMapIcon_UpdateColor();
	}

	protected override Color GetColor()
	{
		FactionHQ hQ = GetHQ();
		switch (DynamicMap.GetFactionMode(hQ, checkNoFactionBeforeSpectator: true))
		{
		default:
			if (!isSelected)
			{
				return ThemeManager.Active.ColorTheme.MapIconNeutral;
			}
			return ThemeManager.Active.ColorTheme.MapIconNeutralSelected;
		case FactionMode.Spectator:
			if (!isSelected)
			{
				return hQ.faction.color;
			}
			return hQ.faction.selectedColor;
		case FactionMode.Friendly:
			if (!isSelected)
			{
				return ThemeManager.Active.ColorTheme.MapIconFriendly;
			}
			return ThemeManager.Active.ColorTheme.MapIconFriendlySelected;
		case FactionMode.Enemy:
			if (!isSelected)
			{
				return ThemeManager.Active.ColorTheme.MapIconHostile;
			}
			return ThemeManager.Active.ColorTheme.MapIconHostileSelected;
		}
	}

	protected override void OnSelectIcon()
	{
		GameObject gameObject = Object.Instantiate(SceneSingleton<DynamicMap>.i.targetMarker, SceneSingleton<DynamicMap>.i.infoLayer.transform);
		targetMarker = gameObject.GetComponent<TargetMarker>();
		targetMarker.Setup(this);
		SceneSingleton<DynamicMap>.i.mapMarkers.Add(targetMarker);
	}

	protected override void OnDeselectIcon()
	{
		if (targetMarker != null)
		{
			targetMarker.Remove();
		}
		targetMarker = null;
	}

	protected override void OnRemoveIcon()
	{
		if (unit != null)
		{
			unit.onChangeFaction -= UnitMapIcon_OnFactionChanged;
			unit.onDisableUnit -= UnitMapIcon_OnUnitDisabled;
			unit.onJam -= UnitMapIcon_OnUnitJammed;
		}
		ThemeManager.ThemeGroupChanged -= UnitMapIcon_UpdateColor;
		if (targetMarker != null)
		{
			targetMarker.Remove();
		}
		if (this != null)
		{
			Object.Destroy(base.gameObject);
		}
	}

	public override void ClickIcon(ClickSource clickSource)
	{
		if (!(unit == SceneSingleton<CombatHUD>.i.aircraft) && SceneSingleton<DynamicMap>.i.IsCursorInMapRectangle())
		{
			bool flag = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
			if (isSelected && flag)
			{
				Deselect();
			}
			else
			{
				Select(flag);
			}
		}
		void Deselect()
		{
			if (SceneSingleton<CombatHUD>.i.aircraft == null || SceneSingleton<CombatHUD>.i.aircraft.disabled)
			{
				SceneSingleton<CombatHUD>.i.DeSelectUnit(unit);
			}
			else
			{
				SceneSingleton<DynamicMap>.i.DeselectIcon(unit);
			}
		}
		void Select(bool shiftPressed)
		{
			bool flag2 = GameManager.gameState == GameState.Editor;
			bool flag3 = DynamicMap.GetFactionMode() == FactionMode.Spectator;
			int num;
			if (SceneSingleton<CombatHUD>.i.aircraft != null)
			{
				num = ((!SceneSingleton<CombatHUD>.i.aircraft.disabled) ? 1 : 0);
				if (num != 0)
				{
					goto IL_0052;
				}
			}
			else
			{
				num = 0;
			}
			if (!shiftPressed || flag2 || flag3)
			{
				SceneSingleton<DynamicMap>.i.UnselectAll();
			}
			goto IL_0052;
			IL_0052:
			if (num != 0)
			{
				if (!SceneSingleton<DynamicMap>.i.selectedIcons.Contains(this) && !SceneSingleton<TargetListSelector>.i.CheckExclusions(unit))
				{
					SceneSingleton<CombatHUD>.i.SelectUnit(unit);
				}
			}
			else
			{
				SceneSingleton<DynamicMap>.i.SelectIcon(unit);
				if (flag2 || !GameManager.GetLocalHQ(out var localHq) || !(unit.NetworkHQ != null) || !(localHq != unit.NetworkHQ) || localHq.IsTargetPositionAccurate(unit, 100f))
				{
					SceneSingleton<CameraStateManager>.i.transform.position = unit.transform.position + Vector3.up * unit.definition.height * 0.8f - SceneSingleton<CameraStateManager>.i.transform.forward * unit.definition.length * 2f;
					SceneSingleton<CameraStateManager>.i.transform.LookAt(unit.transform);
					if (flag2)
					{
						if (shiftPressed)
						{
							SceneSingleton<UnitSelection>.i.ToggleInMultiSelection(unit);
						}
						else
						{
							SceneSingleton<UnitSelection>.i.SetSelection(unit);
							SceneSingleton<DynamicMap>.i.Minimize();
						}
					}
					else
					{
						SceneSingleton<CameraStateManager>.i.SetFollowingUnit(unit);
					}
				}
			}
		}
	}

	public override void UpdateIcon(float mapDisplayFactor, float mapInverseScale, Transform mapTransform, bool mapMaximized)
	{
		if (unit is PilotDismounted)
		{
			if (!SceneSingleton<MapOptions>.i.showPilotIcons && base.gameObject.activeSelf)
			{
				base.gameObject.SetActive(value: false);
				return;
			}
			if (SceneSingleton<MapOptions>.i.showPilotIcons && !base.gameObject.activeSelf)
			{
				base.gameObject.SetActive(value: true);
			}
		}
		globalPosition = ((GameManager.GetLocalFaction(out var _) && trackingInfo != null) ? trackingInfo.GetPosition() : unit.GlobalPosition()).AsVector3() * mapDisplayFactor;
		iconImage.transform.localPosition = new Vector3(globalPosition.x, globalPosition.z, 0f);
		if (scale)
		{
			float num = mapInverseScale * 10f;
			if (unitMaxDimension < num)
			{
				iconImage.transform.localScale = iconProportions * num;
			}
			else
			{
				iconImage.transform.localScale = unitTrueSize;
			}
		}
		else
		{
			iconImage.transform.localScale = mapInverseScale * 15f * unit.definition.mapIconSize * unitSizeFactor * Vector3.one * SceneSingleton<MapOptions>.i.iconSize;
		}
		if (orient)
		{
			if (trackingInfo == null || trackingInfo.Observed())
			{
				iconImage.transform.eulerAngles = new Vector3(0f, 0f, mapTransform.eulerAngles.z - unit.transform.eulerAngles.y);
			}
		}
		else
		{
			iconImage.transform.eulerAngles = Vector3.zero;
		}
		if (flashing)
		{
			float t = Mathf.Sin(Time.realtimeSinceStartup * 20f) * 0.5f + 0.5f;
			iconImage.color = Color.Lerp(ThemeManager.Active.ColorTheme.MapIconHostile, ThemeManager.Active.ColorTheme.MapIconHostileSelected, t);
			iconImage.transform.localScale *= 1.2f;
		}
	}

	public override string GetInfoText()
	{
		string text = unit.unitName;
		if (unit is Aircraft)
		{
			Aircraft aircraft = unit as Aircraft;
			if (aircraft.pilots[0] != null && aircraft.Player != null)
			{
				text = text + "\n" + aircraft.Player.GetDisplayName(PlayerNameContext.Other);
			}
		}
		return text;
	}

	public void SetMissileWarning()
	{
		iconImage.sprite = GameAssets.i.missileWarningSprite;
		iconImage.color = ThemeManager.Active.ColorTheme.MapIconHostileSelected;
		flashing = true;
	}

	public void ClearMissileWarning()
	{
		if (!(iconImage == null))
		{
			iconImage.sprite = unit.definition.mapIcon;
			UnitMapIcon_UpdateColor();
			flashing = false;
		}
	}

	public void JammingDistortion(float jammingStrength)
	{
		jammingStrength = Mathf.Min(jammingStrength, 4f);
		iconImage.transform.position += Random.insideUnitSphere * jammingStrength * 5f / 2f;
		iconImage.color = iconImage.color.WithAlpha(1f - jammingStrength * 0.7f);
	}

	public void ClearJammingDistortion()
	{
		iconImage.color = iconImage.color.WithAlpha(1f);
	}

	private void UnitMapIcon_OnUnitJammed(Unit.JamEventArgs jam)
	{
		if (SceneSingleton<MapOptions>.i.showJamming && unit.radar is Radar radar && jammedMarker == null)
		{
			jammedMarker = Object.Instantiate(SceneSingleton<DynamicMap>.i.jammedMarker, SceneSingleton<DynamicMap>.i.infoLayer.transform).GetComponent<JammedMarker>();
			jammedMarker.Setup(this, jam.jammingUnit, radar);
		}
	}

	private void OnDestroy()
	{
		ThemeManager.ThemeGroupChanged -= UnitMapIcon_UpdateColor;
	}
}
