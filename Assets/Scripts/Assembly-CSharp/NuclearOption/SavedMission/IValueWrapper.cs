using System;

namespace NuclearOption.SavedMission
{
	public interface IValueWrapper
	{
		void RegisterOnChange(object owner, Action callback);

		void UnregisterOnChange(object owner);
	}
	public interface IValueWrapper<T> : IValueWrapper where T : IEquatable<T>
	{
		T Value { get; }

		void SetValue(T value, object source, bool invokeOnChangeOnly = true);

		void RegisterOnChange(object owner, ValueWrapper<T>.OnChangeDelegate callback);
	}
}
