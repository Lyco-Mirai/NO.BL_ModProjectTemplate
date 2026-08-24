using System.Collections.Generic;
using UnityEngine;

public class VehicleFormation
{
	public GroundVehicle leader;

	public Dictionary<GroundVehicle, int> members = new Dictionary<GroundVehicle, int>();

	private int formationWidth = 1;

	private int formationLength = 1;

	private float formationSpacing = 10f;

	public VehicleFormation(GroundVehicle leader)
	{
		members.Add(leader, 0);
		this.leader = leader;
	}

	public void RemoveVehicle(GroundVehicle vehicle)
	{
		if (members.ContainsKey(vehicle))
		{
			members.Remove(vehicle);
			CalcDimensions();
		}
	}

	public void AddVehicle(GroundVehicle vehicle)
	{
		if (!members.ContainsKey(vehicle))
		{
			members.Add(vehicle, members.Count);
			CalcDimensions();
		}
	}

	public void CalcDimensions()
	{
		formationWidth = members.Count;
		formationLength = 1;
		if (members.Count >= 4)
		{
			formationWidth = Mathf.FloorToInt(Mathf.Sqrt(members.Count));
			formationLength = Mathf.CeilToInt(members.Count / formationWidth);
		}
	}

	public GlobalPosition? GetTargetPos(GroundVehicle vehicle)
	{
		GlobalPosition? result = null;
		if (vehicle != leader)
		{
			int num = members[vehicle];
			_ = members.Count;
			int num2 = num % formationWidth;
			int num3 = Mathf.FloorToInt(num / formationLength);
			float num4 = ((float)num2 - (float)formationWidth * 0.5f) * formationSpacing * 2f;
			float num5 = (float)num3 * formationSpacing;
			result = leader.GlobalPosition() + -leader.transform.forward * num5 + leader.transform.right * num4;
		}
		return result;
	}
}
