using System;
using System.Collections.Generic;
using Mirage;
using Mirage.RemoteCalls;
using Mirage.Serialization;
using UnityEngine;

public class MissionMessages : NetworkSceneSingleton<MissionMessages>
{
	private struct DialogueItem
	{
		public int id;

		public string title;

		public string body;

		public string button;

		public FactionHQ filterFaction;

		public Action callback;
	}

	[NetworkMessage]
	public struct ActiveDialogueState
	{
		public int id;

		public string title;

		public string body;

		public string button;

		public FactionHQ filterFaction;
	}

	private int lastPlayedFrame;

	private DialogueBox _dialogueBox;

	private int activeDialogueId;

	private readonly Queue<DialogueItem> dialogueQueue = new Queue<DialogueItem>();

	[SyncVar(hook = "ActiveDialogueChanged")]
	private ActiveDialogueState activeDialogue;

	[NonSerialized]
	private const int SYNC_VAR_COUNT = 1;

	[NonSerialized]
	private const int RPC_COUNT = 2;

	public ActiveDialogueState NetworkactiveDialogue
	{
		get
		{
			return activeDialogue;
		}
		set
		{
			if (!SyncVarEqual(value, activeDialogue))
			{
				ActiveDialogueState activeDialogueState = activeDialogue;
				activeDialogue = value;
				SetDirtyBit(1uL);
				if (!GetSyncVarHookGuard(1uL) && base.IsHost)
				{
					SetSyncVarHookGuard(1uL, value: true);
					ActiveDialogueChanged();
					SetSyncVarHookGuard(1uL, value: false);
				}
			}
		}
	}

	public static void ShowMessage(string message, bool playsound, FactionHQ faction, bool sendToClients)
	{
		if (!string.IsNullOrEmpty(message))
		{
			NetworkSceneSingleton<MissionMessages>.i.ShowMessgeLocal(message, playsound, faction);
			if (sendToClients)
			{
				NetworkSceneSingleton<MissionMessages>.i.RpcShowMessage(message, playsound, faction);
			}
		}
	}

	private void ShowMessgeLocal(string message, bool playsound, FactionHQ filterFaction)
	{
		if (IsLocalFaction(filterFaction))
		{
			SceneSingleton<GameplayUI>.i.GameMessage(message);
			if (playsound)
			{
				NetworkSceneSingleton<MissionMessages>.i.PlaySound();
			}
		}
	}

