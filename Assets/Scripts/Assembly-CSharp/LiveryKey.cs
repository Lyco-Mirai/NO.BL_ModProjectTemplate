using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using Cysharp.Threading.Tasks;
using Mirage;
using NuclearOption.AddressableScripts;
using NuclearOption.AddressableScripts.ModFoldersImpl;
using Steamworks;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

[Serializable]
public readonly struct LiveryKey : IEquatable<LiveryKey>
{
	public enum KeyType : byte
	{
		Builtin = 0,
		AppData = 1,
		Workshop = 2
	}

	public readonly KeyType Type;

	public readonly int Index;

	[MaxLength(128)]
	public readonly string AppDataName;

	public readonly ulong Id;

	public PublishedFileId_t WorkshopId => new PublishedFileId_t(Id);

	public LiveryKey(int index)
	{
		this = default(LiveryKey);
		Type = KeyType.Builtin;
		Index = index;
	}

	public LiveryKey(LiveryMetaData metaData, bool workshop)
	{
		this = default(LiveryKey);
		Type = ((!workshop) ? KeyType.AppData : KeyType.Workshop);
		if (workshop)
		{
			Id = metaData.Id.m_PublishedFileId;
		}
		else
		{
			AppDataName = Path.GetFileName(metaData.FolderFullPath);
		}
	}

	public LiveryKey(KeyType type, int index, string nameOrId)
	{
		this = default(LiveryKey);
		Type = type;
		Index = index;
		switch (type)
		{
		case KeyType.AppData:
			AppDataName = nameOrId;
			break;
		case KeyType.Workshop:
			Id = ulong.Parse(nameOrId);
			break;
		}
	}

	public void Save(out KeyType type, out int index, out string nameOrId)
	{
		type = Type;
		index = Index;
		if (Type == KeyType.AppData)
		{
			nameOrId = AppDataName;
		}
		else if (Type == KeyType.Workshop)
		{
			ulong id = Id;
			nameOrId = id.ToString();
		}
		else
		{
			nameOrId = "";
		}
	}

	public override string ToString()
	{
		return Type switch
		{
			KeyType.AppData => $"({Type},{AppDataName})", 
			KeyType.Workshop => $"({Type},{Id})", 
			_ => $"({Type},{Index})", 
		};
	}

	public bool CanLoad(Aircraft aircraft, out string folder)
	{
		switch (Type)
		{
		case KeyType.Builtin:
			folder = null;
			return true;
		case KeyType.AppData:
			return Skins.CanLoad(this, aircraft, out folder);
		case KeyType.Workshop:
			return Skins.CanLoad(this, aircraft, out folder);
		default:
			throw new InvalidEnumArgumentException("Type", (int)Type, typeof(KeyType));
		}
	}

	public async UniTask<(bool success, AsyncOperationHandle<LiveryData> handle)> Load(Aircraft aircraft)
	{
		if (CanLoad(aircraft, out var folder))
		{
			AsyncOperationHandle<LiveryData> item = await LoadImpl(aircraft, folder);
			return (item.IsValid(), item);
		}
		return (false, default(AsyncOperationHandle<LiveryData>));
	}

	private UniTask<AsyncOperationHandle<LiveryData>> LoadImpl(Aircraft aircraft, string folder)
	{
		return Type switch
		{
			KeyType.Builtin => GetBuiltin(aircraft), 
			KeyType.AppData => ModFolders.AppDataSkins.LoadAsset<LiveryData>(folder), 
			KeyType.Workshop => ModFolders.WorkshopSkins.LoadAsset<LiveryData>(folder), 
			_ => throw new InvalidEnumArgumentException("Type", (int)Type, typeof(KeyType)), 
		};
	}

	private UniTask<AsyncOperationHandle<LiveryData>> GetBuiltin(Aircraft aircraft)
	{
		AircraftParameters aircraftParameters = aircraft.GetAircraftParameters();
		List<AircraftParameters.Livery> liveries = aircraftParameters.liveries;
		if (Index >= liveries.Count)
		{
			Debug.LogError($"Livery index for {aircraftParameters.aircraftName} out of range. index:{Index} Max:{liveries.Count}");
			return default(UniTask<AsyncOperationHandle<LiveryData>>);
		}
		return ModLoader.LoadAsset(aircraft.GetAircraftParameters().liveries[Index].assetReference);
	}

	public bool Equals(LiveryKey other)
	{
		if (Type == other.Type && Index == other.Index && AppDataName == other.AppDataName)
		{
			return Id == other.Id;
		}
		return false;
	}
}
