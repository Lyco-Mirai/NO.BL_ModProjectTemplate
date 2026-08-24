using UnityEngine;

public struct ForceAndTorque
{
	public Vector3 force;

	public Vector3 torque;

	public ForceAndTorque(Vector3 force, Vector3 offset)
	{
		this.force = force;
		torque = Vector3.Cross(force, -offset);
	}

	public ForceAndTorque(Vector3 force, Vector3 torque, bool useTorque)
	{
		this.force = force;
		this.torque = torque;
	}

	public void Add(ForceAndTorque additive)
	{
		force += additive.force;
		torque += additive.torque;
	}

	public void AddTorque(Vector3 torque)
	{
		this.torque += torque;
	}

	public void Clear()
	{
		force = Vector3.zero;
		torque = Vector3.zero;
	}
}
