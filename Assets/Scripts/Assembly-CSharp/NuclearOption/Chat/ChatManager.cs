using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Mirage;
using Mirage.RemoteCalls;
using Mirage.Serialization;
using NuclearOption.Networking;
using UnityEngine;

namespace NuclearOption.Chat
{
	public class ChatManager : NetworkSceneSingleton<ChatManager>
	{
		public const int MAX_CHAT_MESSAGE_LENGTH = 128;

		[Header("Rate Limit")]
		[SerializeField]
		private int messageLimit = 5;

		[SerializeField]
		private float resetTime = 30f;

		[Header("Char Colors")]
		[SerializeField]
		private Color noFaction;

		[SerializeField]
		private Color alliedChat;

		[Tooltip("Changes faction saturation by this amount")]
		[SerializeField]
		private float allChatSaturation;

		private Dictionary<Player, RateLimiter> serverRateLimit = new Dictionary<Player, RateLimiter>();

		private RateLimiter localRateLimit;

		[NonSerialized]
		private const int SYNC_VAR_COUNT = 0;

		[NonSerialized]
		private const int RPC_COUNT = 4;

		public static void SendChatMessage(string message, bool allChat)
		{
			if (CanSend(message, checkAsServer: false, realSend: true))
			{
				NetworkSceneSingleton<ChatManager>.i.CmdSendChatMessage(message, allChat);
			}
		}

		public static bool CanSend(string message, bool checkAsServer, bool realSend)
		{
			if (!NetworkSceneSingleton<ChatManager>.i.ValidateChatMessageSize(message))
			{
				if (realSend)
				{
					throw new ArgumentException("Message size incorrect");
				}
				return false;
			}
			if (!GameManager.GetLocalPlayer<Player>(out var localPlayer))
			{
				Debug.LogError("Can't send because no local player");
				return false;
			}
			if (!NetworkSceneSingleton<ChatManager>.i.CheckRateLimit(localPlayer, checkAsServer, realSend))
			{
				if (realSend)
				{
					throw new ArgumentException("Message rate limit");
				}
				return false;
			}
			return true;
		}

		private bool ValidateChatMessageSize(string message)
		{
			if (string.IsNullOrEmpty(message))
			{
				return false;
			}
			if (message.Length > 128)
			{
				return false;
			}
			return true;
		}

		private bool CheckRateLimit(Player player, bool checkAsServer, bool setSendTime)
		{
			RateLimiter value;
			if (checkAsServer)
			{
				if (!serverRateLimit.TryGetValue(player, out value))
				{
					value = new RateLimiter(messageLimit, resetTime);
					serverRateLimit.Add(player, value);
				}
			}
			else
			{
				if (localRateLimit == null)
				{
					localRateLimit = new RateLimiter(messageLimit, resetTime);
				}
				value = localRateLimit;
			}
			float timeSinceLevelLoad = Time.timeSinceLevelLoad;
			if (value.ShouldLimit(timeSinceLevelLoad))
			{
				return false;
			}
			if (setSendTime)
			{
				value.OnSend(timeSinceLevelLoad);
			}
			return true;
		}

		[RateLimit(Refill = 5, MaxTokens = 15, Penalty = 1, Interval = 30f)]
		[ServerRpc(requireAuthority = false)]
		private void CmdSendChatMessage([MaxLength(128)] string message, bool allChat, INetworkPlayer sender = null)
		{
			if (ServerRpcSender.ShouldInvokeLocally(this, requireAuthority: false, allowServerToCall: false))
			{
				UserCode_CmdSendChatMessage__002D456754112(message, allChat, base.Server.LocalPlayer);
				return;
			}
			PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
			writer.WriteString(message, 128);
			writer.WriteBooleanExtension(allChat);
			ServerRpcSender.Send(this, 0, writer, Mirage.Channel.Reliable, requireAuthority: false);
			writer.Release();
		}

