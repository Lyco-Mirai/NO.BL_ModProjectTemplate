using System;
using System.Collections.Generic;
using Unity.Profiling;

namespace NuclearOption.SavedMission.Objectives
{
	public abstract class CompleteOrderObjectiveWithPositions<T> : CompleteOrderObjective<T>, IObjectiveWithPosition
	{
		private static readonly ProfilerMarker updateAndCheckMarker = new ProfilerMarker("CompleteOrderObjectiveWithPositionsUpdateAndCheck");

		private readonly List<ObjectivePosition> positions = new List<ObjectivePosition>();

		private readonly Action<T> updateNotCompletedCached;

		IReadOnlyList<ObjectivePosition> IObjectiveWithPosition.Positions => positions;

		public CompleteOrderObjectiveWithPositions(SavedObjective savedObjective)
			: base(savedObjective)
		{
			updateNotCompletedCached = UpdateNotCompleted;
		}

		public override bool UpdateAndCheck()
		{
			using (updateAndCheckMarker.Auto())
			{
				if (base.UpdateAndCheck())
				{
					return true;
				}
				UpdatePositions();
				return false;
			}
		}

		public override void ClientOnlyUpdate()
		{
			UpdatePositions();
		}

		protected void UpdatePositions()
		{
			positions.Clear();
			completeList.ForeachNotComplete(updateNotCompletedCached);
		}

		private void UpdateNotCompleted(T target)
		{
			if (TryGetPosition(target, out var position))
			{
				positions.Add(position);
			}
		}

		protected abstract bool TryGetPosition(T item, out ObjectivePosition position);
	}
}
