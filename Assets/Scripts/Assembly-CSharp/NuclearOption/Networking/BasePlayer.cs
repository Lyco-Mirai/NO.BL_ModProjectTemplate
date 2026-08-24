using System;
using System.Runtime.CompilerServices;
using Mirage;
using Mirage.Serialization;
using NuclearOption.Networking.Authentication;
using Steamworks;

namespace NuclearOption.Networking
{
	public abstract class BasePlayer : NetworkBehaviour
	{
		[CompilerGenerated]
		[SyncVar(initialOnly = true)]
		private ulong _003CSteamID_003Ek__BackingField;

		[NonSerialized]
		private const int SYNC_VAR_COUNT = 1;

		[NonSerialized]
		private const int RPC_COUNT = 0;

		public ulong SteamID
		{
			[CompilerGenerated]
			get
			{
				return _003CSteamID_003Ek__BackingField;
			}
			[CompilerGenerated]
			private set
			{
				Network_003CSteamID_003Ek__BackingField = value;
			}
		}

		public CSteamID CSteamID => new CSteamID(SteamID);

		public ulong Network_003CSteamID_003Ek__BackingField
		{
			get
			{
				return SteamID;
			}
			set
			{
				SteamID = value;
			}
		}

		protected virtual void Awake()
		{
			base.transform.SetParent(Datum.origin);
			base.Identity.OnStartServer.AddListener(OnStartServer);
			base.Identity.OnStartClient.AddListener(OnStartClient);
		}

		protected virtual void OnStartServer()
		{
			NetworkAuthenticatorNuclearOption.AuthData authData = base.Owner.GetAuthData();
			if (authData.UsingSteamTransport)
			{
				Network_003CSteamID_003Ek__BackingField = authData.SteamID.m_SteamID;
			}
			else if (base.Owner.IsHost && SteamManager.ClientInitialized)
			{
				Network_003CSteamID_003Ek__BackingField = SteamUser.GetSteamID().m_SteamID;
			}
		}

		private void OnStartClient()
		{
			if (!base.IsServer)
			{
				ColorLog<BasePlayer>.Info("Client local player spawned");
			}
		}

		public NetworkAuthenticatorNuclearOption.AuthData GetAuthData()
		{
			return base.Owner.GetAuthData();
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
				writer.WritePackedUInt64(SteamID);
				return true;
			}
			writer.Write(syncVarDirtyBits, 1);
			return result;
		}

		public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
		{
			base.DeserializeSyncVars(reader, initialState);
			if (initialState)
			{
				SteamID = reader.ReadPackedUInt64();
				return;
			}
			ulong dirtyBit = reader.Read(1);
			SetDeserializeMask(dirtyBit, 0);
		}

		protected override int GetRpcCount()
		{
			return 0;
		}
	}
}
