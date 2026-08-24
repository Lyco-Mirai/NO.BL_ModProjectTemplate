using System.Collections.Generic;
using Mirage.SocketLayer;
using NuclearOption.Networking;
using UnityEngine;

public class BulletSim : MonoBehaviour
{
	public class TracerView : MonoBehaviour
	{
		private Renderer rend;

		private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

		private static Dictionary<Color, Material> materialCache = new Dictionary<Color, Material>();

		private void Awake()
		{
			rend = GetComponent<Renderer>();
		}

		public void Setup(Vector3 position, Quaternion rotation, Vector3 localScale, Color color)
		{
			base.transform.position = position;
			base.transform.rotation = rotation;
			base.transform.localScale = localScale;
			if (!materialCache.TryGetValue(color, out var value))
			{
				value = new Material(rend.sharedMaterial);
				value.SetColor(EmissionColorId, color);
				materialCache[color] = value;
			}
			if (rend.sharedMaterial != value)
			{
				rend.sharedMaterial = value;
			}
		}
	}

	public class Bullet
	{
		public bool active;

		public bool impacted;

		public Vector3 velocity;

		public GlobalPosition position;

		public TracerView tracer;

		public float destructTimer;

		public bool explosive;

		public bool proximityFuse;

		public float reliability;

		public Unit target;

		public Bullet(Vector3 velocity, TracerView tracer, float destructTimer, bool proximityFuse, bool explosive, Unit target)
		{
			active = true;
			impacted = false;
			this.velocity = velocity;
			this.tracer = tracer;
			this.destructTimer = destructTimer;
			this.explosive = explosive;
			this.target = target;
			this.proximityFuse = proximityFuse;
			reliability = Random.value;
			if (target != null && target.definition.armorTier > 2f)
			{
				this.proximityFuse = false;
			}
		}

