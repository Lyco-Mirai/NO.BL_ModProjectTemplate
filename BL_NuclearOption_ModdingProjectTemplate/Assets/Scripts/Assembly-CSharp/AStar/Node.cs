using System;
using UnityEngine;

namespace AStar
{
	public class Node : IHeapItem<Node>, IComparable<Node>
	{
		public float traversability;

		public bool traversable;

		public Vector3 worldPosition;

		private int heapIndex;

		public int gCost;

		public int hCost;

		public Node parent;

		public int gridX;

		public int gridY;

		public int fCost => gCost + hCost;

		public int HeapIndex
		{
			get
			{
				return heapIndex;
			}
			set
			{
				heapIndex = value;
			}
		}

		public Node(float traversability, Vector3 worldPosition, int gridX, int gridY)
		{
			this.traversability = traversability;
			traversable = traversability > 0.05f;
			this.worldPosition = worldPosition;
			this.gridX = gridX;
			this.gridY = gridY;
		}

		public int CompareTo(Node nodeToCompare)
		{
			int num = fCost.CompareTo(nodeToCompare.fCost);
			if (num == 0)
			{
				num = hCost.CompareTo(nodeToCompare.hCost);
			}
			return -num;
		}
	}
}
