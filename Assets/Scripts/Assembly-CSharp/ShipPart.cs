using System;
using NuclearOption.Jobs;
using UnityEngine;

public class ShipPart : UnitPart
{
	[SerializeField]
	private float displacement;

	[SerializeField]
	private float height;

	[SerializeField]
	private float leakThreshold;

	[SerializeField]
	private float leakRateMin;

	[SerializeField]
	private float leakRateMax;

	[SerializeField]
	private float sinkThreshold;

	[SerializeField]
	private float breakJointStrength = 1000f;

	[SerializeField]
	private Vector3 directionalDrag;

	[SerializeField]
	private Transform forceTransform;

	public float originalDisplacement;

	public float leakToDisplacement;

	[SerializeField]
	private GameObject sinkEffect;

	[SerializeField]
	private ShipPart[] connectedCompartments;

	[SerializeField]
	private bool compartmentalized;

	[SerializeField]
	private bool debugBreak;

	private bool submerged;

	private float surfaceArea;

	private float leakRate;

	private Ship parentShip;

	public PtrAllocation<ShipPartFields> JobFields;

	public JobPart<ShipPart, ShipPartFields> JobPart;

	private bool damageControlActive;

	private float damageControlDelay = 20f;

	public event Action<ShipPart> onDetachFromParent;

	protected override void Awake()
	{
		base.Awake();
		base.enabled = false;
		bounds = base.gameObject.GetComponent<Collider>().bounds;
		leakRate = 0f;
		surfaceArea = bounds.extents.x * bounds.extents.z;
		height = bounds.extents.y;
		rb = parentUnit.rb;
		originalDisplacement = displacement;
		leakToDisplacement = displacement;
		parentShip = parentUnit as Ship;
		parentShip.parts.Add(this);
		damageControlActive = false;
		damageControlDelay /= Mathf.Clamp(parentShip.skill, 0.1f, 1f);
		if (forceTransform == null)
		{
			forceTransform = base.xform;
		}
		if (base.xform.parent == null)
		{
			parentUnit.rb.mass = CalcMassWithChildren();
		}
	}

	public void Flood()
	{
		leakToDisplacement = 0f;
		leakRate = leakRateMax;
		directionalDrag.z = directionalDrag.x;
	}

