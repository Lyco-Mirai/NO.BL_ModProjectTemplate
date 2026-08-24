using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts
{
	public class MissionEditorFileDropdown : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
	{
		[Header("Controllers")]
		[SerializeField]
		private MissionEditorFileMenu fileMenu;

		[Header("UI Panels")]
		[SerializeField]
		private RectTransform dropdownPanel;

		[SerializeField]
		private CanvasGroup dropdownCanvasGroup;

		[Header("Main Button")]
		[SerializeField]
		private Button fileButton;

		[Header("Sub Buttons")]
		[SerializeField]
		private Button newButton;

		[SerializeField]
		private Button loadButton;

		[SerializeField]
		private Button saveButton;

		[SerializeField]
		private Button settingsButton;

		[Header("UX Settings")]
		[SerializeField]
		private float hideDelay = 0.3f;

		[SerializeField]
		private float transitionDuration = 0.15f;

		[SerializeField]
		private float slideOffset = 10f;

		private float _targetAlpha;

		private Vector2 _originalPos;

		private bool _isPointerOver;

		private CancellationTokenSource _fadeCts;

		private void Awake()
		{
			_originalPos = dropdownPanel.anchoredPosition;
			dropdownCanvasGroup.alpha = 0f;
			dropdownPanel.gameObject.SetActive(value: false);
			fileButton.onClick.AddListener(delegate
			{
				fileMenu.OpenSaveMenu();
				CloseDropdown();
			});
			newButton.onClick.AddListener(delegate
			{
				fileMenu.OpenNewMission();
				CloseDropdown();
			});
			loadButton.onClick.AddListener(delegate
			{
				fileMenu.OpenLoadMenu();
				CloseDropdown();
			});
			saveButton.onClick.AddListener(delegate
			{
				fileMenu.OpenSaveMenu();
				CloseDropdown();
			});
			settingsButton.onClick.AddListener(delegate
			{
				fileMenu.OpenSettings();
				CloseDropdown();
			});
		}

		private void OnDestroy()
		{
			_fadeCts?.Cancel();
			_fadeCts?.Dispose();
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			_isPointerOver = true;
			_fadeCts?.Cancel();
			ShowDropdown();
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			_isPointerOver = false;
			_fadeCts?.Cancel();
			HideDropdownWithDelay().Forget();
		}

		private void ShowDropdown()
		{
			dropdownPanel.gameObject.SetActive(value: true);
			AnimateTransition(1f).Forget();
		}

		private async UniTaskVoid HideDropdownWithDelay()
		{
			_fadeCts = new CancellationTokenSource();
			CancellationToken token = _fadeCts.Token;
			await UniTask.Delay(TimeSpan.FromSeconds(hideDelay));
			if (!token.IsCancellationRequested)
			{
				await AnimateTransitionInternal(0f, token);
				if (!token.IsCancellationRequested && !_isPointerOver)
				{
					dropdownPanel.gameObject.SetActive(value: false);
				}
			}
		}

		private async UniTask AnimateTransition(float targetAlpha)
		{
			_fadeCts?.Cancel();
			_fadeCts = new CancellationTokenSource();
			await AnimateTransitionInternal(targetAlpha, _fadeCts.Token);
		}

		private async UniTask AnimateTransitionInternal(float targetAlpha, CancellationToken token)
		{
			float startAlpha = dropdownCanvasGroup.alpha;
			Vector2 startPos = dropdownPanel.anchoredPosition;
			Vector2 targetPos = ((targetAlpha > 0.5f) ? _originalPos : (_originalPos + new Vector2(0f, slideOffset)));
			float elapsed = 0f;
			while (elapsed < transitionDuration)
			{
				elapsed += Time.deltaTime;
				float num = Mathf.Clamp01(elapsed / transitionDuration);
				float t = num * num * (3f - 2f * num);
				dropdownCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
				dropdownPanel.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
				await UniTask.Yield();
				if (token.IsCancellationRequested)
				{
					return;
				}
			}
			dropdownCanvasGroup.alpha = targetAlpha;
			dropdownPanel.anchoredPosition = targetPos;
		}

		public void CloseDropdown()
		{
			_isPointerOver = false;
			_fadeCts?.Cancel();
			dropdownCanvasGroup.alpha = 0f;
			dropdownPanel.anchoredPosition = _originalPos + new Vector2(0f, slideOffset);
			dropdownPanel.gameObject.SetActive(value: false);
		}
	}
}
