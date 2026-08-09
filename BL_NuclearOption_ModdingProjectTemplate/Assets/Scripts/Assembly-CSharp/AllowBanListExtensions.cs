using Mirage.SteamworksSocket;
using NuclearOption.DedicatedServer;
using NuclearOption.Networking;
using Steamworks;
using UnityEngine;

public static class AllowBanListExtensions
{
	private static bool NotUsingSteam(bool logWarning = true)
	{
		if (!(NetworkManagerNuclearOption.i.Server.SocketFactory is SteamworksSocketFactory))
		{
			if (logWarning)
			{
				Debug.LogWarning("Can only block user when using Steam transport");
			}
			return true;
		}
		return false;
	}

	public static bool Contains(this AllowBanList list, Player player)
	{
		if (NotUsingSteam(logWarning: false))
		{
			return false;
		}
		return list.Contains(player.CSteamID);
	}

	public static bool Toggle(this AllowBanList list, Player player, string reason = null)
	{
		return list.Toggle(player.CSteamID, reason);
	}

	public static bool Toggle(this AllowBanList list, CSteamID id, string reason = null)
	{
		if (NotUsingSteam())
		{
			return false;
		}
		if (list.Contains(id))
		{
			list.Remove(id);
			return false;
		}
		list.Add(id, reason);
		return true;
	}
}
