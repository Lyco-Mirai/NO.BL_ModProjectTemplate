using System;
using System.Collections.Generic;

namespace NuclearOption.SavedMission.Objectives
{
	public interface IObjectiveList<T>
	{
		float GetCompletePercent();

		void ReadNetworkData(List<int> data);

		List<int> UpdateNetworkList();

		bool UpdateAndCheck(CheckCallback<T> checkCallback);

		void ForeachNotComplete(Action<T> item);
	}
}
