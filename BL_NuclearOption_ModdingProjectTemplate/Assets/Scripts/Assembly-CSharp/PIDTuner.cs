using UnityEngine;
using UnityEngine.UI;

public class PIDTuner : MonoBehaviour
{
	[SerializeField]
	private Slider pSlider;

	[SerializeField]
	private Slider iSlider;

	[SerializeField]
	private Slider dSlider;

	[SerializeField]
	private Text pValue;

	[SerializeField]
	private Text iValue;

	[SerializeField]
	private Text dValue;

	private PID pid;

	private void OnEnable()
	{
		pSlider.SetValueWithoutNotify(0.1f);
		iSlider.SetValueWithoutNotify(0.01f);
		dSlider.SetValueWithoutNotify(0.1f);
		UpdateValues();
	}

	public void AttachTuner(PID pid)
	{
		this.pid = pid;
	}

	public void UpdateValues()
	{
		pValue.text = pSlider.value.ToString("F2");
		iValue.text = iSlider.value.ToString("F2");
		dValue.text = dSlider.value.ToString("F2");
		if (pid != null)
		{
			pid.SetValues(pSlider.value, iSlider.value, dSlider.value);
		}
	}
}
