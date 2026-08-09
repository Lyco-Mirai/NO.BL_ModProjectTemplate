using NuclearOption.UIStyleSystem;
using UnityEngine;
using UnityEngine.UI;

public class AoAIndexer : HUDApp
{
	[SerializeField]
	private Image outline;

	[SerializeField]
	private Image slowArrow;

	[SerializeField]
	private Image fastArrow;

	[SerializeField]
	private Image onSpeedCircle;

	[SerializeField]
	private float targetAoA;

	[SerializeField]
	private float AoARange;

	[SerializeField]
	private GameObject[] hideWhenActive;

	private ControlInputs inputs;

	private Aircraft aircraft;

	public override void Initialize(Aircraft aircraft)
	{
		this.aircraft = aircraft;
		inputs = aircraft.GetInputs();
		AoAIndexer_OnThemeGroupChanged();
		ThemeManager.ThemeGroupChanged += AoAIndexer_OnThemeGroupChanged;
	}

	private void OnDestroy()
	{
		ThemeManager.ThemeGroupChanged -= AoAIndexer_OnThemeGroupChanged;
	}

	public override void Refresh()
	{
		if (aircraft == null)
		{
			return;
		}
		if (aircraft.gearState == LandingGear.GearState.LockedRetracted || aircraft.radarAlt < 1f || inputs.throttle > 0.7f)
		{
			if (outline.enabled)
			{
				outline.enabled = false;
				slowArrow.enabled = false;
				fastArrow.enabled = false;
				onSpeedCircle.enabled = false;
				GameObject[] array = hideWhenActive;
				for (int i = 0; i < array.Length; i++)
				{
					array[i].SetActive(value: true);
				}
			}
			return;
		}
		if (!outline.enabled)
		{
			GameObject[] array = hideWhenActive;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].SetActive(value: false);
			}
			outline.enabled = true;
		}
		Vector3 vector = aircraft.cockpit.transform.InverseTransformDirection(aircraft.cockpit.rb.velocity);
		float num = (Mathf.Atan2(vector.y, vector.z) * -57.29578f - targetAoA) / AoARange;
		slowArrow.enabled = num > 0.3333f;
		fastArrow.enabled = num < -0.3333f;
		onSpeedCircle.enabled = Mathf.Abs(num) < 0.666667f;
	}

	private void AoAIndexer_OnThemeGroupChanged()
	{
		slowArrow.color = ThemeManager.Active.ColorTheme.AllClear;
		onSpeedCircle.color = ThemeManager.Active.ColorTheme.Warning;
		fastArrow.color = ThemeManager.Active.ColorTheme.Alert;
	}
}
