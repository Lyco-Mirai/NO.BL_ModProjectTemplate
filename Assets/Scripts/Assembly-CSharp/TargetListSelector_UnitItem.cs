using NuclearOption.UIStyleSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TargetListSelector_UnitItem : MonoBehaviour
{
	public TargetListSelector selector;

	public Unit unit;

	[SerializeField]
	private TextMeshProUGUI unitName;

	[SerializeField]
	private TextMeshProUGUI unitDist;

	[SerializeField]
	private TextMeshProUGUI laserIndicator;

	[SerializeField]
	private TextMeshProUGUI missilesIndicator;

	[SerializeField]
	private Image unitIcon;

	[SerializeField]
	private Image missilesIcon;

	private float lastUpdate;

	private float refreshRate = 1f;

	private TrackingInfo trackingInfo;

	private void Start()
	{
		ThemeManager.ThemeGroupChanged += TargetListSelector_UnitItem_OnThemeGroupChanged;
	}

	private void OnDestroy()
	{
		ThemeManager.ThemeGroupChanged -= TargetListSelector_UnitItem_OnThemeGroupChanged;
	}

	private void Update()
	{
		if (Time.timeSinceLevelLoad < lastUpdate + refreshRate)
		{
			return;
		}
		FactionHQ hq = SceneSingleton<DynamicMap>.i.HQ;
		string text = (ShouldShowGrid() ? SceneSingleton<DynamicMap>.i.gridLabels.GetGridPosition(unit.GlobalPosition()) : "?");
		if (SceneSingleton<CombatHUD>.i.aircraft != null && SceneSingleton<DynamicMap>.i.HQ.TryGetKnownPosition(unit, out var knownPosition))
		{
			float distance = FastMath.Distance(SceneSingleton<CombatHUD>.i.aircraft.GlobalPosition(), knownPosition);
			text = text + " | " + UnitConverter.DistanceReading(distance);
		}
		unitDist.text = text;
		if (hq != null && hq.IsTargetLased(unit))
		{
			if (SceneSingleton<CombatHUD>.i.aircraft != null)
			{
				LaserDesignator laserDesignator = SceneSingleton<CombatHUD>.i.aircraft.GetLaserDesignator();
				if (laserDesignator != null && laserDesignator.GetLasedTargets().Contains(unit))
				{
					laserIndicator.text = "[L]";
					laserIndicator.color = ThemeManager.Active.ColorTheme.Alert;
				}
				else
				{
					laserIndicator.text = "L";
					laserIndicator.color = ThemeManager.Active.ColorTheme.AllClear;
				}
			}
		}
		else
		{
			laserIndicator.text = "";
		}
		if (trackingInfo == null || trackingInfo.missileAttacks == 0)
		{
			missilesIndicator.text = "";
			missilesIcon.enabled = false;
		}
		else
		{
			string text2 = ((trackingInfo.missileAttacks < 6) ? $"{trackingInfo.missileAttacks}" : "5+");
			missilesIndicator.text = text2;
			missilesIcon.enabled = true;
		}
		lastUpdate = Time.timeSinceLevelLoad;
		bool ShouldShowGrid()
		{
			if (!(hq == null) && !(hq == unit.NetworkHQ))
			{
				return hq.IsTargetPositionAccurate(unit, 100f);
			}
			return true;
		}
	}

	public void ToggleSelect()
	{
		if (SceneSingleton<CombatHUD>.i.aircraft != null)
		{
			SceneSingleton<CombatHUD>.i.DeSelectUnit(unit);
		}
		else
		{
			SceneSingleton<DynamicMap>.i.DeselectIcon(unit);
		}
	}

	public void SetUnit(Unit u)
	{
		unit = u;
		unitName.text = unit.unitName;
		Color allClear = ThemeManager.Active.ColorTheme.AllClear;
		Color color = ThemeManager.Active.ColorTheme.AllClear;
		if (unit.NetworkHQ == null)
		{
			allClear = Color.white;
		}
		else if (SceneSingleton<DynamicMap>.i.HQ != null)
		{
			allClear = ((unit.NetworkHQ == SceneSingleton<DynamicMap>.i.HQ) ? ThemeManager.Active.ColorTheme.MapIconFriendly : ThemeManager.Active.ColorTheme.MapIconHostile);
			color = ((unit.NetworkHQ == SceneSingleton<DynamicMap>.i.HQ) ? ThemeManager.Active.ColorTheme.MapIconHostile : ThemeManager.Active.ColorTheme.AllClear);
		}
		else
		{
			allClear = unit.NetworkHQ.faction.color;
		}
		trackingInfo = null;
		if (unit.NetworkHQ != null)
		{
			foreach (FactionHQ allHQ in FactionRegistry.GetAllHQs())
			{
				if (allHQ != unit.NetworkHQ)
				{
					trackingInfo = allHQ.GetTrackingData(unit.persistentID);
				}
			}
		}
		unitName.color = allClear;
		unitIcon.color = allClear;
		unitDist.color = allClear;
		unitIcon.sprite = ((unit.NetworkHQ == SceneSingleton<DynamicMap>.i.HQ) ? unit.definition.friendlyIcon : unit.definition.hostileIcon);
		if (unit is Aircraft aircraft)
		{
			unitIcon.sprite = aircraft.definition.mapIcon;
		}
		else if (unit is Missile)
		{
			unitIcon.color = 0.75f * allClear;
		}
		missilesIcon.color = 0.5f * color;
		missilesIndicator.color = color;
	}

	private void TargetListSelector_UnitItem_OnThemeGroupChanged()
	{
		if (!(unit == null))
		{
			Color allClear = ThemeManager.Active.ColorTheme.AllClear;
			Color color = ThemeManager.Active.ColorTheme.AllClear;
			if (unit.NetworkHQ == null)
			{
				allClear = Color.white;
			}
			else if (SceneSingleton<DynamicMap>.i.HQ != null)
			{
				allClear = ((unit.NetworkHQ == SceneSingleton<DynamicMap>.i.HQ) ? ThemeManager.Active.ColorTheme.MapIconFriendly : ThemeManager.Active.ColorTheme.MapIconHostile);
				color = ((unit.NetworkHQ == SceneSingleton<DynamicMap>.i.HQ) ? ThemeManager.Active.ColorTheme.MapIconHostile : ThemeManager.Active.ColorTheme.AllClear);
			}
			else
			{
				allClear = unit.NetworkHQ.faction.color;
			}
			unitName.color = allClear;
			unitIcon.color = allClear;
			unitDist.color = allClear;
			if (unit is Missile)
			{
				unitIcon.color = 0.75f * allClear;
			}
			missilesIcon.color = 0.5f * color;
			missilesIndicator.color = color;
			Update();
		}
	}
}