		public void TrajectoryTrace(Transform muzzle, WeaponInfo info, Unit owner, ParticleEffectManager.PrefabEffect[] impactEffects, float tracerSize, bool visualOnly, float deltaTime, bool hasCamera, GlobalPosition cameraPos, Vector3 cameraVelocity, float cameraFov, bool hasTargetPos, GlobalPosition targetPos)
		{
			Vector3 obj = ((muzzle != null) ? muzzle.position : position.ToLocalPosition());
			if (muzzle != null)
			{
				position = muzzle.position.ToGlobalPosition();
			}
			velocity.y -= 9.81f * deltaTime * info.gravMult;
			velocity -= velocity.magnitude * velocity * (info.dragCoef * deltaTime / info.muzzleVelocity);
			destructTimer -= deltaTime;
			GlobalPosition globalPosition = position + velocity * deltaTime;
			if (Physics.Linecast(obj, obj + velocity * 1.5f * deltaTime, out var hitInfo, ~(int)PhysicsLayers.ExclusionZonesMask))
			{
				impacted = true;
				position = hitInfo.point.ToGlobalPosition() - velocity.normalized * 0.1f;
				if (hitInfo.point.y < Datum.LocalSeaY || hitInfo.collider.sharedMaterial == GameAssets.i.WaterMaterial)
				{
					globalPosition.y = 0f;
					impactEffects[2]?.Play(position.ToLocalPosition(), Quaternion.identity);
					active = false;
					return;
				}
				IDamageable component = hitInfo.collider.gameObject.GetComponent<IDamageable>();
				Vector3 vector = velocity;
				ImpactType impactType = ((!(hitInfo.collider.sharedMaterial == GameAssets.i.terrainMaterial)) ? ImpactType.ArmorHits : ImpactType.GroundHits);
				impactEffects[(int)impactType]?.Play(hitInfo.point, Quaternion.LookRotation(Vector3.Reflect(velocity.normalized, hitInfo.normal) + hitInfo.normal, Vector3.up));
				if (component != null)
				{
					Unit unit = component.GetUnit();
					float num = info.pierceDamage * velocity.sqrMagnitude / (info.muzzleVelocity * info.muzzleVelocity);
					if (!visualOnly)
					{
						if (unit != null)
						{
							Vector3 relativePos = unit.transform.InverseTransformPoint(hitInfo.point - velocity.normalized * 0.05f);
							owner.RegisterHit(unit, relativePos, velocity, info);
							if (GameManager.IsLocalAircraft(owner))
							{
								SceneSingleton<CombatHUD>.i.DisplayHit(position, unit);
							}
						}
						else
						{
							component.TakeDamage(num, info.blastDamage, 1f, 0f, 0f, PersistentID.None);
						}
					}
					active = num < component.GetArmorProperties().pierceArmor;
				}
				if ((bool)tracer)
				{
					if (Mathf.Abs(Vector3.Dot(velocity.normalized, hitInfo.normal)) < Random.Range(0f, 0.5f))
					{
						vector = Vector3.Reflect(velocity, hitInfo.normal) * 0.3f + velocity.magnitude * 0.05f * Random.insideUnitSphere;
					}
					else
					{
						active = false;
					}
				}
				active = active && (bool)tracer && vector.sqrMagnitude > 625f;
				explosive = false;
				velocity = vector;
				return;
			}
			if (globalPosition.y < 0f)
			{
				impacted = true;
				position = new GlobalPosition(globalPosition.x, 0f, globalPosition.z);
				impactEffects[2]?.Play(position.ToLocalPosition(), Quaternion.identity);
				active = false;
				return;
			}
			if (tracer != null && hasCamera)
			{
				float num2 = Mathf.Sqrt(FastMath.Distance(position, cameraPos));
				float num3 = tracerSize * 0.45f + num2 * 0.002f * cameraFov;
				tracer.transform.position = position.ToLocalPosition();
				float magnitude = (velocity - cameraVelocity).magnitude;
				tracer.transform.localScale = new Vector3(num3, num3, magnitude * deltaTime);
				if (magnitude > 0.1f)
				{
					tracer.transform.rotation = Quaternion.LookRotation(velocity - cameraVelocity);
				}
			}
			if (proximityFuse && target != null && hasTargetPos && FastMath.InRange(targetPos, position, 100f))
			{
				if (reliability < 0.1f && FastMath.OutOfRange(position, owner.GlobalPosition(), 500f))
				{
					if (NetworkManagerNuclearOption.i.Server.Active)
					{
						DamageEffects.BlastFrag(info.blastDamage, position.ToLocalPosition(), owner.persistentID, PersistentID.None);
					}
					impactEffects[3]?.Play(position.ToLocalPosition(), Quaternion.LookRotation(velocity));
					active = false;
					return;
				}
				GlobalPosition globalPosition2 = position + velocity * deltaTime;
				Vector3 rhs = targetPos - globalPosition2;
				if (Vector3.Dot(velocity, rhs) < 0f)
				{
					GlobalPosition globalPosition3 = position + Vector3.Project(targetPos - position, velocity);
					globalPosition3 += Random.Range(-1f, 1f) * target.maxRadius * velocity.normalized;
					RaycastHit hitInfo2;
					bool flag = Physics.Linecast(position.ToLocalPosition(), globalPosition3.ToLocalPosition(), out hitInfo2, ~(int)PhysicsLayers.ExclusionZonesMask);
					position = (flag ? hitInfo2.point.ToGlobalPosition() : globalPosition3);
					if (NetworkManagerNuclearOption.i.Server.Active)
					{
						DamageEffects.BlastFrag(info.blastDamage, position.ToLocalPosition(), owner.persistentID, PersistentID.None);
					}
					impactEffects[3]?.Play(position.ToLocalPosition(), Quaternion.LookRotation(velocity));
					active = false;
					return;
				}
			}
			if (destructTimer <= 0f || velocity.sqrMagnitude < info.muzzleVelocity * info.muzzleVelocity * 0.02f)
			{
				if (explosive)
				{
					impactEffects[3]?.Play(position.ToLocalPosition(), Quaternion.LookRotation(velocity));
				}
				active = false;
			}
			else
			{
				position += velocity * deltaTime;
			}
		}

		public void Remove()
		{
			if (tracer != null)
			{
				ReleaseTracer(tracer);
				tracer = null;
			}
			target = null;
		}
	}

	private static Pool<TracerView> tracerPool;

	private ParticleEffectManager.PrefabEffect[] impactEffects;

	private Unit owner;

	private bool visualOnly;

	private Gun gun;

	private WeaponInfo weaponInfo;

	public Turret turret;

	private float noBulletsTimer;

	private float tracerSize;

	private GameObject debugVis;

	private List<Bullet> bullets = new List<Bullet>();

	private static Pool<TracerView> GetTracerPool()
	{
		if (tracerPool == null)
		{
			tracerPool = new Pool<TracerView>((Pool<TracerView> pool) => Object.Instantiate(GameAssets.i.tracer, Datum.origin).AddComponent<TracerView>(), 1000, 100000);
		}
		return tracerPool;
	}

	public static TracerView GetTracer()
	{
		Pool<TracerView> pool = GetTracerPool();
		TracerView tracerView;
		do
		{
			tracerView = pool.Take();
		}
		while (tracerView == null);
		tracerView.gameObject.SetActive(value: true);
		return tracerView;
	}

	public static void ReleaseTracer(TracerView tracer)
	{
		if (tracerPool != null)
		{
			tracer.gameObject.SetActive(value: false);
			if (!tracerPool.Put(tracer))
			{
				Object.Destroy(tracer.gameObject);
			}
		}
		else
		{
			Object.Destroy(tracer.gameObject);
		}
	}

