using System;
using System.Collections.Generic;
using UnityEngine;

public class FragmentManager : MonoBehaviour
{
	[Serializable]
	private class Fragment
	{
		public Rigidbody rb;

		[NonSerialized]
		public GameObject gameObject;
	}

	[Serializable]
	private class CollapseDust
	{
		public Transform emitTransform;

		[SerializeField]
		private string effectType;

		[SerializeField]
		private Rigidbody rb;

		[SerializeField]
		private float duration;

		[SerializeField]
		private float flatRadius;

		[SerializeField]
		private float size;

		[SerializeField]
		private float life;

		[SerializeField]
		private float emitSpeed;

		[SerializeField]
		private float emitInterval;

		private float lastEmitted;

		private int effectID = -1;

		public bool Emit(float time)
		{
			if (time > duration)
			{
				return false;
			}
			if (Time.timeSinceLevelLoad - lastEmitted < emitInterval)
			{
				return true;
			}
			lastEmitted = Time.timeSinceLevelLoad;
			emitTransform.GetPositionAndRotation(out var position, out var rotation);
			Vector3 vector = rotation * Vector3.right;
			Vector3 vector2 = rotation * Vector3.forward;
			Vector3 vector3 = ((rb != null) ? rb.GetPointVelocity(position) : Vector3.zero);
			Vector3 vector4 = emitSpeed * UnityEngine.Random.insideUnitSphere;
			vector4.y = Mathf.Max(vector4.y, 0f);
			Vector2 vector5 = UnityEngine.Random.insideUnitCircle * flatRadius;
			if (effectID == -1 && SceneSingleton<ParticleEffectManager>.i != null)
			{
				effectID = SceneSingleton<ParticleEffectManager>.i.GetSystemID(effectType);
			}
			SceneSingleton<ParticleEffectManager>.i.EmitParticles(effectID, 1, (position + vector * vector5.x + vector2 * vector5.y).ToGlobalPosition(), vector3 + vector4, 0f, life, 0.3f, size, 0.3f, 0.3f, 1f, 0.2f);
			return true;
		}
	}

	[SerializeField]
	private float ejectionSpeed;

	[SerializeField]
	private Transform mainDebris;

	[SerializeField]
	private Transform castPoint;

	[SerializeField]
	private Transform mainCollapser;

	[SerializeField]
	private List<Fragment> fragments;

	[SerializeField]
	private List<CollapseDust> collapseDust = new List<CollapseDust>();

	[SerializeField]
	private float mainDebrisAppearanceTime;

	[SerializeField]
	private float mainDebrisVerticalOffset;

	[SerializeField]
	private float mainDebrisRiseAmount;

	[SerializeField]
	private float toppleSpeed;

	private float time;

	private GlobalPosition mainDebrisTarget;

	private GlobalPosition mainDebrisOrigin;

	private Vector3 collapserVelocity;

	private Vector3 toppleAxis;

	[Tooltip("in seconds")]
	[SerializeField]
	private float cullDelay = 10f;

	private void OnEnable()
	{
		for (int i = 0; i < fragments.Count; i++)
		{
			fragments[i].rb.transform.SetParent(null);
		}
		toppleAxis = UnityEngine.Random.insideUnitSphere;
		toppleAxis.y = 0f;
		toppleAxis = toppleAxis.normalized;
	}

