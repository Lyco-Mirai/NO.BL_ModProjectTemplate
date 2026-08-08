using System.Collections.Generic;
using RoadPathfinding;
using UnityEngine;

public class PathfindingAgent
{
	private readonly List<Node> nodes = new List<Node>();

	private readonly List<GlobalPosition> waypoints = new List<GlobalPosition>();

	private readonly List<GlobalPosition> nextWaypoints = new List<GlobalPosition>();

	private GlobalPosition targetPos;

	private Transform movingTarget;

	private RoadPoint startingRoadPoint;

	private RoadPoint endingRoadPoint;

	private readonly Unit unit;

	private Road currentRoad;

	private float lastWaterCheck;

	private float lastSkipCheck;

	public PathfindingAgent(Unit unit)
	{
		nodes = new List<Node>();
		waypoints = new List<GlobalPosition>();
		nextWaypoints = new List<GlobalPosition>();
		this.unit = unit;
	}

	public void Shortcut(GlobalPosition destination)
	{
		movingTarget = null;
		nodes.Clear();
		waypoints.Clear();
		waypoints.Add(destination);
		currentRoad = null;
		if (PlayerSettings.debugVis && unit == SceneSingleton<CameraStateManager>.i.followingUnit)
		{
			List<GlobalPosition> list = new List<GlobalPosition>();
			list.Add(unit.GlobalPosition());
			list.Add(targetPos);
			NetworkSceneSingleton<LevelInfo>.i.VisualizeWaypoints(list, unit);
		}
	}

	public bool IsOnBridge()
	{
		if (currentRoad != null)
		{
			return currentRoad.IsBridge();
		}
		return false;
	}

	public void Pathfind(RoadNetwork network, GlobalPosition targetPos, Transform lineOfSightChecker)
	{
		if (!FastMath.InRange(new Vector3(targetPos.x, 0f, targetPos.z), new Vector3(this.targetPos.x, 0f, this.targetPos.z), 10f))
		{
			movingTarget = null;
			nodes.Clear();
			waypoints.Clear();
			GlobalPosition globalPosition = unit.GlobalPosition();
			this.targetPos = targetPos;
			if (!(unit is Ship) && RaycastTerrain(targetPos, out var hit))
			{
				this.targetPos = hit.point.ToGlobalPosition();
			}
			RoadPathfinder.TryPathfind(network, globalPosition, this.targetPos, nodes, out var result);
			switch (result)
			{
			case RoadPathfinder.PathfindResult.NoNetwork:
				return;
			case RoadPathfinder.PathfindResult.NoConnection:
				return;
			case RoadPathfinder.PathfindResult.NoRoadNearTarget:
				waypoints.Add(targetPos);
				break;
			case RoadPathfinder.PathfindResult.Success:
				waypoints.AddRange(GetStartingWaypoints(globalPosition, lineOfSightChecker));
				break;
			}
			lastSkipCheck = 0f;
			SkipWaypointIfBehind(globalPosition);
			RouteDebug();
		}
	}

	public GlobalPosition GetDryPosition(GlobalPosition startPos, GlobalPosition targetPos)
	{
		if (targetPos.y == 0f && RaycastTerrain(targetPos, out var hit))
		{
			targetPos = hit.point.ToGlobalPosition();
		}
		Vector3 vector = targetPos.ToLocalPosition();
		Vector3 b = startPos.ToLocalPosition();
		for (int i = 0; i < 5; i++)
		{
			if (RaycastTerrain(vector, out var hit2) && hit2.point.y >= Datum.LocalSeaY)
			{
				vector.y = hit2.point.y;
				break;
			}
			vector = FastMath.LerpXZ(vector, b, 0.5f);
		}
		GlobalPosition result = vector.ToGlobalPosition();
		result.NotEqual(targetPos);
		return result;
	}

