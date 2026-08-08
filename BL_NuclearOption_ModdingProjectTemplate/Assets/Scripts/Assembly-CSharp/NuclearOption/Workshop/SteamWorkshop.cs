using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using NuclearOption.ModScripts;
using Steamworks;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Networking;

namespace NuclearOption.Workshop
{
	public class SteamWorkshop : MonoBehaviour
	{
		public class ClearSteamImageCache : MonoBehaviour
		{
			private static ClearSteamImageCache instance;

			public static void Create()
			{
				if (!(instance != null))
				{
					new GameObject("ClearSteamImageCache", typeof(ClearSteamImageCache));
				}
			}

			private void Awake()
			{
				instance = this;
			}

			private void OnDestroy()
			{
				ClearPreviewCache();
			}
		}

		private static readonly ProfilerMarker getSubscribedItemsMarker = new ProfilerMarker("SteamWorkshop.GetSubscribedItems");

		private static readonly ProfilerMarker refreshSubscribedMarker = new ProfilerMarker("SteamWorkshop.RefreshSubscribed");

		private static readonly ProfilerMarker readFileOrInvalidMarker = new ProfilerMarker("WorkshopJson.ReadFileOrInvalid");

		private const int MAX_PATH_LENGTH = 1000;

		public const string UNKNOWN_NAME = "[unknown]";

		private static PublishedFileId_t[] subscribedCache = new PublishedFileId_t[100];

		private static ArraySegment<PublishedFileId_t> Subscribed;

		private static readonly Dictionary<string, Sprite> previewCache = new Dictionary<string, Sprite>();

		private Callback<PersonaStateChange_t> _personaStateChangeCallback;

		private static readonly Dictionary<ulong, SteamWorkshopItem> ownerNamesToUpdate = new Dictionary<ulong, SteamWorkshopItem>();

		[SerializeField]
		private SteamErrorPopup errorPopup;

		private void Awake()
		{
			ClearSteamImageCache.Create();
			_personaStateChangeCallback = Callback<PersonaStateChange_t>.Create(OnPersonaStateChange);
		}

		private void OnDestroy()
		{
			_personaStateChangeCallback?.Dispose();
			ownerNamesToUpdate.Clear();
		}

		public static void ClearPreviewCache()
		{
			foreach (Sprite value in previewCache.Values)
			{
				UnityEngine.Object.Destroy(value.texture);
				UnityEngine.Object.Destroy(value);
			}
			previewCache.Clear();
		}

		private void OnPersonaStateChange(PersonaStateChange_t param)
		{
			if (ownerNamesToUpdate.TryGetValue(param.m_ulSteamID, out var value))
			{
				ownerNamesToUpdate.Remove(param.m_ulSteamID);
				value.SetOwnerName(SteamFriends.GetFriendPersonaName(new CSteamID(param.m_ulSteamID)));
			}
		}

		private async UniTask<(bool, EResult)> CreateItem(SteamWorkshopItem item, IProgress<(float overall, float part)> progress, IWorkshopCreateCallbacks createCallbacks)
		{
			var (id, flag, item2) = await CreateAsync();
			if (!flag)
			{
				return (false, item2);
			}
			createCallbacks.OnCreateOrFail(id);
			(bool success, EResult) uploadResult = await UpdateItem(id, item, "First Version", progress);
			if (uploadResult.success)
			{
				item.OnItemCreated(id);
			}
			else
			{
				createCallbacks.OnCreateOrFail(PublishedFileId_t.Invalid);
				(DeleteItemResult_t, bool) obj = await WaitAsync<DeleteItemResult_t>(SteamUGC.DeleteItem(id));
				var (deleteItemResult_t, _) = obj;
				if (obj.Item2 || deleteItemResult_t.m_eResult != EResult.k_EResultOK)
				{
					Debug.LogError($"Failed to clear up create item, {id} after failing to update");
				}
			}
			return uploadResult;
		}

		private void AssertSuccess(string tag, bool success)
		{
			if (!success)
			{
				Debug.LogError("Failed to set " + tag + " on SteamUGC");
			}
		}

