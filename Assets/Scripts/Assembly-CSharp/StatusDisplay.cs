using System.Collections.Generic;
using NuclearOption.UIStyleSystem;
using UnityEngine;
using UnityEngine.UI;

public class StatusDisplay : MonoBehaviour
{
	[SerializeField]
	private List<PartStatusDisplay> statusDisplays = new List<PartStatusDisplay>();

	[SerializeField]
	private Image aircraftBackground;

	[SerializeField]
	private List<GameObject> failureIndicators = new List<GameObject>();

	private Dictionary<string, GameObject> failureIndicatorsLookup = new Dictionary<string, GameObject>();

	private AudioSource damageAlertSource;

	private List<IReportDamage> damageReporters = new List<IReportDamage>();

	private bool initialized;

	private float displayTimer = 10f;

	private List<AudioClip> messageQueue = new List<AudioClip>();

	private Aircraft aircraft;

	public void Initialize(Aircraft aircraft)
	{
		for (int i = 0; i < statusDisplays.Count; i++)
		{
			statusDisplays[i].DamageSubscribe(aircraft, this);
		}
		for (int j = 0; j < failureIndicators.Count; j++)
		{
			failureIndicatorsLookup.Add(failureIndicators[j].name, failureIndicators[j]);
			failureIndicators[j].SetActive(value: false);
		}
		foreach (UnitPart item in aircraft.partLookup)
		{
			if (item.TryGetComponent<IReportDamage>(out var component))
			{
				component.onReportDamage += StatusDisplay_OnReportDamage;
				damageReporters.Add(component);
			}
		}
		foreach (DamageablePart damageable in aircraft.damageables)
		{
			if (damageable.Damageable is IReportDamage reportDamage)
			{
				reportDamage.onReportDamage += StatusDisplay_OnReportDamage;
				damageReporters.Add(reportDamage);
			}
		}
		this.aircraft = aircraft;
		aircraft.onDisableUnit += StatusDisplay_OnDisable;
		damageAlertSource = aircraft.CockpitRB().gameObject.AddComponent<AudioSource>();
		damageAlertSource.outputAudioMixerGroup = SoundManager.i.InterfaceMixer;
		damageAlertSource.bypassEffects = true;
		damageAlertSource.bypassListenerEffects = true;
		damageAlertSource.spatialize = false;
		damageAlertSource.volume = 1f;
		initialized = true;
		aircraftBackground.color = new Color(1f, 1f, 1f, 1f);
		base.transform.SetParent(SceneSingleton<FlightHud>.i.statusAnchor);
		base.transform.localPosition = Vector3.zero;
	}

	private void StatusDisplay_OnReportDamage(OnReportDamage e)
	{
		if (e.audioReport != null)
		{
			AddMessage(e.audioReport);
		}
		if (failureIndicatorsLookup.ContainsKey(e.failureMessage))
		{
			failureIndicatorsLookup[e.failureMessage].SetActive(value: true);
		}
		SceneSingleton<AircraftActionsReport>.i.ReportText(e.failureMessage.AddColor(ThemeManager.Active.ColorTheme.Alert), 5f);
		aircraftBackground.color = Color.white;
	}

	private void StatusDisplay_OnDisable(Unit unit)
	{
		damageAlertSource.Stop();
		Object.Destroy(base.gameObject);
	}

	public void AddMessage(AudioClip message)
	{
		messageQueue.Add(message);
	}

	public void DisplayDamage()
	{
		aircraftBackground.color = Color.white;
		displayTimer = float.MaxValue;
		base.enabled = true;
	}

	private void OnDestroy()
	{
		if (!initialized)
		{
			return;
		}
		for (int i = 0; i < statusDisplays.Count; i++)
		{
			statusDisplays[i].DamageUnsubscribe();
		}
		foreach (IReportDamage damageReporter in damageReporters)
		{
			if ((bool)(damageReporter as Object))
			{
				damageReporter.onReportDamage -= StatusDisplay_OnReportDamage;
			}
		}
	}

	private void Update()
	{
		displayTimer -= Time.deltaTime;
		displayTimer = Mathf.Max(displayTimer, 0f);
		aircraftBackground.color = new Color(1f, 1f, 1f, displayTimer * 0.1f);
		if (displayTimer < 10f)
		{
			for (int i = 0; i < statusDisplays.Count; i++)
			{
				Color color = statusDisplays[i].partImage.color;
				color.a = (1f - statusDisplays[i].displayCondition) * displayTimer * 0.1f;
				statusDisplays[i].partImage.color = color;
			}
		}
		if (displayTimer <= 0f)
		{
			base.enabled = false;
		}
		if (messageQueue.Count > 0 && !aircraft.disabled && !damageAlertSource.isPlaying)
		{
			damageAlertSource.clip = messageQueue[0];
			damageAlertSource.Play();
			messageQueue.RemoveAt(0);
		}
	}
}
