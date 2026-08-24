using UnityEngine;
using UnityEngine.UI;

public class AoABracket : HUDApp
{
	[SerializeField]
	private Image eBracket;

	[SerializeField]
	private float targetAoA;

	[SerializeField]
	private float AoARange;

	private ControlInputs inputs;

	private Aircraft aircraft;

	private void Awake()
	{
	}

	public override void Initialize(Aircraft aircraft)
	{
		this.aircraft = aircraft;
		inputs = aircraft.GetInputs();
	}

	private void OnDestroy()
	{
	}

	public override void Refresh()
	{
		if (aircraft == null)
		{
			return;
		}
		if (aircraft.gearState == LandingGear.GearState.LockedRetracted || aircraft.radarAlt < 1f || inputs.throttle > 0.7f)
		{
			if (eBracket.enabled)
			{
				eBracket.enabled = false;
			}
			return;
		}
		if (!eBracket.enabled)
		{
			eBracket.enabled = true;
		}
		float num = 1080f / SceneSingleton<CameraStateManager>.i.mainCamera.fieldOfView;
		eBracket.transform.localPosition = new Vector3(0f, (0f - targetAoA) * num, 0f);
		eBracket.transform.localScale = Vector3.one * AoARange * num;
	}
}