		public UniTask<(bool success, EResult)> UpdateItem(PublishedFileId_t itemId, SteamWorkshopItem item, string changeLog, IProgress<(float overall, float part)> progress = null)
		{
			UGCUpdateHandle_t uGCUpdateHandle_t = SteamUGC.StartItemUpdate(SteamUtils.GetAppID(), itemId);
			AssertSuccess("title", SteamUGC.SetItemTitle(uGCUpdateHandle_t, item.Name));
			AssertSuccess("description", SteamUGC.SetItemDescription(uGCUpdateHandle_t, item.Description));
			AssertSuccess("visiblity", SteamUGC.SetItemVisibility(uGCUpdateHandle_t, (!item.Public) ? ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityUnlisted : ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPublic));
			AssertSuccess("tag", SteamUGC.SetItemTags(uGCUpdateHandle_t, new List<string> { item.Tag }));
			AssertSuccess("content", SteamUGC.SetItemContent(uGCUpdateHandle_t, item.ContentPath));
			if (!string.IsNullOrEmpty(item.ImagePath))
			{
				SteamUGC.SetItemPreview(uGCUpdateHandle_t, item.ImagePath);
			}
			return SubmitAsync(uGCUpdateHandle_t, changeLog, progress);
		}

		private async UniTask<(PublishedFileId_t, bool, EResult)> CreateAsync()
		{
			var (createItemResult_t, flag) = await WaitAsync<CreateItemResult_t>(SteamUGC.CreateItem(SteamUtils.GetAppID(), EWorkshopFileType.k_EWorkshopFileTypeFirst));
			CheckLegal(createItemResult_t.m_bUserNeedsToAcceptWorkshopLegalAgreement);
			string message = $"Create IOFailur={flag} Result={createItemResult_t.m_eResult} item={createItemResult_t.m_nPublishedFileId} need Legal={createItemResult_t.m_bUserNeedsToAcceptWorkshopLegalAgreement}";
			if (flag || createItemResult_t.m_eResult != EResult.k_EResultOK)
			{
				ColorLog<SteamWorkshop>.LogError(message);
			}
			if (flag)
			{
				return (default(PublishedFileId_t), false, EResult.k_EResultNone);
			}
			if (createItemResult_t.m_eResult != EResult.k_EResultOK)
			{
				return (default(PublishedFileId_t), false, createItemResult_t.m_eResult);
			}
			return (createItemResult_t.m_nPublishedFileId, true, createItemResult_t.m_eResult);
		}

		private async UniTask<(bool, EResult)> SubmitAsync(UGCUpdateHandle_t handle, string changeLog, IProgress<(float overall, float part)> progress)
		{
			SteamAPICall_t request = SteamUGC.SubmitItemUpdate(handle, changeLog);
			UniTask<(SubmitItemUpdateResult_t result, bool ioFailure)> task = WaitAsync<SubmitItemUpdateResult_t>(request);
			float percent = 0f;
			while (task.Status == UniTaskStatus.Pending)
			{
				ulong punBytesProcessed;
				ulong punBytesTotal;
				EItemUpdateStatus itemUpdateProgress = SteamUGC.GetItemUpdateProgress(handle, out punBytesProcessed, out punBytesTotal);
				float target = ((itemUpdateProgress == EItemUpdateStatus.k_EItemUpdateStatusInvalid) ? 1f : ((float)(itemUpdateProgress - 1) / 5f));
				percent = Mathf.MoveTowards(percent, target, 0.02f);
				float num = (float)punBytesProcessed / (float)punBytesTotal;
				if (!float.IsFinite(num))
				{
					num = 0f;
				}
				progress?.Report((percent, num));
				await UniTask.Yield();
			}
			(SubmitItemUpdateResult_t, bool) obj = await task;
			SubmitItemUpdateResult_t item = obj.Item1;
			bool item2 = obj.Item2;
			bool flag = !item2 && item.m_eResult == EResult.k_EResultOK;
			string message = $"Submit bIOFailure={item2} Result={item.m_eResult} item={item.m_nPublishedFileId} need Legal={item.m_bUserNeedsToAcceptWorkshopLegalAgreement}";
			if (!flag)
			{
				ColorLog<SteamWorkshop>.LogError(message);
			}
			CheckLegal(item.m_bUserNeedsToAcceptWorkshopLegalAgreement);
			if (flag)
			{
				progress?.Report((1f, 1f));
			}
			return (flag, item.m_eResult);
		}

