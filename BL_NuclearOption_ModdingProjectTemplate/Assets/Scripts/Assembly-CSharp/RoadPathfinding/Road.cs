using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoadPathfinding
{
	[Serializable]
	public class Road
	{
		[SerializeField]
		private Bounds bounds;

		[SerializeField]
		private bool bridge;

		public List<GlobalPosition> points = new List<GlobalPosition>();

		public float length;

		[NonSerialized]
		public Node startNode;

		[NonSerialized]
		public Node endNode;

		private bool editable;

		public Road()
		{
			bounds = default(Bounds);
			points = new List<GlobalPosition>();
			CalcLength();
			UpdateBB();
		}

		public void SetEditable(bool canEdit)
		{
			editable = canEdit;
		}

		public void SetBridge(bool isBridge)
		{
			bridge = isBridge;
		}

		public bool IsEditable()
		{
			return editable;
		}

		public bool IsBridge()
		{
			return bridge;
		}

		public void AddPoint(GlobalPosition point)
		{
			points.Add(point);
			UpdateBB();
		}

		public bool InBounds(GlobalPosition point)
		{
			if (bounds.Contains(point.AsVector3()))
			{
				return true;
			}
			return false;
		}

		public void UpdateBB()
		{
			if (points.Count != 0)
			{
				bounds.center = points[0].AsVector3();
				bounds.size = Vector3.up * 10000f;
				for (int i = 0; i < points.Count; i++)
				{
					bounds.Encapsulate(points[i].AsVector3());
				}
			}
		}

		public void TrySplit(RoadNetwork roadNetwork, GlobalPosition splitPosition)
		{
			if (!bounds.Contains(splitPosition.AsVector3()))
			{
				return;
			}
			int num = -1;
			for (int i = 1; i < points.Count - 1; i++)
			{
				if (FastMath.InRange(splitPosition, points[i], 10f))
				{
					num = i;
					break;
				}
			}
			if (num != -1)
			{
				Road road = new Road();
				road.AddPoint(points[num]);
				for (int num2 = num - 1; num2 >= 0; num2--)
				{
					road.AddPoint(points[num2]);
					points.RemoveAt(num2);
				}
				road.CalcLength();
				roadNetwork.roads.Add(road);
				CalcLength();
				UpdateBB();
			}
		}

		public float CalcLength()
		{
			length = 0f;
			for (int i = 0; i < points.Count - 1; i++)
			{
				length += FastMath.Distance(points[i], points[i + 1]);
			}
			return length;
		}

		public void GenerateNodes(RoadNetwork roadNetwork)
		{
			startNode = null;
			endNode = null;
			for (int i = 0; i < roadNetwork.nodes.Count; i++)
			{
				if (FastMath.InRange(points[0], roadNetwork.nodes[i].position, 10f))
				{
					startNode = roadNetwork.nodes[i];
				}
				List<GlobalPosition> list = points;
				if (FastMath.InRange(list[list.Count - 1], roadNetwork.nodes[i].position, 10f))
				{
					endNode = roadNetwork.nodes[i];
				}
			}
			if (startNode == null)
			{
				startNode = new Node(roadNetwork, points[0]);
			}
			if (endNode == null)
			{
				List<GlobalPosition> list2 = points;
				endNode = new Node(roadNetwork, list2[list2.Count - 1]);
			}
		}

		public void CheckIntersection(RoadNetwork roadNetwork)
		{
			for (int num = roadNetwork.roads.Count - 1; num >= 0; num--)
			{
				if (roadNetwork.roads[num] != this)
				{
					roadNetwork.roads[num].TrySplit(roadNetwork, points[0]);
					Road road = roadNetwork.roads[num];
					List<GlobalPosition> list = points;
					road.TrySplit(roadNetwork, list[list.Count - 1]);
				}
			}
		}
	}
}
