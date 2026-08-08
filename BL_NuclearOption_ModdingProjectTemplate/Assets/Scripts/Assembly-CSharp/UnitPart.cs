using System;
using System.Collections.Generic;
using NuclearOption.SavedMission;
using UnityEngine;

public class UnitPart : MonoBehaviour, IDamageable
{
	protected class AttachInfo
	{
		public UnitPart parentPart;

		public float attachmentStrength;

		public bool detachedFromParentPart;

		public Vector3 localPosition;

		public Quaternion localRotation;

		public AttachInfo(UnitPart parentPart, bool detached, Vector3 localPosition, Quaternion localRotation)
		{
			this.parentPart = parentPart;
			detachedFromParentPart = detached;
			this.localPosition = localPosition;
			this.localRotation = localRotation;
		}
	}

	public struct OnApplyDamage
	{
		public bool detached;

		public float hitPoints;

		public float pierceDamage;

		public float blastDamage;

		public float fireDamage;

		public float impactDamage;
	}

	public struct OnCollision
	{
		public float force;

		public float g;
	}

	[HideInInspector]
	public byte id;

	[Header("Structure")]
	[SerializeField]
	protected bool criticalPart;

	public Unit parentUnit;

	public float mass;

	protected Vector3 collisionSize;

	protected Bounds bounds;

	public Rigidbody rb;

	private Vector3 velocity;

	protected bool fragmented;

	protected bool detachedFromUnit;

	[SerializeField]
	protected Transform centerOfMass;

	[Header("Damage")]
	public float hitPoints = 100f;

	[SerializeField]
	protected ArmorProperties armorProperties;

	[SerializeField]
	protected float structuralThreshold;

	[SerializeField]
	protected float integrityThreshold = float.MinValue;

	[SerializeField]
	protected AudioClip hitSound;

	protected AudioSource hitSource;

	[SerializeField]
	protected ImpactDamage impactDamage;

	[SerializeField]
	protected DamageMaterial damageMaterial;

	[SerializeField]
	protected List<DamageEffect> damageEffects = new List<DamageEffect>();

	protected List<DamageParticles> hostedParticles = new List<DamageParticles>();

	[SerializeField]
	protected GameObject[] disintegrationEffects;

	[SerializeField]
	protected GameObject[] disintegrateObjects;

	protected AttachInfo attachInfo;

	protected float baseMass;

	protected float weaponDrag;

	protected float weaponMass;

	protected float fuelMass;

	public Transform CenterOfMass => centerOfMass;

	public Transform xform { get; private set; }

	public event Action<UnitPart> onParentDetached;

	public event Action<UnitPart> onPartDetached;

	public event Action<Rigidbody> onParentRBChanged;

	public event Action<UnitPart> onJointBroken;

	public event Action<OnApplyDamage> onApplyDamage;

	public bool IsDetached()
	{
		return detachedFromUnit;
	}

	protected virtual void Awake()
	{
		xform = base.transform;
		UnitPart unitPart = ((xform.parent == null) ? null : xform.parent.GetComponent<UnitPart>());
		if (parentUnit == null && xform.parent != null)
		{
			unitPart = xform.parent.parent.GetComponent<UnitPart>();
			parentUnit = unitPart.parentUnit;
		}
		rb = parentUnit.rb;
		id = parentUnit.RegisterDamageable(this);
		parentUnit.partLookup.Add(this);
		CreateAttachInfo(unitPart);
		if (baseMass == 0f)
		{
			baseMass = mass;
		}
		hitPoints = 100f;
		if (damageMaterial != null)
		{
			Renderer component = GetComponent<Renderer>();
			if (damageMaterial.renderers.Count == 0 && component != null)
			{
				damageMaterial.renderers.Add(component);
			}
		}
		Collider component2 = GetComponent<Collider>();
		if (component2 != null)
		{
			collisionSize = component2.bounds.extents;
		}
	}

	public void CreateAttachInfo(UnitPart parentPart)
	{
		if (parentPart != null)
		{
			attachInfo = new AttachInfo(parentPart, detached: false, parentPart.transform.InverseTransformPoint(xform.position), xform.localRotation);
			attachInfo.parentPart.onParentDetached += UnitPart_OnParentDetached;
		}
	}

	public virtual void RemoveFromUnit()
	{
		if (!(parentUnit == null))
		{
			parentUnit.DeregisterDamageable(id);
			parentUnit.partLookup.Remove(this);
		}
	}

