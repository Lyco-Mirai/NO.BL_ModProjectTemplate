using System;
using System.Collections.Generic;
using System.Linq;
using NuclearOption.MissionEditorScripts;
using NuclearOption.MissionEditorScripts.ObjectiveGraph;
using NuclearOption.NodeGraph;
using UnityEngine;

namespace NuclearOption.SavedMission
{
	public abstract class Objective : ISaveableReference, IHasFaction
	{
		public readonly SavedObjective SavedObjective;

		public readonly ValueWrapperString FactionWrapper = new ValueWrapperString();

		public ObjectiveStatus Status;

		private FactionHQ hq;

		public readonly List<Outcome> Outcomes = new List<Outcome>();

		public MissionManager MissionManager;

		private object factionPinOwner;

		public virtual float CompletePercent => 0f;

		public virtual string FactionLabelOverride => null;

		bool ISaveableReference.CanBeSorted => true;

		public FactionHQ FactionHQ
		{
			get
			{
				_ = hq;
				return hq;
			}
			set
			{
				hq = value;
			}
		}

		string IHasFaction.FactionName => SavedObjective.Faction;

		string ISaveableReference.UniqueName => SavedObjective.UniqueName;

		bool ISaveableReference.Destroyed { get; set; }

		bool ISaveableReference.CanBeReference => SavedObjective.UniqueName != MissionObjectivesFactory.MissionStartName;

		public virtual bool NeedsFaction => false;

		public event RenamedDelegate OnRenamed;

		public Objective(SavedObjective savedObjective)
		{
			SavedObjective = savedObjective ?? throw new ArgumentNullException("savedObjective");
			FactionWrapper.RegisterOnChange(this, delegate(string name)
			{
				SavedObjective.Faction = name;
				hq = FactionRegistry.HqFromName(name);
			});
			hq = FactionRegistry.HqFromName(SavedObjective.Faction);
			FactionWrapper.SetValue(SavedObjective.Faction, this);
		}

		public virtual IObjectiveEditorUpdate CreateEditorUpdate(Canvas canvas, UIPrefabs prefabs)
		{
			return null;
		}

		public void Rename(string newName)
		{
			if (!(SavedObjective.UniqueName == newName))
			{
				string uniqueName = SavedObjective.UniqueName;
				SavedObjective.UniqueName = newName;
				this.OnRenamed?.Invoke(this, uniqueName, newName);
			}
		}

		public abstract void OnStart();

		public virtual void Cleanup()
		{
		}

		public abstract bool UpdateAndCheck();

		public abstract void ClientOnlyUpdate();

		public virtual void CopyFrom(Objective original)
		{
			SavedObjective.DisplayName = original.SavedObjective.DisplayName;
			SavedObjective.Faction = original.SavedObjective.Faction;
			SavedObjective.Hidden = original.SavedObjective.Hidden;
			FactionWrapper.SetValue(SavedObjective.Faction, this);
		}

		public virtual void Load(MissionLookups lookups)
		{
			FactionWrapper.SetValue(SavedObjective.Faction, this);
			LoadOutcomes(lookups.Outcomes);
		}

		public virtual void Save()
		{
			SavedObjective.Faction = FactionWrapper.Value;
			SaveOutcomes();
		}

		private void LoadOutcomes(Dictionary<string, Outcome> outcomeLookup)
		{
			if (SavedObjective.Outcomes != null)
			{
				Outcomes.AddRange(SavedObjective.Outcomes.Select((string x) => outcomeLookup[x]));
			}
		}

		private void SaveOutcomes()
		{
			SavedObjective.Outcomes = Outcomes.Select((Outcome x) => x.SavedOutcome.UniqueName).ToList();
		}

		public void ReferenceDestroyed(ISaveableReference reference)
		{
			Outcomes.RemoveAll((Outcome x) => x.SavedReferenceEquals(reference));
			DataReferenceDestroyed(reference);
		}

		protected abstract void DataReferenceDestroyed(ISaveableReference reference);