	private List<GlobalPosition> GetStartingWaypoints(GlobalPosition startPos, Transform lineOfSightChecker)
	{
		nextWaypoints.Clear();
		if (lineOfSightChecker != null)
		{
			if (RoadPathfinder.VisibleUnderwater(lineOfSightChecker.GlobalPosition(), targetPos))
			{
				Shortcut(targetPos);
				return nextWaypoints;
			}
			if (TrySkipNodesUnderwater(lineOfSightChecker))
			{
				startPos = nodes[0].position;
			}
		}
		startingRoadPoint = RoadPathfinder.GetStartingRoadPoint(startPos, nodes);
		GlobalPosition position = targetPos;
		List<Node> list = nodes;
		endingRoadPoint = RoadPathfinder.GetNearestRoadPoint(position, list[list.Count - 1]);
		if (startingRoadPoint.road == endingRoadPoint.road)
		{
			nodes.Clear();
			currentRoad = startingRoadPoint.road;
			if (startingRoadPoint.index == endingRoadPoint.index)
			{
				nextWaypoints.Add(startingRoadPoint.road.points[startingRoadPoint.index]);
				nextWaypoints.Add(targetPos);
			}
			RoadPathfinder.GetRoadFragment(nextWaypoints, startingRoadPoint.road, startingRoadPoint.index, endingRoadPoint.index, out currentRoad);
			nextWaypoints.Add(targetPos);
			return nextWaypoints;
		}
		return RoadPathfinder.GetWaypointsToStartNode(startingRoadPoint, nodes, SceneSingleton<CameraStateManager>.i.followingUnit == unit, out currentRoad);
	}

	private List<GlobalPosition> GetNextWaypoints(List<Node> nodeList)
	{
		nextWaypoints.Clear();
		if (nodeList.Count > 2)
		{
			nextWaypoints.AddRange(RoadPathfinder.GetWaypointsBetweenNodes(nodeList[0], nodeList[1], out currentRoad));
			nodeList.RemoveAt(0);
		}
		else
		{
			currentRoad = null;
			nextWaypoints.AddRange(RoadPathfinder.GetWaypointsFromEndNode(endingRoadPoint, nodeList));
			nextWaypoints.Add(targetPos);
			nodeList.Clear();
		}
		return nextWaypoints;
	}

	public void AddWaypoints(List<GlobalPosition> waypoints)
	{
		this.waypoints.AddRange(waypoints);
	}

	public void InsertWaypoints(List<GlobalPosition> waypoints)
	{
		this.waypoints.InsertRange(0, waypoints);
	}

	public void AddWaypoint(GlobalPosition waypoint)
	{
		waypoints.Add(waypoint);
	}

	public void ClearDestination()
	{
		targetPos = unit.GlobalPosition();
		nodes.Clear();
		waypoints.Clear();
	}

	private void RouteDebug()
	{
		if (!PlayerSettings.debugVis || !(unit == SceneSingleton<CameraStateManager>.i.followingUnit))
		{
			return;
		}
		List<Node> list = new List<Node>(nodes);
		List<GlobalPosition> list2 = new List<GlobalPosition>(waypoints);
		foreach (Node node in nodes)
		{
			GameObject gameObject = Object.Instantiate(GameAssets.i.debugPoint, Datum.origin);
			gameObject.GetComponent<Renderer>().material.SetColor("_EmissionColor", Color.green);
			gameObject.transform.localScale = Vector3.one * 30f;
			gameObject.transform.localPosition = node.position.AsVector3();
			NetworkSceneSingleton<Spawner>.i.DestroyLocal(gameObject, 5f);
		}
		GlobalPosition item = unit.GlobalPosition();
		list2.Insert(0, item);
		while (list.Count > 0)
		{
			list2.AddRange(GetNextWaypoints(list));
		}
		NetworkSceneSingleton<LevelInfo>.i.VisualizeWaypoints(list2, unit);
	}

	public void SetMovingTarget(Transform target)
	{
		movingTarget = target;
	}

