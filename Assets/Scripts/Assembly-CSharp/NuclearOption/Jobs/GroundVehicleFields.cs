using System.Runtime.CompilerServices;
using UnityEngine;

namespace NuclearOption.Jobs
{
	public struct GroundVehicleFields
	{
		public float maxRadius;

		public float acceleration;

		public float mass;

		public bool mobile;

		public float topSpeedOnroad;

		public float topSpeedOffroad;

		public float suspensionTravel;

		public float dampingRate;

		public float springRate;

		public float frictionCoef;

		public float inertiaTensor;

		public float speed;

		public float stationaryTime;

		public Vector3 velocity;

		public Vector3 angularVelocity;

		public bool monoBehaviourEnabled;

		public bool unitDisabled;

		public bool unitWasDisabled;

		public bool underwater;

		public SteeringInfo? steeringInfoNullable;

		public PtrList<ObstaclePosition> ObstaclesArray;

		public bool DEBUG_VIS;

		public GroundVehicle.VehicleInputs inputs;

		public SampleGroundResult sampleGroundResult;

		public float engineOutput;

		public float bulldozeTimer;

		public float reverseTimer;

		public float stuckTimer;

		public Plane surfacePlane;

		private bool addForce;

		private bool addTorque;

		private Vector3 force;

		private Vector3 torque;

		public bool stationary;

		public float radarAlt;

		public bool disabled
		{
			get
			{
				if (!unitDisabled)
				{
					return underwater;
				}
				return true;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ResetForceResults()
		{
			addForce = false;
			addTorque = false;
			force = default(Vector3);
			torque = default(Vector3);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void AddForce(Vector3 force)
		{
			addForce = true;
			this.force += force;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void AddTorque(Vector3 torque)
		{
			addTorque = true;
			this.torque += torque;
		}

		public void ApplyForce(Rigidbody rigidbody)
		{
			if (addForce)
			{
				rigidbody.AddForce(force);
			}
			if (addTorque)
			{
				rigidbody.AddTorque(torque, ForceMode.VelocityChange);
			}
		}
	}
}