	public static BulletSim Create(Unit owner, Gun gun, Turret turret)
	{
		BulletSim bulletSim = new GameObject().AddComponent<BulletSim>();
		bulletSim.gameObject.name = "bulletSim";
		bulletSim.gameObject.transform.SetParent(Datum.origin);
		bulletSim.owner = owner;
		bulletSim.gun = gun;
		bulletSim.weaponInfo = gun.info;
		bulletSim.turret = turret;
		bulletSim.visualOnly = (gun.ForceServerAuthority ? (!owner.IsServer) : owner.remoteSim);
		bulletSim.impactEffects = new ParticleEffectManager.PrefabEffect[4];
		for (int i = 0; i < 4; i++)
		{
			bulletSim.impactEffects[i] = SceneSingleton<ParticleEffectManager>.i.GetPrefabEffect(gun.ImpactEffectsPrefabs[i]);
		}
		if (GameAssets.i != null && !GameManager.IsHeadless)
		{
			GetTracerPool();
		}
		return bulletSim;
	}

	public void AddBullet(Transform muzzle, Vector3 inheritedVelocity, float spread, float destructTimer, bool tracer, float tracerSize, Color tracerColor, float timeOffset, Unit target)
	{
		this.tracerSize = tracerSize;
		TracerView tracerView = null;
		if (tracer && !GameManager.IsHeadless)
		{
			tracerView = GetTracer();
			tracerView.Setup(muzzle.position, muzzle.rotation, new Vector3(1f, 1f, inheritedVelocity.magnitude + gun.info.muzzleVelocity * 0.0083f), tracerColor);
		}
		if (NetworkManagerNuclearOption.i.Server.Active)
		{
			HitValidator.LogFiring(owner.persistentID, muzzle.transform.position - Datum.origin.position, inheritedVelocity);
		}
		bool proximityFuse = target != null && target.definition.armorTier < 2f;
		Vector3 vector = Random.insideUnitSphere.normalized * Random.Range(0f, spread * 1.2f);
		Bullet bullet = new Bullet(inheritedVelocity + vector, tracerView, destructTimer, proximityFuse, weaponInfo.blastDamage > 0f, target);
		bullets.Add(bullet);
		bool flag = SceneSingleton<CameraStateManager>.i != null;
		GlobalPosition cameraPos = (flag ? SceneSingleton<CameraStateManager>.i.transform.position.ToGlobalPosition() : default(GlobalPosition));
		Vector3 cameraVelocity = (flag ? SceneSingleton<CameraStateManager>.i.cameraVelocity : default(Vector3));
		Camera main = Camera.main;
		float cameraFov = ((flag && main != null) ? main.fieldOfView : 60f);
		bool flag2 = target != null;
		GlobalPosition targetPos = (flag2 ? target.GlobalPosition() : default(GlobalPosition));
		bullet.TrajectoryTrace(muzzle, weaponInfo, owner, impactEffects, tracerSize, visualOnly, Time.fixedDeltaTime + timeOffset, flag, cameraPos, cameraVelocity, cameraFov, flag2, targetPos);
		if (turret != null)
		{
			turret.SetObservedBullet(bullet);
		}
	}

	private void FixedUpdate()
	{
		if (bullets.Count > 0)
		{
			noBulletsTimer = 0f;
			bool flag = SceneSingleton<CameraStateManager>.i != null;
			GlobalPosition cameraPos = (flag ? SceneSingleton<CameraStateManager>.i.transform.position.ToGlobalPosition() : default(GlobalPosition));
			Vector3 cameraVelocity = (flag ? SceneSingleton<CameraStateManager>.i.cameraVelocity : default(Vector3));
			Camera main = Camera.main;
			float cameraFov = ((flag && main != null) ? main.fieldOfView : 60f);
			Unit unit = null;
			GlobalPosition targetPos = default(GlobalPosition);
			for (int num = bullets.Count - 1; num >= 0; num--)
			{
				Bullet bullet = bullets[num];
				if (bullet.active)
				{
					bool hasTargetPos = false;
					if (bullet.target != null)
					{
						if (bullet.target != unit)
						{
							unit = bullet.target;
							targetPos = bullet.target.GlobalPosition();
						}
						hasTargetPos = true;
					}
					bullet.TrajectoryTrace(null, weaponInfo, owner, impactEffects, tracerSize, visualOnly, Time.fixedDeltaTime, flag, cameraPos, cameraVelocity, cameraFov, hasTargetPos, targetPos);
				}
				else
				{
					bullet.Remove();
					bullets.RemoveAt(num);
				}
			}
		}
		else
		{
			noBulletsTimer += Time.fixedDeltaTime;
			if (noBulletsTimer > 25f)
			{
				Object.Destroy(base.gameObject);
			}
		}
	}

	private void Update()
	{
		if (!(Time.timeScale < 1f))
		{
			return;
		}
		for (int num = bullets.Count - 1; num >= 0; num--)
		{
			if (bullets[num].tracer != null)
			{
				bullets[num].tracer.transform.position += bullets[num].velocity * Time.deltaTime;
			}
		}
	}
}
