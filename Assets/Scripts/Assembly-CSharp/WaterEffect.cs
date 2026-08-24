using UnityEngine;

public class WaterEffect : MonoBehaviour
{
	[SerializeField]
	private float lifetime = 120f;

	[SerializeField]
	private bool snapToDatum = true;

	[SerializeField]
	private Unit unit;

	private void Start()
	{
		base.transform.SetParent(Datum.origin);
		if (snapToDatum)
		{
			base.transform.position = new Vector3(base.transform.position.x, Datum.LocalSeaY, base.transform.position.z);
		}
		base.transform.localEulerAngles = new Vector3(0f, base.transform.localEulerAngles.y, 0f);
		if (unit != null)
		{
			unit.onDisableUnit += WaterEffect_OnUnitDisable;
		}
		else
		{
			Object.Destroy(base.gameObject, lifetime);
		}
		base.enabled = false;
	}

	private void WaterEffect_OnUnitDisable(Unit unit)
	{
		unit.onDisableUnit -= WaterEffect_OnUnitDisable;
		if (this != null)
		{
			Object.Destroy(base.gameObject, lifetime);
		}
	}
}
