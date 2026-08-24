using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts
{
	public abstract class NewPanelBase<T> : MonoBehaviour, ISidePanel where T : struct, Enum
	{
		private static int stickyIndex;

		[SerializeField]
		protected TMP_InputField nameField;

		[SerializeField]
		private TMP_Dropdown typeDropdown;

		[SerializeField]
		private Button createButton;

		[SerializeField]
		private Button closeButton;

		private List<string> options;

		public SidePanel Panel { get; set; }

		void ISidePanel.PanelRefresh()
		{
		}

		private void Start()
		{
			createButton.onClick.AddListener(Create);
			closeButton.onClick.AddListener(Close);
			options = new List<string>(EnumNames<T>.GetNames());
			options.Remove("None");
			typeDropdown.ClearOptions();
			typeDropdown.AddOptions(options);
			stickyIndex = Mathf.Clamp(stickyIndex, 0, options.Count - 1);
			typeDropdown.SetValueWithoutNotify(stickyIndex);
			typeDropdown.onValueChanged.AddListener(OnTypeChanged);
		}

		private void OnTypeChanged(int arg0)
		{
			stickyIndex = arg0;
		}

		private void Create()
		{
			int value = typeDropdown.value;
			T type = EnumNames<T>.Parse(options[value]);
			CreateItem(type);
		}

		protected abstract void CreateItem(T type);

		private void Close()
		{
			Panel.Destroy();
		}
	}
}
