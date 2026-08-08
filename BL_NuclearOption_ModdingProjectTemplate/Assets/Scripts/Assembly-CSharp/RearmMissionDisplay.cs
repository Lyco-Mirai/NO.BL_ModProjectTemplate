using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RearmMissionDisplay : MonoBehaviour
{
	[SerializeField]
	private GameObject rearmLinePrefab;

	[SerializeField]
	private GameObject requesterIconPrefab;

	[SerializeField]
	private List<GameObject> rearmLines;

	[SerializeField]
	private List<Image> requesterIcons;

	private List<FactionHQ> hqToShow = new List<FactionHQ>();

	private FactionHQ localHQ;

	private bool initialized;

	private bool displayActive;

	private void GenerateRequesterIcons()
	{
		int num = 0;
		float num2 = 1f / SceneSingleton<DynamicMap>.i.mapImage.transform.localScale.x;
		foreach (FactionHQ item in hqToShow)
		{
			foreach (Unit item2 in item.RearmMissionController.UnitsNeedingRearm)
			{
				_ = item2;
				num++;
			}
		}
		while (requesterIcons.Count != num)
		{
			if (requesterIcons.Count < num)
			{
				GameObject gameObject = Object.Instantiate(requesterIconPrefab, SceneSingleton<DynamicMap>.i.iconLayer.transform);
				gameObject.transform.localScale = Vector3.one * num2;
				gameObject.SetActive(value: true);
				requesterIcons.Add(gameObject.GetComponent<Image>());
			}
			if (requesterIcons.Count > num)
			{
				List<Image> list = requesterIcons;
				Object.Destroy(list[list.Count - 1]);
				requesterIcons.RemoveAt(requesterIcons.Count - 1);
			}
		}
		int num3 = 0;
		foreach (FactionHQ item3 in hqToShow)
		{
			foreach (Unit item4 in item3.RearmMissionController.UnitsNeedingRearm)
			{
				if (item4 != null)
				{
					if (SceneSingleton<DynamicMap>.i.TryGetIcon(item4, out var icon))
					{
						Image image = requesterIcons[num3];
						image.color = ((item4.GetAmmoLevel() > 0.001f) ? Color.Lerp(Color.yellow, Color.white, 0.5f) : Color.red);
						image.transform.position = icon.transform.position;
					}
					num3++;
				}
			}
		}
	}

	private void GenerateRearmLines()
	{
		int num = 0;
		foreach (FactionHQ item in hqToShow)
		{
			foreach (RearmMissionController.RearmerWithMission rearmersWithMission in item.RearmMissionController.RearmersWithMissions)
			{
				if (rearmersWithMission.RearmerUnit != null && rearmersWithMission.RequesterUnit != null)
				{
					num++;
				}
			}
		}
		while (rearmLines.Count != num)
		{
			if (rearmLines.Count < num)
			{
				GameObject gameObject = Object.Instantiate(rearmLinePrefab, SceneSingleton<DynamicMap>.i.iconLayer.transform);
				gameObject.SetActive(value: true);
				rearmLines.Add(gameObject);
			}
			if (rearmLines.Count > num)
			{
				List<GameObject> list = rearmLines;
				Object.Destroy(list[list.Count - 1]);
				rearmLines.RemoveAt(rearmLines.Count - 1);
			}
		}
		int num2 = 0;
		foreach (FactionHQ item2 in hqToShow)
		{
			foreach (RearmMissionController.RearmerWithMission rearmersWithMission2 in item2.RearmMissionController.RearmersWithMissions)
			{
				if (rearmersWithMission2.RearmerUnit != null && rearmersWithMission2.RequesterUnit != null)
				{
					if (SceneSingleton<DynamicMap>.i.TryGetIcon(rearmersWithMission2.RearmerUnit, out var icon) && SceneSingleton<DynamicMap>.i.TryGetIcon(rearmersWithMission2.RequesterUnit, out var icon2))
					{
						GameObject obj = rearmLines[num2];
						obj.transform.position = icon.transform.position;
						Vector3 vector = icon2.transform.localPosition - icon.transform.localPosition;
						float z = (0f - Mathf.Atan2(vector.x, vector.y)) * 57.29578f;
						obj.transform.eulerAngles = new Vector3(0f, 0f, z);
						obj.transform.localScale = new Vector3(2f, vector.magnitude, 1f);
					}
					num2++;
				}
			}
		}
	}

	private void RearmMissionDisplay_OnChanged()
	{
		Regenerate();
	}

	private void RearmMissonDisplay_OnMapScaleChanged()
	{
		float num = 1f / SceneSingleton<DynamicMap>.i.mapImage.transform.localScale.x;
		foreach (Image requesterIcon in requesterIcons)
		{
			Transform obj = requesterIcon.transform;
			obj.localScale = Vector3.one * num;
			obj.eulerAngles = Vector3.zero;
		}
	}

	public void Regenerate()
	{
		if (displayActive)
		{
			GenerateRequesterIcons();
			GenerateRearmLines();
		}
	}

	private void Start()
	{
		this.StartSlowUpdateDelayed(10f, Regenerate);
		DynamicMap.onMapChanged += RearmMissonDisplay_OnMapScaleChanged;
	}

	private void OnDestroy()
	{
		DynamicMap.onMapChanged -= RearmMissonDisplay_OnMapScaleChanged;
	}

	private void Update()
	{
		GameManager.GetLocalHQ(out var localHq);
		bool flag = DynamicMap.mapMaximized && SceneSingleton<MapOptions>.i.tooltipType == MapOptions.TooltipType.Ammo;
		if (flag != displayActive)
		{
			displayActive = flag;
			foreach (Image requesterIcon in requesterIcons)
			{
				requesterIcon.gameObject.SetActive(displayActive);
			}
			foreach (GameObject rearmLine in rearmLines)
			{
				rearmLine.SetActive(displayActive);
			}
			Regenerate();
		}
		if (!displayActive || (!(localHq != localHQ) && initialized))
		{
			return;
		}
		hqToShow.Clear();
		localHQ = localHq;
		FactionHQ[] sortedHQs = FactionRegistry.GetSortedHQs();
		foreach (FactionHQ factionHQ in sortedHQs)
		{
			factionHQ.RearmMissionController.OnChange -= RearmMissionDisplay_OnChanged;
			if (!GameManager.GetLocalHQ(out var localHq2) || localHq2 == factionHQ)
			{
				hqToShow.Add(factionHQ);
				factionHQ.RearmMissionController.OnChange += RearmMissionDisplay_OnChanged;
			}
		}
		initialized = true;
		Regenerate();
	}
}
