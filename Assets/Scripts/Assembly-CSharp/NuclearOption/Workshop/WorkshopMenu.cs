using Cysharp.Threading.Tasks;
using NuclearOption.ModScripts;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.Workshop
{
	public class WorkshopMenu : MonoBehaviour
	{
		public class WorkshopPanel : MonoBehaviour
		{
		}

		[SerializeField]
		private Button updateAll;

		[SerializeField]
		private Button uploadItem;

		[SerializeField]
		private Button openWorkshopPage;

		[SerializeField]
		private Button backToListButton;

		[SerializeField]
		private ListWorkshopPanel listPanel;

		[SerializeField]
		private ItemDetailsWorkshopPanel detailsPanel;

		[SerializeField]
		private UploadWorkshopPanel uploadPanel;

		private WorkshopPanel activePanel;

		private void SetActivePanel(WorkshopPanel behaviour)
		{
			if (!(activePanel == behaviour))
			{
				if (activePanel != null)
				{
					activePanel.gameObject.SetActive(value: false);
				}
				activePanel = behaviour;
				behaviour.gameObject.SetActive(value: true);
				uploadItem.interactable = activePanel != uploadPanel;
				backToListButton.interactable = activePanel != listPanel;
			}
		}

		public void OpenListPanel()
		{
			SetActivePanel(listPanel);
		}

		public void OpenUploadPanel()
		{
			SetActivePanel(uploadPanel);
		}

		public void OpenUploadPanel(WorkshopUploadItem item, SteamWorkshopItem details)
		{
			SetActivePanel(uploadPanel);
			uploadPanel.OpenWithItem(item, details);
		}

		public void OpenItemDetailsPanel(SteamWorkshopItem item)
		{
			SetActivePanel(detailsPanel);
			detailsPanel.ShowItem(item);
		}

		private void Start()
		{
			updateAll.onClick.AddListener(UpdateAllClicked);
			uploadItem.onClick.AddListener(OpenUploadPanel);
			backToListButton.onClick.AddListener(OpenListPanel);
			openWorkshopPage.onClick.AddListener(SteamWorkshop.OpenWorkshopPage);
			detailsPanel.gameObject.SetActive(value: false);
			uploadPanel.gameObject.SetActive(value: false);
			SetActivePanel(listPanel);
			updateAll.interactable = SteamWorkshop.AnyNeedUpdates();
		}

		private void UpdateAllClicked()
		{
			UniTask.Void(async delegate
			{
				updateAll.interactable = false;
				await SteamWorkshop.UpdateAllSubscribedItems();
				await UniTask.Delay(500);
				updateAll.interactable = SteamWorkshop.AnyNeedUpdates();
			});
		}

		public void CloseMenu()
		{
			Object.Destroy(base.gameObject);
		}
	}
}
