using Mirage;
using NuclearOption.Networking.Authentication;
using UnityEngine;

namespace NuclearOption.Networking
{
	public static class PlayerHelper
	{
		public static bool TryGetPlayer<T>(this INetworkPlayer networkPlayer, out T player) where T : BasePlayer
		{
			player = null;
			NetworkIdentity identity = networkPlayer.Identity;
			if (identity != null)
			{
				return identity.TryGetComponent<T>(out player);
			}
			return false;
		}

		public static Player GetPlayer(this Unit unit)
		{
			if (unit is Aircraft aircraft)
			{
				return aircraft.Player;
			}
			return null;
		}

		public static NetworkAuthenticatorNuclearOption.AuthData GetAuthData(this INetworkPlayer networkPlayer)
		{
			return networkPlayer.Authentication.GetData<NetworkAuthenticatorNuclearOption.AuthData>();
		}

		public static void RegisterPrefabs(ClientObjectManager clientObjectManager)
		{
			RegisterPrefab<SpectatorPlayer>(clientObjectManager);
			RegisterPrefab<DedicatedServerPlayer>(clientObjectManager);
		}

		public static void RegisterPrefab<T>(ClientObjectManager clientObjectManager) where T : BasePlayer
		{
			clientObjectManager.RegisterSpawnHandler(PlayerSpawnHash<T>(), (SpawnMessage _) => CreatePrefab<T>().Identity, null);
		}

		public static T CreatePrefab<T>() where T : BasePlayer
		{
			T component = new GameObject("player", typeof(NetworkIdentity), typeof(T)).GetComponent<T>();
			component.Identity.PrefabHash = PlayerSpawnHash<T>();
			return component;
		}

		private static int PlayerSpawnHash<T>()
		{
			return typeof(T).Name.GetStableHashCode();
		}
	}
}
