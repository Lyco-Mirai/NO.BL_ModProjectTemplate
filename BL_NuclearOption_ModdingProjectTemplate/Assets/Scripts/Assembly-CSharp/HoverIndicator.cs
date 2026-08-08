using UnityEngine;
using UnityEngine.UI;

public class HoverIndicator : HUDApp
{
	[SerializeField]
	private float maxSpeed;

	[SerializeField]
	private float maxLineLength;

	[SerializeField]
	private Transform indicatorLine;

	[SerializeField]
	private Transform indicatorLineAnchor;

	[SerializeField]
	private Transform indicatorLineTip;

	[SerializeField]
	private Image indicatorImage;

	private Aircraft aircraft;

	public override void Initialize(Aircraft aircraft)
	{
		this.aircraft = aircraft;
	}

	public override void Refresh()
	{
		if (aircraft == null)
		{
			return;
		}
		if (aircraft.speed > maxSpeed * (5f / 18f))
		{
			if (indicatorImage.enabled)
			{
				indicatorImage.enabled = false;
			}
			return;
		}
		if (!indicatorImage.enabled)
		{
			indicatorImage.enabled = true;
		}
		float num = Vector3.Dot(aircraft.rb.velocity, -aircraft.transform.forward) * 3.6f;
		float num2 = Vector3.Dot(aircraft.rb.velocity, -aircraft.transform.right) * 3.6f;
		num *= maxLineLength / maxSpeed;
		num2 *= maxLineLength / maxSpeed;
		indicatorLineTip.localPosition = new Vector3(num2, num, 0f);
		float z = (0f - Mathf.Atan2(num2, num)) * 57.29578f + 180f;
		indicatorLine.transform.eulerAngles = new Vector3(0f, 0f, z);
		float magnitude = indicatorLineTip.localPosition.magnitude;
		indicatorLine.transform.localScale = new Vector3(1f, magnitude, 1f);
	}
}
