using System;
using System.Collections.Generic;
using NuclearOption.MissionEditorScripts;

namespace NuclearOption.SavedMission.Objectives
{
	public class CompleteOrderPercentWrapper
	{
		public Action<CompleteOrder> setEnum;

		public DropdownDataField orderDropdown;

		public FloatDataField completePercent;

		private void SetEnum(int _value)
		{
			setEnum((CompleteOrder)_value);
			CheckShowPercent((CompleteOrder)_value);
		}

		private void CheckShowPercent(CompleteOrder order)
		{
			bool flag = order == CompleteOrder.CompleteSome;
			if (completePercent.gameObject.activeSelf != flag)
			{
				completePercent.gameObject.SetActive(flag);
				FixLayout.RebuildRoot(completePercent.gameObject);
			}
		}

		public static CompleteOrderPercentWrapper Create(DataDrawer drawer, CompleteOrder completeOrder, Action<CompleteOrder> setEnum, ValueWrapperFloat completeSomePercent, List<CompleteOrder> completeOrderOptions = null)
		{
			CompleteOrderPercentWrapper i = new CompleteOrderPercentWrapper();
			i.setEnum = setEnum;
			if (completeOrderOptions != null)
			{
				List<string> list = new List<string>(completeOrderOptions.Count);
				foreach (CompleteOrder completeOrderOption in completeOrderOptions)
				{
					list.Add(completeOrderOption.ToNicifyString());
				}
				int num = completeOrderOptions.IndexOf(completeOrder);
				if (num == -1)
				{
					num = 0;
				}
				i.orderDropdown = drawer.DrawDropdown("Complete Order", list, num, delegate(int index)
				{
					CompleteOrder completeOrder2 = completeOrderOptions[index];
					i.SetEnum((int)completeOrder2);
				});
			}
			else
			{
				i.orderDropdown = drawer.DrawEnum<CompleteOrder>("Complete Order", (int)completeOrder, i.SetEnum);
			}
			i.completePercent = drawer.InstantiateWithParent(drawer.Prefabs.FloatFieldPrefab);
			FloatDataField.FloatSettings value = new FloatDataField.FloatSettings
			{
				Slider = new FloatDataField.FloatSlider
				{
					Min = 0f,
					Max = 1f
				},
				TextFormat = "P1"
			};
			i.completePercent.Setup("Complete Percent", completeSomePercent, value);
			i.CheckShowPercent(completeOrder);
			return i;
		}
	}
}
