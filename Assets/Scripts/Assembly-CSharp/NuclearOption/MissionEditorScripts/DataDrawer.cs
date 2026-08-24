using System;
using System.Collections.Generic;
using NuclearOption.SavedMission;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts
{
	public class DataDrawer
	{
		private readonly RectTransform mainParent;

		private readonly UIPrefabs prefabs;

		private bool addedGroupBox;

		public bool Success;

		private readonly List<IValueWrapper> TrackedChanges = new List<IValueWrapper>();

		private Stack<RectTransform> parentStack = new Stack<RectTransform>();

		public float? Width;

		public PanelDrawOptions Options { get; set; }

		public RectTransform Parent => parentStack.Peek();

		public UIPrefabs Prefabs => prefabs;

		public event Action PanelAfterEdit;

		public event Action TrackedValueChanged;

		public DataDrawer(RectTransform parent, UIPrefabs prefabs)
		{
			parentStack.Push(parent);
			mainParent = parent;
			this.prefabs = prefabs;
		}

		public void Cleanup()
		{
			this.PanelAfterEdit = null;
			this.TrackedValueChanged = null;
			foreach (IValueWrapper trackedChange in TrackedChanges)
			{
				trackedChange.UnregisterOnChange(this);
			}
			TrackedChanges.Clear();
		}

		public void Reset()
		{
			Cleanup();
			while (parentStack.Peek() != mainParent)
			{
				UnityEngine.Object.Destroy(parentStack.Pop().gameObject);
			}
			Success = false;
			addedGroupBox = false;
		}

		public void InvokeAfterEdit()
		{
			this.PanelAfterEdit?.Invoke();
		}

		public void CheckGroupBox()
		{
			if (!addedGroupBox)
			{
				addedGroupBox = true;
				Success = true;
				RectTransform rectTransform = UnityEngine.Object.Instantiate(prefabs.GroupBoxPrefab, Parent);
				parentStack.Push((RectTransform)rectTransform.transform);
				if (Width.HasValue)
				{
					Width -= prefabs.GroupBoxPadding;
				}
			}
		}

		private void GroupInternal<T>(T prefab, Action<T> inner) where T : Component
		{
			CheckGroupBox();
			T val = UnityEngine.Object.Instantiate(prefab, Parent);
			parentStack.Push((RectTransform)val.transform);
			inner(val);
			parentStack.Pop();
		}

		public void HorizontalGroup(Action<HorizontalLayoutGroup> inner)
		{
			GroupInternal(prefabs.HorizontalGroupPrefab, inner);
		}

		public void VerticalGroup(Action<VerticalLayoutGroup> inner)
		{
			GroupInternal(prefabs.VerticalGroupPrefab, inner);
		}

		public void Nothing()
		{
			Success = true;
		}

		public void Space(int height)
		{
			RectTransform obj = (RectTransform)new GameObject("spacer", typeof(RectTransform)).transform;
			obj.SetParent(Parent);
			obj.sizeDelta = new Vector2(10f, height);
		}

		public ReferenceList DrawList<T>(int height, string listTitle, string dropdownTitle, List<T> list, Func<T, string> toDropdownString, Func<T, string> toListString, Func<IEnumerable<T>> getAllOptions, Action<int> readonlyListEditCallback = null) where T : ISaveableReference
		{
			ReferenceList referenceList = InstantiateWithParent(prefabs.ReferenceListPrefab);
			referenceList.TitleText.text = listTitle;
			referenceList.SelectExistingDropdown.SetTitle(dropdownTitle);
			referenceList.SetHeight(height);
			if (readonlyListEditCallback != null)
			{
				referenceList.Setup(null, null, readonlyListEditCallback, null);
				referenceList.SetupList(list, toDropdownString, toListString, getAllOptions, ReferenceList.ButtonsEvents.DontAdd);
			}
			else
			{
				referenceList.SetupList(list, toDropdownString, toListString, getAllOptions, ReferenceList.ButtonsEvents.OverrideNullOnly);
			}
			Success = true;
			return referenceList;
		}

		public EmptyDataList DrawList<T>(int height, List<T> list, DrawInnerData<T> drawContent) where T : class, new()
		{
			return DrawList(height, list, drawContent, () => new T());
		}

		public EmptyDataList DrawList<T>(int height, List<T> list, DrawInnerData<T> drawContent, Func<T> createNew, Action<T> onDelete = null)
		{
			EmptyDataList controller = InstantiateWithParent(prefabs.DataListPrefab);
			controller.SetHeight(height);
			controller.Setup(CreateNew, null, null, Delete);
			Refresh();
			return controller;
			void CreateNew()
			{
				list.Add(createNew());
				Refresh();
			}
			void Delete(int index)
			{
				onDelete?.Invoke(list[index]);
				list.RemoveAt(index);
				Refresh();
			}
			void DrawItem(int index, T value, RectTransform parent)
			{
				parentStack.Push(parent);
				drawContent(index, value, this);
				parentStack.Pop();
			}
			void Refresh()
			{
				controller.UpdateList(list, DrawItem);
			}
		}

		public DropdownDataField DrawEnum<T>(string label, int value, Action<int> setValue) where T : struct, Enum
		{
			DropdownDataField dropdownDataField = DrawDropdown(label, EnumNames<T>.GetNames(), value, setValue);
			dropdownDataField.LabelWidth(160);
			return dropdownDataField;
		}

		public DropdownDataField DrawDropdown(string label, List<string> options, string value, Action<string> setValue)
		{
			DropdownDataField dropdownDataField = InstantiateWithParent(prefabs.Dropdown);
			dropdownDataField.Setup(label, options, value, setValue);
			return dropdownDataField;
		}

		public DropdownDataField DrawDropdown(string label, List<string> options, int value, Action<int> setValue)
		{
			DropdownDataField dropdownDataField = InstantiateWithParent(prefabs.Dropdown);
			dropdownDataField.Setup(label, options, value, setValue);
			return dropdownDataField;
		}

		public (OverrideDataField, TDataField) DrawOverride<T, TDataField>(string label, ValueWrapperOverride<T> wrapper, TDataField dataFieldPrefab) where T : IEquatable<T> where TDataField : DataField, IDataField<T>
		{
			OverrideDataField overrideDataField = InstantiateWithParent(Prefabs.OverrideField);
			TDataField val = UnityEngine.Object.Instantiate(dataFieldPrefab, overrideDataField.innerHolder);
			val.Setup(label, wrapper);
			overrideDataField.Setup(wrapper, val);
			return (overrideDataField, val);
		}

		public T InstantiateWithParent<T>(T prefab) where T : Component
		{
			CheckGroupBox();
			T val = UnityEngine.Object.Instantiate(prefab, Parent);
			if (Width.HasValue)
			{
				val.SetRectWidth(Width.Value);
			}
			return val;
		}

		public TextMeshProUGUI DrawHeader(string text, int spaceBefore = 16, int spaceAfter = 10)
		{
			Space(spaceBefore);
			TextMeshProUGUI textMeshProUGUI = InstantiateWithParent(Prefabs.TextPrefab);
			textMeshProUGUI.text = text;
			textMeshProUGUI.alignment = TextAlignmentOptions.Left;
			textMeshProUGUI.fontWeight = FontWeight.Bold;
			textMeshProUGUI.fontSize *= 1.3f;
			Space(spaceAfter);
			return textMeshProUGUI;
		}

		public TextMeshProUGUI DrawLabel(string text)
		{
			TextMeshProUGUI textMeshProUGUI = InstantiateWithParent(Prefabs.TextPrefab);
			textMeshProUGUI.text = text;
			textMeshProUGUI.alignment = TextAlignmentOptions.Left;
			return textMeshProUGUI;
		}

		public TextMeshProUGUI DrawLabelWarning(string text)
		{
			TextMeshProUGUI textMeshProUGUI = InstantiateWithParent(Prefabs.TextPrefab);
			textMeshProUGUI.text = text;
			textMeshProUGUI.alignment = TextAlignmentOptions.Left;
			textMeshProUGUI.color = Color.yellow;
			return textMeshProUGUI;
		}

		public TextMeshProUGUI DrawLabelError(string text)
		{
			TextMeshProUGUI textMeshProUGUI = InstantiateWithParent(Prefabs.TextPrefab);
			textMeshProUGUI.text = text;
			textMeshProUGUI.alignment = TextAlignmentOptions.Left;
			textMeshProUGUI.color = Color.red;
			return textMeshProUGUI;
		}

		public void TrackChanges(IValueWrapper valueWrapper)
		{
			TrackedChanges.Add(valueWrapper);
			valueWrapper.RegisterOnChange(this, delegate
			{
				this.TrackedValueChanged?.Invoke();
			});
		}
	}
}
