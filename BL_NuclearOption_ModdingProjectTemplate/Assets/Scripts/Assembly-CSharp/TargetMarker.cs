using NuclearOption.Networking;
using UnityEngine;
using UnityEngine.UI;

public class TargetMarker : UnitMapMarker
{
	[SerializeField]
	private Text infoPlayer;

	[SerializeField]
	private Text infoName;

	[SerializeField]
	private Text infoRange;

	[SerializeField]
	private Text infoSpeed;

	[SerializeField]
	private Text infoAlt;

	[SerializeField]
	private Text infoHeading;

	protected override void ExtraSetup()
	{
		Unit unit = GetUnit();
		markerImg.sprite = ((unit.NetworkHQ != null && unit.NetworkHQ == SceneSingleton<DynamicMap>.i.HQ && SceneSingleton<DynamicMap>.i.HQ != null) ? GameAssets.i.targetUnitSpriteFriendly : GameAssets.i.targetUnitSprite);
		infoPlayer.enabled = false;
		if (unit is Aircraft aircraft && aircraft.pilots[0].player != null)
		{
			infoPlayer.enabled = true;
			infoPlayer.text = aircraft.pilots[0].player.GetDisplayName(PlayerNameContext.Other);
		}
		infoName.text = unit.definition.code;
		infoSpeed.text = "SPD " + UnitConverter.SpeedReading(unit.speed);
		infoAlt.text = "ALT " + UnitConverter.AltitudeReading(unit.radarAlt);
		infoHeading.text = $"HDG {unit.transform.eulerAngles.y}°";
		infoRange.text = "RNG -";
		base.transform.localPosition = base.Icon.transform.localPosition;
		base.transform.eulerAngles = Vector3.zero;
		float num = 1f / SceneSingleton<DynamicMap>.i.mapImage.transform.localScale.x;
		base.transform.localScale = Vector3.one * num * 1f;
		DynamicMap.onShowTypesChanged += Marker_Refresh;
		Marker_Refresh();
	}

	private void Update()
	{
		float num = (DynamicMap.mapMaximized ? 1f : 0.5f) / SceneSingleton<DynamicMap>.i.mapImage.transform.localScale.x;
		base.transform.localScale = Vector3.one * num;
		base.transform.eulerAngles = Vector3.zero;
		base.transform.localPosition = base.Icon.transform.localPosition;
		if (!(Time.timeSinceLevelLoad > lastRefresh + refreshDelay))
		{
			return;
		}
		Unit unit = GetUnit();
		if (unit != null)
		{
			if (unit.NetworkHQ == null || unit.NetworkHQ == SceneSingleton<DynamicMap>.i.HQ || SceneSingleton<DynamicMap>.i.HQ == null || SceneSingleton<DynamicMap>.i.HQ.IsTargetPositionAccurate(unit, 20f))
			{
				infoSpeed.text = "SPD " + UnitConverter.SpeedReading(unit.speed);
				if (unit is Aircraft)
				{
					infoAlt.text = "ALT " + UnitConverter.AltitudeReading(unit.radarAlt);
				}
				else if (unit is Missile)
				{
					infoAlt.text = "ALT " + UnitConverter.AltitudeReading(unit.GlobalPosition().y);
				}
				else
				{
					infoAlt.text = "ALT -";
				}
				infoHeading.text = $"HDG {Mathf.RoundToInt(unit.transform.eulerAngles.y)}°";
				if (SceneSingleton<CombatHUD>.i.aircraft != null)
				{
					infoRange.text = "RNG " + UnitConverter.DistanceReading(FastMath.Distance(SceneSingleton<CombatHUD>.i.aircraft.GlobalPosition(), unit.GlobalPosition()));
				}
				else
				{
					infoRange.text = "";
				}
			}
			else
			{
				infoSpeed.text = "SPD -";
				infoAlt.text = "ALT -";
				infoHeading.text = "HDG -";
				infoRange.text = "RNG -";
			}
		}
		else
		{
			Remove();
		}
		DynamicHide();
		lastRefresh = Time.timeSinceLevelLoad;
	}

	public override void DynamicHide()
	{
		if (!SceneSingleton<MapOptions>.i.showTargetInfo || !DynamicMap.mapMaximized)
		{
			Show(value: false);
			return;
		}
		if (DynamicMap.mapMaximized && !infoName.enabled)
		{
			Show(value: true);
		}
		if (SceneSingleton<DynamicMap>.i.mapMarkers.Count <= 1)
		{
			return;
		}
		int num = SceneSingleton<DynamicMap>.i.mapMarkers.FindIndex((MapMarker x) => x == this);
		bool flag = false;
		for (int num2 = 0; num2 < SceneSingleton<DynamicMap>.i.mapMarkers.Count; num2++)
		{
			MapMarker mapMarker = SceneSingleton<DynamicMap>.i.mapMarkers[num2];
			if (mapMarker != null && mapMarker != this && mapMarker is TargetMarker && Vector3.Distance(base.transform.position, mapMarker.transform.position) < 80f && num < num2)
			{
				flag = true;
			}
		}
		if (flag)
		{
			Mask();
		}
		else
		{
			Show(value: true);
		}
	}

	public override void Mask()
	{
		if (infoRange.enabled)
		{
			Color grey = Color.grey;
			grey.a = 0.5f;
			markerImg.color = grey;
			infoPlayer.color = grey;
			infoName.color = grey;
			infoSpeed.color = grey;
			infoAlt.color = grey;
			infoHeading.color = grey;
			infoRange.enabled = false;
		}
	}

	public override void Show(bool value)
	{
		Color white = Color.white;
		white.a = 0.75f;
		if (value)
		{
			markerImg.enabled = true;
			markerImg.color = white;
			infoPlayer.color = white;
			infoName.enabled = true;
			infoName.color = white;
			infoSpeed.enabled = true;
			infoSpeed.color = white;
			infoAlt.enabled = true;
			infoAlt.color = white;
			infoHeading.enabled = true;
			infoHeading.color = white;
			infoRange.enabled = true;
			infoRange.color = white;
		}
		else
		{
			markerImg.enabled = SceneSingleton<MapOptions>.i.showTargetInfo;
			markerImg.color = white;
			infoName.enabled = false;
			infoSpeed.enabled = false;
			infoAlt.enabled = false;
			infoHeading.enabled = false;
			infoRange.enabled = false;
		}
	}

	public void Marker_Refresh()
	{
		Show(SceneSingleton<MapOptions>.i.showTargetInfo);
	}

	private void OnDestroy()
	{
		DynamicMap.onShowTypesChanged -= Marker_Refresh;
	}
}
