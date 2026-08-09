using System;
using Mirage;
using Mirage.RemoteCalls;
using Mirage.Serialization;
using NuclearOption.Networking;
using NuclearOption.UIStyleSystem;
using UnityEngine;

public class MessageManager : NetworkSceneSingleton<MessageManager>
{
	[NonSerialized]
	private const int SYNC_VAR_COUNT = 0;

	[NonSerialized]
	private const int RPC_COUNT = 9;

	private static Color GetSpawnMessageColor(bool localAircraft, FactionHQ aircraftHQ, FactionHQ localHq)
	{
		if (localAircraft)
		{
			return GameAssets.i.HUDFriendly;
		}
		if (localHq == null)
		{
			return aircraftHQ.faction.color;
		}
		if (localHq == aircraftHQ)
		{
			return GameAssets.i.HUDFriendly;
		}
		return GameAssets.i.HUDHostile;
	}

	public void JoinMessage(Player joinedPlayer)
	{
		string message = (joinedPlayer.GetDisplayName(PlayerNameContext.ChatOrLeaderboard) + " joined the game").AddColor(ThemeManager.Active.ColorTheme.ChatSystem);
		SceneSingleton<GameplayUI>.i.GameMessage(message);
	}

	public void DisconnectedMessage(Player player)
	{
		SceneSingleton<GameplayUI>.i.GameMessage(player.GetDisplayName(PlayerNameContext.ChatOrLeaderboard) + " Disconnected");
	}

