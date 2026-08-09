using UnityEngine;

public class TerrainWarningSystem
{
	public float urgency;

	private float lastCheck;

	private float lastTerrainWaypoint;

	private Aircraft aircraft;

	private GlobalPosition followTerrainWaypoint;

	public TerrainWarningSystem(Aircraft aircraft)
	{
		this.aircraft = aircraft;
	}

	public void CheckTerrain()
	{
		if (!(Time.timeSinceLevelLoad - lastCheck < 0.5f))
		{
			lastCheck = Time.timeSinceLevelLoad;
			Vector3 normalized = aircraft.rb.velocity.normalized;
			float num = aircraft.speed * 5f;
			Vector3 vector = aircraft.cockpit.xform.position - Vector3.up * aircraft.maxRadius;
			Vector3 vector2 = normalized * num;
			float num2 = float.MaxValue;
			Vector3 rhs = Vector3.up;
			if (Datum.WaterPlane().Raycast(new Ray(vector, vector2), out var enter) && enter < num && enter > 0f)
			{
				num2 = enter;
			}
			if (Physics.Linecast(vector, vector + vector2, out var hitInfo, (int)PhysicsLayers.StaticsMask | (int)PhysicsLayers.ExclusionZonesMask) && hitInfo.distance < num2)
			{
				num2 = hitInfo.distance;
				rhs = hitInfo.normal;
			}
			urgency = ((num2 < num) ? (urgency + Mathf.Clamp(Vector3.Dot(-normalized, rhs), 0.25f, 1f) * num / num2) : 0f);
		}
	}

	public GlobalPosition GetFollowTerrainWaypoint(Vector3 direction, float altitudeTarget, Autopilot autopilot)
	{
		if (Time.timeSinceLevelLoad - lastTerrainWaypoint > 0.5f)
		{
			lastTerrainWaypoint = Time.timeSinceLevelLoad;
			followTerrainWaypoint = autopilot.TerrainWaypoint(direction, altitudeTarget, Mathf.Max(aircraft.speed, 100f) * 6f * (9f / aircraft.GetAircraftParameters().aircraftGLimit));
		}
		return followTerrainWaypoint;
	}
}
