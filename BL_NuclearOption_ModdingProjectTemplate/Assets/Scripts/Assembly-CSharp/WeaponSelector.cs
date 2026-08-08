using System;
using System.Collections.Generic;
using System.ComponentModel;
using NuclearOption.Networking;
using NuclearOption.SavedMission;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class WeaponSelector : MonoBehaviour
{
	private enum OptionType
	{
		DifferentValues = 0,
		Empty = 1,
		Weapon = 2
	}

	[SerializeField]
	private TMP_Text hardpointName;

	[FormerlySerializedAs("weaponOptions")]
	[SerializeField]
	private TMP_Dropdown dropdown;

	[SerializeField]
	private TextMeshProUGUI dropdownText;

	[SerializeField]
	private Color dropdownTextNormalColor;

	[FormerlySerializedAs("dropdownTextNormalInactive")]
	[SerializeField]
	private Color dropdownTextInactiveColor;

	private readonly List<WeaponMount> getCache = new List<WeaponMount>();

	private readonly List<(OptionType type, WeaponMount mount)> dropdownOptions = new List<(OptionType, WeaponMount)>();

	private HardpointSet hardpointSet;

	private Aircraft aircraft;

	private LayoutElement layoutElement;

	[SerializeField]
	private Button symmetryButton;

	[SerializeField]
	private WeaponSelector symmetryWeaponSelector;

	private bool forceSymmetry;

	public event Action<WeaponMount> OnWeaponSelected;

	public event Action<WeaponMount> OnHover;

	private void Awake()
	{
		dropdown.onValueChanged.AddListener(DropdownChanged);
	}

	public void DropdownChanged(int index)
	{
		WeaponMount value = GetValue();
		if (dropdownOptions[0].type == OptionType.DifferentValues)
		{
			if (index == 0)
			{
				return;
			}
			dropdownOptions.RemoveAt(0);
			dropdown.options.RemoveAt(0);
			dropdown.SetValueWithoutNotify(index - 1);
			dropdown.RefreshShownValue();
		}
		if (symmetryWeaponSelector != null && !symmetryWeaponSelector.gameObject.activeSelf)
		{
			symmetryWeaponSelector.SetValue(value);
			symmetryWeaponSelector.DropdownChanged(index);
		}
		this.OnWeaponSelected?.Invoke(value);
	}

	public void Initialize(Aircraft aircraft, HardpointSet hardpointSet, FactionHQ HQ, Airbase airbase)
	{
		layoutElement = base.gameObject.GetComponent<LayoutElement>();
		this.aircraft = aircraft;
		this.hardpointSet = hardpointSet;
		hardpointName.text = hardpointSet.name;
		symmetryButton.gameObject.SetActive(value: false);
		PopulateOptions(airbase, HQ, hardpointSet);
		aircraft.weaponManager.OnWeaponsLoaded += UpdateSymmetry;
	}

	private void OnDestroy()
	{
		if (aircraft != null && aircraft.weaponManager != null)
		{
			aircraft.weaponManager.OnWeaponsLoaded -= UpdateSymmetry;
		}
	}

	public void LinkSymmetry(WeaponSelector otherSelector)
	{
		symmetryWeaponSelector = otherSelector;
	}

	public void UpdateSymmetry()
	{
		if (!(symmetryWeaponSelector == null))
		{
			forceSymmetry = symmetryWeaponSelector.GetValue() == GetValue();
			symmetryButton.gameObject.SetActive(forceSymmetry);
			symmetryWeaponSelector.gameObject.SetActive(!forceSymmetry);
			hardpointName.text = (forceSymmetry ? hardpointSet.SymmetryName : hardpointSet.name);
		}
	}

	public void SplitSymmetry()
	{
		if (!(symmetryWeaponSelector == null))
		{
			forceSymmetry = false;
			symmetryWeaponSelector.gameObject.SetActive(value: true);
			hardpointName.text = hardpointSet.name;
			symmetryButton.gameObject.SetActive(value: false);
		}
	}

	public void HoverWeapon(TMP_Text weaponLabel)
	{
		string text = weaponLabel.text;
		WeaponMount obj = null;
		foreach (var dropdownOption in dropdownOptions)
		{
			if (dropdownOption.type == OptionType.Weapon && dropdownOption.mount.mountName == text)
			{
				obj = dropdownOption.mount;
				break;
			}
		}
		this.OnHover?.Invoke(obj);
	}

	public void Initialize(HardpointSet hardpointSet, SavedLoadout.SelectedMount? selectedMount)
	{
		layoutElement = base.gameObject.GetComponent<LayoutElement>();
		this.hardpointSet = hardpointSet;
		hardpointName.text = hardpointSet.name;
		PopulateOptions(null, null, hardpointSet, !selectedMount.HasValue);
		if (selectedMount.HasValue)
		{
			WeaponMount weaponMount = selectedMount.Value.GetWeaponMount(hardpointSet);
			SetValue(weaponMount);
		}
		else
		{
			dropdown.SetValueWithoutNotify(0);
		}
		symmetryButton.gameObject.SetActive(value: false);
	}

	public void SetInteractable(Loadout loadout)
	{
		bool flag = hardpointSet.BlockedByOtherHardpoint(loadout);
		dropdown.interactable = !flag;
		dropdownText.color = (flag ? dropdownTextInactiveColor : dropdownTextNormalColor);
		if (flag)
		{
			dropdown.value = 0;
			hardpointName.text = "-";
		}
		else
		{
			hardpointName.text = (forceSymmetry ? hardpointSet.SymmetryName : hardpointSet.name);
		}
		if (layoutElement != null)
		{
			layoutElement.flexibleWidth = (dropdown.interactable ? 3f : 1f);
		}
	}

	public void SetHidden(bool hidden)
	{
		base.gameObject.SetActive(!hidden);
	}

	private void PopulateOptions(Airbase airbase, FactionHQ hq, HardpointSet hardpointSet, bool includeDifferentMultiselectValue = false)
	{
		dropdown.ClearOptions();
		this.hardpointSet = hardpointSet;
		FactionHQ factionHQ = hq;
		Player localPlayer = null;
		if (GameManager.gameState != GameState.Editor)
		{
			factionHQ = hq;
			GameManager.GetLocalPlayer<Player>(out localPlayer);
		}
		else
		{
			factionHQ = null;
		}
		WeaponChecker.GetAvailableWeaponsNonAlloc(localPlayer, hardpointSet, airbase, factionHQ, allowEmpty: false, getCache);
		getCache.Sort((WeaponMount x, WeaponMount y) => x.mountName.CompareTo(y.mountName));
		if (includeDifferentMultiselectValue)
		{
			dropdownOptions.Add((OptionType.DifferentValues, null));
		}
		dropdownOptions.Add((OptionType.Empty, null));
		foreach (WeaponMount item3 in getCache)
		{
			dropdownOptions.Add((OptionType.Weapon, item3));
		}
		for (int num = 0; num < dropdownOptions.Count; num++)
		{
			(OptionType type, WeaponMount mount) tuple = dropdownOptions[num];
			OptionType item = tuple.type;
			WeaponMount item2 = tuple.mount;
			string text = item switch
			{
				OptionType.DifferentValues => "-", 
				OptionType.Empty => "Empty", 
				OptionType.Weapon => item2.mountName, 
				_ => throw new InvalidEnumArgumentException(), 
			};
			dropdown.options.Add(new TMP_Dropdown.OptionData(text));
		}
	}

	public void SetValue(WeaponMount weaponMount)
	{
		int num = dropdownOptions.FindIndex(delegate((OptionType type, WeaponMount mount) x)
		{
			if (x.type == OptionType.Empty && weaponMount == null)
			{
				return true;
			}
			return (x.type == OptionType.Weapon && x.mount == weaponMount) ? true : false;
		});
		if (num == -1)
		{
			Debug.LogWarning($"Count not find {weaponMount} in dropdown options");
		}
		dropdown.SetValueWithoutNotify(num);
	}

	public WeaponMount GetValue()
	{
		return dropdownOptions[dropdown.value].mount;
	}
}
