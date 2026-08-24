using System;
using NuclearOption.DebugScripts;
using Unity.Profiling;
using UnityEngine;

public class SwashRotor : MonoBehaviour, IDamageable
{
	[Serializable]
	private class RotorSegment
	{
		public Transform transform;

		[SerializeField]
		private MeshFilter meshFilter;

		[SerializeField]
		private MeshRenderer meshRenderer;

		[SerializeField]
		private Material slowMaterial;

		[SerializeField]
		private Material fastMaterial;

		[SerializeField]
		private Mesh slowMesh;

		[SerializeField]
		private Mesh fastMesh;

		private bool detached;

		private bool blurred;

		public void Animate(float speedRatio)
		{
			if (detached)
			{
				return;
			}
			bool flag = speedRatio * Time.timeScale > 0.6f;
			if (blurred)
			{
				if (!flag)
				{
					blurred = false;
					meshFilter.mesh = slowMesh;
					meshRenderer.material = slowMaterial;
				}
			}
			else if (flag)
			{
				blurred = true;
				meshFilter.mesh = fastMesh;
				meshRenderer.material = fastMaterial;
			}
		}

		public void Detach(Aircraft aircraft, float tipSpeed, float mass)
		{
			detached = true;
			meshFilter.mesh = slowMesh;
			meshRenderer.material = slowMaterial;
			transform.SetParent(null);
			transform.gameObject.AddComponent<BoxCollider>();
			Rigidbody rigidbody = transform.gameObject.AddComponent<Rigidbody>();
			transform.gameObject.layer = PhysicsLayers.IgnoreCollisions;
			rigidbody.mass = mass;
			rigidbody.velocity = aircraft.rb.velocity + new Vector3(UnityEngine.Random.Range(0f - tipSpeed, tipSpeed), UnityEngine.Random.Range(0f, tipSpeed), UnityEngine.Random.Range(0f - tipSpeed, tipSpeed));
			rigidbody.angularVelocity = UnityEngine.Random.insideUnitSphere * 2f;
			rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
			rigidbody.drag = 0.1f;
			UnityEngine.Object.Destroy(transform.gameObject, 30f);
		}
	}

	private static readonly ProfilerMarker sampleForcesMarker = new ProfilerMarker("SwashRotor.SampleForces");

	[SerializeField]
	private Aircraft aircraft;

	[SerializeField]
	private RotorSegment[] segments;

	private RotorShaft rotorShaft;

	public Transform hinge;

	public Transform tip;

	[SerializeField]
	private ArmorProperties armorProperties;

	[SerializeField]
	private GameObject[] breakEffects;

	[SerializeField]
	private float stiffnessMultiplier = 1f;

	private Transform hub;

	private Transform span;

	private Transform pitchVis;

	private Transform flapVis;

	private Transform pitchTransform;

	private Transform[] velocityVis;

	private Transform[] liftVis;

	private int samples;

	private float length;

	private float originalLength;

	private float mass;

	private float originalMass;

	private float momentOfInertia;

	private float flapSpringUp;

	private float flapSpringDown;

	private float flapDamp;

	private float cyclicTravel;

	private float collectiveTravel;

	private float hubRadius;

	private float washout;

	private float flapAngularVelocity;

	private float flapAngle;

	private float shaftAngularSpeed;

	private float hitPoints = 100f;

	private ForceAndTorque forceAndTorque;

	private int lastAttachedSegmentIndex;

	private bool bendSegments;

	private byte damageableIndex;

