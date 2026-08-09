using System;
using System.Collections.Generic;
using NuclearOption.Jobs;
using Unity.Profiling;
using UnityEngine;

public class AeroPart : UnitPart
{
	private static readonly ProfilerMarker aeroPhysicsMarker = new ProfilerMarker("AeroPhysics");

	[Header("Structure")]
	[SerializeField]
	protected PartJoint[] joints;

	private List<PartJoint> movingJoints = new List<PartJoint>();

	[Header("Aerodynamics")]
	[SerializeField]
	private float wingArea;

	public float dragArea;

	[Tooltip("Drag to add to parent part when breaking off")]
	[SerializeField]
	private float streamlining;

	private float wingEffectiveness = 1f;

	[SerializeField]
	private Transform liftNormal;

	[SerializeField]
	private Transform connectedAnchor;

	[SerializeField]
	private Vector3 centerOfLift;

	[SerializeField]
	private float buoyancy = 2f;

	[SerializeField]
	private float airflowChanneling;

	[SerializeField]
	private int airfoil = -1;

	private int airfoilID;

	private float submergedAmount;

	private bool simplePhysics = true;

	private GameObject debugArrow;

	private PtrAllocation<AeroPartFields> JobFields;

	private JobPart<AeroPart, AeroPartFields> JobPart;

	private static int contactDustID = -1;

	private static int contactSmokeID = -1;

	public PartJoint[] Joints
	{
		get
		{
			return joints;
		}
		set
		{
			joints = value;
		}
	}

	public float WingArea => wingArea;

	public Transform LiftNormal => liftNormal;

	public Vector3 CenterOfLift => centerOfLift;

	protected override void Awake()
	{
		base.Awake();
		base.enabled = false;
		if (liftNormal == null)
		{
			liftNormal = base.xform;
		}
		if (attachInfo != null)
		{
			attachInfo.attachmentStrength = 0f;
		}
		for (int i = 0; i < joints.Length; i++)
		{
			if (i == 0)
			{
				attachInfo.attachmentStrength += joints[i].breakForce + joints[i].breakTorque;
			}
			if (joints[i].tensor != null)
			{
				joints[i].tensor.onJointBroken += Part_OnTensorBreak;
			}
		}
		if (parentUnit is Aircraft aircraft)
		{
			aircraft.RegisterAeroPart(this);
		}
		parentUnit.onInitialize += AeroPart_OnInitialize;
	}

	private void AeroPart_OnInitialize()
	{
		if (!parentUnit.remoteSim && parentUnit is Aircraft)
		{
			airfoilID = (parentUnit.definition as AircraftDefinition).aircraftParameters.GetAirfoilID(airfoil);
			JobPart = new JobPart<AeroPart, AeroPartFields>(this, GetOrCreateJobField());
			JobManager.Add(JobPart);
		}
	}

	public void UnregisterFromJob()
	{
		JobManager.Remove(ref JobPart);
		DisposeJobFields(ref JobFields);
	}

	public float GetWingArea()
	{
		return wingArea;
	}

	public void SetWingArea(float area)
	{
		wingArea = area;
	}

	public float GetAltitude()
	{
		return liftNormal.position.GlobalY();
	}

	public Transform GetLiftTransform()
	{
		return liftNormal;
	}

	public override void Repair()
	{
		base.Repair();
		if (attachInfo != null)
		{
			detachedFromUnit = false;
			base.xform.position = attachInfo.parentPart.xform.TransformPoint(attachInfo.localPosition);
			base.xform.rotation = attachInfo.parentPart.xform.rotation * attachInfo.localRotation;
		}
	}

	private void OnTriggerStay(Collider other)
	{
		if (other.sharedMaterial == GameAssets.i.WaterMaterial && base.gameObject.TryGetComponent<Collider>(out var component))
		{
			Transform transform = component.transform;
			Transform transform2 = other.transform;
			Physics.ComputePenetration(component, transform.position, transform.rotation, other, transform2.position, transform2.rotation, out var _, out submergedAmount);
		}
	}