		private void CheckLegal(bool needLegal)
		{
			if (needLegal)
			{
				errorPopup.Show("Legal Agreement Required", "You need to accept the Steam Workshop Legal Agreement.", "Open Agreement", delegate
				{
					Application.OpenURL("http://steamcommunity.com/sharedfiles/workshoplegalagreement");
				});
			}
		}

		private static async UniTask<(T result, bool ioFailure)> WaitAsync<T>(SteamAPICall_t request)
		{
			if (request == SteamAPICall_t.Invalid)
			{
				throw new ArgumentException("Steam Api call is invalid");
			}
			CallResult<T> callResult = CallResult<T>.Create();
			UniTaskCompletionSource<(T param, bool bIOFailure)> completionSource = new UniTaskCompletionSource<(T, bool)>();
			callResult.Set(request, delegate(T a, bool b)
			{
				completionSource.TrySetResult((a, b));
			});
			return await completionSource.Task;
		}

		private static EUGCQuery OrderByToQuery(OrderBy orderBy)
		{
			return orderBy switch
			{
				OrderBy.Trend30Days => EUGCQuery.k_EUGCQuery_RankedByTrend, 
				OrderBy.TopAllTime => EUGCQuery.k_EUGCQuery_RankedByVote, 
				OrderBy.New => EUGCQuery.k_EUGCQuery_RankedByPublicationDate, 
				_ => throw new ArgumentException("Invalid OrderBy value"), 
			};
		}

		public async UniTask<bool> RefreshItems(OrderBy orderBy, string tag, List<SteamWorkshopItem> results, uint page, string searchText = null)
		{
			AppId_t appID = SteamUtils.GetAppID();
			UGCQueryHandle_t handle = SteamUGC.CreateQueryAllUGCRequest(OrderByToQuery(orderBy), EUGCMatchingUGCType.k_EUGCMatchingUGCType_Items, appID, appID, page);
			SteamUGC.AddRequiredTag(handle, tag);
			if (!string.IsNullOrEmpty(searchText))
			{
				SteamUGC.SetSearchText(handle, searchText);
			}
			await RunItemQuery(orderBy, refreshSubscribed: true, results, handle);
			return results.Count == 50;
		}

		private static async UniTask RunItemQuery(OrderBy? orderBy, bool refreshSubscribed, List<SteamWorkshopItem> results, UGCQueryHandle_t handle)
		{
			try
			{
				if (orderBy == OrderBy.Trend30Days)
				{
					SteamUGC.SetRankedByTrendDays(handle, 30u);
				}
				(SteamUGCQueryCompleted_t, bool) obj = await WaitAsync<SteamUGCQueryCompleted_t>(SteamUGC.SendQueryUGCRequest(handle));
				var (steamUGCQueryCompleted_t, _) = obj;
				if (obj.Item2)
				{
					throw new Exception("IO Failed");
				}
				if (steamUGCQueryCompleted_t.m_eResult != EResult.k_EResultOK)
				{
					throw new Exception($"Failed to refresh workshop list, result={steamUGCQueryCompleted_t.m_eResult}");
				}
				if (refreshSubscribed)
				{
					RefreshSubscribed();
				}
				results.Clear();
				for (uint num = 0u; num < steamUGCQueryCompleted_t.m_unNumResultsReturned; num++)
				{
					if (!SteamUGC.GetQueryUGCResult(handle, num, out var pDetails))
					{
						Debug.LogError("Failed to get details");
						continue;
					}
					if (pDetails.m_eResult == EResult.k_EResultFileNotFound)
					{
						Debug.Log($"Item not found {pDetails.m_nPublishedFileId} (maybe it is deleted)");
						continue;
					}
					if (pDetails.m_eResult != EResult.k_EResultOK)
					{
						Debug.LogError($"Details result not ok: {pDetails.m_eResult}");
						continue;
					}
					CSteamID cSteamID = new CSteamID(pDetails.m_ulSteamIDOwner);
					SteamWorkshopItem steamWorkshopItem = new SteamWorkshopItem(pDetails.m_rgchTitle, pDetails.m_rgchTags, pDetails.m_nPublishedFileId)
					{
						Description = pDetails.m_rgchDescription,
						OwnerId = cSteamID,
						Public = (pDetails.m_eVisibility == ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPublic),
						Subscribed = (Subscribed.Array != null && Subscribed.Contains(pDetails.m_nPublishedFileId))
					};
					string friendPersonaName = SteamFriends.GetFriendPersonaName(cSteamID);
					if (friendPersonaName == "[unknown]")
					{
						ownerNamesToUpdate.Add(steamWorkshopItem.WorkshopId.m_PublishedFileId, steamWorkshopItem);
						SteamFriends.RequestUserInformation(cSteamID, bRequireNameOnly: true);
					}
					steamWorkshopItem.SetOwnerName(friendPersonaName);
					if (SteamUGC.GetItemInstallInfo(steamWorkshopItem.WorkshopId, out var _, out var pchFolder, 1000u, out var _))
					{
						steamWorkshopItem.ContentPath = pchFolder;
					}
					results.Add(steamWorkshopItem);
					if (SteamUGC.GetQueryUGCPreviewURL(handle, num, out var pchURL, 1000u))
					{
						steamWorkshopItem.PreviewURL = pchURL;
					}
					else
					{
						Debug.Log("No preview");
					}
				}
			}
			finally
			{
				SteamUGC.ReleaseQueryUGCRequest(handle);
			}
		}

