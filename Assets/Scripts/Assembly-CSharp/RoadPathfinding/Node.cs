using System.Collections.Generic;

namespace RoadPathfinding
{
	public class Node
	{
		public int id;

		public GlobalPosition position;

		public Dictionary<Road, Node> connectionsLookup = new Dictionary<Road, Node>();

		public Node parent;

		public float dist;

		public Node(RoadNetwork roadNetwork, GlobalPosition position)
		{
			this.position = position;
			id = roadNetwork.nodes.Count;
			roadNetwork.nodes.Add(this);
		}

		public void GenerateConnections(RoadNetwork roadNetwork)
		{
			connectionsLookup.Clear();
			foreach (Road road in roadNetwork.roads)
			{
				if (road.startNode == this || road.endNode == this)
				{
					Node value = ((road.startNode != this) ? road.startNode : road.endNode);
					connectionsLookup.Add(road, value);
				}
			}
		}

		public void ClearPathfindingData()
		{
			parent = null;
			dist = float.MaxValue;
		}
	}
}
