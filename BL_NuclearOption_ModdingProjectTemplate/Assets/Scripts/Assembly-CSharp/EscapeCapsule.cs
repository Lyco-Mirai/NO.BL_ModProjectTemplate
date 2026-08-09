using System.Threading;
using Cysharp.Threading.Tasks;
using Mirage;
using NuclearOption.Networking;
using UnityEngine;

public class EscapeCapsule : MonoBehaviour
{
	[SerializeField]
	private AeroPart part;

	[SerializeField]
	private AeroPart[] partsToDetachFrom;

	[SerializeField]
	private ParticleSystem[] launchSystems;

	[SerializeField]
	private Transform forceTransform;

	[SerializeField]
	private float launchForce;

	[SerializeField]
	private float launchDuration;

	[SerializeField]
	private float aligningStrength;

	[SerializeField]
	private float aligningDamp;

	[SerializeField]
	private float cushionTravel;

	[SerializeField]
	private float cushionSpring;

	[SerializeField]
	private float cushionDamp;

	[SerializeField]
	private Parachute drogueChute;

	[SerializeField]
	private Parachute[] parachutes;

	[SerializeField]
	private Canopy[] canopies;

	[SerializeField]
	private Pilot[] pilots;

	[SerializeField]
	private GameObject splashPrefab;

	[SerializeField]
	private AudioClip launchSound;

	[SerializeField]
	private Transform[] cushionTransforms;

	[SerializeField]
	private Transform[] visibleCushionTransforms;

	[SerializeField]
	private Transform centerOfMass;

	private Aircraft aircraft;

	private bool launched;

	private bool parachutesDeployed;

	private bool landed;

	private bool inWater;

	private float radarAlt = 1000f;

	private float timeSinceLaunch;

	private Rigidbody rb;

	private Player player;

	private string unitName;

	private PersistentID ID;

	private FactionHQ HQ;

	private INetworkPlayer Owner;

	private void Awake()
	{
		base.enabled = false;
	}

