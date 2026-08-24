using System;
using Mirage;
using Mirage.Serialization;
using UnityEngine;

namespace NuclearOption.SceneLoading
{
	public class NetworkMap : MonoBehaviour
	{
		private const int INDEX_SIZE = 24;

		private static readonly int indexMask = (int)BitMask.Mask(24);

		private static readonly int prefixMask = (int)BitMask.Mask(8);

		[Tooltip("Value that will be used for largest byte in PrefabHash")]
		public int MapPrefix = 1;

		public NetworkIdentity[] NetworkObjects;

		private bool clientRegistered;

		private ClientObjectManager clientObjectManager;

		private void OnValidate()
		{
			if (MapPrefix == 0)
			{
				Debug.LogError("MapPrefix can't be zero");
				MapPrefix = 1;
			}
		}

		private void OnDestroy()
		{
			if (clientRegistered)
			{
				ColorLog<NetworkMap>.Info("Unregister from destroy");
				ClientUnregister(clientObjectManager);
			}
		}

		public void ServerSpawn(ServerObjectManager manager)
		{
			ColorLog<NetworkMap>.Info("Server Spawn scene objects");
			ForeachObject(delegate(NetworkIdentity identity, int id)
			{
				manager.Spawn(identity, id);
			});
		}

		public void ServerUnspawn(ServerObjectManager manager)
		{
			ColorLog<NetworkMap>.Info("Server Unspawn scene objects");
			ForeachObject(delegate(NetworkIdentity identity, int id)
			{
				manager.Destroy(identity, destroyServerObject: false);
			});
		}

		public void ClientRegister(ClientObjectManager manager)
		{
			ColorLog<NetworkMap>.Info("Register scene objects");
			if (clientRegistered)
			{
				throw new InvalidOperationException("NetworkMap.ClientRegister already called");
			}
			clientRegistered = true;
			clientObjectManager = manager;
			ForeachObject(delegate(NetworkIdentity identity, int id)
			{
				manager.RegisterSpawnHandler(id, (SpawnHandlerDelegate)ClientSpawn, (UnSpawnDelegate)ClientUnspawn);
				NetworkIdentity ClientSpawn(SpawnMessage message)
				{
					if (identity.NetId != 0)
					{
						throw new Exception($"Map object ({identity}) is already spawned and can not be enabled by NetworkMap");
					}
					return identity;
				}
			});
			static void ClientUnspawn(NetworkIdentity identity)
			{
			}
		}

		public void ClientUnregister(ClientObjectManager manager)
		{
			ColorLog<NetworkMap>.Info("Unregister scene objects");
			if (!clientRegistered)
			{
				ColorLog<NetworkMap>.InfoWarn("ClientUnregister called when map was not registered");
				return;
			}
			clientRegistered = false;
			ForeachObject(delegate(NetworkIdentity _, int id)
			{
				manager.UnregisterSpawnHandler(id);
			});
		}

		private void ForeachObject(Action<NetworkIdentity, int> callback)
		{
			for (int i = 0; i < NetworkObjects.Length; i++)
			{
				int arg = (MapPrefix << 24) | (i & indexMask);
				callback(NetworkObjects[i], arg);
			}
		}
	}
}
