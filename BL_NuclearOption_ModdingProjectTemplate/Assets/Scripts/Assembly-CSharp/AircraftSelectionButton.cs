using NuclearOption.MissionEditorScripts.Buttons;
using NuclearOption.Networking;
using UnityEngine;
using UnityEngine.UI;

public class AircraftSelectionButton : MonoBehaviour
{
	[SerializeField]
	private Button button;

	[SerializeField]
	private Image image;

	[SerializeField]
	private Image border;

	[SerializeField]
	private Image background;

	[SerializeField]
	private Text label;

	[SerializeField]
	private Text ownedValue;

	[SerializeField]
	private Text supplyValue;

	[SerializeField]
	private ShowHoverText buttonHoverText;

	public AircraftDefinition definition;

	[SerializeField]
	private AircraftSelectionMenu selectionMenu;

	[SerializeField]
	private AircraftInventoryMenu inventoryMenu;

	private float timeSinceLastUpdate;

	private bool isActive;

	private bool isOwned;

	private bool isAvailable;

	private Player localPlayer;

	public void Initialize(AircraftInventoryMenu inventory, Player localPlayer, HoverText hover)
	{
		this.localPlayer = localPlayer;
		inventoryMenu = inventory;
		label.text = definition.code;
		image.sprite = definition.mapIcon;
		buttonHoverText = base.gameObject.GetComponent<ShowHoverText>();
		buttonHoverText.SetHover(hover);
		SetHoverText();
	}

	public void Setup(bool owned, bool available = true)
	{
		button.enabled = true;
		button.interactable = true;
		isOwned = owned;
		isAvailable = available;
		SetColor();
		UpdateOwned();
	}

	public void SetColor()
	{
		Color white = Color.white;
		white = ((isOwned && isAvailable) ? Color.green : ((isOwned && !isAvailable) ? Color.yellow : ((isOwned || isAvailable) ? Color.white : Color.grey)));
		image.color = white;
		label.color = white;
		ownedValue.color = white;
		supplyValue.color = white;
		border.enabled = isActive;
		border.color = white;
		background.color = (isActive ? Color.black : (0.45f * Color.gray));
	}

	public void UpdateOwned()
	{
		timeSinceLastUpdate = Time.timeSinceLevelLoad;
		int num = localPlayer.OwnedAirframeTypeCount(definition, includeReserved: true);
		if (num == 0)
		{
			ownedValue.text = "0";
			ownedValue.enabled = false;
		}
		else
		{
			ownedValue.text = $"{num}";
			ownedValue.enabled = true;
		}
		/*int unitSupply = localPlayer.HQ.GetUnitSupply(definition);
		if (unitSupply > 0)
		{
			supplyValue.text = $"{unitSupply}";
			supplyValue.enabled = true;
		}
		else
		{
			supplyValue.text = "0";
			supplyValue.enabled = false;
		}
		if (localPlayer.PossessesReservedAirframe(definition))
		{
			ownedValue.color = Color.yellow;
			ownedValue.text = "R";
		}
		SetHoverText();*/
	}

	public void SetHoverText()
	{
		string text = definition.code ?? "";
		int num = localPlayer.OwnedAirframeTypeCount(definition, includeReserved: true);
		text += $"\nRank: {definition.aircraftParameters.rankRequired}";
		text += $"\nOwned: {num}";
		if (localPlayer.PossessesReservedAirframe(definition))
		{
			text += "(R)";
		}
		/*int unitSupply = localPlayer.HQ.GetUnitSupply(definition);
		text += $"\nFaction: {unitSupply}";
		if (!isAvailable)
		{
			text += "\nNot available here";
		}
		buttonHoverText.SetText(text);*/
	}

	private void Update()
	{
		if (Time.timeSinceLevelLoad - timeSinceLastUpdate > 0.5f)
		{
			UpdateOwned();
		}
	}

	public void OnClick()
	{
		if (inventoryMenu != null)
		{
			inventoryMenu.SetSelectedType(definition);
		}
	}

	public void SetActive(bool state)
	{
		isActive = state;
		SetColor();
	}

	public bool CheckAvailable()
	{
		return isAvailable;
	}
}