	private void OnCollisionEnter(Collision collision)
	{
		if (collision.body == null)
		{
			float magnitude = collision.impulse.magnitude;
			if (magnitude > 1000f && base.xform.position.y > Datum.LocalSeaY)
			{
				SceneSingleton<EffectManager>.i.ImpactDust(magnitude, collision.GetContact(0).point.ToGlobalPosition(), Quaternion.LookRotation(collision.impulse + rb.velocity * rb.mass * 5f));
			}
		}
		if (!(impactDamage.threshold > 0f) || !parentUnit.LocalSim)
		{
			return;
		}
		float num = (collision.impulse.magnitude / Time.fixedDeltaTime / (rb.mass * 9.81f) - impactDamage.threshold) * impactDamage.multiplier;
		if (!(num <= 0f))
		{
			if (parentUnit.IsServer)
			{
				TakeDamage(0f, 0f, 1f, 0f, num, PersistentID.None);
			}
			else
			{
				parentUnit.Damage(id, new DamageInfo(0f, 0f, 0f, num));
			}
		}
	}

	private void OnCollisionStay(Collision collision)
	{
		if (collision.relativeVelocity.sqrMagnitude < 25f)
		{
			return;
		}
		Vector3 point = collision.GetContact(0).point;
		if (point.y < Datum.LocalSeaY)
		{
			return;
		}
		if (UnityEngine.Random.value > 0.75f && collision.collider.sharedMaterial == GameAssets.i.terrainMaterial)
		{
			if (contactDustID == -1 && SceneSingleton<ParticleEffectManager>.i != null)
			{
				contactDustID = SceneSingleton<ParticleEffectManager>.i.GetSystemID("ContactDust");
			}
			SceneSingleton<ParticleEffectManager>.i.EmitParticles(contactDustID, 1, base.xform.GlobalPosition(), rb.velocity, 0f, Mathf.Max(5f - parentUnit.speed * 0.1f, 1f), 0.5f, Mathf.Max(collisionSize.x + collisionSize.y + collisionSize.z, 4f) + Mathf.Min(parentUnit.speed * 0.05f, 10f), 0.3f, parentUnit.speed * 0.3f, 0.5f, 0.3f);
		}
		else if (UnityEngine.Random.value > 0.75f && collision.collider.sharedMaterial == null && parentUnit != null && parentUnit is Aircraft aircraft)
		{
			aircraft.ThrowSparks(point, Vector3.zero);
			if (contactSmokeID == -1 && SceneSingleton<ParticleEffectManager>.i != null)
			{
				contactSmokeID = SceneSingleton<ParticleEffectManager>.i.GetSystemID("ContactSmoke");
			}
			SceneSingleton<ParticleEffectManager>.i.EmitParticles(contactSmokeID, 1, base.xform.GlobalPosition(), rb.velocity, 0f, Mathf.Max(3f - parentUnit.speed * 0.1f, 1f), 0.5f, Mathf.Max(collisionSize.x + collisionSize.y + collisionSize.z, 4f) + Mathf.Min(parentUnit.speed * 0.05f, 10f), 0.3f, parentUnit.speed * 0.3f, 0.2f, 0.3f);
		}
	}

	public override void ModifyDrag(float amount)
	{
		dragArea += amount;
	}

	public void ModifyWingArea(float amount)
	{
		wingArea += amount;
	}

	private void Part_OnTensorBreak(UnitPart part)
	{
		for (int i = 0; i < joints.Length; i++)
		{
			if (part == joints[i].tensor && joints[i].joint != null)
			{
				joints[i].joint.breakForce = 0f;
				joints[i].joint.breakTorque = 0f;
				if (parentUnit.displayDetail < 1f)
				{
					break;
				}
				AudioSource audioSource = base.gameObject.AddComponent<AudioSource>();
				audioSource.outputAudioMixerGroup = SoundManager.i.EffectsMixer;
				audioSource.bypassListenerEffects = true;
				audioSource.clip = joints[i].breakSound;
				audioSource.pitch = UnityEngine.Random.Range(0.7f, 1.5f);
				audioSource.spatialBlend = 1f;
				audioSource.dopplerLevel = 1f;
				audioSource.spread = 5f;
				audioSource.maxDistance = 500f;
				audioSource.minDistance = 5f;
				audioSource.volume = 0.8f;
				audioSource.Play();
				UnityEngine.Object.Destroy(audioSource, 3f);
			}
		}
	}

	public void CheckAttachment()
	{
		if (simplePhysics || attachInfo == null || attachInfo.detachedFromParentPart)
		{
			return;
		}
		Vector3 b = attachInfo.parentPart.xform.InverseTransformPoint(base.xform.position);
		if (FastMath.OutOfRange(attachInfo.localPosition, b, 0.5f))
		{
			attachInfo.detachedFromParentPart = true;
			if (streamlining > 0f)
			{
				attachInfo.parentPart.ModifyDrag(streamlining);
			}
			parentUnit.DetachPart(id, rb.velocity, base.xform.position - parentUnit.transform.position);
		}
	}

