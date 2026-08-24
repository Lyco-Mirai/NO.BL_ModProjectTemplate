using System.Collections.Generic;
using AStar;
using UnityEngine;

public class NodeGrid : MonoBehaviour
{
	public Transform tester;

	public Texture2D traversabilityMap;

	public Vector2 gridWorldSize;

	private float nodeRadius;

	private Node[,] grid;

	private float nodeDiameter;

	public int maxSize;

	private int gridSizeX;

	private int gridSizeY;

	public bool initialized;

	public List<Node> path;

	private void Awake()
	{
		gridSizeX = traversabilityMap.width;
		gridSizeY = traversabilityMap.height;
		maxSize = gridSizeX * gridSizeY;
		nodeDiameter = gridWorldSize.x / (float)gridSizeX;
		nodeRadius = nodeDiameter * 0.5f;
		CreateGrid();
		initialized = true;
	}

	private void CreateGrid()
	{
		grid = new Node[gridSizeX, gridSizeY];
		Vector3 vector = base.transform.position - Vector3.right * gridWorldSize.x / 2f - Vector3.forward * gridWorldSize.y / 2f;
		for (int i = 0; i < gridSizeX; i++)
		{
			for (int j = 0; j < gridSizeY; j++)
			{
				Vector3 worldPosition = vector + Vector3.right * ((float)i * nodeDiameter + nodeRadius) + Vector3.forward * ((float)j * nodeDiameter + nodeRadius);
				float r = traversabilityMap.GetPixel(i, j).r;
				grid[i, j] = new Node(r, worldPosition, i, j);
			}
		}
	}

	public List<Node> GetNeighbors(Node node)
	{
		List<Node> list = new List<Node>();
		for (int i = -1; i <= 1; i++)
		{
			for (int j = -1; j <= 1; j++)
			{
				if (i != 0 || j != 0)
				{
					int num = node.gridX + i;
					int num2 = node.gridY + j;
					if (num >= 0 && num < gridSizeX && num2 >= 0 && num2 < gridSizeY)
					{
						list.Add(grid[num, num2]);
					}
				}
			}
		}
		return list;
	}

	public Node NodeFromWorldPoint(GlobalPosition worldPosition)
	{
		float value = (worldPosition.x + gridWorldSize.x / 2f) / gridWorldSize.x;
		float value2 = (worldPosition.z + gridWorldSize.y / 2f) / gridWorldSize.y;
		value = Mathf.Clamp01(value);
		value2 = Mathf.Clamp01(value2);
		int num = Mathf.RoundToInt((float)(gridSizeX - 1) * value);
		int num2 = Mathf.RoundToInt((float)(gridSizeY - 1) * value2);
		return grid[num, num2];
	}

	private void OnDrawGizmos()
	{
		Gizmos.DrawWireCube(base.transform.position, new Vector3(gridWorldSize.x, 1000f, gridWorldSize.y));
		if (grid == null)
		{
			return;
		}
		NodeFromWorldPoint(tester.GlobalPosition());
		if (path == null)
		{
			return;
		}
		foreach (Node item in path)
		{
			Gizmos.color = Color.white;
			Gizmos.DrawCube(new GlobalPosition(item.worldPosition).ToLocalPosition() + Vector3.up * 300f, Vector3.one * nodeDiameter * 0.9f);
		}
	}
}
