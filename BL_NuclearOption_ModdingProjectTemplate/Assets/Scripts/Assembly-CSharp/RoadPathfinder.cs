using System.Collections.Generic;
using RoadPathfinding;
using Unity.Profiling;
using UnityEngine;

public static class RoadPathfinder
{
	public enum PathfindResult
	{
		NoNetwork = 0,
		NoConnection = 1,
		NoRoadNearTarget = 2,
		Success = 3
	}

	private static readonly ProfilerMarker tryPathfindMarker = new ProfilerMarker("RoadPathfinder.TryPathfind");

	private static readonly List<Node> unvisitedSet = new List<Node>();

	private static List<GlobalPosition> generatedWaypoints = new List<GlobalPosition>();

	public static void TryPathfind(RoadNetwork network, GlobalPosition startPos, GlobalPosition targetPos, List<Node> results, out PathfindResult result)
	{
		result = PathfindResult.NoNetwork;
		using (tryPathfindMarker.Auto())
		{
			if (network == null || network.roads.Count == 0)
			{
				return;
			}
			network.ClearPathfindingData();
			Node closestNode = null;
			Node closestNode2 = null;
			if (network.TryGetNearestRoad(startPos, out var nearestRoad))
			{
				float num = FastMath.SquareDistance(startPos, nearestRoad.startNode.position);
				float num2 = FastMath.SquareDistance(startPos, nearestRoad.endNode.position);
				closestNode = ((num < num2) ? nearestRoad.startNode : nearestRoad.endNode);
			}
			else
			{
				TryFindNearestNode(network, startPos, out closestNode);
			}
			closestNode.dist = 0f;
			float range = float.MaxValue;
			if (network.TryGetNearestRoad(targetPos, out var nearestRoad2))
			{
				float num3 = FastMath.SquareDistance(targetPos, nearestRoad2.startNode.position);
				float num4 = FastMath.SquareDistance(targetPos, nearestRoad2.endNode.position);
				closestNode2 = ((num3 < num4) ? nearestRoad2.startNode : nearestRoad2.endNode);
				foreach (GlobalPosition point in nearestRoad2.points)
				{
					if (FastMath.InRange(point, targetPos, range))
					{
						range = FastMath.Distance(point, targetPos);
					}
				}
			}
			else if (TryFindNearestNode(network, targetPos, out closestNode2))
			{
				range = FastMath.Distance(closestNode2.position, targetPos);
			}
			if (FastMath.InRange(startPos, targetPos, range))
			{
				result = PathfindResult.NoRoadNearTarget;
				return;
			}
			unvisitedSet.Clear();
			unvisitedSet.AddRange(network.nodes);
			while (unvisitedSet.Count > 0)
			{
				unvisitedSet.Sort((Node a, Node b) => a.dist.CompareTo(b.dist));
				closestNode = unvisitedSet[0];
				unvisitedSet.Remove(closestNode);
				foreach (KeyValuePair<Road, Node> item in closestNode.connectionsLookup)
				{
					Node value = item.Value;
					Road key = item.Key;
					if (unvisitedSet.Contains(value) && closestNode.dist + key.length < value.dist)
					{
						value.dist = closestNode.dist + key.length;
						value.parent = closestNode;
					}
				}
				if (closestNode.dist == float.MaxValue)
				{
					result = PathfindResult.NoConnection;
					results.Clear();
					return;
				}
				if (closestNode == closestNode2)
				{
					break;
				}
			}
			results.Clear();
			while (closestNode.parent != null)
			{
				results.Add(closestNode);
				closestNode = closestNode.parent;
			}
			results.Add(closestNode);
			results.Reverse();
			result = PathfindResult.Success;
		}
	}

	public static bool TryFindNearestNode(RoadNetwork network, GlobalPosition worldPos, out Node closestNode)
	{
		closestNode = null;
		float num = float.MaxValue;
		for (int i = 0; i < network.nodes.Count; i++)
		{
			float num2 = FastMath.SquareDistance(worldPos, network.nodes[i].position);
			if (num2 < num)
			{
				closestNode = network.nodes[i];
				num = num2;
			}
		}
		return closestNode != null;
	}

