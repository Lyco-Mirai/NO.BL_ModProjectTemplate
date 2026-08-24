using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts
{
	public class FileMenu : MonoBehaviour, ILayoutRebuildRoot
	{
		public enum TabIndex
		{
			New = 0,
			Load = 1,
			Save = 2,
			Settings = 3
		}

		public NavbarTabs Tabs;

		public MissionEditorNewMenuV2 NewMenu;

		public MissionEditorLoadMenuV2 LoadMenu;

		public MissionEditorSaveMenuV2 SaveMenu;

		public MissionEditorSettingsMenu SettingsMenu;

		[SerializeField]
		private Button closeButton;

		private void Awake()
		{
			if (closeButton != null)
			{
				closeButton.onClick.AddListener(Close);
			}
		}

		public void Show(TabIndex index)
		{
			base.gameObject.SetActive(value: true);
			Tabs.SelectTab((int)index);
			FixLayout.ForceRebuildRecursive(base.transform.AsRectTransform());
		}

		public void Close()
		{
			base.gameObject.SetActive(value: false);
		}

		void ILayoutRebuildRoot.Rebuild()
		{
			FixLayout.ForceRebuildRecursive(base.transform.AsRectTransform());
		}

		void ILayoutRebuildRoot.RebuildEndOfFrame()
		{
			FixLayout.ForceRebuildAtEndOfFrame(base.transform.AsRectTransform());
		}
	}
}
