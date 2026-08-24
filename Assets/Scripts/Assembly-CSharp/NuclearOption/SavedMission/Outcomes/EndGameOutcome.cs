using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NuclearOption.MissionEditorScripts;
using NuclearOption.NodeGraph;

namespace NuclearOption.SavedMission.Outcomes
{
	public class EndGameOutcome : Outcome
	{
		private EndType endType;

		private readonly ValueWrapperFloat endDelay = new ValueWrapperFloat();

		private readonly ValueWrapperEnum<EndType> endTypeWrapper;

		public EndGameSavedOutcome Saved => (EndGameSavedOutcome)SavedOutcome;

		public EndGameOutcome(EndGameSavedOutcome savedOutcome)
			: base(savedOutcome)
		{
			endTypeWrapper = new ValueWrapperEnum<EndType>(this, () => endType, delegate(EndType v)
			{
				endType = v;
			});
		}

		public override void Complete(Objective completedObjective)
		{
			if ((float)endDelay > 0f)
			{
				UniTask.Delay((int)(float)endDelay * 1000).ContinueWith(delegate
				{
					completedObjective.FactionHQ.DeclareEndGame(endType);
				}).Forget();
			}
			else
			{
				completedObjective.FactionHQ.DeclareEndGame(endType);
			}
		}

		public override void CopyFrom(Outcome original)
		{
			base.CopyFrom(original);
			EndGameOutcome endGameOutcome = (EndGameOutcome)original;
			endType = endGameOutcome.endType;
			endDelay.SetValue(endGameOutcome.endDelay.Value, this);
		}

		public override void Load(MissionLookups lookups)
		{
			base.Load(lookups);
			endType = Saved.endType;
			endTypeWrapper.SetValue((int)endType, this);
			endDelay.SetValue(Saved.endDelay, this);
		}

		public override void Save()
		{
			base.Save();
			Saved.endType = endType;
			Saved.endDelay = endDelay.Value;
		}

		public override void ReferenceDestroyed(ISaveableReference reference)
		{
		}

		public override void DrawData(DataDrawer drawer)
		{
			drawer.DrawEnum<EndType>("End Type", (int)endType, delegate(int v)
			{
				endType = (EndType)v;
			});
			drawer.InstantiateWithParent(drawer.Prefabs.FloatFieldPrefab).Setup("End delay", endDelay, new FloatDataField.FloatSettings
			{
				Slider = new FloatDataField.FloatSlider(0f, 10f)
			});
		}

		public override void AddPins(GraphNodeData data)
		{
			data.InputElements.Add(new GraphFloatFieldData
			{
				PinId = new PinId("End delay"),
				DisplayName = "End delay",
				ValueWrapper = endDelay
			});
			data.InputElements.Add(new GraphDropdownFieldData
			{
				PinId = new PinId("End Type"),
				DisplayName = "End Type",
				Options = new List<string> { "Victory", "Defeat" },
				ValueWrapper = endTypeWrapper
			});
		}
	}
}
