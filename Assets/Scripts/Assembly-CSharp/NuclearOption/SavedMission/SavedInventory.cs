using System;
using System.Collections.Generic;
using UnityEngine;

namespace NuclearOption.SavedMission
{
	[Serializable]
	public class SavedInventory
	{
		public List<UnitCount> StoredList = new List<UnitCount>();

		public void Transfer(SavedInventory fromInventory)
		{
			foreach (UnitCount stored in fromInventory.StoredList)
			{
				if (stored.Count == 0)
				{
					continue;
				}
				bool flag = false;
				for (int num = StoredList.Count - 1; num >= 0; num--)
				{
					UnitCount unitCount = StoredList[num];
					if (unitCount.UnitType == stored.UnitType)
					{
						flag = true;
						unitCount.Count += stored.Count;
					}
				}
				if (!flag)
				{
					StoredList.Add(new UnitCount(stored.UnitType, stored.Count));
				}
			}
			fromInventory.Clear();
		}

		public void AddOrRemove(UnitDefinition unitDefinition, int number)
		{
			if (StoredList == null && number > 0)
			{
				StoredList = new List<UnitCount>();
			}
			bool flag = false;
			for (int num = StoredList.Count - 1; num >= 0; num--)
			{
				UnitCount unitCount = StoredList[num];
				if (unitCount.UnitType == unitDefinition.jsonKey)
				{
					flag = true;
					unitCount.Count += number;
					if (unitCount.Count <= 0)
					{
						StoredList.RemoveAt(num);
					}
					break;
				}
			}
			if (!flag && number > 0)
			{
				Debug.Log($"[UnitStorage] adding entry for {number} {unitDefinition.unitName} to inventory stored list");
				StoredList.Add(new UnitCount(unitDefinition.jsonKey, number));
			}
		}

		public void Clear()
		{
			StoredList.Clear();
		}
	}
}