	public void Setup(RotorShaft rotorShaft, byte damageableIndex, float directionMult, float length, float mass, float flapSpringUp, float flapSpringDown, float flapDamp, float cyclicTravel, float collectiveTravel, float washout, int samples)
	{
		forceAndTorque = default(ForceAndTorque);
		this.rotorShaft = rotorShaft;
		hub = rotorShaft.transform;
		this.damageableIndex = damageableIndex;
		this.samples = samples;
		this.length = length;
		originalLength = length;
		this.mass = mass;
		originalMass = mass;
		this.flapSpringUp = flapSpringUp;
		this.flapSpringDown = flapSpringDown;
		this.flapDamp = flapDamp;
		this.cyclicTravel = cyclicTravel;
		this.collectiveTravel = collectiveTravel;
		this.washout = washout;
		hubRadius = GetDistanceFromAxis();
		momentOfInertia = 0.33333f * mass * (length * length);
		lastAttachedSegmentIndex = segments.Length - 1;
		span = base.transform;
		tip = new GameObject("tip").transform;
		tip.SetParent(span);
		tip.localPosition = new Vector3(0f, 0f, length);
		tip.localEulerAngles = new Vector3(0f, 90f * directionMult, 0f);
		pitchTransform = new GameObject("pitchTransform").transform;
		pitchTransform.SetParent(span);
		pitchTransform.localEulerAngles = new Vector3(0f, 90f * directionMult, 0f);
		pitchTransform.localPosition = new Vector3(0f, 0f, 0f);
	}

	public void TakeDamage(float pierceDamage, float blastDamage, float amountAffected, float fireDamage, float impactDamage, PersistentID damagedBy)
	{
		float num = Mathf.Max(pierceDamage - armorProperties.pierceArmor, 0f) / armorProperties.pierceTolerance;
		float num2 = Mathf.Max(blastDamage - armorProperties.blastArmor, 0f) / armorProperties.blastTolerance;
		float num3 = Mathf.Max(fireDamage - armorProperties.fireArmor, 0f) / armorProperties.fireTolerance;
		float num4 = num + num2 + num3 + impactDamage;
		hitPoints -= num4;
		if (hitPoints <= 0f)
		{
			float impactDamage2 = UnityEngine.Random.Range(length * 0.5f, length * 0.9f) / originalLength;
			aircraft.Damage(damageableIndex, new DamageInfo(num, num2, num3, impactDamage2));
		}
	}

	public void ApplyDamage(float pierceDamage, float blastDamage, float fireDamage, float impactDamage)
	{
		length = originalLength * Mathf.Max(impactDamage, 0.1f);
		BreakSegments();
	}

	public void Detach(Vector3 velocity, Vector3 relativePos)
	{
	}

	public void TakeShockwave(Vector3 origin, float overpressure, float blastPower)
	{
		if (overpressure > armorProperties.overpressureLimit)
		{
			TakeDamage(0f, overpressure - armorProperties.overpressureLimit, 1f, 0f, 0f, PersistentID.None);
		}
	}

	public ArmorProperties GetArmorProperties()
	{
		return armorProperties;
	}

	public Unit GetUnit()
	{
		return aircraft;
	}

	public Transform GetTransform()
	{
		return base.transform;
	}

	public float GetMass()
	{
		return mass;
	}

	public float GetDistanceFromAxis()
	{
		return FastMath.Distance(new Vector3(hinge.localPosition.x, 0f, hinge.localPosition.z), Vector3.zero);
	}

