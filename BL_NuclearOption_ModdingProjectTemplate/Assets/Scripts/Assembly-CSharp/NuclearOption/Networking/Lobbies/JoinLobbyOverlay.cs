using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.Networking.Lobbies
{
	public class JoinLobbyOverlay : MonoBehaviour
	{
		private static JoinLobbyOverlay Instance;

		[SerializeField]
		private GameObject overlay;

		[Space]
		[SerializeField]
		private TextMeshProUGUI titleText;

		[SerializeField]
		private TextMeshProUGUI bodyText;

		[Space]
		[SerializeField]
		private TextMeshProUGUI closeText;

		[SerializeField]
		private MaskableGraphic closeImage;

		[SerializeField]
		private MaskableGraphic closeBorderImage;

		[SerializeField]
		private Button closeButton;

		[Header("Title Color")]
		[SerializeField]
		private Color joiningTitleColor;

		[SerializeField]
		private Color failTitleColor;

		[Header("Close Color")]
		[SerializeField]
		private Color joiningCloseImageColor;

		[SerializeField]
		private Color joiningCloseBorderImageColor;

		[SerializeField]
		private Color joiningCloseTextColor;

		[Space]
		[SerializeField]
		private Color failCloseImageColor;

		[SerializeField]
		private Color failCloseBorderImageColor;

		[SerializeField]
		private Color failCloseTextColor;

		private void Awake()
		{
			overlay.SetActive(value: false);
			closeButton.onClick.AddListener(Close);
		}

		private void OnDestroy()
		{
			Instance = null;
		}

		public static void Open(JoinProgress joinProgress)
		{
			ColorLog<JoinProgress>.Info(joinProgress.ToString());
			if (Instance == null)
			{
				GameObject obj = Object.Instantiate((GameObject)Resources.Load("JoinLobbyOverlayCanvas"));
				Instance = obj.GetComponent<JoinLobbyOverlay>();
				Object.DontDestroyOnLoad(obj);
			}
			Instance.SetValues(joinProgress);
		}

		public static void Close()
		{
			if (!(Instance == null))
			{
				Instance.overlay.SetActive(value: false);
			}
		}

		private void SetValues(JoinProgress joinProgress)
		{
			if (joinProgress.Join)
			{
				titleText.text = "Joining Server...";
				closeText.text = "Cancel";
				titleText.color = joiningTitleColor;
				closeImage.color = joiningCloseImageColor;
				closeBorderImage.color = joiningCloseBorderImageColor;
				closeText.color = joiningCloseTextColor;
			}
			else
			{
				titleText.text = "Connect Failed";
				closeText.text = "Close";
				titleText.color = failTitleColor;
				closeImage.color = failCloseImageColor;
				closeBorderImage.color = failCloseBorderImageColor;
				closeText.color = failCloseTextColor;
			}
			overlay.SetActive(value: true);
			bodyText.text = joinProgress.Body;
		}

		private void CloseButton()
		{
			overlay.SetActive(value: false);
			if (NetworkManagerNuclearOption.i.Client.Active)
			{
				NetworkManagerNuclearOption.i.Stop(setDisconnectReason: true);
			}
		}
	}
}
