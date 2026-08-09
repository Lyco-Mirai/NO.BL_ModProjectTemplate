using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NuclearOption.UI
{
	public abstract class HoverMenuBase : DropdownMenuBase, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
	{
		[Header("Hover Settings")]
		[SerializeField]
		private float closeDelaySeconds = 0.3f;

		private bool _isMouseOver;

		void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
		{
			_isMouseOver = true;
			ShowMenuAsync().Forget();
		}

		void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
		{
			_isMouseOver = false;
			StartCloseTimer().Forget();
		}

		private async UniTaskVoid StartCloseTimer()
		{
			CancellationToken token = ResetCancel();
			await UniTask.Delay(TimeSpan.FromSeconds(closeDelaySeconds), ignoreTimeScale: true);
			if (!token.IsCancellationRequested && !_isMouseOver)
			{
				await HideMenuAsync(token);
			}
		}

		protected override void HideMenu()
		{
			_isMouseOver = false;
			base.HideMenu();
		}
	}
}
