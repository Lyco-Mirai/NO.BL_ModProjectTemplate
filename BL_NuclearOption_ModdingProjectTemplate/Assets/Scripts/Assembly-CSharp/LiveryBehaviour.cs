using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class LiveryBehaviour : MonoBehaviour
{
	private Aircraft aircraft;

	private LiveryKey? fallback;

	private LiveryKey key;

	private bool hasLoaded;

	private AsyncOperationHandle<LiveryData> handle;

	private MaterialCleanup materialCleanup;

	public bool IsLoading { get; private set; }

	private void OnDestroy()
	{
		ReleaseHandle(ref handle);
		materialCleanup?.CleanupAll();
	}

	public void RemoveFromMaterialCleanup(Material material)
	{
		materialCleanup?.Remove(material);
	}

	private static void ReleaseHandle(ref AsyncOperationHandle<LiveryData> handle)
	{
		if (handle.IsValid())
		{
			Addressables.Release(handle);
			handle = default(AsyncOperationHandle<LiveryData>);
		}
	}

	public void Setup(Aircraft aircraft, LiveryKey? fallback = null)
	{
		this.aircraft = aircraft;
		this.fallback = fallback;
	}

	public void SetKey(LiveryKey key)
	{
		if (!hasLoaded || !this.key.Equals(key))
		{
			if (IsLoading)
			{
				Debug.LogError("LiveryBehaviour.Load called multiple times");
			}
			else
			{
				Load(key, fallback).Forget();
			}
		}
	}

	private async UniTask Load(LiveryKey key, LiveryKey? fallback, CancellationToken cancel = default(CancellationToken))
	{
		if (cancel == CancellationToken.None)
		{
			cancel = base.destroyCancellationToken;
		}
		this.key = key;
		hasLoaded = true;
		bool success = false;
		try
		{
			IsLoading = true;
			AsyncOperationHandle<LiveryData> asyncOperationHandle;
			(success, asyncOperationHandle) = await key.Load(aircraft);
			if (cancel.IsCancellationRequested && success)
			{
				ReleaseHandle(ref asyncOperationHandle);
				return;
			}
			if (success)
			{
				SetLivery(asyncOperationHandle.Result);
				AsyncOperationHandle<LiveryData> asyncOperationHandle2 = handle;
				handle = asyncOperationHandle;
				ReleaseHandle(ref asyncOperationHandle2);
			}
		}
		catch (Exception arg)
		{
			Debug.LogWarning($"Exception when loading: {arg}");
		}
		finally
		{
			IsLoading = false;
		}
		if (!success)
		{
			string arg2 = (aircraft.HasAuthority ? "local" : "remote");
			Debug.LogWarning($"Failed to load livery for {arg2} player. Key:{key}");
			if (fallback.HasValue)
			{
				Debug.LogWarning("Loading fallback livery");
				await Load(fallback.Value, null, cancel);
			}
		}
	}

	private void SetLivery(LiveryData livery)
	{
		if (aircraft.weaponManager != null)
		{
			aircraft.weaponManager.UpdateColorables(livery);
		}
		if (materialCleanup == null)
		{
			materialCleanup = new MaterialCleanup();
		}
		foreach (UnitPart item in aircraft.partLookup)
		{
			item.SetLivery(livery, materialCleanup);
		}
	}
}
