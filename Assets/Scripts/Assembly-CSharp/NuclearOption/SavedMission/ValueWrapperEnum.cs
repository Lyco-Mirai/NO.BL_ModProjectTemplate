using System;
using System.Collections.Generic;

namespace NuclearOption.SavedMission
{
	public class ValueWrapperEnum<T> : IValueWrapper<int>, IValueWrapper where T : unmanaged, Enum
	{
		private readonly Func<T> getter;

		private readonly Action<T> setter;

		private readonly object owner;

		private readonly List<(object owner, ValueWrapper<int>.OnChangeDelegate callback)> callbacks = new List<(object, ValueWrapper<int>.OnChangeDelegate)>();

		private readonly List<(object owner, Action callback)> simpleCallbacks = new List<(object, Action)>();

		public int Value => UnsafeCast<T, int>(getter());

		public ValueWrapperEnum(object owner, Func<T> getter, Action<T> setter)
		{
			this.owner = owner;
			this.getter = getter;
			this.setter = setter;
		}

		public void SetValue(int value, object source, bool invokeOnChangeOnly = true)
		{
			T val = UnsafeCast<int, T>(value);
			T y = getter();
			if (invokeOnChangeOnly && EqualityComparer<T>.Default.Equals(val, y))
			{
				return;
			}
			setter(val);
			(object, ValueWrapper<int>.OnChangeDelegate)[] array = callbacks.ToArray();
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

		public void RegisterOnChange(object owner, ValueWrapper<int>.OnChangeDelegate callback)
		{
			callbacks.Add((owner, callback));
		}

		public void UnregisterOnChange(object owner)
		{
			callbacks.RemoveAll(((object owner, ValueWrapper<int>.OnChangeDelegate callback) c) => c.owner == owner);
			simpleCallbacks.RemoveAll(((object owner, Action callback) c) => c.owner == owner);
		}

		private unsafe static TResult UnsafeCast<TFrom, TResult>(TFrom value) where TFrom : unmanaged where TResult : unmanaged
		{
			return *(TResult*)(&value);
		}
	}
}