	private bool TrySkipNodesUnderwater(Transform lineOfSightChecker)
	{
		if (nodes.Count < 2)
		{
			return false;
		}
		int num = 0;
		for (int i = 0; i < nodes.Count && RoadPathfinder.VisibleUnderwater(lineOfSightChecker.GlobalPosition(), nodes[i].position); i++)
		{
			num = i;
		}
		if (num > 0)
		{
			if (PlayerSettings.debugVis && unit == SceneSingleton<CameraStateManager>.i.followingUnit)
			{
				for (int j = 0; j <= num; j++)
				{
					GameObject gameObject = Object.Instantiate(GameAssets.i.debugPoint, Datum.origin);
					gameObject.GetComponent<Renderer>().material.SetColor("_EmissionColor", Color.red);
					gameObject.transform.localScale = Vector3.one * 40f;
					gameObject.transform.localPosition = nodes[j].position.AsVector3();
					NetworkSceneSingleton<Spawner>.i.DestroyLocal(gameObject, 5f);
				}
			}
			nodes.RemoveRange(0, num);
		}
		return num > 0;
	}

	private void TrySkipToWaypointVisibleUnderwater(Transform keel, float margin)
	{
		if (waypoints.Count >= 3 && !(Time.timeSinceLevelLoad - lastSkipCheck < 5f))
		{
			lastSkipCheck = Time.timeSinceLevelLoad;
			if (NetworkSceneSingleton<LevelInfo>.i.seaLanes.TryGetNearestPoint(keel.GlobalPosition(), out var _, out var _) && RoadPathfinder.VisibleUnderwater(keel.GlobalPosition(), waypoints[2]))
			{
				waypoints.RemoveAt(0);
			}
		}
	}

	private void SkipWaypointIfBehind(GlobalPosition position)
	{
		if (waypoints.Count < 2)
		{
			if (waypoints.Count > 0 && nodes.Count > 0)
			{
				Vector3 lhs = FastMath.Direction(position, waypoints[0]);
				Vector3 rhs = FastMath.Direction(position, nodes[0].position);
				if (Vector3.Dot(lhs, rhs) < 0f)
				{
					waypoints[0] = Vector3.Lerp(position.ToLocalPosition(), nodes[0].position.ToLocalPosition(), 0.5f).ToGlobalPosition();
				}
			}
		}
		else
		{
			Vector3 lhs2 = FastMath.Direction(waypoints[0], waypoints[1]);
			Vector3 rhs2 = FastMath.Direction(position, waypoints[0]);
			if (Vector3.Dot(lhs2, rhs2) < 0f)
			{
				waypoints.RemoveAt(0);
			}
		}
	}

