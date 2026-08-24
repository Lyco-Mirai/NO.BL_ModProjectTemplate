using System;
using System.Collections.Generic;
using Steamworks;
using UnityEngine;

namespace NuclearOption.Networking.Authentication
{
	public class TimeoutManager
	{
		private readonly Dictionary<CSteamID, PlayerTimeout> _players = new Dictionary<CSteamID, PlayerTimeout>();

		private readonly TimeoutConfig _config;

		public double Now => Time.unscaledTimeAsDouble;

		public TimeoutManager(TimeoutConfig config)
		{
			_config = config;
		}

		private PlayerTimeout GetPlayer(CSteamID steamId)
		{
			if (_players.TryGetValue(steamId, out var value))
			{
				return value;
			}
			value = new PlayerTimeout(steamId);
			_players[steamId] = value;
			return value;
		}

		public bool HasTimeout(CSteamID steamId)
		{
			PlayerTimeout player = GetPlayer(steamId);
			double now = Now;
			double num = player.TimeoutExpiry - now;
			if (0.0 < num)
			{
				player.TimeoutExpiry += _config.SpamConnectPenaltySeconds;
				if (now - player.LastJoinWhileTimeoutLogTime > 60.0)
				{
					player.LastJoinWhileTimeoutLogTime = now;
					ColorLog<TimeoutManager>.Info($"{steamId} tried to join while timeout, timeoutDuration={num + (double)_config.SpamConnectPenaltySeconds} seconds");
				}
				return true;
			}
			return false;
		}

		public void OnDisconnect(CSteamID steamId)
		{
			PlayerTimeout player = GetPlayer(steamId);
			if (Now - player.LastErrorKickTime < 1.0)
			{
				return;
			}
			if (player.LastDisconnectTime == 0.0)
			{
				ColorLog<TimeoutManager>.Info($"{steamId} first disconnect");
				player.LastDisconnectTime = Now;
				return;
			}
			double num = Now - player.LastDisconnectTime;
			if (num < (double)_config.RapidDisconnectWindowSeconds)
			{
				player.ViolationLevel++;
				if (player.ViolationLevel > _config.FreeDisconnectAllowed)
				{
					float num2 = ApplyTimeout(player, _config.BaseTimeoutSeconds);
					ColorLog<TimeoutManager>.Info($"{steamId} rapid disconnect, ViolationLevel={player.ViolationLevel} ErrorKickCount={player.ErrorKickCount} Timeout={num2}s");
				}
				else
				{
					ColorLog<TimeoutManager>.Info($"{steamId} rapid disconnect, ViolationLevel={player.ViolationLevel} ErrorKickCount={player.ErrorKickCount} no timeout");
				}
			}
			else
			{
				int violationLevel = player.ViolationLevel;
				int errorKickCount = player.ErrorKickCount;
				bool num3 = Decay(ref player.ViolationLevel, num, _config.PenaltyDecayRateSeconds);
				bool flag = Decay(ref player.ErrorKickCount, num, _config.ErrorKickDecayRateSeconds);
				if (num3)
				{
					ColorLog<TimeoutManager>.Info($"{steamId} violation level lowered, {violationLevel} -> {player.ViolationLevel}");
				}
				if (flag)
				{
					ColorLog<TimeoutManager>.Info($"{steamId} error level lowered, {errorKickCount} -> {player.ErrorKickCount}");
				}
			}
			player.LastDisconnectTime = Now;
		}

		public bool OnKickFromError(CSteamID steamId, NuclearOptionPlayerErrorFlags.Names errorFlag)
		{
			PlayerTimeout player = GetPlayer(steamId);
			if (_config.CheckInstantBanErrorFlags(errorFlag))
			{
				return true;
			}
			player.ErrorKickCount++;
			if (_config.BanOnRepeatedErrorKicks && player.ErrorKickCount > _config.MaxErrorKicksBeforeBan)
			{
				return true;
			}
			player.ViolationLevel += 2;
			float num = ApplyTimeout(player, _config.ErrorKickBaseTimeout);
			ColorLog<TimeoutManager>.Info($"{steamId} kick from error, ViolationLevel={player.ViolationLevel} ErrorKickCount={player.ErrorKickCount} Timeout={num}s");
			player.LastErrorKickTime = Now;
			player.LastDisconnectTime = Now;
			return false;
		}

		public void AddCustomTimeout(CSteamID steamId, int increaseViolationLevel, float baseSeconds)
		{
			PlayerTimeout player = GetPlayer(steamId);
			player.ViolationLevel += increaseViolationLevel;
			ApplyTimeout(player, baseSeconds);
		}

		private static bool Decay(ref int level, double timeSinceLast, double decayRate)
		{
			if (level > 0)
			{
				int num = (int)(timeSinceLast / decayRate);
				int num2 = level - num;
				if (num2 < 0)
				{
					num2 = 0;
				}
				bool result = level != num2;
				level = num2;
				return result;
			}
			return false;
		}

		private float ApplyTimeout(PlayerTimeout player, float baseSeconds)
		{
			int num = player.ViolationLevel - _config.FreeDisconnectAllowed;
			float num2 = ((num > 1) ? (baseSeconds * Mathf.Pow(_config.TimeoutMultiplier, num - 1)) : baseSeconds);
			num2 = ((!float.IsFinite(num2)) ? _config.MaxTimeoutSeconds : Math.Min(num2, _config.MaxTimeoutSeconds));
			if (player.TimeoutExpiry >= Now)
			{
				ColorLog<TimeoutManager>.LogError("Should not be applying timeout if player is already timeout");
			}
			player.TimeoutExpiry = Now + (double)num2;
			return num2;
		}
	}
}
