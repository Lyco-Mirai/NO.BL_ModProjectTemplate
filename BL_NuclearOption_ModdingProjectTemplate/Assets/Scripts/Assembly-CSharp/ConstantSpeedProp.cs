using System;
using System.Collections.Generic;
using NuclearOption.DebugScripts;
using UnityEngine;

public class ConstantSpeedProp : MonoBehaviour, IEngine, IThrustSource, IPitchTelemetry, IPowerOutput
{
	private enum TurnDirection
	{
		clockwise = 1,
		antiClockwise = -1
	}

	private enum PitchDirection
	{
		pusher = 1,
		puller = -1
	}

	[Serializable]
	public class PropBlade
	{
		[SerializeField]
		private Transform transform;

		[SerializeField]
		private Transform liftTransform;

		[SerializeField]
		private float mass;

		public Renderer renderer;

		public MeshFilter bladeMeshFilter;

		private Mesh[] bladeDamageMeshes;

		private float hubRadius;

		private float currentDamageIndex;

		private float originalLength;

		private float currentLength;

		private float originalMass;

		private float currentMass;

		private float lastWaterHit;

		[HideInInspector]
		public bool struck;

		private GameObject forceDebug;

		private GameObject incomingAirDebug;

		private GameObject deflectedAirDebug;

		private GameObject supersonicDebug;

		public void Initialize(float hubRadius, Mesh[] bladeDamageMeshes)
		{
			this.hubRadius = hubRadius;
			this.bladeDamageMeshes = bladeDamageMeshes;
			currentLength = liftTransform.localPosition.z;
			originalLength = currentLength;
			currentDamageIndex = bladeDamageMeshes.Length - 1;
			struck = false;
			originalMass = mass;
		}

		public float GetLengthRatio()
		{
			return currentLength / originalLength;
		}

		public bool CheckCollision(out float strikeLength, GameObject fragmentPrefab, Vector3 tipVelocity)
		{
			strikeLength = 0f;
			if (Physics.Linecast(transform.position, liftTransform.position, out var hitInfo, ~(int)PhysicsLayers.ExclusionZonesMask))
			{
				ApplyDamage(hitInfo.distance, out strikeLength, hitInfo.point, hitInfo.normal, fragmentPrefab, tipVelocity);
			}
			return strikeLength > 0f;
		}

		public bool CheckWaterCollision(out float strikeLength, Plane waterPlane, GameObject fragmentPrefab, Vector3 tipVelocity)
		{
			strikeLength = 0f;
			if (waterPlane.Raycast(new Ray(transform.position, transform.forward), out var enter) && enter > 0f && enter < currentLength)
			{
				Vector3 tipPosition = GetTipPosition();
				Vector3 forward = (Vector3.Reflect(tipVelocity.normalized, Vector3.up) + Vector3.Reflect(Vector3.Normalize(tipPosition - transform.position), Vector3.up)) / 2f;
				if (Time.timeSinceLevelLoad - lastWaterHit > 1f)
				{
					lastWaterHit = Time.timeSinceLevelLoad;
					if (SceneSingleton<ParticleEffectManager>.i != null)
					{
						SceneSingleton<ParticleEffectManager>.i.GetPrefabEffect(GameAssets.i.rotorStrike_water).Play(new Vector3(tipPosition.x, 0f, tipPosition.z), Quaternion.LookRotation(forward));
					}
				}
				ApplyDamage(enter, out strikeLength, transform.position + transform.forward * enter, Vector3.up, fragmentPrefab, tipVelocity);
			}
			return strikeLength > 0f;
		}

		private void ApplyDamage(float hitDistance, out float strikeLength, Vector3 hitPosition, Vector3 hitNormal, GameObject fragmentPrefab, Vector3 tipVelocity)
		{
			liftTransform.localPosition = new Vector3(0f, 0f, hitDistance);
			strikeLength = currentLength - hitDistance;
			currentLength = hitDistance;
			currentMass = originalMass * currentLength / originalLength;
			int num = Mathf.FloorToInt((float)bladeDamageMeshes.Length * Mathf.Max((currentLength - hubRadius) / originalLength, 0f));
			if ((float)num != currentDamageIndex)
			{
				currentDamageIndex = num;
				bladeMeshFilter.mesh = bladeDamageMeshes[num];
				if (fragmentPrefab != null)
				{
					GameObject gameObject = NetworkSceneSingleton<Spawner>.i.SpawnLocal(fragmentPrefab, null);
					gameObject.transform.position = hitPosition;
					Rigidbody component = gameObject.GetComponent<Rigidbody>();
					float magnitude = tipVelocity.magnitude;
					component.velocity = magnitude * 0.25f * hitNormal + magnitude * 0.25f * UnityEngine.Random.insideUnitSphere;
					component.angularVelocity = UnityEngine.Random.insideUnitSphere * 4f;
					UnityEngine.Object.Destroy(gameObject, 10f);
				}
			}
		}