	protected virtual void OnDestroy()
	{
		this.onParentDetached = null;
		this.onPartDetached = null;
		this.onJointBroken = null;
		this.onParentRBChanged = null;
		this.onApplyDamage = null;
	}

	public Transform GetTransform()
	{
		return xform;
	}

	public float GetMass()
	{
		return mass;
	}

	public virtual void TakeShockwave(Vector3 origin, float overpressure, float blastPower)
	{
		if (!(rb == null))
		{
			float num = Mathf.Pow(rb.mass * 0.001f, 0.33f);
			float a = Mathf.Min(num * num, blastPower * blastPower * 1f) * overpressure * 0.03f;
			Vector3 vector = FastMath.NormalizedDirection(origin, xform.position);
			a = Mathf.Min(a, 60f * rb.mass);
			rb.AddForce(vector * a, ForceMode.Impulse);
		}
	}

	protected virtual void UnitPart_OnParentDetached(UnitPart parentPart)
	{
		detachedFromUnit = true;
		this.onParentDetached?.Invoke(this);
	}

	protected void UnitPart_OnParentRBChanged(Rigidbody rb)
	{
		this.rb = rb;
	}

	private void OnJointBreak(float breakForce)
	{
		this.onJointBroken?.Invoke(this);
	}

	public void SetLivery(LiveryData livery, MaterialCleanup materialCleanup)
	{
		for (int i = 0; i < damageMaterial.renderers.Count; i++)
		{
			Material material = damageMaterial.renderers[i].material;
			materialCleanup.Add(material);
			material.SetTexture("_Livery", livery.Texture);
			material.SetFloat("_Glossiness", livery.Glossiness);
		}
	}

	public ArmorProperties GetArmorProperties()
	{
		return armorProperties;
	}

	public float GetStructuralThreshold()
	{
		return structuralThreshold;
	}

	public float CalcMassWithChildren()
	{
		float num = 0f;
		UnitPart[] componentsInChildren = base.gameObject.GetComponentsInChildren<UnitPart>();
		foreach (UnitPart unitPart in componentsInChildren)
		{
			num += unitPart.mass;
		}
		return num;
	}

	public void ModifyMass(float amount)
	{
		mass += amount;
		rb.mass = mass;
		if (centerOfMass != null && parentUnit.LocalSim)
		{
			rb.centerOfMass = centerOfMass.localPosition;
		}
	}

	public virtual void ModifyDrag(float amount)
	{
	}

	public virtual void Repair()
	{
		hitPoints = 100f;
	}

	public void AddHostedParticles(DamageParticles damageParticles)
	{
		hostedParticles.Add(damageParticles);
	}

	public void TakeDamage(float pierceDamage, float blastDamage, float amountAffected, float fireDamage, float impactDamage, PersistentID dealerID)
	{
		if (parentUnit.SavedUnit is SavedScenery { indestructible: not false })
		{
			return;
		}
		if (!parentUnit.IsServer)
		{
			Debug.LogWarning($"TakeDamage called on {this} but it is not spawned on server");
			return;
		}
		float num = Mathf.Max(pierceDamage - armorProperties.pierceArmor, 0f) / Mathf.Max(armorProperties.pierceTolerance, 0.01f);
		float num2 = blastDamage * amountAffected / Mathf.Max(armorProperties.blastTolerance, 0.01f);
		float num3 = Mathf.Max(fireDamage - armorProperties.fireArmor, 0f) / Mathf.Max(armorProperties.fireTolerance, 0.01f);
		float num4 = num + num2 + num3 + impactDamage;
		if (parentUnit == null || num4 <= 0f)
		{
			return;
		}
		if (dealerID.IsValid && dealerID != parentUnit.persistentID)
		{
			parentUnit.RecordDamage(dealerID, num4);
		}
		if (criticalPart && hitPoints - num4 <= 0f && !parentUnit.disabled)
		{
			parentUnit.Networkdisabled = true;
			if (!(parentUnit is Scenery))
			{
				parentUnit.ReportKilled();
			}
		}
		parentUnit.RpcDamage(id, new DamageInfo(num, num2, num3, impactDamage));
		if (attachInfo != null && !attachInfo.detachedFromParentPart && hitPoints - num4 < structuralThreshold && !(this is AeroPart))
		{
			attachInfo.detachedFromParentPart = true;
			parentUnit.DetachPart(id, (rb != null) ? rb.GetPointVelocity(xform.position) : Vector3.zero, xform.position - attachInfo.parentPart.transform.position);
		}
	}

