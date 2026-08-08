using System;
using NuclearOption.MissionEditorScripts;
using NuclearOption.NodeGraph;

namespace NuclearOption.SavedMission.Outcomes
{
	public class ModifyFactionOutcome : Outcome
	{
		private static readonly MissionFaction defaultSettings = new MissionFaction("Defaults");

		private readonly ValueWrapperBool bothFactions = new ValueWrapperBool();

		private readonly ValueWrapperOverride<float> excessFundsThreshold = new ValueWrapperOverride<float>();

		private readonly ValueWrapperOverride<float> playerJoinAllowance = new ValueWrapperOverride<float>();

		private readonly ValueWrapperOverride<float> playerTaxRate = new ValueWrapperOverride<float>();

		private readonly ValueWrapperOverride<float> regularIncome = new ValueWrapperOverride<float>();

		private readonly ValueWrapperOverride<float> killReward = new ValueWrapperOverride<float>();

		private readonly ValueWrapperOverride<bool> preventDonation = new ValueWrapperOverride<bool>();

		private readonly ValueWrapperOverride<float> aiAircraftLimit = new ValueWrapperOverride<float>();

		private readonly ValueWrapperOverride<float> reduceAIPerFriendlyPlayer = new ValueWrapperOverride<float>();

		private readonly ValueWrapperOverride<float> addAIPerEnemyPlayer = new ValueWrapperOverride<float>();

		private readonly ValueWrapperOverride<float> warheadsReserve = new ValueWrapperOverride<float>();

		private readonly ValueWrapperOverride<float> reserveAirframes = new ValueWrapperOverride<float>();

		private readonly ValueWrapperOverride<float> extraReservesPerPlayer = new ValueWrapperOverride<float>();

		private readonly ValueWrapperOverride<float> excessFundsDistributePercent = new ValueWrapperOverride<float>();

		private readonly ValueWrapperOverride<bool> preventJoin = new ValueWrapperOverride<bool>();

		public ModifyFactionSavedOutcome Saved => (ModifyFactionSavedOutcome)SavedOutcome;

		public ModifyFactionOutcome(ModifyFactionSavedOutcome savedOutcome)
			: base(savedOutcome)
		{
		}

		public override void CopyFrom(Outcome original)
		{
			base.CopyFrom(original);
			ModifyFactionOutcome modifyFactionOutcome = (ModifyFactionOutcome)original;
			bothFactions.SetValue(modifyFactionOutcome.bothFactions.Value, this);
			excessFundsThreshold.SetValue(modifyFactionOutcome.excessFundsThreshold.Value, this);
			playerJoinAllowance.SetValue(modifyFactionOutcome.playerJoinAllowance.Value, this);
			playerTaxRate.SetValue(modifyFactionOutcome.playerTaxRate.Value, this);
			regularIncome.SetValue(modifyFactionOutcome.regularIncome.Value, this);
			killReward.SetValue(modifyFactionOutcome.killReward.Value, this);
			preventDonation.SetValue(modifyFactionOutcome.preventDonation.Value, this);
			aiAircraftLimit.SetValue(modifyFactionOutcome.aiAircraftLimit.Value, this);
			reduceAIPerFriendlyPlayer.SetValue(modifyFactionOutcome.reduceAIPerFriendlyPlayer.Value, this);
			addAIPerEnemyPlayer.SetValue(modifyFactionOutcome.addAIPerEnemyPlayer.Value, this);
			warheadsReserve.SetValue(modifyFactionOutcome.warheadsReserve.Value, this);
			reserveAirframes.SetValue(modifyFactionOutcome.reserveAirframes.Value, this);
			extraReservesPerPlayer.SetValue(modifyFactionOutcome.extraReservesPerPlayer.Value, this);
			excessFundsDistributePercent.SetValue(modifyFactionOutcome.excessFundsDistributePercent.Value, this);
			preventJoin.SetValue(modifyFactionOutcome.preventJoin.Value, this);
		}

