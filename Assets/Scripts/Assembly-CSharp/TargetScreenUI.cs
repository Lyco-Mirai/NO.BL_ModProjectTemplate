using System;
using System.Collections.Generic;
using NuclearOption.Networking;
using NuclearOption.UIStyleSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TargetScreenUI : MonoBehaviour
{
	[Serializable]
	private class GunCCIPDisplay
	{
		[SerializeField]
		private Image crosshair;

		[NonSerialized]
		public bool Active;

		public void Update(Camera cam, Aircraft aircraft, List<Unit> targetList)
		{
			WeaponStation currentWeaponStation = aircraft.weaponManager.currentWeaponStation;
			if (targetList.Count == 0 || currentWeaponStation == null || currentWeaponStation.WeaponInfo.muzzleVelocity == 0f || currentWeaponStation.HasTurret())
			{
				crosshair.enabled = false;
				return;
			}
			Unit unit = targetList[0];
			aircraft.GetControlsFilter().GetAim(unit, out var _, out var impactPoint);
			crosshair.enabled = impactPoint.HasValue;
			if (crosshair.enabled)
			{
				Vector3 localPosition = Vector3.Scale(cam.WorldToScreenPoint(impactPoint.Value.ToLocalPosition()), new Vector3(1f, 1f, 0f));
				crosshair.transform.localPosition = localPosition;
				bool flag = FastMath.InRange(impactPoint.Value, unit.GlobalPosition(), unit.maxRadius);
				crosshair.color = (flag ? ThemeManager.Active.ColorTheme.AllClear : Color.gray);
			}
		}
	}

	[SerializeField]
	private Canvas displayCanvas;

	[SerializeField]
	private TextMeshProUGUI typeText;

	[SerializeField]
	private TextMeshProUGUI pilotText;

	[SerializeField]
	private TextMeshProUGUI distance;

	[SerializeField]
	private TextMeshProUGUI heading;

	[SerializeField]
	private TextMeshProUGUI altitude;

	[SerializeField]
	private TextMeshProUGUI rel_altitude;

	[SerializeField]
	private TextMeshProUGUI speed;

	[SerializeField]
	private TextMeshProUGUI rel_speed;

	[SerializeField]
	private TextMeshProUGUI noLock;

	[SerializeField]
	private TextMeshProUGUI magText;

	[SerializeField]
	private TextMeshProUGUI modeText;

	[SerializeField]
	private TextMeshProUGUI bearingText;

	[SerializeField]
	private TextMeshProUGUI gridText;

	[SerializeField]
	private Image bearingImg;

	[SerializeField]
	private GameObject targetLockBox;

	[SerializeField]
	private GameObject lasingIcon;

	[SerializeField]
	private Sprite targetBoxSprite;

	[SerializeField]
	private Sprite friendlyBoxSprite;

	[SerializeField]
	private Sprite jammedSprite;

	[SerializeField]
	private Sprite lasedSprite;

	[SerializeField]
	private Sprite outdatedSprite;

	[SerializeField]
	private Transform bottomLeft;

	[SerializeField]
	private Image aimingBoxBgd;

	[SerializeField]
	private Image aimingBoxImg;

	[SerializeField]
	private Image aimingDotImg;

	private FactionHQ hq;

	private Vector3 scaleVector = new Vector3(1f, 1f, 0f);

	private bool displayingInfo;

	private Camera cam;

	private List<Image> targetBoxes = new List<Image>();

	private List<Image> lasingIcons = new List<Image>();

	private List<Unit> targetList;

	private TargetCam targetCam;

	private LaserDesignator laserDesignator;

	[SerializeField]
	private GunCCIPDisplay gunCCIPDisplay;

	public void SetupCamera(Camera cam, Camera UICam, Aircraft aircraft)
	{
		displayCanvas.worldCamera = UICam;
		targetCam = aircraft.targetCam;
		hq = aircraft.NetworkHQ;
		this.cam = cam;
		laserDesignator = aircraft.GetLaserDesignator();
		targetList = aircraft.weaponManager.GetTargetList();
		this.StartSlowUpdate(0.1f, UpdateTargetInfo);
	}

	private void OnDestroy()
	{
		if (displayCanvas != null)
		{
			UnityEngine.Object.Destroy(displayCanvas.gameObject);
		}
	}

	private void UpdateTargetInfo()
	{
		if (SceneSingleton<CombatHUD>.i.aircraft == null)
		{
			return;
		}
		while (targetBoxes.Count != targetList.Count)
		{
			if (targetBoxes.Count < targetList.Count)
			{
				Image component = UnityEngine.Object.Instantiate(targetLockBox, bottomLeft).GetComponent<Image>();
				targetBoxes.Add(component);
			}
			else
			{
				List<Image> list = targetBoxes;
				UnityEngine.Object.Destroy(list[list.Count - 1].gameObject);
				targetBoxes.RemoveAt(targetBoxes.Count - 1);
			}
		}
		if (targetList.Count == 0)
		{
			if (displayingInfo)
			{
				ToggleInfoDisplay(enabled: false);
			}
			return;
		}
		if (!displayingInfo)
		{
			ToggleInfoDisplay(enabled: true);
		}
		for (int i = 0; i < targetBoxes.Count; i++)
		{
			Image image = targetBoxes[i];
			Unit unit = targetList[i];
			Sprite sprite = targetBoxSprite;
			if (unit.NetworkHQ == SceneSingleton<CombatHUD>.i.aircraft.NetworkHQ)
			{
				sprite = friendlyBoxSprite;
			}
			else
			{
				if (unit.HasRadarEmission() && (targetList[i].radar as Radar).IsJammed())
				{
					sprite = jammedSprite;
				}
				if (hq.IsTargetLased(unit))
				{
					sprite = lasedSprite;
				}
				if (!hq.IsTargetPositionAccurate(unit, 20f))
				{
					sprite = outdatedSprite;
				}
			}
			if (image.sprite != sprite)
			{
				image.sprite = sprite;
			}
		}
		magText.text = $"Mag x{targetCam.GetMag():F1}";
		distance.text = "RNG " + UnitConverter.DistanceReading(targetCam.GetDist());
		gridText.text = "GRID: " + targetCam.GetGrid();
		modeText.text = (targetCam.UsingIR() ? "MODE: IR" : "MODE: COLOR");
		Transform camMount = targetCam.GetCamMount();
		bearingText.text = $"{camMount.transform.localEulerAngles.y:F0}°";
		bearingImg.rectTransform.localEulerAngles = new Vector3(0f, 0f, 0f - camMount.transform.localEulerAngles.y);
		Unit unit2 = targetList[0];
		bool flag = unit2 is Aircraft || unit2 is Missile;
		if (unit2.NetworkHQ == null)
		{
			typeText.color = Color.white;
		}
		else
		{
			typeText.color = ((unit2.NetworkHQ == hq) ? ThemeManager.Active.ColorTheme.HudUnitFriendly : ThemeManager.Active.ColorTheme.HudUnitHostile);
		}
		if (flag && unit2 is Aircraft aircraft && aircraft.pilots[0].player != null)
		{
			pilotText.gameObject.SetActive(value: true);
			pilotText.text = "Pilot : " + aircraft.pilots[0].player.GetDisplayName(PlayerNameContext.Other);
			pilotText.color = typeText.color;
		}
		else
		{
			pilotText.gameObject.SetActive(value: false);
		}
		if (hq.IsTargetPositionAccurate(targetList[0], 20f) && flag)
		{
			GlobalPosition globalPosition = targetList[0].GlobalPosition();
			Vector3 vector = globalPosition - SceneSingleton<CombatHUD>.i.aircraft.GlobalPosition();
			heading.text = $"HDG {targetList[0].transform.eulerAngles.y:F0}°";
			altitude.text = "ALT " + UnitConverter.AltitudeReading(globalPosition.y);
			rel_altitude.text = "REL " + UnitConverter.AltitudeReading(vector.y);
			speed.text = "SPD " + UnitConverter.SpeedReading(targetList[0].speed);
			rel_speed.text = "REL " + UnitConverter.SpeedReading(Vector3.Dot(SceneSingleton<CombatHUD>.i.aircraft.rb.velocity, vector.normalized) - Vector3.Dot(targetList[0].rb.velocity, vector.normalized));
		}
		else
		{
			heading.text = "HDG -";
			altitude.text = "ALT -";
			rel_altitude.text = "REL -";
			speed.text = "SPD -";
			rel_speed.text = "REL -";
		}
		if (targetList.Count > 1)
		{
			typeText.text = $"{targetList.Count} targets";
			heading.text = "HDG -";
			altitude.text = "ALT -";
			rel_altitude.text = "REL -";
			speed.text = "SPD -";
			rel_speed.text = "REL -";
		}
		else
		{
			typeText.text = ((unit2 is Aircraft) ? unit2.definition.unitName : unit2.unitName);
		}
		if (SceneSingleton<CombatHUD>.i.aircraft.weaponManager.currentWeaponStation != null && SceneSingleton<CombatHUD>.i.aircraft.weaponManager.currentWeaponStation.HasTurret())
		{
			if (!aimingBoxBgd.enabled)
			{
				aimingBoxBgd.enabled = true;
			}
			if (!aimingBoxImg.enabled)
			{
				aimingBoxImg.enabled = true;
			}
			if (!aimingDotImg.enabled)
			{
				aimingDotImg.enabled = true;
			}
			Vector2 turretRelativeAim = SceneSingleton<CombatHUD>.i.aircraft.weaponManager.currentWeaponStation.GetTurretRelativeAim();
			aimingDotImg.transform.localPosition = new Vector3(Mathf.Clamp(0f - turretRelativeAim.x, -30f, 30f), Mathf.Clamp(turretRelativeAim.y, -30f, 30f), 0f);
		}
		else
		{
			if (aimingBoxBgd.enabled)
			{
				aimingBoxBgd.enabled = false;
			}
			if (aimingBoxImg.enabled)
			{
				aimingBoxImg.enabled = false;
			}
			if (aimingDotImg.enabled)
			{
				aimingDotImg.enabled = false;
			}
		}
	}

	private void ToggleInfoDisplay(bool enabled)
	{
		displayingInfo = enabled;
		noLock.gameObject.SetActive(!enabled);
		typeText.gameObject.SetActive(enabled);
		pilotText.gameObject.SetActive(enabled);
		distance.gameObject.SetActive(enabled);
		heading.gameObject.SetActive(enabled);
		altitude.gameObject.SetActive(enabled);
		rel_altitude.gameObject.SetActive(enabled);
		speed.gameObject.SetActive(enabled);
		rel_speed.gameObject.SetActive(enabled);
		magText.gameObject.SetActive(enabled);
		modeText.gameObject.SetActive(enabled);
		gridText.gameObject.SetActive(enabled);
		bearingText.gameObject.SetActive(enabled);
		bearingImg.gameObject.SetActive(enabled);
		aimingBoxBgd.gameObject.SetActive(enabled);
		aimingDotImg.gameObject.SetActive(enabled);
	}

	public void LateUpdate()
	{
		if (!(SceneSingleton<CombatHUD>.i.aircraft == null))
		{
			for (int i = 0; i < targetBoxes.Count && i < targetList.Count; i++)
			{
				SceneSingleton<CombatHUD>.i.aircraft.NetworkHQ.TryGetKnownPosition(targetList[i], out var knownPosition);
				targetBoxes[i].gameObject.transform.localPosition = Vector3.Scale(cam.WorldToScreenPoint(knownPosition.ToLocalPosition()), scaleVector);
			}
			gunCCIPDisplay.Update(cam, SceneSingleton<CombatHUD>.i.aircraft, targetList);
		}
	}
}
