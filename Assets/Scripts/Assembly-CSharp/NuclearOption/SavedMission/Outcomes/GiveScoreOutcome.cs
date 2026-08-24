using System.Collections.Generic;
using NuclearOption.MissionEditorScripts;
using NuclearOption.Networking;
using NuclearOption.NodeGraph;
using UnityEngine.UI;

namespace NuclearOption.SavedMission.Outcomes
{
	public class GiveScoreOutcome : Outcome
	{
		private readonly ValueWrapperBool bothFactions = new ValueWrapperBool();

		private readonly ValueWrapperFloat playerFunds = new ValueWrapperFloat();

		private readonly ValueWrapperFloat factionFunds = new ValueWrapperFloat();

		private readonly ValueWrapperFloat playerScore = new ValueWrapperFloat();

		private readonly ValueWrapperInt rank = new ValueWrapperInt();

		private readonly ValueWrapperFloat factionScore = new ValueWrapperFloat();

		private ChangeType playerFundsType;

		private ChangeType factionFundsType;

		private ChangeType playerScoreType;

		private ChangeType rankType;

		private ChangeType factionScoreType;

		private readonly ValueWrapperEnum<ChangeType> playerFundsTypeWrapper;

		private readonly ValueWrapperEnum<ChangeType> factionFundsTypeWrapper;

		private readonly ValueWrapperEnum<ChangeType> playerScoreTypeWrapper;

		private readonly ValueWrapperEnum<ChangeType> factionScoreTypeWrapper;

		private readonly ValueWrapperEnum<ChangeType> rankTypeWrapper;

		public GiveScoreSavedOutcome Saved => (GiveScoreSavedOutcome)SavedOutcome;

		public GiveScoreOutcome(GiveScoreSavedOutcome savedOutcome)
			: base(savedOutcome)
		{
			playerFundsTypeWrapper = new ValueWrapperEnum<ChangeType>(this, () => playerFundsType, delegate(ChangeType v)
			{
				playerFundsType = v;
			});
			factionFundsTypeWrapper = new ValueWrapperEnum<ChangeType>(this, () => factionFundsType, delegate(ChangeType v)
			{
				factionFundsType = v;
			});
			playerScoreTypeWrapper = new ValueWrapperEnum<ChangeType>(this, () => playerScoreType, delegate(ChangeType v)
			{
				playerScoreType = v;
			});
			factionScoreTypeWrapper = new ValueWrapperEnum<ChangeType>(this, () => factionScoreType, delegate(ChangeType v)
			{
				factionScoreType = v;
			});
			rankTypeWrapper = new ValueWrapperEnum<ChangeType>(this, () => rankType, delegate(ChangeType v)
			{
				rankType = v;
			});
		}

		public override void CopyFrom(Outcome original)
		{
			base.CopyFrom(original);
			GiveScoreOutcome giveScoreOutcome = (GiveScoreOutcome)original;
			bothFactions.SetValue(giveScoreOutcome.bothFactions.Value, this);
			playerFunds.SetValue(giveScoreOutcome.playerFunds.Value, this);
			factionFunds.SetValue(giveScoreOutcome.factionFunds.Value, this);
			playerScore.SetValue(giveScoreOutcome.playerScore.Value, this);
			rank.SetValue(giveScoreOutcome.rank.Value, this);
			factionScore.SetValue(giveScoreOutcome.factionScore.Value, this);
			playerFundsType = giveScoreOutcome.playerFundsType;
			factionFundsType = giveScoreOutcome.factionFundsType;
			playerScoreType = giveScoreOutcome.playerScoreType;
			rankType = giveScoreOutcome.rankType;
			factionScoreType = giveScoreOutcome.factionScoreType;
		}

		public override void Load(MissionLookups lookups)
		{
			base.Load(lookups);
			bothFactions.SetValue(Saved.bothFactions, this);
			playerFundsType = Saved.playerFundsType;
			playerFundsTypeWrapper.SetValue((int)playerFundsType, this);
			playerFunds.SetValue(Saved.playerFunds, this);
			factionFundsType = Saved.factionFundsType;
			factionFundsTypeWrapper.SetValue((int)factionFundsType, this);
			factionFunds.SetValue(Saved.factionFunds, this);
			playerScoreType = Saved.playerScoreType;
			playerScoreTypeWrapper.SetValue((int)playerScoreType, this);
			playerScore.SetValue(Saved.playerScore, this);
			rankType = Saved.rankType;
			rankTypeWrapper.SetValue((int)rankType, this);
			rank.SetValue(Saved.rank, this);
			factionScoreType = Saved.factionScoreType;
			factionScoreTypeWrapper.SetValue((int)factionScoreType, this);
			factionScore.SetValue(Saved.factionScore, this);
		}

