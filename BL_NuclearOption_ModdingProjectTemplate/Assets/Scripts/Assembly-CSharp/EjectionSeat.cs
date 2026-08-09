using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class EjectionSeat : MonoBehaviour
{
	private const float RAIL_TIME = 1.5f;

	[SerializeField]
	private Transform nozzle;

	[SerializeField]
	private float mass;

	[SerializeField]
	private float thrust;

	[SerializeField]
	private float duration;

	private Rigidbody cockpitRB;

	private Rigidbody pilotRB;

	private Rigidbody detachedRB;

	private UnitPart cockpitPart;

	[SerializeField]
	private ParticleSystem[] ejectParticles;

	[SerializeField]
	private Collider seatCollider;

	[SerializeField]
	private AudioClip fireSound;

	private PilotDismounted pilotDismounted;

	private Vector3 cockpitOffset;

	private float railPosition;

	private bool firing;

	public bool IsOnEjectionRail
	{
		get
		{
			if (firing)
			{
				return railPosition < 1.5f;
			}
			return false;
		}
	}

	public void LinkToPilot(PilotDismounted pilotDismounted)
	{
		this.pilotDismounted = pilotDismounted;
		pilotRB = pilotDismounted.rb;
		pilotRB.mass += mass;
	}

	public void Fire(UnitPart unitPart)
	{
		cockpitPart = unitPart;
		cockpitRB = unitPart.rb;
		firing = true;
		cockpitOffset = unitPart.transform.InverseTransformPoint(pilotRB.transform.position);
		FireEffects();
		FirePhysics().Forget();
	}

	private async UniTask FirePhysics()
	{
		CancellationToken cancel = base.destroyCancellationToken;
		railPosition = 0f;
		float ejectTime = 0f;
		float speed = 0f;
		seatCollider.enabled = false;
		while (railPosition < 1.5f)
		{
			speed += thrust / pilotRB.mass * Time.fixedDeltaTime;
			Vector3 position = cockpitPart.transform.TransformPoint(cockpitOffset) + nozzle.transform.up * railPosition;
			Quaternion rotation = cockpitPart.transform.rotation;
			pilotRB.Move(position, rotation);
			pilotRB.angularVelocity = cockpitRB.angularVelocity;
			pilotRB.AddForce(nozzle.transform.up * thrust);
			railPosition += speed * Time.fixedDeltaTime;
			ejectTime += Time.fixedDeltaTime;
			await UniTask.Yield(PlayerLoopTiming.FixedUpdate);
			if (cancel.IsCancellationRequested)
			{
				return;
			}
		}
		seatCollider.enabled = true;
		pilotDismounted.SetCollidable(enabled: true);
		while (ejectTime < duration)
		{
			ejectTime += Time.fixedDeltaTime;
			pilotRB.AddTorque(0.3f * pilotRB.mass * Vector3.Cross(nozzle.transform.up, Vector3.up), ForceMode.Force);
			pilotRB.AddForce(Vector3.RotateTowards(nozzle.transform.up, Vector3.up, 0.5f, 0f) * thrust);
			await UniTask.Yield(PlayerLoopTiming.FixedUpdate);
			if (cancel.IsCancellationRequested)
			{
				break;
			}
		}
	}

	private void LateUpdate()
	{
		if (IsOnEjectionRail)
		{
			Vector3 position = cockpitPart.transform.TransformPoint(cockpitOffset) + nozzle.transform.up * railPosition;
			Quaternion rotation = cockpitPart.transform.rotation;
			pilotRB.transform.SetPositionAndRotation(position, rotation);
			pilotRB.Move(position, rotation);
		}
	}

	private void FireEffects()
	{
		AudioSource audioSource = base.gameObject.AddComponent<AudioSource>();
		audioSource.outputAudioMixerGroup = SoundManager.i.EffectsMixer;
		audioSource.bypassListenerEffects = true;
		audioSource.clip = fireSound;
		audioSource.volume = 2f;
		audioSource.dopplerLevel = 0f;
		audioSource.minDistance = 50f;
		audioSource.maxDistance = 1000f;
		audioSource.spatialBlend = 1f;
		audioSource.rolloffMode = AudioRolloffMode.Linear;
		audioSource.Play();
		Object.Destroy(audioSource, 10f);
		ParticleSystem[] array = ejectParticles;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Play();
		}
	}

	public void Detach()
	{
		if (!(detachedRB != null) && !(pilotRB == null))
		{
			seatCollider.enabled = false;
			base.transform.SetParent(null);
			pilotRB.mass -= mass;
			detachedRB = base.gameObject.AddComponent<Rigidbody>();
			detachedRB.mass = mass;
			detachedRB.drag = 0.05f;
			detachedRB.angularDrag = 0.05f;
			detachedRB.interpolation = RigidbodyInterpolation.Interpolate;
			detachedRB.velocity = pilotRB.velocity;
			detachedRB.angularVelocity = pilotRB.angularVelocity;
			Object.Destroy(base.gameObject, 20f);
		}
	}

	public void BailOut(Rigidbody pilotRB, Rigidbody cockpit)
	{
		pilotRB.mass -= mass;
		base.transform.SetParent(cockpit.transform);
	}
}
