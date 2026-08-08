using System;
using System.IO;
using System.Text;
using AOT;
using Cysharp.Threading.Tasks;
using NuclearOption.BuildScripts;
using NuclearOption.DedicatedServer;
using NuclearOption.SavedMission;
using Steamworks;
using UnityEngine;

[DisallowMultipleComponent]
public class SteamManager : MonoBehaviour
{
	private static bool s_EverInitialized;

	private static SteamManager instance;

	private bool initialized;

	private bool isServer;

	private SteamAPIWarningMessageHook_t m_SteamAPIWarningMessageHook;

	private FSteamNetworkingSocketsDebugOutput m_FSteamNetworkingSocketsDebugOutput;

	public static string SteamAppId { get; private set; }

	public static bool ClientInitialized
	{
		get
		{
			if (instance != null && instance.initialized)
			{
				return !instance.isServer;
			}
			return false;
		}
	}

	public static bool ServerInitialized
	{
		get
		{
			if (instance != null && instance.initialized)
			{
				return instance.isServer;
			}
			return false;
		}
	}

	[MonoPInvokeCallback(typeof(SteamAPIWarningMessageHook_t))]
	protected static void SteamAPIWarningMessageHook(int nSeverity, StringBuilder pchDebugText)
	{
		ColorLog<SteamManager>.Info(string.Format("{0} {1}", nSeverity switch
		{
			0 => "[msg]", 
			1 => "[warning]", 
			_ => "[unknown]", 
		}, pchDebugText));
	}

