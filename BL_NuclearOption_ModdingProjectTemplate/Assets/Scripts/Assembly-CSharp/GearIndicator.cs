using NuclearOption.UIStyleSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GearIndicator : HUDApp
{
	private Aircraft aircraft;

	[SerializeField]
	private float speedLimit;

	[SerializeField]
	private Image brakeIcon;

	[SerializeField]
	private Image gearIcon;

	[SerializeField]
	private Sprite brakeSprite;

	[SerializeField]
	private Sprite parkingBrakeSprite;

	[SerializeField]
	private AudioClip raiseGearVoice;

	[SerializeField]
	private AudioClip lowerGearVoice;

	[SerializeField]
	private bool lowerGearWarning;

	[SerializeField]
	private TextMeshProUGUI lowerGearText;

	private float lowerGearLastPlayed = -45f;

	private bool raiseGearPlayed;

	private AircraftParameters aircraftParameters;

	private ControlInputs inputs;

	private void Awake()
	{
		lowerGearText.enabled = false;
	}

	public override void RefreshSettings()
	{
	}

	public override void Initialize(Aircraft aircraft)
	{
		speedLimit /= 3.6f;
		this.aircraft = aircraft;
		inputs = aircraft.GetInputs();
		aircraftParameters = aircraft.definition.aircraftParameters;
		ThemeManager.ThemeGroupChanged += GearIndicator_OnThemeGroupChanged;
	}

	private void OnDestroy()
	{
		ThemeManager.ThemeGroupChanged -= GearIndicator_OnThemeGroupChanged;
	}

	public override void Refresh()
	{
		if (aircraft.gearState == LandingGear.GearState.LockedExtended)
		{
			if (!gearIcon.enabled)
			{
				gearIcon.enabled = true;
			}
			if (aircraft.speed > speedLimit)
			{
				gearIcon.color = ThemeManager.Active.ColorTheme.Alert;
				if (!raiseGearPlayed)
				{
					SoundManager.PlayInterfaceOneShot(raiseGearVoice);
					raiseGearPlayed = true;
				}
			}
			else
			{
				raiseGearPlayed = false;
				gearIcon.color = ThemeManager.Active.ColorTheme.AllClear;
			}
			bool flag = aircraft.speed < 1f && inputs.throttle < 0.1f;
			lowerGearText.enabled = false;
			brakeIcon.enabled = flag || inputs.brake > 0.02f;
		}
		else
		{
			raiseGearPlayed = false;
			gearIcon.color = ThemeManager.Active.ColorTheme.AllClear;
			if (aircraft.gearState == LandingGear.GearState.LockedRetracted && gearIcon.enabled)
			{
				gearIcon.enabled = false;
				brakeIcon.enabled = false;
			}
			if (aircraft.gearState == LandingGear.GearState.Retracting || aircraft.gearState == LandingGear.GearState.Extending)
			{
				Image image = gearIcon;
				bool flag2 = (base.enabled = Mathf.Sin(Time.timeSinceLevelLoad * 16f) > 0f);
				image.enabled = flag2;
			}
		}
		if (lowerGearWarning && aircraft.gearState == LandingGear.GearState.LockedRetracted)
		{
			if (lowerGearText.enabled && Time.timeSinceLevelLoad - lowerGearLastPlayed > 4f)
			{
				lowerGearText.enabled = false;
			}
			if (inputs.throttle < 0.5f && aircraft.radarAlt < 30f && aircraft.rb.velocity.y < -0.5f && aircraft.speed < aircraftParameters.takeoffSpeed * 1.5f && Time.timeSinceLevelLoad - lowerGearLastPlayed > 60f)
			{
				lowerGearLastPlayed = Time.timeSinceLevelLoad;
				SoundManager.PlayInterfaceOneShot(lowerGearVoice);
				lowerGearText.enabled = true;
			}
		}
	}

	private void GearIndicator_OnThemeGroupChanged()
	{
		Refresh();
	}
}