		public override void Load(MissionLookups lookups)
		{
			base.Load(lookups);
			bothFactions.SetValue(Saved.bothFactions, this);
			excessFundsThreshold.SetValue(Saved.excessFundsThreshold, this);
			playerJoinAllowance.SetValue(Saved.playerJoinAllowance, this);
			playerTaxRate.SetValue(Saved.playerTaxRate, this);
			regularIncome.SetValue(Saved.regularIncome, this);
			killReward.SetValue(Saved.killReward, this);
			preventDonation.SetValue(Saved.preventDonation, this);
			aiAircraftLimit.SetValue(Saved.aiAircraftLimit, this);
			reduceAIPerFriendlyPlayer.SetValue(Saved.reduceAIPerFriendlyPlayer, this);
			addAIPerEnemyPlayer.SetValue(Saved.addAIPerEnemyPlayer, this);
			warheadsReserve.SetValue(Saved.warheadsReserve, this);
			reserveAirframes.SetValue(Saved.reserveAirframes, this);
			extraReservesPerPlayer.SetValue(Saved.extraReservesPerPlayer, this);
			excessFundsDistributePercent.SetValue(Saved.excessFundsDistributePercent, this);
			preventJoin.SetValue(Saved.preventJoin, this);
			SetIfNoOverride<float>(excessFundsThreshold, defaultSettings.startingBalance);
			SetIfNoOverride<float>(playerJoinAllowance, defaultSettings.playerJoinAllowance);
			SetIfNoOverride<float>(playerTaxRate, defaultSettings.playerTaxRate);
			SetIfNoOverride<float>(regularIncome, defaultSettings.regularIncome);
			SetIfNoOverride<float>(excessFundsDistributePercent, defaultSettings.excessFundsDistributePercent);
			SetIfNoOverride<float>(killReward, defaultSettings.killReward);
			SetIfNoOverride<bool>(preventDonation, defaultSettings.preventDonation);
			SetIfNoOverride<float>(aiAircraftLimit, defaultSettings.AIAircraftLimit);
			SetIfNoOverride<float>(reduceAIPerFriendlyPlayer, defaultSettings.reduceAIPerFriendlyPlayer);
			SetIfNoOverride<float>(addAIPerEnemyPlayer, defaultSettings.addAIPerEnemyPlayer);
			SetIfNoOverride<float>(warheadsReserve, defaultSettings.reserveWarheads);
			SetIfNoOverride<float>(reserveAirframes, defaultSettings.reserveAirframes);
			SetIfNoOverride<float>(extraReservesPerPlayer, defaultSettings.extraReservesPerPlayer);
			SetIfNoOverride<bool>(preventJoin, defaultSettings.preventJoin);
			static void SetIfNoOverride<T>(ValueWrapperOverride<T> wrapper, T value) where T : IEquatable<T>
			{
				if (!wrapper.Value.IsOverride)
				{
					wrapper.SetValue(new Override<T>(isOverride: false, value), null);
				}
			}
		}

