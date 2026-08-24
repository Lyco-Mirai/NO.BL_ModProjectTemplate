using System;
using NuclearOption.Networking;
using UnityEngine;

public class Laser : Weapon
{
	[SerializeField]
	private float power;

	[SerializeField]
	private float maxAngle;

	[SerializeField]
	private float trackingRate;

	[SerializeField]
	private AnimationCurve damageAtRange;

	[ColorUsage(true, true)]
	[SerializeField]
	private Color color;

	private ParticleSystem[] hitParticles;

	[SerializeField]
	private ParticleSystem[] muzzleParticles;

	[SerializeField]
	private GameObject hitEffectPrefab;

	[SerializeField]
	private Renderer beamRenderer;

	private GameObject hitEffectSpawn;

	private Transform beamTransform;

	private Transform currentTargetTransform;

	private Transform hitTransform;

	private Vector3 hitOffset;

	[SerializeField]
	private float blastDamage;

	[SerializeField]
	private float fireDamage;

	[SerializeField]
	private Transform directionTransform;

	[SerializeField]
	private AudioClip fireStart;

	[SerializeField]
	private AudioClip fireSustained;

	[SerializeField]
	private AudioClip fireEnd;

	[SerializeField]
	private bool modifyPitch;

	[SerializeField]
	private float pitchFallRate = 1f;

	[SerializeField]
	private float startPitchMultiplier = 1f;

	private float startPitch;

	private bool fireCommanded;

	private AudioSource[] sources;

	[SerializeField]
	private float volume = 1f;

	[SerializeField]
	private float pitch = 1f;

	private PowerSupply powerSupply;

	[SerializeField]
	private bool vehicularPowerSupply;

	private float lastDamageTick;

	private float fireTime;

	private float laserLastFired;

	private float previousLastFired;

	private float beamScale;

	private void Start()
	{
		beamRenderer.enabled = false;
		beamTransform = beamRenderer.transform;
		beamScale = beamRenderer.transform.localScale.x;
		lastDamageTick = Time.timeSinceLevelLoad;
		sources = new AudioSource[3];
		for (int i = 0; i < sources.Length; i++)
		{
			AudioSource audioSource = base.gameObject.AddComponent<AudioSource>();
			audioSource.outputAudioMixerGroup = SoundManager.i.EffectsMixer;
			audioSource.spatialBlend = 1f;
			audioSource.spread = 20f;
			audioSource.dopplerLevel = 0f;
			audioSource.minDistance = 10f;
			audioSource.maxDistance = 50f;
			sources[i] = audioSource;
		}
		sources[0].clip = fireStart;
		sources[1].clip = fireSustained;
		sources[2].clip = fireEnd;
		sources[1].loop = true;
		if (modifyPitch)
		{
			startPitch = pitch * startPitchMultiplier;
			sources[1].pitch = startPitch;
		}
	}

	public override void AttachToUnit(Unit unit)
	{
		base.AttachToUnit(unit);
		powerSupply = unit.GetPowerSupply();
		powerSupply.AddUser();
		attachedUnit.onDisableUnit += AttachedUnit_onDisableUnit;
	}

	private void AttachedUnit_onDisableUnit(Unit obj)
	{
		beamRenderer.enabled = false;
	}

	public void OnDestroy()
	{
		beamRenderer.enabled = false;
		if (powerSupply != null)
		{
			powerSupply.RemoveUser();
		}
		attachedUnit.onDisableUnit -= AttachedUnit_onDisableUnit;
	}

	private void LaserSound()
	{
		if (beamRenderer.enabled)
		{
			if (fireTime > 0.2f && !sources[1].isPlaying)
			{
				sources[1].Play();
			}
			if (fireTime == 0f && Time.timeSinceLevelLoad - laserLastFired > 0.3f)
			{
				sources[0].PlayOneShot(fireStart);
			}
			fireTime += Time.deltaTime;
			laserLastFired = Time.timeSinceLevelLoad;
		}
		else
		{
			if (sources[1].isPlaying)
			{
				sources[1].Stop();
			}
			if (fireTime > 0f)
			{
				sources[2].Play();
			}
			fireTime = 0f;
		}
		if (sources[1].isPlaying)
		{
			if (modifyPitch)
			{
				if (sources[1].pitch > pitch)
				{
					sources[1].pitch -= Time.deltaTime * pitchFallRate;
				}
				else
				{
					sources[1].pitch = pitch;
				}
			}
		}
		else if (modifyPitch)
		{
			if (sources[1].pitch < startPitch)
			{
				sources[1].pitch += Time.deltaTime * pitchFallRate;
			}
			else
			{
				sources[1].pitch = startPitch;
			}
		}
	}

