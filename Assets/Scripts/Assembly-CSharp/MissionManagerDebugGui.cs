using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cysharp.Threading.Tasks;
using Mirage;
using NuclearOption.Networking;
using NuclearOption.SavedMission;
using UnityEngine;

[ExecuteInEditMode]
public class MissionManagerDebugGui : MonoBehaviour
{
	[SerializeField]
	private Rect box = new Rect(20f, 60f, 300f, 800f);

	[SerializeField]
	private bool rightAlign;

	[SerializeField]
	private bool bottomAlign;

	[SerializeField]
	private Color normalColor = new Color(0.1f, 0.1f, 0.1f, 0.2f);

	[SerializeField]
	private Color loadingColor = new Color(0.5f, 0.1f, 0.1f, 0.6f);

	private string missionName = "";

	private StringBuilder builder = new StringBuilder();

	private Texture2D background;

	private GUIStyle backgroundStyle;

	private void Start()
	{
		SetBackgroundColor(normalColor);
		MissionManager.onMissionLoad += MissionManager_onMissionLoad;
	}

	private void OnDestroy()
	{
		MissionManager.onMissionLoad -= MissionManager_onMissionLoad;
		if (background != null)
		{
			UnityEngine.Object.Destroy(background);
		}
	}

	private void OnValidate()
	{
		SetBackgroundColor(normalColor);
	}

	private void MissionManager_onMissionLoad(Mission obj)
	{
		missionName = obj.Name;
	}

	private void SetBackgroundColor(Color color)
	{
		if (background == null)
		{
			background = new Texture2D(2, 2);
		}
		Color[] array = new Color[background.width * background.height];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = color;
		}
		background.SetPixels(array);
		background.Apply();
		if (backgroundStyle == null)
		{
			backgroundStyle = new GUIStyle();
		}
		backgroundStyle.normal.background = background;
	}

	private void OnGUI()
	{
		if (rightAlign)
		{
			box.x = (float)Screen.width - box.x - box.width;
		}
		if (bottomAlign)
		{
			box.y = (float)Screen.height - box.y - box.height;
		}
		GUILayout.BeginArea(box);
		using (new GUILayout.VerticalScope(backgroundStyle))
		{
			GUILayout.Label($"Is Running {MissionManager.IsRunning} <color=#00FF00>(Toggle F2)</color>");
			GUILayout.Label("Mission: " + (MissionManager.CurrentMission?.Name ?? "NULL"));
			if (MissionManager.IsRunning && MissionManager.CurrentMission != null)
			{
				DrawObjectives();
			}
		}
		GUILayout.EndArea();
	}

	private void DrawLoadUnloadButtons()
	{
		using (new GUILayout.HorizontalScope())
		{
			GUI.enabled = MissionManager.IsRunning;
			if (GUILayout.Button("Unload"))
			{
				UnloadSlow().Forget();
			}
			GUI.enabled = !MissionManager.IsRunning;
			if (GUILayout.Button("Load"))
			{
				LoadSlow(missionName, GameState.Multiplayer).Forget();
			}
			GUI.enabled = MissionManager.IsRunning;
			if (GUILayout.Button("Reload"))
			{
				Reload(GameManager.gameState).Forget();
			}
			GUI.enabled = true;
			if (GameManager.gameState != GameState.Editor)
			{
				if (GUILayout.Button("Open editor"))
				{
					Reload(GameState.Editor).Forget();
				}
			}
			else if (GUILayout.Button("Play Mission"))
			{
				Reload(GameState.SinglePlayer).Forget();
			}
		}
		using (new GUILayout.HorizontalScope())
		{
			GUILayout.Label(new GUIContent("mission"));
			missionName = GUILayout.TextField(missionName);
		}
	}

	private void DrawObjectives()
	{
		MissionRunner runner = MissionManager.Runner;
		IEnumerable<Objective> objectives = runner.ActiveObjectives.Where((Objective x) => x.FactionHQ == null);
		DrawObjectives("No Faction", objectives);
		foreach (Faction faction in FactionRegistry.factions)
		{
			FactionHQ key = FactionRegistry.HQLookup[faction];
			IReadOnlyList<Objective> readOnlyList2;
			if (!runner.activeByFaction.TryGetValue(key, out var value))
			{
				IReadOnlyList<Objective> readOnlyList = Array.Empty<Objective>();
				readOnlyList2 = readOnlyList;
			}
			else
			{
				IReadOnlyList<Objective> readOnlyList = value;
				readOnlyList2 = readOnlyList;
			}
			IReadOnlyList<Objective> objectives2 = readOnlyList2;
			DrawObjectives(faction.factionName, objectives2);
		}
	}

	private void DrawObjectives(string factionName, IEnumerable<Objective> objectives)
	{
		builder.AppendLine("Objective: " + factionName);
		bool flag = true;
		foreach (Objective objective in objectives)
		{
			flag = false;
			builder.AppendLine($" - {objective}");
			float completePercent = objective.CompletePercent;
			builder.Append("   ");
			AppendProgressBar(builder, completePercent);
			builder.Append("\n");
		}
		if (flag)
		{
			builder.AppendLine("<none>");
		}
		GUILayout.Label(builder.ToString());
		builder.Clear();
	}

	public static string CreateProgressBar(float percent, int blocks = 24)
	{
		StringBuilder stringBuilder = new StringBuilder();
		AppendProgressBar(stringBuilder, percent, blocks);
		return stringBuilder.ToString();
	}

	public static string CreateProgressBar(StringBuilder builder, float percent, int blocks = 24)
	{
		builder.Clear();
		AppendProgressBar(builder, percent, blocks);
		return builder.ToString();
	}

	public static void AppendProgressBar(StringBuilder builder, float percent, int blocks = 24)
	{
		int num = (int)(percent * (float)blocks);
		builder.Append("[");
		for (int i = 0; i < num; i++)
		{
			builder.Append("#");
		}
		for (int j = 0; j < blocks - num; j++)
		{
			builder.Append("_");
		}
		builder.Append($"] {(int)(percent * 100f)}%");
	}

	private UniTask UnloadSlow()
	{
		throw new NotImplementedException();
	}

	private UniTask LoadSlow(string missionName, GameState state)
	{
		throw new NotImplementedException();
	}

	private static void UnloadInScene()
	{
		MissionManager.SetNullMission();
		ServerObjectManager serverObjectManager = NetworkManagerNuclearOption.i.ServerObjectManager;
		foreach (Unit allUnit in UnitRegistry.allUnits)
		{
			serverObjectManager.Destroy(allUnit.gameObject, destroyServerObject: false);
		}
		GameManager.ResetGameResolution();
	}

	private void LoadInScene(string missionName, GameState state)
	{
		throw new NotImplementedException();
	}

	private async UniTask Reload(GameState state)
	{
		string mission = missionName;
		await UnloadSlow();
		missionName = mission;
		await LoadSlow(mission, state);
	}
}
