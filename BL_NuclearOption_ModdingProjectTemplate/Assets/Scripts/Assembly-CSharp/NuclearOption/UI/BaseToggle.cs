using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace NuclearOption.UI
{
	public abstract class BaseToggle : Selectable, IPointerClickHandler, IEventSystemHandler, ISubmitHandler, ICanvasElement
	{
		[Serializable]
		public class ToggleEvent : UnityEvent<bool>
		{
		}

		public Graphic graphic;

		public ToggleEvent onValueChanged = new ToggleEvent();

		[Tooltip("Is the toggle currently on or off?")]
		[SerializeField]
		protected bool m_IsOn;

		public bool isOn
		{
			get
			{
				return m_IsOn;
			}
			set
			{
				Set(value);
			}
		}

		Transform ICanvasElement.transform => base.transform;

		public virtual void Rebuild(CanvasUpdate executing)
		{
			if (executing == CanvasUpdate.Prelayout)
			{
				onValueChanged.Invoke(m_IsOn);
			}
		}

		public virtual void LayoutComplete()
		{
		}

		public virtual void GraphicUpdateComplete()
		{
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			PlayEffect(instant: true);
		}

		protected override void OnDidApplyAnimationProperties()
		{
			if (graphic != null)
			{
				bool flag = !Mathf.Approximately(graphic.canvasRenderer.GetColor().a, 0f);
				if (m_IsOn != flag)
				{
					m_IsOn = flag;
					Set(!flag);
				}
			}
			base.OnDidApplyAnimationProperties();
		}

		public void SetIsOnWithoutNotify(bool value)
		{
			Set(value, sendCallback: false);
		}

		private void Set(bool value, bool sendCallback = true)
		{
			if (m_IsOn != value)
			{
				m_IsOn = value;
				PlayEffect(instant: false);
				if (sendCallback)
				{
					UISystemProfilerApi.AddMarker("SliderToggle.value", this);
					onValueChanged.Invoke(m_IsOn);
				}
			}
		}

		protected abstract void PlayEffect(bool instant);

		protected override void Start()
		{
			PlayEffect(instant: true);
		}

		private void InternalToggle()
		{
			if (IsActive() && IsInteractable())
			{
				isOn = !isOn;
			}
		}

		public virtual void OnPointerClick(PointerEventData eventData)
		{
			if (eventData.button == PointerEventData.InputButton.Left)
			{
				InternalToggle();
			}
		}

		public virtual void OnSubmit(BaseEventData eventData)
		{
			InternalToggle();
		}
	}
}
