using System;
using NuclearOption.SavedMission;

namespace NuclearOption.MissionEditorScripts
{
	public interface IDataField<T> where T : IEquatable<T>
	{
		void Setup(string label, IValueWrapper<T> wrapper);
	}
}
