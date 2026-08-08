using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Triggers;
using UnityEngine;

namespace NuclearOption.SavedMission
{
	public abstract class ValueWrapper
	{
		public static TWrapper FromCallback<TWrapper, TValue>(TValue value, Action<TValue> onSet) where TWrapper : ValueWrapper<TValue>, new() where TValue : IEquatable<TValue>
		{
			return FromCallback<TWrapper, TValue>(onSet, value, onSet);
		}

		public static TWrapper FromCallback<TWrapper, TValue>(object owner, TValue value, Action<TValue> onSet) where TWrapper : ValueWrapper<TValue>, new() where TValue : IEquatable<TValue>
		{
			TWrapper val = new TWrapper();
			val.SetValue(value, null);
			val.RegisterOnChange(owner, onSet.Invoke);
			return val;
		}
	}
	public abstract class ValueWrapper<T> : ValueWrapper, IValueWrapper<T>, IValueWrapper where T : IEquatable<T>
	{
		public delegate void OnChangeDelegate(T newValue);

		private readonly List<(object owner, OnChangeDelegate callback)> callbacks = new List<(object, OnChangeDelegate)>();

		private T _value;

		public T Value => _value;

		public ref T GetValueRef()
		{
			return ref _value;
		}

		public void RegisterOnChange(object owner, Action callback)
		{
			callbacks.Add((owner, delegate
			{
				callback();
			}));
		}

		public void RegisterOnChange(object owner, OnChangeDelegate callback)
		{
			callbacks.Add((owner, callback));
		}

		public void UnregisterOnChange(object owner)
		{
			for (int i = 0; i < callbacks.Count; i++)
			{
				if (callbacks[i].owner == owner)
				{
					callbacks.RemoveAt(i);
				}
			}
		}

		public void RegisterOnChangeWithAutoDestroy(Component owner, OnChangeDelegate callback)
		{
			callbacks.Add((owner, callback));
			owner.OnDestroyAsync().ContinueWith(delegate
			{
				UnregisterOnChange(owner);
			}).Forget();
		}

		public void RegisterOnChangeWithAutoDestroy(GameObject owner, OnChangeDelegate callback)
		{
			callbacks.Add((owner, callback));
			owner.OnDestroyAsync().ContinueWith(delegate
			{
				UnregisterOnChange(owner);
			}).Forget();
		}

		public ValueWrapper()
		{
		}

		public ValueWrapper(T value)
		{
			_value = value;
		}

		public void SetValue(T value, object source, bool invokeOnChangeOnly = true)
		{
			if (invokeOnChangeOnly && EqualityComparer<T>.Default.Equals(value, Value))
			{
				return;
			}
			_value = value;
			(object, OnChangeDelegate)[] array = callbacks.ToArray();
			for (int i = 0; i < array.Length; i++)
			{
				var (obj, onChangeDelegate) = array[i];
				if (obj != source)
				{
					onChangeDelegate(value);
				}
			}
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		public static implicit operator T(ValueWrapper<T> wrapper)
		{
			return wrapper.Value;
		}
	}
}