	private void SetupDebugVis()
	{
		if (!PlayerSettings.debugVis)
		{
			return;
		}
		if (DebugVis.Create(ref pitchVis, GameAssets.i.debugArrow.transform, span))
		{
			pitchVis.gameObject.GetComponent<MeshRenderer>().material.SetColor("_EmissionColor", Color.cyan);
			pitchVis.localPosition = new Vector3(0f, 0f, 1f);
			pitchVis.localEulerAngles = tip.localEulerAngles;
		}
		if (DebugVis.Create(ref flapVis, GameAssets.i.debugArrow.transform, span))
		{
			flapVis.gameObject.GetComponent<MeshRenderer>().material.SetColor("_EmissionColor", Color.magenta);
			flapVis.localPosition = new Vector3(0f, 0f, originalLength);
		}
		if (velocityVis == null || velocityVis.Length == 0)
		{
			velocityVis = new Transform[samples];
			liftVis = new Transform[samples];
			for (int i = 0; i < samples; i++)
			{
				velocityVis[i] = UnityEngine.Object.Instantiate(GameAssets.i.debugArrow, span).transform;
				DebugVis.AddMarker(velocityVis[i].gameObject);
				velocityVis[i].gameObject.GetComponent<MeshRenderer>().material.SetColor("_EmissionColor", new Color(1f, 1f, 1f, 1f));
				velocityVis[i].localPosition = new Vector3(0f, 0f, originalLength * (float)(i + 1) / (float)samples);
				velocityVis[i].localScale = new Vector3(0.5f, 0.5f, 0.5f);
				velocityVis[i].rotation = tip.rotation;
				liftVis[i] = UnityEngine.Object.Instantiate(GameAssets.i.debugArrow, span).transform;
				DebugVis.AddMarker(liftVis[i].gameObject);
				liftVis[i].gameObject.GetComponent<MeshRenderer>().material.SetColor("_EmissionColor", Color.yellow);
				liftVis[i].localPosition = new Vector3(0f, 0f, originalLength * (float)(i + 1) / (float)samples);
				liftVis[i].localScale = new Vector3(0.5f, 0.5f, 0.5f);
				liftVis[i].rotation = Quaternion.LookRotation(hinge.up);
			}
		}
	}

	private void RefreshDebug(Vector3 velocity, Vector3 force, int sample)
	{
		SetupDebugVis();
		velocityVis[sample].localScale = new Vector3(0.5f, 0.5f, velocity.magnitude * Time.fixedDeltaTime);
		velocityVis[sample].rotation = Quaternion.LookRotation(velocity);
		float z = force.magnitude * 0.0005f;
		liftVis[sample].localPosition = new Vector3(0f, 0f, length * (1f + (float)sample) / (float)samples);
		liftVis[sample].localScale = new Vector3(1f, 1f, z);
		liftVis[sample].rotation = Quaternion.LookRotation(force);
	}

	private void VisualizeFlap(Vector3 flapDirection)
	{
		SetupDebugVis();
		flapVis.localScale = new Vector3(0.5f, 0.5f, flapAngularVelocity * length);
		flapVis.localRotation = Quaternion.LookRotation(flapDirection);
	}

	public void SetPitch(Transform swashPlate, float swashPlatePitch, float swashPlateRoll, float collective, float directionMult)
	{
		float num = Vector3.Dot(hinge.forward, swashPlate.forward);
		float num2 = Vector3.Dot(hinge.forward, -swashPlate.right);
		float num3 = num * swashPlatePitch;
		float num4 = num2 * swashPlateRoll;
		float num5 = num3 * (0f - cyclicTravel) + num4 * cyclicTravel + collective * collectiveTravel;
		Vector3 localEulerAngles = span.localEulerAngles;
		localEulerAngles.z = num5 * directionMult;
		span.localEulerAngles = localEulerAngles;
	}

	private Vector3 SampleForce(float liftNumber, float stallAngle, float dragBase, float dragExponent, Vector3 airVelocity, out float angleOfAttack)
	{
		Vector3 normalized = Vector3.Cross(airVelocity, -tip.right).normalized;
		float sqrMagnitude = airVelocity.sqrMagnitude;
		Vector3 vector = pitchTransform.InverseTransformDirection(airVelocity);
		angleOfAttack = Mathf.Atan2(vector.y, vector.z) * 57.29578f;
		float num = angleOfAttack * liftNumber;
		if (Mathf.Abs(angleOfAttack) > stallAngle)
		{
			float num2 = num;
			float to = Mathf.Sin(2f * angleOfAttack * (MathF.PI / 180f)) * (liftNumber * stallAngle);
			num = Mathf.SmoothStep(num2, to, (Mathf.Abs(angleOfAttack) - stallAngle) * 0.1f);
		}
		float num3 = dragBase + (0f - Mathf.Cos(2f * angleOfAttack * (MathF.PI / 180f))) * dragExponent + dragExponent;
		Vector3 vector2 = num * sqrMagnitude * normalized;
		return num3 * sqrMagnitude * -airVelocity.normalized + vector2;
	}

