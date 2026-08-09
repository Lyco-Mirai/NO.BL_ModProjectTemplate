using System.Collections.Generic;
using NuclearOption.UIStyleSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDCargoState : HUDWeaponState
{
	[SerializeField]
	private GameObject capturePanel;

	[SerializeField]
	private RectTransform captureBar;

	[SerializeField]
	private Image capturePanelImage;

	[SerializeField]
	private Image captureBarImage;

	[SerializeField]
	private TMP_Text currentAirbaseTitle;

	[SerializeField]
	private TMP_Text captureText;

	[SerializeField]
	private CanvasGroup captureCanvasGroup;

	private float fadeTarget;

	private float fadeSmoothed;

	private Image targetDesignator;

	private WeaponStation weaponStation;

	private float lastAirbaseCheck;

	private float progressSmoothed;

	private float progressSmoothingVel;

	private Airbase currentAirbase;

	private Vector2 captureBarSize;

	public override void SetHUDWeaponState(Image targetDesignator, Aircraft aircraft, WeaponStation weaponStation)
	{
		this.weaponStation = weaponStation;
		weaponInfo = weaponStation.WeaponInfo;
		this.targetDesignator = targetDesignator;
		targetDesignator.transform.localScale = Vector3.one;
		SceneSingleton<FlightHud>.i.waterline.enabled = true;
		SceneSingleton<FlightHud>.i.velocityVector.transform.localScale = Vector3.one;
		captureBarSize = captureBar.sizeDelta;
		capturePanel.SetActive(value: false);
		fadeTarget = 0f;
		progressSmoothed = 0.5f;
	}

	public void RefreshNearestAirbase(Aircraft aircraft)
	{
		if (Time.timeSinceLevelLoad - lastAirbaseCheck < 1f)
		{
			return;
		}
		lastAirbaseCheck = Time.timeSinceLevelLoad;
		currentAirbase = null;
		if (aircraft.speed < 20f)
		{
			float b = float.MaxValue;
			GlobalPosition b2 = aircraft.GlobalPosition();
			foreach (KeyValuePair<string, Airbase> item in FactionRegistry.airbaseLookup)
			{
				Airbase value = item.Value;
				if (FastMath.InRange(value.center.GlobalPosition(), b2, Mathf.Min(value.GetRadius(), b)))
				{
					b = FastMath.Distance(value.center.GlobalPosition(), b2);
					currentAirbase = value;
				}
			}
		}
		fadeTarget = ((currentAirbase != null) ? 1 : 0);
		if (fadeTarget != 0f)
		{
			string factionTag = aircraft.NetworkHQ.faction.factionTag;
			string text = ((currentAirbase.CurrentHQ != null) ? " Defending" : " Capturing");
			if (currentAirbase.capture.controlBalance == 1f)
			{
				text = "";
			}
			if (currentAirbase.CurrentHQ != null)
			{
				factionTag = currentAirbase.CurrentHQ.faction.factionTag;
			}
			currentAirbaseTitle.text = currentAirbase.SavedAirbase.DisplayName;
			string text2 = "";
			if (currentAirbase.capture.controlBalance < 1f)
			{
				text2 = $" {currentAirbase.capture.controlBalance * 100f:F0}%";
			}
			captureText.text = "(" + factionTag + text + text2 + ")";
			if (currentAirbase.CurrentHQ == null)
			{
				captureBarImage.color = ThemeManager.Active.ColorTheme.AllClear;
			}
			else
			{
				bool flag = currentAirbase.CurrentHQ == aircraft.NetworkHQ;
				captureBarImage.color = (flag ? ThemeManager.Active.ColorTheme.AllClear : (ThemeManager.Active.ColorTheme.Alert + ThemeManager.Active.ColorTheme.AllClear * 0.25f));
			}
			capturePanelImage.color = captureBarImage.color.MultiplyRGB(0.1f, 0.7f);
			currentAirbaseTitle.color = captureBarImage.color;
			captureText.color = captureBarImage.color;
		}
	}

	public override void HUDFixedUpdate(Aircraft aircraft, List<Unit> targetList)
	{
		RefreshNearestAirbase(aircraft);
		if (fadeTarget == 0f && fadeSmoothed <= 0f)
		{
			if (capturePanel.activeSelf)
			{
				capturePanel.SetActive(value: false);
			}
			return;
		}
		if (!capturePanel.activeSelf)
		{
			capturePanel.SetActive(value: true);
		}
		fadeSmoothed += Mathf.Clamp(fadeTarget - fadeSmoothed, -2f * Time.fixedDeltaTime, 2f * Time.fixedDeltaTime);
		captureCanvasGroup.alpha = fadeSmoothed;
		if (fadeTarget > 0f)
		{
			if (currentAirbase.capture.controlBalance < 1f)
			{
				progressSmoothed = Mathf.SmoothDamp(progressSmoothed, currentAirbase.capture.controlBalance, ref progressSmoothingVel, 1f);
			}
			else
			{
				progressSmoothed = 1f;
			}
			captureBar.sizeDelta = captureBarSize * new Vector2(progressSmoothed, 1f);
		}
	}

	public override void UpdateWeaponDisplay(Aircraft aircraft, List<Unit> targetList)
	{
	}
}
