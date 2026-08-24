using System;
using System.Collections.Generic;
using Mirage;
using NuclearOption.MissionEditorScripts;
using NuclearOption.Networking;
using NuclearOption.NodeGraph;
using Unity.Profiling;
using UnityEngine;

namespace NuclearOption.SavedMission.Objectives
{
	public class ReachUnitsObjective : Objective, IObjectiveWithPosition
	{
		public class TargetUnit
		{
			public SavedUnit Unit;

			public readonly ValueWrapperFloat Range = new ValueWrapperFloat();

			public GlobalPosition GlobalPosition;

			public ObjectivePosition ToObjectivePosition()
			{
				return new ObjectivePosition(GlobalPosition, Range);
			}
		}

		private static readonly ProfilerMarker updateAndCheckMarker = new ProfilerMarker("ReachUnitsObjectiveUpdateAndCheck");

		private CompleteOrder completeOrder;

		private List<TargetUnit> targets;

		private int currentTarget;

		private readonly List<ObjectivePosition> nextPosition = new List<ObjectivePosition>();

		private readonly List<TargetUnit> toCheck = new List<TargetUnit>();

		private readonly List<TargetUnit> allReached = new List<TargetUnit>();

		private readonly List<int> networkList = new List<int>();

		private float completePercent;

		private readonly List<SavedUnit> allUnitsDropdownList = new List<SavedUnit>();

		private readonly List<string> allUnitsNamesDropdownList = new List<string>();

		private readonly ValueWrapperFloat completeSomePercent = new ValueWrapperFloat();

		public override bool NeedsFaction => true;

		public override float CompletePercent => completePercent;

		IReadOnlyList<ObjectivePosition> IObjectiveWithPosition.Positions
		{
			get
			{
				if (targets.Count <= 0)
				{
					return Array.Empty<ObjectivePosition>();
				}
				return nextPosition;
			}
		}

		public ReachUnitsSavedObjective Saved => (ReachUnitsSavedObjective)SavedObjective;

		public ReachUnitsObjective(ReachUnitsSavedObjective savedObjective)
			: base(savedObjective)
		{
		}

		private void UpdateCompletePercent()
		{
			switch (completeOrder)
			{
			case CompleteOrder.CompleteAny:
				completePercent = 0f;
				break;
			case CompleteOrder.CompleteAll:
				completePercent = 1f - (float)nextPosition.Count / (float)targets.Count;
				break;
			case CompleteOrder.InOrder:
				completePercent = (float)currentTarget / (float)targets.Count;
				break;
			}
		}

		public override void ReceiveNetworkData(List<int> data)
		{
			toCheck.Clear();
			foreach (int datum in data)
			{
				TargetUnit item = targets[datum];
				toCheck.Add(item);
			}
			UpdateCompletePercent();
		}

		public override void CopyFrom(Objective original)
		{
			base.CopyFrom(original);
			ReachUnitsObjective reachUnitsObjective = (ReachUnitsObjective)original;
			completeOrder = reachUnitsObjective.completeOrder;
			completeSomePercent.SetValue(reachUnitsObjective.completeSomePercent.Value, this);
			targets = new List<TargetUnit>();
			if (reachUnitsObjective.targets == null)
			{
				return;
			}
			foreach (TargetUnit target in reachUnitsObjective.targets)
			{
				TargetUnit targetUnit = new TargetUnit
				{
					Unit = target.Unit
				};
				targetUnit.Range.SetValue(target.Range.Value, this);
				targetUnit.GlobalPosition = target.GlobalPosition;
				targets.Add(targetUnit);
			}
		}

		public override void Load(MissionLookups lookups)
		{
			base.Load(lookups);
			completeOrder = Saved.completeOrder;
			completeSomePercent.SetValue(Saved.completeSomePercent, this);
			targets = new List<TargetUnit>();
			if (Saved.targets == null)
			{
				return;
			}
			foreach (SavedReachUnitData target in Saved.targets)
			{
				TargetUnit targetUnit = new TargetUnit();
				targetUnit.Range.SetValue(target.Range, targetUnit);
				if (lookups.SavedUnits.TryGetValue(target.TargetUnit, out var value))
				{
					targetUnit.Unit = value;
				}
				else
				{
					targetUnit.Unit = null;
					lookups.LoadErrors.AddWarn("'" + target.TargetUnit + "' was not found in lookup for SavedUnit");
				}
				targets.Add(targetUnit);
			}
		}

		public override void Save()
		{
			base.Save();
			Saved.completeOrder = completeOrder;
			Saved.completeSomePercent = completeSomePercent.Value;
			Saved.targets.Clear();
			foreach (TargetUnit target in targets)
			{
				SavedReachUnitData savedReachUnitData = new SavedReachUnitData();
				savedReachUnitData.Range = target.Range.Value;
				savedReachUnitData.TargetUnit = target.Unit.GetNameSavedCheckDestroyed();
				Saved.targets.Add(savedReachUnitData);
			}
		}

		protected override void DataReferenceDestroyed(ISaveableReference reference)
		{
			targets.RemoveAll((TargetUnit t) => t.Unit.SavedReferenceEquals(reference));
		}

		public override void OnStart()
		{
			RefreshTargets();
		}

