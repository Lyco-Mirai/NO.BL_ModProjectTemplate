using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

[Serializable]
public class CountermeasureManager
{
	[Serializable]
	private class CountermeasureStation
	{
		[SerializeField]
		private List<Countermeasure> countermeasures;

		public List<string> threatTypes;

		public string displayName;

		public Sprite icon;

		public int ammo;

		public int maxAmmo;

		public CountermeasureStation(Countermeasure countermeasure)
		{
			countermeasures = new List<Countermeasure> { countermeasure };
			threatTypes = countermeasure.GetThreatTypes();
			displayName = countermeasure.displayName;
			icon = countermeasure.displayImage;
			ammo += countermeasure.ammo;
		}

		public void AddCountermeasure(Countermeasure countermeasure)
		{
			if (threatTypes == null)
			{
				threatTypes = new List<string>();
			}
			foreach (string threatType in countermeasure.GetThreatTypes())
			{
				if (!threatTypes.Contains(threatType))
				{
					threatTypes.Add(threatType);
				}
			}
			countermeasures.Add(countermeasure);
			displayName = countermeasure.displayName;
			icon = countermeasure.displayImage;
			ammo += countermeasure.ammo;
			maxAmmo = ammo;
		}

		public void RemoveCountermeasure(Countermeasure countermeasure, out int countermeasuresRemaining)
		{
			countermeasures.Remove(countermeasure);
			countermeasuresRemaining = countermeasures.Count;
		}

		public void SetActive(Aircraft aircraft)
		{
			CountAmmo(aircraft);
		}

		public float GetAmmoProportion()
		{
			if (maxAmmo == 0)
			{
				return 0f;
			}
			return (float)ammo / (float)maxAmmo;
		}

		public void Rearm(Aircraft aircraft, Unit rearmer)
		{
			foreach (Countermeasure countermeasure in countermeasures)
			{
				countermeasure.Rearm(aircraft, rearmer);
			}
			CountAmmo(aircraft);
			maxAmmo = ammo;
		}

		public void CountAmmo(Aircraft aircraft)
		{
			ammo = 0;
			foreach (Countermeasure countermeasure in countermeasures)
			{
				ammo += countermeasure.ammo;
			}
			if (aircraft == SceneSingleton<CombatHUD>.i.aircraft)
			{
				countermeasures[0].UpdateHUD();
				SceneSingleton<CombatHUD>.i.DisplayCountermeasureAmmo(ammo);
			}
		}

		public Countermeasure GetFirstCountermeasure()
		{
			return countermeasures[0];
		}

		public void Fire(Aircraft aircraft)
		{
			foreach (Countermeasure countermeasure in countermeasures)
			{
				countermeasure.Fire();
				if (!countermeasure.chargeable)
				{
					aircraft.RequestRearm();
				}
			}
			CountAmmo(aircraft);
		}
	}

	[SerializeField]
	private List<CountermeasureStation> countermeasureStations;

	public byte activeIndex;

	[SerializeField]
	private Aircraft aircraft;

	public void RegisterCountermeasure(Countermeasure countermeasure)
	{
		foreach (CountermeasureStation countermeasureStation in countermeasureStations)
		{
			if (countermeasureStation.displayName == countermeasure.displayName)
			{
				countermeasureStation.AddCountermeasure(countermeasure);
				return;
			}
		}
		countermeasureStations.Add(new CountermeasureStation(countermeasure));
		if (GameManager.IsLocalAircraft(aircraft))
		{
			countermeasureStations[0].SetActive(aircraft);
		}
		if (countermeasureStations.Count > 1)
		{
			countermeasureStations.Sort((CountermeasureStation x, CountermeasureStation y) => x.displayName.CompareTo(y.displayName));
		}
	}

	public void DeregisterCountermeasure(Countermeasure countermeasure)
	{
		for (int num = countermeasureStations.Count - 1; num >= 0; num--)
		{
			if (countermeasureStations[num].displayName == countermeasure.displayName)
			{
				countermeasureStations[num].RemoveCountermeasure(countermeasure, out var countermeasuresRemaining);
				if (countermeasuresRemaining == 0)
				{
					countermeasureStations.RemoveAt(num);
				}
				break;
			}
		}
	}

	public void NextCountermeasure()
	{
		activeIndex++;
		if (activeIndex >= countermeasureStations.Count)
		{
			activeIndex = 0;
		}
		aircraft.Countermeasures(active: false, activeIndex);
		countermeasureStations[activeIndex].SetActive(aircraft);
	}

	public void DeployCountermeasure(Aircraft aircraft)
	{
		if (activeIndex != byte.MaxValue)
		{
			countermeasureStations[activeIndex].Fire(aircraft);
		}
	}

	public void UpdateHUD()
	{
		if (countermeasureStations.Count > 0)
		{
			countermeasureStations[activeIndex].SetActive(aircraft);
		}
	}

	public string ChooseCountermeasure(Missile missileThreat)
	{
		string seekerType = missileThreat.GetSeekerType();
		activeIndex = byte.MaxValue;
		for (byte b = 0; b < countermeasureStations.Count; b++)
		{
			if (countermeasureStations[b].threatTypes.Contains(seekerType))
			{
				activeIndex = b;
				break;
			}
		}
		aircraft.Countermeasures(active: false, activeIndex);
		if (activeIndex != byte.MaxValue)
		{
			return seekerType;
		}
		return string.Empty;
	}

	public float GetFlareAmmoProportion()
	{
		if (countermeasureStations.Count == 0)
		{
			return 0f;
		}
		return countermeasureStations[0].GetAmmoProportion();
	}

	public void PopFlares()
	{
		if (countermeasureStations.Count != 0)
		{
			PopSingleFlares().Forget();
		}
	}

	private async UniTask PopSingleFlares()
	{
		Debug.Log("Popping Flares");
		aircraft.Countermeasures(active: true, 0);
		await UniTask.WaitForSeconds(0.1f);
		aircraft.Countermeasures(active: false, 0);
	}

	public void Rearm(RearmEventArgs e)
	{
		foreach (CountermeasureStation countermeasureStation in countermeasureStations)
		{
			countermeasureStation.Rearm(aircraft, e.Rearmer);
		}
	}

	public Countermeasure GetActiveCountermeasure()
	{
		if (countermeasureStations.Count > 0)
		{
			return countermeasureStations[activeIndex].GetFirstCountermeasure();
		}
		return null;
	}
}
