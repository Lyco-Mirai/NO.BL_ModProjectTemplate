using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NuclearOption.MissionEditorScripts.Buttons;
using NuclearOption.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ContributeToFaction : MonoBehaviour
{
	[Serializable]
	public enum ContributionType
	{
		None = 0,
		Funds = 1,
		Vehicles = 2
	}

	private ContributionType contributionType;

	[SerializeField]
	private AircraftSelectionMenu selectionMenu;

	[SerializeField]
	private TMP_Text contributePercentage;

	[SerializeField]
	private TMP_Text contributeValue;

	[SerializeField]
	private TMP_Text giveAirframeNumber;

	[SerializeField]
	private TMP_Text giveAirframeValue;

	[SerializeField]
	private TMP_Text giveVehicleValue;

	[SerializeField]
	private TMP_Text currentFunds;

	[SerializeField]
	private TMP_Text remainingFunds;

	[SerializeField]
	private Button fundsButton;

	[SerializeField]
	private Button aircraftButton;

	[SerializeField]
	private Button vehiclesButton;

	[SerializeField]
	private Slider contributeSlider;

	[SerializeField]
	private Slider giveAirframesSlider;

	[SerializeField]
	private Button contributeConfirm;

	[SerializeField]
	private Button giveAirframesConfirm;

	[SerializeField]
	private Button giveVehicleConfirm;

	[SerializeField]
	private TMP_Dropdown aircraftDropdown;

	[SerializeField]
	private GameObject convoySelectPrefab;

	[SerializeField]
	private Transform convoySelectBackground;

	[SerializeField]
	private HoverText hoverText;

	[SerializeField]
	private ShowHoverText convoyHoverText;

	private List<UnitDefinition> sortedUnitList;

	private Faction.ConvoyGroup selectedConvoy;

	private Player localPlayer;

	private FactionHQ localHq;

	private bool canSpawnConvoy;

	private void OnEnable()
	{
		ResetValues();
	}

	public void SetContributeVehicles()
	{
		selectedConvoy = null;
		contributionType = ContributionType.Vehicles;
		RefreshVehicleList();
		CheckValues();
	}

	public void SetContributeFunds()
	{
		contributionType = ContributionType.Funds;
		CheckValues();
	}

	public void ResetValues()
	{
		if (!GameManager.GetLocalPlayer<Player>(out localPlayer))
		{
			Debug.LogError("AircraftInventoryMenu should not be active without a local player");
			return;
		}/*
		localHq = localPlayer.HQ;
		if (localHq == null)
		{
			Debug.LogError("AircraftInventoryMenu should not be active without a local faction");
			return;
		}*/
		CheckAllowedToSpawnConvoy().Forget();
		//vehiclesButton.interactable = !localPlayer.HQ.preventDonation && canSpawnConvoy;
		fundsButton.interactable = localPlayer.Allocation > 0f;
		selectedConvoy = null;
		contributeSlider.value = 0f;
		giveAirframesSlider.value = 0f;
		contributionType = ContributionType.None;
		CheckValues();
	}

	public void CheckValues()
	{
		if (contributionType == ContributionType.Funds)
		{
			if (localPlayer.Allocation > 0f)
			{
				contributePercentage.text = $"{100f * contributeSlider.value:F0}%";
				contributeValue.text = "-" + UnitConverter.ValueReading(localPlayer.Allocation * contributeSlider.value);
				contributeConfirm.interactable = contributeSlider.value > 0f;
			}
			else
			{
				contributePercentage.text = "0%";
				contributeValue.text = "$0";
				contributeConfirm.interactable = false;
			}
		}
		if (contributionType == ContributionType.Vehicles)
		{
			float num = ((selectedConvoy != null) ? selectedConvoy.GetCost() : 0f);
			giveVehicleValue.text = "-" + UnitConverter.ValueReading(num);
			giveVehicleConfirm.interactable = num > 0f;
		}
		CalculateFunds();
	}

	public void CalculateFunds()
	{
		currentFunds.text = UnitConverter.ValueReading(localPlayer.Allocation) ?? "";
		float num = localPlayer.Allocation;
		if (contributionType == ContributionType.Funds && contributeSlider.value > 0f)
		{
			num -= localPlayer.Allocation * contributeSlider.value;
		}
		if (contributionType == ContributionType.Vehicles && selectedConvoy != null)
		{
			num -= selectedConvoy.GetCost();
		}
		remainingFunds.text = UnitConverter.ValueReading(num) ?? "";
	}

	public void CancelContribution()
	{
		contributionType = ContributionType.None;
	}

	public void SelectConvoy(Faction.ConvoyGroup convoy)
	{
		selectedConvoy = convoy;
		CheckValues();
	}

	public void ContributeFunds()
	{
		if (localPlayer.Allocation > 0f)
		{
			float amount = localPlayer.Allocation * contributeSlider.value;
			localPlayer.CmdDonateFactionFunds(amount);
		}
	}

	public void PurchaseConvoy()
	{
		if (selectedConvoy == null)
		{
			ColorLog<ContributeToFaction>.LogError("PurchaseConvoy called but selectedConvoy is null");
			return;
		}
		int num = localHq.faction.GetConvoyGroups().IndexOf(selectedConvoy);
		if (num == -1)
		{
			ColorLog<ContributeToFaction>.LogError("PurchaseConvoy called but selectedConvoy is not in faction's convoy list");
		}
		else
		{
			localPlayer.CmdPurchaseConvoy(num);
		}
	}

	public void RefreshVehicleList()
	{
		List<Faction.ConvoyGroup> convoyGroups = localHq.faction.GetConvoyGroups();
		for (int i = 0; i < convoyGroups.Count; i++)
		{
			Faction.ConvoyGroup convoyGroup = convoyGroups[i];
			ConvoyPurchaseOption component = UnityEngine.Object.Instantiate(convoySelectPrefab, convoySelectBackground).GetComponent<ConvoyPurchaseOption>();
			component.SetButtonHoverText(hoverText);
			component.Initialize(this, localPlayer, convoyGroup);
		}
	}

	public void RefreshAircraftList()
	{
		aircraftDropdown.ClearOptions();
		if (sortedUnitList == null)
		{
			sortedUnitList = new List<UnitDefinition>();
		}
		else
		{
			sortedUnitList.Clear();
		}
		sortedUnitList.AddRange(Encyclopedia.i.aircraft);
		sortedUnitList.Sort((UnitDefinition a, UnitDefinition b) => a.value.CompareTo(b.value));
		for (int num = 0; num < sortedUnitList.Count; num++)
		{
			AddAircraftToDropdown(sortedUnitList[num].unitName + " (" + UnitConverter.ValueReading(sortedUnitList[num].value) + ")");
		}
		aircraftDropdown.value = 0;
		aircraftDropdown.RefreshShownValue();
		void AddAircraftToDropdown(string text)
		{
			aircraftDropdown.options.Add(new TMP_Dropdown.OptionData(text));
		}
	}

	public async UniTask CheckAllowedToSpawnConvoy()
	{
		float num = await localPlayer.CmdGetDelaySpawnConvoy();
		canSpawnConvoy = num <= 0f;
		string text = (canSpawnConvoy ? "Contribute a group of vehicles to the faction" : $"Wait {num:F0}s before next contribution of vehicles");
		convoyHoverText.SetText(text);
	}
}
