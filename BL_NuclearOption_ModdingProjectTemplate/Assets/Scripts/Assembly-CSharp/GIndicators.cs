using TMPro;
using UnityEngine;

public class GIndicators : HUDApp
{
	[SerializeField]
	private Aircraft aircraft;

	[SerializeField]
	private TextMeshProUGUI gForceLabel;

	[SerializeField]
	private TextMeshProUGUI gMaxForceLabel;

	private float maxGNumber;

	public override void Initialize(Aircraft aircraft)
	{
		this.aircraft = aircraft;
	}

	public override void RefreshSettings()
	{
		base.RefreshSettings();
		gForceLabel.fontSize = (int)((float)fontSize * fontSizeMultiplier);
		gMaxForceLabel.fontSize = (int)((float)fontSize * fontSizeMultiplier);
	}

	public override void Refresh()
	{
		if (!(aircraft == null))
		{
			float num = Vector3.Dot(aircraft.pilots[0].GetAccel() + Vector3.up, aircraft.transform.up);
			maxGNumber = Mathf.Max(maxGNumber, num);
			gForceLabel.text = $"{num:F1}";
			gMaxForceLabel.text = $"[{maxGNumber:F1}]";
		}
	}
}