	public override void TakeShockwave(Vector3 origin, float overpressure, float blastPower)
	{
		float num = (collisionSize.x + collisionSize.y + collisionSize.z) * 0.3333f;
		float a = Mathf.Min(wingArea + dragArea + num * num, blastPower * blastPower * 1f) * overpressure;
		Vector3 vector = Vector3.Lerp(FastMath.NormalizedDirection(origin, base.xform.position), UnityEngine.Random.insideUnitSphere.normalized, overpressure * 0.001f);
		a = Mathf.Min(a, 60f * rb.mass);
		rb.AddForceAtPosition(vector * a, liftNormal.position, ForceMode.Impulse);
	}

	public override void Detach(Vector3 velocity, Vector3 relativePos)
	{
		base.Detach(velocity, relativePos);
		if (FastMath.InRange(SceneSingleton<CameraStateManager>.i.transform.position, base.transform.position, 5000f))
		{
			SpawnFragments();
		}
		if (simplePhysics)
		{
			_ = attachInfo.parentPart.rb;
			_ = attachInfo.parentPart.rb.mass;
			base.xform.SetParent(null);
			rb = base.gameObject.AddComponent<Rigidbody>();
			rb.mass = CalcMassWithChildren();
			rb.interpolation = RigidbodyInterpolation.Interpolate;
			rb.velocity = velocity;
			rb.angularVelocity = attachInfo.parentPart.rb.angularVelocity;
		}
		rb.drag = 0.15f;
		wingEffectiveness = 0f;
	}

	protected override void UnitPart_OnParentDetached(UnitPart parentPart)
	{
		base.UnitPart_OnParentDetached(this);
	}

	public void CreateRB(Vector3 velocity, Vector3 position)
	{
		if (mass == 0f || attachInfo == null || (rb != null && rb != parentUnit.rb))
		{
			if (rb == null)
			{
				Debug.LogError($"Couldn't find rb for part {base.gameObject}, attachInfo.parentPart = {attachInfo.parentPart}");
			}
			rb.mass = mass;
			return;
		}
		if (joints.Length != 0 && joints[0].connectedPart == null)
		{
			joints[0].connectedPart = attachInfo.parentPart;
		}
		base.xform.SetParent(null, worldPositionStays: true);
		rb = base.gameObject.AddComponent<Rigidbody>();
		rb.mass = mass;
		rb.drag = 0f;
		rb.angularDrag = 0f;
		rb.sleepThreshold = 0f;
		rb.velocity = velocity;
		rb.angularVelocity = parentUnit.rb.angularVelocity;
		rb.useGravity = true;
		if (position != Vector3.zero)
		{
			rb.position = position;
		}
		rb.interpolation = RigidbodyInterpolation.Interpolate;
		simplePhysics = false;
		if (debugArrow != null)
		{
			debugArrow.GetComponent<MeshRenderer>().material.color = Color.red;
		}
	}

	public void CreateJoints()
	{
		for (int i = 0; i < joints.Length; i++)
		{
			PartJoint partJoint = joints[i];
			FixedJoint fixedJoint = base.gameObject.AddComponent<FixedJoint>();
			if (partJoint.anchor != null)
			{
				fixedJoint.anchor = partJoint.anchor.position - rb.position;
			}
			fixedJoint.connectedBody = partJoint.connectedPart.rb;
			fixedJoint.enableCollision = false;
			fixedJoint.breakForce = partJoint.breakForce * 10f;
			fixedJoint.breakTorque = partJoint.breakTorque * 10f;
			partJoint.joint = fixedJoint;
			if (partJoint.solverIterations != 6)
			{
				rb.solverIterations = partJoint.solverIterations;
			}
		}
	}