		public void SetPitch(float pitchChange)
		{
			transform.Rotate(0f, 0f, pitchChange, Space.Self);
		}

		public Vector3 GetTipVelocity(Transform hub, Vector3 hubVelocity, float direction, float rotationRate)
		{
			Vector3 vector = Vector3.Cross(-hub.forward, transform.forward) * direction;
			return hubVelocity + 1f * currentLength * rotationRate * vector;
		}

		public Vector3 GetTipPosition()
		{
			return liftTransform.position;
		}

		public ForceAndTorque GetCentripetalForce(float angularVelocity)
		{
			float num = angularVelocity * currentLength * 0.5f;
			return new ForceAndTorque(num * num / currentLength * currentMass * transform.forward, Vector3.zero);
		}

		public float GetCurrentLength()
		{
			return currentLength;
		}

		public float GetCurrentMass()
		{
			return currentMass;
		}

		public void ApplyForceAndTorque(Rigidbody rb, Transform hub, Vector3 hubAirVelocity, float direction, int bladeNum, float efficiency, float drag, ref ForceAndTorque forceAndTorque, ref float meanAoA, ref float hubTorque, ref float noise, float rotationRate, float airDensity)
		{
			Vector3 vector = Vector3.Cross(-hub.forward, transform.forward) * direction;
			float num = currentLength * rotationRate;
			Vector3 vector2 = hubAirVelocity + num * vector * 0.75f;
			Vector3 vector3 = liftTransform.InverseTransformDirection(vector2);
			float num2 = (0f - Mathf.Atan2(vector3.y, vector3.z)) * 57.29578f;
			meanAoA += num2;
			float magnitude = vector2.magnitude;
			Vector3 vector4 = airDensity * magnitude * magnitude * drag * vector2.normalized;
			float num3 = Mathf.Max((magnitude - 300f) / 30f, 1f);
			if (num3 > 1f)
			{
				efficiency /= num3;
				vector4 *= num3;
			}
			float num4 = MathF.PI * currentLength * currentLength / (float)bladeNum;
			float num5 = airDensity * num4 * vector2.magnitude * efficiency;
			Vector3 vector5 = Vector3.Reflect(-vector2, -liftTransform.up);
			Vector3 vector6 = -vector2 - vector5;
			Vector3 vector7 = num5 * vector6 - vector4;
			Vector3 vector8 = vector7;
			float sqrMagnitude = vector2.sqrMagnitude;
			noise += sqrMagnitude * num2;
			float num6 = Vector3.Dot(vector8, vector);
			hubTorque += num6 * currentLength * 0.75f;
			forceAndTorque.Add(new ForceAndTorque(vector8, (liftTransform.position - hub.position) * 0.75f));
			if (DebugVis.Enabled)
			{
				DebugVis.Create(ref forceDebug, GameAssets.i.debugArrow, liftTransform);
				if (DebugVis.Create(ref incomingAirDebug, GameAssets.i.debugArrow, liftTransform))
				{
					incomingAirDebug.GetComponent<MeshRenderer>().material.SetColor("_EmissionColor", Color.white);
				}
				if (DebugVis.Create(ref deflectedAirDebug, GameAssets.i.debugArrow, liftTransform))
				{
					deflectedAirDebug.GetComponent<MeshRenderer>().material.SetColor("_EmissionColor", Color.yellow);
				}
				if (DebugVis.Create(ref supersonicDebug, GameAssets.i.debugPoint, liftTransform))
				{
					supersonicDebug.transform.localScale = Vector3.one * 0.5f;
				}
				incomingAirDebug.transform.rotation = Quaternion.LookRotation(-vector2);
				incomingAirDebug.transform.localScale = new Vector3(0.5f, 0.5f, Mathf.Sqrt(vector2.magnitude) * 0.1f);
				deflectedAirDebug.transform.rotation = Quaternion.LookRotation(vector5);
				deflectedAirDebug.transform.localScale = new Vector3(0.5f, 0.5f, Mathf.Sqrt(vector5.magnitude) * 0.1f);
				forceDebug.transform.rotation = Quaternion.LookRotation(vector7);
				forceDebug.transform.localScale = new Vector3(1f, 1f, Mathf.Sqrt(vector7.magnitude) * 0.02f);
				supersonicDebug.SetActive(magnitude > 340f);
			}
		}
	}

