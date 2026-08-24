using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class LandingGear : MonoBehaviour
{
	[Serializable]
	public class GearDoor
	{
		public Transform transform;

		public Vector3 closedAngle;

		public Vector3 openAngle;

		public void Animate(float openAmount)
		{
			if (transform != null)
			{
				transform.localEulerAngles = Vector3.Lerp(closedAngle, openAngle, openAmount);
			}
		}
	}

	[Serializable]
	private class IKJoint
	{
		[SerializeField]
		private Transform hinge1;

		[SerializeField]
		private Transform hinge2;

		[SerializeField]
		private Transform elbow;

		private float a;

		private float b;

		public void Initialize()
		{
			a = FastMath.Distance(hinge1.position, elbow.position);
			b = FastMath.Distance(hinge2.position, elbow.position);
		}

		public void Update()
		{
			float num = FastMath.Distance(hinge1.position, hinge2.position);
			float num2 = (b * b - a * a - num * num) / (-2f * num);
			float num3 = Mathf.Sqrt(a * a - num2 * num2);
			Vector3 vector = Vector3.Lerp(hinge1.position, hinge2.position, num2 / num);
			Vector3 vector2 = FastMath.NormalizedDirection(hinge2.position, hinge1.position);
			Vector3 vector3 = Vector3.Cross(vector2, -elbow.right);
			Vector3 worldPosition = vector + vector3 * num3;
			hinge1.LookAt(worldPosition, vector2);
			hinge2.LookAt(worldPosition, vector2);
		}
	}

	public enum GearState
	{
		Uninitialized = 0,
		LockedRetracted = 1,
		LockedExtended = 2,
		Retracting = 3,
		Extending = 4
	}

	private static int tireSmokeID = -1;

	[Header("Physics")]
	[SerializeField]
	private AeroPart attachedPart;

	[SerializeField]
	private float extendedDrag;

	private float retractedDrag;

	[SerializeField]
	private GameObject bumpStop;

	[SerializeField]
	private GameObject unsprung;

	[SerializeField]
	private float suspensionTravel;

	[SerializeField]
	private float springRate;

	[SerializeField]
	private float dampingRate;

	[SerializeField]
	private Transform castPoint;

	[SerializeField]
	private float wheelRadius;

	[SerializeField]
	private Transform[] wheels;

	[SerializeField]
	private Transform axle;

	[SerializeField]
	private IKJoint[] joints;

	[SerializeField]
	private float frictionCoef = 1f;

	[SerializeField]
	private float response = 1f;

	[SerializeField]
	private float contactArea;

	[SerializeField]
	private float rollingResistance;

	[SerializeField]
	private Aircraft aircraft;

	[Header("Audio")]
	[SerializeField]
	private AudioSource tireNoiseSound;

	[SerializeField]
	private AudioSource tireSkidSound;

	[SerializeField]
	private float skidVolumeFloor = -0.4f;

	[SerializeField]
	private float skidPitchMult = 1f;

	[Header("Strength")]
	[SerializeField]
	private float maxCompression;

	[SerializeField]
	private float mass;

	[SerializeField]
	private Collider gearCollider;

	[SerializeField]
	private AudioClip breakSound;

	[Header("Folding")]
	[SerializeField]
	private AudioClip foldSound;

	[SerializeField]
	private float foldVolume;

	private AudioSource foldSoundSource;

	[SerializeField]
	private AudioClip latchSound;

	[SerializeField]
	private float latchVolume;

	[SerializeField]
	private Transform gearHinge;

	[SerializeField]
	private Vector3 hingeFoldMotion;

	[SerializeField]
	private float foldDegrees;

	[SerializeField]
	private float foldSpeed;

	[SerializeField]
	private Transform strutRotationTransform;

	[SerializeField]
	private float strutRotation;

	[SerializeField]
	private GearPart[] movingParts;

	[SerializeField]
	private List<GearDoor> gearDoors = new List<GearDoor>();

	private Vector3 hingeBasePos;

	private Vector3 hingeBaseAngles;

	[Header("Steering")]
	[SerializeField]
	private bool steering;

	[Header("Steering")]
	[SerializeField]
	private bool braked;

	[SerializeField]
	private float steeringLock;

	[SerializeField]
	private float steeringSpeed;

	[SerializeField]
	private float aligningStrength;

	[SerializeField]
	private float differentialBrakeFactor;

	private float steeringAngle;

	private ControlInputs controlInputs;

	[Header("Effects")]
	[SerializeField]
	private ParticleSystem dust;

	private RaycastHit hit;

	private float groundDepth;

	private float compressionDistance;

	private float compressionDistancePrev;

	private float compressionForce;

	private float dampingForce;

	private float groundSpeed;

	private float foldAmount;

	private float wheelSpeed;

	private float wheelSpeedPrev;

	private bool onTarmac = true;

	private bool prevOnTarmac;

	private float brakeStrength;

	private Collider contactCollider;

	private Vector3 contactPatch;

	private float contactPatchSize;

	private GameObject skidEffect;

	public event Action<LandingGear> onGearBreak;

	private void Awake()
	{
		IKJoint[] array = joints;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Initialize();
		}
		if (strutRotationTransform == null)
		{
			strutRotationTransform = unsprung.transform;
		}
		hingeBaseAngles = gearHinge.localEulerAngles;
		contactPatchSize = wheelRadius * 0.5f;
		aircraft.onSetGear += LandingGear_OnSetGear;
		retractedDrag = attachedPart.dragArea;
		controlInputs = aircraft.GetInputs();
		hingeBasePos = gearHinge.localPosition;
		attachedPart.onParentDetached += LandingGear_OnPartDetached;
		if (castPoint == null)
		{
			castPoint = bumpStop.transform;
		}
		if (dust == null)
		{
			dust = wheels[0].GetComponent<ParticleSystem>();
		}
	}

	private void OnDestroy()
	{
		if (aircraft != null)
		{
			aircraft.onSetGear -= LandingGear_OnSetGear;
		}
	}

	private void PlayLatchSound()
	{
		AudioSource audioSource = base.gameObject.AddComponent<AudioSource>();
		audioSource.outputAudioMixerGroup = SoundManager.i.EffectsMixer;
		audioSource.bypassListenerEffects = true;
		audioSource.clip = latchSound;
		audioSource.volume = latchVolume;
		audioSource.dopplerLevel = 0f;
		audioSource.minDistance = 5f;
		audioSource.maxDistance = 20f;
		audioSource.spatialBlend = 1f;
		audioSource.Play();
		UnityEngine.Object.Destroy(audioSource, 2f);
		if (foldSoundSource != null)
		{
			foldSoundSource.Stop();
			UnityEngine.Object.Destroy(foldSoundSource, 2f);
		}
	}

	private void LandingGear_OnPartDetached(UnitPart part)
	{
		BreakWheel();
	}

	private void LandingGear_OnSetGear(Aircraft.OnSetGear e)
	{
		if (e.gearState == GearState.LockedExtended)
		{
			foldAmount = 0f;
			gearHinge.localEulerAngles = hingeBaseAngles;
			gearHinge.localPosition = Vector3.Lerp(hingeBasePos, hingeBasePos + hingeFoldMotion, foldAmount);
			strutRotationTransform.localEulerAngles = Vector3.zero;
			attachedPart.dragArea = extendedDrag;
			foreach (GearDoor gearDoor in gearDoors)
			{
				gearDoor.Animate(1f);
			}
			UpdateMovingParts();
			base.enabled = true;
		}
		if (e.gearState == GearState.LockedRetracted)
		{
			foldAmount = 1f;
			gearHinge.localEulerAngles = hingeBaseAngles + new Vector3(foldDegrees, 0f, 0f);
			gearHinge.localPosition = Vector3.Lerp(hingeBasePos, hingeBasePos + hingeFoldMotion, foldAmount);
			attachedPart.dragArea = retractedDrag;
			strutRotationTransform.localEulerAngles = new Vector3(0f, strutRotation, 0f);
			foreach (GearDoor gearDoor2 in gearDoors)
			{
				gearDoor2.Animate(0f);
			}
			UpdateMovingParts();
			base.enabled = false;
		}
		if (e.gearState == GearState.Extending || e.gearState == GearState.Retracting)
		{
			MoveGear(e.gearState).Forget();
			foldSoundSource = base.gameObject.AddComponent<AudioSource>();
			foldSoundSource.outputAudioMixerGroup = SoundManager.i.EffectsMixer;
			foldSoundSource.bypassListenerEffects = true;
			foldSoundSource.clip = foldSound;
			foldSoundSource.volume = foldVolume;
			foldSoundSource.dopplerLevel = 0f;
			foldSoundSource.minDistance = 5f;
			foldSoundSource.maxDistance = 20f;
			foldSoundSource.spatialBlend = 1f;
			foldSoundSource.Play();
			base.enabled = true;
		}
	}

	private async UniTask MoveGear(GearState gearState)
	{
		float gearAngle = Vector3.Angle(gearHinge.forward, gearHinge.parent.forward);
		bool doorsOpen = gearState != GearState.Extending;
		float moveTime = 0f;
		CancellationToken cancel = base.destroyCancellationToken;
		while (true)
		{
			await UniTask.Yield();
			if (cancel.IsCancellationRequested)
			{
				break;
			}
			if (gearHinge == null)
			{
				continue;
			}
			if (gearState == GearState.Extending)
			{
				if (!doorsOpen)
				{
					moveTime += Time.deltaTime;
					for (int i = 0; i < gearDoors.Count; i++)
					{
						gearDoors[i].Animate(moveTime);
					}
					if (moveTime > 1f)
					{
						doorsOpen = true;
					}
				}
				else
				{
					gearAngle -= foldSpeed * Time.deltaTime;
					float y = Mathf.Abs(gearAngle / foldDegrees) * strutRotation;
					gearHinge.localEulerAngles = hingeBaseAngles + new Vector3(gearAngle * Mathf.Sign(foldDegrees), 0f, 0f);
					strutRotationTransform.localEulerAngles = new Vector3(0f, y, 0f);
					attachedPart.dragArea = Mathf.Lerp(extendedDrag, retractedDrag, gearAngle / Mathf.Abs(foldDegrees));
					if (gearAngle < 0f)
					{
						gearHinge.localEulerAngles = hingeBaseAngles;
						strutRotationTransform.localEulerAngles = Vector3.zero;
						attachedPart.dragArea = extendedDrag;
						aircraft.SetGear(GearState.LockedExtended);
						PlayLatchSound();
						break;
					}
				}
			}
			if (gearState == GearState.Retracting)
			{
				gearAngle += foldSpeed * Time.deltaTime;
				float y2 = Mathf.Abs(gearAngle / foldDegrees) * strutRotation;
				gearHinge.localEulerAngles = hingeBaseAngles + new Vector3(gearAngle * Mathf.Sign(foldDegrees), 0f, 0f);
				strutRotationTransform.localEulerAngles = new Vector3(0f, y2, 0f);
				attachedPart.dragArea = Mathf.Lerp(extendedDrag, retractedDrag, gearAngle / Mathf.Abs(foldDegrees));
				if (gearAngle > Mathf.Abs(foldDegrees))
				{
					gearHinge.localEulerAngles = hingeBaseAngles + new Vector3(foldDegrees, 0f, 0f);
					strutRotationTransform.localEulerAngles = new Vector3(0f, strutRotation, 0f);
					moveTime += Time.deltaTime;
					bool flag = gearDoors.Count == 0;
					for (int j = 0; j < gearDoors.Count; j++)
					{
						gearDoors[j].Animate(1f - moveTime);
						if (moveTime > 1f)
						{
							flag = true;
							gearDoors[j].transform.localEulerAngles = Vector3.zero;
						}
					}
					if (flag)
					{
						aircraft.SetGear(GearState.LockedRetracted);
						attachedPart.dragArea = retractedDrag;
						PlayLatchSound();
						break;
					}
				}
			}
			foldAmount = Mathf.Abs(gearAngle / foldDegrees);
			gearHinge.transform.localPosition = Vector3.Lerp(hingeBasePos, hingeBasePos + hingeFoldMotion, foldAmount);
			UpdateMovingParts();
		}
	}

	public bool WeightOnWheel(float threshold)
	{
		return compressionDistance > suspensionTravel * threshold;
	}

	private void UpdateMovingParts()
	{
		GearPart[] array = movingParts;
		foreach (GearPart gearPart in array)
		{
			if (gearPart.transform == null)
			{
				break;
			}
			if (gearPart.target != null)
			{
				gearPart.transform.LookAt(gearPart.target);
			}
			if (gearPart.foldAngles != Vector3.zero)
			{
				gearPart.transform.localEulerAngles = Vector3.Lerp(Vector3.zero, gearPart.foldAngles, foldAmount);
			}
		}
	}

	private void BreakWheel()
	{
		this.onGearBreak?.Invoke(this);
		if (dust.isPlaying)
		{
			dust.Stop();
		}
		if (aircraft != null)
		{
			aircraft.onSetGear -= LandingGear_OnSetGear;
		}
		if (foldSoundSource != null && foldSoundSource.isPlaying)
		{
			foldSoundSource.Stop();
		}
		if (tireNoiseSound.isPlaying)
		{
			tireNoiseSound.Stop();
		}
		if (tireSkidSound.isPlaying)
		{
			tireSkidSound.Stop();
		}
		gearCollider.enabled = true;
		gearHinge.transform.SetParent(null, worldPositionStays: true);
		if (attachedPart != null)
		{
			attachedPart.rb.mass -= mass;
			attachedPart.onParentDetached -= LandingGear_OnPartDetached;
		}
		Rigidbody rigidbody = gearHinge.gameObject.AddComponent<Rigidbody>();
		rigidbody.sleepThreshold = 0f;
		rigidbody.mass = mass;
		rigidbody.drag = 0.05f;
		rigidbody.angularDrag = 0.05f;
		rigidbody.useGravity = true;
		rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
		rigidbody.velocity = attachedPart.rb.velocity;
		rigidbody.angularVelocity = attachedPart.rb.angularVelocity;
		AudioSource audioSource = base.gameObject.AddComponent<AudioSource>();
		audioSource.outputAudioMixerGroup = SoundManager.i.EffectsMixer;
		audioSource.pitch = UnityEngine.Random.Range(0.8f, 1.2f);
		audioSource.clip = breakSound;
		audioSource.spatialBlend = 1f;
		audioSource.dopplerLevel = 1f;
		audioSource.spread = 5f;
		audioSource.maxDistance = 200f;
		audioSource.minDistance = 5f;
		audioSource.Play();
		UnityEngine.Object.Destroy(audioSource, 3f);
		UnityEngine.Object.Destroy(gearHinge.gameObject, 20f);
		UnityEngine.Object.Destroy(this);
	}

	private void FixedUpdate()
	{
		if (aircraft.remoteSim && aircraft.NetId != 0)
		{
			base.enabled = false;
			return;
		}
		Rigidbody rigidbody = null;
		if (!(attachedPart.rb != null))
		{
			return;
		}
		float num = (braked ? controlInputs.brake : 0f);
		if (braked && aircraft.speed < 1f && controlInputs.throttle < 0.1f)
		{
			num = 1f;
		}
		if (differentialBrakeFactor != 0f && groundSpeed < 30f && groundSpeed > 3f)
		{
			num += Mathf.Clamp01(controlInputs.yaw * 0.3f * differentialBrakeFactor);
		}
		brakeStrength = Mathf.Lerp(brakeStrength, num, Time.deltaTime * 5f);
		for (int i = 0; i < wheels.Length; i++)
		{
			wheels[i].transform.Rotate(360f * Time.deltaTime * (wheelSpeed / (6.28318f * wheelRadius)), 0f, 0f, Space.Self);
		}
		if (foldAmount < 0.2f && Physics.Linecast(castPoint.position, castPoint.position - castPoint.up * suspensionTravel, out hit, ~((int)PhysicsLayers.ExclusionZonesMask | (int)PhysicsLayers.IgnoreCollisionsMask)))
		{
			contactCollider = hit.collider;
			Vector3 pointVelocity = attachedPart.rb.GetPointVelocity(hit.point);
			if (contactCollider.attachedRigidbody != null)
			{
				rigidbody = contactCollider.attachedRigidbody;
				pointVelocity -= rigidbody.GetPointVelocity(hit.point);
				AnimatedPhysicsSurface component = contactCollider.gameObject.GetComponent<AnimatedPhysicsSurface>();
				if (component != null)
				{
					pointVelocity -= component.GetVelocity();
				}
			}
			onTarmac = !(hit.collider.sharedMaterial == GameAssets.i.terrainMaterial);
			Vector3 vector = Vector3.zero;
			if (!onTarmac)
			{
				float num2 = (compressionForce + dampingForce) / (contactArea * 10000f);
				GlobalPosition globalPosition = base.transform.GlobalPosition();
				float num3 = 1f + (4f + Mathf.Sin(globalPosition.x * 0.8f) + Mathf.Cos(globalPosition.z * 0.37f) + Mathf.Sin(globalPosition.z * 0.8f) + Mathf.Cos(globalPosition.z * 0.37f));
				groundDepth = Mathf.Lerp(groundDepth, Mathf.Max(num2 * 0.003f / num3, 0f), 3f * Time.deltaTime / Mathf.Clamp(wheelSpeed * 0.1f, 1f, 3f));
				vector = 2.5f * groundDepth * groundDepth * Mathf.Abs(wheelSpeed) * num2 * num2 * -pointVelocity.normalized;
				if (wheelSpeed > 2f && !dust.isPlaying)
				{
					dust.Play();
				}
			}
			else
			{
				groundDepth = 0f;
				if (dust.isPlaying)
				{
					dust.Stop();
				}
			}
			compressionDistance = suspensionTravel - (hit.distance + groundDepth);
			compressionDistance = Mathf.Max(compressionDistance, 0f);
			float num4 = Vector3.Dot(hit.normal, pointVelocity);
			dampingForce = (0f - num4) * dampingRate;
			compressionForce = springRate * compressionDistance;
			groundSpeed = Vector3.Dot(pointVelocity, base.transform.forward);
			if (steering && groundSpeed > 1f)
			{
				float angleOnAxis = TargetCalc.GetAngleOnAxis(axle.forward, pointVelocity, axle.up);
				steeringAngle += angleOnAxis * Mathf.Min(groundSpeed * 0.1f, 10f) * aligningStrength * Time.deltaTime;
			}
			Vector3 normalized = Vector3.ProjectOnPlane(pointVelocity, hit.normal).normalized;
			float num5 = groundSpeed - wheelSpeed;
			if (num5 > 20f)
			{
				Vector3 vector2 = hit.point - aircraft.transform.position;
				vector2.y = 0f;
				if (tireSmokeID == -1 && SceneSingleton<ParticleEffectManager>.i != null)
				{
					tireSmokeID = SceneSingleton<ParticleEffectManager>.i.GetSystemID("TireSmoke");
				}
				SceneSingleton<ParticleEffectManager>.i.EmitParticles(tireSmokeID, (int)(num5 * 0.03f), (hit.point + hit.normal * wheelRadius).ToGlobalPosition(), Vector3.Project(attachedPart.rb.velocity, normalized) + vector2.normalized * aircraft.speed * 0.1f, 0f, Mathf.Max(5f - aircraft.speed * 0.03f, 1f), 0.3f, wheelRadius * 10f + num5 * 0.1f, 0.3f, aircraft.speed * 0.03f, Mathf.Clamp01(num5 * 0.01f), 0.3f);
			}
			wheelSpeed += Mathf.Clamp(num5, -100f * Time.deltaTime, 100f * Time.deltaTime);
			if (compressionDistance > maxCompression || gearHinge.transform.localEulerAngles.x > 10f || vector.sqrMagnitude > springRate * springRate)
			{
				BreakWheel();
				base.enabled = false;
				return;
			}
			Vector3 vector3 = hit.normal * Mathf.Max(compressionForce + dampingForce, 0f);
			float num6 = (compressionForce + dampingForce) * frictionCoef;
			Vector3 normalized2 = Vector3.Cross(normalized, hit.normal).normalized;
			Vector3 vector4 = new Vector3(0f, 0f, 0f);
			Vector3 vector5 = new Vector3(0f, 0f, 0f);
			Vector3 vector6 = -normalized * (rollingResistance * (compressionForce + dampingForce) * 1f);
			float num7 = 0f;
			if (Mathf.Abs(groundSpeed) < 1f)
			{
				if (FastMath.OutOfRange(hit.point, contactCollider.transform.TransformPoint(contactPatch), contactPatchSize))
				{
					contactPatch = contactCollider.transform.InverseTransformPoint(hit.point);
				}
				if (dust.isPlaying)
				{
					dust.Stop();
				}
				Vector3 vector7 = (contactCollider.transform.TransformPoint(contactPatch) - hit.point) / contactPatchSize - pointVelocity * 1f;
				Vector3 vector8 = Vector3.Project(vector7, axle.transform.right);
				Vector3 vector9 = Vector3.Project(vector7, (0f - Mathf.Sign(wheelSpeed)) * axle.transform.forward) * (brakeStrength + 0.05f);
				vector4 = Vector3.ClampMagnitude(vector8 + vector9, 1f) * num6;
			}
			else
			{
				num7 = Mathf.Clamp(TargetCalc.GetAngleOnAxis(axle.transform.forward * Mathf.Sign(wheelSpeed), normalized, hit.normal), -10f, 10f);
				Vector3 vector10 = Mathf.Clamp(num7 * response * (0.2f + Mathf.Abs(wheelSpeed) * 0.01f), -1f, 1f) * num6 * normalized2;
				Vector3 vector11 = Mathf.Clamp01(Mathf.Abs(Vector3.Dot(axle.transform.forward, normalized2))) * num6 * -normalized;
				Vector3 vector12 = -normalized * brakeStrength * (compressionForce + dampingForce) * frictionCoef;
				vector5 = Vector3.ClampMagnitude(vector10 + vector11 + vector12 + vector6, num6);
			}
			attachedPart.rb.AddForceAtPosition(vector3 + vector5 + vector4 + vector, hit.point);
			if (rigidbody != null)
			{
				rigidbody.AddForceAtPosition(-(vector3 + vector5 + vector4 + vector), hit.point);
			}
			unsprung.transform.position = bumpStop.transform.position - bumpStop.transform.up * (suspensionTravel - compressionDistance - wheelRadius);
			if (!tireNoiseSound.isPlaying || prevOnTarmac != onTarmac)
			{
				tireNoiseSound.Stop();
				tireSkidSound.Stop();
				if (onTarmac)
				{
					tireNoiseSound.clip = GameAssets.i.wheelRollingRoad;
					tireSkidSound.clip = GameAssets.i.wheelSlidingRoad;
				}
				else
				{
					tireNoiseSound.clip = GameAssets.i.wheelRollingDirt;
					tireSkidSound.clip = GameAssets.i.wheelSlidingDirt;
				}
				tireNoiseSound.time = tireNoiseSound.clip.length * UnityEngine.Random.value;
				tireNoiseSound.Play();
				tireSkidSound.time = tireSkidSound.clip.length * UnityEngine.Random.value;
				tireSkidSound.Play();
			}
			prevOnTarmac = onTarmac;
			tireNoiseSound.volume = Mathf.Abs(compressionForce + dampingForce) / springRate * groundSpeed * (0.2f + groundDepth * 10f);
			tireNoiseSound.pitch = 0.5f + groundSpeed * 0.015f;
			tireSkidSound.volume = Mathf.Max(skidVolumeFloor + Mathf.Abs(num7) * 0.1f, num5 * 0.1f + groundDepth * groundDepth * groundSpeed);
			tireSkidSound.pitch = Mathf.Min((0.75f + (Mathf.Abs(num7) * groundSpeed * 0.0004f + num5 * 0.003f)) * skidPitchMult, 3f);
		}
		else
		{
			if (dust.isPlaying)
			{
				dust.Stop();
			}
			if (tireNoiseSound.isPlaying)
			{
				tireNoiseSound.Stop();
			}
			if (tireSkidSound.isPlaying)
			{
				tireSkidSound.Stop();
			}
			wheelSpeed -= (wheelSpeed * 0.1f + 1f) * Time.deltaTime;
			wheelSpeed = Mathf.Max(wheelSpeed, 0f);
			compressionForce = 0f;
			dampingForce = 0f;
			compressionDistance = 0f;
			unsprung.transform.position = bumpStop.transform.position - unsprung.transform.up * (suspensionTravel - wheelRadius);
		}
		wheelSpeedPrev = wheelSpeed;
		if (compressionDistance != compressionDistancePrev)
		{
			IKJoint[] array = joints;
			for (int j = 0; j < array.Length; j++)
			{
				array[j].Update();
			}
		}
		compressionDistancePrev = compressionDistance;
		if (steering)
		{
			float value = controlInputs.yaw * steeringLock - steeringAngle;
			int num8 = 1;
			steeringAngle += Mathf.Clamp(value, (0f - steeringSpeed) * (float)num8 * Time.deltaTime, steeringSpeed * (float)num8 * Time.deltaTime);
			steeringAngle = Mathf.Clamp(steeringAngle, 0f - Mathf.Abs(steeringLock), Mathf.Abs(steeringLock));
			unsprung.transform.localEulerAngles = new Vector3(0f, steeringAngle, 0f);
		}
	}
}
