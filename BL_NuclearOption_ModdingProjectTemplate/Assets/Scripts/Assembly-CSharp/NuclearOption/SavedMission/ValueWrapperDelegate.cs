using System;
using System.Collections.Generic;

namespace NuclearOption.SavedMission
{
	public class ValueWrapperDelegate<T> : IValueWrapper<T>, IValueWrapper where T : IEquatable<T>
	{
		private readonly Func<T> getter;

		private readonly Action<T> setter;

		private readonly List<(object owner, ValueWrapper<T>.OnChangeDelegate callback)> callbacks = new List<(object, ValueWrapper<T>.OnChangeDelegate)>();

		private readonly List<(object owner, Action callback)> simpleCallbacks = new List<(object, Action)>();

		public T Value => getter();

		public ValueWrapperDelegate(Func<T> getter, Action<T> setter)
		{
			this.getter = getter;
			this.setter = setter;
		}

		public void SetValue(T value, object source, bool invokeOnChangeOnly = true)
		{
			T y = getter();
			if (invokeOnChangeOnly && EqualityComparer<T>.Default.Equals(value, y))
			{
				return;
			}
			setter(value);
			(object, ValueWrapper<T>.OnChangeDelegate)[] array = callbacks.ToArray();
			for (int i = 0; i < array.Length; i++)
			{
				var (obj, onChangeDelegate) = array[i];
				if (obj != source)
				{
					onChangeDelegate(value);
				}
			}
			(object, Action)[] array2 = simpleCallbacks.ToArray();
			for (int i = 0; i < array2.Length; i++)
			{
				var (obj2, action) = array2[i];
				if (obj2 != source)
				{
					action();
				}
			}
		}

		public void RegisterOnChange(object owner, Action callback)
		{
			simpleCallbacks.Add((owner, callback));
		}

		public void RegisterOnChange(object owner, ValueWrapper<T>.OnChangeDelegate callback)
		{
			callbacks.Add((owner, callback));
		}

		public void UnregisterOnChange(object owner)
		{
			callbacks.RemoveAll(((object owner, ValueWrapper<T>.OnChangeDelegate callback) c) => c.owner == owner);
			simpleCallbacks.RemoveAll(((object owner, Action callback) c) => c.owner == owner);
		}
	}
}