		[ClientRpc(target = RpcTarget.Player)]
		public void TargetReceiveMessage(INetworkPlayer _, string message, Player player, bool allChat)
		{
			if (ClientRpcSender.ShouldInvokeLocally(this, RpcTarget.Player, _, excludeOwner: false))
			{
				UserCode_TargetReceiveMessage_1307761090(base.Client.Player, message, player, allChat);
				return;
			}
			PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
			writer.WriteString(message);
			GeneratedNetworkCode._Write_NuclearOption_002ENetworking_002EPlayer(writer, player);
			writer.WriteBooleanExtension(allChat);
			ClientRpcSender.SendTarget(this, 1, writer, Mirage.Channel.Reliable, _);
			writer.Release();
		}

		[ClientRpc]
		public void RpcServerMessage(string message, bool runTtsIfEnabled)
		{
			if (ClientRpcSender.ShouldInvokeLocally(this, RpcTarget.Observers, null, excludeOwner: false))
			{
				UserCode_RpcServerMessage_1244201393(message, runTtsIfEnabled);
			}
			PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
			writer.WriteString(message);
			writer.WriteBooleanExtension(runTtsIfEnabled);
			ClientRpcSender.Send(this, 2, writer, Mirage.Channel.Reliable, excludeOwner: false);
			writer.Release();
		}

		[ClientRpc(target = RpcTarget.Player)]
		public void RpcTargetServerMessage(INetworkPlayer _, string message, bool runTtsIfEnabled)
		{
			if (ClientRpcSender.ShouldInvokeLocally(this, RpcTarget.Player, _, excludeOwner: false))
			{
				UserCode_RpcTargetServerMessage__002D537879009(base.Client.Player, message, runTtsIfEnabled);
				return;
			}
			PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
			writer.WriteString(message);
			writer.WriteBooleanExtension(runTtsIfEnabled);
			ClientRpcSender.SendTarget(this, 3, writer, Mirage.Channel.Reliable, _);
			writer.Release();
		}

		private async UniTask RunTTS(string playerName, string message)
		{
			await WindowsTTS.SpeakAsync(PlayerSettings.chatTtsSpeed, PlayerSettings.chatTtsVolume, playerName + " said: " + message, PlayerSettings.chatFilter);
		}

		private Color GetTextColor(FactionHQ senderHQ, bool allChat)
		{
			if (senderHQ == null)
			{
				return noFaction;
			}
			if (allChat)
			{
				Color.RGBToHSV(senderHQ.faction.color, out var H, out var S, out var V);
				return Color.HSVToRGB(H, S * allChatSaturation, V);
			}
			return alliedChat;
		}

		private void MirageProcessed()
		{
		}

		private void UserCode_CmdSendChatMessage__002D456754112(string message, bool allChat, INetworkPlayer sender)
		{
			if (!ValidateChatMessageSize(message))
			{
				sender.SetError(1, NuclearOptionPlayerErrorFlags.MessagingSpam);
				return;
			}
			if (!sender.TryGetPlayer<Player>(out var player))
			{
				Debug.LogWarning("CmdSendChatMessage called without a player, disconnecting player");
				sender.SetError(100, PlayerErrorFlags.Critical);
				sender.Disconnect();
				return;
			}
			if (!CheckRateLimit(player, checkAsServer: true, setSendTime: true))
			{
				sender.SetError(1, NuclearOptionPlayerErrorFlags.MessagingSpam);
				return;
			}
			ColorLog<ChatManager>.Info($"CmdSendChatMessage allChat:{allChat} {sender} {player.SteamID} {message}");
			foreach (Player value in UnitRegistry.playerLookup.Values)
			{
				/*
				if (allChat || value.HQ == player.HQ)
				{
					INetworkPlayer owner = value.Owner;
					TargetReceiveMessage(owner, message, player, allChat);
				}*/
			}
		}

