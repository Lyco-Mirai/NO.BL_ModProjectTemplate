using System;
using NuclearOption.UIStyleSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TacScreen : MonoBehaviour
{
	[Serializable]
	private class HardpointSetDisplay
	{
		[SerializeField]
		private Image[] hardpoints;
	}

	[SerializeField]
	private TextMeshProUGUI timeDisplay;

	[SerializeField]
	private GameObject timeObject;

	[SerializeField]
	private bool targetCamHidesTime = true;

	[SerializeField]
	private Canvas canvas;

	[SerializeField]
	private RenderTexture renderTexture;

	[SerializeField]
	private Camera cam;

	[SerializeField]
	private GameObject targetCamDisplay;

	[SerializeField]
	private GameObject landingCamDisplay;

	[SerializeField]
	private Material screenMaterial;

	private bool radarOn;

	private Aircraft aircraft;

	private TargetDetector opticalScanner;

	[SerializeField]
	private float displaySize;

	[SerializeField]
	private float metersPerPixel;

	[SerializeField]
	private float lastUpdate;

	[SerializeField]
	private Transform radarCenter;

	[SerializeField]
	private Transform iconLayer;

	[SerializeField]
	private GameObject iconPrefab;

	[SerializeField]
	private GameObject pingPrefab;

	[SerializeField]
	private GameObject missilePrefab;

	[SerializeField]
	private GameObject radarUnitPrefab;

	private Vector3 headingAtScan;

	[SerializeField]
	private Image radarCone;

	[SerializeField]
	private Image scanLine;

	public void Initialize(Aircraft aircraft, Cockpit cockpit)
	{
		this.aircraft = aircraft;
		headingAtScan = aircraft.transform.forward;
		aircraft.onDisableUnit += TacScreen_OnAircraftDisabled;
		aircraft.targetCam.onCamToggle += TacScreen_OnCamToggle;
		targetCamDisplay.SetActive(value: false);
		landingCamDisplay.SetActive(value: false);
		this.StartSlowUpdateDelayed(1.5f, 1f, ShowTime);
		this.StartSlowUpdateDelayed(1.5f, 1f, UpdateMissileWarning);
		if (aircraft.radar != null)
		{
			scanLine.enabled = true;
			aircraft.radar.onScan += TacScreen_OnRadarScan;
		}
		else
		{
			scanLine.enabled = false;
			opticalScanner = aircraft.gameObject.GetComponentInChildren<TargetDetector>();
			opticalScanner.onScan += TacScreen_OnOpticalScan;
		}
		RectTransform component = iconLayer.GetComponent<RectTransform>();
		metersPerPixel = component.rect.height / displaySize;
		aircraft.onRadarWarning += TacScreen_OnRadarWarning;
	}

	private void OnDestroy()
	{
		if (aircraft != null)
		{
			aircraft.onDisableUnit -= TacScreen_OnAircraftDisabled;
			aircraft.onRadarWarning -= TacScreen_OnRadarWarning;
			if (aircraft.targetCam != null)
			{
				aircraft.targetCam.onCamToggle -= TacScreen_OnCamToggle;
			}
			if (aircraft.radar != null)
			{
				aircraft.radar.onScan -= TacScreen_OnRadarScan;
			}
		}
		if (opticalScanner != null)
		{
			opticalScanner.onScan -= TacScreen_OnOpticalScan;
		}
	}

	private void UpdateMissileWarning()
	{
		if (aircraft.GetMissileWarningSystem().knownMissiles.Count <= 0)
		{
			return;
		}
		foreach (Missile knownMissile in aircraft.GetMissileWarningSystem().knownMissiles)
		{
			GameObject obj = UnityEngine.Object.Instantiate(missilePrefab, iconLayer);
			Vector3 vector = aircraft.transform.InverseTransformPoint(knownMissile.transform.position - aircraft.transform.position);
			Color color = ThemeManager.Active.ColorTheme.MapIconHostileSelected;
			if (vector.magnitude < displaySize / 4f)
			{
				color = ThemeManager.Active.ColorTheme.MapIconHostile;
			}
			else if (vector.magnitude < displaySize / 2f)
			{
				color = Color.Lerp(ThemeManager.Active.ColorTheme.MapIconHostileSelected, ThemeManager.Active.ColorTheme.MapIconHostile, 0.5f);
			}
			vector *= metersPerPixel;
			obj.transform.localPosition = new Vector3(vector.x, vector.z, 0f);
			obj.GetComponentInChildren<Image>().color = color;
			UnityEngine.Object.Destroy(obj, 1f);
		}
	}

	private void TacScreen_OnRadarWarning(Aircraft.OnRadarWarning source)
	{
		float num = Vector3.Distance(source.emitter.transform.position, aircraft.transform.position);
		Color color = ThemeManager.Active.ColorTheme.DetectedPing.WithAlpha(0.5f);
		Color color2 = ThemeManager.Active.ColorTheme.TargetPing.WithAlpha(0.5f);
		Color color3 = (source.isTarget ? color2 : color);
		bool flag = num < displaySize / 2f;
		if (aircraft.NetworkHQ.trackingDatabase.ContainsKey(source.emitter.persistentID) && flag)
		{
			float num2 = 2f;
			GameObject obj = UnityEngine.Object.Instantiate(radarUnitPrefab, iconLayer);
			Vector3 vector = aircraft.transform.InverseTransformPoint(source.emitter.transform.position - aircraft.transform.position);
			vector *= metersPerPixel;
			obj.transform.localPosition = new Vector3(vector.x, vector.z, 0f);
			Image component = obj.GetComponent<Image>();
			component.sprite = source.emitter.definition.hostileIcon;
			if (source.detected)
			{
				component.color = color3;
				num2 += 1f;
			}
			UnityEngine.Object.Destroy(obj, num2);
		}
		if (!aircraft.NetworkHQ.trackingDatabase.ContainsKey(source.emitter.persistentID) || !flag || source.isTarget)
		{
			float num3 = 1f;
			GameObject gameObject = UnityEngine.Object.Instantiate(pingPrefab, iconLayer);
			Vector3 position = source.emitter.transform.position;
			position = new Vector3(position.x, 0f, position.z);
			Vector3 position2 = aircraft.transform.position;
			position2 = new Vector3(position2.x, 0f, position2.z);
			Vector3 forward = aircraft.transform.forward;
			forward = new Vector3(forward.x, 0f, forward.z);
			Vector3 normalized = (position - position2).normalized;
			float num4 = Vector3.SignedAngle(forward, normalized, aircraft.transform.up);
			gameObject.transform.localEulerAngles = new Vector3(0f, 0f, 0f - num4);
			if (source.detected)
			{
				gameObject.GetComponent<Image>().color = color3;
				num3 += 1f;
			}
			UnityEngine.Object.Destroy(gameObject, num3);
		}
	}

	private void TacScreen_OnRadarScan()
	{
		headingAtScan = aircraft.transform.forward;
		foreach (Unit detectedTarget in aircraft.radar.detectedTargets)
		{
			GameObject obj = UnityEngine.Object.Instantiate(iconPrefab, iconLayer);
			Vector3 vector = aircraft.transform.InverseTransformPoint(detectedTarget.transform.position - aircraft.transform.position);
			vector *= metersPerPixel;
			obj.transform.localPosition = new Vector3(vector.x, vector.z, 0f);
			UnityEngine.Object.Destroy(obj, 4f);
		}
	}

	private void TacScreen_OnOpticalScan()
	{
		headingAtScan = aircraft.transform.forward;
		Vector3 normalized = new Vector3(aircraft.transform.forward.x, 0f, aircraft.transform.forward.z).normalized;
		foreach (Unit detectedTarget in opticalScanner.detectedTargets)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(iconPrefab, iconLayer);
			Vector3 vector = detectedTarget.transform.position - aircraft.transform.position;
			vector.y = 0f;
			vector *= metersPerPixel;
			gameObject.transform.localPosition = new Vector3(Vector3.Dot(vector, Vector3.Cross(normalized, -Vector3.up)), Vector3.Dot(normalized, vector), 0f);
			UnityEngine.Object.Destroy(gameObject, 3f);
		}
	}

	private void ScanRadar()
	{
		scanLine.transform.localEulerAngles = Vector3.forward * Mathf.Sin(Time.timeSinceLevelLoad * 0.5f * MathF.PI) * 26f;
		iconLayer.transform.localEulerAngles = Vector3.forward * TargetCalc.GetAngleOnAxis(aircraft.transform.forward, headingAtScan, -Vector3.up);
	}

	private void ScanOptical()
	{
		scanLine.transform.localEulerAngles = Vector3.forward * Time.timeSinceLevelLoad * -180f;
		iconLayer.transform.localEulerAngles = Vector3.forward * TargetCalc.GetAngleOnAxis(aircraft.transform.forward, headingAtScan, -Vector3.up);
	}

	private void TacScreen_OnCamToggle(TargetCam.OnCamToggle e)
	{
		if (e.camMode != TargetCam.CamMode.landingMode)
		{
			targetCamDisplay.SetActive(e.enabled);
		}
		else
		{
			landingCamDisplay.SetActive(e.enabled);
		}
		if (targetCamHidesTime)
		{
			timeObject.SetActive(!e.enabled);
		}
	}

	private void TacScreen_OnAircraftDisabled(Unit unit)
	{
		UnityEngine.Object.Destroy(base.gameObject);
	}

	private void ShowTime()
	{
		timeDisplay.text = UnitConverter.TimeOfDay(NetworkSceneSingleton<LevelInfo>.i.timeOfDay, includeSeconds: true);
	}

	private void Update()
	{
		if (Time.timeSinceLevelLoad - lastUpdate < 0.05f)
		{
			return;
		}
		lastUpdate = Time.timeSinceLevelLoad;
		float num = 2f;
		if (NetworkSceneSingleton<LevelInfo>.i.timeOfDay < 6f || NetworkSceneSingleton<LevelInfo>.i.timeOfDay > 18f)
		{
			num = 0.5f;
		}
		if (!NetworkSceneSingleton<LevelInfo>.i.PostProcessing.enabled)
		{
			num /= 5f;
		}
		screenMaterial.SetColor("_EmissionColor", Color.white * num);
		if (radarCone == null || aircraft.radar == null)
		{
			ScanOptical();
			return;
		}
		if (aircraft.radar.activated && !radarOn)
		{
			radarCone.enabled = true;
			scanLine.enabled = true;
			radarOn = true;
		}
		if (!aircraft.radar.activated && radarOn)
		{
			radarCone.enabled = false;
			scanLine.enabled = false;
			radarOn = false;
		}
		if (radarOn)
		{
			ScanRadar();
		}
	}
}
