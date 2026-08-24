using System;

namespace NuclearOption.MissionEditorScripts.MultiSelect
{
	public struct MultiField<T>
	{
		public readonly Func<T> Get;

		public readonly Action<T> Set;

		public MultiField(Func<T> get, Action<T> set)
		{
			Get = get;
			Set = set;
		}
	}
}
