using UnityEngine;

public class SoftBodyRotor : MonoBehaviour, IDamageable
{
	private class MassPoint
	{
		public float mass;

		public Vector3 position;

		private Vector3 velocity;

		private Vector3 velocityPrev;

		private Vector3 frameForces;

		public bool anchoredToTransform;

		private Vector3 anchoredPosition;

		private Transform anchoredTransform;

		private Transform debugPointTransform;

		public MassPoint(float mass, Vector3 position, Transform anchoredTransform)
		{
			anchoredToTransform = anchoredTransform != null;
			this.mass = mass;
			this.position = position;
			if (anchoredToTransform)
			{
				this.anchoredTransform = anchoredTransform;
				anchoredPosition = anchoredTransform.InverseTransformPoint(position);
			}
			GameObject gameObject = Object.Instantiate(GameAssets.i.debugPoint, Datum.origin);
			debugPointTransform = gameObject.transform;
			debugPointTransform.localScale = Vector3.one * 0.15f;
		}

		public void MoveAnchoredTransform(float deltaTime)
		{
			Vector3 vector = anchoredTransform.TransformPoint(anchoredPosition);
			velocity = (vector - position) / deltaTime;
			position = vector;
			if (mass > 0f)
			{
				Vector3 vector2 = (velocity - velocityPrev) / deltaTime;
				velocityPrev = velocity;
				AddForce(vector2 * mass);
			}
		}

		public void AddForce(Vector3 force)
		{
			frameForces += force;
		}

		public void Simulate(float deltaTime, out ForceAndTorque forceAndTorque)
		{
			AddForce(mass * -9.81f * Vector3.up);
			forceAndTorque = default(ForceAndTorque);
			if (!anchoredToTransform)
			{
				velocity += frameForces * deltaTime / mass;
				position += velocity * deltaTime;
			}
			else
			{
				MoveAnchoredTransform(deltaTime);
				forceAndTorque = new ForceAndTorque(frameForces, position - anchoredTransform.position);
			}
			debugPointTransform.position = position;
			frameForces = Vector3.zero;
		}
	}

	private class Segment
	{
		private float restLength;

		private float lengthPrev;

		private float spring;

		private float damp;

		private MassPoint startMass;

		private MassPoint endMass;

		private Transform debugArrow;

		public Segment(MassPoint startMass, MassPoint endMass, float spring, float damp)
		{
			this.spring = spring;
			this.damp = damp;
			this.startMass = startMass;
			this.endMass = endMass;
			restLength = FastMath.Distance(startMass.position, endMass.position);
			lengthPrev = restLength;
			debugArrow = Object.Instantiate(GameAssets.i.debugArrowGreen, Datum.origin).transform;
			debugArrow.transform.localScale = new Vector3(0.2f, 0.2f, restLength);
			Vector3 forward = startMass.position - endMass.position;
			debugArrow.rotation = Quaternion.LookRotation(forward);
			debugArrow.position = startMass.position;
		}

		public void Simulate(float deltaTime)
		{
			Vector3 normalized = (endMass.position - startMass.position).normalized;
			float num = FastMath.Distance(startMass.position, endMass.position);
			float num2 = (num - lengthPrev) / deltaTime;
			float num3 = (num - restLength) * spring;
			float num4 = num2 * damp;
			lengthPrev = num;
			startMass.AddForce(normalized * (num3 + num4));
			endMass.AddForce(-normalized * (num3 + num4));
			debugArrow.SetPositionAndRotation(startMass.position, Quaternion.LookRotation(normalized));
			debugArrow.transform.localScale = new Vector3(0.2f, 0.2f, num);
		}
	}

	private MassPoint[] anchoredMassPoints;

	private MassPoint[] massPoints;

	private Segment[] segments;

	public RotorShaft rotorShaft;

	public Transform tip;

	[SerializeField]
	private Transform swashPlate;

	[SerializeField]
	private Aircraft aircraft;

	[SerializeField]
	private Mesh mesh_slow;

