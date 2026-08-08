using System;
using NuclearOption.MissionEditorScripts;
using NuclearOption.NodeGraph;
using UnityEngine;

namespace NuclearOption.SavedMission
{
	public abstract class Outcome : ISaveableReference
	{
		public readonly SavedOutcome SavedOutcome;

		string ISaveableReference.UniqueName => SavedOutcome.UniqueName;

		bool ISaveableReference.Destroyed { get; set; }

		bool ISaveableReference.CanBeReference => true;

		bool ISaveableReference.CanBeSorted => true;

		public event RenamedDelegate OnRenamed;

		public Outcome(SavedOutcome savedOutcome)
		{
			SavedOutcome = savedOutcome ?? throw new ArgumentNullException("savedOutcome");
		}

		public void Rename(string newName)
		{
			if (!(SavedOutcome.UniqueName == newName))
			{
				string uniqueName = SavedOutcome.UniqueName;
				SavedOutcome.UniqueName = newName;
				this.OnRenamed?.Invoke(this, uniqueName, newName);
			}
		}

		public abstract void Complete(Objective completedObjective);

		public virtual void CopyFrom(Outcome original)
		{
		}

		public virtual void Load(MissionLookups lookups)
		{
		}

		public virtual void Save()
		{
		}

		public virtual void ReferenceDestroyed(ISaveableReference reference)
		{
		}

		public virtual void ReferenceReplaced(ISaveableReference oldRef, ISaveableReference newRef)
		{
		}

		public override string ToString()
		{
			return $"[{SavedOutcome.UniqueName} Type={SavedOutcome.OutcomeTypeEnum}]";
		}

		public virtual void DrawData(DataDrawer drawer)
		{
		}

		public string ToUIString(bool oneLine = false)
		{
			SavedOutcome savedOutcome = SavedOutcome;
			string text = savedOutcome.UniqueName.AddColor(new Color(0.7f, 0.7f, 0.7f));
			string text2 = savedOutcome.OutcomeTypeEnum.ToString();
			Color color = ColorLog.ColorFromName(text2, 0.8f, 1f);
			string text3 = text2.AddColor(color);
			string text4 = "[" + text + "] - Type:" + text3;
			if (oneLine)
			{
				return text4;
			}
			int num = SaveHelper.CountUsedBy(MissionManager.CurrentMission.RuntimeObjectives, this);
			return $"{text4}\nRef:{num}";
		}

		public virtual void AddPins(GraphNodeData data)
		{
		}
	}
}