	public SteeringInfo? GetSteerpoint(GlobalPosition position, Vector3 forward, float speed, bool stayOnRoad)
	{
		if (movingTarget != null)
		{
			return new SteeringInfo(FastMath.Direction(position, movingTarget.position.ToGlobalPosition()), 90f);
		}
		if (waypoints.Count < 3 && nodes.Count > 0)
		{
			waypoints.AddRange(GetNextWaypoints(nodes));
		}
		if (waypoints.Count == 0)
		{
			currentRoad = null;
			return SteeringInfo.None;
		}
		Vector3 vector = Vector3.Cross(forward, -Vector3.up) * 2f;
		GlobalPosition globalPosition = waypoints[0] + vector;
		float num = 0f;
		int num2 = 20;
		GlobalPosition globalPosition2 = globalPosition;
		bool flag;
		if (waypoints.Count > 1 && waypoints[1].NotEqual(globalPosition))
		{
			GlobalPosition globalPosition3 = waypoints[1] + vector;
			Vector3 vector2 = FastMath.NormalizedDirection(globalPosition, globalPosition3);
			globalPosition2 += vector2 * num2;
			float num3 = FastMath.Distance(position, globalPosition2);
			globalPosition2 -= vector2 * Mathf.Min(num3 * 0.5f, num2);
			Vector3 to = ((waypoints.Count > 2) ? (waypoints[2] - waypoints[1]) : (waypoints[1] - globalPosition3));
			flag = num3 < (float)num2 || (num3 < (float)(num2 * 3) && Vector3.Dot(FastMath.Direction(position, globalPosition), forward) < 0f);
			num = ((speed * speed * 0.2f < num3) ? 0f : Vector3.Angle(forward, to));
		}
		else
		{
			float num3 = FastMath.Distance(position, globalPosition);
			flag = num3 < 5f || (num3 < (float)(num2 * 3) && Vector3.Dot(FastMath.Direction(position, globalPosition), forward) < 0f);
			num = ((speed > num3) ? 90 : 0);
		}
		if (flag)
		{
			waypoints.RemoveAt(0);
		}
		if (Time.timeSinceLevelLoad - lastWaterCheck > 4f && waypoints.Count > 0)
		{
			lastWaterCheck = Time.timeSinceLevelLoad;
			Vector3 position2 = unit.transform.position;
			Vector3 zero = Vector3.zero;
			zero = ((!FastMath.InRange(unit.GlobalPosition(), waypoints[0], 100f)) ? (unit.transform.position + FastMath.NormalizedDirection(unit.GlobalPosition(), waypoints[0]) * 100f) : waypoints[0].ToLocalPosition());
			if ((!RaycastTerrain(zero, out var hit) || hit.point.y < Datum.LocalSeaY) && RaycastTerrain(position2, out var hit2) && hit2.point.y > Datum.LocalSeaY && unit is GroundVehicle groundVehicle)
			{
				groundVehicle.StopImmediately();
			}
		}
		if (position == globalPosition2)
		{
			return SteeringInfo.None;
		}
		return new SteeringInfo(FastMath.Direction(position, globalPosition2), num);
	}

	public SteeringInfo? GetShipSteerpoint(GlobalPosition position, Vector3 forward, float speed, bool stayOnRoad, Transform keel)
	{
		float num = (unit.maxRadius + 50f) * 5f;
		TrySkipToWaypointVisibleUnderwater(keel, unit.maxRadius * 3f);
		if (waypoints.Count < 2 && nodes.Count > 0)
		{
			waypoints.AddRange(GetNextWaypoints(nodes));
		}
		if (waypoints.Count == 0)
		{
			return SteeringInfo.None;
		}
		GlobalPosition globalPosition = waypoints[0];
		float num2 = FastMath.Distance(globalPosition, position);
		float nextWaypointAngle = 0f;
		GlobalPosition to = globalPosition;
		bool flag = false;
		if (waypoints.Count > 1)
		{
			if (num2 < num * 2f)
			{
				GlobalPosition to2 = waypoints[1];
				float num3 = Mathf.Clamp01((num - num2) / num);
				if (num3 > 0f)
				{
					to = globalPosition + num3 * num * FastMath.NormalizedDirection(globalPosition, to2);
				}
				flag = Vector3.Dot(globalPosition - position, forward) < 0f;
			}
		}
		else
		{
			flag = num2 < num && Vector3.Dot(FastMath.Direction(position, globalPosition), forward) < 0f;
		}
		if (flag)
		{
			waypoints.RemoveAt(0);
		}
		return new SteeringInfo(FastMath.Direction(position, to), nextWaypointAngle);
	}

	public static bool RaycastTerrain(GlobalPosition position, out RaycastHit hit)
	{
		return RaycastTerrain(position.ToLocalPosition(), out hit);
	}

	public static bool RaycastTerrain(Vector3 position, out RaycastHit hit)
	{
		return Physics.Raycast(position + Vector3.up * 10000f, Vector3.down * 20000f, out hit, 20000f, PhysicsLayers.StaticsMask);
	}
}
