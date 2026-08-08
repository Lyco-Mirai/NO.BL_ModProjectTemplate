using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using JamesFrowen;
using NuclearOption.MissionEditorScripts.Buttons;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts
{
	public class EditorTabs : MonoBehaviour
	{
		private readonly struct PanelInfo
		{
			public readonly GameObject panel;

			public readonly PanelScrollView scroll;

			public readonly PanelLayout layout;

			private PanelInfo(GameObject panel, PanelScrollView scroll, PanelLayout layout)
			{
				this.panel = panel;
				this.scroll = scroll;
				this.layout = layout;
			}

			public void DestroyPanel()
			{
				if (scroll != null)
				{
					UnityEngine.Object.Destroy(scroll.gameObject);
				}
				else if (panel != null)
				{
					UnityEngine.Object.Destroy(panel);
				}
			}

			public static PanelInfo LeftPanel(GameObject panel, PanelScrollView scroll)
			{
				return new PanelInfo(panel, scroll, PanelLayout.LeftPanel);
			}

			public static PanelInfo Fullscreen(GameObject panel)
			{
				return new PanelInfo(panel, null, PanelLayout.Fullscreen);
			}
		}

		[SerializeField]
		private UnitSelection unitSelection;

		[SerializeField]
		private RectTransform leftPanel;

		[SerializeField]
		private RectTransform fullScreenHolder;

		[SerializeField]
		private PanelScrollView scrollPrefab;

		[Label("canvas group parent to show/hide all panels", true)]
		[SerializeField]
		private CanvasGroup panelsCanvasGroup;

		[SerializeField]
		private HighlightButton mapTabButton;

		private HighlightButton currentTabButton;

		private bool rebuildRequested;

		private bool isHidingTab;

		private List<PanelInfo> panels = new List<PanelInfo>();

		private List<GameObject> enabledMenus = new List<GameObject>();

		public bool IsPanelsVisible => panelsCanvasGroup.alpha > 0f;

		public event Action<bool> OnPanelsVisibilityChanged;

		public bool TryGetOpenTab<T>(out T foundPanel) where T : MonoBehaviour
		{
			foreach (PanelInfo panel in panels)
			{
				if (panel.panel != null)
				{
					T componentInChildren = panel.panel.GetComponentInChildren<T>();
					if (componentInChildren != null)
					{
						foundPanel = componentInChildren;
						return true;
					}
				}
			}
			foundPanel = null;
			return false;
		}

		public void ChangeTabMap(HighlightButton tabButton)
		{
			mapTabButton = tabButton;
			if (SelectOrHideTab(tabButton, clearUnit: false))
			{
				SceneSingleton<DynamicMap>.i.Maximize();
			}
		}

		public void ToggleMapTab()
		{
			if (DynamicMap.mapMaximized)
			{
				HideTab(clearUnit: false);
			}
			else if (SelectOrHideTab(mapTabButton, clearUnit: false))
			{
				SceneSingleton<DynamicMap>.i.Maximize();
			}
		}

		public void ToggleTabPrefab(HighlightButton tabButton, GameObject tabPrefab, bool clearUnit, PanelLayout layout = PanelLayout.LeftPanel)
		{
			if (tabButton != null)
			{
				if (SelectOrHideTab(tabButton, clearUnit))
				{
					AddPanel(tabPrefab, layout);
					ShowLeftPanel();
				}
			}
			else
			{
				HideTab(clearUnit);
				AddPanel(tabPrefab, layout);
				ShowLeftPanel();
			}
		}

		public void ToggleTabOpen(HighlightButton tabButton, GameObject menu)
		{
			if (tabButton != null)
			{
				if (SelectOrHideTab(tabButton, clearUnit: true))
				{
					EnableMenu(menu);
					ShowLeftPanel();
				}
			}
			else
			{
				HideTab(clearUnit: true);
				EnableMenu(menu);
				ShowLeftPanel();
			}
		}

		private void EnableMenu(GameObject menu)
		{
			enabledMenus.Add(menu);
			menu.SetActive(value: true);
		}

		private void DisableAllMenu()
		{
			foreach (GameObject enabledMenu in enabledMenus)
			{
				if (enabledMenu != null)
				{
					enabledMenu.SetActive(value: false);
				}
			}
			enabledMenus.Clear();
		}

		public T ChangeTab<T>(T tabPrefab, bool clearUnit, PanelLayout layout = PanelLayout.LeftPanel) where T : Component
		{
			HideTab(clearUnit);
			T result = AddPanel(tabPrefab, layout);
			ShowLeftPanel();
			return result;
		}

		public GameObject ChangeTab(GameObject tabPrefab, bool clearUnit, PanelLayout layout = PanelLayout.LeftPanel)
		{
			HideTab(clearUnit);
			GameObject result = AddPanel(tabPrefab, layout);
			ShowLeftPanel();
			return result;
		}

		public T AddPanel<T>(T tabPrefab, PanelLayout layout = PanelLayout.LeftPanel) where T : Component
		{
			T val;
			if (layout == PanelLayout.Fullscreen)
			{
				val = UnityEngine.Object.Instantiate(tabPrefab, fullScreenHolder);
				panels.Add(PanelInfo.Fullscreen(val.gameObject));
			}
			else
			{
				PanelScrollView panelScrollView = UnityEngine.Object.Instantiate(scrollPrefab, leftPanel);
				val = panelScrollView.AddChild(tabPrefab);
				panels.Add(PanelInfo.LeftPanel(val.gameObject, panelScrollView));
			}
			RequestRebuild();
			return val;
		}

		public GameObject AddPanel(GameObject tabPrefab, PanelLayout layout = PanelLayout.LeftPanel)
		{
			GameObject gameObject;
			if (layout == PanelLayout.Fullscreen)
			{
				gameObject = UnityEngine.Object.Instantiate(tabPrefab, fullScreenHolder);
				panels.Add(PanelInfo.Fullscreen(gameObject));
			}
			else
			{
				PanelScrollView panelScrollView = UnityEngine.Object.Instantiate(scrollPrefab, leftPanel);
				gameObject = panelScrollView.AddChild(tabPrefab);
				panels.Add(PanelInfo.LeftPanel(gameObject, panelScrollView));
			}
			RequestRebuild();
			return gameObject;
		}

		public void DestroyPanel(GameObject key)
		{
			if (isHidingTab)
			{
				return;
			}
			for (int i = 0; i < panels.Count; i++)
			{
				if (panels[i].panel == key)
				{
					panels[i].DestroyPanel();
					panels.RemoveAt(i);
					return;
				}
			}
			Debug.LogError("Trying to destroy a panel, but key was not in panels List");
		}

		public void RequestRebuild()
		{
			if (!rebuildRequested)
			{
				RebuildAtEndOfFrame().Forget();
			}
		}

		private async UniTask RebuildAtEndOfFrame()
		{
			rebuildRequested = true;
			await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);
			if (this == null)
			{
				return;
			}
			rebuildRequested = false;
			foreach (PanelInfo panel in panels)
			{
				if (panel.scroll != null)
				{
					panel.scroll.Rebuild();
				}
			}
			LayoutRebuilder.ForceRebuildLayoutImmediate(leftPanel);
		}

		private bool SelectOrHideTab(HighlightButton tabButton, bool clearUnit)
		{
			if (currentTabButton != null && currentTabButton == tabButton)
			{
				if (!IsPanelsVisible)
				{
					ShowLeftPanel();
					return false;
				}
				HideTab(clearUnit);
				return false;
			}
			HideTab(clearUnit);
			currentTabButton = tabButton;
			if (currentTabButton != null)
			{
				currentTabButton.Highlight(highlight: true);
			}
			return true;
		}

		public void HideTab(bool clearUnit)
		{
			if (isHidingTab)
			{
				return;
			}
			isHidingTab = true;
			try
			{
				List<PanelInfo> list = new List<PanelInfo>(panels);
				panels.Clear();
				foreach (PanelInfo item in list)
				{
					item.DestroyPanel();
				}
			}
			finally
			{
				isHidingTab = false;
			}
			if (currentTabButton != null)
			{
				currentTabButton.Highlight(highlight: false);
				currentTabButton = null;
			}
			DisableAllMenu();
			if (SceneSingleton<DynamicMap>.i != null && DynamicMap.mapMaximized)
			{
				SceneSingleton<DynamicMap>.i.Minimize();
			}
			if (clearUnit)
			{
				unitSelection.ClearSelection();
			}
			RequestRebuild();
		}

		public void ToggleLeftPanel()
		{
			if (panels.Count != 0 || enabledMenus.Count != 0)
			{
				SetLeftPanelVisible(panelsCanvasGroup.alpha <= 0f);
			}
		}

		private void ShowLeftPanel()
		{
			SetLeftPanelVisible(visible: true);
		}

		private void SetLeftPanelVisible(bool visible)
		{
			panelsCanvasGroup.alpha = (visible ? 1f : 0f);
			panelsCanvasGroup.interactable = visible;
			panelsCanvasGroup.blocksRaycasts = visible;
			this.OnPanelsVisibilityChanged?.Invoke(visible);
			if (visible)
			{
				RequestRebuild();
			}
		}
	}
}
