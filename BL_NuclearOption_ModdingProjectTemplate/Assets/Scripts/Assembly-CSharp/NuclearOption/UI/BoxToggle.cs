using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.UI
{
	[AddComponentMenu("UI/BoxToggle", 531)]
	[RequireComponent(typeof(RectTransform))]
	public class BoxToggle : BaseToggle
	{
		[Header("Animation")]
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

		private float colorLerp;

		protected override void PlayEffect(bool instant)
		{
			if (graphic != null)
			{
				if (!Application.isPlaying)
				{
					graphic.canvasRenderer.SetAlpha(m_IsOn ? 1f : 0f);
				}
				else
				{
					graphic.CrossFadeAlpha(m_IsOn ? 1f : 0f, instant ? 0f : 0.1f, ignoreTimeScale: true);
				}
			}
			if (!(background != null))
			{
				return;
			}
			if (instant)
			{
				background.color = (m_IsOn ? onColor : offColor);
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
				float deltaTime = Time.deltaTime;
				MoveTowards(ref colorLerp, colorDuration, deltaTime, targetOn);
				background.color = Color.Lerp(offColor, onColor, EaseInOut(colorLerp));
				if (colorLerp >= 1f)
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
