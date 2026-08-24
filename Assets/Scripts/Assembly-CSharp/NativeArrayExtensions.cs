using System;
using Cysharp.Threading.Tasks;
using NuclearOption.Jobs;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Jobs;

public static class NativeArrayExtensions
{
	public static void SafeClear(this Array array)
	{
		if (array != null)
		{
			Array.Clear(array, 0, array.Length);
		}
	}

	public static void DelayDispose(this ref TransformAccessArray array, bool delayed = true)
	{
		if (!array.isCreated)
		{
			return;
		}
		if (delayed)
		{
			TransformAccessArray copy = array;
			UniTask.Delay(1000).ContinueWith(delegate
			{
				copy.Dispose();
			}).Forget();
		}
		else
		{
			array.Dispose();
		}
		array = default(TransformAccessArray);
	}

	public static void DelayDispose<T>(this ref NativeArray<T> array, bool delayed = true) where T : unmanaged
	{
		if (!array.IsCreated)
		{
			return;
		}
		if (delayed)
		{
			NativeArray<T> copy = array;
			UniTask.Delay(1000).ContinueWith(delegate
			{
				SafeDispose(ref copy);
			}).Forget();
		}
		else
		{
			array.Dispose();
		}
		array = default(NativeArray<T>);
	}

	public static void SafeDispose<T>(this ref NativeArray<T> array) where T : struct
	{
		try
		{
			array.Dispose();
			array = default(NativeArray<T>);
		}
		catch (ObjectDisposedException)
		{
			Debug.Log("NativeArray already disposed");
		}
	}

	public static void ClearAndDispose<T>(this ref NativeArray<PtrRefCounter<T>> array, int length) where T : unmanaged
	{
		if (array.IsCreated)
		{
			for (int i = 0; i < length; i++)
			{
				if (array[i].IsCreated)
				{
					array[i].RemoveRef();
				}
			}
		}
		array.Dispose();
	}

	public static void Resize<T>(this ref NativeArray<T> array, int newLength, bool copyItems, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory) where T : unmanaged
	{
		NativeArray<T> nativeArray = new NativeArray<T>(newLength, Allocator.Persistent, options);
		NativeArray<T> src = array;
		if (src.IsCreated)
		{
			if (copyItems)
			{
				NativeArray<T>.Copy(src, 0, nativeArray, 0, src.Length);
			}
			src.Dispose();
		}
		array = nativeArray;
	}

	public static void Resize(this ref TransformAccessArray array, int newLength)
	{
		if (array.isCreated)
		{
			array.capacity = newLength * 2;
		}
		else
		{
			array = new TransformAccessArray(newLength * 2);
		}
	}
}
