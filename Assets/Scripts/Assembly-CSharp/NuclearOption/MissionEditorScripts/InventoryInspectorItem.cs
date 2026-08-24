using NuclearOption.SavedMission;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts
{
	public class InventoryInspectorItem : MonoBehaviour
	{
		[SerializeField]
		private Button addButton;

		[SerializeField]
		private Button removeButton;

		[SerializeField]
		private TextMeshProUGUI unitName;

		private InventoryInspector inventoryInspector;

		public UnitDefinition UnitDefinition { get; private set; }

		private void Awake()
		{
			addButton.onClick.AddListener(AddOne);
			removeButton.onClick.AddListener(RemoveOne);
		}

		public void SetEntry(InventoryInspector inventoryInspector, UnitCount supply)
		{
			this.inventoryInspector = inventoryInspector;
			UnitDefinition = Encyclopedia.Lookup[supply.UnitType];
			unitName.text = $"{UnitDefinition.unitName}[{supply.Count}]";
		}

		public void AddOne()
		{
			inventoryInspector.AddOne(this);
		}

		public void RemoveOne()
		{
			inventoryInspector.RemoveOne(this);
		}
	}
}
