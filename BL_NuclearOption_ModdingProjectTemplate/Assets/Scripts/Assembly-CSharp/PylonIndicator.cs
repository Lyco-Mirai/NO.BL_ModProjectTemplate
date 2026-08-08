using System;
using System.Collections;
using System.Collections.Generic;
using NuclearOption.UIStyleSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PylonIndicator : HUDApp
{
	[Serializable]
	private class PylonElementSet
	{
		[SerializeField]
		private Image[] pylonImages;

		[SerializeField]
		private TextMeshProUGUI[] pylonTexts;

		public void Update(Color color, bool keepSat = true)
		{
			Image[] array = pylonImages;
			foreach (Image image in array)
			{
				image.color = (keepSat ? color.WithSat(image.color.GetSat()) : color).WithAlpha(image.color.a);
			}
			TextMeshProUGUI[] array2 = pylonTexts;
			foreach (TextMeshProUGUI textMeshProUGUI in array2)
			{
				textMeshProUGUI.color = (keepSat ? color.WithSat(textMeshProUGUI.color.GetSat()) : color).WithAlpha(textMeshProUGUI.color.a);
			}
		}
	}

	[SerializeField]
	private PylonElementSet[] pylonElementSets;

	private Aircraft attachedAircraft;

	private Coroutine updateCoroutine;

	public void Start()
	{
		GameManager.GetLocalAircraft(out var localAircraft);
		attachedAircraft = localAircraft;
		for (int i = 0; i < pylonElementSets.Length; i++)
		{
			if (!attachedAircraft.weaponManager.HardpointsIndexes.ContainsKey(i))
			{
				pylonElementSets[i].Update(Color.gray, keepSat: false);
			}
		}
		UpdateImages();
		attachedAircraft.weaponManager.OnStationFired += DebouncedUpdate;
		attachedAircraft.OnRearmUnit += DebouncedUpdate;
		ThemeManager.ThemeGroupChanged += UpdateImages;
	}

	private void OnDestroy()
	{
		attachedAircraft.weaponManager.OnStationFired -= DebouncedUpdate;
		attachedAircraft.OnRearmUnit -= DebouncedUpdate;
		ThemeManager.ThemeGroupChanged -= UpdateImages;
		if (updateCoroutine != null)
		{
			StopCoroutine(updateCoroutine);
		}
	}

	private void DebouncedUpdate()
	{
		if (updateCoroutine != null)
		{
			StopCoroutine(updateCoroutine);
		}
		updateCoroutine = StartCoroutine(WaitAndUpdate());
	}

	private IEnumerator WaitAndUpdate()
	{
		yield return new WaitForSeconds(0.5f);
		UpdateImages();
		updateCoroutine = null;
	}

	private void UpdateImages()
	{
		foreach (KeyValuePair<int, List<Weapon>> hardpointsIndex in attachedAircraft.weaponManager.HardpointsIndexes)
		{
			int num = 0;
			foreach (Weapon item in hardpointsIndex.Value)
			{
				num += item.ammo;
			}
			pylonElementSets[hardpointsIndex.Key].Update((num <= 0) ? ThemeManager.Active.ColorTheme.Alert : ThemeManager.Active.ColorTheme.AllClear);
		}
	}
}
