using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.UI
{
	public class ButtonErrorFeedback : MonoBehaviour
	{
		[SerializeField]
		private MaskableGraphic targetGraphic;

		[SerializeField]
		private Color flashColor = new Color(0.7f, 0.26f, 0.26f);

		[SerializeField]
		private float durationIn = 0.1f;

		[SerializeField]
		private float durationHold = 0.3f;

		[SerializeField]
		private float durationOut = 0.05f;

		private Color _originalColor;

		private bool _isFlashing;

		private void Awake()
		{
			if (targetGraphic != null)
			{
				_originalColor = targetGraphic.color;
			}
		}

		public async UniTaskVoid TriggerError()
		{
			if (_isFlashing || targetGraphic == null)
			{
				return;
			}
			_isFlashing = true;
			CancellationToken cancel = base.destroyCancellationToken;
			try
			{
				await LerpColor(_originalColor, flashColor, durationIn, cancel);
				await UniTask.Delay((int)(durationHold * 1000f), ignoreTimeScale: true);
				if (!cancel.IsCancellationRequested)
				{
					await LerpColor(flashColor, _originalColor, durationOut, cancel);
				}
			}
			finally
			{
				_isFlashing = false;
			}
		}

		private async UniTask LerpColor(Color start, Color end, float time, CancellationToken token)
		{
			float elapsed = 0f;
			while (elapsed < time)
			{
				elapsed += Time.unscaledDeltaTime;
				targetGraphic.color = Color.Lerp(start, end, elapsed / time);
				await UniTask.Yield();
				if (token.IsCancellationRequested)
				{
					return;
				}
			}
			targetGraphic.color = end;
		}
	}
}
