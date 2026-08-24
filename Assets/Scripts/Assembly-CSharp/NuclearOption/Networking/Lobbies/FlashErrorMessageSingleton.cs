using System.Collections.Generic;
using UnityEngine;

namespace NuclearOption.Networking.Lobbies
{
	public class FlashErrorMessageSingleton : MonoBehaviour
	{
		private static FlashErrorMessageSingleton instance;

		[SerializeField]
		private Canvas _canvas;

		[SerializeField]
		private Transform _parent;

		[SerializeField]
		private FlashErrorMessageModal prefab;

		private Stack<FlashErrorMessageModal> pool = new Stack<FlashErrorMessageModal>();

		private Queue<FlashErrorMessageModal> active = new Queue<FlashErrorMessageModal>();

		[SerializeField]
		private int maxMessages = 10;

		[SerializeField]
		private float defaultHideTime = 5f;

		private void Awake()
		{
			if (instance == null)
			{
				instance = this;
				Object.DontDestroyOnLoad(base.gameObject);
			}
			else
			{
				Debug.LogError("2 FlashErrorMessageSingleton existed at once");
			}
		}

		public static void ShowError(string message, float? hideSeconds = null)
		{
			if (instance == null)
			{
				Object.Instantiate(GameAssets.i.flashErrorMessage);
			}
			if (instance != null)
			{
				instance.ShowErrorInternal(message, hideSeconds);
			}
		}

		private void ShowErrorInternal(string message, float? hideSeconds = null)
		{
			FlashErrorMessageModal modal;
			if (active.Count == maxMessages)
			{
				modal = active.Dequeue();
			}
			else if (pool.Count > 0)
			{
				modal = pool.Pop();
			}
			else
			{
				modal = Object.Instantiate(prefab, _parent);
				modal.cancelButton.onClick.AddListener(delegate
				{
					Hide(modal);
				});
			}
			modal.HideTime = Time.timeSinceLevelLoad + (hideSeconds ?? defaultHideTime);
			modal.message.text = message;
			modal.panel.SetActive(value: true);
			modal.transform.SetAsLastSibling();
			active.Enqueue(modal);
			if (!base.enabled)
			{
				base.enabled = true;
				_canvas.gameObject.SetActive(value: true);
			}
		}

		private void Hide(FlashErrorMessageModal modal)
		{
			pool.Push(modal);
			modal.panel.SetActive(value: false);
		}

		private void Update()
		{
			float timeSinceLevelLoad = Time.timeSinceLevelLoad;
			FlashErrorMessageModal result;
			while (active.TryPeek(out result) && timeSinceLevelLoad > result.HideTime)
			{
				active.Dequeue();
				Hide(result);
			}
			if (active.Count == 0)
			{
				base.enabled = false;
				_canvas.gameObject.SetActive(value: false);
			}
		}
	}
}
