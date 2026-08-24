using System;
using System.Collections.Generic;
using NuclearOption.SavedMission;

namespace NuclearOption.NodeGraph
{
	public class GraphReferenceFieldData : GraphElementData
	{
		public Func<ISaveableReference> GetValue;

		public Action<ISaveableReference> SetValue;

		public Func<List<ISaveableReference>> GetOptions;
	}
}
