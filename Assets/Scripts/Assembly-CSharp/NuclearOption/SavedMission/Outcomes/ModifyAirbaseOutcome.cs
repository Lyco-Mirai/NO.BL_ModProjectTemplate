using System.Collections.Generic;
using NuclearOption.MissionEditorScripts;
using NuclearOption.NodeGraph;
using UnityEngine;

namespace NuclearOption.SavedMission.Outcomes
{
	internal class ModifyAirbaseOutcome : Outcome
	{
		private SavedAirbase airbase;

		private ValueWrapperOverride<string> faction = new ValueWrapperOverride<string>();

		private ValueWrapperOverride<bool> disabled = new ValueWrapperOverride<bool>();

		private ValueWrapperOverride<bool> capturable = new ValueWrapperOverride<bool>();

		private ValueWrapperOverride<float> captureDefense = new ValueWrapperOverride<float>();

		public ModifyAirbaseSavedOutcome Saved => (ModifyAirbaseSavedOutcome)SavedOutcome;

		public ModifyAirbaseOutcome(ModifyAirbaseSavedOutcome savedOutcome)
			: base(savedOutcome)
		{
		}

		public override void CopyFrom(Outcome original)
		{
			base.CopyFrom(original);
			ModifyAirbaseOutcome modifyAirbaseOutcome = (ModifyAirbaseOutcome)original;
			airbase = modifyAirbaseOutcome.airbase;
			faction.SetValue(modifyAirbaseOutcome.faction.Value, this);
			disabled.SetValue(modifyAirbaseOutcome.disabled.Value, this);
			capturable.SetValue(modifyAirbaseOutcome.capturable.Value, this);
			captureDefense.SetValue(modifyAirbaseOutcome.captureDefense.Value, this);
		}

		public override void Load(MissionLookups lookups)
		{
			base.Load(lookups);
			if (!string.IsNullOrEmpty(Saved.airbase) && lookups.TryGetAirbaseReference(Saved.airbase, out var savedAirbase))
			{
				airbase = savedAirbase;
			}
			else if (!string.IsNullOrEmpty(Saved.airbase))
			{
				lookups.LoadErrors.AddWarn("'" + Saved.airbase + "' was not found in lookup for SavedAirbase");
			}
			faction.SetValue(Saved.faction, this);
			disabled.SetValue(Saved.disabled, this);
			capturable.SetValue(Saved.capturable, this);
			captureDefense.SetValue(Saved.captureDefense, this);
		}

		public override void Save()
		{
			base.Save();
			Saved.airbase = ((airbase != null) ? airbase.GetNameSavedCheckDestroyed() : "");
			Saved.faction = faction.Value;
			Saved.disabled = disabled.Value;
			Saved.capturable = capturable.Value;
			Saved.captureDefense = captureDefense.Value;
		}

		public override void ReferenceDestroyed(ISaveableReference reference)
		{
			if (airbase.SavedReferenceEquals(reference))
			{
				airbase = null;
			}
		}

		public override void ReferenceReplaced(ISaveableReference oldRef, ISaveableReference newRef)
		{
			if (airbase == oldRef && newRef is SavedAirbase savedAirbase)
			{
				airbase = savedAirbase;
			}
		}

		public override void Complete(Objective completedObjective)
		{
			if (this.airbase == null)
			{
				Debug.LogWarning("No airbase set for ModifyAirbaseOutcome");
				return;
			}
			if (!this.airbase.TryGetAirbase(out var airbase))
			{
				Debug.LogWarning("Airbase '" + this.airbase.UniqueName + "' has not spawned");
				return;
			}
			if (faction.Value.IsOverride)
			{
				FactionHQ factionHQ = FactionRegistry.HqFromName(faction.Value.Value);
				if (factionHQ != null)
				{
					airbase.capture.ForceCapture(factionHQ);
				}
			}
			if (disabled.Value.IsOverride)
			{
				airbase.SavedAirbase.Disabled = disabled.Value.Value;
				airbase.SetDisabled(disabled.Value.Value);
			}
			if (capturable.Value.IsOverride)
			{
				airbase.SavedAirbase.Capturable = capturable.Value.Value;
				airbase.capture.SetCapturable(capturable.Value.Value);
			}
			if (captureDefense.Value.IsOverride)
			{
				airbase.SavedAirbase.CaptureDefense = captureDefense.Value.Value;
			}
		}

		public override void DrawData(DataDrawer drawer)
		{
			ReferenceDataField referenceDataField = drawer.InstantiateWithParent(drawer.Prefabs.ReferenceDataPrefab);
			List<SavedAirbase> list = new List<SavedAirbase>();
			MissionManager.GetAllSavedAirbaseNonAlloc(list);
			referenceDataField.Setup("Airbase", list, airbase, delegate(SavedAirbase v)
			{
				airbase = v;
			});
			drawer.Space(12);
			drawer.DrawOverride("Faction", faction, drawer.Prefabs.FactionDataPrefab);
			drawer.DrawOverride("Disabled", disabled, drawer.Prefabs.BoolFieldPrefab);
			drawer.DrawOverride("Capturable", capturable, drawer.Prefabs.BoolFieldPrefab);
			drawer.DrawOverride("Capture Defense", captureDefense, drawer.Prefabs.FloatFieldPrefab);
		}

		public override void AddPins(GraphNodeData data)
		{
			data.InputElements.Add(new GraphReferenceFieldData
			{
				PinId = new PinId("Airbase"),
				DisplayName = "Airbase",
				GetValue = () => airbase,
				SetValue = delegate(ISaveableReference v)
				{
					airbase = v as SavedAirbase;
				},
				GetOptions = delegate
				{
					List<SavedAirbase> list = new List<SavedAirbase>();
					MissionManager.GetAllSavedAirbaseNonAlloc(list);
					return new List<ISaveableReference>(list);
				}
			});
			data.InputElements.Add(new GraphStringFieldData
			{
				PinId = new PinId("Faction"),
				DisplayName = "Faction",
				ValueWrapper = faction
			});
			data.InputElements.Add(new GraphBoolFieldData
			{
				PinId = new PinId("Disabled"),
				DisplayName = "Disabled",
				ValueWrapper = disabled
			});
			data.InputElements.Add(new GraphBoolFieldData
			{
				PinId = new PinId("Capturable"),
				DisplayName = "Capturable",
				ValueWrapper = capturable
			});
			data.InputElements.Add(new GraphFloatFieldData
			{
				PinId = new PinId("Capture Defense"),
				DisplayName = "Capture Defense",
				ValueWrapper = captureDefense
			});
		}
	}
}