	[SerializeField]
	private Material material_slow;

	[SerializeField]
	private Mesh mesh_fast;

	[SerializeField]
	private Material material_fast;

	[SerializeField]
	private ArmorProperties armorProperties;

	private float hitpoints = 100f;

	private Vector3 centripetalForce;

	private GameObject vectorDebug1;

	private GameObject vectorDebug2;

	[SerializeField]
	private GameObject[] breakEffects;

	private float originalLength;

	private float length;

	private float originalMass;

	private float originalArea;

	[SerializeField]
	private float mass;

	[SerializeField]
	private float maxPitchRate;

	[SerializeField]
	private float flapStiffness;

	[SerializeField]
	private float flapDamping;

	[SerializeField]
	private float stiffnessAtSpeed;

	private int segmentsRemoved;

	private float bladePitch;

	private float swashPlatePitch;

	private float swashPlateRoll;

	private float swashPlateCollective;

	private float flapForce;

	private float flapSpeed;

	private float flapPosition;

	public float bladeArticulation;

	private Vector3 bladeTurnDirection;

	private ForceAndTorque forceAndTorque;

	private RaycastHit rotorHit;

	private Renderer rotorRenderer;

	private MeshFilter meshFilter;

	private float spawnTime;

	private bool broken;

	private byte index;

	private void Awake()
	{
		rotorRenderer = GetComponent<Renderer>();
		meshFilter = GetComponent<MeshFilter>();
		index = aircraft.RegisterDamageable(this);
		originalLength = Vector3.Distance(base.transform.position, tip.transform.position);
		originalMass = mass;
		length = originalLength;
		massPoints = new MassPoint[3];
		segments = new Segment[2];
		massPoints[0] = new MassPoint(25f, base.transform.position + base.transform.forward * 0.6f, swashPlate);
		massPoints[1] = new MassPoint(0f, base.transform.position + base.transform.forward * length, swashPlate);
		massPoints[2] = new MassPoint(25f, base.transform.position + base.transform.forward * length, null);
		segments[0] = new Segment(massPoints[0], massPoints[2], 400000f, 1000f);
		segments[1] = new Segment(massPoints[1], massPoints[2], 300f, 50f);
		if (PlayerSettings.debugVis)
		{
			vectorDebug1 = Object.Instantiate(GameAssets.i.debugArrow, tip.transform);
			vectorDebug2 = Object.Instantiate(GameAssets.i.debugArrow, tip.transform);
		}
		rotorShaft.GetUnitPart().onParentDetached += SwashRotor_OnRotorShaftDetach;
		aircraft.onEject += SwashRotor_OnEject;
	}

	public void TakeDamage(float pierceDamage, float blastDamage, float amountAffected, float fireDamage, float impactDamage, PersistentID damagedBy)
	{
		if (impactDamage > 0f)
		{
			if (aircraft.remoteSim)
			{
				_ = broken;
			}
			return;
		}
		float num = Mathf.Max(pierceDamage - armorProperties.pierceArmor, 0f) / armorProperties.pierceTolerance;
		float num2 = Mathf.Max(blastDamage - armorProperties.blastArmor, 0f) / armorProperties.blastTolerance;
		float num3 = Mathf.Max(fireDamage - armorProperties.fireArmor, 0f) / armorProperties.fireTolerance;
		if (!(num + num2 + num3 + impactDamage <= 0f))
		{
			aircraft.Damage(index, new DamageInfo(num, num2, num3, impactDamage));
		}
	}