		public override void ClientOnlyUpdate()
		{
			UpdatePositions(updateCache: true);
		}

		public override bool UpdateAndCheck()
		{
			using (updateAndCheckMarker.Auto())
			{
				if (CheckTargetsReached(out var reached))
				{
					if (completeOrder == CompleteOrder.CompleteAny)
					{
						return true;
					}
					if (completeOrder == CompleteOrder.CompleteAll)
					{
						allReached.Add(reached);
						if (allReached.Count == targets.Count)
						{
							return true;
						}
					}
					if (completeOrder == CompleteOrder.InOrder)
					{
						currentTarget++;
						if (currentTarget >= targets.Count)
						{
							return true;
						}
					}
					RefreshTargets();
				}
				UpdatePositions(updateCache: false);
				return false;
			}
		}

		private bool CheckTargetsReached(out TargetUnit reached)
		{
			reached = null;
			List<Player> players = base.FactionHQ.GetPlayers(sortByScore: false);
			if (players.Count == 0)
			{
				return false;
			}
			foreach (TargetUnit item in toCheck)
			{
				UnitRegistry.TryGetPosition(item.Unit, out item.GlobalPosition);
			}
			foreach (Player item2 in players)
			{
				Aircraft aircraft = item2.Aircraft;
				if (aircraft == null)
				{
					continue;
				}
				GlobalPosition globalPosition = aircraft.GlobalPosition();
				Vector3 velocity = aircraft.rb.velocity;
				foreach (TargetUnit item3 in toCheck)
				{
					if (FastMath.InRange(globalPosition, item3.GlobalPosition, item3.Range) && Vector3.Dot(velocity, item3.GlobalPosition - globalPosition) < 0f)
					{
						reached = item3;
						return true;
					}
				}
			}
			reached = null;
			return false;
		}

		private void RefreshTargets()
		{
			toCheck.Clear();
			networkList.Clear();
			if (targets.Count == 0)
			{
				return;
			}
			if (completeOrder == CompleteOrder.InOrder)
			{
				toCheck.Add(targets[currentTarget]);
				networkList.Add(currentTarget);
			}
			else
			{
				for (int i = 0; i < targets.Count; i++)
				{
					TargetUnit item = targets[i];
					if (!allReached.Contains(item))
					{
						toCheck.Add(item);
						networkList.Add(i);
					}
				}
			}
			UpdatePositions(updateCache: false);
			UpdateCompletePercent();
			if (MissionManager.IsServer)
			{
				MissionManager.UpdateNetworkData(this, networkList);
			}
		}

		private void UpdatePositions(bool updateCache)
		{
			if (updateCache)
			{
				foreach (TargetUnit item in toCheck)
				{
					UnitRegistry.TryGetPosition(item.Unit, out item.GlobalPosition);
				}
			}
			nextPosition.Clear();
			foreach (TargetUnit item2 in toCheck)
			{
				nextPosition.Add(item2.ToObjectivePosition());
			}
		}

		public override void DrawData(DataDrawer drawer)
		{
			if (targets == null)
			{
				targets = new List<TargetUnit>();
			}
			RefreshAllUnitsDropdown();
			CompleteOrderPercentWrapper.Create(drawer, completeOrder, delegate(CompleteOrder v)
			{
				completeOrder = v;
			}, completeSomePercent);
			drawer.Space(10);
			drawer.DrawList(300, targets, CreateWaypointGUI);
		}

		public override void AddPins(GraphNodeData data)
		{
			base.AddPins(data);
			data.InputElements.Add(new GraphReadOnlyFieldData
			{
				PinId = new PinId("Targets Count"),
				DisplayName = "Targets Count",
				GetText = () => (targets == null) ? "0" : targets.Count.ToString()
			});
		}

		private void RefreshAllUnitsDropdown()
		{
			allUnitsDropdownList.Clear();
			allUnitsDropdownList.Add(null);
			using AutoPool<List<SavedUnit>>.Wrapper wrapper = AutoPool<List<SavedUnit>>.Take();
			List<SavedUnit> item = wrapper.Item;
			MissionManager.GetAllSavedUnitsNonAlloc(item, includeBuiltIn: true);
			for (int i = 0; i < item.Count; i++)
			{
				SavedUnit savedUnit = item[i];
				if (!string.IsNullOrEmpty(savedUnit.UniqueName))
				{
					allUnitsDropdownList.Add(savedUnit);
				}
			}
			allUnitsNamesDropdownList.Clear();
			foreach (SavedUnit allUnitsDropdown in allUnitsDropdownList)
			{
				string item2 = ((allUnitsDropdown != null) ? allUnitsDropdown.UniqueName : string.Empty);
				allUnitsNamesDropdownList.Add(item2);
			}
		}

		private void CreateWaypointGUI(int _, TargetUnit target, DataDrawer drawer)
		{
			drawer.InstantiateWithParent(drawer.Prefabs.FloatFieldPrefab).Setup("Range", target.Range);
			int num = allUnitsDropdownList.IndexOf(target.Unit);
			if (num < 0)
			{
				num = 0;
			}
			drawer.DrawDropdown("Unit", allUnitsNamesDropdownList, num, delegate(int i)
			{
				target.Unit = allUnitsDropdownList[i];
			});
		}
	}
}
