using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using NuclearOption.SavedMission;
using NuclearOption.UI.HeaderSorting;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts
{
	public class MissionEditorSaveMenuV2 : MonoBehaviour, IMissionEditorFileMenu
	{
		[Header("Save list")]
		[SerializeField]
		private MissionEditorLoadMenuV2ListItem mainSaveItem;

		[SerializeField]
		private RectTransform missionListHolder;

		[SerializeField]
		private MissionEditorLoadMenuV2ListItem subItemPrefab;

		[Header("Save panel")]
		[SerializeField]
		private TMP_InputField missionNameInput;

		[SerializeField]
		private TextMeshProUGUI alreadyExistWarning;

		[SerializeField]
		private Button resetMissionNameChange;

		[SerializeField]
		private TMP_InputField missionDescriptionInput;

		[SerializeField]
		private TMP_InputField manualSaveNameInput;

		[SerializeField]
		private Button manualSaveButton;

		[Header("Save buttons")]
		[SerializeField]
		private GameObject saveNoNameBox;

		[SerializeField]
		private Button saveMainButton;

		[SerializeField]
		private Button saveAsButton;

		[SerializeField]
		private Button renameButton;

		[SerializeField]
		private Button saveAsOverExistingButton;

		[SerializeField]
		private Button renameOverExistingButton;

		[SerializeField]
		private GameObject confirmPopup;

		[SerializeField]
		private TextMeshProUGUI confirmText;

		[SerializeField]
		private Button confirmButton;

		[SerializeField]
		private Button confirmCancelButton;

		[Space]
		[SerializeField]
		private CanvasGroup savedInfo;

		[SerializeField]
		private TextMeshProUGUI saveInfoText;

		[SerializeField]
		private float saveInfoDuration;

		[Space]
		[SerializeField]
		private GameObject saveError;

		[SerializeField]
		private TextMeshProUGUI saveErrorText;

		private Action confirmAction;

		private Mission mission;

		public MissionEditorLoadMenuV2ListItem SubItemPrefab => subItemPrefab;

		private void Awake()
		{
			missionNameInput.onEndEdit.AddListener(OnNameEditEnd);
			missionNameInput.onValueChanged.AddListener(OnNameValueChanged);
			manualSaveNameInput.onValueChanged.AddListener(OnManualSaveValueChanged);
			TMP_InputField tMP_InputField = missionNameInput;
			tMP_InputField.onValidateInput = (TMP_InputField.OnValidateInput)Delegate.Combine(tMP_InputField.onValidateInput, new TMP_InputField.OnValidateInput(FileUtils.ValidateSaveName));
			TMP_InputField tMP_InputField2 = manualSaveNameInput;
			tMP_InputField2.onValidateInput = (TMP_InputField.OnValidateInput)Delegate.Combine(tMP_InputField2.onValidateInput, new TMP_InputField.OnValidateInput(FileUtils.ValidateSaveName));
			resetMissionNameChange.onClick.AddListener(ResetMissionName);
			manualSaveButton.onClick.AddListener(ManualSaveClicked);
			OnManualSaveValueChanged(manualSaveNameInput.text);
			saveMainButton.onClick.AddListener(OnSaveMainClicked);
			saveAsButton.onClick.AddListener(OnSaveAsClicked);
			renameButton.onClick.AddListener(OnRenameClicked);
			saveAsOverExistingButton.onClick.AddListener(OnSaveAsOverExistingClicked);
			renameOverExistingButton.onClick.AddListener(OnRenameOverExistingClicked);
			confirmPopup.SetActive(value: false);
			confirmButton.onClick.AddListener(ConfirmPopupConfirmClicked);
			confirmCancelButton.onClick.AddListener(ClearConfirmPopup);
		}

		private void ConfirmWrapper(string text, Action action)
		{
			confirmText.text = text;
			confirmAction = action;
			confirmPopup.SetActive(value: true);
		}

		private void ConfirmPopupConfirmClicked()
		{
			try
			{
				confirmAction?.Invoke();
			}
			finally
			{
				ClearConfirmPopup();
			}
		}

		private void ClearConfirmPopup()
		{
			confirmPopup.SetActive(value: false);
			confirmAction = null;
		}

		private void TryAction(string errorPrefix, Action action)
		{
			try
			{
				action?.Invoke();
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
				ShowSavedFailed(errorPrefix + ": " + ex.Message);
			}
		}

		private async UniTaskVoid FlashSavedInfo(string versionName = null)
		{
			CancellationToken cancel = base.destroyCancellationToken;
			saveInfoText.text = (string.IsNullOrEmpty(versionName) ? ("Saved Mission: " + MissionManager.CurrentMission.Name) : ("Saved Mission: " + MissionManager.CurrentMission.Name + ", Verison Name:" + versionName));
			savedInfo.alpha = 1f;
			savedInfo.gameObject.SetActive(value: true);
			await UniTask.Delay((int)(saveInfoDuration * 800f));
			if (cancel.IsCancellationRequested || !savedInfo.gameObject.activeSelf)
			{
				return;
			}
			float fadeDuration = saveInfoDuration * 0.2f;
			for (float t = 0f; t < fadeDuration; t += Time.deltaTime)
			{
				savedInfo.alpha = 1f - t / fadeDuration;
				await UniTask.Yield();
				if (cancel.IsCancellationRequested || !savedInfo.gameObject.activeSelf)
				{
					return;
				}
			}
			savedInfo.alpha = 0f;
			savedInfo.gameObject.SetActive(value: false);
		}

		private void ShowSavedFailed(string error)
		{
			saveErrorText.text = error;
			saveError.SetActive(value: true);
		}

		private void OnManualSaveValueChanged(string versionName)
		{
			manualSaveButton.interactable = !string.IsNullOrEmpty(versionName);
		}

		private void OnNameEditEnd(string newName)
		{
			if (MissionSaveLoad.ValidateName(ref newName, allowEmpty: true))
			{
				missionNameInput.SetTextWithoutNotify(newName);
			}
		}

		private void OnNameValueChanged(string newName)
		{
			MissionSaveLoad.ValidateName(ref newName, allowEmpty: true);
			bool hasValue = mission.LoadKey.HasValue;
			bool flag = mission.LoadKey.IsUserOrEditorGroup();
			bool flag2 = hasValue && FileUtils.NamesEqual(newName, mission.LoadKey.Value.Name);
			bool flag3 = string.IsNullOrWhiteSpace(newName);
			string validName;
			bool flag4 = !flag3 && MissionGroup.UserGroup.Exist(newName, out validName);
			saveNoNameBox.SetActive(flag3);
			bool active = !flag3 && hasValue && flag2 && flag;
			bool flag5 = !flag3 && (!hasValue || !flag2 || !flag);
			bool flag6 = !flag3 && hasValue && !flag2 && flag;
			saveMainButton.gameObject.SetActive(active);
			saveAsButton.gameObject.SetActive(flag5 && !flag4);
			renameButton.gameObject.SetActive(flag6 && !flag4);
			saveAsOverExistingButton.gameObject.SetActive(flag5 && flag4);
			renameOverExistingButton.gameObject.SetActive(flag6 && flag4);
			bool active2 = flag4 && (!flag2 || !flag);
			alreadyExistWarning.gameObject.SetActive(active2);
			alreadyExistWarning.text = "<size=120%><b>" + GoogleIconFont.FontString("\ue002") + " '" + newName + "' already exists.</b></size>\nSaving using this name will delete the other mission";
			resetMissionNameChange.gameObject.SetActive(hasValue && !flag2);
			FixLayout.ForceRebuildRecursive(base.transform.AsRectTransform());
		}

		private void ResetMissionName()
		{
			missionNameInput.text = mission.LoadKey?.Name ?? "";
		}

		private void OnEnable()
		{
			Setup(MissionManager.CurrentMission);
		}

		private void Setup(Mission mission)
		{
			this.mission = mission;
			savedInfo.gameObject.SetActive(value: false);
			saveError.SetActive(value: false);
			confirmAction = null;
			confirmPopup.SetActive(value: false);
			RefreshUI();
		}

		private void RefreshUI()
		{
			missionNameInput.SetTextWithoutNotify(mission.Name);
			OnNameValueChanged(mission.Name);
			missionDescriptionInput.text = mission.missionSettings.description;
			for (int num = missionListHolder.childCount - 1; num >= 0; num--)
			{
				UnityEngine.Object.Destroy(missionListHolder.GetChild(num).gameObject);
			}
			MissionFileInfo fileInfo = MissionSaveLoad.GetFileInfo(MissionSaveLoad.GetOrCreateUserSaveKey(mission));
			mainSaveItem.Setup(this, fileInfo.StripSubItems());
			if (!fileInfo.HasExtraSaves)
			{
				return;
			}
			Comparison<MissionFileInfo> comparer = MissionEditorLoadMenuV2.GetComparer(new SortOrder(2, isAscending: false));
			fileInfo.SubItems.Sort(comparer);
			foreach (MissionFileInfo subItem in fileInfo.SubItems)
			{
				UnityEngine.Object.Instantiate(subItemPrefab, missionListHolder).Setup(this, subItem);
			}
		}

		void IMissionEditorFileMenu.LoadMission(MissionKey key)
		{
		}

		void IMissionEditorFileMenu.SelectMission(MissionKey key)
		{
		}

		private void ManualSaveClicked()
		{
			TryAction("Failed to save version", delegate
			{
				MissionSaveLoad.SaveMissionVersion(mission, manualSaveNameInput.text);
				RefreshUI();
				FlashSavedInfo().Forget();
			});
		}

		private void OnSaveMainClicked()
		{
			TryAction("Failed to save", delegate
			{
				MissionSaveLoad.SaveMission(mission);
				FlashSavedInfo().Forget();
				RefreshUI();
			});
		}

		private void OnSaveAsClicked()
		{
			TryAction("Failed to Save As", SaveAsMission);
		}

		private void OnRenameClicked()
		{
			ConfirmWrapper("Are you sure you want to <color=yellow>Rename</color> Mission from '" + mission.Name + "' to '" + missionNameInput.text + "'", delegate
			{
				TryAction("Failed to Rename", RenameMission);
			});
		}

		private void OnSaveAsOverExistingClicked()
		{
			ConfirmWrapper("Are you sure you want to <color=red>DELETE</color> '" + missionNameInput.text + "' and save a copy of '" + mission.Name + "'", delegate
			{
				TryAction("Failed to Save As over Existing", SaveAsMission);
			});
		}

		private void OnRenameOverExistingClicked()
		{
			ConfirmWrapper("Are you sure you want to <color=red>DELETE</color> '" + missionNameInput.text + "' and <color=yellow>Rename</color> '" + mission.Name + "' to '" + missionNameInput.text + "'", delegate
			{
				TryAction("Failed to Rename over Existing", RenameMission);
			});
		}

		private void RenameMission()
		{
			string oldName = mission.LoadKey.Value.Name;
			string saveName = missionNameInput.text;
			MissionSaveLoad.ValidateName(ref saveName, allowEmpty: false);
			MissionGroup.UserGroup.DeleteMission(saveName);
			if (!MissionGroup.UserGroup.MoveMission(oldName, saveName, out var error))
			{
				ShowSavedFailed(error);
				return;
			}
			EditorMissionGroup.DeleteEditorFolder(saveName);
			if (!EditorMissionGroup.MoveEditorFolder(oldName, saveName, out var error2))
			{
				ShowSavedFailed(error2);
				return;
			}
			MissionSaveLoad.SaveMission(mission, ref saveName);
			FlashSavedInfo().Forget();
			RefreshUI();
			if (SceneSingleton<MissionEditor>.i != null)
			{
				SceneSingleton<MissionEditor>.i.SetMissionNameText(mission);
			}
		}

		private void SaveAsMission()
		{
			string saveName = missionNameInput.text;
			MissionSaveLoad.ValidateName(ref saveName, allowEmpty: false);
			MissionGroup.UserGroup.DeleteMission(saveName);
			if (MissionGroup.UserGroup.Exist(saveName, out var _))
			{
				Debug.LogError("SaveAs should not have Mission folder for newName");
				return;
			}
			EditorMissionGroup.DeleteEditorFolder(saveName);
			MissionSaveLoad.SaveMission(mission, ref saveName);
			mission.LoadKey = new MissionKey(saveName, MissionGroup.User);
			FlashSavedInfo().Forget();
			RefreshUI();
			if (SceneSingleton<MissionEditor>.i != null)
			{
				SceneSingleton<MissionEditor>.i.SetMissionNameText(mission);
			}
		}
	}
}
