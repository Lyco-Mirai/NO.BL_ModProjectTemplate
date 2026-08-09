using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NuclearOption.UI
{
	public abstract class RightClickDropdownMenuBase : DropdownMenuBase, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
	{
		protected RightClickDropdownMenuTrigger trigger;

		private bool _isMouseOver;

		private float lastShowTime;

		protected override void Awake()
		{
			base.Awake();
		}

		protected virtual void Update()
		{
			if (!(Time.unscaledTime < lastShowTime + 0.2f) && dropdownPanel.activeSelf && (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)) && !_isMouseOver)
			{
				HideMenuAsync().Forget();
			}
		}

		void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
		{
			_isMouseOver = true;
		}

		void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
		{
			_isMouseOver = false;
		}

		public void Show(RightClickDropdownMenuTrigger trigger, Vector2 pos)
		{
			this.trigger = trigger;
			if (dropdownPanel.activeSelf)
			{
				HideMenu();
			}
			ShowMenuAsync(pos).Forget();
			lastShowTime = Time.unscaledTime;
		}
	}
}
