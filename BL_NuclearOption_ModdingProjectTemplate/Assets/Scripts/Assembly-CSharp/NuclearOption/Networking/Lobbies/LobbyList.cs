using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NuclearOption.MissionEditorScripts.Buttons;
using NuclearOption.SceneLoading;
using NuclearOption.UI;
using NuclearOption.UI.HeaderSorting;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.Networking.Lobbies
{
	public class LobbyList : MonoBehaviour
	{
		public enum SortMode
		{
			None = 0,
			FuzzySearch = 1,
			PlayerCount = 2,
			Ping = 3,
			Name = 4,
			Mission = 5,
			Map = 6,
			Duration = 7
		}

		[Header("List")]
		[SerializeField]
		private LobbyListFadeOverlay refreshOverlay;

		[SerializeField]
		private RectTransform lobbyListContent;

		[SerializeField]
		private LobbyListItem entryPrefab;

		[SerializeField]
		private List<LobbyListItem> startingEntries;

		[Header("References")]
		[SerializeField]
		private LobbyDetailsModal lobbyPopup;

		[SerializeField]
		private Button createLobbyButton;

		[SerializeField]
		private CreateLobbyModal createLobbyModal;

		[SerializeField]
		private Button mainMenuButton;

		[Header("Filters")]
		[SerializeField]
		private SliderToggle hideFullServersToggle;

		[SerializeField]
		private SliderToggle hideEmptyServersToggle;

		[SerializeField]
		private SliderToggle hidePasswordProtectedToggle;

		[SerializeField]
		private BetterToggleGroup missionTypeToggleGroup;

		[SerializeField]
		private BetterToggleGroup serverTypeToggleGroup;

		[SerializeField]
		private BetterToggleGroup distanceFilterToggleGroup;

		[SerializeField]
		private TMP_InputField searchInputField;

		[SerializeField]
		private bool DEBUG_ignoreVersionFilter;

		[Header("Sorting Buttons")]
		[SerializeField]
		private Button refreshButton;

		[SerializeField]
		private Button clearSearchButton;

		[SerializeField]
		private Color activeHeaderColor = Color.white;

		[SerializeField]
		private Color inactiveHeaderColor = Color.grey;

		[SerializeField]
		private ListSortButton sortByNameButton;

		[SerializeField]
		private ListSortButton sortByMissionButton;

		[SerializeField]
		private ListSortButton sortByMapButton;

		[SerializeField]
		private ListSortButton sortByPlayerCountButton;

		[SerializeField]
		private ListSortButton sortByPingButton;

		[SerializeField]
		private ListSortButton sortByDurationButton;

		[SerializeField]
		private int fuzzySearchMaxDistance = 8;

		[Header("Too many players")]
		[SerializeField]
		public int TooManyPlayerLimit = 16;

		[SerializeField]
		public HoverText TooManyPlayerHover;

		[SerializeField]
		public HoverText IconTooltipHover;

		private FuzzySearch fuzzySearch;

		private readonly Stack<LobbyListItem> pool = new Stack<LobbyListItem>();

		private readonly Dictionary<LobbyInstance, LobbyListItem> allLobbies = new Dictionary<LobbyInstance, LobbyListItem>();

		private List<LobbyListItem> sortList = new List<LobbyListItem>();

		private List<int> sortListScore = new List<int>();

		private readonly HashSet<LobbyListItem> _processedLobbiesSet = new HashSet<LobbyListItem>();

		private SortHeaderController sortController;

		private SortOrder _activeSortOrder = new SortOrder(2, isAscending: false);

		private SortOrder _previousHeaderSortOrder = new SortOrder(2, isAscending: false);

		private LobbySearchFilter activeFilter;

		private bool refreshPending;

		private void Awake()
		{
			foreach (LobbyListItem startingEntry in startingEntries)
			{
				startingEntry.Hide();
				pool.Push(startingEntry);
			}
			createLobbyButton.onClick.AddListener(CreateLobbyClicked);
			mainMenuButton.onClick.AddListener(UniTask.UnityAction(MainMenuClicked));
			SteamLobby.instance.CheckRelayLocationTask();
			SteamLobby.instance.OnLobbyListCleared += RefreshLobbyListCleared;
			SteamLobby.instance.OnLobbyDataUpdated += OnLobbyDataUpdated;
			SteamLobby.instance.OnLobbyPingUpdated += OnLobbyPingUpdated;
			SteamLobby.instance.OnLobbyRefreshFinished += HideRefreshOverlay;
			SteamLobby.instance.OnLocationUpdated += Instance_OnLocationUpdated;
			hideFullServersToggle.onValueChanged.AddListener(delegate
			{
				GetListOfLobbies();
			});
			hideEmptyServersToggle.onValueChanged.AddListener(delegate
			{
				GetListOfLobbies();
			});
			hidePasswordProtectedToggle.onValueChanged.AddListener(delegate
			{
				GetListOfLobbies();
			});
			missionTypeToggleGroup.OnChangeValue += delegate
			{
				GetListOfLobbies();
			};
			serverTypeToggleGroup.OnChangeValue += delegate
			{
				GetListOfLobbies();
			};
			distanceFilterToggleGroup.OnChangeValue += delegate
			{
				GetListOfLobbies();
			};
			searchInputField.onValueChanged.AddListener(OnSearchTermChanged);
			clearSearchButton.onClick.AddListener(OnClearSearchClicked);
			refreshButton.onClick.AddListener(GetListOfLobbies);
			sortController = new SortHeaderController(activeHeaderColor, inactiveHeaderColor, OnSortHeaderButtonClicked);
			sortController.Register(sortByNameButton, 4);
			sortController.Register(sortByMissionButton, 5);
			sortController.Register(sortByMapButton, 6);
			sortController.Register(sortByPlayerCountButton, 2);
			sortController.Register(sortByPingButton, 3);
			sortController.Register(sortByDurationButton, 7);
			SetSortMode(_activeSortOrder, setPrevious: true);
			UpdateTimeLoop().Forget();
		}

		private void Update()
		{
			SteamLobby.instance.CheckPingServers();
		}

		private void OnLobbyPingUpdated(ServerLobbyInstance lobby)
		{
			if (allLobbies.TryGetValue(lobby, out var value))
			{
				value.UpdatePing();
				return;
			}
			int value2 = lobby.CalculatePing().Value;
			if (activeFilter.PingDistanceAllowed(value2))
			{
				OnLobbyDataUpdated(lobby);
			}
		}

		private void Instance_OnLocationUpdated(string _)
		{
			foreach (LobbyListItem sort in sortList)
			{
				if (sort.lobby is PlayerLobbyInstance)
				{
					sort.UpdatePing();
				}
			}
		}

		private async UniTaskVoid UpdateTimeLoop()
		{
			CancellationToken cancel = base.destroyCancellationToken;
			while (!cancel.IsCancellationRequested)
			{
				foreach (LobbyListItem sort in sortList)
				{
					sort.UpdateTime();
				}
				await UniTask.Delay(TimeSpan.FromMinutes(1.0));
			}
		}

		private void CreateLobbyClicked()
		{
			createLobbyModal.Show(this);
		}

		private async UniTaskVoid MainMenuClicked()
		{
			mainMenuButton.interactable = false;
			await NetworkManagerNuclearOption.i.LoadSystemScene(MapLoader.MainMenu, warnIfAlreadyLoaded: false);
			if (mainMenuButton != null)
			{
				mainMenuButton.interactable = true;
			}
		}

		private void Start()
		{
			refreshOverlay.Show();
			GetListOfLobbies();
			LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)base.transform);
		}

		private void OnDestroy()
		{
			if (SteamLobby.instance != null)
			{
				SteamLobby.instance.OnLobbyListCleared -= RefreshLobbyListCleared;
				SteamLobby.instance.OnLobbyDataUpdated -= OnLobbyDataUpdated;
				SteamLobby.instance.OnLobbyPingUpdated -= OnLobbyPingUpdated;
				SteamLobby.instance.OnLobbyRefreshFinished -= HideRefreshOverlay;
				SteamLobby.instance.OnLocationUpdated -= Instance_OnLocationUpdated;
				SteamLobby.instance.ClearLobbyCache();
			}
		}

		public void ShowLobbyPopup(LobbyInstance lobby)
		{
			lobbyPopup.Show(this, lobby);
		}

		private void RefreshLobbyListCleared()
		{
			if (!refreshPending)
			{
				refreshPending = true;
				refreshOverlay.Show();
				ClearLobbies();
			}
		}

		private void ClearLobbies()
		{
			foreach (LobbyListItem value in allLobbies.Values)
			{
				value.Hide();
				pool.Push(value);
			}
			allLobbies.Clear();
			UpdateLobbyList();
		}

		private LobbyListItem GetFromPool()
		{
			if (pool.TryPop(out var result))
			{
				return result;
			}
			return UnityEngine.Object.Instantiate(entryPrefab, lobbyListContent.transform).GetComponent<LobbyListItem>();
		}

		private void HideRefreshOverlay()
		{
			if (refreshPending)
			{
				refreshPending = false;
				refreshOverlay.Hide();
			}
		}

		private void OnLobbyDataUpdated(LobbyInstance lobby)
		{
			HideRefreshOverlay();
			if (!allLobbies.TryGetValue(lobby, out var value))
			{
				value = GetFromPool();
			}
			bool flag = false;
			try
			{
				flag = value.Show(this, lobby);
			}
			catch (Exception arg)
			{
				Debug.LogError($"Error caught in OnLobbyDataUpdated: {arg}");
			}
			if (flag)
			{
				allLobbies[lobby] = value;
				InsertLobbyDataEntry(value);
			}
			else
			{
				pool.Push(value);
			}
		}

		public void GetListOfLobbies()
		{
			activeFilter = new LobbySearchFilter
			{
				HideFull = hideFullServersToggle.isOn,
				HideEmpty = hideEmptyServersToggle.isOn,
				HidePasswordProtected = hidePasswordProtectedToggle.isOn,
				MissionPvpType = (MissionPvpType)missionTypeToggleGroup.GetIndex(),
				ServerType = (FilterServerType)serverTypeToggleGroup.GetIndex(),
				distanceFilter = DistanceToggleToEnum()
			};
			SteamLobby.instance.GetLobbiesList(activeFilter);
		}

		private ELobbyDistanceFilter? DistanceToggleToEnum()
		{
			return distanceFilterToggleGroup.GetIndex() switch
			{
				0 => ELobbyDistanceFilter.k_ELobbyDistanceFilterDefault, 
				1 => ELobbyDistanceFilter.k_ELobbyDistanceFilterFar, 
				_ => null, 
			};
		}

		private void OnSortHeaderButtonClicked(SortOrder order)
		{
			SetSortMode(order, setPrevious: true);
			UpdateLobbyList();
		}

		private void OnSearchTermChanged(string searchTerm)
		{
			if (!string.IsNullOrWhiteSpace(searchTerm))
			{
				SetSortMode(new SortOrder(1, isAscending: true), setPrevious: false);
			}
			else
			{
				SetSortMode(_previousHeaderSortOrder, setPrevious: false);
			}
			UpdateLobbyList();
		}

		private void OnClearSearchClicked()
		{
			searchInputField.SetTextWithoutNotify(string.Empty);
			SetSortMode(_previousHeaderSortOrder, setPrevious: false);
			UpdateLobbyList();
		}

		private void SetSortMode(SortOrder newOrder, bool setPrevious)
		{
			if (setPrevious)
			{
				_previousHeaderSortOrder = _activeSortOrder;
			}
			_activeSortOrder = newOrder;
			sortController.UpdateVisuals(newOrder);
		}

		private void UpdateLobbyList()
		{
			sortList.Clear();
			_processedLobbiesSet.Clear();
			FuzzySearch.SubstringSearch(allLobbies.Values, (LobbyListItem l) => l.LobbyName, searchInputField.text, sortList);
			if (_activeSortOrder.Mode != 1)
			{
				Comparison<LobbyListItem> comparer = GetComparer(_activeSortOrder);
				sortList.Sort(comparer);
			}
			foreach (LobbyListItem sort in sortList)
			{
				_processedLobbiesSet.Add(sort);
			}
			foreach (LobbyListItem value in allLobbies.Values)
			{
				value.gameObject.SetActive(_processedLobbiesSet.Contains(value));
			}
			for (int num = 0; num < sortList.Count; num++)
			{
				sortList[num].transform.SetSiblingIndex(num);
			}
		}

		private void InsertLobbyDataEntry(LobbyListItem newEntry)
		{
			if (!string.IsNullOrWhiteSpace(searchInputField.text))
			{
				UpdateLobbyList();
				return;
			}
			Comparison<LobbyListItem> comparer = GetComparer(_activeSortOrder);
			int num = -1;
			for (int i = 0; i < sortList.Count; i++)
			{
				if (comparer(newEntry, sortList[i]) < 0)
				{
					num = i;
					break;
				}
			}
			if (num == -1)
			{
				num = sortList.Count;
			}
			sortList.Insert(num, newEntry);
			newEntry.gameObject.SetActive(value: true);
			for (int j = num; j < sortList.Count; j++)
			{
				sortList[j].transform.SetSiblingIndex(j);
			}
		}

		private Comparison<LobbyListItem> GetComparer(SortOrder order)
		{
			Comparison<LobbyListItem> comparison = GetComparer((SortMode)order.Mode);
			if (order.IsAscending)
			{
				return comparison;
			}
			return (LobbyListItem a, LobbyListItem b) => comparison(b, a);
		}

		private Comparison<LobbyListItem> GetComparer(SortMode mode)
		{
			return mode switch
			{
				SortMode.PlayerCount => (LobbyListItem a, LobbyListItem b) => a.PlayerCount.CompareTo(b.PlayerCount), 
				SortMode.Ping => (LobbyListItem a, LobbyListItem b) => SortHelper.CompareNullable(a.Ping, b.Ping), 
				SortMode.Name => (LobbyListItem a, LobbyListItem b) => string.Compare(a.LobbyName, b.LobbyName, StringComparison.OrdinalIgnoreCase), 
				SortMode.Mission => (LobbyListItem a, LobbyListItem b) => string.Compare(a.MissionName, b.MissionName, StringComparison.OrdinalIgnoreCase), 
				SortMode.Map => (LobbyListItem a, LobbyListItem b) => string.Compare(a.MapName, b.MapName, StringComparison.OrdinalIgnoreCase), 
				SortMode.Duration => (LobbyListItem a, LobbyListItem b) => SortHelper.CompareNullable(a.StartTime, b.StartTime), 
				_ => (LobbyListItem a, LobbyListItem b) => 0, 
			};
		}
	}
}
