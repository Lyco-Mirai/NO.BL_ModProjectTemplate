using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts
{
	public class NavbarTabs : MonoBehaviour
	{
		[Serializable]
		public class TabMenu
		{
			public GameObject Tab;

			public Button Button;

			public ButtonStyleController StyleController;
		}

		[Header("Button Colors")]
		[SerializeField]
		private ButtonStyle activeStyle;

		[SerializeField]
		private ButtonStyle inactiveStyle;

		[Header("Tabs")]
		[SerializeField]
		private int activeIndex;

		[FormerlySerializedAs("Tabs")]
		[SerializeField]
		private TabMenu[] tabs;

		private void Awake()
		{
			for (int i = 0; i < tabs.Length; i++)
			{
				int index = i;
				if (tabs[i].Button != null)
				{
					tabs[i].Button.onClick.AddListener(delegate
					{
						SelectTab(index);
					});
				}
			}
		}

		private void OnValidate()
		{
			if (tabs != null && tabs.Length != 0)
			{
				activeIndex = Mathf.Clamp(activeIndex, 0, tabs.Length - 1);
				SelectTab(activeIndex);
			}
		}

		public TabMenu SelectTab(int index)
		{
			activeIndex = index;
			for (int i = 0; i < tabs.Length; i++)
			{
				SetActive(tabs[i], i == index);
			}
			return tabs[index];
		}

		public void SetActive(TabMenu tab, bool active)
		{
			if (tab.Tab != null)
			{
				tab.Tab.SetActive(active);
			}
			if (tab.StyleController != null)
			{
				tab.StyleController.ApplyStyle(active ? activeStyle : inactiveStyle);
			}
			if (active)
			{
				FixLayout.RebuildRootEndOfFrame(tab.Tab);
			}
		}
	}
}