	[SerializeField]
	private UnitPart unitPart;

	private Aircraft aircraft;

	[SerializeField]
	private Transmission transmission;

	[SerializeField]
	private ConstantSpeedProp oppositeCorner;

	[SerializeField]
	private TurnDirection turnDirection;

	[SerializeField]
	private PitchDirection pitchDirection = PitchDirection.pusher;

	[SerializeField]
	private List<PropBlade> blades = new List<PropBlade>();

	[SerializeField]
	private GameObject hubVisible;

	[SerializeField]
	private MeshFilter propDisc;

	[SerializeField]
	private Mesh[] bladeDamageMeshes;

	[SerializeField]
	private GameObject fragmentPrefab;

	[SerializeField]
	private List<Mesh> propDiscMeshes;

	[SerializeField]
	private Collider propDiscCollider;

	[SerializeField]
	private PropStrikeDetector propStrikeDetector;

	[SerializeField]
	private float propDiskThicknessMin;

	[SerializeField]
	private float propDiskThicknessMax;

	[HideInInspector]
	public float RPM;

	private float angularVelocity;

	private float rpmRatio;

	private float currentThrust;

	private int propBlurIndex;

	[SerializeField]
	private float momentOfInertia;

	[SerializeField]
	private float bladeLength;

	[SerializeField]
	private float hubRadius;

	[SerializeField]
	private float bladeEfficiency;

	[SerializeField]
	private float bladeDrag;

	[SerializeField]
	private float bladeStrength = 20000f;

	[SerializeField]
	private float propStrikeTolerance = 0.5f;

	[SerializeField]
	private float rpmLimit;

	[SerializeField]
	private float nominalPower;

	[SerializeField]
	private float bladeMinPitch;

	[SerializeField]
	private float bladeMaxPitch;

	[SerializeField]
	private float targetAOA;

	[SerializeField]
	private float pitchRate;

	[SerializeField]
	private float reversePitchBraking = 10f;

	[SerializeField]
	private Vector3 pitchPIDFactors;

	[SerializeField]
	private Vector3 differentialControlFactors;

	[SerializeField]
	private bool featherIfPowerLost;

	[SerializeField]
	private AudioSource propAudio;

	[SerializeField]
	private AudioClip interiorSound;

	[SerializeField]
	private AudioClip exteriorSound;

	[SerializeField]
	private float volumeBase;

	[SerializeField]
	private float pitchBase;

	[SerializeField]
	private float rpmModifyVolume;

	[SerializeField]
	private float angleModifyVolume;

	[SerializeField]
	private float rpmModifyPitch;

	[SerializeField]
	private float angleModifyPitch;

	[SerializeField]
	private bool debug;

	[SerializeField]
	private Material propBlurLightsMaterial;

	private PID propPitchPID;

	private Material propBlurBaseMaterial;

	[HideInInspector]
	public float PropPitch;

	private ControlInputs controlInputs;

	[SerializeField]
	private float averageAoA;

	private float powerAvailable;

	private float engineTorque;

	private ForceAndTorque forceAndTorque;

	[SerializeField]
	private float hubFriction;

	private float bladeNoise;

	private float originalMomentOfInertia;

	private float averageBladeLength;

	private float imbalance;

	private float imbalanceApplicationAngle;

	[SerializeField]
	private float propTorqueLimit;

	private bool operable;

	private bool featherMode;

	private bool propStrike;

	private Renderer propDiscRenderer;

	private GameObject totalForcesDebug;

	Transform IEngine.transform => base.transform;

	public event Action OnEngineDisable;

	public event Action OnEngineDamage;

	public event Action OnBladeDamage;

