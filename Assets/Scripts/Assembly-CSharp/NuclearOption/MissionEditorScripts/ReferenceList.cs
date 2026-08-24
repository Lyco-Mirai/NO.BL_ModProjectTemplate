using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NuclearOption.SavedMission;
using UnityEngine;

namespace NuclearOption.MissionEditorScripts
{
	public class ReferenceList : ListControllerWithButtonsBase<ItemWithButtonsWrapper>
	{
		public enum ButtonsEvents
		{
			OverrideNullOnly = 0,
			DontAdd = 1,
			Override = 2
		}

		public class ListWrapper : IEnumerable<ISaveableReference>, IEnumerable
		{
			public readonly IList Inner;

			public ISaveableReference this[int index]
			{
				get
				{
					return (ISaveableReference)Inner[index];
				}
				set
				{
					ISaveableReference saveableReference = Inner[index] as ISaveableReference;
					Inner[index] = value;
					if (saveableReference != null)
					{
						this.ItemRemoved?.Invoke(saveableReference);
					}
					if (value != null)
					{
						this.ItemAdded?.Invoke(value);
					}
				}
			}

			public int Count => Inner.Count;

			public event Action<ISaveableReference> ItemAdded;

			public event Action<ISaveableReference> ItemRemoved;

			public ListWrapper(IList inner)
			{
				Inner = inner;
			}

			public bool Contains(ISaveableReference obj)
			{
				for (int i = 0; i < Inner.Count; i++)
				{
					if (((ISaveableReference)Inner[i]).UniqueName == obj.UniqueName)
					{
						return true;
					}
				}
				return false;
			}

			public void Add(ISaveableReference item)
			{
				Inner.Add(item);
				if (item != null)
				{
					this.ItemAdded?.Invoke(item);
				}
			}

			public void RemoveAt(int i)
			{
				ISaveableReference saveableReference = Inner[i] as ISaveableReference;
				Inner.RemoveAt(i);
				if (saveableReference != null)
				{
					this.ItemRemoved?.Invoke(saveableReference);
				}
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return Inner.GetEnumerator();
			}

			public IEnumerator<ISaveableReference> GetEnumerator()
			{
				return Inner.Cast<ISaveableReference>().GetEnumerator();
			}
		}

		[Header("Select Dropdown")]
		public ReferencePopup SelectExistingDropdown;

		private ReferenceToString toDropdownString;

		private ReferenceToString toListString;

		private GetAllOptions getAllOptions;

		public readonly FilterSet FilterSet = new FilterSet();

		public string EditButtonText;

		public string DeleteButtonText;

		private bool dropdownOpen;

		private ListWrapper dataList;

		public event Action<ISaveableReference> ItemAdded;

		public event Action<ISaveableReference> ItemRemoved;

		protected override IList GetDataList()
		{
			return dataList?.Inner;
		}

		protected override void Awake()
		{
			base.Awake();
			FilterSet.OnFilterChanged += RefreshList;
			SelectExistingDropdown.Hide();
			SelectExistingDropdown.FilterSet.Apply("InRefList", (object obj) => !dataList.Contains((ISaveableReference)obj));
		}

		public void SetupList<T>(List<T> data, Func<T, string> toListString) where T : ISaveableReference
		{
			SetupListInternal(data, null, (ISaveableReference o) => toListString((T)o), null, ButtonsEvents.DontAdd);
		}

		public void SetupList<T>(List<T> data, Func<T, string> toListString, ButtonsEvents addButtonEvents) where T : ISaveableReference
		{
			SetupList(data, null, toListString, null, addButtonEvents);
		}

		public void SetupList<T>(List<T> data, Func<T, string> toDropdownString, Func<T, string> toListString, Func<IEnumerable<T>> getAllOptions, ButtonsEvents addButtonEvents) where T : ISaveableReference
		{
			SetupListInternal(data, (ISaveableReference o) => toDropdownString((T)o), (ISaveableReference o) => toListString((T)o), () => getAllOptions().Cast<ISaveableReference>(), addButtonEvents);
		}