	[ClientRpc]
	private void RpcShowMessage(string message, bool playsound, FactionHQ faction)
	{
		if (ClientRpcSender.ShouldInvokeLocally(this, RpcTarget.Observers, null, excludeOwner: false))
		{
			UserCode_RpcShowMessage__002D186615428(message, playsound, faction);
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteString(message);
		writer.WriteBooleanExtension(playsound);
		GeneratedNetworkCode._Write_FactionHQ(writer, faction);
		ClientRpcSender.Send(this, 0, writer, Channel.Reliable, excludeOwner: false);
		writer.Release();
	}

	private void PlaySound()
	{
		int frameCount = Time.frameCount;
		if (frameCount != lastPlayedFrame)
		{
			lastPlayedFrame = frameCount;
			SoundManager.PlayInterfaceOneShot(GameAssets.i.radioStatic);
		}
	}

	private static bool IsLocalFaction(FactionHQ filterFaction)
	{
		if (filterFaction == null)
		{
			return true;
		}
		if (GameManager.GetLocalHQ(out var localHq))
		{
			return filterFaction == localHq;
		}
		return false;
	}

	public void ShowDialogue(string title, string body, string button, FactionHQ filterFaction, Action onDialogueComplete)
	{
		activeDialogueId++;
		DialogueItem item = new DialogueItem
		{
			id = activeDialogueId,
			title = title,
			body = body,
			button = button,
			filterFaction = filterFaction,
			callback = onDialogueComplete
		};
		dialogueQueue.Enqueue(item);
		if (dialogueQueue.Count == 1)
		{
			NetworkactiveDialogue = new ActiveDialogueState
			{
				id = item.id,
				title = item.title,
				body = item.body,
				button = item.button,
				filterFaction = item.filterFaction
			};
		}
	}

	public void ActiveDialogueChanged()
	{
		if (!GameManager.IsHeadless)
		{
			if (activeDialogue.id == 0)
			{
				HideDialogueBox();
			}
			else if (IsLocalFaction(activeDialogue.filterFaction))
			{
				ShowDialogueBox(activeDialogue.id, activeDialogue.title, activeDialogue.body, activeDialogue.button);
			}
			else
			{
				HideDialogueBox();
			}
		}
	}

	private void ShowDialogueBox(int id, string title, string body, string button)
	{
		if (_dialogueBox == null)
		{
			_dialogueBox = SceneSingleton<GameplayUI>.i.DialogueBox;
			_dialogueBox.ButtonPressed += DialogueBox_ButtonPressed;
		}
		_dialogueBox.Show(id, title, body, button);
	}

	private void HideDialogueBox()
	{
		if (_dialogueBox != null)
		{
			_dialogueBox.Hide();
		}
	}

	private void DialogueBox_ButtonPressed(int id)
	{
		CmdDialogueButton(id);
	}

	[RateLimit(Refill = 2, MaxTokens = 10, Penalty = 5)]
	[ServerRpc(requireAuthority = false)]
	private void CmdDialogueButton(int id)
	{
		if (ServerRpcSender.ShouldInvokeLocally(this, requireAuthority: false, allowServerToCall: false))
		{
			UserCode_CmdDialogueButton__002D61528126(id);
			return;
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WritePackedInt32(id);
		ServerRpcSender.Send(this, 1, writer, Channel.Reliable, requireAuthority: false);
		writer.Release();
	}

	private void MirageProcessed()
	{
	}

	public override bool SerializeSyncVars(NetworkWriter writer, bool initialize)
	{
		ulong syncVarDirtyBits = base.SyncVarDirtyBits;
		bool result = base.SerializeSyncVars(writer, initialize);
		if (initialize)
		{
			GeneratedNetworkCode._Write_MissionMessages_002FActiveDialogueState(writer, activeDialogue);
			return true;
		}
		writer.Write(syncVarDirtyBits, 1);
		if ((syncVarDirtyBits & 1L) != 0L)
		{
			GeneratedNetworkCode._Write_MissionMessages_002FActiveDialogueState(writer, activeDialogue);
			result = true;
		}
		return result;
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			ActiveDialogueState value = activeDialogue;
			activeDialogue = GeneratedNetworkCode._Read_MissionMessages_002FActiveDialogueState(reader);
			if (!base.IsServer && !SyncVarEqual(value, activeDialogue))
			{
				ActiveDialogueChanged();
			}
			return;
		}
		ulong num = reader.Read(1);
		SetDeserializeMask(num, 0);
		if ((num & 1L) != 0L)
		{
			ActiveDialogueState value2 = activeDialogue;
			activeDialogue = GeneratedNetworkCode._Read_MissionMessages_002FActiveDialogueState(reader);
			if (!base.IsServer && !SyncVarEqual(value2, activeDialogue))
			{
				ActiveDialogueChanged();
			}
		}
	}

	private void UserCode_RpcShowMessage__002D186615428(string message, bool playsound, FactionHQ faction)
	{
		if (!base.IsServer)
		{
			ShowMessgeLocal(message, playsound, faction);
		}
	}

	protected static void Skeleton_RpcShowMessage__002D186615428(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((MissionMessages)behaviour).UserCode_RpcShowMessage__002D186615428(reader.ReadString(), reader.ReadBooleanExtension(), GeneratedNetworkCode._Read_FactionHQ(reader));
	}

	private void UserCode_CmdDialogueButton__002D61528126(int id)
	{
		if (dialogueQueue.TryPeek(out var result) && result.id == id)
		{
			dialogueQueue.Dequeue();
			result.callback?.Invoke();
			if (dialogueQueue.TryPeek(out var result2))
			{
				NetworkactiveDialogue = new ActiveDialogueState
				{
					id = result2.id,
					title = result2.title,
					body = result2.body,
					button = result2.button,
					filterFaction = result2.filterFaction
				};
			}
			else
			{
				NetworkactiveDialogue = default(ActiveDialogueState);
			}
		}
	}

	protected static void Skeleton_CmdDialogueButton__002D61528126(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((MissionMessages)behaviour).UserCode_CmdDialogueButton__002D61528126(reader.ReadPackedInt32());
	}

	protected override int GetRpcCount()
	{
		return 2;
	}

	protected override void RegisterRpc(RemoteCallCollection collection)
	{
		base.RegisterRpc(collection);
		collection.Register(0, "MissionMessages.RpcShowMessage", cmdRequireAuthority: false, RpcInvokeType.ClientRpc, this, Skeleton_RpcShowMessage__002D186615428, RpcRateLimitConfig.Disabled());
		collection.Register(1, "MissionMessages.CmdDialogueButton", cmdRequireAuthority: false, RpcInvokeType.ServerRpc, this, Skeleton_CmdDialogueButton__002D61528126, RpcRateLimitConfig.Enabled(1f, 2, 10, 5));
	}
}
