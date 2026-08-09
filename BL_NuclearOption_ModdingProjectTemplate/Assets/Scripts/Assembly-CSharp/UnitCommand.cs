using System;
using Cysharp.Threading.Tasks;
using Mirage;
using Mirage.RemoteCalls;
using Mirage.Serialization;
using NuclearOption.Networking;
using Unity.Profiling;
using UnityEngine;

public class UnitCommand : NetworkBehaviour
{
	public delegate void ProcessCommand(ref Command command);

	public struct Command
	{
		public float time;

		public Player player;

		public GlobalPosition position;

		public bool FromPlayer => player != null;

		public Command(float time, GlobalPosition position, Player player)
		{
			this.time = time;
			this.player = player;
			this.position = position;
		}
	}

	private static readonly ProfilerMarker setDestinationMarker = new ProfilerMarker("UnitCommand.SetDestination");

	private ICommandable target;

	private Command currentCommand;

	private float RequestCommandFromServerNextAllowedTime;

	[NonSerialized]
	private const int SYNC_VAR_COUNT = 0;

	[NonSerialized]
	private const int RPC_COUNT = 2;

	public event ProcessCommand ProcessSetDestination;

	private void Awake()
	{
		target = GetComponent<ICommandable>();
		if (target == null)
		{
			Debug.LogError("Capture could not find ICapturable on " + base.name);
		}
	}

	public void SetDestination(GlobalPosition waypoint, bool playerCommand)
	{
		if (target.Disabled)
		{
			return;
		}
		using (setDestinationMarker.Auto())
		{
			if (NetworkManagerNuclearOption.i.Server.Active)
			{
				Player localPlayer = null;
				if (playerCommand)
				{
					GameManager.GetLocalPlayer<Player>(out localPlayer);
				}
				ServerSetDestination(waypoint, localPlayer);
			}
			else
			{
				CmdSetDestination(waypoint);
			}
		}
	}

	[RateLimit(Refill = 5, MaxTokens = 20, Penalty = 1)]
	[ServerRpc(requireAuthority = false)]
	private void CmdSetDestination(GlobalPosition waypoint, INetworkPlayer sender = null)
	{
		if (ServerRpcSender.ShouldInvokeLocally(this, requireAuthority: false, allowServerToCall: false))
		{
			UserCode_CmdSetDestination_1791143641(waypoint, base.Server.LocalPlayer);
			return;
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteGlobalPosition(waypoint);
		ServerRpcSender.Send(this, 0, writer, Mirage.Channel.Reliable, requireAuthority: false);
		writer.Release();
	}

	[Server]
	private void ServerSetDestination(GlobalPosition waypoint, Player player)
	{
		if (!base.IsServer)
		{
			throw new MethodInvocationException("[Server] function 'ServerSetDestination' called when server not active");
		}
		currentCommand = new Command(NetworkSceneSingleton<MissionManager>.i.MissionTime, waypoint, player);
		this.ProcessSetDestination?.Invoke(ref currentCommand);
	}

	public Command GetCommandCached()
	{
		return currentCommand;
	}

	public async UniTask<Command> CmdTryGetCommand()
	{
		float timeSinceLevelLoad = Time.timeSinceLevelLoad;
		if (timeSinceLevelLoad > RequestCommandFromServerNextAllowedTime)
		{
			RequestCommandFromServerNextAllowedTime = timeSinceLevelLoad + 1f;
			currentCommand = await CmdGetCommandInternal();
		}
		return currentCommand;
	}

	[RateLimit(Refill = 5, MaxTokens = 45, Penalty = 1)]
	[ServerRpc(requireAuthority = false)]
	private UniTask<Command> CmdGetCommandInternal()
	{
		if (ServerRpcSender.ShouldInvokeLocally(this, requireAuthority: false, allowServerToCall: false))
		{
			return UserCode_CmdGetCommandInternal__002D133123890();
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		UniTask<Command> result = ServerRpcSender.SendWithReturn<Command>(this, 1, writer, requireAuthority: false);
		writer.Release();
		return result;
	}

	private void MirageProcessed()
	{
	}

	private void UserCode_CmdSetDestination_1791143641(GlobalPosition waypoint, INetworkPlayer sender)
	{
		/*
		Player player;
		if (!NetworkFloatHelper.Validate(waypoint, logErrors: false, null))
		{
			sender.SetError(1, NuclearOptionPlayerErrorFlags.InvalidValue);
		}
		else if (!sender.TryGetPlayer<Player>(out player))
		{
			ColorLog<UnitCommand>.LogError($"CmdSetDestination: {sender} did not have player object");
			sender.SetError(100, PlayerErrorFlags.Critical);
		}
		else if (!player.HasAuthority && player.HQ != target.HQ)
		{
			ColorLog<UnitCommand>.InfoWarn($"CmdSetDestination: {player.SteamID} attempted to command unit {base.name} belonging to another faction");
			sender.SetError(2, NuclearOptionPlayerErrorFlags.FactionViolation);
		}
		else
		{
			ColorLog<UnitCommand>.Info($"CmdSetDestination: {player.SteamID} set destination to {waypoint} for {base.name}");
			ServerSetDestination(waypoint, player);
		}*/
	}

	protected static void Skeleton_CmdSetDestination_1791143641(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((UnitCommand)behaviour).UserCode_CmdSetDestination_1791143641(reader.ReadGlobalPosition(), senderConnection);
	}

	private UniTask<Command> UserCode_CmdGetCommandInternal__002D133123890()
	{
		return UniTask.FromResult(currentCommand);
	}

	protected static UniTask<Command> Skeleton_CmdGetCommandInternal__002D133123890(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		return ((UnitCommand)behaviour).UserCode_CmdGetCommandInternal__002D133123890();
	}

	protected override int GetRpcCount()
	{
		return 2;
	}

	protected override void RegisterRpc(RemoteCallCollection collection)
	{
		base.RegisterRpc(collection);
		collection.Register(0, "UnitCommand.CmdSetDestination", cmdRequireAuthority: false, RpcInvokeType.ServerRpc, this, Skeleton_CmdSetDestination_1791143641, RpcRateLimitConfig.Enabled(1f, 5, 20, 1));
		collection.RegisterRequest(1, "UnitCommand.CmdGetCommandInternal", cmdRequireAuthority: false, RpcInvokeType.ServerRpc, this, Skeleton_CmdGetCommandInternal__002D133123890, RpcRateLimitConfig.Enabled(1f, 5, 45, 1));
	}
}
