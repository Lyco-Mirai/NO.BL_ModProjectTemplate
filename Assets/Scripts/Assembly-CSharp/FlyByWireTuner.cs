using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FlyByWireTuner : MonoBehaviour
{
	private Aircraft aircraft;

	private ControlsFilter controlsFilter;

	private ControlsFilter.FlyByWire flyByWire;

	[Header("Script Enabled by DebugVis")]
	[SerializeField]
	private Toggle fbwEnabled;

	[SerializeField]
	private TMP_InputField directControlFactor;

	[SerializeField]
	private TMP_InputField angVel;

	[SerializeField]
	private TMP_InputField cornerSpeed;

	[SerializeField]
	private TMP_InputField psmSpeed;

	[SerializeField]
	private TMP_InputField slowFast;

	[SerializeField]
	private TMP_InputField pitchAdjustLimitSlow;

	[SerializeField]
	private TMP_InputField pFactorSlow;

	[SerializeField]
	private TMP_InputField dFactorSlow;

	[SerializeField]
	private TMP_InputField pitchAdjustLimitFast;

	[SerializeField]
	private TMP_InputField pFactorFast;

	[SerializeField]
	private TMP_InputField dFactorFast;

	[SerializeField]
	private TMP_InputField rollTrimRate;

	[SerializeField]
	private TMP_InputField rollTrimLimit;

	[SerializeField]
	private TMP_InputField yawTightness;

	[SerializeField]
	private TMP_InputField rollTightness;

	[SerializeField]
	private TMP_Text pitchTrim;

	[SerializeField]
	private TMP_Text remapFactor;

	[SerializeField]
	private TMP_Text filteredPitch;

	[SerializeField]
	private TMP_Text pitchAngVel;

	private void Awake()
	{
	}

	private void CheckAircraft()
	{
		if (SceneSingleton<CombatHUD>.i.aircraft != null && SceneSingleton<CombatHUD>.i.aircraft != aircraft)
		{
			aircraft = SceneSingleton<CombatHUD>.i.aircraft;
			controlsFilter = aircraft.GetControlsFilter();
			flyByWire = controlsFilter.GetFlyByWire();
			if (controlsFilter is HeloControlsFilter)
			{
				base.gameObject.SetActive(value: false);
				return;
			}
			base.gameObject.SetActive(value: true);
			GetParameters();
		}
		else if (SceneSingleton<CombatHUD>.i.aircraft == null)
		{
			aircraft = null;
			controlsFilter = null;
			flyByWire = null;
			base.gameObject.SetActive(value: false);
		}
	}

	public void GetParameters()
	{
		if (!(controlsFilter == null))
		{
			(bool, float[]) flyByWireParameters = controlsFilter.GetFlyByWireParameters();
			fbwEnabled.SetIsOnWithoutNotify(flyByWireParameters.Item1);
			float[] item = flyByWireParameters.Item2;
			directControlFactor.text = $"{item[0]:F2}";
			angVel.text = $"{item[1]:F2}";
			cornerSpeed.text = $"{item[2]:F0}";
			psmSpeed.text = $"{item[3]:F0}";
			slowFast.text = $"{item[4]:F0}";
			pitchAdjustLimitSlow.text = $"{item[5]:F2}";
			pFactorSlow.text = $"{item[6]:F3}";
			dFactorSlow.text = $"{item[7]:F3}";
			pitchAdjustLimitFast.text = $"{item[8]:F2}";
			pFactorFast.text = $"{item[9]:F3}";
			dFactorFast.text = $"{item[10]:F3}";
			rollTrimRate.text = $"{item[11]:F2}";
			rollTrimLimit.text = $"{item[12]:F2}";
			yawTightness.text = $"{item[13]:F2}";
			rollTightness.text = $"{item[14]:F2}";
		}
	}

	public void SetParameters()
	{
		if (!(controlsFilter == null))
		{
			float[] parameters = new float[15]
			{
				float.Parse(directControlFactor.text),
				float.Parse(angVel.text),
				float.Parse(cornerSpeed.text),
				float.Parse(psmSpeed.text),
				float.Parse(slowFast.text),
				float.Parse(pitchAdjustLimitSlow.text),
				float.Parse(pFactorSlow.text),
				float.Parse(dFactorSlow.text),
				float.Parse(pitchAdjustLimitFast.text),
				float.Parse(pFactorFast.text),
				float.Parse(dFactorFast.text),
				float.Parse(rollTrimRate.text),
				float.Parse(rollTrimLimit.text),
				float.Parse(yawTightness.text),
				float.Parse(rollTightness.text)
			};
			controlsFilter.SetFlyByWireParameters(fbwEnabled.isOn, parameters);
		}
	}

	private void FixedUpdate()
	{
		if (flyByWire != null)
		{
			pitchTrim.text = $"{flyByWire.GetPitchTrim():F2}";
			remapFactor.text = $"{flyByWire.GetRemapFactor():F2}";
			filteredPitch.text = $"{aircraft.GetInputs().pitch:F2} [{flyByWire.GetRawPitch():F2}]";
			pitchAngVel.text = $"{flyByWire.GetPitchAngVel():F2} [{flyByWire.GetTargetPitchAngVel():F2}]";
		}
	}
}
