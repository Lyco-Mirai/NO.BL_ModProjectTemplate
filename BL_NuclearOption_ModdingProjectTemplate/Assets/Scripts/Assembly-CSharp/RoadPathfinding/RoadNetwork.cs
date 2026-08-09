using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoadPathfinding
{
	[Serializable]
	public class RoadNetwork
	{
		[NonSerialized]
		public bool AllowMerge;

		public List<Road> roads = new List<Road>();

		[NonSerialized]
		public List<Node> nodes = new List<Node>();

		public bool Exists()
		{
			return roads.Count > 0;
		}

		public bool TryGetNearestRoad(GlobalPosition fromPosition, out Road nearestRoad)
		{
			nearestRoad = null;
			float num = float.MaxValue;
			foreach (Road road in roads)
			{
				if (!road.InBounds(fromPosition))
				{
					continue;
				}
				foreach (GlobalPosition point in road.points)
				{
					float num2 = FastMath.SquareDistance(point, fromPosition);
					if (num2 < num)
					{
						nearestRoad = road;
						num = num2;
					}
				}
			}
			return nearestRoad != null;
		}

		public void RegenerateNetwork()
		{
			nodes.Clear();
			foreach (Road road in roads)
			{
				road.GenerateNodes(this);
			}
			foreach (Node node in nodes)
			{
				node.GenerateConnections(this);
			}
		}

		public void ClearPathfindingData()
		{
			foreach (Node node in nodes)
			{
				node.ClearPathfindingData();
			}
		}

		public bool TryGetNearestNode(GlobalPosition fromPosition, out Node nearestNode)
		{
			nearestNode = null;
			if (nodes.Count == 0)
			{
				return false;
			}
			float num = float.MaxValue;
			foreach (Node node in nodes)
			{
				if ((fromPosition - node.position).sqrMagnitude < num)
				{
					num = FastMath.SquareDistance(fromPosition, node.position);
					nearestNode = node;
				}
			}
			return true;
		}

		public bool TryGetNearestPoint(GlobalPosition fromPosition, out GlobalPosition nearestPoint, out Vector3 roadDirection)
		{
			nearestPoint = fromPosition;
			roadDirection = Vector3.forward;
			if (!TryGetNearestNode(fromPosition, out var nearestNode))
			{
				return false;
			}
			float num = float.MaxValue;
			int num2 = 0;
			Road road = null;
			foreach (KeyValuePair<Road, Node> item in nearestNode.connectionsLookup)
			{
				Road key = item.Key;
				for (int i = 0; i < key.points.Count; i++)
				{
					GlobalPosition globalPosition = key.points[i];
					float num3 = FastMath.SquareDistance(globalPosition, fromPosition);
					if (num3 < num)
					{
						num2 = i;
						nearestPoint = globalPosition;
						num = num3;
						road = key;
					}
				}
			}
			float range = float.MaxValue;
			if (num2 + 1 < road.points.Count)
			{
				GlobalPosition globalPosition2 = road.points[num2 + 1];
				range = FastMath.Distance(globalPosition2, fromPosition);
				roadDirection = FastMath.NormalizedDirection(nearestPoint, globalPosition2);
			}
			if (num2 - 1 >= 0)
			{
				GlobalPosition globalPosition3 = road.points[num2 - 1];
				if (FastMath.InRange(fromPosition, globalPosition3, range))
				{
					roadDirection = FastMath.NormalizedDirection(nearestPoint, globalPosition3);
				}
			}
			return true;
		}

		public void Merge(RoadNetwork other)
		{
			if (!AllowMerge)
			{
				ColorLog<RoadNetwork>.LogError("RoadNetwork was not allowed to be merged with another network. Make sure RoadNetwork is a copy and not part of Asset");
			}
			else if (other.roads.Count > 0)
			{
				roads.AddRange(other.roads);
				RegenerateNetwork();
			}
		}
	}
}
