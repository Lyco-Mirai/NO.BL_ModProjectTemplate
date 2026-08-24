using System;
using System.Collections;
using System.Collections.Generic;
using JamesFrowen.ScriptableVariables.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts
{
	public abstract class ListControllerWithButtonsBase<TWrapper> : ListController<TWrapper>
	{
		private enum ButtonInList
		{
			None = 0,
			AtStart = 1,
			AtEnd = 2
		}

		[Header("Extra layout")]
		[Tooltip("Use _layoutRoot if _parent is not the root layout for content")]
		[SerializeField]
		protected RectTransform _layoutRoot;

		[Header("Create New")]
		[SerializeField]
		private Button createNewButton;

		[SerializeField]
		private ButtonInList createPlacement;

		[Header("Add Existing")]
		[SerializeField]
		private Button addExistingButton;

		[SerializeField]
		private ButtonInList addPlacement;

		[Header("Add Selected")]
		[SerializeField]
		private Button addSelectedButton;

		[SerializeField]
		private ButtonInList addSelectedPlacement;

		[Header("Select Assigned")]
		[SerializeField]
		private Button selectAssignedButton;

		[SerializeField]
		private ButtonInList selectAssignedPlacement;

		[Header("Title")]
		[SerializeField]
		public TextMeshProUGUI TitleText;

		[SerializeField]
		private RectTransform listParent;

		protected Action newClicked;

		protected Action addClicked;

		protected Action addSelectedClicked;

		protected Action selectAssignedClicked;

		protected Action<int> editClicked;

		protected Action<int> deleteClicked;

		protected List<TWrapper> wrappers = new List<TWrapper>();

		protected abstract IList GetDataList();

		public void SetHeight(int listPanelHeight)
		{
			Vector2 sizeDelta = listParent.sizeDelta;
			sizeDelta.y = listPanelHeight;
			listParent.sizeDelta = sizeDelta;
		}

		protected override void Awake()
		{
			base.Awake();
			if (_layoutRoot == null)
			{
				_layoutRoot = _parent;
			}
			if (createNewButton != null)
			{
				createNewButton.onClick.AddListener(CallNewClicked);
				createNewButton.gameObject.SetActive(value: false);
			}
			if (addExistingButton != null)
			{
				addExistingButton.onClick.AddListener(CallAddClicked);
				addExistingButton.gameObject.SetActive(value: false);
			}
			if (addSelectedButton != null)
			{
				addSelectedButton.onClick.AddListener(CallAddSelectedClicked);
				addSelectedButton.gameObject.SetActive(value: false);
			}
			if (selectAssignedButton != null)
			{
				selectAssignedButton.onClick.AddListener(CallSelectAssignedClicked);
				selectAssignedButton.gameObject.SetActive(value: false);
			}
		}

		private void CallNewClicked()
		{
			newClicked?.Invoke();
		}

		private void CallAddClicked()
		{
			addClicked?.Invoke();
		}

		private void CallAddSelectedClicked()
		{
			addSelectedClicked?.Invoke();
		}

		private void CallSelectAssignedClicked()
		{
			selectAssignedClicked?.Invoke();
		}

		public virtual void Setup(Action newClicked, Action addClicked, Action<int> editClicked, Action<int> deleteClicked, Action addSelectedClicked = null, Action selectAssignedClicked = null)
		{
			this.newClicked = newClicked;
			this.addClicked = addClicked;
			this.editClicked = editClicked;
			this.deleteClicked = deleteClicked;
			this.addSelectedClicked = addSelectedClicked;
			this.selectAssignedClicked = selectAssignedClicked;
			if (createNewButton != null)
			{
				createNewButton.gameObject.SetActive(newClicked != null);
			}
			if (addExistingButton != null)
			{
				addExistingButton.gameObject.SetActive(addClicked != null);
			}
			if (addSelectedButton != null)
			{
				addSelectedButton.gameObject.SetActive(addSelectedClicked != null);
			}
			if (selectAssignedButton != null)
			{
				selectAssignedButton.gameObject.SetActive(selectAssignedClicked != null);
			}
		}

		protected void UpdateListFromWrapper()
		{
			UpdateList(wrappers);
			RebuildLayout();
		}

		protected override void RebuildLayout()
		{
			if (createNewButton != null)
			{
				MoveButton(createNewButton, createPlacement);
			}
			if (addExistingButton != null)
			{
				MoveButton(addExistingButton, addPlacement);
			}
			if (addSelectedButton != null)
			{
				MoveButton(addSelectedButton, addSelectedPlacement);
			}
			if (selectAssignedButton != null)
			{
				MoveButton(selectAssignedButton, selectAssignedPlacement);
			}
			FixLayout.ForceRebuildAtEndOfFrame(_layoutRoot);
		}

		private static void MoveButton(Button button, ButtonInList placement)
		{
			switch (placement)
			{
			case ButtonInList.AtStart:
				((RectTransform)button.transform).SetAsFirstSibling();
				break;
			case ButtonInList.AtEnd:
				((RectTransform)button.transform).SetAsLastSibling();
				break;
			}
		}

		protected virtual void SwapItems(int from, int to)
		{
			IList dataList;
			IList list = (dataList = GetDataList());
			IList list2 = list;
			object obj = list[from];
			object obj2 = list[to];
			object obj3 = (dataList[to] = obj);
			obj3 = (list2[from] = obj2);
			RefreshList();
		}

		public abstract void RefreshList();
	}
}
