using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Steamworks;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.Workshop
{
	public class SteamWorkshopItem
	{
		public readonly string Name;

		public readonly string Tag;

		public string Description;

		public string ImagePath;

		public bool Public;

		public string ContentPath;

		public string PreviewURL;

		public bool Subscribed;

		public CSteamID OwnerId;

		public string OwnerName { get; private set; }

		public PublishedFileId_t WorkshopId { get; private set; }

		public event Action OwnerNameChanged;

		public SteamWorkshopItem(string name, string tag, PublishedFileId_t itemId)
		{
			Name = name;
			Tag = tag;
			WorkshopId = itemId;
		}

		public void OnItemCreated(PublishedFileId_t itemId)
		{
			WorkshopId = itemId;
		}

		public void SetOwnerName(string name)
		{
			OwnerName = name;
			this.OwnerNameChanged?.Invoke();
		}

		public UniTask<Sprite> GetPreview(CancellationToken cancellationToken = default(CancellationToken))
		{
			return SteamWorkshop.DownloadImage(PreviewURL, cancellationToken);
		}

		public UniTask SetPreviewImageAsync(Image image, ref CancellationTokenSource cancellationSource)
		{
			cancellationSource?.Cancel();
			cancellationSource = null;
			if (PreviewURL != null)
			{
				cancellationSource = new CancellationTokenSource();
				return SetPreviewAsync(image, cancellationSource.Token);
			}
			image.sprite = null;
			image.color = new Color(0.2f, 0.2f, 0.2f);
			return UniTask.CompletedTask;
		}

		private async UniTask SetPreviewAsync(Image image, CancellationToken cancellationToken)
		{
			Sprite sprite = await GetPreview(cancellationToken);
			if (!(sprite == null) && !(image == null) && !cancellationToken.IsCancellationRequested)
			{
				image.color = Color.white;
				image.sprite = sprite;
			}
		}

		public void OpenSteamPage()
		{
			OpenSteamPage(WorkshopId);
		}

		public static void OpenSteamPage(PublishedFileId_t itemId)
		{
			string text = $"steam://url/CommunityFilePage/{itemId}";
			Debug.Log("Opening steam " + text);
			SteamFriends.ActivateGameOverlayToWebPage(text);
		}

		public void OpenLocalContent()
		{
			Debug.Log("Opening local content " + ContentPath);
			Application.OpenURL(ContentPath);
		}

		public bool IsOwner()
		{
			return SteamUser.GetSteamID() == OwnerId;
		}
	}
}
