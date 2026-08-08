using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using NuclearOption.Networking;
using Rewired;
using UnityEngine;
using UnityEngine.UI;

public class DebugUI : MonoBehaviour
{
	private static readonly ResourcesAsyncLoader<DebugUI> loader = ResourcesAsyncLoader.Create("DebugUI", (Func<GameObject, DebugUI>)null);

	[SerializeField]
	private GameObject mission;

	[SerializeField]
	private Canvas canvas;

	[SerializeField]
	private GameObject graphy;

	[SerializeField]
	private GameObject performanceText;

	[SerializeField]
	private GameObject bandwidthText;

	[SerializeField]
	private GameObject[] ClientGraphs;

	[SerializeField]
	private NetworkRTTGraphDataSource networkDataSource;

	[SerializeField]
	private BandwidthText bandwidthTextSource;

	[SerializeField]
	private bool toggleLayouts_editorButton;

	private bool clientOnlyActive;

	private bool serverWithConnActive;

	public static DebugUI i => loader.Get();

	public static async UniTask Preload(CancellationToken cancel)
	{
		await loader.Load(cancel);
	}

	private void Awake()
	{
		loader.AssetNotLoaded();
	}

	private void OnValidate()
	{
		if (toggleLayouts_editorButton)
		{
			toggleLayouts_editorButton = false;
			bool flag = GetComponentInChildren<LayoutGroup>().enabled;
			LayoutGroup[] componentsInChildren = GetComponentsInChildren<LayoutGroup>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].enabled = !flag;
			}
			ContentSizeFitter[] componentsInChildren2 = GetComponentsInChildren<ContentSizeFitter>();
			for (int i = 0; i < componentsInChildren2.Length; i++)
			{
				componentsInChildren2[i].enabled = !flag;
			}
		}
	}

	private void Start()
	{
		mission.SetActive(GetBoolPlayerPrefs("MissionDebug"));
		graphy.SetActive(GetBoolPlayerPrefs("GraphDebug"));
		performanceText.SetActive(GetBoolPlayerPrefs("PerfDebug"));
		OnChange();
	}

	private void OnChange()
	{
		if (serverWithConnActive)
		{
			bandwidthTextSource.Metrics = NetworkManagerNuclearOption.i.Server.Metrics;
		}
		else if (clientOnlyActive)
		{
			networkDataSource.Client = NetworkManagerNuclearOption.i.Client;
			bandwidthTextSource.Metrics = NetworkManagerNuclearOption.i.Client.Metrics;
		}
		else
		{
			bandwidthTextSource.Metrics = null;
		}
		canvas.gameObject.SetActive(graphy.activeSelf || performanceText.activeSelf);
		bandwidthText.SetActive(performanceText.activeSelf && bandwidthTextSource.Metrics != null);
		GameObject[] clientGraphs = ClientGraphs;
		for (int i = 0; i < clientGraphs.Length; i++)
		{
			clientGraphs[i].SetActive(clientOnlyActive);
		}
	}

	private void Update()
	{
		if (MainMenu.State == MainMenu.LoadingState.Loaded && ReInput.isReady)
		{
			Rewired.Player player = ReInput.players.GetPlayer(0);
			if ((0u | (CheckToggle(player, "MissionDebug", mission) ? 1u : 0u) | (CheckToggle(player, "GraphDebug", graphy) ? 1u : 0u) | (CheckToggle(player, "PerfDebug", performanceText) ? 1u : 0u) | (CheckNetwork() ? 1u : 0u)) != 0)
			{
				OnChange();
			}
		}
	}

	private static bool CheckToggle(Rewired.Player player, string input, GameObject target)
	{
		if (player.GetButtonDown(input))
		{
			target.SetActive(!target.activeSelf);
			SetBoolPlayerPrefs(input, target.activeSelf);
			return true;
		}
		return false;
	}

	private bool CheckNetwork()
	{
		bool flag = NetworkManagerNuclearOption.i.Client.Active && !NetworkManagerNuclearOption.i.Server.Active;
		bool flag2 = NetworkManagerNuclearOption.i.Server.Active && (NetworkManagerNuclearOption.i.Server.AllPlayers.Count >= 2 || (NetworkManagerNuclearOption.i.Server.AllPlayers.Count == 1 && !NetworkManagerNuclearOption.i.Server.AllPlayers.First().IsHost));
		bool result = clientOnlyActive != flag || serverWithConnActive != flag2;
		clientOnlyActive = flag;
		serverWithConnActive = flag2;
		return result;
	}

	private static bool GetBoolPlayerPrefs(string input)
	{
		return PlayerPrefs.GetInt("DebugUI_" + input, 0) == 1;
	}

	private static void SetBoolPlayerPrefs(string input, bool value)
	{
		PlayerPrefs.SetInt("DebugUI_" + input, value ? 1 : 0);
	}
}
