using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class Canopy : MonoBehaviour
{
	[Serializable]
	private class CanopyHinge
	{
		[SerializeField]
		private Transform transform;

		[SerializeField]
		private float hingeAngle;

		public void Animate(float openAmount)
		{
			transform.localEulerAngles = Vector3.right * hingeAngle * openAmount;
		}
	}

	public Transform ejectionTransform;

	public AeroPart attachedPart;

	public float mass;

	public Collider ejectionCollider;

	[SerializeField]
	private Vector3 ejectionForce;

	[SerializeField]
	private Vector3 forcePosition;

	private Rigidbody rb;

	[SerializeField]
	private CanopyHinge[] canopyHinges;

	[SerializeField]
	private AudioClip ejectSound;

	[SerializeField]
	private float fireTime;

	[SerializeField]
	private float openSpeed;

	[SerializeField]
	private float glassDamageThreshold;

	[SerializeField]
	private float glassDamageLimit;

	[SerializeField]
	private Renderer[] glassRenderers;

	private float openAmount;

	private bool firing;

	private bool opening;

	private void Awake()
	{
		base.enabled = false;
		if (attachedPart.parentUnit is Aircraft aircraft)
		{
			aircraft.onInitialize += Canopy_OnInitialize;
		}
	}

	public void OpenHinges()
	{
		base.enabled = true;
		opening = true;
	}

	private void Canopy_OnInitialize()
	{
		if (GameManager.IsLocalAircraft(attachedPart.parentUnit))
		{
			attachedPart.onApplyDamage += Canopy_OnApplyDamage;
		}
	}

	private void Canopy_OnApplyDamage(UnitPart.OnApplyDamage e)
	{
		float f = 1f - Mathf.Clamp01((e.hitPoints - glassDamageLimit) / (glassDamageThreshold - glassDamageLimit));
		for (int i = 0; i < glassRenderers.Length; i++)
		{
			if (glassRenderers[i] != null)
			{
				glassRenderers[i].material.SetFloat("_Cracked_Amount", Mathf.Sqrt(f));
			}
		}
	}

	private async UniTask EjectionSequence()
	{
		await UniTask.Yield(PlayerLoopTiming.FixedUpdate);
		Rigidbody rigidbody = attachedPart.rb;
		Vector3 position = rigidbody.transform.InverseTransformPoint(base.transform.position);
		if (!firing && !(ejectionTransform == null))
		{
			firing = true;
			if (attachedPart.transform == ejectionTransform)
			{
				attachedPart.BreakAllJoints();
				attachedPart.CreateRB(rigidbody.velocity, attachedPart.transform.position);
				rb = attachedPart.rb;
				rigidbody.drag = 0.1f;
			}
			else
			{
				ejectionTransform.SetParent(null);
				ejectionCollider.enabled = true;
				rb = ejectionTransform.gameObject.AddComponent<Rigidbody>();
				rb.mass = mass;
				rb.velocity = rigidbody.GetPointVelocity(base.transform.position);
				rb.interpolation = RigidbodyInterpolation.Interpolate;
				rb.MovePosition(rigidbody.transform.TransformPoint(position));
				base.transform.position = rigidbody.transform.TransformPoint(position);
			}
			rb.angularDrag = 0.01f;
			rb.drag = 0.1f;
			AudioSource audioSource = ejectionTransform.gameObject.AddComponent<AudioSource>();
			audioSource.outputAudioMixerGroup = SoundManager.i.EffectsMixer;
			audioSource.bypassListenerEffects = true;
			audioSource.clip = ejectSound;
			audioSource.volume = 2f;
			audioSource.dopplerLevel = 0f;
			audioSource.minDistance = 50f;
			audioSource.maxDistance = 1000f;
			audioSource.spatialBlend = 1f;
			audioSource.rolloffMode = AudioRolloffMode.Linear;
			audioSource.Play();
			if (ejectionTransform.GetComponent<UnitPart>() == null)
			{
				NetworkSceneSingleton<Spawner>.i.DestroyLocal(ejectionTransform.gameObject, 10f);
			}
			base.enabled = true;
		}
	}

	public void Eject()
	{
		EjectionSequence().Forget();
	}

	private void FixedUpdate()
	{
		if (firing)
		{
			if (fireTime <= 0f)
			{
				base.enabled = false;
			}
			rb.AddForceAtPosition(rb.transform.forward * ejectionForce.z + rb.transform.up * ejectionForce.y, rb.transform.position + forcePosition);
			fireTime -= Time.deltaTime;
		}
	}

	private void Update()
	{
		if (opening)
		{
			if (openAmount >= 1f)
			{
				base.enabled = false;
			}
			openAmount += openSpeed * Time.deltaTime;
			for (int i = 0; i < canopyHinges.Length; i++)
			{
				canopyHinges[i].Animate(openAmount);
			}
		}
	}
}