	public void StartEjection()
	{
		if (!launched)
		{
			launched = true;
			aircraft = part.parentUnit as Aircraft;
			player = aircraft.Player;
			unitName = aircraft.unitName;
			ID = aircraft.persistentID;
			HQ = aircraft.NetworkHQ;
			Owner = aircraft.Owner;
			CreateLaunchSound();
			if (GameManager.IsLocalAircraft(aircraft))
			{
				player.ShowMap(5f);
			}
			part.DisableMaterialCleanup();
			ParticleSystem[] array = launchSystems;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Play();
			}
			this.StartSlowUpdateDelayed(1f, CheckRadarAlt);
			base.enabled = true;
			Eject().Forget();
		}
	}

	private void CreateLaunchSound()
	{
		AudioSource audioSource = base.gameObject.AddComponent<AudioSource>();
		audioSource.outputAudioMixerGroup = SoundManager.i.EffectsMixer;
		audioSource.bypassListenerEffects = true;
		audioSource.clip = launchSound;
		audioSource.volume = 1f;
		audioSource.dopplerLevel = 0f;
		audioSource.minDistance = 50f;
		audioSource.maxDistance = 1000f;
		audioSource.spatialBlend = 1f;
		audioSource.Play();
		Object.Destroy(audioSource, 10f);
	}

	private void CheckRadarAlt()
	{
		if (!landed)
		{
			radarAlt = base.transform.GlobalPosition().y;
			if (Physics.Raycast(base.transform.position, -Vector3.up, out var hitInfo, 20000f, (int)PhysicsLayers.StaticsMask | (int)PhysicsLayers.ShipsMask))
			{
				radarAlt = Mathf.Min(hitInfo.distance, radarAlt);
			}
			if (radarAlt < 2f && rb.velocity.sqrMagnitude < 1f)
			{
				landed = true;
				Disembark().Forget();
			}
		}
	}

	private void CushionLanding()
	{
		Transform[] array = visibleCushionTransforms;
		foreach (Transform obj in array)
		{
			obj.localScale = Vector3.Lerp(obj.localScale, Vector3.one, Time.fixedDeltaTime);
		}
		if (radarAlt > 15f || cushionDamp <= 0f)
		{
			return;
		}
		bool flag = false;
		array = cushionTransforms;
		foreach (Transform transform in array)
		{
			if (Physics.Raycast(transform.position, transform.forward, out var hitInfo, cushionTravel, ~(int)PhysicsLayers.ExclusionZonesMask))
			{
				flag = true;
				float num = (cushionTravel - hitInfo.distance) * cushionSpring;
				float num2 = Mathf.Max(Vector3.Dot(rb.GetPointVelocity(transform.position), transform.forward) * cushionDamp, 0f);
				rb.AddForceAtPosition(hitInfo.normal * (num + num2), hitInfo.point);
			}
		}
		if (rb.velocity.sqrMagnitude < 1f || flag)
		{
			cushionDamp *= 1f - Time.fixedDeltaTime;
			cushionDamp -= 200f * Time.fixedDeltaTime;
			cushionSpring *= 1f - Time.fixedDeltaTime;
			cushionSpring -= 200f * Time.fixedDeltaTime;
		}
	}

	private async UniTask Disembark()
	{
		CancellationToken cancel = base.destroyCancellationToken;
		await UniTask.Delay(1000);
		bool flag = false;
		Pilot[] array = pilots;
		for (int i = 0; i < array.Length; i++)
		{
			if (!array[i].dead)
			{
				flag = true;
			}
		}
		if (flag)
		{
			Canopy[] array2 = canopies;
			for (int i = 0; i < array2.Length; i++)
			{
				array2[i].OpenHinges();
			}
		}
		for (int i2 = pilots.Length - 1; i2 >= 0; i2--)
		{
			await UniTask.WaitForFixedUpdate();
			await UniTask.Delay(1000);
			if (cancel.IsCancellationRequested)
			{
				return;
			}
			if (!pilots[i2].dead && NetworkManagerNuclearOption.i.Server.Active)
			{
				pilots[i2].TogglePilotVisibility(enabled: false);
				SpawnDisembarkingPilot(i2);
			}
		}
		await UniTask.WaitForSeconds(1);
		if (cancel.IsCancellationRequested)
		{
			return;
		}
		if (SceneSingleton<CameraStateManager>.i.followingRB == rb)
		{
			SceneSingleton<CameraStateManager>.i.SetFollowingUnit(null);
		}
		if (!cancel.IsCancellationRequested)
		{
			await UniTask.WaitForSeconds(34);
			if (!cancel.IsCancellationRequested)
			{
				Object.Destroy(part);
				Object.Destroy(base.gameObject);
			}
		}
	}

	public void SpawnDisembarkingPilot(int pilotNumber)
	{
		UnitPart unitPart = pilots[pilotNumber].GetUnitPart();
		GameObject gameObject = Object.Instantiate(GameAssets.i.pilotDismounted, pilots[pilotNumber].transform.position, pilots[pilotNumber].transform.rotation);
		PilotDismounted component = gameObject.GetComponent<PilotDismounted>();
		component.NetworkparentUnit = ID;
		component.NetworkunitPart = unitPart.id;
		component.NetworkunitName = unitName + " pilot";
		component.NetworkHQ = HQ;
		component.NetworkpilotNumber = (byte)pilotNumber;
		component.NetworkstartPosition = pilots[pilotNumber].transform.position.ToGlobalPosition();
		pilots[pilotNumber].ejected = true;
		if (pilotNumber == 0)
		{
			component.Networkplayer = player;
		}
		NetworkManagerNuclearOption.i.ServerObjectManager.Spawn(gameObject, Owner);
	}

	private async UniTask CutDrogueChute()
	{
		CancellationToken cancel = base.destroyCancellationToken;
		await UniTask.WaitForSeconds(1);
		if (!cancel.IsCancellationRequested)
		{
			drogueChute.CutCanopy();
			drogueChute.gameObject.SetActive(value: false);
		}
	}

	private void WaterPhysics()
	{
		float y = base.transform.GlobalPosition().y;
		if (!(y < 1f))
		{
			return;
		}
		float num = (1f - y) * 30f * rb.mass;
		rb.drag = 1f;
		rb.angularDrag = 1f;
		rb.AddForce(Vector3.up * num);
		Vector3 torque = Vector3.Cross(base.transform.up, Vector3.up) * aligningStrength;
		part.rb.AddTorque(torque);
		if (!inWater)
		{
			inWater = true;
			if (SceneSingleton<ParticleEffectManager>.i != null)
			{
				SceneSingleton<ParticleEffectManager>.i.GetPrefabEffect(splashPrefab).Play(new Vector3(base.transform.position.x, Datum.LocalSeaY, base.transform.position.z), Quaternion.LookRotation(Vector3.up + rb.velocity * 0.05f));
			}
		}
	}

	private void FixedUpdate()
	{
		timeSinceLaunch += Time.fixedDeltaTime;
		WaterPhysics();
		if (!parachutesDeployed && part.rb.velocity.y < 0f && timeSinceLaunch > 1f && part.rb.velocity.sqrMagnitude < 22500f && part.rb.position.GlobalY() < 3000f)
		{
			parachutesDeployed = true;
			Parachute[] array = parachutes;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].enabled = true;
			}
			if (drogueChute != null)
			{
				CutDrogueChute().Forget();
			}
		}
		if (radarAlt < 50f && parachutesDeployed)
		{
			CushionLanding();
		}
	}

	private async UniTask Eject()
	{
		await UniTask.WaitForFixedUpdate();
		CancellationToken cancel = base.destroyCancellationToken;
		float ejectTime = 0f;
		AeroPart[] array = partsToDetachFrom;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].BreakJointsToPart(part);
		}
		part.BreakAllJoints();
		part.CreateRB(part.rb.GetPointVelocity(part.transform.position), part.transform.position);
		rb = part.rb;
		rb.centerOfMass = base.transform.InverseTransformPoint(centerOfMass.position);
		Parachute[] array2 = parachutes;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].SetAttachedPart(part);
		}
		parachutesDeployed = false;
		if (drogueChute != null && part.rb.velocity.sqrMagnitude > 22500f)
		{
			drogueChute.SetAttachedPart(part);
			drogueChute.enabled = true;
		}
		while (ejectTime < launchDuration)
		{
			await UniTask.WaitForFixedUpdate();
			if (cancel.IsCancellationRequested)
			{
				return;
			}
			ejectTime += Time.fixedDeltaTime;
			part.rb.AddForce(forceTransform.forward * launchForce);
			Vector3 normalized = part.rb.velocity.normalized;
			Vector3 vector = Vector3.Cross(new Vector3(part.rb.velocity.x, 0f, part.rb.velocity.z), new Vector3(normalized.x, 0f, normalized.z)) * aligningStrength;
			vector -= part.rb.angularVelocity * aligningDamp;
			Vector3 vector2 = Vector3.Cross(base.transform.up, Vector3.up) * aligningStrength;
			part.rb.AddTorque(vector + vector2);
		}
		if (cancel.IsCancellationRequested)
		{
			return;
		}
		while (ejectTime < 5f)
		{
			await UniTask.WaitForFixedUpdate();
			if (cancel.IsCancellationRequested)
			{
				return;
			}
			ejectTime += Time.fixedDeltaTime;
			Vector3 torque = Vector3.Cross(base.transform.forward, part.rb.velocity.normalized) * aligningStrength;
			torque -= part.rb.angularVelocity * aligningDamp;
			part.rb.AddTorque(torque);
		}
		part.rb.angularDrag = 0.1f;
		part.RemoveFromUnit();
	}
}