		public static async UniTask<(bool success, SteamWorkshopItem details)> GetDetails(PublishedFileId_t id)
		{
			UGCQueryHandle_t handle = SteamUGC.CreateQueryUGCDetailsRequest(new PublishedFileId_t[1] { id }, 1u);
			List<SteamWorkshopItem> resultsList = new List<SteamWorkshopItem>(1);
			resultsList.Clear();
			await RunItemQuery(OrderBy.Trend30Days, refreshSubscribed: false, resultsList, handle);
			if (resultsList.Count == 0)
			{
				return (false, null);
			}
			SteamWorkshopItem item = resultsList.First();
			return (true, item);
		}

		public async UniTask DownloadItem(SteamWorkshopItem item, IProgress<float> progress = null)
		{
			(RemoteStorageSubscribePublishedFileResult_t, bool) obj = await WaitAsync<RemoteStorageSubscribePublishedFileResult_t>(SteamUGC.SubscribeItem(item.WorkshopId));
			var (remoteStorageSubscribePublishedFileResult_t, _) = obj;
			if (obj.Item2 || remoteStorageSubscribePublishedFileResult_t.m_eResult != EResult.k_EResultOK)
			{
				throw new Exception("Failed to subscribe to item");
			}
			await DownloadAsync(item.WorkshopId, progress);
			item.Subscribed = true;
		}

		private static async UniTask DownloadAsync(PublishedFileId_t itemId, IProgress<float> progress = null)
		{
			if (!SteamUGC.DownloadItem(itemId, bHighPriority: true))
			{
				throw new Exception("Failed to start download");
			}
			ulong punBytesDownloaded;
			ulong punBytesTotal;
			while (!SteamUGC.GetItemDownloadInfo(itemId, out punBytesDownloaded, out punBytesTotal))
			{
				progress?.Report((float)punBytesDownloaded / (float)punBytesTotal);
				await UniTask.Yield();
			}
		}

