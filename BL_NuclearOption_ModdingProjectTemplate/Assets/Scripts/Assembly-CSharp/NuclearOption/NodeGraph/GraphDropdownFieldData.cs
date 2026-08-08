using System.Collections.Generic;
using NuclearOption.SavedMission;

namespace NuclearOption.NodeGraph
{
	public class GraphDropdownFieldData : GraphElementData
	{
		public IValueWrapper<int> ValueWrapper;

		public List<string> Options;
	}
}
