using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.Workshop
{
	public class SteamErrorPopup : MonoBehaviour
	{
		[SerializeField]
		private GameObject holder;

		[SerializeField]
		private TextMeshProUGUI titleText;

		[SerializeField]
		private TextMeshProUGUI descriptionText;

		[SerializeField]
		private TextMeshProUGUI buttonText;

		[SerializeField]
		private Button actionButton;

		private Action callback;

		private void Awake()
		{
			actionButton.onClick.AddListener(OnActionButtonClicked);
		}

		public void Show(string title, string description, string buttonLabel, Action callback)
		{
			holder.SetActive(value: true);
			titleText.text = title;
			descriptionText.text = description;
			buttonText.text = buttonLabel;
			this.callback = callback;
		}

		private void OnActionButtonClicked()
		{
			callback?.Invoke();
			callback = null;
			holder.SetActive(value: false);
		}
	}
}
