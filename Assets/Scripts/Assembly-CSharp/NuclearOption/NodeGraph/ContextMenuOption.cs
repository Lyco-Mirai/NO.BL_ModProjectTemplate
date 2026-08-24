using System;
using System.Collections.Generic;

namespace NuclearOption.NodeGraph
{
	public class ContextMenuOption
	{
		public ContextMenuOptionId optionId;

		public string label;

		public List<ContextMenuPinSetup> compatibilityPins = new List<ContextMenuPinSetup>();

		public Action<ContextMenuOptionId> onClick;

		public bool Disable;
	}
}
