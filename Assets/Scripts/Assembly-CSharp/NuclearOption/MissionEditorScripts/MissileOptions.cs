using System.Collections.Generic;
using System.Linq;
using Mirage;
using NuclearOption.SavedMission;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts
{
	public class MissileOptions : UnitPanelOptions
	{
		[Header("Speed")]
		[SerializeField]
		private float maxSpeed = 1020f;

		[SerializeField]
		private Slider airspeedSlider;

		[SerializeField]
		private TextMeshProUGUI airspeedHandleLabel;

		[SerializeField]
		private TMP_Dropdown targetDropdown;

		[SerializeField]
		private GameObject radarPanel;

		[SerializeField]
		private TMP_Dropdown guidingRadarDropdown;

		private readonly List<(SavedUnit key, string unitName)> targetOptions = new List<(SavedUnit, string)>();

		private readonly List<(SavedUnit key, string unitName)> radarOptions = new List<(SavedUnit, string)>();

		private List<string> targetStrings = new List<string>();

		private List<string> radarStrings = new List<string>();

		protected override void SetupInner()
		{
			SetupTargetList();
			SetupRadarList();
			targetDropdown.onValueChanged.AddListener(TargetChanged);
			guidingRadarDropdown.onValueChanged.AddListener(RadarChanged);
			radarPanel.SetActive(value: false);
			if (targets.Targets[0].Unit.definition.unitPrefab.GetComponent<MissileSeeker>() is SARHSeeker)
			{
				radarPanel.SetActive(value: true);
			}
			targets.SetupSlider(airspeedSlider, airspeedHandleLabel, (SavedUnit x) => ref ((SavedMissile)x).startingSpeed, (float v) => Mathf.Sqrt(v / maxSpeed), (float v) => v * v * maxSpeed, (float v) => UnitConverter.SpeedReading(v));
			targets.SetupDropdown(targetDropdown, targetStrings, (SavedUnit x) => ref ((SavedMissile)x).targetUnitName);
			targets.SetupDropdown(guidingRadarDropdown, radarStrings, (SavedUnit x) => ref ((SavedMissile)x).guidingUnit);
			FixLayout.RebuildRootEndOfFrame(base.gameObject);
		}

		public override void Cleanup()
		{
			targets.RemoveChanged(airspeedSlider);
		}

		public override void OnTargetsChanged()
		{
		}

		public void SetupTargetList()
		{
			targetOptions.Clear();
			targetStrings.Clear();
			using AutoPool<List<SavedUnit>>.Wrapper wrapper = AutoPool<List<SavedUnit>>.Take();
			List<SavedUnit> item = wrapper.Item;
			MissionManager.GetAllSavedUnitsNonAlloc(item, includeBuiltIn: true);
			targetStrings.Add("None");
			foreach (SavedUnit item2 in item)
			{
				if (!targets.Targets.Contains(item2))
				{
					targetOptions.Add((item2, item2.UniqueName));
					targetStrings.Add(item2.UniqueName);
					_ = item2.UniqueName;
				}
			}
		}

		private void TargetChanged(int arg0)
		{
			if (targetDropdown.value == 0)
			{
				foreach (SavedMissile target in targets.Targets)
				{
					target.targetUnitName = "";
				}
				return;
			}
			(SavedUnit, string) tuple = targetOptions[targetDropdown.value - 1];
			foreach (SavedUnit target2 in targets.Targets)
			{
				if (target2 == tuple.Item1)
				{
					((SavedMissile)target2).targetUnitName = "";
				}
				else
				{
					((SavedMissile)target2).targetUnitName = tuple.Item1.UniqueName;
				}
			}
		}

		public void SetupRadarList()
		{
			radarOptions.Clear();
			radarStrings.Clear();
			using AutoPool<List<SavedUnit>>.Wrapper wrapper = AutoPool<List<SavedUnit>>.Take();
			List<SavedUnit> item = wrapper.Item;
			MissionManager.GetAllSavedUnitsNonAlloc(item, includeBuiltIn: false);
			radarStrings.Add("None");
			foreach (SavedUnit item2 in item)
			{
				if (!(item2.faction != targets.Targets[0].faction) && !(item2.Unit.definition.unitPrefab.GetComponentInChildren<Radar>() == null))
				{
					radarOptions.Add((item2, item2.UniqueName));
					radarStrings.Add(item2.UniqueName);
				}
			}
		}

		private void RadarChanged(int arg0)
		{
			if (guidingRadarDropdown.value == 0)
			{
				foreach (SavedMissile target in targets.Targets)
				{
					target.guidingUnit = "";
				}
				return;
			}
			(SavedUnit, string) tuple = radarOptions[guidingRadarDropdown.value - 1];
			foreach (SavedUnit target2 in targets.Targets)
			{
				if (target2.Unit.definition.unitPrefab.GetComponent<MissileSeeker>() is SARHSeeker)
				{
					((SavedMissile)target2).guidingUnit = tuple.Item1.UniqueName;
				}
				else
				{
					((SavedMissile)target2).guidingUnit = "";
				}
			}
		}
	}
}