	public void SetHingeJoint(int index, AeroPart connectedPart, float spring, float damp, float targetPosition, float breakStrength, float baseAngleOffset, Vector3 rotationAxis, Transform anchorTransform = null, int solverIterations = 8)
	{
		if (rb == parentUnit.rb)
		{
			if (anchorTransform != null)
			{
				if (base.xform.parent != anchorTransform)
				{
					base.xform.SetParent(anchorTransform);
				}
				anchorTransform.localEulerAngles = new Vector3(0f, 0f, targetPosition);
			}
			else
			{
				base.xform.localEulerAngles = rotationAxis * (targetPosition + baseAngleOffset);
			}
			return;
		}
		if (movingJoints.Count <= index)
		{
			PartJoint partJoint = new PartJoint();
			HingeJoint hingeJoint = base.gameObject.AddComponent<HingeJoint>();
			hingeJoint.connectedBody = connectedPart.rb;
			hingeJoint.anchor = ((anchorTransform != null) ? base.transform.InverseTransformPoint(anchorTransform.position) : Vector3.zero);
			hingeJoint.axis = ((anchorTransform != null) ? base.transform.InverseTransformDirection(anchorTransform.forward) : Vector3.right);
			rb.solverIterations = solverIterations;
			connectedPart.rb.solverIterations = solverIterations;
			hingeJoint.breakForce = breakStrength;
			hingeJoint.breakTorque = breakStrength;
			hingeJoint.useSpring = true;
			partJoint.joint = hingeJoint;
			movingJoints.Add(partJoint);
		}
		HingeJoint hingeJoint2 = movingJoints[index].joint as HingeJoint;
		if (!(hingeJoint2 == null))
		{
			JointSpring spring2 = new JointSpring
			{
				spring = spring,
				damper = damp,
				targetPosition = targetPosition
			};
			attachInfo.localPosition = attachInfo.parentPart.xform.InverseTransformPoint(base.xform.position);
			hingeJoint2.spring = spring2;
		}
	}

	public void BreakAllJoints()
	{
		for (int num = joints.Length - 1; num >= 0; num--)
		{
			if (joints[num].joint != null)
			{
				UnityEngine.Object.Destroy(joints[num].joint);
			}
		}
		joints = Array.Empty<PartJoint>();
	}

	public void BreakJointsToPart(AeroPart part)
	{
		PartJoint[] array = joints;
		foreach (PartJoint partJoint in array)
		{
			if (partJoint.joint != null && partJoint.connectedPart == part)
			{
				partJoint.joint.breakForce = 0f;
				partJoint.joint.breakTorque = 0f;
			}
		}
	}

	unsafe ~AeroPart()
	{
		if (JobFields.ptr != null)
		{
			Console.WriteLine("[PtrAllocation] AeroPart memory leaked.");
		}
	}

	private static void DisposeJobFields(ref PtrAllocation<AeroPartFields> fields)
	{
		if (fields.IsCreated)
		{
			ref AeroPartFields reference = ref fields.Ref();
			if (reference.liftTransformIndex.IsCreated)
			{
				reference.liftTransformIndex.RemoveRef();
			}
			if (reference.otherTransformIndex.IsCreated)
			{
				reference.otherTransformIndex.RemoveRef();
			}
		}
		fields.Dispose();
	}

	public bool GetJobTransforms(out Transform liftTransform, out Transform otherTransform)
	{
		if (airflowChanneling > 0f)
		{
			liftTransform = liftNormal;
			otherTransform = base.xform;
			return true;
		}
		liftTransform = liftNormal;
		otherTransform = null;
		return false;
	}

	private Ptr<AeroPartFields> GetOrCreateJobField()
	{
		if (!JobFields.IsCreated)
		{
			JobsAllocator<AeroPartFields>.Allocate(ref JobFields);
			ref AeroPartFields reference = ref JobFields.Ref();
			reference.centerOfLift = centerOfLift;
			reference.airfoilID = airfoilID;
			reference.buoyancy = buoyancy;
			reference.collisionSize = collisionSize;
			reference.airflowChanneling = airflowChanneling;
			reference.lastSplashTime = 0f;
		}
		return JobFields;
	}

	public void UpdateJobFields()
	{
		ref AeroPartFields reference = ref JobFields.Ref();
		reference.velocity = rb.velocity;
		reference.mass = mass;
		reference.wingArea = wingArea;
		reference.dragArea = dragArea;
		reference.wingEffectiveness = wingEffectiveness;
		reference.submergedAmount = submergedAmount;
		submergedAmount = 0f;
	}

	public void MergeWithParent()
	{
		if (attachInfo == null || attachInfo.detachedFromParentPart)
		{
			return;
		}
		base.xform.SetParent(attachInfo.parentPart.xform);
		base.xform.localPosition = attachInfo.localPosition;
		base.xform.localRotation = attachInfo.localRotation;
		for (int i = 0; i < joints.Length; i++)
		{
			if (joints[i].joint != null)
			{
				UnityEngine.Object.Destroy(joints[i].joint);
			}
		}
		if (rb != parentUnit.rb)
		{
			UnityEngine.Object.Destroy(rb);
		}
		rb = parentUnit.rb;
		simplePhysics = true;
		if (debugArrow != null)
		{
			debugArrow.GetComponent<MeshRenderer>().material.color = Color.yellow;
		}
	}

