using NuclearOption.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InfoPanel_PlayerEntry : MonoBehaviour
{
	private Player player;

	private InfoPanel_Faction factionInfoPanel;

	[SerializeField]
	private TextMeshProUGUI playerName;

	[SerializeField]
	private Button playerAircraft_Button;

	[SerializeField]
	private TextMeshProUGUI playerAircraft;

	[SerializeField]
	private TextMeshProUGUI playerRank;

	[SerializeField]
	private TextMeshProUGUI playerScore;

	[SerializeField]
	private TextMeshProUGUI playerFunds;

	private bool selected;

	[SerializeField]
	private Image selectedImg;

	private float lastRefresh;

	private float refreshRate = 1f;

	private void Awake()
	{
		lastRefresh = Time.timeSinceLevelLoad;
	}

	public void Update()
	{
		if (!(Time.timeSinceLevelLoad > lastRefresh + refreshRate))
		{
			return;
		}
		if (player != null)
		{
			playerRank.text = player.PlayerRank.ToString("N0");
			playerScore.text = player.PlayerScore.ToString("N1");
			if (player.Aircraft != null)
			{
				playerAircraft.text = player.Aircraft.definition.code;
			}
			else
			{
				if (selected)
				{
					Deselect();
				}
				playerAircraft.text = "-";
			}
			playerFunds.text = "$ " + player.Allocation.ToString("N1") + "M";
		}
		else
		{
			factionInfoPanel.RemoveNullPlayers();
			Object.Destroy(base.gameObject);
		}
		lastRefresh = Time.timeSinceLevelLoad;
	}

	public void SetPlayer(Player p, InfoPanel_Faction panel)
	{
		player = p;
		playerName.text = p.GetDisplayName(PlayerNameContext.Other);
		factionInfoPanel = panel;
	}

	public void Select()
	{
		selected = true;
		selectedImg.enabled = true;
		SceneSingleton<DynamicMap>.i.DeselectAllIcons();
		SceneSingleton<DynamicMap>.i.SelectIcon(player.Aircraft);
		SceneSingleton<CameraStateManager>.i.SetFollowingUnit(player.Aircraft);
		SceneSingleton<DynamicMap>.i.Maximize();
	}

	public void Deselect()
	{
		selected = false;
		selectedImg.enabled = false;
		SceneSingleton<DynamicMap>.i.DeselectIcon(player.Aircraft);
		SceneSingleton<CameraStateManager>.i.SetFollowingUnit(null);
		SceneSingleton<DynamicMap>.i.Maximize();
	}

	public bool IsSelected()
	{
		return selected;
	}

	public void OnButtonClick()
	{
		if (SceneSingleton<DynamicMap>.i.HQ == null && player.Aircraft != null)
		{
			selected = !selected;
			if (selected)
			{
				SceneSingleton<GameplayUI>.i.SpectatorPanelDeselectAll();
				Select();
			}
			else
			{
				Deselect();
			}
		}
	}
}
