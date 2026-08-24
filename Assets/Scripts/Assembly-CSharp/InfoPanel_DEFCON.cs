using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class InfoPanel_DEFCON : MonoBehaviour
{
	[FormerlySerializedAs("currentlevelIndicator")]
	public GameObject currentLevelIndicator;

	[FormerlySerializedAs("currentlevelLabel")]
	public Text currentLevelLabel;

	private float previousLevel;

	public Text levelTactical;

	public Text levelStrategic;

	public Text levelMAD;

	private void Start()
	{
		levelTactical.text = NetworkSceneSingleton<MissionManager>.i.tacticalThreshold.ToString("N0");
		levelStrategic.text = NetworkSceneSingleton<MissionManager>.i.strategicThreshold.ToString("N0");
		levelMAD.text = (5f * NetworkSceneSingleton<MissionManager>.i.strategicThreshold).ToString("N0");
		UpdateUI(0f);
	}

	private void Update()
	{
		float currentEscalation = NetworkSceneSingleton<MissionManager>.i.currentEscalation;
		if (previousLevel != currentEscalation)
		{
			previousLevel = currentEscalation;
			UpdateUI(currentEscalation);
		}
	}

	private void UpdateUI(float currentLevel)
	{
		float tacticalThreshold = NetworkSceneSingleton<MissionManager>.i.tacticalThreshold;
		float strategicThreshold = NetworkSceneSingleton<MissionManager>.i.strategicThreshold;
		currentLevelLabel.text = currentLevel.ToString("N0");
		float num = -120f;
		num += FastMath.Map(currentLevel, 0f, tacticalThreshold, 0f, 120f);
		num += FastMath.Map(currentLevel, tacticalThreshold, strategicThreshold, 0f, 120f);
		num += FastMath.Map(currentLevel, strategicThreshold, strategicThreshold * 4f, 0f, 120f);
		currentLevelIndicator.transform.localPosition = new Vector3(num, currentLevelIndicator.transform.localPosition.y, currentLevelIndicator.transform.localPosition.z);
	}
}