	public ForceAndTorque SampleForces(Rigidbody rb, float angularSpeed, float directionMult, float liftNumber, float stallAngle, float dragBase, float dragExponent, Vector3 airVelocity, float deltaTime, out float angleOfAttack)
	{
		using (sampleForcesMarker.Auto())
		{
			forceAndTorque.Clear();
			shaftAngularSpeed = angularSpeed;
			Vector3 vector = hinge.right * directionMult;
			Vector3 vector2 = Vector3.Cross(span.forward, hinge.right);
			float num = Mathf.Cos(Mathf.Abs(flapAngle));
			angleOfAttack = 0f;
			Vector3 zero = Vector3.zero;
			Vector3 zero2 = Vector3.zero;
			float num2 = 0f;
			float num3 = length / (originalLength * (float)samples);
			for (int i = 0; i < samples; i++)
			{
				float num4 = length * (float)(i + 1) * num3;
				Vector3 vector3 = hinge.position + span.forward * num4;
				float num5 = 1f - num4 / originalLength;
				pitchTransform.localEulerAngles = new Vector3((0f - washout) * num5, 90f * directionMult, 0f);
				float num6 = angularSpeed * (num4 + hubRadius) * num;
				Vector3 vector4 = num6 * vector - airVelocity + rb.GetPointVelocity(vector3);
				vector4 += num4 * flapAngularVelocity * vector2;
				Vector3 vector5 = SampleForce(liftNumber, stallAngle, dragBase, dragExponent, vector4, out angleOfAttack);
				vector5 += originalMass * num3 * -9.81f * Vector3.up;
				if (PlayerSettings.debugVis)
				{
					RefreshDebug(vector4, vector5, i);
				}
				Vector3 vector6 = originalMass * num3 * (num6 * num6) / (num4 + hubRadius) * hinge.forward;
				vector5 += vector6;
				num2 += Vector3.Dot(vector5, vector2) * num4;
				zero += vector5;
				zero2 += Vector3.Cross(vector5, hub.position - vector3);
			}
			zero *= aircraft.airDensity;
			num2 /= stiffnessMultiplier;
			float num7 = ((flapAngle < 0f) ? flapSpringDown : flapSpringUp);
			if (Mathf.Abs(flapAngle) > 0.5f)
			{
				num7 *= 5f;
			}
			num2 += num7 * (0f - flapAngle);
			num2 += (0f - flapAngularVelocity) * flapDamp;
			flapAngularVelocity += deltaTime * num2 / momentOfInertia;
			flapAngle += deltaTime * flapAngularVelocity;
			float num8 = num2 / (length * 0.7f);
			zero -= num8 * vector2;
			zero2 -= Vector3.Cross(num8 * vector2, (hub.position - tip.position) * 0.7f);
			if (PlayerSettings.debugVis)
			{
				VisualizeFlap(vector2);
			}
			Vector3 localEulerAngles = span.localEulerAngles;
			localEulerAngles.x = (0f - flapAngle) * 57.29578f;
			span.localEulerAngles = localEulerAngles;
			return new ForceAndTorque(zero, zero2, useTorque: true);
		}
	}