	[ClientRpc]
	public void RpcPlayerJoinFactionMessage(Player player, FactionHQ hq)
	{
		if (ClientRpcSender.ShouldInvokeLocally(this, RpcTarget.Observers, null, excludeOwner: false))
		{
			UserCode_RpcPlayerJoinFactionMessage_156835807(player, hq);
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		GeneratedNetworkCode._Write_NuclearOption_002ENetworking_002EPlayer(writer, player);
		GeneratedNetworkCode._Write_FactionHQ(writer, hq);
		ClientRpcSender.Send(this, 0, writer, Channel.Reliable, excludeOwner: false);
		writer.Release();
	}

	public void AircraftDeployedMessage(Aircraft aircraft, Airbase airbase, bool playersOnly)
	{
		if (!GameManager.IsLocalAircraft(aircraft) && (!playersOnly || !(aircraft.Player == null)) && (!GameManager.GetLocalHQ(out var localHq) || !(localHq != aircraft.NetworkHQ)) && PlayerSettings.showAicraftDeployedMessage)
		{
			Color color = ColorFromFaction(localHq);
			string text = aircraft.unitName.AddColor(color);
			string text2 = airbase.SavedAirbase.DisplayName.AddColor(color);
			string text3 = SceneSingleton<DynamicMap>.i.gridLabels.GetGridPosition(aircraft.transform.GlobalPosition()).AddColor(color) ?? "";
			SceneSingleton<GameplayUI>.i.GameMessage(text + " deployed at " + text2 + " / " + text3);
		}
	}

	[ClientRpc(target = RpcTarget.Player)]
	public void TargetCreditMessage(INetworkPlayer _, PersistentID killedID, float creditAwarded, FactionHQ.RewardType actionType)
	{
		if (ClientRpcSender.ShouldInvokeLocally(this, RpcTarget.Player, _, excludeOwner: false))
		{
			UserCode_TargetCreditMessage_106951341(base.Client.Player, killedID, creditAwarded, actionType);
			return;
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		GeneratedNetworkCode._Write_PersistentID(writer, killedID);
		writer.WriteSingleConverter(creditAwarded);
		GeneratedNetworkCode._Write_FactionHQ_002FRewardType(writer, actionType);
		ClientRpcSender.SendTarget(this, 1, writer, Channel.Reliable, _);
		writer.Release();
	}

	[ClientRpc]
	public void RpcBombFailMessage(PersistentID bombID, float gForce)
	{
		if (ClientRpcSender.ShouldInvokeLocally(this, RpcTarget.Observers, null, excludeOwner: false))
		{
			UserCode_RpcBombFailMessage__002D1758898816(bombID, gForce);
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		GeneratedNetworkCode._Write_PersistentID(writer, bombID);
		writer.WriteSingleConverter(gForce);
		ClientRpcSender.Send(this, 2, writer, Channel.Reliable, excludeOwner: false);
		writer.Release();
	}

	[ClientRpc]
	public void RpcKillMessage(PersistentID killerID, PersistentID killedID, KillType killedType)
	{
		if (ClientRpcSender.ShouldInvokeLocally(this, RpcTarget.Observers, null, excludeOwner: false))
		{
			UserCode_RpcKillMessage_635947223(killerID, killedID, killedType);
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		GeneratedNetworkCode._Write_PersistentID(writer, killerID);
		GeneratedNetworkCode._Write_PersistentID(writer, killedID);
		GeneratedNetworkCode._Write_KillType(writer, killedType);
		ClientRpcSender.Send(this, 3, writer, Channel.Reliable, excludeOwner: false);
		writer.Release();
	}

	private static bool KillFeedFilter(KillType killedType, PersistentUnit attacker, PersistentUnit killed)
	{
		if (killed.unit.definition.value < PlayerSettings.killFeedMinValue)
		{
			return false;
		}
		switch (killedType.GetFilterLevel())
		{
		case PlayerSettings.KillFeedFilter.None:
			return false;
		default:
			return true;
		case PlayerSettings.KillFeedFilter.Player:
			if (!IsLocalPlayer(attacker))
			{
				return IsLocalPlayer(killed);
			}
			return true;
		case PlayerSettings.KillFeedFilter.Friendly:
			if (!IsFriendlyFaction(attacker))
			{
				return IsFriendlyFaction(killed);
			}
			return true;
		case PlayerSettings.KillFeedFilter.Enemy:
			if (!IsEnemyFaction(attacker))
			{
				return IsEnemyFaction(killed);
			}
			return true;
		}
		static bool IsEnemyFaction(PersistentUnit unit)
		{
			if (unit != null && unit.HasHQ(out var hq) && GameManager.GetLocalHQ(out var localHq))
			{
				return hq != localHq;
			}
			return false;
		}
		static bool IsFriendlyFaction(PersistentUnit unit)
		{
			if (unit != null)
			{
				return GameManager.IsLocalHQ(unit.GetHQ());
			}
			return false;
		}
		static bool IsLocalPlayer(PersistentUnit unit)
		{
			if (unit != null)
			{
				return GameManager.IsLocalPlayer(unit.player);
			}
			return false;
		}
	}

	[ClientRpc]
	public void RpcAllHQMessage(string message)
	{
		if (ClientRpcSender.ShouldInvokeLocally(this, RpcTarget.Observers, null, excludeOwner: false))
		{
			UserCode_RpcAllHQMessage_913698537(message);
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteString(message);
		ClientRpcSender.Send(this, 4, writer, Channel.Reliable, excludeOwner: false);
		writer.Release();
	}

	[ClientRpc]
	public void RpcHQMessage(FactionHQ HQ, string message)
	{
		if (ClientRpcSender.ShouldInvokeLocally(this, RpcTarget.Observers, null, excludeOwner: false))
		{
			UserCode_RpcHQMessage_324162117(HQ, message);
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		GeneratedNetworkCode._Write_FactionHQ(writer, HQ);
		writer.WriteString(message);
		ClientRpcSender.Send(this, 5, writer, Channel.Reliable, excludeOwner: false);
		writer.Release();
	}

	private void HQMessageInternal(FactionHQ HQ, string message)
	{
		SoundManager.PlayInterfaceOneShot(GameAssets.i.radioStatic);
		SceneSingleton<GameplayUI>.i.GameMessage(message, 0.5f);
		if (message.Contains("Tactical"))
		{
			MusicManager.i.QueueMusicClip(NetworkSceneSingleton<LevelInfo>.i.LoadedMapSettings.GetTacticalMusic(HQ.faction), 2f);
		}
		if (message.Contains("Strategic"))
		{
			MusicManager.i.QueueMusicClip(NetworkSceneSingleton<LevelInfo>.i.LoadedMapSettings.GetStrategicMusic(HQ.faction), 2f);
		}
	}

	[ClientRpc]
	public void RpcPilotCaptureMessage(PersistentID pilotID, PersistentID capturerID, bool rescued)
	{
		if (ClientRpcSender.ShouldInvokeLocally(this, RpcTarget.Observers, null, excludeOwner: false))
		{
			UserCode_RpcPilotCaptureMessage_1554742742(pilotID, capturerID, rescued);
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		GeneratedNetworkCode._Write_PersistentID(writer, pilotID);
		GeneratedNetworkCode._Write_PersistentID(writer, capturerID);
		writer.WriteBooleanExtension(rescued);
		ClientRpcSender.Send(this, 6, writer, Channel.Reliable, excludeOwner: false);
		writer.Release();
	}

	[ClientRpc]
	public void RpcRepairMessage(PersistentID id)
	{
		if (ClientRpcSender.ShouldInvokeLocally(this, RpcTarget.Observers, null, excludeOwner: false))
		{
			UserCode_RpcRepairMessage_1444052622(id);
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		GeneratedNetworkCode._Write_PersistentID(writer, id);
		ClientRpcSender.Send(this, 7, writer, Channel.Reliable, excludeOwner: false);
		writer.Release();
	}

	[ClientRpc]
	public void RpcWarheadDestroyedMessage(Airbase airbase, FactionHQ hq, int number)
	{
		if (ClientRpcSender.ShouldInvokeLocally(this, RpcTarget.Observers, null, excludeOwner: false))
		{
			UserCode_RpcWarheadDestroyedMessage__002D662431755(airbase, hq, number);
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		GeneratedNetworkCode._Write_Airbase(writer, airbase);
		GeneratedNetworkCode._Write_FactionHQ(writer, hq);
		writer.WritePackedInt32(number);
		ClientRpcSender.Send(this, 8, writer, Channel.Reliable, excludeOwner: false);
		writer.Release();
	}

	private static Color ColorFromFaction(FactionHQ hq)
	{
		if (hq == null)
		{
			return ThemeManager.Active.ColorTheme.ChatNeutral;
		}
		if (GameManager.GetLocalHQ(out var localHq))
		{
			if (!(localHq == hq))
			{
				return ThemeManager.Active.ColorTheme.ChatHostile;
			}
			return ThemeManager.Active.ColorTheme.ChatFriendly;
		}
		return hq.faction.color;
	}

	private void MirageProcessed()
	{
	}

	public void UserCode_RpcPlayerJoinFactionMessage_156835807(Player player, FactionHQ hq)
	{
		if (!GameManager.IsLocalPlayer(player))
		{
			Color color = ColorFromFaction(hq);
			string text = ((player != null) ? player.GetDisplayName(PlayerNameContext.ChatOrLeaderboard) : "").AddColor(color);
			string text2 = hq.faction.factionExtendedName.AddColor(color);
			string message = text + " joined " + text2;
			SceneSingleton<GameplayUI>.i.GameMessage(message);
		}
	}

	protected static void Skeleton_RpcPlayerJoinFactionMessage_156835807(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((MessageManager)behaviour).UserCode_RpcPlayerJoinFactionMessage_156835807(GeneratedNetworkCode._Read_NuclearOption_002ENetworking_002EPlayer(reader), GeneratedNetworkCode._Read_FactionHQ(reader));
	}

	public void UserCode_TargetCreditMessage_106951341(INetworkPlayer _, PersistentID killedID, float creditAwarded, FactionHQ.RewardType actionType)
	{
		if (!PlayerSettings.cinematicMode)
		{
			PersistentUnit persistentUnit = null;
			if (killedID.IsValid)
			{
				UnitRegistry.TryGetPersistentUnit(killedID, out persistentUnit);
			}
			if (SceneSingleton<KillDisplay>.i == null)
			{
				UnityEngine.Object.Instantiate(GameAssets.i.killDisplay, SceneSingleton<GameplayUI>.i.gameplayCanvas.transform);
			}
			SceneSingleton<KillDisplay>.i.DisplayKill(persistentUnit, creditAwarded, actionType);
		}
	}

	protected static void Skeleton_TargetCreditMessage_106951341(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((MessageManager)behaviour).UserCode_TargetCreditMessage_106951341(behaviour.Client.Player, GeneratedNetworkCode._Read_PersistentID(reader), reader.ReadSingleConverter(), GeneratedNetworkCode._Read_FactionHQ_002FRewardType(reader));
	}

	public void UserCode_RpcBombFailMessage__002D1758898816(PersistentID bombID, float gForce)
	{
		if (UnitRegistry.TryGetPersistentUnit(bombID, out var persistentUnit))
		{
			Color color = ColorFromFaction(persistentUnit.GetHQ());
			SceneSingleton<GameplayUI>.i.GameMessage($"{persistentUnit.unitName.AddColor(color)} failed on {gForce:F0}g impact");
		}
	}

	protected static void Skeleton_RpcBombFailMessage__002D1758898816(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((MessageManager)behaviour).UserCode_RpcBombFailMessage__002D1758898816(GeneratedNetworkCode._Read_PersistentID(reader), reader.ReadSingleConverter());
	}

	public void UserCode_RpcKillMessage_635947223(PersistentID killerID, PersistentID killedID, KillType killedType)
	{
		if (!UnitRegistry.TryGetPersistentUnit(killedID, out var persistentUnit))
		{
			return;
		}
		PersistentUnit persistentUnit2;
		bool flag = UnitRegistry.TryGetPersistentUnit(killerID, out persistentUnit2);
		if (KillFeedFilter(killedType, persistentUnit2, persistentUnit))
		{
			Color color = ColorFromFaction(persistentUnit.GetHQ());
			string verb = killedType.GetVerb(flag);
			if (!flag)
			{
				string message = persistentUnit.unitName.AddColor(color) + " " + verb;
				SceneSingleton<GameplayUI>.i.KillFeed(message);
				return;
			}
			Color color2 = ColorFromFaction(persistentUnit2.GetHQ());
			string message2 = persistentUnit2.unitName.AddColor(color2) + " " + verb + " " + persistentUnit.unitName.AddColor(color);
			SceneSingleton<GameplayUI>.i.KillFeed(message2);
		}
	}

	protected static void Skeleton_RpcKillMessage_635947223(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((MessageManager)behaviour).UserCode_RpcKillMessage_635947223(GeneratedNetworkCode._Read_PersistentID(reader), GeneratedNetworkCode._Read_PersistentID(reader), GeneratedNetworkCode._Read_KillType(reader));
	}

	public void UserCode_RpcAllHQMessage_913698537(string message)
	{
		if (GameManager.GetLocalHQ(out var localHq))
		{
			HQMessageInternal(localHq, message);
		}
	}

	protected static void Skeleton_RpcAllHQMessage_913698537(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((MessageManager)behaviour).UserCode_RpcAllHQMessage_913698537(reader.ReadString());
	}

	public void UserCode_RpcHQMessage_324162117(FactionHQ HQ, string message)
	{
		if (GameManager.IsLocalHQ(HQ))
		{
			HQMessageInternal(HQ, message);
		}
	}

	protected static void Skeleton_RpcHQMessage_324162117(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((MessageManager)behaviour).UserCode_RpcHQMessage_324162117(GeneratedNetworkCode._Read_FactionHQ(reader), reader.ReadString());
	}

	public void UserCode_RpcPilotCaptureMessage_1554742742(PersistentID pilotID, PersistentID capturerID, bool rescued)
	{
		if (UnitRegistry.TryGetPersistentUnit(pilotID, out var persistentUnit) && UnitRegistry.TryGetPersistentUnit(capturerID, out var persistentUnit2))
		{
			FactionHQ hQ = persistentUnit.GetHQ();
			GameManager.GetLocalHQ(out var localHq);
			if (!(localHq != null) || !(localHq != hQ) || localHq.IsTargetBeingTracked(persistentUnit.unit))
			{
				Color color = ColorFromFaction(hQ);
				Color color2 = ColorFromFaction(persistentUnit2.GetHQ());
				string text = (rescued ? "rescued" : "captured");
				string message = persistentUnit.unitName.AddColor(color) + " was " + text + " by " + persistentUnit2.unitName.AddColor(color2);
				SceneSingleton<GameplayUI>.i.KillFeed(message);
			}
		}
	}

	protected static void Skeleton_RpcPilotCaptureMessage_1554742742(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((MessageManager)behaviour).UserCode_RpcPilotCaptureMessage_1554742742(GeneratedNetworkCode._Read_PersistentID(reader), GeneratedNetworkCode._Read_PersistentID(reader), reader.ReadBooleanExtension());
	}

	public void UserCode_RpcRepairMessage_1444052622(PersistentID id)
	{
		if (UnitRegistry.TryGetPersistentUnit(id, out var persistentUnit))
		{
			FactionHQ hQ = persistentUnit.GetHQ();
			GameManager.GetLocalHQ(out var localHq);
			if (!(localHq != null) || !(localHq != hQ) || localHq.IsTargetBeingTracked(persistentUnit.unit))
			{
				Color color = ColorFromFaction(hQ);
				string message = persistentUnit.unitName.AddColor(color) + " was repaired";
				SceneSingleton<GameplayUI>.i.KillFeed(message);
			}
		}
	}

	protected static void Skeleton_RpcRepairMessage_1444052622(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((MessageManager)behaviour).UserCode_RpcRepairMessage_1444052622(GeneratedNetworkCode._Read_PersistentID(reader));
	}

	public void UserCode_RpcWarheadDestroyedMessage__002D662431755(Airbase airbase, FactionHQ hq, int number)
	{
		if (!(airbase == null) && hq != null && GameManager.IsLocalHQ(hq))
		{
			Color color = ColorFromFaction(hq);
			string arg = ((number > 1) ? "warheads" : "warhead");
			string message = $"Warning : {number} {arg} destroyed at {airbase.SavedAirbase.DisplayName.AddColor(color)}";
			HQMessageInternal(hq, message);
		}
	}

	protected static void Skeleton_RpcWarheadDestroyedMessage__002D662431755(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((MessageManager)behaviour).UserCode_RpcWarheadDestroyedMessage__002D662431755(GeneratedNetworkCode._Read_Airbase(reader), GeneratedNetworkCode._Read_FactionHQ(reader), reader.ReadPackedInt32());
	}

	protected override int GetRpcCount()
	{
		return 9;
	}

	protected override void RegisterRpc(RemoteCallCollection collection)
	{
		base.RegisterRpc(collection);
		collection.Register(0, "MessageManager.RpcPlayerJoinFactionMessage", cmdRequireAuthority: false, RpcInvokeType.ClientRpc, this, Skeleton_RpcPlayerJoinFactionMessage_156835807, RpcRateLimitConfig.Disabled());
		collection.Register(1, "MessageManager.TargetCreditMessage", cmdRequireAuthority: false, RpcInvokeType.ClientRpc, this, Skeleton_TargetCreditMessage_106951341, RpcRateLimitConfig.Disabled());
		collection.Register(2, "MessageManager.RpcBombFailMessage", cmdRequireAuthority: false, RpcInvokeType.ClientRpc, this, Skeleton_RpcBombFailMessage__002D1758898816, RpcRateLimitConfig.Disabled());
		collection.Register(3, "MessageManager.RpcKillMessage", cmdRequireAuthority: false, RpcInvokeType.ClientRpc, this, Skeleton_RpcKillMessage_635947223, RpcRateLimitConfig.Disabled());
		collection.Register(4, "MessageManager.RpcAllHQMessage", cmdRequireAuthority: false, RpcInvokeType.ClientRpc, this, Skeleton_RpcAllHQMessage_913698537, RpcRateLimitConfig.Disabled());
		collection.Register(5, "MessageManager.RpcHQMessage", cmdRequireAuthority: false, RpcInvokeType.ClientRpc, this, Skeleton_RpcHQMessage_324162117, RpcRateLimitConfig.Disabled());
		collection.Register(6, "MessageManager.RpcPilotCaptureMessage", cmdRequireAuthority: false, RpcInvokeType.ClientRpc, this, Skeleton_RpcPilotCaptureMessage_1554742742, RpcRateLimitConfig.Disabled());
		collection.Register(7, "MessageManager.RpcRepairMessage", cmdRequireAuthority: false, RpcInvokeType.ClientRpc, this, Skeleton_RpcRepairMessage_1444052622, RpcRateLimitConfig.Disabled());
		collection.Register(8, "MessageManager.RpcWarheadDestroyedMessage", cmdRequireAuthority: false, RpcInvokeType.ClientRpc, this, Skeleton_RpcWarheadDestroyedMessage__002D662431755, RpcRateLimitConfig.Disabled());
	}
}