	public override void ApplyDamage(float netPierceDamage, float netBlastDamage, float netFireDamage, float netImpactDamage)
	{
		base.ApplyDamage(netPierceDamage, netBlastDamage, netFireDamage, netImpactDamage);
		wingEffectiveness = Mathf.Lerp(0.5f, 1f, hitPoints * 0.01f);
		if (!parentUnit.LocalSim)
		{
			return;
		}
		if (attachInfo != null)
		{
			attachInfo.attachmentStrength = 0f;
		}
		float num = Mathf.Max((hitPoints - structuralThreshold) / (100f - structuralThreshold), 0f);
		PartJoint[] array = joints;
		foreach (PartJoint partJoint in array)
		{
			if (partJoint.joint != null)
			{
				partJoint.joint.breakForce = partJoint.breakForce * num * 10f;
				partJoint.joint.breakTorque = partJoint.breakTorque * num * 10f;
				attachInfo.attachmentStrength += partJoint.breakForce + partJoint.breakTorque;
			}
		}
		if (parentUnit.displayDetail > 1f && netPierceDamage > 0f)
		{
			if (hitSource == null)
			{
				hitSource = base.gameObject.AddComponent<AudioSource>();
				hitSource.outputAudioMixerGroup = SoundManager.i.EffectsMixer;
				hitSource.bypassListenerEffects = true;
				hitSource.clip = hitSound;
				hitSource.spatialBlend = 1f;
				hitSource.dopplerLevel = 0f;
				hitSource.spread = 5f;
				hitSource.maxDistance = 200f;
				hitSource.minDistance = 5f;
				hitSource.priority = 128;
			}
			hitSource.pitch = UnityEngine.Random.Range(0.8f, 1.2f);
			hitSource.Play();
		}
	}

	public override void SpawnFragments()
	{
		base.SpawnFragments();
		for (int i = 0; i < joints.Length; i++)
		{
			if (joints[i].joint != null)
			{
				joints[i].joint.breakForce = 0f;
				joints[i].joint.breakTorque = 0f;
			}
		}
	}

	public void ApplyJobFields()
	{
		if (!JobFields.IsCreated)
		{
			return;
		}
		ref AeroPartFields reference = ref JobFields.Ref();
		if (reference.splashed)
		{
			Vector3 position = base.xform.position;
			bool flag = false;
			bool flag2 = false;
			if (parentUnit.speed > 83f && parentUnit.LocalSim)
			{
				PartJoint[] array = joints;
				foreach (PartJoint partJoint in array)
				{
					if (partJoint.joint != null)
					{
						partJoint.joint.breakForce = 0f;
						partJoint.joint.breakTorque = 0f;
						attachInfo.attachmentStrength = 0f;
					}
				}
			}
			if (Physics.Linecast(position + Vector3.up * 100f, position - Vector3.up * 10f, out var hitInfo, PhysicsLayers.StaticsMask))
			{
				flag2 = hitInfo.collider.sharedMaterial == GameAssets.i.WaterMaterial;
				flag = !flag2 && hitInfo.point.y > Datum.LocalSeaY;
			}
			if (!flag2)
			{
				position.y = Datum.LocalSeaY;
			}
			if (!flag && SceneSingleton<ParticleEffectManager>.i != null)
			{
				SceneSingleton<ParticleEffectManager>.i.GetPrefabEffect(GameAssets.i.splash_large).Play(position, Quaternion.LookRotation(Vector3.up + new Vector3(rb.velocity.x, 0f, rb.velocity.z) * 0.1f));
			}
		}
		if (reference.angularDragChanged)
		{
			rb.angularDrag = reference.angularDrag;
		}
		switch (reference.hasForce)
		{
		case JobForceType.Force:
			rb.AddForce(reference.force);
			break;
		case JobForceType.ForceAndTorque:
			rb.AddForce(reference.force);
			rb.AddTorque(reference.torque);
			break;
		}
	}

	public void DisableMaterialCleanup()
	{
		if (!(parentUnit is Aircraft aircraft) || !aircraft.TryGetLiveryBehaviour(out var liveryBehaviour))
		{
			return;
		}
		foreach (Renderer renderer in damageMaterial.renderers)
		{
			liveryBehaviour.RemoveFromMaterialCleanup(renderer.material);
		}
	}

	public override void RemoveFromUnit()
	{
		base.RemoveFromUnit();
		if (parentUnit is Aircraft aircraft)
		{
			aircraft.DeregisterAeroPart(this);
		}
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		JobManager.Remove(ref JobPart);
		DisposeJobFields(ref JobFields);
	}
}
