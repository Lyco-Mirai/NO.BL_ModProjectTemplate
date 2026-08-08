using System.Collections.Generic;
using UnityEngine;

namespace NuclearOption.NodeGraph
{
	public class GraphNodeData
	{
		public NodeId ID;

		public string TitleText;

		public NodeType NodeType;

		public string TagText;

		public Color NodeColor = Color.grey;

		public Color TagColor = Color.grey;

		public Vector2 Position;

		public bool StopDelete;

		public bool StopDuplicate;

		public List<GraphElementData> InputElements = new List<GraphElementData>();

		public List<GraphElementData> OutputElements = new List<GraphElementData>();
	}
}
