using System.Collections.Generic;
using System.Diagnostics;
using AStar;
using UnityEngine;

public class Pathfinding : MonoBehaviour
{
	private NodeGrid grid;

	public Transform seeker;

	public Transform target;

	private bool pathFound;

	private Stopwatch sw = new Stopwatch();

	private void Awake()
	{
		grid = GetComponent<NodeGrid>();
	}

	private void Update()
	{
		if (grid.initialized && !pathFound && Input.GetKey(KeyCode.Space))
		{
			FindPath(seeker.position.ToGlobalPosition(), target.GlobalPosition());
			pathFound = true;
		}
	}

	private int GetDistance(Node nodeA, Node nodeB)
	{
		int num = Mathf.Abs(nodeA.gridX - nodeB.gridX);
		int num2 = Mathf.Abs(nodeA.gridY - nodeB.gridY);
		if (num > num2)
		{
			return 14 * num2 + 10 * (num - num2);
		}
		return 14 * num + 10 * (num2 - num);
	}

	private void FindPath(GlobalPosition startPos, GlobalPosition targetPos)
	{
		sw.Start();
		Node node = grid.NodeFromWorldPoint(startPos);
		Node node2 = grid.NodeFromWorldPoint(targetPos);
		Heap<Node> heap = new Heap<Node>(grid.maxSize);
		HashSet<Node> hashSet = new HashSet<Node>();
		heap.Add(node);
		while (heap.Count > 0)
		{
			Node node3 = heap.RemoveFirst();
			hashSet.Add(node3);
			if (node3 == node2)
			{
				sw.Stop();
				RetracePath(node, node2);
				break;
			}
			foreach (Node neighbor in grid.GetNeighbors(node3))
			{
				if (!neighbor.traversable || hashSet.Contains(neighbor))
				{
					continue;
				}
				int num = node3.gCost + (int)((float)GetDistance(node3, neighbor) / neighbor.traversability);
				if (num < neighbor.gCost || !heap.Contains(neighbor))
				{
					neighbor.gCost = num;
					neighbor.hCost = GetDistance(neighbor, node2);
					neighbor.parent = node3;
					if (!heap.Contains(neighbor))
					{
						heap.Add(neighbor);
					}
					else
					{
						heap.UpdateItem(neighbor);
					}
				}
			}
		}
	}

	private void RetracePath(Node startNode, Node endNode)
	{
		List<Node> list = new List<Node>();
		for (Node node = endNode; node != startNode; node = node.parent)
		{
			list.Add(node);
		}
		list.Reverse();
		grid.path = list;
	}
}
