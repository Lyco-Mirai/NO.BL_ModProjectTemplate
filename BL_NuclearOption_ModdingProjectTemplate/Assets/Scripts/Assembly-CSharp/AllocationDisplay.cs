using NuclearOption.Networking;
using TMPro;
using UnityEngine;

public class AllocationDisplay : SceneSingleton<AllocationDisplay>
{
	[SerializeField]
	private TMP_Text allocationReadout;

	private float lastActivatedTime;

	private bool visible = true;

	protected override void Awake()
	{
		base.Awake();
		base.gameObject.SetActive(value: false);
		base.enabled = false;
	}

	public void SetVisible(bool visible)
	{
		this.visible = visible && PlayerSettings.cinematicMode;
		if (!visible)
		{
			base.gameObject.SetActive(value: false);
			base.enabled = false;
		}
		Debug.Log($"Setting Allocation Display Visibility to {visible}");
	}

	public void Show(Player localPlayer, float change)
	{
		Color color = ((localPlayer.Allocation >= 0f) ? Color.white : Color.red);
		string text = "<color=#" + ColorUtility.ToHtmlStringRGBA(color) + ">";
		string text2 = ((change >= 0f) ? "+" : "-");
		Color color2 = ((change >= 0f) ? (Color.green + Color.white * 0.5f) : (Color.red + Color.green * 0.5f));
		string text3 = "<color=#" + ColorUtility.ToHtmlStringRGBA(color2) + ">";
		allocationReadout.text = text + UnitConverter.ValueReading(localPlayer.Allocation) + "</color> " + text3 + "(" + text2 + UnitConverter.ValueReading(Mathf.Abs(change)) + ")</color>";
		if (visible)
		{
			base.enabled = true;
			base.gameObject.SetActive(value: true);
			lastActivatedTime = Time.timeSinceLevelLoad;
			base.transform.position = SceneSingleton<GameplayUI>.i.topPanelTransform.position;
		}
	}

	private void Update()
	{
		float num = Time.timeSinceLevelLoad - lastActivatedTime;
		if (num > 5f)
		{
			base.transform.position += Vector3.up * 50f * Time.deltaTime;
			if (num > 6f)
			{
				base.enabled = false;
				base.gameObject.SetActive(value: false);
			}
		}
	}
}
