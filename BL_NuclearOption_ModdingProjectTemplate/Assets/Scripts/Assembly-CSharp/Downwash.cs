using System;
using UnityEngine;

public class Downwash : MonoBehaviour
{
	[Serializable]
	private class DownwashSource
	{
		[SerializeField]
		private GameObject thrustSourceObject;

		[SerializeField]
		private Transform castTransform;

		private IThrustSource thrustSource;

		[SerializeField]
		private float maxThrust;

		public void Initialize()
		{
			thrustSource = thrustSourceObject.GetComponent<IThrustSource>();
		}

		public void GetSurfaceContact(float range, float radarAlt, out Vector3 contactPoint, out Vector3 contactNormal, out bool hitWater, out bool hitGround, out float thrustRatio)
		{
			hitWater = false;
			hitGround = false;
			thrustRatio = thrustSource.GetThrust() / maxThrust;
			float num = range * thrustRatio;
			contactPoint = castTransform.position - castTransform.forward * num;
			contactNormal = Vector3.up;
			if (thrustRatio > 0.1f && radarAlt < num && castTransform.position.y > Datum.LocalSeaY)
			{
				if (Datum.WaterPlane().Raycast(new Ray(castTransform.position, -castTransform.forward), out var enter) && enter < num && enter > 0f)
				{
					contactPoint = castTransform.position - enter * castTransform.forward;
					hitWater = true;
				}
				if (Physics.Linecast(castTransform.position, contactPoint, out var hitInfo, (int)PhysicsLayers.StaticsMask | (int)PhysicsLayers.ShipsMask) && hitInfo.point.y > Datum.LocalSeaY - 0.1f)
				{
					contactPoint = hitInfo.point;
					contactNormal = hitInfo.normal;
					hitGround = hitInfo.collider.sharedMaterial == GameAssets.i.terrainMaterial;
					hitWater = false;
				}
			}
		}
	}

	[SerializeField]
	private Unit aircraft;

	[SerializeField]
	private ParticleSystemForceField forceField;

	[SerializeField]
	private DownwashSource[] downwashSources;

	[SerializeField]
	private Transform dustTransform;

	[SerializeField]
	private Transform waterSprayTransform;

	private ParticleSystem[] dustSystems;

	private ParticleSystem[] waterSystems;

	[SerializeField]
	private float downwashSpeed = 40f;

	[SerializeField]
	private float speedReducesRange = 1f;

	[SerializeField]
	private float downwashRange = 70f;

	private GlobalPosition positionTarget;

	private Vector3 normalTarget;

	private float forceFieldMaxStrength;

	private bool dustEffectPlaying;

	private bool waterEffectPlaying;

	private void Awake()
	{
		base.enabled = false;
		dustTransform.SetParent(Datum.origin);
		waterSprayTransform.SetParent(Datum.origin);
		waterSprayTransform.rotation = Quaternion.identity;
		if (forceField != null)
		{
			forceFieldMaxStrength = forceField.directionY.constant;
		}
		dustSystems = dustTransform.GetComponents<ParticleSystem>();
		waterSystems = waterSprayTransform.GetComponentsInChildren<ParticleSystem>();
		DownwashSource[] array = downwashSources;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Initialize();
		}
		this.StartSlowUpdateDelayed(0.5f, CheckSurface);
	}

	private void UpdateSystems(bool hitWater, bool hitGround)
	{
		base.enabled = hitGround || hitWater;
		if (hitGround)
		{
			if (!dustEffectPlaying)
			{
				dustEffectPlaying = true;
				dustTransform.position = positionTarget.ToLocalPosition();
				ParticleSystem[] array = dustSystems;
				for (int i = 0; i < array.Length; i++)
				{
					array[i].Play();
				}
			}
		}
		else if (dustEffectPlaying)
		{
			dustEffectPlaying = false;
			ParticleSystem[] array = dustSystems;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Stop();
			}
		}
		if (hitWater)
		{
			if (!waterEffectPlaying)
			{
				waterEffectPlaying = true;
				dustTransform.position = positionTarget.ToLocalPosition();
				ParticleSystem[] array = waterSystems;
				for (int i = 0; i < array.Length; i++)
				{
					array[i].Play();
				}
			}
		}
		else if (waterEffectPlaying)
		{
			waterEffectPlaying = false;
			ParticleSystem[] array = waterSystems;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Stop();
			}
		}
	}

	private void CheckSurface()
	{
		if (aircraft == null)
		{
			UnityEngine.Object.Destroy(dustTransform.gameObject);
			UnityEngine.Object.Destroy(waterSprayTransform.gameObject);
			return;
		}
		int num = 0;
		float num2 = 0f;
		Vector3 zero = Vector3.zero;
		bool flag = false;
		bool flag2 = false;
		float num3 = downwashRange - speedReducesRange * aircraft.speed;
		normalTarget = Vector3.zero;
		if (aircraft.radarAlt < num3)
		{
			DownwashSource[] array = downwashSources;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].GetSurfaceContact(num3, aircraft.radarAlt, out var contactPoint, out var contactNormal, out var hitWater, out var hitGround, out var thrustRatio);
				num2 += thrustRatio;
				if (hitWater || hitGround)
				{
					num++;
					zero += contactPoint;
					normalTarget += contactNormal;
					flag = hitWater || flag;
					flag2 = hitGround || flag2;
				}
			}
		}
		num2 *= (float)(1 / downwashSources.Length);
		if (forceField != null)
		{
			forceField.directionY = new ParticleSystem.MinMaxCurve(forceFieldMaxStrength * num2);
		}
		if (num > 0)
		{
			float num4 = 1f / (float)num;
			zero *= num4;
			normalTarget *= num4;
			positionTarget = zero.ToGlobalPosition();
		}
		UpdateSystems(flag, flag2);
	}

	private void Update()
	{
		if (!(aircraft == null))
		{
			float num = downwashSpeed / Mathf.Max(aircraft.radarAlt, 1f);
			positionTarget += new Vector3(aircraft.rb.velocity.x, 0f, aircraft.rb.velocity.z) * Time.deltaTime;
			dustTransform.SetPositionAndRotation(Vector3.Lerp(dustTransform.position, positionTarget.ToLocalPosition(), num * Time.deltaTime), Quaternion.LookRotation(Vector3.Lerp(dustTransform.forward, normalTarget, Time.deltaTime)));
			waterSprayTransform.position = new Vector3(dustTransform.position.x, Datum.LocalSeaY, dustTransform.position.z);
		}
	}
}