	public void ApplyDamage(float pierceDamage, float blastDamage, float fireDamage, float impactDamage)
	{
		hitpoints -= pierceDamage + blastDamage + fireDamage + impactDamage;
		if (!(hitpoints > 0f))
		{
			rotorShaft.ReportDamage();
		}
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

	private void SwashRotor_OnEject()
	{
	}

	public void Simulate(float deltaTime, out ForceAndTorque forceAndTorque)
	{
		forceAndTorque = default(ForceAndTorque);
		MassPoint[] array = massPoints;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Simulate(deltaTime, out var additive);
			forceAndTorque.Add(additive);
		}
		Segment[] array2 = segments;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].Simulate(deltaTime);
		}
	}

	public Vector3 GetCentripetalForce(float angularVelocity)
	{
		float num = angularVelocity * length;
		return Vector3.Cross(bladeTurnDirection, swashPlate.up) * mass * 0.5f * (num * num / Mathf.Max(length, 0.1f));
	}

	private Vector3 SampleForce(float liftNumber, float stallAngle, float dragBase, float dragExponent, Vector3 airVelocity, float vrsFactor)
	{
		float sqrMagnitude = airVelocity.sqrMagnitude;
		Vector3 normalized = Vector3.Cross(airVelocity, -tip.right).normalized;
		float num = Mathf.Clamp(TargetCalc.GetAngleOnAxis(airVelocity, tip.forward, tip.right), -45f, 45f);
		float num2 = ((Mathf.Abs(num) < stallAngle) ? (num * liftNumber) : Mathf.Lerp(liftNumber * stallAngle * Mathf.Sign(num), 0f, (Mathf.Abs(num) - stallAngle) * 0.04f));
		float num3 = dragBase + dragExponent * num * num;
		Vector3 vector = num2 * sqrMagnitude * normalized;
		Vector3 vector2 = num3 * sqrMagnitude * -airVelocity.normalized;
		vector *= 1f - vrsFactor;
		return vector + vector2;
	}

	public Vector3 CalculateLift(ControlInputs controlInputs, float liftNumber, float stallAngle, float dragBase, float dragExponent, Rigidbody hubRB, float angularVelocity, Vector3 downdraft, float VRSFactor, out float shaftTorque)
	{
		Vector3 vector = hubRB.GetPointVelocity(tip.position) - ((NetworkSceneSingleton<LevelInfo>.i != null) ? NetworkSceneSingleton<LevelInfo>.i.GetWind(tip.GlobalPosition()) : Vector3.zero);
		vector += downdraft;
		Vector3 vector2 = Vector3.Cross(tip.right, swashPlate.up);
		if (spawnTime > 2f)
		{
			CheckCollisions(angularVelocity * length * vector2);
		}
		else
		{
			spawnTime += Time.fixedDeltaTime;
		}
		Vector3 zero = Vector3.zero;
		shaftTorque = 0f;
		for (float num = 0.35f; num < 1f; num += 0.3f)
		{
			float num2 = num * length;
			Vector3 vector3 = SampleForce(liftNumber, stallAngle, dragBase, dragExponent, vector + num2 * angularVelocity * vector2, VRSFactor);
			float num3 = Vector3.Dot(vector2, vector3);
			vector3 -= num3 * vector2;
			shaftTorque += num3 * num2;
			zero += vector3;
		}
		return zero;
	}

	private void SwashRotor_OnRotorShaftDetach(UnitPart part)
	{
		broken = true;
	}

	private void CheckCollisions(Vector3 tipVelocity)
	{
		bool flag = false;
		bool num = Physics.Linecast(base.transform.position, tip.position, out rotorHit, ~((int)PhysicsLayers.ExclusionZonesMask | (int)PhysicsLayers.IgnoreCollisionsMask));
		if (!num && !aircraft.remoteSim && tip.position.y < Datum.LocalSeaY && base.transform.position.y > Datum.LocalSeaY)
		{
			Plane plane = Datum.WaterPlane();
			Vector3 vector = Vector3.Normalize(tip.position - base.transform.position);
			Ray ray = new Ray(base.transform.position, vector);
			if (plane.Raycast(ray, out var enter) && enter < length)
			{
				flag = true;
				rotorHit = default(RaycastHit);
				rotorHit.distance = enter;
				rotorHit.point = base.transform.position + vector * enter;
				rotorHit.normal = Vector3.up;
			}
		}
		_ = tipVelocity.magnitude;
		_ = length;
		_ = rotorHit.distance;
		_ = num || flag;
	}
}