	private void Start()
	{
		for (int i = 0; i < fragments.Count; i++)
		{
			Vector3 vector = UnityEngine.Random.insideUnitSphere * ejectionSpeed;
			fragments[i].rb.AddForce(vector, ForceMode.VelocityChange);
			fragments[i].rb.AddTorque(vector * 0.1f, ForceMode.VelocityChange);
			fragments[i].gameObject = fragments[i].rb.gameObject;
			DebrisManager.RegisterDebris(fragments[i].gameObject);
		}
		if (mainDebris != null)
		{
			if (castPoint != null)
			{
				if (Physics.Linecast(castPoint.position, castPoint.position - Vector3.up * 1000f, out var hitInfo, PhysicsLayers.StaticsMask))
				{
					mainDebrisTarget = hitInfo.point.ToGlobalPosition() + Vector3.up * mainDebrisVerticalOffset;
					mainDebris.rotation = Quaternion.LookRotation(base.transform.forward, hitInfo.normal);
					mainDebrisOrigin = mainDebrisTarget - mainDebris.up * mainDebrisRiseAmount;
					mainDebris.position = mainDebrisOrigin.ToLocalPosition();
				}
			}
			else
			{
				mainDebrisTarget = mainDebris.position.ToGlobalPosition();
				mainDebrisOrigin = mainDebrisTarget - Vector3.up * mainDebrisRiseAmount;
				mainDebris.position = mainDebrisOrigin.ToLocalPosition();
			}
		}
		this.StartSlowUpdateDelayed(cullDelay, 1f, FragmentCheck);
	}

	private void OnDestroy()
	{
		if (castPoint != null)
		{
			UnityEngine.Object.Destroy(castPoint.gameObject);
		}
	}

	private void Update()
	{
		if (time > mainDebrisAppearanceTime && collapseDust.Count == 0)
		{
			base.enabled = false;
		}
		time += Time.deltaTime;
		if (mainDebris != null && time < mainDebrisAppearanceTime)
		{
			mainDebris.transform.position = Vector3.Lerp(mainDebrisOrigin.ToLocalPosition(), mainDebrisTarget.ToLocalPosition(), time / mainDebrisAppearanceTime);
		}
		if (mainCollapser != null)
		{
			collapserVelocity -= Vector3.up * 9f * Time.deltaTime;
			mainCollapser.transform.position += collapserVelocity * Time.deltaTime;
			mainCollapser.transform.Rotate(toppleAxis * toppleSpeed * Time.deltaTime, Space.World);
		}
		for (int num = collapseDust.Count - 1; num >= 0; num--)
		{
			if (collapseDust[num].emitTransform == null)
			{
				collapseDust.RemoveAt(num);
			}
			else if (!collapseDust[num].Emit(time))
			{
				if (collapseDust[num].emitTransform.gameObject != base.gameObject)
				{
					UnityEngine.Object.Destroy(collapseDust[num].emitTransform.gameObject);
				}
				collapseDust.RemoveAt(num);
			}
		}
	}

	public List<GameObject> GetWreckageObjects()
	{
		List<GameObject> list = new List<GameObject>();
		for (int i = 0; i < fragments.Count; i++)
		{
			list.Add(fragments[i].rb.gameObject);
		}
		return list;
	}

	public void ApplyExplosionForce(Building.RecentExplosion explosion)
	{
		float num = Mathf.Pow(explosion.yield, 0.3333f);
		foreach (Fragment fragment in fragments)
		{
			Vector3 vector = fragment.rb.transform.position.ToGlobalPosition() - explosion.globalPosition;
			float num2 = Mathf.Max(vector.magnitude / num, 1f);
			float a = 25000f / (num2 * num2 * num2);
			Vector3 extents = fragment.rb.GetComponentInChildren<Collider>().bounds.extents;
			float num3 = Mathf.Pow((extents.x + extents.y + extents.z) / 3f, 2f);
			a = Mathf.Min(a, num * 250f);
			float num4 = num3 * a * 25f;
			num4 = Mathf.Min(num4, explosion.yield * 200f, 60f * fragment.rb.mass);
			fragment.rb.AddForce(vector.normalized * num4, ForceMode.Impulse);
		}
	}

	private void FragmentCheck()
	{
		for (int num = fragments.Count - 1; num >= 0; num--)
		{
			Fragment fragment = fragments[num];
			if (fragment.rb == null)
			{
				fragments.RemoveAt(num);
			}
			else if (fragment.rb.velocity.sqrMagnitude < 1f || fragment.rb.position.y < Datum.LocalSeaY - 100f)
			{
				UnityEngine.Object.Destroy(fragment.rb);
				fragments.RemoveAt(num);
			}
		}
		if (fragments.Count == 0 && collapseDust.Count == 0)
		{
			UnityEngine.Object.Destroy(this);
		}
	}
}