		public virtual void ReferenceReplaced(ISaveableReference oldRef, ISaveableReference newRef)
		{
		}

		public void Complete()
		{
			foreach (Outcome outcome in Outcomes)
			{
				outcome.Complete(this);
			}
		}

		public override string ToString()
		{
			string text = ((FactionHQ != null) ? FactionHQ.faction.factionName.AddColor(FactionHQ.faction.color) : "None");
			return $"[{SavedObjective.UniqueName} Type={SavedObjective.ObjectiveTypeEnum} Faction={text} Hidden={SavedObjective.Hidden}]";
		}

		public abstract void DrawData(DataDrawer drawer);

		public string ToUIString(bool oneLine = false)
		{
			SavedObjective savedObjective = SavedObjective;
			string text = (string.IsNullOrEmpty(savedObjective.DisplayName) ? "<no name>".AddColor(new Color(0.5f, 0.5f, 0.5f)) : savedObjective.DisplayName);
			string text2 = savedObjective.UniqueName.AddColor(new Color(0.7f, 0.7f, 0.7f));
			string text3 = savedObjective.ObjectiveTypeEnum.ToString();
			Color color = ColorLog.ColorFromName(text3, 0.2f, 1f);
			string text4 = text3.AddColor(color);
			string text5 = text + " - [" + text2 + "] - Type:" + text4;
			if (oneLine)
			{
				return text5;
			}
			FactionHQ factionHQ = FactionHQ;
			string text6 = ((factionHQ != null) ? factionHQ.faction.factionName.AddColor(factionHQ.faction.color) : "None");
			string text7 = (savedObjective.Hidden ? "true".AddColor(new Color(0.4f, 1f, 0.4f)) : "false".AddColor(new Color(1f, 0.4f, 0.4f)));
			int num = SaveHelper.CountStartedBy(MissionManager.CurrentMission.RuntimeObjectives, this);
			return $"{text5}\nRef:{num} - Type:{text4} - Faction:{text6} - Hidden:{text7}";
		}

		public virtual void ReceiveNetworkData(List<int> data)
		{
		}

		public virtual void AddPins(GraphNodeData data)
		{
			if (SavedObjective.UniqueName != MissionObjectivesFactory.MissionStartName)
			{
				AddFactionPin(data);
			}
		}

		private void AddFactionPin(GraphNodeData data)
		{
			string item = (string.IsNullOrEmpty(FactionLabelOverride) ? "None" : FactionLabelOverride);
			List<string> factionOptions = new List<string> { item };
			foreach (MissionFaction faction in MissionManager.CurrentMission.factions)
			{
				factionOptions.Add(faction.factionName);
			}
			ValueWrapperDelegate<int> factionWrapper = new ValueWrapperDelegate<int>(delegate
			{
				string value = FactionWrapper.Value;
				int num = ((!FactionHelper.EmptyOrNoFaction(value)) ? factionOptions.IndexOf(value) : 0);
				return (num >= 0) ? num : 0;
			}, delegate(int index)
			{
				string text = ((index > 0 && index < factionOptions.Count) ? factionOptions[index] : "None");
				string value = (FactionHelper.EmptyOrNoFaction(text) ? "None" : text);
				FactionWrapper.SetValue(value, factionPinOwner);
			});
			if (factionPinOwner != null)
			{
				FactionWrapper.UnregisterOnChange(factionPinOwner);
			}
			factionPinOwner = factionWrapper;
			FactionWrapper.RegisterOnChange(factionPinOwner, delegate(string newFactionName)
			{
				int num = ((!FactionHelper.EmptyOrNoFaction(newFactionName)) ? factionOptions.IndexOf(newFactionName) : 0);
				int value = ((num >= 0) ? num : 0);
				factionWrapper.SetValue(value, factionPinOwner, invokeOnChangeOnly: false);
			});
			data.InputElements.Add(new GraphDropdownFieldData
			{
				PinId = ObjectiveOutcomeGraph.Ids.Faction,
				DisplayName = "Faction",
				Options = factionOptions,
				ValueWrapper = factionWrapper
			});
		}
	}
}