		public override void Save()
		{
			base.Save();
			Saved.bothFactions = bothFactions.Value;
			Saved.playerFundsType = playerFundsType;
			Saved.playerFunds = playerFunds.Value;
			Saved.factionFundsType = factionFundsType;
			Saved.factionFunds = factionFunds.Value;
			Saved.playerScoreType = playerScoreType;
			Saved.playerScore = playerScore.Value;
			Saved.rankType = rankType;
			Saved.rank = rank.Value;
			Saved.factionScoreType = factionScoreType;
			Saved.factionScore = factionScore.Value;
		}

		public override void ReferenceDestroyed(ISaveableReference reference)
		{
		}

		public override void Complete(Objective completedObjective)
		{
			if ((bool)bothFactions)
			{
				foreach (FactionHQ value in FactionRegistry.HQLookup.Values)
				{
					RewardFaction(value);
				}
				return;
			}
			FactionHQ factionHQ = completedObjective.FactionHQ;
			if (factionHQ != null)
			{
				RewardFaction(factionHQ);
			}
		}

		private void RewardFaction(FactionHQ faction)
		{
			ChangeTypeHelper.ChangeValue(factionFunds.Value, factionFundsType, faction.factionFunds, delegate(float v)
			{
				faction.SetFunds(v);
			});
			ChangeTypeHelper.ChangeValue(factionScore.Value, factionScoreType, faction.factionScore, delegate(float v)
			{
				faction.SetScore(v);
			});
			foreach (Player player in faction.GetPlayers(sortByScore: false))
			{
				ChangeTypeHelper.ChangeValue(playerFunds.Value, playerFundsType, player.Allocation, delegate(float v)
				{
					player.SetAllocation(v);
				});
				ChangeTypeHelper.ChangeValue(playerScore.Value, playerScoreType, player.PlayerScore, delegate(float v)
				{
					player.SetScore(v);
				});
				ChangeTypeHelper.ChangeValue(rank.Value, rankType, player.PlayerRank, delegate(float v)
				{
					player.SetRank((int)v, setScoreOffset: true);
				});
			}
		}

		public override void DrawData(DataDrawer drawer)
		{
			drawer.CheckGroupBox();
			float value = drawer.Width.Value;
			float fieldWidth = value * 2f / 3f - 10f;
			float typeWidth = value * 1f / 3f;
			drawer.InstantiateWithParent(drawer.Prefabs.BoolFieldPrefab).Setup("Both Factions", bothFactions);
			drawer.HorizontalGroup(delegate(HorizontalLayoutGroup group)
			{
				group.spacing = 10f;
				FloatDataField floatDataField = drawer.InstantiateWithParent(drawer.Prefabs.FloatFieldPrefab);
				floatDataField.Setup("Player Funds", playerFunds);
				floatDataField.SetRectWidth(fieldWidth);
				DropdownDataField dropdownDataField = drawer.DrawEnum<ChangeType>("", (int)playerFundsType, delegate(int v)
				{
					playerFundsType = (ChangeType)v;
				});
				dropdownDataField.HideLabel();
				dropdownDataField.FieldLayout.minWidth = typeWidth;
				dropdownDataField.SetRectWidth(typeWidth);
			});
			drawer.HorizontalGroup(delegate(HorizontalLayoutGroup group)
			{
				group.spacing = 10f;
				FloatDataField floatDataField = drawer.InstantiateWithParent(drawer.Prefabs.FloatFieldPrefab);
				floatDataField.Setup("Faction Funds", factionFunds);
				floatDataField.SetRectWidth(fieldWidth);
				DropdownDataField dropdownDataField = drawer.DrawEnum<ChangeType>("", (int)factionFundsType, delegate(int v)
				{
					factionFundsType = (ChangeType)v;
				});
				dropdownDataField.HideLabel();
				dropdownDataField.FieldLayout.minWidth = typeWidth;
				dropdownDataField.SetRectWidth(typeWidth);
			});
			drawer.HorizontalGroup(delegate(HorizontalLayoutGroup group)
			{
				group.spacing = 10f;
				FloatDataField floatDataField = drawer.InstantiateWithParent(drawer.Prefabs.FloatFieldPrefab);
				floatDataField.Setup("Player Score", playerScore);
				floatDataField.SetRectWidth(fieldWidth);
				DropdownDataField dropdownDataField = drawer.DrawEnum<ChangeType>("", (int)playerScoreType, delegate(int v)
				{
					playerScoreType = (ChangeType)v;
				});
				dropdownDataField.HideLabel();
				dropdownDataField.FieldLayout.minWidth = typeWidth;
				dropdownDataField.SetRectWidth(typeWidth);
			});
			drawer.HorizontalGroup(delegate(HorizontalLayoutGroup group)
			{
				group.spacing = 10f;
				FloatDataField floatDataField = drawer.InstantiateWithParent(drawer.Prefabs.FloatFieldPrefab);
				floatDataField.Setup("Faction Score", factionScore);
				floatDataField.SetRectWidth(fieldWidth);
				DropdownDataField dropdownDataField = drawer.DrawEnum<ChangeType>("", (int)factionScoreType, delegate(int v)
				{
					factionScoreType = (ChangeType)v;
				});
				dropdownDataField.HideLabel();
				dropdownDataField.FieldLayout.minWidth = typeWidth;
				dropdownDataField.SetRectWidth(typeWidth);
			});
			drawer.HorizontalGroup(delegate(HorizontalLayoutGroup group)
			{
				group.spacing = 10f;
				FloatDataField floatDataField = drawer.InstantiateWithParent(drawer.Prefabs.FloatFieldPrefab);
				floatDataField.Setup("Rank", rank);
				floatDataField.SetRectWidth(fieldWidth);
				DropdownDataField dropdownDataField = drawer.DrawEnum<ChangeType>("", (int)rankType, delegate(int v)
				{
					rankType = (ChangeType)v;
				});
				dropdownDataField.HideLabel();
				dropdownDataField.FieldLayout.minWidth = typeWidth;
				dropdownDataField.SetRectWidth(typeWidth);
			});
		}