	private void Awake()
	{
		operable = true;
		if (propBlurLightsMaterial != null)
		{
			propDiscRenderer = propDisc.gameObject.GetComponent<Renderer>();
			propBlurBaseMaterial = propDiscRenderer.material;
		}
		if (propStrikeDetector != null)
		{
			propStrikeDetector.OnStrike += ConstantSpeedProp_OnPropStrike;
		}
		if (oppositeCorner != null)
		{
			oppositeCorner.OnBladeDamage += ConstantSpeedProp_OnOppositeCornerDamage;
		}
		unitPart.onPartDetached += Prop_OnAttachmentBreak;
		unitPart.onParentDetached += Prop_OnAttachmentBreak;
		propPitchPID = new PID(pitchPIDFactors);
		originalMomentOfInertia = momentOfInertia;
		aircraft = unitPart.parentUnit as Aircraft;
		aircraft.engineStates.Add(this);
		controlInputs = aircraft.GetInputs();
		foreach (PropBlade blade in blades)
		{
			blade.Initialize(hubRadius, bladeDamageMeshes);
		}
		aircraft.engines.Add(this);
		aircraft.onInitialize += ConstantSpeedProp_OnInitialize;
		if (PlayerSettings.debugVis)
		{
			totalForcesDebug = UnityEngine.Object.Instantiate(GameAssets.i.debugArrow, base.transform);
		}
	}

	private void ConstantSpeedProp_OnInitialize()
	{
		if (aircraft.radarAlt > aircraft.definition.spawnOffset.y + 1f)
		{
			RPM = rpmLimit * 1.1f;
			angularVelocity = RPM * (MathF.PI * 2f) / 60f;
		}
	}

	private void ConstantSpeedProp_OnOppositeCornerDamage()
	{
		differentialControlFactors *= 3f;
		oppositeCorner.OnBladeDamage -= ConstantSpeedProp_OnOppositeCornerDamage;
	}

	public float GetMaxThrust()
	{
		return 0f;
	}

	public float GetThrust()
	{
		return currentThrust;
	}

	public float GetRPM()
	{
		return RPM;
	}

	public float GetRPMRatio()
	{
		return rpmRatio;
	}

	public void SetInteriorSounds(bool useInteriorSound)
	{
		if (propAudio.isPlaying)
		{
			if (useInteriorSound)
			{
				propAudio.Stop();
				propAudio.clip = interiorSound;
				propAudio.time = UnityEngine.Random.Range(0f, propAudio.clip.length);
				propAudio.Play();
			}
			else
			{
				propAudio.Stop();
				propAudio.clip = exteriorSound;
				propAudio.time = UnityEngine.Random.Range(0f, propAudio.clip.length);
				propAudio.Play();
			}
		}
		else if (useInteriorSound)
		{
			propAudio.clip = interiorSound;
		}
		else
		{
			propAudio.clip = exteriorSound;
		}
	}

	private void Prop_OnAttachmentBreak(UnitPart part)
	{
		operable = false;
		unitPart.onPartDetached -= Prop_OnAttachmentBreak;
		unitPart.onParentDetached += Prop_OnAttachmentBreak;
		this.OnBladeDamage?.Invoke();
		this.OnEngineDisable?.Invoke();
		hubFriction = 10000f;
	}

	private bool CheckBladeCollisions(bool checkWater, bool checkSolid)
	{
		float num = 0f;
		float num2 = 0f;
		averageBladeLength = 0f;
		foreach (PropBlade blade in blades)
		{
			float lengthRatio = blade.GetLengthRatio();
			averageBladeLength += lengthRatio;
			if (checkSolid && blade.CheckCollision(out var strikeLength, fragmentPrefab, blade.GetTipVelocity(base.transform, unitPart.rb.velocity, (float)turnDirection, RPM * 0.1047f)))
			{
				num += bladeStrength * Mathf.Max(rpmRatio, 0.5f) * Mathf.Clamp(strikeLength, 0.05f, 2f);
			}
			if (checkWater && blade.CheckWaterCollision(out var strikeLength2, Datum.WaterPlane(), fragmentPrefab, blade.GetTipVelocity(base.transform, unitPart.rb.velocity, (float)turnDirection, RPM * 0.1047f)))
			{
				num += bladeStrength * Mathf.Max(rpmRatio, 0.5f) * Mathf.Clamp(strikeLength2, 0.05f, 2f);
			}
			num2 = Mathf.Max(num2, lengthRatio);
		}
		averageBladeLength /= blades.Count;
		momentOfInertia = originalMomentOfInertia * averageBladeLength;
		if (num == 0f)
		{
			return false;
		}
		propDiscCollider.transform.localScale = new Vector3(num2, num2, 1f);
		float num3 = Mathf.Min(num / momentOfInertia, angularVelocity);
		angularVelocity -= num3;
		return true;
	}

