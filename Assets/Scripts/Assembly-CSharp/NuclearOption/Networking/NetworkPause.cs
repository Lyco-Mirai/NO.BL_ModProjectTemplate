using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Mirage;
using Mirage.Serialization;
using NuclearOption.DedicatedServer;
using UnityEngine;

namespace NuclearOption.Networking
{
	public class NetworkPause : NetworkSceneSingleton<NetworkPause>
	{
		public GameObject overlay;

		[SyncVar(hook = "HookPausedChanged", invokeHookOnServer = true)]
		private bool serverPaused;

		private bool localPaused;

		private DedicatedServerManager serverManager;

		[NonSerialized]
		private const int SYNC_VAR_COUNT = 1;

		[NonSerialized]
		private const int RPC_COUNT = 0;

		public bool NetworkserverPaused
		{
			get
			{
				return serverPaused;
			}
			set
			{
				if (!SyncVarEqual(value, serverPaused))
				{
					bool flag = serverPaused;
					serverPaused = value;
					SetDirtyBit(1uL);
					if (!GetSyncVarHookGuard(1uL) && base.IsServer)
					{
						SetSyncVarHookGuard(1uL, value: true);
						HookPausedChanged();
						SetSyncVarHookGuard(1uL, value: false);
					}
				}
			}
		}

		protected override void Awake()
		{
			if (overlay != null)
			{
				overlay.SetActive(value: false);
			}
			base.Awake();
			base.Identity.OnStartServer.AddListener(OnStartServer);
		}

		private void OnDestroy()
		{
			if (localPaused)
			{
				TimeScaleManager.Scale = 1f;
				localPaused = false;
			}
		}

		private void OnStartServer()
		{
			serverManager = NetworkManagerNuclearOption.i.DedicatedServerManager;
			if (DedicatedServerManager.IsRunning)
			{
				NoPlayerCheck().Forget();
			}
		}

		private void HookPausedChanged()
		{
			if (overlay != null)
			{
				overlay.SetActive(serverPaused);
			}
			TimeScaleManager.Scale = ((!serverPaused) ? 1 : 0);
			localPaused = serverPaused;
		}

		private async UniTask NoPlayerCheck()
		{
			ColorLog<NetworkPause>.Info("Starting NetworkPause server loop");
			CancellationToken cancel = base.destroyCancellationToken;
			while (!cancel.IsCancellationRequested)
			{
				await UniTask.Delay(1000, ignoreTimeScale: true);
				bool flag = !serverManager.HasPlayers();
				if (serverPaused != flag)
				{
					NetworkserverPaused = flag;
					ColorLog<NetworkPause>.Info($"Toggling Pause => {serverPaused}");
				}
			}
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
				writer.WriteBooleanExtension(serverPaused);
				return true;
			}
			writer.Write(syncVarDirtyBits, 1);
			if ((syncVarDirtyBits & 1L) != 0L)
			{
				writer.WriteBooleanExtension(serverPaused);
				result = true;
			}
			return result;
		}

		public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
		{
			base.DeserializeSyncVars(reader, initialState);
			if (initialState)
			{
				bool value = serverPaused;
				serverPaused = reader.ReadBooleanExtension();
				if (!SyncVarEqual(value, serverPaused))
				{
					HookPausedChanged();
				}
				return;
			}
			ulong num = reader.Read(1);
			SetDeserializeMask(num, 0);
			if ((num & 1L) != 0L)
			{
				bool value2 = serverPaused;
				serverPaused = reader.ReadBooleanExtension();
				if (!SyncVarEqual(value2, serverPaused))
				{
					HookPausedChanged();
				}
			}
		}

		protected override int GetRpcCount()
		{
			return 0;
		}
	}
}
