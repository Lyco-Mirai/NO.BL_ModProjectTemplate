using System;

namespace NuclearOption.SavedMission
{
	public class ValueWrapperOverride<T> : ValueWrapper<Override<T>>, IValueWrapper<T>, IValueWrapper where T : IEquatable<T>
	{
		T IValueWrapper<T>.Value => base.Value.Value;

		void IValueWrapper<T>.RegisterOnChange(object owner, ValueWrapper<T>.OnChangeDelegate callback)
		{
			RegisterOnChange(owner, delegate(Override<T> v)
			{
				callback(v.Value);
			});
		}

		void IValueWrapper<T>.SetValue(T value, object source, bool invokeOnChangeOnly)
		{
			SetValue(new Override<T>(base.Value.IsOverride, value), source, invokeOnChangeOnly);
		}
	}
}
