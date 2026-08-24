using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NuclearOption.Networking;
using UnityEngine;

public class SubmunitionDispenser : MonoBehaviour, IDamageable
{
	[SerializeField]
	private Missile missile;

	[SerializeField]
	private GameObject[] casings;

	[SerializeField]
	private GameObject[] submunitions;

	[SerializeField]
	private WeaponInfo submunitionType;

	[SerializeField]
	private float ejectSpeed;

	[SerializeField]
	private float ejectInterval = 0.1f;

	[SerializeField]
	private float dispenseDistance = 2000f;

	[SerializeField]
	private float detectionRange = 400f;

	[SerializeField]
	private float deployedDrag = 0.01f;

	private List<Unit> detectedUnits;

	private List<Unit> targetedUnits;

	private bool dispensed;

	private byte dmgID;

	private void Awake()
	{
		dmgID = missile.RegisterDamageable(this);
		if (NetworkManagerNuclearOption.i.Server.Active)
		{
			this.StartSlowUpdateDelayed(1f, TargetApproachCheck);
		}
	}

	public void ApplyDamage(float pierceDamage, float blastDamage, float fireDamage, float impactDamage)
	{
		if (!dispensed && impactDamage == 1f)
		{
			dispensed = true;
			JettisonCasings().Forget();
			AssignSubmunitionTargets().Forget();
		}
	}

	public void Detach(Vector3 velocity, Vector3 relativePos)
	{
	}

	public ArmorProperties GetArmorProperties()
	{
		return missile.GetArmorProperties();
	}

	public float GetMass()
	{
		return missile.GetMass();
	}

	public Transform GetTransform()
	{
		return base.transform;
	}

	public Unit GetUnit()
	{
		return missile;
	}

	private void TargetApproachCheck()
	{
		if (!dispensed && !(missile.NetworkHQ == null))
		{
			_ = missile.targetID;
			if (missile.targetID.TryGetUnit(out var unit) && missile.NetworkHQ.IsTargetPositionAccurate(unit, detectionRange) && FastMath.InRange(unit.GlobalPosition(), missile.GlobalPosition(), dispenseDistance) && unit.LineOfSight(missile.transform.position, 1000f))
			{
				missile.NetworkHQ.RpcUpdateTrackingInfo(missile.targetID);
				missile.Damage(dmgID, new DamageInfo(0f, 0f, 0f, 1f));
			}
		}
	}

	public async UniTask JettisonCasings()
	{
		await UniTask.WaitForFixedUpdate();
		missile.rb.drag += deployedDrag;
		GameObject[] array = casings;
		foreach (GameObject gameObject in array)
		{
			gameObject.transform.SetParent(null);
			Vector3 vector = FastMath.NormalizedDirection(missile.transform.position, gameObject.transform.position);
			Rigidbody rigidbody = gameObject.AddComponent<Rigidbody>();
			rigidbody.angularVelocity = Random.insideUnitSphere;
			rigidbody.drag = 0.1f;
			rigidbody.angularDrag = 0.1f;
			int ignoreCollisions = PhysicsLayers.IgnoreCollisions;
			gameObject.layer = ignoreCollisions;
			gameObject.AddComponent<BoxCollider>();
			rigidbody.velocity = missile.rb.velocity + vector * ejectSpeed;
			rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
		}
	}

	public async UniTask AssignSubmunitionTargets()
	{
		_ = missile.targetID;
		if (!missile.targetID.TryGetUnit(out var unit))
		{
			return;
		}
		if (detectedUnits == null)
		{
			detectedUnits = new List<Unit>();
		}
		BattlefieldGrid.GetUnitsInRangeNonAlloc(unit.GlobalPosition(), detectionRange, detectedUnits);
		for (int num = detectedUnits.Count - 1; num >= 0; num--)
		{
			Unit unit2 = detectedUnits[num];
			if (unit2.NetworkHQ == missile.NetworkHQ || unit2 is Scenery || unit2 is Missile || unit2.speed > 60f || FastMath.OutOfRange(detectedUnits[num].GlobalPosition(), unit.GlobalPosition(), detectionRange))
			{
				detectedUnits.RemoveAt(num);
			}
		}
		CancellationToken cancel = base.destroyCancellationToken;
		int targetsAssigned = 0;
		int detectedIndex = 0;
		while (targetsAssigned < submunitions.Length && detectedUnits.Count > 0)
		{
			await UniTask.WaitForSeconds(ejectInterval);
			if (cancel.IsCancellationRequested || missile.disabled)
			{
				return;
			}
			if (detectedIndex >= detectedUnits.Count)
			{
				detectedIndex = 0;
			}
			if (detectedUnits[detectedIndex].LineOfSight(missile.transform.position, 1000f))
			{
				if (missile.IsServer)
				{
					Vector3 vector = ((Vector3.Dot(submunitions[targetsAssigned].transform.position - missile.transform.position, missile.transform.right) > 0f) ? missile.transform.right : (-missile.transform.right)) * ejectSpeed;
					NetworkSceneSingleton<Spawner>.i.SpawnMissile(submunitionType.weaponPrefab, submunitions[targetsAssigned].transform.position, missile.transform.rotation, missile.rb.velocity + vector, detectedUnits[detectedIndex], missile);
				}
				submunitions[targetsAssigned].SetActive(value: false);
				targetsAssigned++;
			}
			else
			{
				detectedUnits.RemoveAt(detectedIndex);
			}
			detectedIndex++;
		}
		await UniTask.WaitForSeconds(1);
		if (!cancel.IsCancellationRequested && !missile.disabled && missile.IsServer)
		{
			missile.Networkdisabled = true;
			Object.Destroy(missile.gameObject, 5f);
		}
	}

	public void TakeDamage(float pierceDamage, float blastDamage, float amountAffected, float fireDamage, float ImpactDamage, PersistentID dealerID)
	{
	}

	public void TakeShockwave(Vector3 origin, float overpressure, float blastPower)
	{
	}
}