		protected static void Skeleton_CmdSendChatMessage__002D456754112(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
		{
			((ChatManager)behaviour).UserCode_CmdSendChatMessage__002D456754112(reader.ReadString(128), reader.ReadBooleanExtension(), senderConnection);
		}

		public void UserCode_TargetReceiveMessage_1307761090(INetworkPlayer _, string message, Player player, bool allChat)
		{
			if (!PlayerSettings.chatEnabled)
			{
				return;
			}
			if (GameManager.MutedList.Contains(player) || GameManager.BlockList.Contains(player))
			{
				Debug.Log($"Ignoring message from muted player '{player}'");
				return;
			}
			string displayName = player.GetDisplayName(PlayerNameContext.ChatOrLeaderboard);
			message = message.ProfanityFilter();
			displayName = displayName.ProfanityFilter();
			//FactionHQ hQ = player.HQ;
			//Color textColor = GetTextColor(hQ, allChat);
			//string text = ((allChat ? "" : "(ally)") + displayName + ":").AddColor(textColor);
			string text2 = message.SanitizeRichText(128);
			//SceneSingleton<GameplayUI>.i.GameMessage(text + " " + text2);
			if (PlayerSettings.chatTts)
			{
				RunTTS(displayName, message).Forget();
			}
		}

		protected static void Skeleton_TargetReceiveMessage_1307761090(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
		{
			((ChatManager)behaviour).UserCode_TargetReceiveMessage_1307761090(behaviour.Client.Player, reader.ReadString(), GeneratedNetworkCode._Read_NuclearOption_002ENetworking_002EPlayer(reader), reader.ReadBooleanExtension());
		}

		public void UserCode_RpcServerMessage_1244201393(string message, bool runTtsIfEnabled)
		{
			if (PlayerSettings.chatEnabled)
			{
				message = message.ProfanityFilter();
				ColorLog<ChatManager>.Info("RpcServerMessage " + message);
				SceneSingleton<GameplayUI>.i.GameMessage(message);
				if (PlayerSettings.chatTts && runTtsIfEnabled)
				{
					RunTTS("server", message).Forget();
				}
			}
		}

		protected static void Skeleton_RpcServerMessage_1244201393(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
		{
			((ChatManager)behaviour).UserCode_RpcServerMessage_1244201393(reader.ReadString(), reader.ReadBooleanExtension());
		}

		public void UserCode_RpcTargetServerMessage__002D537879009(INetworkPlayer _, string message, bool runTtsIfEnabled)
		{
			if (PlayerSettings.chatEnabled)
			{
				message = message.ProfanityFilter();
				ColorLog<ChatManager>.Info("RpcTargetServerMessage " + message);
				SceneSingleton<GameplayUI>.i.GameMessage(message);
				if (PlayerSettings.chatTts && runTtsIfEnabled)
				{
					RunTTS("server", message).Forget();
				}
			}
		}

		protected static void Skeleton_RpcTargetServerMessage__002D537879009(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
		{
			((ChatManager)behaviour).UserCode_RpcTargetServerMessage__002D537879009(behaviour.Client.Player, reader.ReadString(), reader.ReadBooleanExtension());
		}

		protected override int GetRpcCount()
		{
			return 4;
		}

		protected override void RegisterRpc(RemoteCallCollection collection)
		{
			base.RegisterRpc(collection);
			collection.Register(0, "NuclearOption.Chat.ChatManager.CmdSendChatMessage", cmdRequireAuthority: false, RpcInvokeType.ServerRpc, this, Skeleton_CmdSendChatMessage__002D456754112, RpcRateLimitConfig.Enabled(30f, 5, 15, 1));
			collection.Register(1, "NuclearOption.Chat.ChatManager.TargetReceiveMessage", cmdRequireAuthority: false, RpcInvokeType.ClientRpc, this, Skeleton_TargetReceiveMessage_1307761090, RpcRateLimitConfig.Disabled());
			collection.Register(2, "NuclearOption.Chat.ChatManager.RpcServerMessage", cmdRequireAuthority: false, RpcInvokeType.ClientRpc, this, Skeleton_RpcServerMessage_1244201393, RpcRateLimitConfig.Disabled());
			collection.Register(3, "NuclearOption.Chat.ChatManager.RpcTargetServerMessage", cmdRequireAuthority: false, RpcInvokeType.ClientRpc, this, Skeleton_RpcTargetServerMessage__002D537879009, RpcRateLimitConfig.Disabled());
		}
	}
}
