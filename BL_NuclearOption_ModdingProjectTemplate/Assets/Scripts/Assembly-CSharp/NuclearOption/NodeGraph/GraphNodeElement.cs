using UnityEngine;

namespace NuclearOption.NodeGraph
{
	public abstract class GraphNodeElement : MonoBehaviour
	{
		public GraphNode Node { get; private set; }

		public GraphEditor Editor { get; private set; }

		public PinDirection Direction { get; private set; }

		public abstract void Setup(GraphNode parentNode, GraphElementData data, GraphEditor editor, PinDirection direction);

		protected void Init(GraphNode parentNode, GraphElementData data, GraphEditor editor, PinDirection direction)
		{
			Node = parentNode;
			Editor = editor;
			Direction = direction;
			base.gameObject.name = data.PinId.ToString();
		}
	}
}