		public override void AddPins(GraphNodeData data)
		{
			data.InputElements.Add(new GraphBoolFieldData
			{
				PinId = new PinId("Both Factions"),
				DisplayName = "Both Factions",
				ValueWrapper = bothFactions
			});
			data.InputElements.Add(new GraphFloatFieldData
			{
				PinId = new PinId("Player Funds"),
				DisplayName = "Player Funds",
				ValueWrapper = playerFunds
			});
			data.InputElements.Add(new GraphDropdownFieldData
			{
				PinId = new PinId("Player Funds Type"),
				DisplayName = "Player Funds Type",
				Options = new List<string> { "Add", "Subtract", "Set" },
				ValueWrapper = playerFundsTypeWrapper
			});
			data.InputElements.Add(new GraphFloatFieldData
			{
				PinId = new PinId("Faction Funds"),
				DisplayName = "Faction Funds",
				ValueWrapper = factionFunds
			});
			data.InputElements.Add(new GraphDropdownFieldData
			{
				PinId = new PinId("Faction Funds Type"),
				DisplayName = "Faction Funds Type",
				Options = new List<string> { "Add", "Subtract", "Set" },
				ValueWrapper = factionFundsTypeWrapper
			});
			data.InputElements.Add(new GraphFloatFieldData
			{
				PinId = new PinId("Player Score"),
				DisplayName = "Player Score",
				ValueWrapper = playerScore
			});
			data.InputElements.Add(new GraphDropdownFieldData
			{
				PinId = new PinId("Player Score Type"),
				DisplayName = "Player Score Type",
				Options = new List<string> { "Add", "Subtract", "Set" },
				ValueWrapper = playerScoreTypeWrapper
			});
			data.InputElements.Add(new GraphFloatFieldData
			{
				PinId = new PinId("Faction Score"),
				DisplayName = "Faction Score",
				ValueWrapper = factionScore
			});
			data.InputElements.Add(new GraphDropdownFieldData
			{
				PinId = new PinId("Faction Score Type"),
				DisplayName = "Faction Score Type",
				Options = new List<string> { "Add", "Subtract", "Set" },
				ValueWrapper = factionScoreTypeWrapper
			});
			data.InputElements.Add(new GraphIntFieldData
			{
				PinId = new PinId("Rank"),
				DisplayName = "Rank",
				ValueWrapper = rank
			});
			data.InputElements.Add(new GraphDropdownFieldData
			{
				PinId = new PinId("Rank Type"),
				DisplayName = "Rank Type",
				Options = new List<string> { "Add", "Subtract", "Set" },
				ValueWrapper = rankTypeWrapper
			});
		}
	}
}
