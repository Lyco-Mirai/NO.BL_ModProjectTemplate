using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class ChaffEjector : Countermeasure
{
	[Serializable]
	private class ChaffDoor
	{
		[SerializeField]
		private Transform rotator;

		[SerializeField]
		private float openAngle;

		[SerializeField]
		private float openSpeed;

		[SerializeField]
		private float openTime;

		[SerializeField]
		private float closeSpeed;

		private float openAmount;

		public bool Animate(float lastEjectionTime)
		{
			bool result = true;
			if (Time.timeSinceLevelLoad - lastEjectionTime < openTime)
			{
				openAmount += openSpeed * Time.deltaTime;
			}
			else
			{
				openAmount -= closeSpeed * Time.deltaTime;
				if (openAmount < 0f)
				{
					result = false;
				}
			}
			openAmount = Mathf.Clamp01(openAmount);
			rotator.transform.localEulerAngles = new Vector3(0f, 0f, openAmount * openAngle);
			return result;
		}
	}

	[Serializable]
	public class EjectionPoint
	{
		public UnitPart part;

		public Transform transform;

		[NonSerialized]
		public AudioSource sound;
	}

	[SerializeField]
	private GameObject chaffPrefab;

	[SerializeField]
	private float ejectionVelocity;

	[SerializeField]
	private float ejectionVelocityVariance;

	[SerializeField]
	private EjectionPoint[] ejectionPoints;

	[SerializeField]
	private ChaffDoor[] flareDoors;

	[Tooltip("in seconds")]
	[SerializeField]
	private float ejectionInterval;

	[SerializeField]
	private int ejectionGrouping = 2;

	[SerializeField]
	private AudioClip ejectionSound;

	[SerializeField]
	private float ejectionVolume;

	private int ejectionIndex;

	private int maxAmmo;

	private float lastEjectionTime;

	protected override void Awake()
	{
		maxAmmo = ammo;
		base.enabled = false;
		base.Awake();
	}

	public override List<string> GetThreatTypes()
	{
		if (threatTypes == null)
		{
			threatTypes = new List<string> { "ARH", "SARH" };
		}
		return threatTypes;
	}

	public override void Fire()
	{
		if (maxAmmo == -1)
		{
			maxAmmo = ammo;
		}
		if (aircraft.disabled || Time.timeSinceLevelLoad - lastEjectionTime < ejectionInterval)
		{
			return;
		}
		lastEjectionTime = Time.timeSinceLevelLoad;
		for (int i = 0; i < ejectionGrouping; i++)
		{
			if (ejectionIndex >= ejectionPoints.Length)
			{
				ejectionIndex = 0;
			}
			if (ammo > 0 && ejectionPoints[ejectionIndex].transform != null)
			{
				EjectChaff(aircraft, ejectionPoints[ejectionIndex]).Forget();
			}
			ejectionIndex++;
		}
		if (GameManager.IsLocalAircraft(aircraft))
		{
			UpdateHUD();
		}
	}

	private async UniTask EjectChaff(Aircraft aircraft, EjectionPoint ejectionPoint)
	{
		await UniTask.WaitForFixedUpdate();
		base.enabled = true;
		GameObject obj = NetworkSceneSingleton<Spawner>.i.SpawnLocal(chaffPrefab, Datum.origin);
		obj.transform.position = ejectionPoint.transform.position;
		obj.GetComponent<RadarChaff>().LaunchChaff(launchVelocity: ejectionPoint.part.rb.velocity + ejectionPoint.transform.forward * (ejectionVelocity + (float)UnityEngine.Random.Range(-1, 1) * ejectionVelocityVariance * ejectionVelocity), aircraft: aircraft, launchPoint: ejectionPoint.transform);
		ammo--;
		AudioSource audioSource = ejectionPoint.sound;
		if (audioSource == null)
		{
			audioSource = (ejectionPoint.sound = ejectionPoint.transform.gameObject.AddComponent<AudioSource>());
			audioSource.outputAudioMixerGroup = SoundManager.i.EffectsMixer;
			audioSource.clip = ejectionSound;
			audioSource.volume = ejectionVolume;
			audioSource.spatialBlend = 1f;
			audioSource.dopplerLevel = 0f;
			audioSource.spread = 5f;
			audioSource.maxDistance = 40f;
			audioSource.minDistance = 5f;
		}
		audioSource.pitch = UnityEngine.Random.Range(0.8f, 1.2f);
		audioSource.PlayOneShot(ejectionSound);
	}

	public override void Rearm(Aircraft aircraft, Unit rearmer)
	{
		if (ammo != maxAmmo)
		{
			ammo = maxAmmo;
			if (GameManager.IsLocalAircraft(aircraft))
			{
				UpdateHUD();
				SceneSingleton<AircraftActionsReport>.i.ReportText("Chaff rearmed by " + rearmer.unitName, 5f);
			}
		}
	}

	public override void UpdateHUD()
	{
		SceneSingleton<CombatHUD>.i.DisplayCountermeasures(displayName, displayImage, ammo);
	}

	private void Update()
	{
		ChaffDoor[] array = flareDoors;
		for (int i = 0; i < array.Length; i++)
		{
			if (!array[i].Animate(lastEjectionTime))
			{
				base.enabled = false;
			}
		}
	}

	public int GetAmmo()
	{
		return ammo;
	}

	public int GetMaxAmmo()
	{
		return maxAmmo;
	}
}