		public override void Save()
		{
			base.Save();
			Saved.bothFactions = bothFactions.Value;
			Saved.excessFundsThreshold = excessFundsThreshold.Value;
			Saved.playerJoinAllowance = playerJoinAllowance.Value;
			Saved.playerTaxRate = playerTaxRate.Value;
			Saved.regularIncome = regularIncome.Value;
			Saved.killReward = killReward.Value;
			Saved.preventDonation = preventDonation.Value;
			Saved.aiAircraftLimit = aiAircraftLimit.Value;
			Saved.reduceAIPerFriendlyPlayer = reduceAIPerFriendlyPlayer.Value;
			Saved.addAIPerEnemyPlayer = addAIPerEnemyPlayer.Value;
			Saved.warheadsReserve = warheadsReserve.Value;
			Saved.reserveAirframes = reserveAirframes.Value;
			Saved.extraReservesPerPlayer = extraReservesPerPlayer.Value;
			Saved.excessFundsDistributePercent = excessFundsDistributePercent.Value;
			Saved.preventJoin = preventJoin.Value;
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
					ApplySettings(value);
				}
				return;
			}
			FactionHQ factionHQ = completedObjective.FactionHQ;
			if (factionHQ != null)
			{
				ApplySettings(factionHQ);
			}
		}

		private void ApplySettings(FactionHQ faction)
		{
			if (excessFundsThreshold.Value.IsOverride)
			{
				faction.excessFundsThreshold = excessFundsThreshold.Value.Value;
			}
			if (playerJoinAllowance.Value.IsOverride)
			{
				faction.playerJoinAllowance = playerJoinAllowance.Value.Value;
			}
			if (playerTaxRate.Value.IsOverride)
			{
				faction.playerTaxRate = playerTaxRate.Value.Value;
			}
			if (regularIncome.Value.IsOverride)
			{
				faction.regularIncome = regularIncome.Value.Value;
			}
			if (killReward.Value.IsOverride)
			{
				faction.killReward = killReward.Value.Value;
			}
			if (excessFundsDistributePercent.Value.IsOverride)
			{
				faction.excessFundsDistributePercent = excessFundsDistributePercent.Value.Value;
			}
			if (preventJoin.Value.IsOverride)
			{
				faction.NetworkpreventJoin = preventJoin.Value.Value;
			}
			if (preventDonation.Value.IsOverride)
			{
				faction.NetworkpreventDonation = preventDonation.Value.Value;
			}
			if (aiAircraftLimit.Value.IsOverride)
			{
				faction.AIAircraftLimit = (int)aiAircraftLimit.Value.Value;
			}
			if (reduceAIPerFriendlyPlayer.Value.IsOverride)
			{
				faction.reduceAIPerFriendlyPlayer = reduceAIPerFriendlyPlayer.Value.Value;
			}
			if (addAIPerEnemyPlayer.Value.IsOverride)
			{
				faction.addAIPerEnemyPlayer = addAIPerEnemyPlayer.Value.Value;
			}
			if (warheadsReserve.Value.IsOverride)
			{
				faction.warheadsReserve = (int)warheadsReserve.Value.Value;
			}
			if (reserveAirframes.Value.IsOverride)
			{
				faction.reserveAirframes = (int)reserveAirframes.Value.Value;
			}
			if (extraReservesPerPlayer.Value.IsOverride)
			{
				faction.extraReservesPerPlayer = (int)extraReservesPerPlayer.Value.Value;
			}
		}

		public override void DrawData(DataDrawer drawer)
		{
			drawer.CheckGroupBox();
			drawer.InstantiateWithParent(drawer.Prefabs.BoolFieldPrefab).Setup("Both Factions", bothFactions);
			drawer.DrawOverride("Prevent Join", preventJoin, drawer.Prefabs.BoolFieldPrefab);
			drawer.DrawHeader("Funds");
			drawer.DrawOverride("Excess Funds", excessFundsThreshold, drawer.Prefabs.FloatFieldPrefab);
			drawer.DrawOverride("Join Allowance", playerJoinAllowance, drawer.Prefabs.FloatFieldPrefab);
			drawer.DrawOverride("Player Tax Rate", playerTaxRate, drawer.Prefabs.FloatFieldPrefab);
			drawer.DrawOverride("Regular Income", regularIncome, drawer.Prefabs.FloatFieldPrefab);
			drawer.DrawOverride("Distribute Excess Funds", excessFundsDistributePercent, drawer.Prefabs.FloatFieldPrefab);
			drawer.DrawOverride("Kill Reward", killReward, drawer.Prefabs.FloatFieldPrefab);
			drawer.DrawOverride("Prevent Donation", preventDonation, drawer.Prefabs.BoolFieldPrefab);
			drawer.DrawHeader("Warheads");
			drawer.DrawOverride("Warheads Reserve", warheadsReserve, drawer.Prefabs.FloatFieldPrefab).Item2.SetContentType(wholeNumbers: true);
			drawer.DrawHeader("Ai limits");
			drawer.DrawOverride("AI Aircraft", aiAircraftLimit, drawer.Prefabs.FloatFieldPrefab).Item2.SetContentType(wholeNumbers: true);
			drawer.DrawOverride("Reduce AI Per Friendly", reduceAIPerFriendlyPlayer, drawer.Prefabs.FloatFieldPrefab);
			drawer.DrawOverride("Add AI Per Enemy", addAIPerEnemyPlayer, drawer.Prefabs.FloatFieldPrefab);
			drawer.DrawHeader("Reserve Airframes");
			drawer.DrawOverride("Baseline Reserve", reserveAirframes, drawer.Prefabs.FloatFieldPrefab).Item2.SetContentType(wholeNumbers: true);
			drawer.DrawOverride("Reserves Per Player", extraReservesPerPlayer, drawer.Prefabs.FloatFieldPrefab).Item2.SetContentType(wholeNumbers: true);
			DataField[] componentsInChildren = drawer.Parent.GetComponentsInChildren<DataField>();
			foreach (DataField obj in componentsInChildren)
			{
				obj.LabelLayout.minWidth = 160f;
				obj.FieldLayout.minWidth = 140f;
			}
		}

		public override void AddPins(GraphNodeData data)
		{
			data.InputElements.Add(new GraphBoolFieldData
			{
				PinId = new PinId("Both Factions"),
				DisplayName = "Both Factions",
				ValueWrapper = bothFactions
			});
			data.InputElements.Add(new GraphBoolFieldData
			{
				PinId = new PinId("Prevent Join"),
				DisplayName = "Prevent Join",
				ValueWrapper = preventJoin
			});
			data.InputElements.Add(new GraphFloatFieldData
			{
				PinId = new PinId("Excess Funds"),
				DisplayName = "Excess Funds",
				ValueWrapper = excessFundsThreshold
			});
			data.InputElements.Add(new GraphFloatFieldData
			{
				PinId = new PinId("Join Allowance"),
				DisplayName = "Join Allowance",
				ValueWrapper = playerJoinAllowance
			});
			data.InputElements.Add(new GraphFloatFieldData
			{
				PinId = new PinId("Player Tax Rate"),
				DisplayName = "Player Tax Rate",
				ValueWrapper = playerTaxRate
			});
			data.InputElements.Add(new GraphFloatFieldData
			{
				PinId = new PinId("Regular Income"),
				DisplayName = "Regular Income",
				ValueWrapper = regularIncome
			});
			data.InputElements.Add(new GraphFloatFieldData
			{
				PinId = new PinId("Distribute Excess Funds"),
				DisplayName = "Distribute Excess Funds",
				ValueWrapper = excessFundsDistributePercent
			});
			data.InputElements.Add(new GraphFloatFieldData
			{
				PinId = new PinId("Kill Reward"),
				DisplayName = "Kill Reward",
				ValueWrapper = killReward
			});
			data.InputElements.Add(new GraphBoolFieldData
			{
				PinId = new PinId("Prevent Donation"),
				DisplayName = "Prevent Donation",
				ValueWrapper = preventDonation
			});
			data.InputElements.Add(new GraphFloatFieldData
			{
				PinId = new PinId("Warheads Reserve"),
				DisplayName = "Warheads Reserve",
				ValueWrapper = warheadsReserve
			});
			data.InputElements.Add(new GraphFloatFieldData
			{
				PinId = new PinId("AI Aircraft"),
				DisplayName = "AI Aircraft",
				ValueWrapper = aiAircraftLimit
			});
			data.InputElements.Add(new GraphFloatFieldData
			{
				PinId = new PinId("Reduce AI Per Friendly"),
				DisplayName = "Reduce AI Per Friendly",
				ValueWrapper = reduceAIPerFriendlyPlayer
			});
			data.InputElements.Add(new GraphFloatFieldData
			{
				PinId = new PinId("Add AI Per Enemy"),
				DisplayName = "Add AI Per Enemy",
				ValueWrapper = addAIPerEnemyPlayer
			});
			data.InputElements.Add(new GraphFloatFieldData
			{
				PinId = new PinId("Baseline Reserve"),
				DisplayName = "Baseline Reserve",
				ValueWrapper = reserveAirframes
			});
			data.InputElements.Add(new GraphFloatFieldData
			{
				PinId = new PinId("Reserves Per Player"),
				DisplayName = "Reserves Per Player",
				ValueWrapper = extraReservesPerPlayer
			});
		}
	}
}
