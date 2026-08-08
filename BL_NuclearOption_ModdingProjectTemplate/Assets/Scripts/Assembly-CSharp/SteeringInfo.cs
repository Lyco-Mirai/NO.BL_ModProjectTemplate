using UnityEngine;

public readonly struct SteeringInfo
{
	public readonly Vector3 steerVector;

	public readonly float nextWaypointAngle;

	public static readonly SteeringInfo? None;

	public SteeringInfo(Vector3 steerVector, float nextWaypointAngle)
	{
		this.steerVector = steerVector;
		this.nextWaypointAngle = nextWaypointAngle;
	}
}