	public void BendSegments(float speedRatio)
	{
		if (lastAttachedSegmentIndex < 0)
		{
			return;
		}
		for (int i = 0; i <= lastAttachedSegmentIndex; i++)
		{
			segments[i].Animate(speedRatio);
		}
		bool num = speedRatio < 0.2f && flapAngle < 0f;
		float num2 = (0f - flapAngle) * 57.29578f;
		if (num)
		{
			bendSegments = true;
			segments[0].transform.localEulerAngles = new Vector3((0f - num2) * 0.5f, 0f, 0f);
			float x = num2 / (float)(segments.Length - 1);
			for (int j = 1; j <= lastAttachedSegmentIndex; j++)
			{
				segments[j].transform.localEulerAngles = new Vector3(x, 0f, 0f);
			}
		}
		else if (bendSegments)
		{
			bendSegments = false;
			for (int k = 0; k <= lastAttachedSegmentIndex; k++)
			{
				segments[k].transform.localEulerAngles = Vector3.zero;
			}
		}
	}

	public float GetLength()
	{
		return length;
	}

	public void BreakRandomSegment()
	{
		length *= 0.5f;
		float impactDamage = length / originalLength;
		aircraft.Damage(damageableIndex, new DamageInfo(0f, 0f, 0f, impactDamage));
	}

	private void BreakSegments()
	{
		float num = length / originalLength;
		mass = originalMass * num;
		for (int num2 = lastAttachedSegmentIndex; num2 >= 0; num2--)
		{
			if (num < (0.5f + (float)num2) / (float)segments.Length)
			{
				lastAttachedSegmentIndex = num2 - 1;
				float num3 = shaftAngularSpeed * ((float)num2 / (float)segments.Length) * originalLength;
				segments[num2].Detach(aircraft, num3 * 0.5f, originalMass / (float)segments.Length);
			}
		}
	}

	public bool CheckCollisions()
	{
		RaycastHit hitInfo;
		bool flag = Physics.Linecast(span.position, span.position + span.forward * length, out hitInfo, ~((int)PhysicsLayers.ExclusionZonesMask | (int)PhysicsLayers.IgnoreCollisionsMask));
		float enter;
		bool flag2 = Datum.WaterPlane().Raycast(new Ray(span.position, span.forward), out enter) && enter < length && enter > 0f;
		if (!flag && !flag2)
		{
			return false;
		}
		if (!flag2)
		{
			enter = length;
		}
		float num = Mathf.Min(hitInfo.distance, enter);
		Vector3 inNormal = (flag ? hitInfo.normal : Vector3.up);
		Vector3 vector = hinge.forward * shaftAngularSpeed * length;
		rotorShaft.RotorStrike(length * 1000f);
		if (vector.sqrMagnitude < 100f)
		{
			return true;
		}
		if (num < length && aircraft.LocalSim)
		{
			aircraft.Damage(damageableIndex, new DamageInfo(0f, 0f, 0f, num / originalLength));
		}
		Vector3 forward = (Vector3.Reflect(vector.normalized, inNormal) + Vector3.Reflect(Vector3.Normalize(tip.position - span.position), inNormal)) / 2f;
		if (flag)
		{
			GameObject prefab = ((hitInfo.collider.sharedMaterial == GameAssets.i.terrainMaterial) ? GameAssets.i.rotorStrike_dirt : GameAssets.i.rotorStrike_solid);
			if (SceneSingleton<ParticleEffectManager>.i != null)
			{
				SceneSingleton<ParticleEffectManager>.i.GetPrefabEffect(prefab).Play(hitInfo.point, Quaternion.LookRotation(forward));
			}
		}
		else
		{
			Vector3 position = span.position + span.forward * enter;
			if (SceneSingleton<ParticleEffectManager>.i != null)
			{
				SceneSingleton<ParticleEffectManager>.i.GetPrefabEffect(GameAssets.i.rotorStrike_water).Play(position, Quaternion.LookRotation(forward));
			}
		}
		return true;
	}

	public void ShatterRotor()
	{
		if (aircraft.LocalSim)
		{
			aircraft.Damage(damageableIndex, new DamageInfo(0f, 0f, 0f, 0.1f));
		}
	}
}
