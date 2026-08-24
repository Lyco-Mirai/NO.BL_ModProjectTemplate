using System;
using NuclearOption.Jobs;
using UnityEngine;

public class ControlSurface : MonoBehaviour
{
	[SerializeField]
	private float pitchRange;

	[SerializeField]
	private float rollRange;

	[SerializeField]
	private float yawRange;

	[SerializeField]
	private float brakeRange;

	[SerializeField]
	private UnitPart attachedSurface;

	[SerializeField]
	private GameObject visibleMesh;

	[SerializeField]
	private bool flap;

	[SerializeField]
	private float servoSpeed = 20f;

	[Header("Split Surface")]
	[SerializeField]
	private float splitDrag;

	[SerializeField]
	private Transform splitUpper;

	[SerializeField]
	private Transform splitLower;

	[SerializeField]
	private float maxSplit;

	[SerializeField]
	private float yawSplitFactor;

	[Header("Split Audio")]
	[SerializeField]
	private AudioClip splitSound;

	[SerializeField]
	[Range(0f, 2f)]
	private float splitVolumeMultiplier;

	[SerializeField]
	private float maxVolumeSpeed = 340f;

	[SerializeField]
	private float splitPitchMin = 0.5f;

	[SerializeField]
	private float splitPitchMax = 2f;

	private AudioSource splitSource;

	private bool locked;

	private Aircraft aircraft;

	private float splitAmount;

	private ControlInputs controlInputs;

	private PtrAllocation<ControlSurfaceFields> JobFields;

	private JobPart<ControlSurface, ControlSurfaceFields> JobPart;

	private void Awake()
	{
		_ = maxSplit;
		_ = 0f;
		if (attachedSurface.parentUnit != null && attachedSurface.parentUnit is Aircraft)
		{
			aircraft = attachedSurface.parentUnit as Aircraft;
			aircraft.onInitialize += ControlSurface_OnInitialize;
			controlInputs = aircraft.GetInputs();
		}
		JobPart = new JobPart<ControlSurface, ControlSurfaceFields>(this, GetOrCreateJobField());
		JobManager.Add(JobPart);
	}

	private void OnValidate()
	{
		if (maxSplit > 0f)
		{
			if (splitUpper == null)
			{
				Debug.LogError($"maxSplit was above zero but splitUpper was null on {this}");
			}
			if (splitLower == null)
			{
				Debug.LogError($"maxSplit was above zero but splitLower was null on {this}");
			}
		}
	}

	unsafe ~ControlSurface()
	{
		if (JobFields.ptr != null)
		{
			Console.WriteLine("[PtrAllocation] ControlSurface memory leaked.");
		}
	}

	private static void DisposeJobFields(ref PtrAllocation<ControlSurfaceFields> fields)
	{
		if (fields.IsCreated)
		{
			ref ControlSurfaceFields reference = ref fields.Ref();
			if (reference.visibleTransformLink.IsCreated)
			{
				reference.visibleTransformLink.RemoveRef();
			}
			if (reference.upperTransformLink.IsCreated)
			{
				reference.upperTransformLink.RemoveRef();
			}
			if (reference.lowerTransformLink.IsCreated)
			{
				reference.lowerTransformLink.RemoveRef();
			}
		}
		fields.Dispose();
	}

	private Ptr<ControlSurfaceFields> GetOrCreateJobField()
	{
		if (!JobFields.IsCreated)
		{
			JobsAllocator<ControlSurfaceFields>.Allocate(ref JobFields);
			ref ControlSurfaceFields reference = ref JobFields.Ref();
			reference.pitchRange = pitchRange;
			reference.rollRange = rollRange;
			reference.yawRange = yawRange;
			reference.brakeRange = brakeRange;
			reference.flap = flap;
			reference.servoSpeed = servoSpeed;
			reference.splitDrag = splitDrag;
			reference.maxSplit = maxSplit;
			reference.yawSplitFactor = yawSplitFactor;
			reference.restingRotation = visibleMesh.transform.localRotation;
			if (maxSplit > 0f)
			{
				reference.restingSplitRotation = splitUpper.transform.localRotation;
			}
		}
		return JobFields;
	}

	public void UpdateJobFields()
	{
		ref ControlSurfaceFields reference = ref JobFields.Ref();
		reference.IsDetached = attachedSurface.IsDetached();
		reference.gearState = aircraft.gearState;
		reference.controlInputs = new ControlInputsBurst
		{
			pitch = (locked ? 0f : controlInputs.pitch),
			roll = (locked ? 0f : controlInputs.roll),
			yaw = (locked ? 0f : controlInputs.yaw),
			throttle = (locked ? 0f : controlInputs.throttle),
			brake = (locked ? 0f : controlInputs.brake),
			customAxis1 = (locked ? 0f : controlInputs.customAxis1)
		};
	}

	public bool GetJobTransforms(out Transform liftTransform, out Transform upperTransform, out Transform lowerTransform)
	{
		liftTransform = visibleMesh.transform;
		if (maxSplit > 0f)
		{
			upperTransform = splitUpper;
			lowerTransform = splitLower;
			return true;
		}
		upperTransform = null;
		lowerTransform = null;
		return false;
	}

	public void ApplyJobFields()
	{
		if (JobFields.IsCreated)
		{
			splitAmount = JobFields.Ref().splitAmount;
		}
	}

	private void ControlSurface_OnInitialize()
	{
		if (aircraft.LocalSim && splitUpper != null && NetworkSceneSingleton<LevelInfo>.i != null)
		{
			aircraft.AddControlSurface(this);
		}
	}

	private void OnDestroy()
	{
		JobManager.Remove(ref JobPart);
		DisposeJobFields(ref JobFields);
	}

	public void SetLocked(bool locked)
	{
		this.locked = locked;
	}

	public void Aero()
	{
		if (splitAmount == 0f)
		{
			return;
		}
		if (splitSound != null && SceneSingleton<CameraStateManager>.i.followingUnit == aircraft)
		{
			if (splitSource == null)
			{
				splitSource = base.gameObject.AddComponent<AudioSource>();
				splitSource.outputAudioMixerGroup = SoundManager.i.EffectsMixer;
				splitSource.bypassListenerEffects = true;
				splitSource.clip = splitSound;
				splitSource.dopplerLevel = 0f;
				splitSource.minDistance = 20f;
				splitSource.maxDistance = 50f;
				splitSource.spatialBlend = 1f;
				splitSource.loop = true;
				splitSource.Play();
				aircraft.RegisterDopplerSound(splitSource);
			}
			if (splitAmount > 0.1f * maxSplit)
			{
				if (!splitSource.isPlaying)
				{
					splitSource.Play();
				}
				float num = Mathf.Clamp01((aircraft.speed - 5f) / maxVolumeSpeed);
				float num2 = Mathf.InverseLerp(0.1f, 1f, splitAmount / maxSplit);
				splitSource.volume = Mathf.Sqrt(num2 * num) * splitVolumeMultiplier;
				splitSource.pitch = Mathf.Lerp(splitPitchMin, splitPitchMax, num);
			}
			else if (splitSource.isPlaying)
			{
				splitSource.Stop();
			}
		}
		Vector3 vector = attachedSurface.rb.velocity - aircraft.GetWindVelocity();
		float num3 = -1f * splitAmount * splitDrag * aircraft.airDensity * vector.sqrMagnitude;
		Vector3 force = vector.normalized * num3;
		attachedSurface.rb.AddForce(force);
	}
}
