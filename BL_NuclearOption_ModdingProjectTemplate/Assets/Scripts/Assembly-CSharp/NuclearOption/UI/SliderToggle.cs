using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.UI
{
	[AddComponentMenu("UI/SliderToggle", 530)]
	[RequireComponent(typeof(RectTransform))]
	public class SliderToggle : BaseToggle
	{
		[Header("Animation")]
		[SerializeField]
		private RectTransform handle;

		[SerializeField]
		private Vector2 onPosition;

		[SerializeField]
		private Vector2 offPosition;

		[SerializeField]
		private float moveDuration;

		[SerializeField]
		private Image background;

		[SerializeField]
		private Color onColor;

		[SerializeField]
		private Color offColor;

		[SerializeField]
		private float colorDuration;

		private bool isRunning;

		private bool targetOn;

		private float moveLerp;

		private float colorLerp;

		protected override void PlayEffect(bool instant)
		{
			if (handle == null || background == null)
			{
				return;
			}
			if (instant)
			{
				handle.anchoredPosition = (base.isOn ? onPosition : offPosition);
				background.color = (base.isOn ? onColor : offColor);
				return;
			}
			targetOn = m_IsOn;
			if (!isRunning)
			{
				TransitionTask().Forget();
			}
		}

		private async UniTask TransitionTask()
		{
			isRunning = true;
			CancellationToken cancel = base.destroyCancellationToken;
			while (!cancel.IsCancellationRequested)
			{
				float unscaledDeltaTime = Time.unscaledDeltaTime;
				MoveTowards(ref moveLerp, moveDuration, unscaledDeltaTime, targetOn);
				MoveTowards(ref colorLerp, colorDuration, unscaledDeltaTime, targetOn);
				handle.anchoredPosition = Vector2.Lerp(offPosition, onPosition, EaseInOut(moveLerp));
				background.color = Color.Lerp(offColor, onColor, EaseInOut(colorLerp));
				if (moveLerp >= 1f && colorLerp >= 1f)
				{
					break;
				}
				await UniTask.Yield();
			}
			isRunning = false;
		}

		private static void MoveTowards(ref float value, float duration, float dt, bool target)
		{
			float num = ((duration > 0f) ? (dt / duration) : 1f);
			if (!target)
			{
				num *= -1f;
			}
			value = Mathf.Clamp01(value + num);
		}

		public static float EaseInOut(float t)
		{
			return t * t * (3f - 2f * t);
		}
	}
}
