using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NuclearOption;
using NuclearOption.MissionEditorScripts.Buttons;
using NuclearOption.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AircraftInventoryMenu : MonoBehaviour
{
	[SerializeField]
	private AircraftSelectionMenu selectionMenu;

	[SerializeField]
	private List<AircraftSelectionButton> listAircraftButtons = new List<AircraftSelectionButton>();

	[SerializeField]
	private HoverText hoverText;

	[SerializeField]
	private ShowHoverText requestButtonHoverText;

	[SerializeField]
	private ShowHoverText buyButtonHoverText;

	[SerializeField]
	private ShowHoverText sellButtonHoverText;

	[SerializeField]
	private ShowHoverText expandButtonHoverText;

	private AircraftDefinition selectedType;

	public GameObject buttonPrefab;

	public Transform buttonsContainer;

	[SerializeField]
	private TMP_Text requestLabel;

	[SerializeField]
	private TMP_Text buyLabel;

	[SerializeField]
	private TMP_Text sellLabel;

	[SerializeField]
	private TMP_Text expandLabel;

	[SerializeField]
	private Button requestButton;

	[SerializeField]
	private Button buyButton;

	[SerializeField]
	private Button sellButton;

	private bool checkedReserved;

	private bool hasReserved;

	private bool reserveCheckResult;

	[SerializeField]
	private GameObject reserveNotice;

	[SerializeField]
	private TMP_Text reserveNoticeText;

	private List<AircraftDefinition> sortedAircraftList = new List<AircraftDefinition>();

	[SerializeField]
	private AudioClip selectSound;

	[SerializeField]
	private AudioClip deselectSound;

	private Player localPlayer;

	public event Action onChange;

	public void Initialize(Player localPlayer)
	{
		this.localPlayer = localPlayer;
		checkedReserved = false;
		localPlayer.onReserveNotice += Player_OnReserveNotice;
		if (buttonsContainer.childCount > 0 || listAircraftButtons.Count > 0)
		{
			foreach (Transform item in buttonsContainer)
			{
				UnityEngine.Object.Destroy(item.gameObject);
			}
			listAircraftButtons.Clear();
			sortedAircraftList.Clear();
		}
		sortedAircraftList.AddRange(Encyclopedia.i.aircraft);
		sortedAircraftList.Sort((AircraftDefinition a, AircraftDefinition b) => a.value.CompareTo(b.value));
		for (int num = 0; num < sortedAircraftList.Count; num++)
		{
			AircraftDefinition aircraftDefinition = sortedAircraftList[num];
			if (!aircraftDefinition.NotAllowed(MissionManager.AllowEventContent))
			{
				AircraftSelectionButton component = UnityEngine.Object.Instantiate(buttonPrefab, buttonsContainer).GetComponent<AircraftSelectionButton>();
				component.definition = aircraftDefinition;
				listAircraftButtons.Add(component);
				component.Initialize(this, localPlayer, hoverText);
			}
		}
		ToggleAircraftButtons();
	}

	public void Refresh()
	{
		foreach (AircraftSelectionButton listAircraftButton in listAircraftButtons)
		{
			listAircraftButton.Setup(localPlayer.OwnsAirframe(listAircraftButton.definition, includeReserved: true), selectionMenu.CanFlyAircraft(listAircraftButton.definition));
			listAircraftButton.SetActive(selectedType != null && selectedType == listAircraftButton.definition);
		}
		UpdateSellButton();
		UpdateBuyButton();
		UpdateRequestButton().Forget();
		LayoutRebuilder.ForceRebuildLayoutImmediate(base.transform as RectTransform);
	}

	public void ToggleAircraftButtons()
	{
		buttonsContainer.gameObject.SetActive(!buttonsContainer.gameObject.activeSelf);
		if (buttonsContainer.gameObject.activeSelf)
		{
			expandLabel.transform.eulerAngles = new Vector3(0f, 0f, 180f);
			expandButtonHoverText.SetText("Hide aircraft buttons");
		}
		else
		{
			expandLabel.transform.eulerAngles = new Vector3(0f, 0f, 0f);
			expandButtonHoverText.SetText("Show aircraft buttons");
		}
	}

	public void UpdateSellButton()
	{
		if (selectedType != null && localPlayer.OwnsAirframe(selectedType, includeReserved: true))
		{
			if (localPlayer.PossessesReservedAirframe(selectedType))
			{
				sellLabel.text = "Return\n" + selectedType.unitName;
				sellButtonHoverText.SetText("Return " + selectedType.unitName);
			}
			else
			{
				sellLabel.text = "Sell\n" + selectedType.unitName;
				float valueInMillions = localPlayer.Allocation + selectedType.value;
				string text = "Sell " + selectedType.unitName;
				text = text + "\nFunds: " + UnitConverter.ValueReading(localPlayer.Allocation);
				text = text + "\nValue: +" + UnitConverter.ValueReading(selectedType.value);
				text = text + "\nRemain: " + UnitConverter.ValueReading(valueInMillions);
				sellButtonHoverText.SetText(text);
			}
			sellButton.enabled = true;
			sellButton.interactable = true;
		}
		else
		{
			sellLabel.text = "Return / Sell";
			sellButtonHoverText.SetText("You do not own this type of aircraft");
			sellButton.enabled = false;
			sellButton.interactable = false;
		}
	}

	public void UpdateBuyButton()
	{
		buyLabel.text = $"Purchase for\n${selectedType.value}m";
		if (selectedType != null && localPlayer.Allocation > selectedType.value && selectionMenu.CanFlyAircraft(selectedType))
		{
			float valueInMillions = localPlayer.Allocation - selectedType.value;
			string text = "Buy " + selectedType.unitName;
			text = text + "\nFunds: " + UnitConverter.ValueReading(localPlayer.Allocation);
			text = text + "\nCost: -" + UnitConverter.ValueReading(selectedType.value);
			text = text + "\nRemain: " + UnitConverter.ValueReading(valueInMillions);
			buyButtonHoverText.SetText(text);
			buyButton.enabled = true;
			buyButton.interactable = true;
		}
		else
		{
			if (!selectionMenu.CanFlyAircraft(selectedType))
			{
				buyButtonHoverText.SetText("Aircraft not available at this airbase");
			}
			else if (localPlayer.Allocation < selectedType.value)
			{
				buyButtonHoverText.SetText("Insufficient funds for this aircraft");
			}
			buyButton.enabled = false;
			buyButton.interactable = false;
		}
	}

	public async UniTask UpdateRequestButton()
	{
		await CheckReservingAirframe(selectedType);
		bool flag = localPlayer.OwnsAirframe(selectedType, includeReserved: true);
		bool flag2 = localPlayer.PossessesReservedAirframe();
		bool flag3 = localPlayer.PlayerRank >= selectedType.aircraftParameters.rankRequired;
		requestLabel.text = $"Requisition\n via Rank ({selectedType.aircraftParameters.rankRequired})";
		if (selectedType != null && selectionMenu.CanFlyAircraft(selectedType) && checkedReserved && !flag2 && !hasReserved && !flag && flag3 && reserveCheckResult)
		{
			requestButton.interactable = true;
			requestButton.enabled = true;
			return;
		}
		if (!selectionMenu.CanFlyAircraft(selectedType))
		{
			requestButtonHoverText.SetText("Aircraft not available at this airbase");
		}
		requestButton.enabled = false;
		requestButton.interactable = false;
	}

	private async UniTask CheckReservingAirframe(AircraftDefinition selectedType)
	{
		if (selectedType == null)
		{
			Debug.LogError("Can't send RPC because selectedType is null");
			return;
		}
		ReserveNotice reserveNotice = await localPlayer.CmdCheckReservingAirframe(selectedType);
		if (reserveNotice.outcome == ReserveEvent.Invalid)
		{
			Debug.LogError("Server returned Invalid for CmdCheckReservingAirframe");
		}
		else
		{
			OnReserveCheck(reserveNotice);
		}
	}

	private void OnReserveCheck(ReserveNotice reserveNotice)
	{
		checkedReserved = true;
		hasReserved = reserveNotice.isReserving;
		if (reserveNotice.outcome == ReserveEvent.rejectedRank)
		{
			requestButtonHoverText.SetText($"Unable to requisition: Requires Rank {reserveNotice.aircraftDefinition.aircraftParameters.rankRequired}");
			reserveCheckResult = false;
		}
		else if (reserveNotice.outcome == ReserveEvent.rejectedPossessesReserved)
		{
			requestButtonHoverText.SetText("Unable to requisition: You must return your other requisitioned aircraft first.");
			reserveCheckResult = false;
		}
		else if (reserveNotice.outcome == ReserveEvent.rejectedAfford)
		{
			requestButtonHoverText.SetText("Unable to requisition: you can afford this aircraft.");
			reserveCheckResult = false;
		}
		else if (reserveNotice.outcome == ReserveEvent.rejectedDuplicate)
		{
			requestButtonHoverText.SetText("Unable to requisition: Request already in process.");
			reserveCheckResult = false;
		}
		else if (reserveNotice.outcome == ReserveEvent.rejectedOwned)
		{
			requestButtonHoverText.SetText("Unable to requisition: You already hold this aircraft.");
			reserveCheckResult = false;
		}
		else
		{
			requestButtonHoverText.SetText("Request this aircraft when available.");
			reserveCheckResult = true;
		}
	}

	private void Player_OnReserveNotice(ReserveNotice reserveNotice)
	{
		if (reserveNotice.outcome != ReserveEvent.granted && reserveNotice.outcome != ReserveEvent.cancelledAfford && reserveNotice.outcome != ReserveEvent.cancelledOwned && reserveNotice.outcome != ReserveEvent.cancelledRank && !(this == null))
		{
			this.reserveNotice.SetActive(value: true);
			requestButton.interactable = false;
			hasReserved = true;
			string unitName = reserveNotice.aircraftDefinition.unitName;
			unitName = ((!unitName.EndsWith("ss") && !unitName.EndsWith("ch") && !unitName.EndsWith("sh") && !unitName.EndsWith("x")) ? (unitName + "s") : (unitName + "es"));
			if (reserveNotice.queuePosition == 1)
			{
				reserveNoticeText.text = "No reserve " + unitName + " currently available. You are first in queue for this airframe";
			}
			else if (reserveNotice.queuePosition > 1)
			{
				reserveNoticeText.text = $"No reserve {unitName} currently available. You are number {reserveNotice.queuePosition} in queue for this airframe";
			}
			else if (reserveNotice.queuePosition == -1)
			{
				reserveNoticeText.text = "Already requested an airframe";
			}
			else if (reserveNotice.queuePosition == -2)
			{
				reserveNoticeText.text = "Insufficient rank to request this airframe";
			}
			else if (reserveNotice.queuePosition == -3)
			{
				reserveNoticeText.text = "You already hold one of this airframe";
			}
		}
	}

	public void SellAirframe()
	{
		if (!(selectedType == null) && localPlayer.OwnsAirframe(selectedType, includeReserved: true))
		{
			if (localPlayer.OwnsAirframe(selectedType, includeReserved: false))
			{
				localPlayer.CmdSellAirframe(selectedType);
			}
			else if (localPlayer.OwnsAirframe(selectedType, includeReserved: true))
			{
				localPlayer.CmdReturnAirframe(selectedType);
			}
			SoundManager.PlayInterfaceOneShot(deselectSound);
			Refresh();
			this.onChange();
		}
	}

	public void BuyAirframe()
	{
		if (!(selectedType == null) && !(localPlayer.Allocation < selectedType.value))
		{
			localPlayer.CmdPurchaseAirframe(selectedType);
			SoundManager.PlayInterfaceOneShot(selectSound);
			Refresh();
			this.onChange();
		}
	}

	public void RequestAirframe()
	{
		if (!(selectedType == null))
		{
			localPlayer.CmdRequestReserveAirframe(selectedType);
			reserveCheckResult = false;
			hasReserved = true;
			requestButton.interactable = false;
			Refresh();
			this.onChange();
		}
	}

	public void PreviousAircraft()
	{
		int num = listAircraftButtons.FindIndex((AircraftSelectionButton _x) => _x.definition == selectedType);
		bool flag = false;
		AircraftDefinition definition = selectedType;
		while (!flag)
		{
			num--;
			if (num < 0)
			{
				num = listAircraftButtons.Count - 1;
			}
			definition = listAircraftButtons[num].definition;
			flag = selectionMenu.CanFlyAircraft(definition);
			if (flag || definition == selectedType)
			{
				break;
			}
		}
		SetSelectedType(definition);
	}

	public void NextAircraft()
	{
		int num = listAircraftButtons.FindIndex((AircraftSelectionButton _x) => _x.definition == selectedType);
		bool flag = false;
		AircraftDefinition definition = selectedType;
		while (!flag)
		{
			num++;
			if (num > listAircraftButtons.Count - 1)
			{
				num = 0;
			}
			definition = listAircraftButtons[num].definition;
			flag = selectionMenu.CanFlyAircraft(definition);
			if (flag || definition == selectedType)
			{
				break;
			}
		}
		SetSelectedType(definition);
	}

	public void OnAircraftButtonClick(AircraftSelectionButton button)
	{
		if (button.definition != selectedType)
		{
			SetSelectedType(button.definition);
		}
	}

	public void SetSelectedType(AircraftDefinition definition)
	{
		selectedType = definition;
		checkedReserved = false;
		if (selectedType != selectionMenu.GetSelectedType() && selectionMenu.CanFlyAircraft(selectedType))
		{
			selectionMenu.SetSelectedType(selectedType);
		}
		Refresh();
	}

	public void OnDestroy()
	{
		if ((object)localPlayer != null)
		{
			localPlayer.onReserveNotice -= Player_OnReserveNotice;
		}
	}
}
