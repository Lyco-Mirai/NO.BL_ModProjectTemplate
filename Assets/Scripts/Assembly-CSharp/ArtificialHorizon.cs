using UnityEngine;
using UnityEngine.UI;

public class ArtificialHorizon : HUDApp
{
	[SerializeField]
	private Image attitude_img;

	[SerializeField]
	private Image horizon_img;

	[SerializeField]
	private Image sky_img;

	[SerializeField]
	private Aircraft aircraft;

	public override void Initialize(Aircraft aircraft)
	{
		this.aircraft = aircraft;
	}

	public override void RefreshSettings()
	{
	}

	public override void Refresh()
	{
		if (!(SceneSingleton<CombatHUD>.i.aircraft == null) && !(aircraft == null))
		{
			horizon_img.transform.localEulerAngles = new Vector3(0f, 0f, 0f - SceneSingleton<CombatHUD>.i.aircraft.transform.eulerAngles.z);
			float num = SceneSingleton<CombatHUD>.i.aircraft.transform.eulerAngles.x;
			if (num > 180f)
			{
				num -= 360f;
			}
			horizon_img.fillAmount = Mathf.Clamp(0.5f + num / 180f, 0f, 1f);
			sky_img.fillAmount = Mathf.Clamp(0.5f - num / 180f, 0f, 1f);
		}
	}
}
