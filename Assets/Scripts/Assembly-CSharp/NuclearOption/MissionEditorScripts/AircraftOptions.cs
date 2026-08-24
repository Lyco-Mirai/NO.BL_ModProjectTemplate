using System.Collections.Generic;
using System.Linq;
using NuclearOption.MissionEditorScripts.MultiSelect;
using NuclearOption.SavedMission;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts
{
	public class AircraftOptions : UnitPanelOptions
	{
		[Header("Player Controlled")]
		[SerializeField]
		private Toggle playerControlledToggle;

		[SerializeField]
		private GameObject playerControlledToggleDifferentValue;

		[SerializeField]
		private GameObject priorityHolder;

		[SerializeField]
		private TMP_InputField priorityInput;

		[SerializeField]
		private GameObject playerControlledWarning;

		[Header("Speed")]
		[SerializeField]
		private TextMeshProUGUI altitudeValue;

		[Space]
		[SerializeField]
		private Slider airspeedSlider;

		[SerializeField]
		private TextMeshProUGUI airspeedHandleLabel;

		[SerializeField]
		private GameObject airspeedDifferentValue;

		[Header("Aircraft")]
		[SerializeField]
		private Slider skillSlider;

		[SerializeField]
		private TextMeshProUGUI skillLabel;

		[SerializeField]
		private Slider braverySlider;

		[SerializeField]
		private TextMeshProUGUI braveryLabel;

		[SerializeField]
		private Slider fuelSlider;

		[SerializeField]
		private TextMeshProUGUI fuelLabel;

		[Header("Livery")]
		[SerializeField]
		private GameObject typeDifferentValueLivery;

		[SerializeField]
		private GameObject liveryDifferentFactionWarning;

		[SerializeField]
		private TMP_Dropdown liverySelection;

		[Header("Weapon")]
		[SerializeField]
		private GameObject typeDifferentValueWeapons;

		[SerializeField]
		private RectTransform loadoutPanel;

		[SerializeField]
		private WeaponSelector weaponSelectorPrefab;

		private readonly List<WeaponSelector> weaponSelectors = new List<WeaponSelector>();

		private readonly List<(LiveryKey key, string label)> liveryOptions = new List<(LiveryKey, string)>();

		private float maxAirSpeed = 300f;

		private readonly List<ValueWrapperGlobalPosition> positionWrappers = new List<ValueWrapperGlobalPosition>();

		public override void Cleanup()
		{
			targets.RemoveChanged(playerControlledToggle);
			targets.RemoveChanged(priorityInput);
			targets.RemoveChanged(airspeedSlider);
			targets.RemoveChanged(fuelSlider);
			targets.RemoveChanged(skillSlider);
			targets.RemoveChanged(braverySlider);
			foreach (ValueWrapperGlobalPosition positionWrapper in positionWrappers)
			{
				positionWrapper.UnregisterOnChange(this);
			}
			positionWrappers.Clear();
			DestroyWeaponSelectors();
		}

		private void DestroyWeaponSelectors()
		{
			foreach (WeaponSelector weaponSelector in weaponSelectors)
			{
				if (weaponSelector != null)
				{
					weaponSelector.gameObject.SetActive(value: false);
					Object.Destroy(weaponSelector.gameObject);
				}
			}
			weaponSelectors.Clear();
		}

		protected override void SetupInner()
		{
			liverySelection.onValueChanged.AddListener(LiveryChanged);
			targets.SetupToggle(playerControlledToggle, playerControlledToggleDifferentValue, (SavedUnit x) => ref ((SavedAircraft)x).playerControlled);
			playerControlledToggle.onValueChanged.AddListener(SetPlayerControlled);
			priorityInput.contentType = TMP_InputField.ContentType.IntegerNumber;
			targets.SetupInputField(priorityInput, (SavedUnit x) => ref ((SavedAircraft)x).playerControlledPriority, (int v) => v.ToString(), (string v) => int.Parse(v));
			targets.SetupSlider(airspeedSlider, airspeedHandleLabel, (SavedUnit x) => ref ((SavedAircraft)x).startingSpeed, (float v) => Mathf.Sqrt(v / maxAirSpeed), (float v) => v * v * maxAirSpeed, (float v) => UnitConverter.SpeedReading(v));
			targets.SetupSlider(fuelSlider, fuelLabel, (SavedUnit x) => ref ((SavedAircraft)x).fuel, (float v) => $"{v * 100f:F0}%");
			if (!playerControlledToggle.isOn)
			{
				targets.SetupSlider(skillSlider, skillLabel, (SavedUnit x) => ref ((SavedAircraft)x).skill, (float v) => $"{v:F1}");
				targets.SetupSlider(braverySlider, braveryLabel, (SavedUnit x) => ref ((SavedAircraft)x).bravery, (float v) => $"{v:F1}");
			}
		}

		private void AnyPositionChanged()
		{
			bool flag = true;
			float num = 0f;
			float num2 = GetAltitude(targets.Targets[0]);
			num += num2 / (float)targets.Targets.Count;
			for (int i = 1; i < targets.Targets.Count; i++)
			{
				float num3 = GetAltitude(targets.Targets[i]);
				num += num3 / (float)targets.Targets.Count;
				if (num2 != num3)
				{
					flag = false;
				}
			}
			if (flag)
			{
				altitudeValue.text = UnitConverter.AltitudeReading(num);
			}
			else
			{
				altitudeValue.text = "(Avg) " + UnitConverter.AltitudeReading(num);
			}
			static float GetAltitude(SavedUnit target)
			{
				GlobalPosition globalPosition = target.globalPosition;
				RaycastHit hit;
				float num4 = (PathfindingAgent.RaycastTerrain(globalPosition, out hit) ? hit.point.GlobalY() : 0f);
				Vector3 spawnOffset = target.Unit.definition.spawnOffset;
				return globalPosition.y - num4 - spawnOffset.y;
			}
		}

		public override void OnTargetsChanged()
		{
			foreach (ValueWrapperGlobalPosition positionWrapper2 in positionWrappers)
			{
				positionWrapper2.UnregisterOnChange(this);
			}
			positionWrappers.Clear();
			foreach (SavedUnit target in targets.Targets)
			{
				ValueWrapperGlobalPosition positionWrapper = target.PositionWrapper;
				positionWrappers.Add(positionWrapper);
				positionWrapper.RegisterOnChange(this, AnyPositionChanged);
				AnyPositionChanged();
			}
			float sameValue;
			bool flag = targets.TryGetSameValue((SavedUnit x) => ((Aircraft)x.Unit).GetAircraftParameters().maxSpeed, out sameValue);
			airspeedDifferentValue.SetActive(!flag);
			if (flag)
			{
				maxAirSpeed = sameValue;
			}
			SetPlayerControlled(targets.GetSameValueOrDefault((SavedUnit x) => ((SavedAircraft)x).playerControlled, defaultValue: false));
			bool flag2 = targets.AllTheSame((SavedUnit x) => x.type);
			typeDifferentValueLivery.SetActive(!flag2);
			typeDifferentValueWeapons.SetActive(!flag2);
			if (flag2)
			{
				_ = targets.Targets[0].type;
				SetupLiveryOptions((SavedAircraft)targets.Targets[0]);
				SetupWeaponOptions((SavedAircraft)targets.Targets[0]);
			}
			FixLayout.RebuildRootEndOfFrame(base.gameObject);
		}

		private void SetPlayerControlled(bool isOn)
		{
			priorityHolder.SetActive(isOn);
			playerControlledWarning.SetActive(isOn && targets.Any((SavedUnit x) => FactionHelper.EmptyOrNoFactionOrNeutral(x.faction)));
			skillSlider.transform.parent.gameObject.SetActive(!isOn);
			braverySlider.transform.parent.gameObject.SetActive(!isOn);
			FixLayout.RebuildRoot(base.gameObject);
		}

		private void SetupLiveryOptions(SavedAircraft first)
		{
			bool flag = targets.AllTheSame((SavedUnit x) => x.faction);
			liveryDifferentFactionWarning.SetActive(!flag);
			AircraftDefinition aircraft = (AircraftDefinition)first.Unit.definition;
			string aircraftFaction = first.faction ?? null;
			LoadoutSelector.GetLiveryOptions(liveryOptions, aircraft, aircraftFaction, flag);
			liverySelection.ClearOptions();
			foreach (var liveryOption in liveryOptions)
			{
				liverySelection.options.Add(new TMP_Dropdown.OptionData(liveryOption.label));
			}
			LiveryKey sameLivery;
			int valueWithoutNotify = (targets.TryGetSameValue((SavedUnit x) => ((SavedAircraft)x).liveryKey, out sameLivery) ? liveryOptions.FindIndex(((LiveryKey key, string label) v) => v.key.Equals(sameLivery)) : (-1));
			liverySelection.SetValueWithoutNotify(valueWithoutNotify);
			liverySelection.RefreshShownValue();
		}

		private void LiveryChanged(int arg0)
		{
			(LiveryKey, string) tuple = liveryOptions[liverySelection.value];
			foreach (SavedUnit target in targets.Targets)
			{
				((Aircraft)target.Unit).SetLiveryKey(tuple.Item1, loadIfUnspawned: true);
				((SavedAircraft)target).liveryKey = tuple.Item1;
			}
		}

		private void SetupWeaponOptions(SavedAircraft first)
		{
			DestroyWeaponSelectors();
			WeaponManager weaponManager = ((Aircraft)first.Unit).weaponManager;
			AircraftParameters aircraftParameters = ((AircraftDefinition)first.Unit.definition).aircraftParameters;
			foreach (SavedAircraft target in targets.Targets)
			{
				CheckSavedLoadoutLength(ref target.savedLoadout, weaponManager, aircraftParameters);
			}
			for (int i = 0; i < weaponManager.hardpointSets.Length; i++)
			{
				HardpointSet hardpointSet = weaponManager.hardpointSets[i];
				WeaponSelector weaponSelector = Object.Instantiate(weaponSelectorPrefab, loadoutPanel);
				int hardpointIndex = i;
				SavedLoadout.SelectedMount? sameValueOrDefault = targets.GetSameValueOrDefault((MultiSelect<SavedUnit>.GetField<SavedLoadout.SelectedMount?>)((SavedUnit saved) => ((SavedAircraft)saved).savedLoadout.Selected[hardpointIndex]), (SavedLoadout.SelectedMount?)null);
				weaponSelector.Initialize(hardpointSet, sameValueOrDefault);
				weaponSelector.OnWeaponSelected += delegate(WeaponMount value)
				{
					WeaponSelected(hardpointIndex, value);
				};
				weaponSelectors.Add(weaponSelector);
			}
			RebuildAircraftLoadout();
		}

		public static void CheckSavedLoadoutLength(ref SavedLoadout loadout, WeaponManager weaponManager, AircraftParameters aircraftParameters)
		{
			if (loadout == null)
			{
				loadout = new SavedLoadout();
			}
			List<SavedLoadout.SelectedMount> selected = loadout.Selected;
			while (selected.Count > weaponManager.hardpointSets.Length)
			{
				selected.RemoveAt(selected.Count - 1);
			}
			List<WeaponMount> list = aircraftParameters.StandardLoadouts.FirstOrDefault()?.loadout.weapons;
			if (list != null && list.Count != weaponManager.hardpointSets.Length)
			{
				Debug.LogError("standardWeapons did not have the same length as hardpointSets");
			}
			while (selected.Count < weaponManager.hardpointSets.Length)
			{
				int nextIndex = selected.Count;
				WeaponMount weaponMount;
				if (list != null)
				{
					weaponMount = ((nextIndex < list.Count) ? list[nextIndex] : null);
				}
				else
				{
					HardpointSet hardpointSet = weaponManager.hardpointSets[nextIndex];
					weaponMount = ((!hardpointSet.precludingHardpointSets.Any((byte x) => x < nextIndex)) ? ((hardpointSet.weaponOptions.Count >= 2) ? hardpointSet.weaponOptions[1] : null) : null);
				}
				selected.Add(new SavedLoadout.SelectedMount
				{
					Key = ((weaponMount != null) ? weaponMount.jsonKey : "")
				});
			}
		}

		public void WeaponSelected(int hardpointIndex, WeaponMount weaponMount)
		{
			SavedLoadout.SelectedMount selectedMount = new SavedLoadout.SelectedMount
			{
				Key = ((weaponMount != null) ? weaponMount.jsonKey : "")
			};
			Debug.Log($"from option {weaponMount}, Adding {selectedMount} to loadout");
			foreach (SavedAircraft target in targets.Targets)
			{
				target.savedLoadout.Selected[hardpointIndex] = selectedMount;
			}
			RebuildAircraftLoadout();
		}

		public void RebuildAircraftLoadout()
		{
			foreach (SavedUnit target in targets.Targets)
			{
				SavedLoadout savedLoadout = ((SavedAircraft)target).savedLoadout;
				Aircraft aircraft = (Aircraft)target.Unit;
				aircraft.Networkloadout = savedLoadout.CreateLoadout(aircraft.weaponManager);
			}
			Loadout loadout = ((Aircraft)targets.Targets[0].Unit).loadout;
			foreach (WeaponSelector weaponSelector in weaponSelectors)
			{
				weaponSelector.SetInteractable(loadout);
			}
		}
	}
}
