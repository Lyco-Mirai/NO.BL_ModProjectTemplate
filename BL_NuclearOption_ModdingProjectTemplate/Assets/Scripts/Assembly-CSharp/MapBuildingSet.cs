using System;
using System.Collections.Generic;
using Mirage;
using Mirage.Collections;
using Mirage.RemoteCalls;
using Mirage.Serialization;
using NuclearOption.Jobs;
using NuclearOption.Networking;
using UnityEngine;

public class MapBuildingSet : NetworkBehaviour
{
	[SerializeField]
	private bool animate;

	[Tooltip("Should powerlines be drawn between map buildings")]
	[SerializeField]
	private bool powerlines;

	private MapBuilding[] mapBuildings;

	public readonly SyncList<bool> buildingStates = new SyncList<bool>();

	public readonly List<MapBuilding> animatedBuildings = new List<MapBuilding>();

	[NonSerialized]
	private const int SYNC_VAR_COUNT = 0;

	[NonSerialized]
	private const int RPC_COUNT = 1;

	private void Awake()
	{
		base.Identity.OnStartServer.AddListener(OnStartServer);
		base.Identity.OnStartClient.AddListener(OnStartClient);
		buildingStates.OnSet += OnBuildingStatesChanged;
		base.enabled = false;
	}

	private void OnStartServer()
	{
		NetworkStart();
		buildingStates.Clear();
		for (int i = 0; i < mapBuildings.Length; i++)
		{
			buildingStates.Add(item: true);
		}
	}

	private void NetworkStart()
	{
		mapBuildings = GetComponentsInChildren<MapBuilding>();
		animatedBuildings.Clear();
		for (int i = 0; i < mapBuildings.Length; i++)
		{
			mapBuildings[i].AssignBuildingSet(this, i);
			if (powerlines && i < mapBuildings.Length - 1)
			{
				mapBuildings[i].SpawnPowerline(mapBuildings[i + 1]);
			}
		}
	}

	private void OnStartClient()
	{
		if (!base.IsServer)
		{
			NetworkStart();
		}
		if (animate && GameManager.ShowEffects)
		{
			base.enabled = true;
		}
		for (int i = 0; i < buildingStates.Count; i++)
		{
			OnBuildingStatesChanged(i, oldValue: true, buildingStates[i]);
		}
	}

	public void DestroyBuilding(int index)
	{
		if (base.IsServer)
		{
			buildingStates[index] = false;
		}
		else
		{
			CmdDestroyBuilding(index);
		}
	}

	[RateLimit(Refill = 5, MaxTokens = 50, Penalty = 5)]
	[ServerRpc(requireAuthority = false)]
	private void CmdDestroyBuilding(int index, INetworkPlayer sender = null)
	{
		if (ServerRpcSender.ShouldInvokeLocally(this, requireAuthority: false, allowServerToCall: false))
		{
			UserCode_CmdDestroyBuilding_1002795805(index, base.Server.LocalPlayer);
			return;
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WritePackedInt32(index);
		ServerRpcSender.Send(this, 0, writer, Channel.Reliable, requireAuthority: false);
		writer.Release();
	}

	private void OnBuildingStatesChanged(int index, bool oldValue, bool newValue)
	{
		if (!newValue)
		{
			mapBuildings[index].Destruct();
		}
	}

	private void Update()
	{
		for (int num = animatedBuildings.Count - 1; num >= 0; num--)
		{
			if (animatedBuildings[num].MapBuilding_OnUpdate() == PartResult.Remove)
			{
				animatedBuildings.RemoveAt(num);
			}
		}
	}

	public MapBuildingSet()
	{
		InitSyncObject(buildingStates);
	}

	private void MirageProcessed()
	{
	}

	private void UserCode_CmdDestroyBuilding_1002795805(int index, INetworkPlayer sender)
	{
		if (0 <= index && index < buildingStates.Count)
		{
			ColorLog<MapBuildingSet>.Info($"CmdDestroyBuilding: {sender} destroyed building at index {index}");
			buildingStates[index] = false;
		}
		else
		{
			ColorLog<MapBuildingSet>.InfoWarn($"CmdDestroyBuilding: {sender} attempted to destroy out-of-bounds building index {index} (Count: {buildingStates.Count})");
			sender.SetError(5, NuclearOptionPlayerErrorFlags.OutOfBounds);
		}
	}

	protected static void Skeleton_CmdDestroyBuilding_1002795805(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((MapBuildingSet)behaviour).UserCode_CmdDestroyBuilding_1002795805(reader.ReadPackedInt32(), senderConnection);
	}

	protected override int GetRpcCount()
	{
		return 1;
	}

	protected override void RegisterRpc(RemoteCallCollection collection)
	{
		base.RegisterRpc(collection);
		collection.Register(0, "MapBuildingSet.CmdDestroyBuilding", cmdRequireAuthority: false, RpcInvokeType.ServerRpc, this, Skeleton_CmdDestroyBuilding_1002795805, RpcRateLimitConfig.Enabled(1f, 5, 50, 5));
	}
}
