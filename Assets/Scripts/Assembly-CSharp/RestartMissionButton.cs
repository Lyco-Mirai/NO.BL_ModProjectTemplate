using Cysharp.Threading.Tasks;
using JamesFrowen.ScriptableVariables.UI;
using NuclearOption.BuildScripts;
using NuclearOption.Networking;
using UnityEngine;

public class RestartMissionButton : ButtonController
{
	protected override void Awake()
	{
		base.Awake();
		if (!RestartAllowed())
		{
			base.gameObject.SetActive(value: false);
		}
	}

	protected override void onClick()
	{
		if (RestartAllowed())
		{
			MissionManager.RestartMission().Forget();
		}
		else
		{
			Debug.LogError("Restart Mission pressed, but was not single player");
		}
	}

	public static bool RestartAllowed()
	{
		if (GameManager.gameState == GameState.SinglePlayer)
		{
			return true;
		}
		if (NetworkManagerNuclearOption.i.Server.Active)
		{
			return CommandLineArgParser.IsAutoStart;
		}
		return false;
	}
}
