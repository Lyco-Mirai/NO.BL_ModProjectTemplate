using NuclearOption.MissionEditorScripts.Buttons;
using NuclearOption.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConvoyPurchaseOption : MonoBehaviour
{
	[SerializeField]
	private TMP_Text text;

	[SerializeField]
	private Button button;

	[SerializeField]
	private ShowHoverText buttonHoverText;

	private ContributeToFaction contributeToFaction;

	private Player localPlayer;

	private Faction.ConvoyGroup convoyGroup;

	private float cost;

	private bool wasInteractable;

	public void Initialize(ContributeToFaction contributeToFaction, Player localPlayer, Faction.ConvoyGroup convoyGroup)
	{
		this.localPlayer = localPlayer;
		this.contributeToFaction = contributeToFaction;
		this.convoyGroup = convoyGroup;
		cost = convoyGroup.GetCost();
		text.text = convoyGroup.Name + " (" + UnitConverter.ValueReading(cost) + ")";
		SetHoverText();
	}

	public void SetButtonHoverText(HoverText hoverText)
	{
		buttonHoverText.SetHover(hoverText);
	}

	private void Update()
	{
		bool flag = localPlayer.Allocation >= cost;
		if (flag != wasInteractable)
		{
			button.interactable = flag;
			text.color = (flag ? Color.white : Color.gray);
			wasInteractable = flag;
		}
	}

	private void OnDisable()
	{
		Object.Destroy(base.gameObject);
	}

	public void SelectConvoy()
	{
		contributeToFaction.SelectConvoy(convoyGroup);
	}

	private void SetHoverText()
	{
		string text = "Spawn :";
		for (int i = 0; i < convoyGroup.Constituents.Count; i++)
		{
			text += $"\n{convoyGroup.Constituents[i].Count}x {convoyGroup.Constituents[i].Type.unitName}";
		}
		buttonHoverText.SetText(text);
	}
}
