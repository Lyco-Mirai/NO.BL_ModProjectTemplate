using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using NuclearOption.MissionEditorScripts;
using NuclearOption.ModScripts;
using SFB;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.Workshop
{
	public class UploadWorkshopPanel : WorkshopMenu.WorkshopPanel
	{
		public abstract class Page
		{
			[SerializeField]
			private GameObject holder;

			protected UploadWorkshopPanel panel;

			public void SetActive(bool active)
			{
				if (active && !holder.activeSelf)
				{
					holder.SetActive(value: true);
				}
				else if (!active && holder.activeSelf)
				{
					holder.SetActive(value: false);
				}
			}

			public void Setup(UploadWorkshopPanel panel)
			{
				this.panel = panel;
				SetActive(active: false);
			}

			public abstract void Update();

			public abstract void GoNext();
		}

		[Serializable]
		public class Page1 : Page
		{
			[SerializeField]
			private TMP_Dropdown modTypeDropdown;

			public void Enable()
			{
				panel.nextButton.interactable = false;
				PopulateTypeDropdown();
				panel.nextButtonText.text = "Next";
				panel.openWorkshop.gameObject.SetActive(value: false);
				panel.workshopId = PublishedFileId_t.Invalid;
			}

			private void PopulateTypeDropdown()
			{
				modTypeDropdown.ClearOptions();
				modTypeDropdown.options.Add(new TMP_Dropdown.OptionData
				{
					text = ""
				});
				ModType[] modTypesArray = ModTypes.ModTypesArray;
				foreach (ModType modType in modTypesArray)
				{
					modTypeDropdown.options.Add(new TMP_Dropdown.OptionData(modType.Name));
				}
				modTypeDropdown.SetValueWithoutNotify(0);
			}

			public override void Update()
			{
				panel.nextButton.interactable = modTypeDropdown.value != 0;
			}

			public override void GoNext()
			{
				int num = modTypeDropdown.value - 1;
				ModType type = ModTypes.ModTypesArray[num];
				panel.OpenPage2(type);
			}
		}

		[Serializable]
		public class Page2 : Page
		{
			[SerializeField]
			private TextMeshProUGUI typeLabel;

			[SerializeField]
			private TMP_Dropdown itemSelectDropdown;

			private ModType type;

			public void Enable(ModType type)
			{
				this.type = type;
				panel.nextButton.interactable = true;
				typeLabel.text = "Select " + type.Name;
				type.PopulateItemDropdown(itemSelectDropdown);
				panel.nextButtonText.text = "Next";
				panel.openWorkshop.gameObject.SetActive(value: false);
			}

			public override void Update()
			{
				panel.nextButton.interactable = itemSelectDropdown.value != 0;
			}

			public override void GoNext()
			{
				string text = itemSelectDropdown.options[itemSelectDropdown.value].text;
				panel.OpenPage3(type, text);
			}
		}

		[Serializable]
		public class Page3 : Page
		{
			[SerializeField]
			private GameObject loadingOverlay;

			[Header("Main panel")]
			[SerializeField]
			private TMP_InputField missionNameInput;

			[SerializeField]
			private Toggle isPublicToggle;

			[SerializeField]
			private TMP_InputField descriptionInput;

			[SerializeField]
			private Button openFileSelectorButton;

			[SerializeField]
			private Image previewImage;

			[SerializeField]
			private TextMeshProUGUI needPreviewWarning;

			[SerializeField]
			private TMP_InputField changeLogInput;

			[Header("Details not found")]
			[SerializeField]
			private GameObject notFoundPanel;

			[SerializeField]
			private Button notFoundRetry;

			[SerializeField]
			private Button notFoundCreateNew;

			private bool needToDisposeImage;

			private bool eventAdded;

			private string imagePath;

			private CancellationTokenSource loadImageCancel;

			private WorkshopUploadItem item;

			public void Enable(WorkshopUploadItem item, SteamWorkshopItem details)
			{
				this.item = item;
				SetupAsync(details).Forget();
			}

			public void Enable(ModType type, string itemName)
			{
				if (!eventAdded)
				{
					eventAdded = true;
					openFileSelectorButton.onClick.AddListener(OpenFileSelector);
					notFoundRetry.onClick.AddListener(NotFoundRetry);
					notFoundCreateNew.onClick.AddListener(NotFoundCreateNew);
				}
				loadingOverlay.SetActive(value: false);
				notFoundPanel.SetActive(value: false);
				item = type.GetItem(itemName);
				if (item == null)
				{
					panel.GoBack();
					return;
				}
				UniTask.Void(async delegate
				{
					if (item.WorkshopId != PublishedFileId_t.Invalid)
					{
						await TryLoadDetails();
					}
					else
					{
						await SetupAsync(null);
					}
				});
			}

			private void NotFoundRetry()
			{
				notFoundPanel.SetActive(value: false);
				TryLoadDetails().Forget();
			}

			private void NotFoundCreateNew()
			{
				notFoundPanel.SetActive(value: false);
				item.ClearSteamId();
				SetupAsync(null).Forget();
			}

			private async UniTask TryLoadDetails()
			{
				loadingOverlay.SetActive(value: true);
				var (flag, details) = await SteamWorkshop.GetDetails(item.WorkshopId);
				if (flag)
				{
					await SetupAsync(details);
				}
				else
				{
					notFoundPanel.SetActive(value: true);
				}
			}

			private async UniTask SetupAsync(SteamWorkshopItem details)
			{
				loadingOverlay.SetActive(value: false);
				notFoundPanel.SetActive(value: false);
				FixLayout.ForceRebuildAtEndOfFrame((RectTransform)panel.transform);
				panel.openWorkshop.gameObject.SetActive(item.WorkshopId != PublishedFileId_t.Invalid);
				panel.nextButton.interactable = true;
				panel.nextButtonText.text = ((item.WorkshopId != PublishedFileId_t.Invalid) ? "Update Existing" : "Create New");
				missionNameInput.text = item.DisplayName;
				missionNameInput.interactable = false;
				if (details != null)
				{
					isPublicToggle.isOn = details.Public;
					descriptionInput.text = details.Description;
				}
				else
				{
					isPublicToggle.isOn = false;
					descriptionInput.text = item.Description;
				}
				imagePath = null;
				changeLogInput.text = string.Empty;
				changeLogInput.interactable = item.WorkshopId != PublishedFileId_t.Invalid;
				needPreviewWarning.text = ((item.WorkshopId != PublishedFileId_t.Invalid) ? "Loading..." : "Need Preview");
				if (details != null)
				{
					UniTask uniTask = details.SetPreviewImageAsync(previewImage, ref loadImageCancel);
					CancellationToken cancel = loadImageCancel.Token;
					await uniTask;
					if (!cancel.IsCancellationRequested)
					{
						FixLayout.ForceRebuildRecursive((RectTransform)panel.transform);
						SetPreviewColor();
						needPreviewWarning.text = ((previewImage.sprite != null) ? "" : "Failed to load");
					}
				}
				else
				{
					SetPreviewTexture(null);
				}
			}

			public void OpenFileSelector()
			{
				string[] array = StandaloneFileBrowser.OpenFilePanel("Select Image", "", extensions, multiselect: false);
				if (array.Length != 0)
				{
					bool flag = ValidSize(array[0]);
					loadImageCancel?.Cancel();
					loadImageCancel = null;
					Texture2D texture;
					bool flag2 = TryLoadImage(array[0], out texture);
					if (flag2 && flag)
					{
						imagePath = array[0];
					}
					else
					{
						imagePath = null;
					}
					if (!flag2)
					{
						needPreviewWarning.text = "Load failed";
					}
					else if (!flag)
					{
						needPreviewWarning.text = "Preview must be under 1MB";
					}
					else
					{
						needPreviewWarning.text = "";
					}
					SetPreviewTexture(flag2 ? texture : null);
				}
			}

			private bool ValidSize(string filePath)
			{
				return (double)new FileInfo(filePath).Length / 1048576.0 <= 1.0;
			}

			private void SetPreviewTexture(Texture2D newTexture)
			{
				if (needToDisposeImage)
				{
					Sprite sprite = previewImage.sprite;
					previewImage.sprite = null;
					UnityEngine.Object.Destroy(sprite.texture);
					UnityEngine.Object.Destroy(sprite);
					needToDisposeImage = false;
				}
				if (newTexture != null)
				{
					previewImage.sprite = Sprite.Create(newTexture, new Rect(0f, 0f, newTexture.width, newTexture.height), new Vector2(0.5f, 0.5f));
					needToDisposeImage = true;
				}
				else
				{
					previewImage.sprite = null;
				}
				SetPreviewColor();
			}

			private void SetPreviewColor()
			{
				previewImage.color = ((previewImage.sprite != null) ? Color.white : new Color(0.2f, 0.2f, 0.2f));
			}

			private static bool TryLoadImage(string path, out Texture2D texture)
			{
				texture = null;
				if (string.IsNullOrEmpty(path))
				{
					return false;
				}
				if (!File.Exists(path))
				{
					return false;
				}
				texture = new Texture2D(0, 0);
				byte[] data = File.ReadAllBytes(path);
				return texture.LoadImage(data);
			}

			public override void Update()
			{
				panel.nextButton.interactable = CanUpload();
			}

			private bool CanUpload()
			{
				if (string.IsNullOrEmpty(missionNameInput.text))
				{
					return false;
				}
				if (string.IsNullOrEmpty(descriptionInput.text))
				{
					return false;
				}
				if (item.WorkshopId == PublishedFileId_t.Invalid && string.IsNullOrEmpty(imagePath))
				{
					return false;
				}
				return true;
			}

			public override void GoNext()
			{
				item.Description = descriptionInput.text;
				item.ImagePath = imagePath;
				item.Public = isPublicToggle.isOn;
				panel.OpenPage4(item, changeLogInput.text);
			}
		}

		[Serializable]
		public class Page4 : Page
		{
			internal class UploadProgress : IProgress<(float overall, float part)>
			{
				private readonly TextMeshProUGUI overall;

				private readonly TextMeshProUGUI part;

				public UploadProgress(TextMeshProUGUI overall, TextMeshProUGUI part)
				{
					this.overall = overall;
					this.part = part;
				}

				public void Report((float overall, float part) value)
				{
					overall.text = MissionManagerDebugGui.CreateProgressBar(value.overall, 40);
					part.text = MissionManagerDebugGui.CreateProgressBar(value.part, 20);
				}
			}

			[SerializeField]
			private TextMeshProUGUI uploadingLabel;

			[SerializeField]
			private TextMeshProUGUI uploadingProgressOverall;

			[SerializeField]
			private TextMeshProUGUI uploadingProgressPart;

			public void Enable(WorkshopUploadItem item, string changeLog)
			{
				panel.backButton.interactable = false;
				panel.nextButton.interactable = false;
				panel.openWorkshop.gameObject.SetActive(item.WorkshopId != PublishedFileId_t.Invalid);
				UploadAsync(item, changeLog).Forget();
				uploadingLabel.text = "Uploading";
			}

			public override void Update()
			{
			}

			private async UniTask UploadAsync(WorkshopUploadItem item, string changeLog)
			{
				UploadProgress progress = new UploadProgress(uploadingProgressOverall, uploadingProgressPart);
				SteamWorkshopItem item2 = item.ToSteamItem();
				UniTask<(bool success, EResult)> task = panel.steamWorkshop.CreateOrUpdateItem(item2, changeLog, item, progress);
				while (task.Status == UniTaskStatus.Pending)
				{
					await UniTask.Yield();
				}
				var (flag, eResult) = await task;
				uploadingLabel.text = (flag ? "Success" : ("Failed: " + eResult.Nicify()));
				panel.openWorkshop.gameObject.SetActive(item.WorkshopId != PublishedFileId_t.Invalid);
				FixLayout.ForceRebuildRecursive((RectTransform)panel.transform);
			}

			public override void GoNext()
			{
				throw new NotSupportedException();
			}
		}

		private static readonly ExtensionFilter[] extensions = new ExtensionFilter[2]
		{
			new ExtensionFilter("Image Files", "png", "jpg", "jpeg"),
			new ExtensionFilter("All Files", "*")
		};

		[Header("References")]
		[SerializeField]
		private SteamWorkshop steamWorkshop;

		[Header("Controls")]
		[SerializeField]
		private Button backButton;

		[SerializeField]
		private Button nextButton;

		[SerializeField]
		private TextMeshProUGUI nextButtonText;

		[SerializeField]
		private Button openWorkshop;

		[Header("Pages")]
		[SerializeField]
		private int activePageIndex;

		private Page activePage;

		[SerializeField]
		private Page1 page1;

		[SerializeField]
		private Page2 page2;

		[SerializeField]
		private Page3 page3;

		[SerializeField]
		private Page4 page4;

		private PublishedFileId_t workshopId;

		private void OpenPage1()
		{
			EnablePage(1);
			page1.Enable();
			Update();
		}

		private void OpenPage2(ModType type)
		{
			EnablePage(2);
			page2.Enable(type);
			Update();
		}

		private void OpenPage3(ModType type, string itemName)
		{
			EnablePage(3);
			page3.Enable(type, itemName);
			Update();
		}

		private void OpenPage3(WorkshopUploadItem item, SteamWorkshopItem details)
		{
			EnablePage(3);
			page3.Enable(item, details);
			Update();
		}

		private void OpenPage4(WorkshopUploadItem item, string changeLog)
		{
			EnablePage(4);
			page4.Enable(item, changeLog);
			Update();
		}

		public void OpenWithItem(WorkshopUploadItem item, SteamWorkshopItem details)
		{
			OpenPage3(item, details);
		}

		private void Awake()
		{
			page1.Setup(this);
			page2.Setup(this);
			page3.Setup(this);
			page4.Setup(this);
			nextButton.onClick.AddListener(GoNext);
			backButton.onClick.AddListener(GoBack);
			openWorkshop.onClick.AddListener(OpenItemWorkshop);
		}

		private void OnEnable()
		{
			OpenPage1();
		}

		private void OnValidate()
		{
			EnablePage(activePageIndex);
		}

		private void EnablePage(int index)
		{
			page1.SetActive(index == 1);
			page2.SetActive(index == 2);
			page3.SetActive(index == 3);
			page4.SetActive(index == 4);
			activePageIndex = index;
			switch (index)
			{
			case 1:
				activePage = page1;
				break;
			case 2:
				activePage = page2;
				break;
			case 3:
				activePage = page3;
				break;
			case 4:
				activePage = page4;
				break;
			}
			FixLayout.ForceRebuildAtEndOfFrame((RectTransform)base.transform);
		}

		private void Update()
		{
			activePage.Update();
		}

		private void GoNext()
		{
			activePage.GoNext();
		}

		private void GoBack()
		{
			EnablePage(activePageIndex - 1);
		}

		private void OpenItemWorkshop()
		{
			if (workshopId != PublishedFileId_t.Invalid)
			{
				SteamWorkshopItem.OpenSteamPage(workshopId);
			}
		}
	}
}