	private void ConstantSpeedProp_OnPropStrike(float distance)
	{
		if (aircraft.remoteSim)
		{
			return;
		}
		if (RPM < 60f)
		{
			angularVelocity = 0f;
			hubFriction = 100000f;
			if (!propDiscCollider.isTrigger)
			{
				return;
			}
			float num = 0f;
			foreach (PropBlade blade in blades)
			{
				num += blade.GetLengthRatio();
			}
			num /= (float)blades.Count;
			propDiscCollider.transform.localScale = new Vector3(num, num, 1f);
			propDiscCollider.isTrigger = false;
		}
		else if (CheckBladeCollisions(checkWater: false, checkSolid: true))
		{
			ApplyBladeDamage();
		}
	}

	private void ApplyBladeDamage()
	{
		this.OnBladeDamage?.Invoke();
		float num = 0f;
		float num2 = 0f;
		foreach (PropBlade blade in blades)
		{
			num += blade.GetLengthRatio();
			num2 += blade.GetCurrentMass();
			blade.renderer.enabled = true;
		}
		num /= (float)blades.Count;
		imbalance = 0.1f;
		if (aircraft.radarAlt < 5f || num < 1f - propStrikeTolerance)
		{
			operable = false;
		}
		propDisc.mesh = null;
		propStrike = true;
	}

	public float GetPitch()
	{
		return PropPitch;
	}

	public bool IsOperable()
	{
		return operable;
	}

	public float GetAoA()
	{
		return averageAoA;
	}

	private void PropAnimate()
	{
		if (TimeScaleManager.Scale == 0f)
		{
			return;
		}
		propDisc.transform.localScale = new Vector3(1f, 1f, Mathf.Lerp(propDiskThicknessMin, propDiskThicknessMax, PropPitch / bladeMaxPitch));
		hubVisible.transform.Rotate(0f, 0f, RPM * -6f * (float)turnDirection * Time.deltaTime, Space.Self);
		rpmRatio = RPM / rpmLimit;
		propAudio.pitch = pitchBase + rpmRatio * rpmModifyPitch + angleModifyPitch * averageAoA * rpmRatio;
		propAudio.volume = volumeBase + rpmRatio * rpmModifyVolume + angleModifyVolume * averageAoA * rpmRatio;
		if (!propStrike)
		{
			propBlurIndex = Mathf.FloorToInt(Mathf.Clamp((float)propDiscMeshes.Count * (rpmRatio * 0.55f * Time.timeScale + 0.25f), 0f, propDiscMeshes.Count - 1));
			propDisc.mesh = propDiscMeshes[propBlurIndex];
			if (propBlurLightsMaterial != null)
			{
				propDiscRenderer.material = (aircraft.gearDeployed ? propBlurLightsMaterial : propBlurBaseMaterial);
			}
			foreach (PropBlade blade in blades)
			{
				blade.renderer.enabled = propBlurIndex == 0;
			}
		}
		if (RPM > 1f && !propAudio.isPlaying)
		{
			propAudio.time = UnityEngine.Random.Range(0f, propAudio.clip.length);
			propAudio.Play();
		}
		if (RPM < 1f && propAudio.isPlaying)
		{
			propAudio.Stop();
		}
	}

	private void AutoPropPitch()
	{
		if (operable)
		{
			float num = controlInputs.throttle * targetAOA;
			float num2 = Mathf.Min((rpmRatio - 1f) * 10f * targetAOA, 0f);
			num += num2;
			num = Mathf.Max(num, -1f);
			float num3 = Vector3.Dot(base.transform.forward, aircraft.rb.velocity);
			num3 = Mathf.Clamp(num3 * 0.01f, -1f, 1f);
			float num4 = Mathf.Min(controlInputs.throttle - 0.3f, 0f) * Mathf.Max(num3, 0f);
			num = Mathf.Lerp(num, reversePitchBraking, num4 * 2f);
			Vector3 lhs = base.transform.position - aircraft.transform.position;
			float num5 = Mathf.Clamp01(Vector3.Dot(aircraft.transform.up, base.transform.forward));
			float num6 = Vector3.Dot(lhs, -aircraft.transform.right) * num5 * differentialControlFactors.z;
			float num7 = Vector3.Dot(lhs, -aircraft.transform.forward) * num5 * differentialControlFactors.x;
			num += controlInputs.roll * num6 + controlInputs.pitch * num7;
			if (!featherMode && RPM < 10f && aircraft.speed < 10f)
			{
				averageAoA = PropPitch;
				num = 0f;
			}
			float currentError = num - averageAoA;
			if (featherMode)
			{
				currentError = 10f - averageAoA;
			}
			float output = propPitchPID.GetOutput(currentError, 100f, Time.fixedDeltaTime, pitchPIDFactors);
			PropPitch += Mathf.Clamp(output, 0f - pitchRate, pitchRate) * Time.fixedDeltaTime;
			PropPitch = Mathf.Clamp(PropPitch, bladeMinPitch, bladeMaxPitch);
		}
	}