	public override void SetTarget(Unit target)
	{
		base.enabled = true;
		currentTargetTransform = ((target != null) ? target.GetRandomPart() : null);
		currentTarget = target;
	}

	public override void Fire(Unit owner, Unit target, Vector3 inheritedVelocity, WeaponStation weaponStation, GlobalPosition aimpoint)
	{
		if (!base.enabled)
		{
			base.enabled = true;
		}
		fireCommanded = true;
		previousLastFired = lastFired;
		lastFired = Time.timeSinceLevelLoad;
		weaponStation.LastFiredTime = Time.timeSinceLevelLoad;
		if (hitEffectSpawn == null)
		{
			hitEffectSpawn = UnityEngine.Object.Instantiate(hitEffectPrefab, null);
			hitParticles = hitEffectSpawn.GetComponentsInChildren<ParticleSystem>();
		}
	}

	public void LateUpdate()
	{
		if (lastFired == previousLastFired || Time.timeSinceLevelLoad > lastFired + 0.2f)
		{
			fireCommanded = false;
			beamRenderer.enabled = false;
			base.enabled = false;
			LaserSound();
		}
		if (beamRenderer.enabled && hitTransform != null)
		{
			Vector3 vector = hitTransform.TransformPoint(hitOffset);
			beamTransform.LookAt(vector);
			beamTransform.localScale = new Vector3(beamScale, beamScale, FastMath.Distance(beamTransform.position, vector));
		}
	}

	private void FixedUpdate()
	{
		Vector3 vector = ((currentTargetTransform != null) ? currentTargetTransform.position : (base.transform.position + base.transform.forward * 20000f));
		if (currentTarget != null && !attachedUnit.NetworkHQ.IsTargetBeingTracked(currentTarget) && attachedUnit.NetworkHQ.TryGetKnownPosition(currentTarget, out var knownPosition))
		{
			vector = knownPosition.ToLocalPosition();
		}
		Vector3 vector2 = vector - base.transform.position;
		directionTransform.rotation = Quaternion.LookRotation(Vector3.RotateTowards(base.transform.forward, vector2, maxAngle * (MathF.PI / 180f), 0f));
		float num = Vector3.Angle(directionTransform.forward, vector2);
		if (num > 1f)
		{
			vector = base.transform.position + directionTransform.forward * 20000f;
		}
		if (fireCommanded && num < 1f)
		{
			ParticleSystem[] array = muzzleParticles;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Play();
			}
			float num2 = 1f;
			if (!vehicularPowerSupply)
			{
				float powerRequested = ((fireTime < 0.2f) ? (power * 0.1f) : power);
				num2 = Mathf.Clamp01(powerSupply.DrawPower(powerRequested) / power);
			}
			beamRenderer.enabled = true;
			beamRenderer.material.SetVector("_WorldOffset", Datum.originPosition);
			beamRenderer.material.SetColor("_Color", color * num2);
			if (Physics.Linecast(directionTransform.position, vector, out var hitInfo, ~(int)PhysicsLayers.ExclusionZonesMask))
			{
				hitTransform = hitInfo.collider.transform;
				hitOffset = hitTransform.InverseTransformPoint(hitInfo.point);
				if (hitEffectSpawn != null)
				{
					hitEffectSpawn.transform.SetParent(hitInfo.transform);
					hitEffectSpawn.transform.position = hitInfo.point;
				}
				array = hitParticles;
				foreach (ParticleSystem particleSystem in array)
				{
					if (particleSystem != null)
					{
						particleSystem.Play();
					}
				}
				IDamageable component = hitInfo.collider.gameObject.GetComponent<IDamageable>();
				if (NetworkManagerNuclearOption.i.Server.Active && component != null && Time.timeSinceLevelLoad - lastDamageTick > 0.2f)
				{
					lastDamageTick = Time.timeSinceLevelLoad;
					float num3 = damageAtRange.Evaluate(hitInfo.distance) * num2;
					component.TakeDamage(0f, blastDamage * num3 * 0.2f, 1f, fireDamage * num3 * 0.2f, 0f, attachedUnit.persistentID);
				}
			}
		}
		else
		{
			beamRenderer.enabled = false;
		}
		LaserSound();
	}
}
