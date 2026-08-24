using NuclearOption.MissionEditorScripts;
using NuclearOption.SavedMission;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.UI
{
	public class FileMenuHoverDropdown : HoverMenuBase
	{
		[Header("References")]
		[SerializeField]
		private FileMenu fileMenu;

		[SerializeField]
		private GameObject saveMessageHolder;

		[SerializeField]
		private TextMeshProUGUI saveMessage;

		[Header("Dropdown Buttons")]
		[SerializeField]
		private Button fileButton;

		[SerializeField]
		private Button newButton;

		[SerializeField]
		private Button loadButton;

		[SerializeField]
		private Button quickSaveButton;

		[SerializeField]
		private Button saveAsButton;

		[SerializeField]
		private Button settingsButton;

		[Header("Quick Save Style")]
		[SerializeField]
		private ButtonStyleController quickSaveButtonStyle;

		[SerializeField]
		private ButtonStyle disabledStyle;

		private ButtonStyle enabledStyle;

		protected override void Awake()
		{
			base.Awake();
			enabledStyle = quickSaveButtonStyle.GetCurrentStyle();
			newButton.onClick.AddListener(delegate
			{
				OnButtonClicked(FileMenu.TabIndex.New);
			});
			loadButton.onClick.AddListener(delegate
			{
				OnButtonClicked(FileMenu.TabIndex.Load);
			});
			quickSaveButton.onClick.AddListener(OnQuickSaveButton);
			fileButton.onClick.AddListener(OnSaveAsButton);
			saveAsButton.onClick.AddListener(OnSaveAsButton);
			settingsButton.onClick.AddListener(delegate
			{
				OnButtonClicked(FileMenu.TabIndex.Settings);
			});
		}

		protected override void OnShowPanel()
		{
			base.OnShowPanel();
			bool hasValue = MissionManager.CurrentMission.LoadKey.HasValue;
			quickSaveButton.interactable = hasValue;
			quickSaveButtonStyle.ApplyStyle(hasValue ? enabledStyle : disabledStyle);
		}

		private void OnQuickSaveButton()
		{
			MissionSaveLoad.SaveMission(MissionManager.CurrentMission);
			saveMessage.text = "Saved Mission: " + MissionManager.CurrentMission.Name;
			saveMessageHolder.SetActive(value: true);
		}

		private void OnButtonClicked(FileMenu.TabIndex tab)
		{
			fileMenu.Show(tab);
			HideMenu();
		}

		private void OnSaveAsButton()
		{
			fileMenu.Show(FileMenu.TabIndex.Save);
			HideMenu();
		}
	}
}
