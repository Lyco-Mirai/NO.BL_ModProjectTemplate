using Mirage;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.Networking
{
	public class WaitingForDedicatedServerMenu : MonoBehaviour
	{
		[SerializeField]
		private Button DisconnectButton;

		[SerializeField]
		private TextMeshProUGUI loadingText;

		[SerializeField]
		private TextMeshProUGUI pingText;

		private float updateTime;

		private void Awake()
		{
			DisconnectButton.onClick.AddListener(DisconnectClicked);
		}

		private void Update()
		{
			float timeSinceLevelLoad = Time.timeSinceLevelLoad;
			if (timeSinceLevelLoad > updateTime)
			{
				updateTime = timeSinceLevelLoad + 1f;
				NetworkClient client = NetworkManagerNuclearOption.i.Client;
				if (client.Active)
				{
					int num = (int)(client.World.Time.Rtt * 1000.0);
					pingText.text = $"{num}ms";
				}
			}
		}

		public void SetLoadingMessage(string message)
		{
			loadingText.text = message ?? "";
		}

		private void DisconnectClicked()
		{
			NetworkManagerNuclearOption.i.Stop(setDisconnectReason: true);
		}
	}
}
