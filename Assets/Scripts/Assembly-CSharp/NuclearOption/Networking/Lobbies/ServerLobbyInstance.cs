using System;
using System.Collections.Generic;
using Mirage;
using Steamworks;
using UnityEngine;

namespace NuclearOption.Networking.Lobbies
{
	public class ServerLobbyInstance : LobbyInstance
	{
		public const int MAX_CHUNKS_DESCRIPTION = 10;

		private readonly Dictionary<string, string> rules = new Dictionary<string, string>();

		private ExponentialMovingAverage pingAverage = new ExponentialMovingAverage(5);

		public float NextPingTime;

		public gameserveritem_t details { get; private set; }

		public int PingCount { get; private set; }

		protected override string LobbyName => details.GetServerName();

		public override string MissionDescriptionSanitized => DedicatedServerKeyValues.ParseDescription(rules);

		public override bool DedicatedServer => true;

		public override CSteamID LobbyId => details.m_steamID;

		public override string HostAddress => details.m_steamID.ToString();

		public override string UdpAddress => "";

		public override string UdpPort => "";

		public void SetDetails(gameserveritem_t details)
		{
			rules.Clear();
			this.details = details;
			SetPingResult(details.m_nPing);
			rules["has_password"] = LobbyInstance.BoolToTag(this.details.m_bPassword);
			try
			{
				DedicatedServerKeyValues.ParseTags(details.GetGameTags(), rules);
			}
			catch (Exception arg)
			{
				Debug.LogError($"Failed to parse tags for server {this.details.m_steamID}. {arg}");
			}
		}

		public void SetRule(string key, string value)
		{
			try
			{
				DedicatedServerKeyValues.ParseKeyValue(key, value, rules);
			}
			catch (Exception arg)
			{
				Debug.LogError($"Failed to parse rule for server {details.m_steamID}. {arg}");
			}
		}

		protected override string GetData(string key)
		{
			if (!rules.TryGetValue(key, out var value))
			{
				return string.Empty;
			}
			return value;
		}

		public override int? CalculatePing()
		{
			return (int)pingAverage.Value;
		}

		public override bool GetPlayerCounts(out int current, out int max)
		{
			current = details.m_nPlayers;
			max = details.m_nMaxPlayers;
			return true;
		}

		public override bool IsPasswordProtected(out string shortPassword)
		{
			shortPassword = GetData("short_password");
			return details.m_bPassword;
		}

		public void SetPingResult(int ping)
		{
			pingAverage.Add(ping);
			PingCount++;
		}
	}
}
