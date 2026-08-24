using TMPro;
using UnityEngine;

public class RearmerDisplay : MonoBehaviour
{
	private enum DisplayMode
	{
		Debug = 0,
		HUD = 1,
		Map = 2
	}

	private DisplayMode displayMode;

	private HUDUnitMarker marker;

	[SerializeField]
	private TMP_Text text;

	[SerializeField]
	private TMP_Text state;

	[SerializeField]
	private TMP_Text availability;

	[SerializeField]
	private TMP_Text targetDisplay;

	[SerializeField]
	private GameObject additionalInfoPanel;

	private Rearmer rearmer;

	private float valueDisplayed;

	private RearmVehicleAI vehicleAI;

	public void Initialize(HUDUnitMarker marker, Unit unit, Rearmer rearmer)
	{
		if (marker != null)
		{
			displayMode = DisplayMode.HUD;
			this.marker = marker;
			unit = marker.unit;
			this.rearmer = rearmer;
		}
		else if (unit != null)
		{
			displayMode = DisplayMode.Debug;
			additionalInfoPanel.SetActive(unit.TryGetComponent<RearmVehicleAI>(out vehicleAI));
			GameManager.GetLocalFaction(out var localFaction);
			bool flag = unit.TryGetComponent<Rearmer>(out this.rearmer) && (localFaction == null || (unit.NetworkHQ != null && localFaction == unit.NetworkHQ.faction));
			base.gameObject.SetActive(flag && !PlayerSettings.cinematicMode);
		}
	}

	private void Update()
	{
		if (displayMode == DisplayMode.HUD && (marker == null || !marker.selected))
		{
			Object.Destroy(base.gameObject);
			return;
		}
		if (rearmer == null)
		{
			vehicleAI = null;
			return;
		}
		if (valueDisplayed != rearmer.Capacity)
		{
			valueDisplayed = rearmer.Capacity;
			text.text = UnitConverter.WeightReading(valueDisplayed);
		}
		if (additionalInfoPanel != null && additionalInfoPanel.activeSelf)
		{
			state.text = vehicleAI.GetStateName();
			availability.text = (rearmer.AvailableForMission ? "Available for Mission" : "Occupied");
			Unit target = vehicleAI.GetTarget();
			targetDisplay.text = ((target != null) ? target.unitName : "No Target");
		}
	}
}
