using UnityEngine;

namespace NuclearOption.Jobs
{
	public struct AeroPartFields
	{
		public PtrRefCounter<IndexLink> liftTransformIndex;

		public PtrRefCounter<IndexLink> otherTransformIndex;

		public Vector3 centerOfLift;

		public int airfoilID;

		public float wingEffectiveness;

		public float buoyancy;

		public float angularDrag;

		public Vector3 collisionSize;

		public float airflowChanneling;

		public float lastSplashTime;

		public Vector3 velocity;

		public float mass;

		public float submergedAmount;

		public float wingArea;

		public float dragArea;

		public Vector3 force;

		public Vector3 torque;

		public JobForceType hasForce;

		public bool angularDragChanged;

		public bool splashed;
	}
}