	public void InvokeDamage(float netPierceDamage, float netBlastDamage, float netFireDamage, float netImpactDamage)
	{
		OnApplyDamage obj = new OnApplyDamage
		{
			detached = detachedFromUnit,
			hitPoints = hitPoints,
			pierceDamage = netPierceDamage,
			blastDamage = netBlastDamage,
			fireDamage = netFireDamage,
			impactDamage = netImpactDamage
		};
		this.onApplyDamage?.Invoke(obj);
	}

	public virtual void ApplyDamage(float netPierceDamage, float netBlastDamage, float netFireDamage, float netImpactDamage)
	{
		hitPoints -= netPierceDamage + netBlastDamage + netFireDamage + netImpactDamage;
		for (int i = 0; i < damageMaterial.renderers.Count; i++)
		{
			if (!(damageMaterial.renderers[i] == null))
			{
				damageMaterial.renderers[i].material.SetFloat("_HitPoints", hitPoints);
			}
		}
		InvokeDamage(netPierceDamage, netBlastDamage, netFireDamage, netImpactDamage);
		for (int num = damageEffects.Count - 1; num >= 0; num--)
		{
			DamageEffect damageEffect = damageEffects[num];
			if (hitPoints < damageEffect.threshold)
			{
				GameObject gameObject = UnityEngine.Object.Instantiate(damageEffect.prefab, xform);
				if (gameObject.TryGetComponent<DamageParticles>(out var component))
				{
					parentUnit.spawnedEffects.Add(component);
				}
				else
				{
					gameObject.transform.SetParent(Datum.origin);
				}
				damageEffects.RemoveAt(num);
			}
		}
		if (hitPoints < integrityThreshold)
		{
			SpawnFragments();
		}
	}

	public virtual void Detach(Vector3 velocity, Vector3 relativePos)
	{
		if (attachInfo == null)
		{
			Debug.LogError($"attachInfo null for part {base.name}, id:{id}");
			return;
		}
		attachInfo.parentPart.onParentDetached -= UnitPart_OnParentDetached;
		attachInfo.detachedFromParentPart = true;
		detachedFromUnit = true;
		this.onParentDetached?.Invoke(this);
		this.onPartDetached?.Invoke(this);
	}

	public void RemovePart()
	{
		UnityEngine.Object.Destroy(base.gameObject);
	}

	public void DetachDamageParticles()
	{
		foreach (DamageParticles hostedParticle in hostedParticles)
		{
			if (hostedParticle != null)
			{
				hostedParticle.ParentObjectCulled();
			}
		}
	}

	public virtual void SpawnFragments()
	{
		this.onJointBroken?.Invoke(this);
		if (fragmented)
		{
			return;
		}
		GameObject[] array = disintegrateObjects;
		foreach (GameObject obj in array)
		{
			if (obj.TryGetComponent<Renderer>(out var component))
			{
				component.enabled = false;
			}
			obj.layer = PhysicsLayers.IgnoreCollisions;
		}
		if (xform.position.y > Datum.LocalSeaY)
		{
			array = disintegrationEffects;
			for (int i = 0; i < array.Length; i++)
			{
				GameObject gameObject = UnityEngine.Object.Instantiate(array[i], xform);
				if (gameObject.TryGetComponent<DamageParticles>(out var component2))
				{
					parentUnit.spawnedEffects.Add(component2);
				}
				else
				{
					gameObject.transform.SetParent(Datum.origin);
				}
			}
		}
		else
		{
			Vector3 position = xform.position;
			position.y = Datum.LocalSeaY;
			if (SceneSingleton<ParticleEffectManager>.i != null)
			{
				SceneSingleton<ParticleEffectManager>.i.GetPrefabEffect(GameAssets.i.splash_large).Play(position, Quaternion.LookRotation(Vector3.up));
			}
		}
		fragmented = true;
	}

	public Unit GetUnit()
	{
		return parentUnit;
	}

	public float GetAverageRadius()
	{
		return (collisionSize.x + collisionSize.y + collisionSize.z) * 0.33333f;
	}

	protected void ImpactDrag()
	{
	}
}
