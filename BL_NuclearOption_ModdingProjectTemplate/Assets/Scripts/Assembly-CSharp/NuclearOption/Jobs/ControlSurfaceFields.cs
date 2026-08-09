using UnityEngine;

namespace NuclearOption.Jobs
{
	public struct ControlSurfaceFields
	{
		public PtrRefCounter<IndexLink> visibleTransformLink;

		public PtrRefCounter<IndexLink> upperTransformLink;

		public PtrRefCounter<IndexLink> lowerTransformLink;

		public float pitchRange;

		public float rollRange;

		public float yawRange;

		public float brakeRange;

		public bool flap;

		public float servoSpeed;

		public float splitDrag;

		public float maxSplit;

		public float yawSplitFactor;

		public Quaternion restingRotation;

		public Quaternion restingSplitRotation;

		public bool IsDetached;

		public ControlInputsBurst controlInputs;

		public LandingGear.GearState gearState;

		public float currentPitch;

		public float currentRoll;

		public float currentYaw;

		public float splitAmount;
	}
}
