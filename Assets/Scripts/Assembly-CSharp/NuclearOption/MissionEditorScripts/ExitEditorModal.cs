using System;
using System.Text;
using Cysharp.Threading.Tasks;
using NuclearOption.SavedMission;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts
{
	public class ExitEditorModal : MonoBehaviour
	{
		[SerializeField]
		private GameObject exitModal;

		[SerializeField]
		private TextMeshProUGUI bodyText;

		[SerializeField]
		private Button exitOnlyButton;

		[SerializeField]
		private Button saveAndExitButton;

		[SerializeField]
		private Button discardAndExitButton;

		[SerializeField]
		private Button cancelButton;

		[SerializeField]
		private FileMenu fileMenu;

		private readonly StringBuilder sb = new StringBuilder();

		private void Awake()
		{
			exitOnlyButton.onClick.AddListener(ExitOnly);
			saveAndExitButton.onClick.AddListener(SaveAndExit);
			discardAndExitButton.onClick.AddListener(ExitOnly);
			cancelButton.onClick.AddListener(Hide);
		}

		public void Show()
		{
			Mission currentMission = MissionManager.CurrentMission;
			bool flag = EditorMissionGroup.MainSaveHasChanged(currentMission);
			bool flag2 = currentMission.LoadKey.HasValue && currentMission.LoadKey.Value.IsUserOrEditorGroup();
			MissionKey orCreateUserSaveKey = MissionSaveLoad.GetOrCreateUserSaveKey(currentMission);
			MissionFileInfo userGroupInfo = MissionSaveLoad.GetFileInfo(orCreateUserSaveKey);
			string saveName = currentMission.LoadKey?.Name;
			MissionSaveLoad.ValidateName(ref saveName, allowEmpty: true);
			string text = saveName.AddColor(new Color(0.5f, 1f, 1f));
			sb.Clear();
			sb.Append(flag ? ("Save changes to <b>" + text + "</b> before exiting?\n\n") : ("<b>" + text + "</b> is up to date.\n\n"));
			if (flag2 || flag)
			{
				sb.Append("<size=80%><color=#999999>");
				if (flag2)
				{
					sb.Append("Last save: " + GetLastEditString());
				}
				else if (flag)
				{
					sb.Append("Editing built-in mission. Saving will open the Save Menu to create a copy");
				}
				sb.Append("</color></size>");
			}
			if (flag)
			{
				sb.Append("\n<size=70%><color=#999999>(A temporary auto-save will be created if you discard)</color></size>");
			}
			bodyText.text = sb.ToString();
			saveAndExitButton.gameObject.SetActive(flag);
			discardAndExitButton.gameObject.SetActive(flag);
			exitOnlyButton.gameObject.SetActive(!flag);
			exitModal.SetActive(value: true);
			string GetLastEditString()
			{
				DateTime? time = (userGroupInfo.HasExtraSaves ? userGroupInfo.ExpandedLastEdit : userGroupInfo.LastEdit);
				if (!time.HasValue)
				{
					return "Never";
				}
				return SmartDateFormatter.ToSmartDate(time, SmartDateFormatter.Theme.None);
			}
		}

		public void Hide()
		{
			exitModal.SetActive(value: false);
		}

		public void SaveAndExit()
		{
			Mission currentMission = MissionManager.CurrentMission;
			if (currentMission.LoadKey.HasValue && currentMission.LoadKey.Value.IsUserOrEditorGroup())
			{
				MissionSaveLoad.SaveMission(currentMission);
				MissionEditor.ExitEditor().Forget();
			}
			else
			{
				Hide();
				fileMenu.Show(FileMenu.TabIndex.Save);
			}
		}

		public void ExitOnly()
		{
			MissionEditor.ExitEditor().Forget();
		}
	}
}
