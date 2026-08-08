using System.Collections.Generic;
using NuclearOption.SavedMission;
using UnityEngine;

public static class MissionPosition
{
	public readonly struct PositionResult
	{
		public readonly Objective Objective;

		public readonly GlobalPosition Position;

		public readonly float Distance;

		public readonly Vector3 Direction;

		public readonly float? Range;

		public static PositionResult MaxDistance => new PositionResult(float.MaxValue);

		public PositionResult(Objective obj, ObjectivePosition objPos, float dist, GlobalPosition from)
		{
			this = default(PositionResult);
			Objective = obj;
			Position = objPos.Position;
			Range = objPos.Range;
			Distance = dist;
			Direction = objPos.Position - from;
		}

		private PositionResult(float dist)
		{
			this = default(PositionResult);
			Distance = dist;
		}
	}

	internal static bool TryGetClosestDistance(Unit unit, out float distance)
	{
		return TryGetClosestDistance(unit.NetworkHQ, unit.transform.GlobalPosition(), out distance);
	}

	internal static bool TryGetClosestDistance(FactionHQ factionHQ, Transform transform, out float distance)
	{
		return TryGetClosestDistance(factionHQ, transform.GlobalPosition(), out distance);
	}

	internal static bool TryGetClosestDistance(FactionHQ factionHQ, GlobalPosition from, out float distance)
	{
		if (TryGetClosestObjective(factionHQ, from, out var objective))
		{
			DistanceTo(objective, from, out var result);
			distance = result.Distance;
			return true;
		}
		distance = 0f;
		return false;
	}

	public static bool TryGetClosestPosition(Unit unit, out GlobalPosition destination)
	{
		return TryGetClosestPosition(unit.NetworkHQ, unit.transform.GlobalPosition(), out destination);
	}

	public static bool TryGetClosestPosition(FactionHQ factionHQ, GlobalPosition from, out GlobalPosition destination)
	{
		if (TryGetClosestObjective(factionHQ, from, out var objective))
		{
			DistanceTo(objective, from, out var result);
			destination = result.Position;
			return true;
		}
		destination = default(GlobalPosition);
		return false;
	}

	public static bool TryGetClosestObjective(Unit unit, out Objective objective)
	{
		return TryGetClosestObjective(unit.NetworkHQ, unit.transform.GlobalPosition(), out objective);
	}

	public static bool TryGetClosestObjective(FactionHQ factionHQ, GlobalPosition from, out Objective objective)
	{
		objective = null;
		if (!TryGetActiveObjectives(factionHQ, out var objectives))
		{
			return false;
		}
		float num = float.MaxValue;
		foreach (Objective item in objectives)
		{
			if (DistanceTo(item, from, out var result) && result.Distance < num)
			{
				num = result.Distance;
				objective = item;
			}
		}
		return objective != null;
	}

	public static bool TryGetClosestObjectivePosition(Unit unit, out PositionResult result)
	{
		return TryGetClosestObjectivePosition(unit.NetworkHQ, unit.transform.GlobalPosition(), out result);
	}

	public static bool TryGetClosestObjectivePosition(FactionHQ factionHQ, GlobalPosition from, out PositionResult closest)
	{
		closest = default(PositionResult);
		if (!TryGetActiveObjectives(factionHQ, out var objectives))
		{
			return false;
		}
		closest = PositionResult.MaxDistance;
		bool result = false;
		foreach (Objective item in objectives)
		{
			if (DistanceTo(item, from, out var result2) && result2.Distance < closest.Distance)
			{
				closest = result2;
				result = true;
			}
		}
		return result;
	}

	public static bool DistanceTo(Objective obj, GlobalPosition from, out PositionResult result)
	{
		result = default(PositionResult);
		if (!(obj is IObjectiveWithPosition { Positions: var positions }))
		{
			return false;
		}
		if (positions.Count == 0)
		{
			return false;
		}
		float num = float.MaxValue;
		int index = 0;
		for (int i = 0; i < positions.Count; i++)
		{
			float num2 = FastMath.Distance(positions[i].Position, from);
			if (num2 < num)
			{
				num = num2;
				index = i;
			}
		}
		ObjectivePosition objPos = positions[index];
		result = new PositionResult(obj, objPos, num, from);
		return true;
	}

	public static PositionResult ResultForPosition(GlobalPosition pos, GlobalPosition from, Objective obj = null)
	{
		return ResultForPosition(new ObjectivePosition(pos, null), from, obj);
	}

	public static PositionResult ResultForPosition(ObjectivePosition pos, GlobalPosition from, Objective obj = null)
	{
		float dist = FastMath.Distance(pos.Position, from);
		return new PositionResult(obj, pos, dist, from);
	}

	public static bool TryGetActiveObjectives(FactionHQ factionHQ, out List<Objective> objectives)
	{
		if (factionHQ == null)
		{
			objectives = null;
			return false;
		}
		if (MissionManager.Runner == null)
		{
			Debug.LogWarning("Mission Runner was null");
			objectives = null;
			return false;
		}
		return MissionManager.Runner.activeByFaction.TryGetValue(factionHQ, out objectives);
	}

	public static bool HasObjectiveWithPosition(FactionHQ factionHQ)
	{
		if (TryGetActiveObjectives(factionHQ, out var objectives))
		{
			foreach (Objective item in objectives)
			{
				if (item is IObjectiveWithPosition objectiveWithPosition && objectiveWithPosition.Positions.Count > 0)
				{
					return true;
				}
			}
		}
		return false;
	}

	public static void GetAllPositionsResults(Unit unit, bool includeHidden, List<PositionResult> results)
	{
		GetAllPositionsResults(unit.NetworkHQ, unit.transform.GlobalPosition(), includeHidden, results);
	}

	public static void GetAllPositionsResults(FactionHQ factionHQ, GlobalPosition from, bool includeHidden, List<PositionResult> results)
	{
		results.Clear();
		if (!TryGetActiveObjectives(factionHQ, out var objectives))
		{
			return;
		}
		foreach (Objective item in objectives)
		{
			if ((!includeHidden && item.SavedObjective.Hidden) || !(item is IObjectiveWithPosition objectiveWithPosition))
			{
				continue;
			}
			foreach (ObjectivePosition position in objectiveWithPosition.Positions)
			{
				float dist = FastMath.Distance(position.Position, from);
				results.Add(new PositionResult(item, position, dist, from));
			}
		}
	}
}
