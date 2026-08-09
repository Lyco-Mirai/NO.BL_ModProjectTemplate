using System;
using System.IO;
using NuclearOption.Networking;
using NuclearOption.SavedMission;
using UnityEngine;

namespace NuclearOption.DedicatedServer
{
	[Serializable]
	public class DedicatedServerConfig
	{
		public string MissionDirectory;

		public bool ModdedServer;

		public bool Hidden;

		public string ServerName;

		public Override<ushort> Port;

		public Override<ushort> QueryPort;

		public string Password;

		public int MaxPlayers;

		public string[] BanListPaths;

		public bool DisableErrorKick;

		public string[] ErrorKickImmuneListPaths;

		public float NoPlayerStopTime;

		public float PostMissionDelay;

		public RotationType RotationType;

		public MissionOptions[] MissionRotation;

		public VoteKickConfig VoteKick = new VoteKickConfig();

		public bool HasPassword => !string.IsNullOrEmpty(Password);

		public static DedicatedServerConfig CreateDefault()
		{
			DedicatedServerConfig dedicatedServerConfig = new DedicatedServerConfig();
			dedicatedServerConfig.MissionDirectory = "/home/steam/NuclearOption-Missions";
			dedicatedServerConfig.ModdedServer = false;
			dedicatedServerConfig.ServerName = "Nuclear Option Server";
			dedicatedServerConfig.Port = default(Override<ushort>);
			dedicatedServerConfig.QueryPort = default(Override<ushort>);
			dedicatedServerConfig.MaxPlayers = 16;
			dedicatedServerConfig.Password = null;
			dedicatedServerConfig.BanListPaths = new string[1] { "ban_list.txt" };
			dedicatedServerConfig.ErrorKickImmuneListPaths = new string[0];
			dedicatedServerConfig.DisableErrorKick = false;
			dedicatedServerConfig.NoPlayerStopTime = 30f;
			dedicatedServerConfig.PostMissionDelay = 30f;
			dedicatedServerConfig.RotationType = RotationType.Sequence;
			dedicatedServerConfig.MissionRotation = new MissionOptions[2]
			{
				new MissionOptions
				{
					Key = new MissionKeySaveable
					{
						Group = "BuiltIn",
						Name = "Escalation"
					},
					MaxTime = 7200f
				},
				new MissionOptions
				{
					Key = new MissionKeySaveable
					{
						Group = "BuiltIn",
						Name = "Terminal Control"
					},
					MaxTime = 7200f
				}
			};
			dedicatedServerConfig.VoteKick = VoteKickConfig.CreateDefault();
			return dedicatedServerConfig;
		}

		public static void Save(string path, DedicatedServerConfig config, bool overwrite)
		{
			if (!overwrite && File.Exists(path))
			{
				throw new InvalidOperationException(path + " already exists");
			}
			string contents = JsonUtility.ToJson(config);
			File.WriteAllText(path, contents);
		}

		public static bool TryLoad(string path, out DedicatedServerConfig config)
		{
			ColorLog<DedicatedServerConfig>.Info("TryLoad " + path);
			if (File.Exists(path))
			{
				try
				{
					string json = File.ReadAllText(path);
					config = JsonUtility.FromJson<DedicatedServerConfig>(json);
					ColorLog<DedicatedServerConfig>.Info("Load success " + path);
					return true;
				}
				catch (Exception exception)
				{
					Debug.LogException(exception);
				}
			}
			config = null;
			return false;
		}

		public static (DedicatedServerConfig config, string path) AutoFindOrCreate()
		{
			if (TryLoad("DedicatedServerConfig.json", out var config))
			{
				return (config: config, path: "DedicatedServerConfig.json");
			}
			string text = Path.Combine(Application.persistentDataPath, "DedicatedServerConfig.json");
			if (TryLoad(text, out config))
			{
				return (config: config, path: text);
			}
			config = CreateDefault();
			if (config.BanListPaths.Length != 0 && !File.Exists(config.BanListPaths[0]))
			{
				File.WriteAllText(config.BanListPaths[0], "");
			}
			Save("DedicatedServerConfig.json", config, overwrite: false);
			return (config: config, path: "DedicatedServerConfig.json");
		}

		public void Log(bool isReload)
		{
			DedicatedServerConfig dedicatedServerConfig = JsonUtility.FromJson<DedicatedServerConfig>(JsonUtility.ToJson(this));
			if (!string.IsNullOrEmpty(dedicatedServerConfig.Password))
			{
				dedicatedServerConfig.Password = "[REDACTED]";
			}
			string text = JsonUtility.ToJson(dedicatedServerConfig, prettyPrint: true);
			ColorLog<DedicatedServerConfig>.Info((isReload ? "Reloaded" : "Loaded") + " Dedicated Server Config:\n" + text);
		}
	}
}
