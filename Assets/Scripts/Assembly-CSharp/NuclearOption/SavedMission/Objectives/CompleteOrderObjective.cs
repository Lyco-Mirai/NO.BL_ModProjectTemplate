using System.Collections.Generic;
using Unity.Profiling;

namespace NuclearOption.SavedMission.Objectives
{
	public abstract class CompleteOrderObjective<T> : Objective
	{
		private static readonly ProfilerMarker updateAndCheckMarker = new ProfilerMarker("CompleteOrderObjectiveUpdateAndCheck");

		protected IObjectiveList<T> completeList;

		protected List<T> allItems;

		protected CompleteOrder completeOrder;

		protected readonly ValueWrapperFloat completeSomePercent = new ValueWrapperFloat();

		private readonly CheckCallback<T> checkCallbackCached;

		protected virtual bool AllCompleteAtOnce { get; }

		public override float CompletePercent => completeList.GetCompletePercent();

		public override void CopyFrom(Objective original)
		{
			base.CopyFrom(original);
			CompleteOrderObjective<T> completeOrderObjective = (CompleteOrderObjective<T>)original;
			completeOrder = completeOrderObjective.completeOrder;
			completeSomePercent.SetValue(completeOrderObjective.completeSomePercent.Value, this);
			if (completeOrderObjective.allItems != null)
			{
				allItems = new List<T>(completeOrderObjective.allItems);
			}
		}

		public CompleteOrderObjective(SavedObjective savedObjective)
			: base(savedObjective)
		{
			checkCallbackCached = CheckComplete;
		}

		public override void ReceiveNetworkData(List<int> data)
		{
			completeList.ReadNetworkData(data);
		}

		public override void OnStart()
		{
			if (completeOrder == CompleteOrder.CompleteAll && AllCompleteAtOnce)
			{
				completeList = new CompleteAllList<T>(allItems, UpdateNetworkData);
			}
			else
			{
				completeList = new CompleteOrderList<T>(allItems, completeOrder, completeSomePercent, UpdateNetworkData);
			}
			UpdateNetworkData();
		}

		private void UpdateNetworkData()
		{
			using (updateAndCheckMarker.Auto())
			{
				if (MissionManager.IsServer)
				{
					MissionManager.UpdateNetworkData(this, completeList.UpdateNetworkList());
				}
			}
		}

		public override bool UpdateAndCheck()
		{
			return completeList.UpdateAndCheck(checkCallbackCached);
		}

		protected abstract bool CheckComplete(T toCheck);
	}
}
