using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace NuclearOption.UI
{
	public abstract class DropdownMenuBase : MonoBehaviour
	{
		[Header("Menu Settings")]
		[SerializeField]
		protected GameObject dropdownPanel;

		[SerializeField]
		private CanvasGroup dropdownCanvasGroup;

		[Header("Animation Settings")]
		[SerializeField]
		private float animDuration = 0.15f;

		[Tooltip("how far window slices up/down when showing/hiding")]
		[SerializeField]
		private Vector2 animFromOffset = new Vector2(0f, 10f);

		[SerializeField]
		private Vector2 animToOffset = new Vector2(0f, 0f);

		[SerializeField]
		private bool resetAnchor = true;

		private CancellationTokenSource _cts;

		private RectTransform dropdownRect;

		private Vector2 anchorPos;

		private Vector2 showPos;

		public RectTransform ParentRectTransform => dropdownPanel.transform.parent.AsRectTransform();

		protected virtual void OnShowPanel()
		{
		}

		protected virtual void OnHidePanel()
		{
		}

		protected virtual void Awake()
		{
			dropdownRect = dropdownPanel.transform.AsRectTransform();
			anchorPos = dropdownRect.anchoredPosition;
			dropdownCanvasGroup.alpha = 0f;
			dropdownPanel.SetActive(value: false);
		}

		protected async UniTask ShowMenuAsync()
		{
			dropdownPanel.SetActive(value: true);
			OnShowPanel();
			await AnimateMenu(show: true, null, ResetCancel());
		}

		protected async UniTask ShowMenuAsync(Vector2 targetPos)
		{
			dropdownPanel.SetActive(value: true);
			OnShowPanel();
			await AnimateMenu(show: true, targetPos, ResetCancel());
		}

		protected async UniTaskVoid HideMenuAsync()
		{
			await HideMenuAsync(ResetCancel());
		}

		protected async UniTask HideMenuAsync(CancellationToken token)
		{
			await AnimateMenu(show: false, null, token);
			if (!token.IsCancellationRequested)
			{
				OnHidePanel();
				dropdownPanel.SetActive(value: false);
			}
		}

		protected async UniTask AnimateMenu(bool show, Vector2? _targetPos, CancellationToken token)
		{
			float fromAlpha = ((!show) ? 1 : 0);
			float toAlpha = (show ? 1 : 0);
			if (resetAnchor)
			{
				dropdownRect.anchoredPosition = anchorPos;
			}
			if (show)
			{
				showPos = _targetPos ?? ((Vector2)dropdownRect.position);
			}
			Vector2 vector = showPos;
			Vector2 from = vector + (show ? animFromOffset : animToOffset);
			Vector2 to = vector + (show ? animToOffset : animFromOffset);
			dropdownRect.position = from;
			float elapsed = Mathf.InverseLerp(fromAlpha, toAlpha, dropdownCanvasGroup.alpha);
			while (elapsed < animDuration)
			{
				elapsed += Time.unscaledDeltaTime;
				float num = elapsed / animDuration;
				float t = 1f - (1f - num) * (1f - num);
				dropdownCanvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, t);
				dropdownRect.position = Vector2.Lerp(from, to, t);
				await UniTask.Yield(PlayerLoopTiming.Update);
				if (token.IsCancellationRequested)
				{
					return;
				}
			}
			dropdownCanvasGroup.alpha = toAlpha;
			if (resetAnchor)
			{
				dropdownRect.anchoredPosition = anchorPos;
			}
			dropdownRect.position = to;
		}

		protected CancellationToken ResetCancel()
		{
			CancelTask();
			_cts = new CancellationTokenSource();
			return _cts.Token;
		}

		private void CancelTask()
		{
			_cts?.Cancel();
			_cts?.Dispose();
			_cts = null;
		}

		protected virtual void OnDestroy()
		{
			CancelTask();
		}

		protected virtual void HideMenu()
		{
			CancelTask();
			dropdownCanvasGroup.alpha = 0f;
			OnHidePanel();
			dropdownPanel.SetActive(value: false);
		}
	}
}