		private void SetupListInternal(IList data, ReferenceToString toDropdownString, ReferenceToString toListString, GetAllOptions getAllOptions, ButtonsEvents addButtonEvents)
		{
			dataList = new ListWrapper(data);
			dataList.ItemAdded += delegate(ISaveableReference i)
			{
				this.ItemAdded?.Invoke(i);
			};
			dataList.ItemRemoved += delegate(ISaveableReference i)
			{
				this.ItemRemoved?.Invoke(i);
			};
			this.toListString = toListString;
			this.toDropdownString = toDropdownString;
			this.getAllOptions = getAllOptions;
			addSelectedClicked = null;
			selectAssignedClicked = null;
			if (data is List<SavedUnit>)
			{
				addSelectedClicked = AddSelectedUnits;
				selectAssignedClicked = SelectAssignedUnits;
			}
			switch (addButtonEvents)
			{
			case ButtonsEvents.OverrideNullOnly:
				base.Setup(newClicked ?? null, addClicked ?? new Action(AddExisting), editClicked ?? new Action<int>(EditItem), deleteClicked ?? new Action<int>(RemoveItem), addSelectedClicked, selectAssignedClicked);
				break;
			case ButtonsEvents.Override:
				base.Setup(null, AddExisting, EditItem, RemoveItem, addSelectedClicked, selectAssignedClicked);
				break;
			default:
				base.Setup(newClicked, addClicked, editClicked, deleteClicked, addSelectedClicked, selectAssignedClicked);
				break;
			}
			RefreshList();
		}

		public void Clear()
		{
			dataList = null;
			toListString = null;
			toDropdownString = null;
			getAllOptions = null;
			base.Setup(null, null, null, null);
			UpdateListEmpty();
		}

		public override void RefreshList()
		{
			if (dataList != null)
			{
				UpdateList(dataList, toListString);
			}
		}

		public void UpdateList<T>(List<T> list, ReferenceToString toString) where T : ISaveableReference
		{
			UpdateList(new ListWrapper(list), toString);
		}

		public void UpdateList(ListWrapper listWrapper, ReferenceToString toString)
		{
			wrappers.Clear();
			bool flag = FilterSet.Count == 0 || listWrapper.Where((ISaveableReference item) => item.CanBeSorted).All((ISaveableReference item) => FilterSet.FilterItem(item));
			int count = listWrapper.Count;
			for (int num = 0; num < count; num++)
			{
				ISaveableReference saveableReference = listWrapper[num];
				bool flag2 = FilterSet.FilterItem(saveableReference);
				string text = null;
				if (flag2)
				{
					text = toString(saveableReference);
				}
				Action<int> action = (saveableReference.CanBeReference ? deleteClicked : null);
				MoveAction moveClicked = ((flag && saveableReference.CanBeSorted) ? new MoveAction(SwapItems) : null);
				wrappers.Add(new ItemWithButtonsWrapper(flag2, text, num, count, editClicked, action, moveClicked, EditButtonText, DeleteButtonText));
			}
			UpdateListFromWrapper();
		}

		protected override void SwapItems(int from, int to)
		{
			if (dataList[from].CanBeSorted && dataList[to].CanBeSorted)
			{
				base.SwapItems(from, to);
			}
		}

		public void UpdateListEmpty()
		{
			wrappers.Clear();
			UpdateListFromWrapper();
		}

		private void AddExisting()
		{
			if (dropdownOpen)
			{
				Debug.LogWarning("Dropdown already open");
				return;
			}
			PickOption(null, delegate(ISaveableReference item)
			{
				dataList.Add(item);
			});
		}

		private void AddSelectedUnits()
		{
			if (dataList == null || SceneSingleton<UnitSelection>.i == null)
			{
				return;
			}
			bool flag = false;
			foreach (SavedUnit selectedSavedUnit in SceneSingleton<UnitSelection>.i.GetSelectedSavedUnits())
			{
				if (selectedSavedUnit != null && !dataList.Contains(selectedSavedUnit))
				{
					dataList.Add(selectedSavedUnit);
					flag = true;
				}
			}
			if (flag)
			{
				RefreshList();
			}
		}

		private void SelectAssignedUnits()
		{
			if (dataList != null && !(SceneSingleton<UnitSelection>.i == null))
			{
				List<Unit> objects = (from savedUnit in dataList.OfType<SavedUnit>()
					where savedUnit.Unit != null
					select savedUnit.Unit).ToList();
				SceneSingleton<UnitSelection>.i.ReplaceSelection(objects);
			}
		}

		private void EditItem(int i)
		{
			if (dropdownOpen)
			{
				Debug.LogWarning("Dropdown already open");
				return;
			}
			PickOption(dataList[i], delegate(ISaveableReference item)
			{
				dataList[i] = item;
			});
		}

		private void RemoveItem(int i)
		{
			dataList.RemoveAt(i);
			RefreshList();
		}

		private void PickOption(ISaveableReference startingOption, Action<ISaveableReference> onPick)
		{
			dropdownOpen = true;
			SelectExistingDropdown.ShowPickOption(startingOption, allowNone: false, getAllOptions, toDropdownString, delegate(bool pick, ISaveableReference obj)
			{
				dropdownOpen = false;
				if (pick)
				{
					onPick(obj);
					RefreshList();
				}
			});
		}
	}
}