	public static List<GlobalPosition> GetWaypointsToStartNode(RoadPoint startInfo, List<Node> nodelist, bool debug, out Road startRoad)
	{
		startRoad = startInfo.road;
		generatedWaypoints.Clear();
		if (nodelist.Count > 1 && (startRoad.startNode == nodelist[1] || startRoad.endNode == nodelist[1]))
		{
			int index = ((startRoad.startNode == nodelist[1]) ? 1 : (startRoad.points.Count - 2));
			GetRoadFragment(generatedWaypoints, startRoad, startInfo.index, index, out var _);
			nodelist.RemoveAt(0);
		}
		else if (!FastMath.InRange(startRoad.points[startInfo.index], nodelist[0].position, 10f))
		{
			int index = ((startRoad.startNode == nodelist[0]) ? 1 : (startRoad.points.Count - 2));
			GetRoadFragment(generatedWaypoints, startRoad, startInfo.index, index, out var _);
			if (debug)
			{
				Debug.Log($"generating starting road fragment from {startInfo.index} to {index}");
			}
		}
		return generatedWaypoints;
	}

	public static List<GlobalPosition> GetWaypointsFromEndNode(RoadPoint endInfo, List<Node> nodelist)
	{
		generatedWaypoints.Clear();
		Road road = endInfo.road;
		if (nodelist.Count == 0)
		{
			return generatedWaypoints;
		}
		int index;
		if (road.startNode == nodelist[0] || road.endNode == nodelist[0])
		{
			index = ((road.startNode != nodelist[0]) ? (road.points.Count - 1) : 0);
			GetRoadFragment(generatedWaypoints, road, index, endInfo.index, out var _);
			nodelist.Clear();
			return generatedWaypoints;
		}
		generatedWaypoints = GetWaypointsBetweenNodes(nodelist[0], nodelist[1], out var _);
		index = ((road.startNode != nodelist[1]) ? (road.points.Count - 1) : 0);
		GetRoadFragment(generatedWaypoints, road, index, endInfo.index, out var _);
		nodelist.Clear();
		return generatedWaypoints;
	}

	public static RoadPoint GetNearestRoadPoint(GlobalPosition position, Node fromNode)
	{
		float num = float.MaxValue;
		int index = -1;
		Road road = null;
		foreach (KeyValuePair<Road, Node> item in fromNode.connectionsLookup)
		{
			Road key = item.Key;
			for (int i = 0; i < key.points.Count; i++)
			{
				float num2 = FastMath.SquareDistance(position, key.points[i]);
				if (num2 < num)
				{
					road = key;
					num = num2;
					index = i;
				}
			}
		}
		return new RoadPoint(road, index);
	}

	public static RoadPoint GetStartingRoadPoint(GlobalPosition startPosition, List<Node> nodelist)
	{
		Road road = null;
		float num = 0f;
		foreach (KeyValuePair<Road, Node> item in nodelist[0].connectionsLookup)
		{
			Road key = item.Key;
			float num2 = 0f;
			foreach (GlobalPosition point in key.points)
			{
				num2 += 1f / FastMath.SquareDistance(startPosition, point);
			}
			if (num2 > num)
			{
				road = key;
				num = num2;
			}
		}
		float num3 = float.MaxValue;
		int index = 0;
		for (int i = 0; i < road.points.Count; i++)
		{
			float num4 = FastMath.Distance(road.points[i], startPosition);
			if (num4 < num3)
			{
				index = i;
				num3 = num4;
			}
		}
		return new RoadPoint(road, index);
	}

	public static void GetRoadFragment(List<GlobalPosition> result, Road road, int index1, int index2, out Road currentRoad)
	{
		currentRoad = road;
		if (index1 < index2)
		{
			for (int i = index1; i <= index2; i++)
			{
				result.Add(road.points[i]);
			}
			return;
		}
		for (int num = index1; num >= index2; num--)
		{
			result.Add(road.points[num]);
		}
	}

	public static List<GlobalPosition> GetWaypointsBetweenNodes(Node startNode, Node endNode, out Road currentRoad)
	{
		generatedWaypoints.Clear();
		currentRoad = null;
		foreach (KeyValuePair<Road, Node> item in startNode.connectionsLookup)
		{
			if (item.Key.endNode == endNode)
			{
				GetRoadFragment(generatedWaypoints, item.Key, 0, item.Key.points.Count - 2, out currentRoad);
				break;
			}
			if (item.Key.startNode == endNode)
			{
				GetRoadFragment(generatedWaypoints, item.Key, item.Key.points.Count - 1, 1, out currentRoad);
				break;
			}
		}
		return generatedWaypoints;
	}

	public static bool VisibleUnderwater(GlobalPosition fromPos, GlobalPosition toPos)
	{
		toPos.y = fromPos.y;
		return !Physics.Linecast(fromPos.ToLocalPosition(), toPos.ToLocalPosition(), (int)PhysicsLayers.StaticsMask | (int)PhysicsLayers.ExclusionZonesMask);
	}
}
