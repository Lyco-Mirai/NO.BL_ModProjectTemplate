using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NuclearOption.SavedMission;
using TMPro;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.UI;

public class MissionsPicker : SceneSingleton<MissionsPicker>
{
	public struct Filter
	{
		public List<MissionGroup> DisallowedGroups;

		public List<MissionTag> RequiredTags;
	}

	private static readonly ProfilerMarker setPickerFilterMarker = new ProfilerMarker("MissionsPicker.SetPickerFilter");

	private static readonly ProfilerMarker selectMissionMarker = new ProfilerMarker("MissionsPicker.SelectMission");

	private static readonly ProfilerMarker startMarker = new ProfilerMarker("MissionsPicker.Start");

	private static readonly ProfilerMarker onEnableMarker = new ProfilerMarker("MissionsPicker.OnEnable");

	[Header("Mission Preview")]
	[SerializeField]
	private MissionSelectPanel missionSelect;

	[SerializeField]
	private MissionSelectGroupButtons missionSelectGroups;

	[SerializeField]
	private Text missionTitle;

	[SerializeField]
	private TextMeshProUGUI missionDescription;

	[SerializeField]
	private Image previewImage;

	[SerializeField]
	private Sprite defaultMissionImage;

	[Header("Buttons")]
	[SerializeField]
	private Button openUserFolder;

	[SerializeField]
	private TextMeshProUGUI confirmText;

	[SerializeField]
	private Button confirmButton;

	[Header("Failed to load")]
	[SerializeField]
	private GameObject failedOverlay;

	[SerializeField]
	private TextMeshProUGUI failedText;

	[SerializeField]
	private Button closeFailed;

	private Color normalColor;

	private Mission mission;

	private CancellationTokenSource cancelGetPreview;

	public Mission Mission => mission;

	public event Action<Mission> OnMissionSelect;

	public event Action<Mission> OnMissionConfirmed;

	public void ShowPicker()
	{
		base.gameObject.SetActive(value: true);
	}

	public void HidePicker()
	{
		base.gameObject.SetActive(value: false);
	}

	public void SetPickerFilter(Filter filter)
	{
		using (setPickerFilterMarker.Auto())
		{
			missionSelect.SetPickerFilter(filter);
			missionSelectGroups.SetPickerFilter(filter);
		}
	}

	protected override void Awake()
	{
		base.Awake();
		normalColor = missionDescription.color;
	}

	private void OnEnable()
	{
		using (onEnableMarker.Auto())
		{
			SelectMission(missionSelect.SelectedMission);
		}
	}

	private void Start()
	{
		using (startMarker.Auto())
		{
			PlayerSettings.LoadPrefs();
			openUserFolder.onClick.AddListener(MissionGroup.UserGroup.OpenFolder);
			confirmButton.onClick.AddListener(ConfirmPressed);
			closeFailed.onClick.AddListener(CloseFailed);
			missionSelect.OnMissionSelecteed += SelectMission;
			SelectMission(missionSelect.SelectedMission);
		}
	}

	public void SelectMission(MissionKey item)
	{
		using (selectMissionMarker.Auto())
		{
			if (!item.IsValid())
			{
				missionTitle.text = "";
				missionDescription.text = "";
				confirmButton.interactable = false;
				previewImage.sprite = null;
				previewImage.color = new Color(0.2f, 0.2f, 0.2f);
				return;
			}
			missionTitle.text = item.Name;
			GetPreviewAsync(item).Forget();
			if (item.TryLoad(out mission, out var error))
			{
				this.OnMissionSelect?.Invoke(Mission);
				missionDescription.text = Mission.missionSettings.description;
				missionDescription.color = normalColor;
				confirmButton.interactable = true;
			}
			else
			{
				this.OnMissionSelect?.Invoke(null);
				missionDescription.text = error;
				missionDescription.color = Color.red;
				confirmButton.interactable = false;
				failedOverlay.SetActive(value: true);
				failedText.text = error;
			}
		}
	}

	public void SetMissionWithoutNotify(Mission mission)
	{
		this.mission = mission;
	}

	private async UniTaskVoid GetPreviewAsync(MissionKey item)
	{
		cancelGetPreview?.Cancel();
		cancelGetPreview = new CancellationTokenSource();
		CancellationToken token = cancelGetPreview.Token;
		previewImage.color = new Color(0.2f, 0.2f, 0.2f);
		Sprite sprite = await item.GetPreview(token);
		if (!token.IsCancellationRequested)
		{
			previewImage.color = Color.white;
			previewImage.sprite = sprite ?? defaultMissionImage;
		}
	}

	private void CloseFailed()
	{
		failedOverlay.SetActive(value: false);
	}

	private void ConfirmPressed()
	{
		this.OnMissionConfirmed(Mission);
	}
}
