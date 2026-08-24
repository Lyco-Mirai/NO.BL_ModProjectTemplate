using System;
using NuclearOption.MissionEditorScripts;

namespace NuclearOption.SavedMission
{
	public static class ValueWrapperExtension
	{
		public static void Setup<TWrapper, TValue>(this IDataField<TValue> field, string label, TValue value, Action<TValue> callback) where TWrapper : ValueWrapper<TValue>, new() where TValue : IEquatable<TValue>
		{
			field.Setup(label, ValueWrapper.FromCallback<TWrapper, TValue>(value, callback));
		}
	}
}
