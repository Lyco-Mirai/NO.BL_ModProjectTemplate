using UnityEngine;
using UnityEngine.UI;

public class JammedMarker : MonoBehaviour
{
	[SerializeField]
	private GameObject vectorLinePrefab;

	private GameObject vectorLine;

	private Image vectorLineImage;

	private Image jammedImage;

	private Text jammedText;

	private Radar radar;

	private Unit unit;

	private Unit jammedBy;

	private UnitMapIcon mapIcon;

	private UnitMapIcon jammedByIcon;

	public void Setup(UnitMapIcon unitIcon, Unit jammedBy, Radar radar)
	{
		unit = unitIcon.unit;
		mapIcon = unitIcon;
		this.radar = radar;
		float num = 1f / SceneSingleton<DynamicMap>.i.mapImage.transform.localScale.x;
		base.transform.localScale = Vector3.one * num;
		base.transform.position = unitIcon.transform.position;
		this.jammedBy = jammedBy;
		jammedImage = GetComponent<Image>();
		jammedText = base.transform.GetComponentInChildren<Text>();
		if (DynamicMap.TryGetMapIcon(jammedBy, out jammedByIcon))
		{
			AddVectorLine();
		}
		unit.onJam += JammedMarker_OnUnitJammed;
	}

	private void AddVectorLine()
	{
		vectorLine = Object.Instantiate(vectorLinePrefab, SceneSingleton<DynamicMap>.i.iconLayer.transform);
		vectorLineImage = vectorLine.GetComponent<Image>();
		vectorLineImage.color = Color.yellow;
	}

	private void Remove()
	{
		if (unit != null)
		{
			unit.onJam -= JammedMarker_OnUnitJammed;
		}
		Object.Destroy(base.gameObject);
		Object.Destroy(vectorLine);
	}

	private void JammedMarker_OnUnitJammed(Unit.JamEventArgs jam)
	{
		jammedBy = jam.jammingUnit;
		if (DynamicMap.TryGetMapIcon(jammedBy, out jammedByIcon) && vectorLine == null)
		{
			AddVectorLine();
		}
	}

	private void Update()
	{
		if (unit == null || mapIcon == null || unit.disabled || radar == null || jammedBy == null || jammedBy.disabled || !radar.IsJammed())
		{
			Remove();
			return;
		}
		if (!SceneSingleton<MapOptions>.i.showJamming)
		{
			jammedImage.enabled = false;
			jammedText.enabled = false;
			if (vectorLine != null && vectorLineImage.enabled)
			{
				vectorLineImage.enabled = false;
			}
			return;
		}
		if (!jammedImage.enabled)
		{
			jammedImage.enabled = true;
			jammedText.enabled = true;
		}
		base.transform.position = mapIcon.transform.position;
		base.transform.localScale = Vector3.one / SceneSingleton<DynamicMap>.i.mapImage.transform.localScale.x;
		if (jammedByIcon != null)
		{
			vectorLineImage.enabled = true;
			vectorLineImage.transform.position = base.transform.position;
			Vector3 vector = jammedByIcon.transform.position - base.transform.position;
			float z = (0f - Mathf.Atan2(vector.x, vector.y)) * 57.29578f;
			vectorLine.transform.eulerAngles = new Vector3(0f, 0f, z);
			vectorLine.transform.localScale = (Vector3.one + Vector3.up * vector.magnitude) / SceneSingleton<DynamicMap>.i.iconLayer.transform.lossyScale.x;
		}
		else if (vectorLineImage != null)
		{
			vectorLineImage.enabled = false;
		}
	}
}
