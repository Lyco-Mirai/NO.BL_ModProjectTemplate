using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using NuclearOption.ModScripts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.Workshop
{
	public class ItemDetailsWorkshopPanel : WorkshopMenu.WorkshopPanel
	{
		internal class DownloadProgress : IProgress<float>
		{
			public float Progress;

			void IProgress<float>.Report(float value)
			{
				Progress = value;
			}
		}

		[Header("References")]
		[SerializeField]
		private SteamWorkshop steamWorkshop;

		[SerializeField]
		private WorkshopMenu menu;

		[Header("UI")]
		[SerializeField]
		private TextMeshProUGUI missionName;

		[SerializeField]
		private TextMeshProUGUI owner;

		[SerializeField]
		private TextMeshProUGUI description;

		[SerializeField]
		private Image previewImage;

		[SerializeField]
		private TextMeshProUGUI subscribeText;

		[SerializeField]
		private Button subscribe;

		[SerializeField]
		private Button openSteamPage;

		[SerializeField]
		private Button showLocalFiles;

		[SerializeField]
		private GameObject progressHolder;

		[SerializeField]
		private TextMeshProUGUI progressText;

		[SerializeField]
		private Button updateButton;

		private SteamWorkshopItem item;

		private DownloadProgress downloadProgress;

		private CancellationTokenSource cancellationTokenSource;

		private void Awake()
		{
			subscribe.onClick.AddListener(SubscribedClicked);
			openSteamPage.onClick.AddListener(OpenSteamPage);
			showLocalFiles.onClick.AddListener(ShowLocalFolder);
			updateButton.onClick.AddListener(SubmitUpdate);
			if (steamWorkshop == null)
			{
				steamWorkshop = GetComponentInParent<SteamWorkshop>();
			}
		}

		public void ShowItem(SteamWorkshopItem item)
		{
			if (this.item != null)
			{
				this.item.OwnerNameChanged -= OnOwnerNameChanged;
			}
			this.item = item;
			missionName.text = item.Name;
			item.OwnerNameChanged += OnOwnerNameChanged;
			OnOwnerNameChanged();
			description.text = item.Description;
			subscribeText.text = (item.Subscribed ? "Unsubscribe" : "Subscribe");
			progressHolder.SetActive(value: false);
			showLocalFiles.gameObject.SetActive(item.Subscribed);
			updateButton.gameObject.SetActive(item.IsOwner());
			updateButton.interactable = true;
			item.SetPreviewImageAsync(previewImage, ref cancellationTokenSource).Forget();
		}

		private void OnOwnerNameChanged()
		{
			owner.text = item.OwnerName;
		}

		private void SubscribedClicked()
		{
			if (item.Subscribed)
			{
				Unsubscribe().Forget();
			}
			else
			{
				DownloadMission();
			}
		}

		private void DownloadMission()
		{
			UniTask.Void(async delegate
			{
				downloadProgress = new DownloadProgress();
				SetProgressText();
				progressHolder.SetActive(value: true);
				try
				{
					await steamWorkshop.DownloadItem(item, downloadProgress);
					RefreshSubscribed();
				}
				finally
				{
					progressHolder.SetActive(value: false);
					downloadProgress = null;
				}
			});
		}

		private async UniTaskVoid Unsubscribe()
		{
			try
			{
				await steamWorkshop.Unsubscribe(item);
				RefreshSubscribed();
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
		}

		private void RefreshSubscribed()
		{
			subscribeText.text = (item.Subscribed ? "Unsubscribe" : "Subscribe");
			showLocalFiles.gameObject.SetActive(item.Subscribed);
		}

		private void OpenSteamPage()
		{
			item.OpenSteamPage();
		}

		private void ShowLocalFolder()
		{
			item.OpenLocalContent();
		}

		private void SubmitUpdate()
		{
			if (ModTypes.FromTag(item.Tag).TryGetLocalItem(item, out var workshopUploadItem))
			{
				menu.OpenUploadPanel(workshopUploadItem, item);
			}
			else
			{
				updateButton.interactable = false;
			}
		}

		private void Update()
		{
			if (downloadProgress != null)
			{
				SetProgressText();
			}
		}

		private void SetProgressText()
		{
			progressText.text = MissionManagerDebugGui.CreateProgressBar(downloadProgress.Progress, 40);
		}
	}
}
