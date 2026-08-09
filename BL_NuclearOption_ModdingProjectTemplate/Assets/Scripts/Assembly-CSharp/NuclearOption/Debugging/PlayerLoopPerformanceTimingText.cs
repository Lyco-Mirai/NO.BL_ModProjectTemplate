using JamesFrowen.ScriptableVariables;
using UnityEngine;

namespace NuclearOption.Debugging
{
	public class PlayerLoopPerformanceTimingText : MonoBehaviour
	{
		[SerializeField]
		protected float _updateInterval = 0.2f;

		[Header("References")]
		[SerializeField]
		private NonAllocGui.Wrapper _updateTimeText;

		[SerializeField]
		private NonAllocGui.Wrapper _fixedUpdateTimeText;

		[SerializeField]
		private NonAllocGui.Wrapper _lateUpdateTimeText;

		[SerializeField]
		private NonAllocGui.Wrapper _receiveTimeText;

		[SerializeField]
		private NonAllocGui.Wrapper _sendTimeText;

		private float _updateTimer;

		private int _count;

		private float _updateTimeSum;

		private float _fixedUpdateTimeSum;

		private float _lateUpdateTimeSum;

		private float _receiveTimeSum;

		private float _sendTimeSum;

		private void OnEnable()
		{
			PlayerLoopPerformanceTracker.OnFrameFinished += OnFrameFinished;
		}

		private void OnDisable()
		{
			PlayerLoopPerformanceTracker.OnFrameFinished -= OnFrameFinished;
		}

		private void OnFrameFinished(FrameTiming timing)
		{
			_updateTimeSum += timing.UpdateTime;
			_fixedUpdateTimeSum += timing.FixedUpdateTime;
			_lateUpdateTimeSum += timing.LateUpdateTime;
			_receiveTimeSum += timing.ReceiveTime;
			_sendTimeSum += timing.SendTime;
			_count++;
		}

		public void Update()
		{
			_updateTimer += Time.unscaledDeltaTime;
			if (_updateTimer > _updateInterval)
			{
				_updateTimer = 0f;
				UpdateText();
				_updateTimeSum = 0f;
				_fixedUpdateTimeSum = 0f;
				_lateUpdateTimeSum = 0f;
				_receiveTimeSum = 0f;
				_sendTimeSum = 0f;
				_count = 0;
			}
		}

		private void UpdateText()
		{
			if (_count != 0)
			{
				_updateTimeText.SetValue(_updateTimeSum / (float)_count);
				_fixedUpdateTimeText.SetValue(_fixedUpdateTimeSum / (float)_count);
				_lateUpdateTimeText.SetValue(_lateUpdateTimeSum / (float)_count);
				_receiveTimeText.SetValue(_receiveTimeSum / (float)_count);
				_sendTimeText.SetValue(_sendTimeSum / (float)_count);
			}
		}
	}
}
