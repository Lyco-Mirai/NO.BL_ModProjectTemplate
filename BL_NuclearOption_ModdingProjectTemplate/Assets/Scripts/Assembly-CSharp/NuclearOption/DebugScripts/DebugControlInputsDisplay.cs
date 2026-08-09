using UnityEngine;

namespace NuclearOption.DebugScripts
{
	public class DebugControlInputsDisplay : MonoBehaviour
	{
		[Header("Settings")]
		[SerializeField]
		private float width = 200f;

		[SerializeField]
		private float height = 20f;

		[SerializeField]
		private float spacing = 2f;

		[SerializeField]
		private Color positiveFillColor = Color.green;

		[SerializeField]
		private Color negativeFillColor = Color.red;

		[SerializeField]
		private Gradient percentColor;

		[Header("References")]
		[SerializeField]
		private GameObject holder;

		[SerializeField]
		private GameObject layout;

		[SerializeField]
		private DebugControlInputsDisplayRow template;

		private DebugControlInputsDisplayRow[] all;

		private ControlInputs controlInputs;

		private Pilot pilot;

		public void Setup(Pilot pilot)
		{
			controlInputs = pilot.aircraft.GetInputs();
			this.pilot = pilot;
			all = new DebugControlInputsDisplayRow[8];
			for (int i = 0; i < 8; i++)
			{
				all[i] = Object.Instantiate(template, layout.transform);
				all[i].gameObject.SetActive(value: true);
			}
			template.gameObject.SetActive(value: false);
			all[0].Label.text = "Stuck Low Speed";
			all[1].Label.text = "Stuck Yaw Steering";
			all[2].Label.text = "Pitch";
			all[3].Label.text = "Roll";
			all[4].Label.text = "Yaw";
			all[5].Label.text = "Throttle";
			all[6].Label.text = "Brake";
			all[7].Label.text = "Custom Axis 1";
		}

		private void LateUpdate()
		{
			float yPosition = 0f;
			if (pilot.currentState is AIPilotTaxiState aIPilotTaxiState)
			{
				all[0].gameObject.SetActive(value: true);
				all[1].gameObject.SetActive(value: true);
				UpdatePercent(all[0], aIPilotTaxiState.stuckTimerSpeedPercent, ref yPosition);
				UpdatePercent(all[1], aIPilotTaxiState.stuckTimerYawPercent, ref yPosition);
			}
			else
			{
				all[0].gameObject.SetActive(value: false);
				all[1].gameObject.SetActive(value: false);
			}
			UpdatePlusMinus(all[2], controlInputs.pitch, ref yPosition);
			UpdatePlusMinus(all[3], controlInputs.roll, ref yPosition);
			UpdatePlusMinus(all[4], controlInputs.yaw, ref yPosition);
			UpdatePlusMinus(all[5], controlInputs.throttle, ref yPosition);
			UpdatePlusMinus(all[6], controlInputs.brake, ref yPosition);
			UpdatePlusMinus(all[7], controlInputs.customAxis1, ref yPosition);
			((RectTransform)layout.transform).sizeDelta = new Vector2(width, spacing - yPosition);
		}

		private void UpdatePlusMinus(DebugControlInputsDisplayRow row, float value, ref float yPosition)
		{
			value = Mathf.Clamp(value, -1.5f, 1.5f);
			((RectTransform)row.transform).anchoredPosition = new Vector2(0f, yPosition);
			yPosition -= height + spacing;
			RectTransform rectTransform = row.Image.rectTransform;
			float num = Mathf.Abs(value) * (width / 2f);
			rectTransform.sizeDelta = new Vector2(num, height);
			if (value > 0f)
			{
				rectTransform.anchoredPosition = new Vector2(0f, 0f);
			}
			else
			{
				rectTransform.anchoredPosition = new Vector2(0f - num, 0f);
			}
			row.Image.color = ((value > 0f) ? positiveFillColor : negativeFillColor);
		}

		private void UpdatePercent(DebugControlInputsDisplayRow row, float percent, ref float yPosition)
		{
			percent = Mathf.Clamp01(percent);
			((RectTransform)row.transform).anchoredPosition = new Vector2(0f, yPosition);
			yPosition -= height + spacing;
			RectTransform rectTransform = row.Image.rectTransform;
			float x = percent * width;
			rectTransform.sizeDelta = new Vector2(x, height);
			rectTransform.anchoredPosition = new Vector2((0f - width) / 2f, 0f);
			row.Image.color = percentColor.Evaluate(percent);
		}
	}
}
