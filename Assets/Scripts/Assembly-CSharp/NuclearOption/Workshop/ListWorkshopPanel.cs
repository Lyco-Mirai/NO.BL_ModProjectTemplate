using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NuclearOption.Chat;
using NuclearOption.MissionEditorScripts;
using NuclearOption.ModScripts;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.Workshop
{
	public class ListWorkshopPanel : WorkshopMenu.WorkshopPanel
	{
		[Header("References")]
		[SerializeField]
		private SteamWorkshop steamWorkshop;

		[SerializeField]
		private ListWorkshopPanelControls controls;

		[SerializeField]
		private Button nextPageButton;

		[SerializeField]
		private float searchDelay = 1f;

		[Header("UI")]
		[SerializeField]
		private WorkshopList workshopList;

		[SerializeField]
		private TabController tabController;

		[Space]
		[SerializeField]
		private LoadingImage loading;

		[Header("Steam tags")]
		[SerializeField]
		private bool automaticTags = true;

		[SerializeField]
		private List<string> modTags;

		private string currentTag;

		private readonly RateLimiter rateLimiter = new RateLimiter(5, 30f);

		private bool queryInProgress;

		private readonly List<SteamWorkshopItem> drawItems = new List<SteamWorkshopItem>();

		private readonly List<SteamWorkshopItem> allItems = new List<SteamWorkshopItem>();

		private int currentPage;

		private string lastSearchedText = string.Empty;

		private float searchTimer;

		private bool searchDelayActive;

		private void OnValidate()
		{
			if (automaticTags)
			{
				modTags.Clear();
				modTags.Add(ModTypes.Missions.Tag);
				modTags.Add(ModTypes.AircraftLivery.Tag);
			}
		}

		private void Awake()
		{
			controls.RefreshButton.onClick.AddListener(ForceRefresh);
			controls.ClearFilterButton.onClick.AddListener(ClearFilter);
			controls.FilterNameInput.onSubmit.AddListener(delegate
			{
				TrySubmitSearch(force: true);
			});
			controls.FilterNameInput.onValueChanged.AddListener(OnFilterValueChanged);
			controls.OrderByToggleGroup.OnChangeValue += OrderByToggleGroup_OnChangeValue;
			nextPageButton.onClick.AddListener(GetNextPage);
			nextPageButton.gameObject.SetActive(value: false);
			loading.SetActive(active: false);
			tabController.TabChanged += TabController_TabChanged;
			tabController.Setup(modTags, 0);
		}

		private void TabController_TabChanged(string tag, int index)
		{
			currentTag = tag;
			ClearDelayTimer();
			lastSearchedText = (controls.FilterNameInput.text ?? string.Empty).Trim();
			GetFirstPage();
		}

		private void OrderByToggleGroup_OnChangeValue(int index)
		{
			ClearDelayTimer();
			lastSearchedText = (controls.FilterNameInput.text ?? string.Empty).Trim();
			GetFirstPage();
		}

		private void OnEnable()
		{
			controls.Holder.SetActive(value: true);
			if (!queryInProgress)
			{
				ClearDelayTimer();
				lastSearchedText = (controls.FilterNameInput.text ?? string.Empty).Trim();
				GetFirstPage();
			}
		}

		private void OnDisable()
		{
			controls.Holder.SetActive(value: false);
			loading.gameObject.SetActive(value: false);
			ClearDelayTimer();
		}

		private void SetRequestingUI(bool value)
		{
			queryInProgress = value;
			controls.RefreshButton.interactable = !value;
			nextPageButton.interactable = !value;
			loading.SetActive(value);
		}

		private void GetFirstPage()
		{
			if (string.IsNullOrEmpty(currentTag))
			{
				throw new InvalidOperationException("No tag set when refreshing items");
			}
			if (rateLimiter.ShouldLimit(Time.time))
			{
				Debug.LogWarning("RefreshItems rate limit");
			}
			else
			{
				GetPage(1).Forget();
			}
		}

		private void GetNextPage()
		{
			GetPage(currentPage + 1).Forget();
		}

		private async UniTask GetPage(int page)
		{
			if (queryInProgress)
			{
				Debug.LogError("RefreshItems in progress");
				return;
			}
			CancellationToken cancel = base.destroyCancellationToken;
			try
			{
				SetRequestingUI(value: true);
				OrderBy index = (OrderBy)controls.OrderByToggleGroup.GetIndex();
				currentPage = page;
				string searchText = (controls.FilterNameInput.text ?? string.Empty).Trim();
				UniTask<bool> refreshTask = steamWorkshop.RefreshItems(index, currentTag, allItems, (uint)page, searchText);
				while (refreshTask.Status == UniTaskStatus.Pending)
				{
					await UniTask.Yield();
					if (!base.gameObject.activeInHierarchy)
					{
						break;
					}
					if (cancel.IsCancellationRequested)
					{
						return;
					}
				}
				bool flag = await refreshTask;
				if (!cancel.IsCancellationRequested)
				{
					RedrawList();
					if (nextPageButton.gameObject.activeSelf != flag)
					{
						nextPageButton.gameObject.SetActive(flag);
					}
					FixLayout.ForceRebuildRecursive((RectTransform)base.transform);
				}
			}
			finally
			{
				SetRequestingUI(value: false);
			}
		}

		private void ClearFilter()
		{
			controls.FilterNameInput.text = string.Empty;
			ClearDelayTimer();
			lastSearchedText = string.Empty;
			GetFirstPage();
		}

		private void RedrawList()
		{
			drawItems.Clear();
			drawItems.AddRange(allItems);
			workshopList.UpdateList(drawItems);
		}

		private void OnFilterValueChanged(string value)
		{
			searchTimer = searchDelay;
			if (!searchDelayActive)
			{
				searchDelayActive = true;
				StartDelayedSearch(this.GetCancellationTokenOnDestroy()).Forget();
			}
		}

		private async UniTaskVoid StartDelayedSearch(CancellationToken cancellationToken)
		{
			while (searchDelayActive && (searchTimer > 0f || queryInProgress))
			{
				await UniTask.Yield();
				if (cancellationToken.IsCancellationRequested)
				{
					return;
				}
				if (searchTimer > 0f)
				{
					searchTimer -= Time.deltaTime;
				}
			}
			if (searchDelayActive)
			{
				searchDelayActive = false;
				TrySubmitSearch();
			}
		}

		private void TrySubmitSearch(bool force = false)
		{
			ClearDelayTimer();
			string text = (controls.FilterNameInput.text ?? string.Empty).Trim();
			if (force || !(text == lastSearchedText))
			{
				lastSearchedText = text;
				GetFirstPage();
			}
		}

		private void ForceRefresh()
		{
			ClearDelayTimer();
			GetFirstPage();
		}

		private void ClearDelayTimer()
		{
			searchDelayActive = false;
			searchTimer = 0f;
		}
	}
}