	public void SendPower(float power)
	{
		powerAvailable = power;
	}

	public float GetPowerAvailable()
	{
		return powerAvailable;
	}

	private void Update()
	{
		PropAnimate();
	}

	private void FixedUpdate()
	{
		float propPitch = PropPitch;
		AutoPropPitch();
		bool flag = false;
		if (operable)
		{
			float throttle = controlInputs.throttle;
			throttle += (1f - rpmRatio) * 5f;
			transmission.RequestPower(this, Mathf.Clamp(throttle * nominalPower, 0f, nominalPower * 1.5f));
			flag = true;
		}
		else
		{
			powerAvailable = 0f;
			engineTorque = 0f;
		}
		featherMode = !flag && featherIfPowerLost;
		engineTorque = powerAvailable * 9.5488f / Mathf.Max(RPM, 1f);
		if (powerAvailable <= 0f)
		{
			engineTorque -= hubFriction;
		}
		engineTorque = Mathf.Min(engineTorque, Mathf.Clamp(rpmRatio * 5f, 0.3f, 1f) * propTorqueLimit);
		averageAoA = 0f;
		Vector3 hubAirVelocity = ((NetworkSceneSingleton<LevelInfo>.i != null) ? (unitPart.rb.velocity - NetworkSceneSingleton<LevelInfo>.i.GetWind(base.transform.GlobalPosition())) : Vector3.zero);
		this.forceAndTorque.Clear();
		float hubTorque = 0f;
		if (RPM > 60f && base.transform.position.y - Datum.LocalSeaY < bladeLength && CheckBladeCollisions(checkWater: true, checkSolid: false))
		{
			ApplyBladeDamage();
		}
		foreach (PropBlade blade in blades)
		{
			blade.SetPitch((PropPitch - propPitch) * (0f - (float)turnDirection) * (0f - (float)pitchDirection));
			blade.ApplyForceAndTorque(unitPart.rb, base.transform, hubAirVelocity, (float)turnDirection, blades.Count, bladeEfficiency, bladeDrag, ref this.forceAndTorque, ref averageAoA, ref hubTorque, ref bladeNoise, RPM * 0.1047f, aircraft.airDensity);
		}
		if (imbalance > 0f)
		{
			imbalanceApplicationAngle += Mathf.Min(angularVelocity * Time.fixedDeltaTime, MathF.PI * 2f / 3f);
			Vector3 vector = Vector3.right * Mathf.Sin(imbalanceApplicationAngle) + Vector3.up * Mathf.Cos(imbalanceApplicationAngle);
			ForceAndTorque forceAndTorque = default(ForceAndTorque);
			foreach (PropBlade blade2 in blades)
			{
				forceAndTorque.Add(blade2.GetCentripetalForce(angularVelocity));
			}
			this.forceAndTorque.Add(new ForceAndTorque(forceAndTorque.force.magnitude * vector, Vector3.zero));
		}
		averageAoA /= blades.Count;
		bladeNoise = averageAoA * RPM;
		angularVelocity += Time.deltaTime * (engineTorque + hubTorque) / momentOfInertia;
		angularVelocity = Mathf.Max(angularVelocity, 0f);
		RPM = angularVelocity * 60f / (MathF.PI * 2f);
		engineTorque = 0f;
		currentThrust = Vector3.Dot(this.forceAndTorque.force, base.transform.forward);
		if (totalForcesDebug != null)
		{
			totalForcesDebug.transform.rotation = Quaternion.LookRotation(this.forceAndTorque.force);
			totalForcesDebug.transform.localScale = new Vector3(2f, 2f, Mathf.Sqrt(this.forceAndTorque.force.magnitude) * 0.01f);
		}
		if (aircraft.LocalSim)
		{
			unitPart.rb.AddForce(this.forceAndTorque.force);
			unitPart.rb.AddTorque(this.forceAndTorque.torque + Mathf.Max(engineTorque, 0f) * base.transform.forward * (float)turnDirection);
		}
	}
}
