using System.Collections.Generic;
using System.Linq;
using NuclearOption.SavedMission;
using TMPro;
using UnityEngine;

namespace NuclearOption.MissionEditorScripts
{
	public class FactionDataField : DataField, IDataField<string>
	{
		[SerializeField]
		private TMP_Dropdown dropdown;

		private List<string> factionNames;

		private IValueWrapper<string> wrapper;

		private string noFactionString;

		public void SetNoFactionString(string label)
		{
			noFactionString = label;
			factionNames[0] = label;
			dropdown.ClearOptions();
			dropdown.AddOptions(factionNames);
		}

		protected override void SetFieldInteractable(bool value)
		{
			dropdown.interactable = value;
		}

		protected override void AwakeSetup()
		{
			factionNames = MissionManager.CurrentMission.factions.Select((MissionFaction x) => x.factionName).Prepend("None").ToList();
			dropdown.ClearOptions();
			dropdown.AddOptions(factionNames);
			dropdown.onValueChanged.AddListener(OnValueChanged);
		}

		private void OnDestroy()
		{
			wrapper?.UnregisterOnChange(this);
		}

		public void Setup(string label, IValueWrapper<string> wrapper)
		{
			base.label.text = label;
			Setup(wrapper);
		}

		public void Setup(IValueWrapper<string> wrapper)
		{
			this.wrapper?.UnregisterOnChange(this);
			this.wrapper = wrapper;
			wrapper.RegisterOnChange(this, SetDropdown);
			SetDropdown(wrapper.Value);
			base.Interactable = true;
		}

		private void SetDropdown(string value)
		{
			int num = ((!FactionHelper.EmptyOrNoFaction(value)) ? factionNames.IndexOf(value) : 0);
			if (num == -1)
			{
				Debug.LogError("Could not find faction with name " + value);
			}
			dropdown.SetValueWithoutNotify(num);
		}

		private void OnValueChanged(int index)
		{
			string text = factionNames[index];
			string value = ((text == noFactionString) ? "None" : text);
			wrapper.SetValue(value, this);
		}
	}
}