		public static async UniTask<bool> DownloadItemServer(PublishedFileId_t itemId, CancellationToken cancellation = default(CancellationToken))
		{
			if (!SteamManager.ServerInitialized)
			{
				throw new InvalidOperationException("DownloadItemServer called when server is not init");
			}
			EItemState itemState = (EItemState)SteamGameServerUGC.GetItemState(itemId);
			if (AlreadyUpToDate(itemState) && CheckFolderExists(itemId))
			{
				ColorLog<SteamWorkshop>.Info($"Workshop item {itemId} is already installed and up to date.");
				return true;
			}
			if (!SteamGameServerUGC.DownloadItem(itemId, bHighPriority: true))
			{
				ColorLog<SteamWorkshop>.LogError($"Failed to start download for workshop item {itemId}");
				return false;
			}
			int inactiveTicks = 0;
			int lastLoggedProgressPercent = -10;
			do
			{
				itemState = (EItemState)SteamGameServerUGC.GetItemState(itemId);
				if (IsInstalledAndReady(itemState) && CheckFolderExists(itemId))
				{
					ColorLog<SteamWorkshop>.Info($"Successfully downloaded workshop item {itemId}.");
					return true;
				}
				if (IsDownloadingOrPending(itemState))
				{
					inactiveTicks = 0;
					if (SteamGameServerUGC.GetItemDownloadInfo(itemId, out var punBytesDownloaded, out var punBytesTotal))
					{
						int num = Mathf.RoundToInt(((punBytesTotal != 0) ? ((float)punBytesDownloaded / (float)punBytesTotal) : 0f) * 100f);
						if (num >= lastLoggedProgressPercent + 10 || num == 100)
						{
							lastLoggedProgressPercent = num;
							ColorLog<SteamWorkshop>.Info($"Downloading workshop item {itemId}: {num}%");
						}
					}
				}
				else
				{
					inactiveTicks++;
					if (inactiveTicks > 50)
					{
						ColorLog<SteamWorkshop>.LogError($"Failed to download workshop item {itemId}. State: {itemState}");
						return false;
					}
				}
				await UniTask.Delay(100);
			}
			while (!cancellation.IsCancellationRequested);
			return false;
			static bool AlreadyUpToDate(EItemState state)
			{
				if (state.HasFlag(EItemState.k_EItemStateInstalled))
				{
					return !state.HasFlag(EItemState.k_EItemStateNeedsUpdate);
				}
				return false;
			}
			static bool IsDownloadingOrPending(EItemState state)
			{
				if (!state.HasFlag(EItemState.k_EItemStateDownloading))
				{
					return state.HasFlag(EItemState.k_EItemStateDownloadPending);
				}
				return true;
			}
			static bool IsInstalledAndReady(EItemState state)
			{
				if (state.HasFlag(EItemState.k_EItemStateInstalled) && !state.HasFlag(EItemState.k_EItemStateNeedsUpdate))
				{
					return !state.HasFlag(EItemState.k_EItemStateDownloading);
				}
				return false;
			}
		}

		private static bool CheckFolderExists(PublishedFileId_t itemId)
		{
			if (TryGetInstallFolder(itemId, out var folder) && Directory.Exists(folder))
			{
				return Directory.GetFileSystemEntries(folder).Length != 0;
			}
			return false;
		}

		public async UniTask Unsubscribe(SteamWorkshopItem item)
		{
			(RemoteStorageUnsubscribePublishedFileResult_t, bool) obj = await WaitAsync<RemoteStorageUnsubscribePublishedFileResult_t>(SteamUGC.UnsubscribeItem(item.WorkshopId));
			var (remoteStorageUnsubscribePublishedFileResult_t, _) = obj;
			if (obj.Item2 || remoteStorageUnsubscribePublishedFileResult_t.m_eResult != EResult.k_EResultOK)
			{
				throw new Exception("Failed to subscribe to item");
			}
			item.Subscribed = false;
		}

		public UniTask<(bool success, EResult)> CreateOrUpdateItem(SteamWorkshopItem item, string changeLog, IWorkshopCreateCallbacks createCallbacks, IProgress<(float overall, float part)> progress = null)
		{
			progress?.Report((0f, 0f));
			if (item.WorkshopId == PublishedFileId_t.Invalid)
			{
				return CreateItem(item, progress, createCallbacks);
			}
			return UpdateItem(item.WorkshopId, item, changeLog, progress);
		}

		public static void RefreshSubscribed()
		{
			using (refreshSubscribedMarker.Auto())
			{
				Subscribed = new ArraySegment<PublishedFileId_t>(count: (SteamManager.ClientInitialized || SteamManager.ServerInitialized) ? RefreshSubscribedInternal() : 0, array: subscribedCache, offset: 0);
			}
		}

		private static int RefreshSubscribedInternal()
		{
			uint num = (SteamManager.ClientInitialized ? SteamUGC.GetNumSubscribedItems() : SteamGameServerUGC.GetNumSubscribedItems());
			Debug.Log($"{num} Subscribed Items");
			checked
			{
				int num2 = (int)num;
				if (num2 > subscribedCache.Length)
				{
					if (num2 > 1000000)
					{
						Debug.LogError("Subscribed Count was greater than max. Clamping value");
						num2 = 1000000;
					}
					Array.Resize(ref subscribedCache, num2 + 20);
				}
				return (int)(SteamManager.ClientInitialized ? SteamUGC.GetSubscribedItems(subscribedCache, (uint)subscribedCache.Length) : SteamGameServerUGC.GetSubscribedItems(subscribedCache, (uint)subscribedCache.Length));
			}
		}