	private void Leak()
	{
		if (base.transform.GlobalPosition().y - height < Datum.SeaLevel.y)
		{
			displacement -= leakRate * Time.deltaTime;
		}
		if (compartmentalized || !(displacement < originalDisplacement * parentShip.damageControlDeploymentThreshold))
		{
			return;
		}
		parentShip.damageControlAvailable -= originalDisplacement;
		compartmentalized = true;
		if (parentShip.damageControlAvailable <= 0f)
		{
			ShipPart[] array = connectedCompartments;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Flood();
			}
		}
	}

	private void DamageControl()
	{
		if (!compartmentalized && !detachedFromUnit && !parentShip.disabled && !submerged && !(parentShip.damageControlAvailable <= 0f))
		{
			float num = 0.02f * leakRateMin;
			leakRate -= num;
			leakRate = Mathf.Max(leakRate, 0f);
			float num2 = 0.001f * originalDisplacement;
			displacement += num2;
			displacement = Mathf.Min(displacement, originalDisplacement);
			parentShip.damageControlAvailable -= 10f * num + num2;
		}
	}

	public JobPart<ShipPart, ShipPartFields> SetupJob()
	{
		ColorLog<ShipPart>.InfoAssert(JobPart == null, "should not have job part before detached");
		JobPart = new JobPart<ShipPart, ShipPartFields>(this, GetOrCreateJobField());
		return JobPart;
	}

	public Transform GetJobTransforms()
	{
		return forceTransform;
	}

	private static void DisposeJobFields(ref PtrAllocation<ShipPartFields> fields)
	{
		fields.Dispose();
	}

	private Ptr<ShipPartFields> GetOrCreateJobField()
	{
		if (!JobFields.IsCreated)
		{
			JobsAllocator<ShipPartFields>.Allocate(ref JobFields);
			ref ShipPartFields reference = ref JobFields.Ref();
			reference.directionalDrag = directionalDrag;
			reference.partHeight = height;
		}
		return JobFields;
	}

	unsafe ~ShipPart()
	{
		if (JobFields.ptr != null)
		{
			Console.WriteLine("[PtrAllocation] ShipPart memory leaked.");
		}
	}

	public void UpdateJobFields(ref JobTransformValues.ReadOnly transform)
	{
		ref ShipPartFields reference = ref JobFields.Ref();
		reference.velocity = rb.GetPointVelocity(transform.Position);
		reference.displacement = displacement;
		reference.mass = mass;
	}

	public void ApplyJobFields()
	{
		if (!JobFields.IsCreated)
		{
			return;
		}
		ref ShipPartFields reference = ref JobFields.Ref();
		if (reference.submergedAmount > sinkThreshold && !submerged && displacement < originalDisplacement * 0.5f)
		{
			submerged = true;
			Flood();
			if (attachInfo != null && !attachInfo.detachedFromParentPart)
			{
				(attachInfo.parentPart as ShipPart).Flood();
			}
			Vector3 position = base.xform.position;
			position.y = Datum.LocalSeaY;
			UnityEngine.Object.Instantiate(sinkEffect, position, Quaternion.LookRotation(Vector3.up)).transform.SetParent(base.xform);
		}
		if (leakToDisplacement < displacement)
		{
			Leak();
		}
		if (detachedFromUnit)
		{
			rb.angularDrag = Mathf.Clamp(reference.submergedAmount * 2f, 0.1f, 2f);
			rb.AddForce(reference.force);
		}
	}

	protected override void UnitPart_OnParentDetached(UnitPart parentPart)
	{
		rb = parentPart.rb;
		if (parentShip.remoteSim && !detachedFromUnit)
		{
			JobManager.Add(SetupJob());
		}
		base.UnitPart_OnParentDetached(this);
	}

	public override void Detach(Vector3 velocity, Vector3 relativePos)
	{
		Rigidbody connectedBody = attachInfo.parentPart.rb;
		parentShip.damageControlAvailable -= originalDisplacement;
		base.xform.SetParent(null);
		rb = base.gameObject.AddComponent<Rigidbody>();
		rb.mass = CalcMassWithChildren();
		attachInfo.parentPart.rb.mass -= rb.mass;
		rb.interpolation = RigidbodyInterpolation.Interpolate;
		rb.velocity = velocity;
		rb.angularVelocity = attachInfo.parentPart.rb.angularVelocity;
		rb.maxLinearVelocity = 60f;
		if (breakJointStrength > 0f)
		{
			ConfigurableJoint configurableJoint = base.gameObject.AddComponent<ConfigurableJoint>();
			configurableJoint.connectedBody = connectedBody;
			configurableJoint.xMotion = ConfigurableJointMotion.Locked;
			configurableJoint.yMotion = ConfigurableJointMotion.Locked;
			configurableJoint.zMotion = ConfigurableJointMotion.Locked;
			configurableJoint.angularXMotion = ConfigurableJointMotion.Limited;
			configurableJoint.angularYMotion = ConfigurableJointMotion.Limited;
			configurableJoint.angularZMotion = ConfigurableJointMotion.Limited;
			SoftJointLimit softJointLimit = new SoftJointLimit
			{
				limit = 20f
			};
			configurableJoint.highAngularXLimit = softJointLimit;
			configurableJoint.lowAngularXLimit = softJointLimit;
			configurableJoint.angularYLimit = softJointLimit;
			configurableJoint.angularZLimit = softJointLimit;
			configurableJoint.anchor = (base.xform.InverseTransformPoint(attachInfo.parentPart.xform.position) + attachInfo.localPosition) / 2f;
			float breakForce = rb.mass * breakJointStrength;
			configurableJoint.breakForce = breakForce;
		}
		displacement *= 0f;
		Flood();
		ShipPart shipPart = attachInfo.parentPart as ShipPart;
		shipPart.displacement *= 0.8f;
		shipPart.Flood();
		ShipPart[] array = connectedCompartments;
		foreach (ShipPart shipPart2 in array)
		{
			if (!(shipPart2 == shipPart))
			{
				shipPart2.displacement *= 0.8f;
				shipPart2.Flood();
			}
		}
		base.Detach(Vector3.zero, Vector3.zero);
	}

	public override void ApplyDamage(float netPierceDamage, float netBlastDamage, float netFireDamage, float netImpactDamage)
	{
		float num = netPierceDamage + netBlastDamage + netFireDamage + netImpactDamage;
		hitPoints -= num;
		float num2 = Mathf.Clamp01((hitPoints - structuralThreshold) / (leakThreshold - structuralThreshold));
		leakRate = Mathf.Lerp(leakRateMax, leakRateMin, num2);
		leakToDisplacement = Mathf.Clamp(originalDisplacement * hitPoints / leakThreshold, 0f, originalDisplacement);
		directionalDrag.z = Mathf.Lerp(directionalDrag.z, directionalDrag.x, Mathf.Pow(1f - num2, 2f));
		if (!damageControlActive && leakToDisplacement < originalDisplacement)
		{
			damageControlActive = true;
			this.StartSlowUpdateDelayed(damageControlDelay, 1f, DamageControl);
		}
		for (int i = 0; i < damageMaterial.renderers.Count; i++)
		{
			if (damageMaterial.indices.Length != 0)
			{
				for (int j = 0; j < damageMaterial.indices.Length; j++)
				{
					damageMaterial.renderers[i].materials[j].SetFloat("_HitPoints", hitPoints);
				}
			}
			else if (damageMaterial.renderers[i] != null)
			{
				damageMaterial.renderers[i].material.SetFloat("_HitPoints", hitPoints);
			}
		}
		for (int num3 = damageEffects.Count - 1; num3 >= 0; num3--)
		{
			DamageEffect damageEffect = damageEffects[num3];
			if (hitPoints < damageEffect.threshold)
			{
				GameObject gameObject = UnityEngine.Object.Instantiate(damageEffect.prefab, base.xform.position, base.xform.rotation, base.xform);
				if (gameObject.TryGetComponent<DamageParticles>(out var component))
				{
					parentUnit.spawnedEffects.Add(component);
				}
				else
				{
					gameObject.transform.SetParent(Datum.origin);
				}
				damageEffects.RemoveAt(num3);
			}
		}
		if (hitPoints < integrityThreshold)
		{
			SpawnFragments();
		}
		InvokeDamage(netPierceDamage, netBlastDamage, netFireDamage, netImpactDamage);
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		JobManager.Remove(ref JobPart);
		DisposeJobFields(ref JobFields);
	}

	public bool IsCriticallyDamaged()
	{
		if (!submerged && !(hitPoints <= 0f))
		{
			return IsDetached();
		}
		return true;
	}

	public float GetDisplacement()
	{
		return displacement;
	}

	public float GetOriginalDisplacement()
	{
		return originalDisplacement;
	}

	public float GetLeakThreshold()
	{
		return leakThreshold;
	}

	public float GetLeakMin()
	{
		return leakRateMin;
	}

	public float GetLeak()
	{
		return leakRate;
	}

	public float GetLeakMax()
	{
		return leakRateMax;
	}

	public float GetSinkThreshold()
	{
		return sinkThreshold;
	}

	public float GetBreakStrength()
	{
		return breakJointStrength;
	}

	public Vector3 GetDirectionalDrag()
	{
		return directionalDrag;
	}
}
