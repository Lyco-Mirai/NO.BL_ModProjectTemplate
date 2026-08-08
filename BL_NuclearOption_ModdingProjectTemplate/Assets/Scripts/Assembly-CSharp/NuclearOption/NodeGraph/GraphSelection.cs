using System.Collections.Generic;

namespace NuclearOption.NodeGraph
{
	public readonly struct GraphSelection
	{
		public readonly GraphSelectionType Type;

		public readonly IReadOnlyList<GraphNode> Nodes;

		public readonly GraphPin Pin;

		public readonly GraphConnection? Connection;

		private GraphSelection(GraphSelectionType type, IReadOnlyList<GraphNode> nodes, GraphPin pin, GraphConnection? connection)
		{
			Type = type;
			Nodes = nodes;
			Pin = pin;
			Connection = connection;
		}

		public static GraphSelection None()
		{
			return default(GraphSelection);
		}

		public static GraphSelection FromNodes(IReadOnlyList<GraphNode> nodes)
		{
			return new GraphSelection((nodes.Count > 0) ? GraphSelectionType.Nodes : GraphSelectionType.None, nodes, null, null);
		}

		public static GraphSelection FromPin(GraphPin pin)
		{
			return new GraphSelection(GraphSelectionType.Pin, null, pin, null);
		}

		public static GraphSelection FromConnection(GraphConnection connection)
		{
			return new GraphSelection(GraphSelectionType.Connection, null, null, connection);
		}
	}
}