	[MonoPInvokeCallback(typeof(SteamAPIWarningMessageHook_t))]
	protected static void FSteamNetworkingSocketsDebugOutput(ESteamNetworkingSocketsDebugOutputType nType, StringBuilder pszMsg)
	{
		ColorLog<SteamManager>.Info(string.Format("{0} {1}", nType switch
		{
			ESteamNetworkingSocketsDebugOutputType.k_ESteamNetworkingSocketsDebugOutputType_None => "[None]", 
			ESteamNetworkingSocketsDebugOutputType.k_ESteamNetworkingSocketsDebugOutputType_Bug => "[Bug]", 
			ESteamNetworkingSocketsDebugOutputType.k_ESteamNetworkingSocketsDebugOutputType_Error => "[Error]", 
			ESteamNetworkingSocketsDebugOutputType.k_ESteamNetworkingSocketsDebugOutputType_Important => "[Important]", 
			ESteamNetworkingSocketsDebugOutputType.k_ESteamNetworkingSocketsDebugOutputType_Warning => "[Warning]", 
			ESteamNetworkingSocketsDebugOutputType.k_ESteamNetworkingSocketsDebugOutputType_Msg => "[Msg]", 
			ESteamNetworkingSocketsDebugOutputType.k_ESteamNetworkingSocketsDebugOutputType_Verbose => "[Verbose]", 
			ESteamNetworkingSocketsDebugOutputType.k_ESteamNetworkingSocketsDebugOutputType_Debug => "[Debug]", 
			ESteamNetworkingSocketsDebugOutputType.k_ESteamNetworkingSocketsDebugOutputType_Everything => "[Everything]", 
			_ => "[unknown]", 
		}, pszMsg));
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void InitOnPlayMode()
	{
		s_EverInitialized = false;
	}

	public static UniTask CheckIdFileAsync(string steamAppID)
	{
		SteamAppId = steamAppID;
		return UniTask.RunOnThreadPool(delegate
		{
			ColorLog<SteamManager>.Info("[SideThread] Running CheckIdFile");
			if (File.Exists("steam_appid.txt"))
			{
				ColorLog<SteamManager>.Info("[SideThread] File exists");
				string text = File.ReadAllText("steam_appid.txt");
				if (text != steamAppID)
				{
					ColorLog<SteamManager>.Info("[SideThread] Id different");
					File.WriteAllText("steam_appid.txt", steamAppID.ToString());
					ColorLog<SteamManager>.Info("[SideThread] Updating steam_appid.txt. Previous: " + text + ", new SteamAppID " + steamAppID);
				}
			}
			else
			{
				ColorLog<SteamManager>.Info("[SideThread] No file");
				File.WriteAllText("steam_appid.txt", steamAppID.ToString());
				ColorLog<SteamManager>.Info("[SideThread] New steam_appid.txt written with SteamAppID " + steamAppID);
			}
		});
	}

	public void InitAsClient()
	{
		ColorLog<SteamManager>.Info("Init As Client");
		CheckInit();
		if (ClientInit())
		{
			DebugHooks();
		}
	}

	public void InitAsServer(DedicatedServerConfig serverConfig)
	{
		ColorLog<SteamManager>.Info("Init As Server");
		CheckInit();
		if (!ServerInit(serverConfig.Port.AsNullable(), serverConfig.QueryPort.AsNullable()))
		{
			CommandLineArgParser.Quit();
		}
		else
		{
			DebugHooks();
		}
	}

	private void CheckInit()
	{
		ColorLog<SteamManager>.Info("CheckInit");
		if (initialized)
		{
			throw new Exception("This SteamManager instance was already initialized");
		}
		if (s_EverInitialized)
		{
			throw new Exception("Tried to Initialize the SteamAPI twice in one session!");
		}
		if (instance != null)
		{
			throw new Exception("SteamManager.instance should be null when init is called");
		}
		if (!Packsize.Test())
		{
			Debug.LogError("[Steamworks.NET] Packsize Test returned false, the wrong version of Steamworks.NET is being run in this platform.", this);
		}
		if (!DllCheck.Test())
		{
			Debug.LogError("[Steamworks.NET] DllCheck Test returned false, One or more of the Steamworks binaries seems to be the wrong version.", this);
		}
	}

	private void CheckRestart()
	{
		ColorLog<SteamManager>.Info("CheckRestart");
		try
		{
			if (SteamAPI.RestartAppIfNecessary(AppId_t.Invalid))
			{
				Application.Quit();
			}
		}
		catch (DllNotFoundException ex)
		{
			Debug.LogError("[Steamworks.NET] Could not load [lib]steam_api.dll/so/dylib. It's likely not in the correct location. Refer to the README for more details.\n" + ex, this);
			Application.Quit();
		}
	}

	private bool ClientInit()
	{
		ColorLog<SteamManager>.Info("ClientInit");
		initialized = SteamAPI.Init();
		if (!initialized)
		{
			Debug.LogError("[Steamworks.NET] SteamAPI_Init() failed.");
			return false;
		}
		ColorLog<SteamManager>.Info("ClientInit Success");
		MarkInit(isServer: false);
		return true;
	}

	private bool ServerInit(ushort? portNullable, ushort? queryPortNullable)
	{
		ColorLog<SteamManager>.Info("ServerInit");
		ushort usGamePort = portNullable ?? 7777;
		ushort usQueryPort = queryPortNullable ?? 7778;
		initialized = GameServer.Init(0u, usGamePort, usQueryPort, EServerMode.eServerModeAuthenticationAndSecure, Application.version);
		if (!initialized)
		{
			Debug.LogError("[Steamworks.NET] SteamGameServer_Init call failed");
			SteamAPI.Shutdown();
			return false;
		}
		ColorLog<SteamManager>.Info("ServerInit Success");
		MarkInit(isServer: true);
		return true;
	}

	private void MarkInit(bool isServer)
	{
		s_EverInitialized = true;
		instance = this;
		this.isServer = isServer;
	}

	private void DebugHooks()
	{
		ColorLog<SteamManager>.Info("DebugHooks");
		if (m_SteamAPIWarningMessageHook == null)
		{
			m_SteamAPIWarningMessageHook = SteamAPIWarningMessageHook;
			m_FSteamNetworkingSocketsDebugOutput = FSteamNetworkingSocketsDebugOutput;
			ESteamNetworkingSocketsDebugOutputType eDetailLevel = (CommandLineArgParser.UseSteamNetworkingVerboseLogging ? ESteamNetworkingSocketsDebugOutputType.k_ESteamNetworkingSocketsDebugOutputType_Everything : ((!Debug.isDebugBuild) ? ESteamNetworkingSocketsDebugOutputType.k_ESteamNetworkingSocketsDebugOutputType_Warning : ESteamNetworkingSocketsDebugOutputType.k_ESteamNetworkingSocketsDebugOutputType_Msg));
			if (isServer)
			{
				SteamGameServerClient.SetWarningMessageHook(m_SteamAPIWarningMessageHook);
				SteamGameServerNetworkingUtils.SetDebugOutputFunction(eDetailLevel, m_FSteamNetworkingSocketsDebugOutput);
			}
			else
			{
				SteamClient.SetWarningMessageHook(m_SteamAPIWarningMessageHook);
				SteamNetworkingUtils.SetDebugOutputFunction(eDetailLevel, m_FSteamNetworkingSocketsDebugOutput);
			}
		}
	}

	private void OnDestroy()
	{
		if (instance != this)
		{
			ColorLog<SteamManager>.InfoWarn("instance of SteamManager destroyed that is not the main instance");
			return;
		}
		instance = null;
		if (initialized)
		{
			ColorLog<SteamManager>.Info("Shutdown");
			if (isServer)
			{
				GameServer.Shutdown();
			}
			else
			{
				SteamAPI.Shutdown();
			}
		}
	}

	private void Update()
	{
		if (initialized)
		{
			if (isServer)
			{
				GameServer.RunCallbacks();
			}
			else
			{
				SteamAPI.RunCallbacks();
			}
		}
	}
}
