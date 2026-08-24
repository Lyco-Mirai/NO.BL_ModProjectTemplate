using System;
using System.IO;
using System.Text;
using Steamworks;
using UnityEngine;

public class SteamDebugLog
{
	private static string logFilePath;

	public static void Initialize(bool server)
	{
		string path = (Application.isEditor ? "SteamLog_editor.Log" : "SteamLog_player.Log");
		logFilePath = Path.Combine(Application.persistentDataPath, path);
		File.WriteAllText(logFilePath, "--- NEW SESSION START: " + DateTime.Now.ToString() + " ---\n");
		if (server)
		{
			SteamGameServerNetworkingUtils.SetDebugOutputFunction(ESteamNetworkingSocketsDebugOutputType.k_ESteamNetworkingSocketsDebugOutputType_Everything, DebugOutput);
		}
		else
		{
			SteamNetworkingUtils.SetDebugOutputFunction(ESteamNetworkingSocketsDebugOutputType.k_ESteamNetworkingSocketsDebugOutputType_Everything, DebugOutput);
		}
	}

	public static void DebugOutput(ESteamNetworkingSocketsDebugOutputType nType, StringBuilder pszMsg)
	{
		string text = string.Format("{0:HH:mm:ss:fff} {1} {2}", DateTime.Now, nType switch
		{
			ESteamNetworkingSocketsDebugOutputType.k_ESteamNetworkingSocketsDebugOutputType_None => "[NONE]", 
			ESteamNetworkingSocketsDebugOutputType.k_ESteamNetworkingSocketsDebugOutputType_Bug => "[BUG]", 
			ESteamNetworkingSocketsDebugOutputType.k_ESteamNetworkingSocketsDebugOutputType_Error => "[ERROR]", 
			ESteamNetworkingSocketsDebugOutputType.k_ESteamNetworkingSocketsDebugOutputType_Important => "[IMPORTANT]", 
			ESteamNetworkingSocketsDebugOutputType.k_ESteamNetworkingSocketsDebugOutputType_Warning => "[WARN]", 
			ESteamNetworkingSocketsDebugOutputType.k_ESteamNetworkingSocketsDebugOutputType_Msg => "[INFO]", 
			ESteamNetworkingSocketsDebugOutputType.k_ESteamNetworkingSocketsDebugOutputType_Verbose => "[VERBOSE]", 
			ESteamNetworkingSocketsDebugOutputType.k_ESteamNetworkingSocketsDebugOutputType_Debug => "[DEBUG]", 
			ESteamNetworkingSocketsDebugOutputType.k_ESteamNetworkingSocketsDebugOutputType_Everything => "[EVERYTHING]", 
			_ => "[UNKNOWN]", 
		}, pszMsg.ToString());
		try
		{
			File.AppendAllText(logFilePath, text + Environment.NewLine);
		}
		catch (Exception ex)
		{
			Debug.LogError("Failed to write to log file: " + ex.Message);
		}
	}
}
