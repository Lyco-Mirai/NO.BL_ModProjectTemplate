using System;
using UnityEngine;

namespace NuclearOption.NetworkTransforms
{
	[Serializable]
	public class NetworkPIDSmoother
	{
		[SerializeField]
		private Vector3 posPID;

		[SerializeField]
		private Vector3 rotPID;

		[SerializeField]
		private float positionTeleportThreshold = 50f;

		[SerializeField]
		private float rotationTeleportThreshold = 20f;

		private PID xPositionPID;

		private PID yPositionPID;

		private PID zPositionPID;

		private PID xRotationPID;

		private PID yRotationPID;

		private PID zRotationPID;

		public void Initialize(Rigidbody rb)
		{
			rb.useGravity = false;
			xPositionPID = new PID(posPID);
			yPositionPID = new PID(posPID);
			zPositionPID = new PID(posPID);
			xRotationPID = new PID(rotPID);
			yRotationPID = new PID(rotPID);
			zRotationPID = new PID(rotPID);
		}

		public void SmoothRB(Rigidbody rb, NetworkTransformBase.ViewSnapshot snapshot)
		{
			float deltaTime = Mathf.Clamp(Time.deltaTime, 0.001f, 0.2f);
			Vector3 vector = snapshot.Position - rb.position;
			if (vector.sqrMagnitude > positionTeleportThreshold * positionTeleportThreshold)
			{
				rb.MovePosition(snapshot.Position);
				rb.velocity = snapshot.Velocity;
			}
			else
			{
				rb.AddForce(new Vector3
				{
					x = xPositionPID.GetOutput(vector.x, deltaTime),
					y = yPositionPID.GetOutput(vector.y, deltaTime),
					z = zPositionPID.GetOutput(vector.z, deltaTime)
				}, ForceMode.Acceleration);
			}
			Vector3 other = snapshot.Rotation * Vector3.forward;
			Vector3 other2 = snapshot.Rotation * Vector3.up;
			Vector3 vector2 = new Vector3
			{
				x = TargetCalc.GetAngleOnAxis(rb.transform.forward, other, rb.transform.right),
				y = TargetCalc.GetAngleOnAxis(rb.transform.forward, other, rb.transform.up),
				z = TargetCalc.GetAngleOnAxis(rb.transform.up, other2, rb.transform.forward)
			};
			if (vector2.sqrMagnitude > rotationTeleportThreshold * rotationTeleportThreshold)
			{
				rb.MoveRotation(snapshot.Rotation);
				rb.angularVelocity = Vector3.zero;
				return;
			}
			rb.AddRelativeTorque(new Vector3
			{
				x = xRotationPID.GetOutput(vector2.x, deltaTime),
				y = yRotationPID.GetOutput(vector2.y, deltaTime),
				z = zRotationPID.GetOutput(vector2.z, deltaTime)
			} * rb.mass);
		}
	}
}
