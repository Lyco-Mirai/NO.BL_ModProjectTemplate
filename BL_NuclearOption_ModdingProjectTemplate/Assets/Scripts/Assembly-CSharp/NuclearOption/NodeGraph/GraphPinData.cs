using System.Collections.Generic;

namespace NuclearOption.NodeGraph
{
	public class GraphPinData : GraphElementData
	{
		public PinType PinType;

		public List<PinType> AllowedConnectionTypes = new List<PinType>();

		public bool AllowMultipleConnections;
	}
}
