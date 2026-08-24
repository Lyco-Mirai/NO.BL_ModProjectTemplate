using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class CargoRamp : BayDoor
{
	private enum RampState
	{
		opening = 0,
		open = 1,
		closing = 2
	}

	[Serializable]
	private struct Strut
	{
		[SerializeField]
		private Transform transform;

		[SerializeField]
		private Transform target;

		[SerializeField]
		private float maxLength;

		public void Animate()
		{
			transform.LookAt(target);
			float num = FastMath.Distance(target.position, transform.position);
			if (num > maxLength)
			{
				Remove();
			}
			transform.localScale = new Vector3(1f, 1f, num);
		}

		public void Remove()
		{
			transform.gameObject.GetComponent<Renderer>().enabled = false;
		}
	}

	private RampState rampState = RampState.closing;

	[SerializeField]
	private AudioClip openStopSound;

	[SerializeField]
	private AudioClip closeStopSound;

	[SerializeField]
	private AeroPart rampPart;

	[SerializeField]
	private AeroPart attachedPart;

	[SerializeField]
	private AeroPart latchPart;

	[SerializeField]
	private float hingeSpring;

	[SerializeField]
	private float hingeDamp;

	[SerializeField]
	private float hingeStrength;

	[SerializeField]
	private Strut[] struts;

	[SerializeField]
	private Transform rampLatch;

	[SerializeField]
	private Transform fuselageLatch;

	[SerializeField]
	private Transform rampSurfaceNormal;

	[SerializeField]
	private Renderer[] rampLightingRenderers;

	[SerializeField]
	private Light[] rampLights;

	private Joint latchJoint;

	private float defaultAngle;

	private float currentAngle;

	protected override void Awake()
	{
		rampPart.parentUnit.onInitialize += CargoRamp_OnSpawned;
	}

	private void Start()
	{
		if (!rampPart.parentUnit.networked)
		{
			CargoRamp_OnSpawned();
		}
	}

	private async UniTask EnableLatch(bool report)
	{
		Renderer[] array = rampLightingRenderers;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].enabled = false;
		}
		Light[] array2 = rampLights;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].enabled = false;
		}
		if (rampPart.parentUnit.LocalSim || !rampPart.parentUnit.networked)
		{
			await UniTask.WaitForFixedUpdate();
			if (report && GameManager.IsLocalAircraft(rampPart.parentUnit))
			{
				SceneSingleton<AircraftActionsReport>.i.ReportText("Locking Cargo Door", 3f);
			}
			latchJoint = rampPart.gameObject.AddComponent<FixedJoint>();
			latchJoint.connectedBody = latchPart.rb;
			latchJoint.breakForce = hingeStrength * 0.5f;
			latchJoint.breakTorque = hingeStrength * 0.5f;
			latchJoint.enableCollision = false;
		}
	}

	public bool IsOpen()
	{
		return Vector3.Dot(rampSurfaceNormal.forward, attachedPart.transform.up) > 0f;
	}

	private void DisableLatch()
	{
		Renderer[] array = rampLightingRenderers;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].enabled = true;
		}
		Light[] array2 = rampLights;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].enabled = true;
		}
		if ((rampPart.parentUnit.LocalSim || !rampPart.parentUnit.networked) && latchJoint != null)
		{
			if (GameManager.IsLocalAircraft(rampPart.parentUnit))
			{
				SceneSingleton<AircraftActionsReport>.i.ReportText("Unlocking Cargo Door", 3f);
			}
			UnityEngine.Object.Destroy(latchJoint);
		}
	}

	private void CargoRamp_OnSpawned()
	{
		EnableLatch(report: false).Forget();
		rampPart.SetHingeJoint(0, attachedPart, hingeSpring, hingeDamp, currentAngle, hingeStrength, defaultAngle, Vector3.right);
		Strut[] array = struts;
		foreach (Strut strut in array)
		{
			strut.Animate();
		}
		rampPart.onPartDetached += CargoRamp_OnDetached;
		rampPart.parentUnit.onInitialize -= CargoRamp_OnSpawned;
		base.enabled = false;
	}

	public override void OpenDoor(float duration)
	{
		DisableLatch();
		base.enabled = true;
		openTimer = duration;
		rampState = RampState.opening;
	}

	private void Opening()
	{
		currentAngle += Mathf.Clamp(hingeAngle - currentAngle, (0f - openSpeed) * Time.fixedDeltaTime, openSpeed * Time.fixedDeltaTime);
		rampPart.SetHingeJoint(0, attachedPart, hingeSpring, hingeDamp, currentAngle, hingeStrength, defaultAngle, Vector3.right);
		if (currentAngle == hingeAngle)
		{
			rampState = RampState.open;
			if (doorAudioSource != null)
			{
				doorAudioSource.Stop();
				doorAudioSource.clip = openStopSound;
				doorAudioSource.Play();
			}
		}
		else if (doorAudioSource != null && doorAudioSource.clip != openStartSound)
		{
			doorAudioSource.Stop();
			doorAudioSource.clip = openStartSound;
			doorAudioSource.Play();
		}
	}

	private void Open()
	{
		openTimer -= Time.fixedDeltaTime;
		if (openTimer <= 0f)
		{
			rampState = RampState.closing;
		}
	}

	private void CargoRamp_OnDetached(UnitPart unitPart)
	{
		Strut[] array = struts;
		foreach (Strut strut in array)
		{
			strut.Remove();
		}
		DisableLatch();
		Renderer[] array2 = rampLightingRenderers;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].enabled = false;
		}
		Light[] array3 = rampLights;
		for (int i = 0; i < array3.Length; i++)
		{
			array3[i].enabled = false;
		}
		if (doorAudioSource != null)
		{
			doorAudioSource.Stop();
		}
		UnityEngine.Object.Destroy(this);
	}

	private void Closing()
	{
		currentAngle += Mathf.Clamp(2f - currentAngle, (0f - closeSpeed) * Time.fixedDeltaTime, closeSpeed * Time.fixedDeltaTime);
		rampPart.SetHingeJoint(0, attachedPart, hingeSpring, hingeDamp, currentAngle, hingeStrength, defaultAngle, Vector3.right);
		if (Vector3.Dot(rampLatch.position - fuselageLatch.position, fuselageLatch.forward) > 0f && !rampPart.IsDetached() && !latchPart.IsDetached())
		{
			if (doorAudioSource != null)
			{
				doorAudioSource.Stop();
				doorAudioSource.clip = closeStopSound;
				doorAudioSource.Play();
			}
			EnableLatch(report: true).Forget();
			base.enabled = false;
		}
		else if (doorAudioSource != null && doorAudioSource.clip != closeStartSound)
		{
			doorAudioSource.Stop();
			doorAudioSource.clip = closeStartSound;
			doorAudioSource.Play();
		}
	}

	protected override void Update()
	{
		Strut[] array = struts;
		foreach (Strut strut in array)
		{
			strut.Animate();
		}
		switch (rampState)
		{
		case RampState.opening:
			Opening();
			break;
		case RampState.open:
			Open();
			break;
		case RampState.closing:
			Closing();
			break;
		}
	}
}
