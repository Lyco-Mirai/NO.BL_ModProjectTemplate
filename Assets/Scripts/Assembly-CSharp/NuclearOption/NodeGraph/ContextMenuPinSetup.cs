using System;
using System.Collections.Generic;

namespace NuclearOption.NodeGraph
{
	[Serializable]
	public class ContextMenuPinSetup
	{
		public PinId pinId;

		public PinType pinType;

		public PinDirection direction;

		public List<PinType> allowedConnectionTypes = new List<PinType>();
	}
}