		public static List<SubscribedItem> GetSubscribedItems(bool refresh, SubscribedItemType? filterType)
		{
			using (getSubscribedItemsMarker.Auto())
			{
				if (refresh)
				{
					RefreshSubscribed();
				}
				List<SubscribedItem> list = new List<SubscribedItem>();
				for (int i = 0; i < Subscribed.Count; i++)
				{
					PublishedFileId_t publishedFileId_t = subscribedCache[i];
					bool flag = false;
					string pchFolder = null;
					if (SteamManager.ClientInitialized)
					{
						flag = SteamUGC.GetItemInstallInfo(publishedFileId_t, out var _, out pchFolder, 1000u, out var _);
					}
					else if (SteamManager.ServerInitialized)
					{
						flag = SteamGameServerUGC.GetItemInstallInfo(publishedFileId_t, out var _, out pchFolder, 1000u, out var _);
					}
					if (!flag)
					{
						Debug.LogWarning($"Failed to get item info for id {publishedFileId_t.m_PublishedFileId}");
						continue;
					}
					WorkshopJson workshop = WorkshopJson.ReadFileOrInvalid(pchFolder);
					if (!filterType.HasValue || workshop.Type == filterType.Value)
					{
						list.Add(new SubscribedItem(publishedFileId_t, pchFolder, workshop));
					}
				}
				return list;
			}
		}

		public static bool TryGetInstallFolder(PublishedFileId_t itemId, out string folder)
		{
			if (itemId == PublishedFileId_t.Invalid)
			{
				Debug.LogWarning("Invalid Id");
				folder = null;
				return false;
			}
			if (!SteamManager.ClientInitialized && !SteamManager.ServerInitialized)
			{
				Debug.LogError("Steam not initialized");
				folder = null;
				return false;
			}
			if (SteamManager.ClientInitialized ? SteamUGC.GetItemInstallInfo(itemId, out var punSizeOnDisk, out folder, 1000u, out var punTimeStamp) : SteamGameServerUGC.GetItemInstallInfo(itemId, out punSizeOnDisk, out folder, 1000u, out punTimeStamp))
			{
				return true;
			}
			Debug.LogWarning($"Failed to resolve steam folder for ({itemId})");
			return false;
		}

		public static bool AnyNeedUpdates()
		{
			foreach (SubscribedItem subscribedItem in GetSubscribedItems(refresh: true, null))
			{
				EItemState itemState = (EItemState)SteamUGC.GetItemState(subscribedItem.Id);
				if (itemState.HasFlag(EItemState.k_EItemStateNeedsUpdate))
				{
					return true;
				}
			}
			return false;
		}

		public static UniTask UpdateAllSubscribedItems()
		{
			List<SubscribedItem> subscribedItems = GetSubscribedItems(refresh: true, null);
			List<UniTask> list = new List<UniTask>();
			foreach (SubscribedItem item in subscribedItems)
			{
				EItemState itemState = (EItemState)SteamUGC.GetItemState(item.Id);
				if (itemState.HasFlag(EItemState.k_EItemStateNeedsUpdate))
				{
					list.Add(DownloadAsync(item.Id));
				}
			}
			return UniTask.WhenAll(list);
		}

		public static async UniTask<Sprite> DownloadImage(string url, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (string.IsNullOrEmpty(url))
			{
				Debug.LogError("Image url was null");
				return null;
			}
			if (previewCache.TryGetValue(url, out var value))
			{
				return value;
			}
			using UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
			await request.SendWebRequest().ToUniTask(null, PlayerLoopTiming.Update, cancellationToken);
			if (request.result == UnityWebRequest.Result.Success)
			{
				Texture2D texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
				value = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
				previewCache[url] = value;
				return value;
			}
			Debug.LogError("Failed to download image: " + request.error);
			return null;
		}

		public static void OpenWorkshopPage()
		{
			SteamFriends.ActivateGameOverlayToWebPage($"steam://url/SteamWorkshopPage/{SteamUtils.GetAppID()}");
		}
	}
}
