using System;
using System.Collections.Generic;
using NuclearOption.MissionEditorScripts;
using NuclearOption.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace NuclearOption.NodeGraph
{
	public class GraphContextMenu : RightClickDropdownMenuBase
	{
		[Header("Serialized References")]
		[SerializeField]
		private TMP_InputField searchInputField;

		[SerializeField]
		private RectTransform categoriesContainer;

		[SerializeField]
		private TextMeshProUGUI headerText;

		[Header("Scroll Constraints")]
		[SerializeField]
		private ScrollRect scrollRect;

		[SerializeField]
		private LayoutElement scrollRectLayout;

		[SerializeField]
		private float headerHeight = 40f;

		[SerializeField]
		private float minMenuHeight = 200f;

		[SerializeField]
		private float maxMenuHeight = 400f;

		[SerializeField]
		private float screenPadding = 10f;

		[Header("Prefabs")]
		[SerializeField]
		private GraphContextMenuCategory categoryPrefab;

		[SerializeField]
		private GraphContextMenuItem itemButtonPrefab;

		private ContextMenuOpenSource contextMenuArgs;

		private Vector2 spawnPosition;

		private string activeFilter;

		private readonly Dictionary<string, ContextMenuCategory> categories = new Dictionary<string, ContextMenuCategory>(StringComparer.OrdinalIgnoreCase);

		private readonly Dictionary<ContextMenuOption, GameObject> optionButtons = new Dictionary<ContextMenuOption, GameObject>();

		private readonly List<GameObject> _spawnedItems = new List<GameObject>();

		private Vector2 _lastLoggedPosition;

		private Vector2 _lastLoggedSize;

		public Vector2 SpawnPosition => spawnPosition;

		public ContextMenuOpenSource ContextMenuArgs => contextMenuArgs;

		private void OnValidate()
		{
		}

		protected override void Awake()
		{
			dropdownPanel.transform.AsRectTransform().pivot = new Vector2(0f, 1f);
			base.Awake();
			searchInputField.onValueChanged.AddListener(OnSearchValueChanged);
		}

		protected override void Update()
		{
			base.Update();
			if (dropdownPanel.activeSelf)
			{
				RectTransform rectTransform = dropdownPanel.transform.AsRectTransform();
				Vector2 vector = rectTransform.position;
				Vector2 sizeDelta = rectTransform.sizeDelta;
				if (vector != _lastLoggedPosition || sizeDelta != _lastLoggedSize)
				{
					Debug.Log($"[GraphContextMenu] Panel changed — pos:{vector} size:{sizeDelta} screen:({Screen.width}x{Screen.height})");
					_lastLoggedPosition = vector;
					_lastLoggedSize = sizeDelta;
				}
			}
		}

		private void ToggleCategory(ContextMenuCategory category)
		{
			category.expanded = !category.expanded;
			category.container.gameObject.SetActive(category.expanded);
			category.arrowText.text = (category.expanded ? "\ue5c5" : "\ue5df");
			AdjustMenuSizingAndClamping();
		}

		private ContextMenuCategory GetOrCreateCategory(string categoryName)
		{
			if (categories.TryGetValue(categoryName, out var value))
			{
				return value;
			}
			GraphContextMenuCategory graphContextMenuCategory = UnityEngine.Object.Instantiate(categoryPrefab, categoriesContainer);
			_spawnedItems.Add(graphContextMenuCategory.gameObject);
			graphContextMenuCategory.gameObject.name = categoryName;
			ContextMenuCategory category = new ContextMenuCategory
			{
				categoryName = categoryName,
				foldoutGo = graphContextMenuCategory.gameObject,
				headerButton = graphContextMenuCategory.HeaderButton,
				container = graphContextMenuCategory.ItemsContainer,
				arrowText = graphContextMenuCategory.ArrowText,
				expanded = false
			};
			graphContextMenuCategory.TitleText.text = categoryName;
			graphContextMenuCategory.ArrowText.text = "\ue5df";
			graphContextMenuCategory.HeaderButton.onClick.AddListener(delegate
			{
				ToggleCategory(category);
			});
			categories.Add(categoryName, category);
			return category;
		}

		public void ClearMenu()
		{
			optionButtons.Clear();
			foreach (GameObject spawnedItem in _spawnedItems)
			{
				spawnedItem.SetActive(value: false);
				UnityEngine.Object.Destroy(spawnedItem);
			}
			_spawnedItems.Clear();
			categories.Clear();
		}

		protected override void OnShowPanel()
		{
			base.OnShowPanel();
			AdjustMenuSizingAndClamping();
		}

		public void Open(ContextMenuOpenSource args, ContextMenuConfig config, Vector2 screenPos, string filter = null)
		{
			Populate(config);
			contextMenuArgs = args;
			spawnPosition = screenPos;
			activeFilter = filter;
			searchInputField.text = "";
			ApplyFiltering("");
			Show(null, screenPos);
			searchInputField.Select();
			searchInputField.ActivateInputField();
		}

		private void Populate(ContextMenuConfig config)
		{
			ClearMenu();
			headerText.text = config.Title ?? "Options";
			foreach (ContextMenuOption option in config.Options)
			{
				if (!option.Disable)
				{
					string category = option.optionId.category;
					bool isTopLevel = option.optionId.isTopLevel;
					UnityAction onClick = delegate
					{
						option.onClick?.Invoke(option.optionId);
						HideMenuAsync().Forget();
					};
					GameObject gameObject;
					if (isTopLevel)
					{
						gameObject = CreateItemButton(categoriesContainer, option.label, onClick);
					}
					else
					{
						ContextMenuCategory orCreateCategory = GetOrCreateCategory(category);
						gameObject = CreateItemButton(orCreateCategory.container, option.label, onClick);
						orCreateCategory.spawnedButtons.Add(gameObject);
					}
					gameObject.name = option.optionId.ToString();
					optionButtons[option] = gameObject;
				}
			}
		}

		private GameObject CreateItemButton(Transform parent, string label, UnityAction onClick)
		{
			GraphContextMenuItem graphContextMenuItem = UnityEngine.Object.Instantiate(itemButtonPrefab, parent);
			_spawnedItems.Add(graphContextMenuItem.gameObject);
			graphContextMenuItem.LabelText.text = label;
			graphContextMenuItem.Button.onClick.AddListener(onClick);
			return graphContextMenuItem.gameObject;
		}

		private void OnSearchValueChanged(string value)
		{
			ApplyFiltering(value);
			AdjustMenuSizingAndClamping();
		}

		private void ApplyFiltering(string search)
		{
			foreach (KeyValuePair<ContextMenuOption, GameObject> optionButton in optionButtons)
			{
				ContextMenuOption key = optionButton.Key;
				GameObject value = optionButton.Value;
				bool flag = IsOptionVisible(key, contextMenuArgs, activeFilter);
				if (flag && !string.IsNullOrEmpty(search))
				{
					flag = key.label.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 || key.optionId.ToString().IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
				}
				value.SetActive(flag);
			}
			GraphPin resultPin;
			bool flag2 = !string.IsNullOrEmpty(search) || !string.IsNullOrEmpty(activeFilter) || contextMenuArgs.TryGetPin(out resultPin);
			foreach (ContextMenuCategory value2 in categories.Values)
			{
				bool flag3 = false;
				foreach (GameObject spawnedButton in value2.spawnedButtons)
				{
					if (spawnedButton.activeSelf)
					{
						flag3 = true;
						break;
					}
				}
				value2.foldoutGo.SetActive(flag3);
				bool flag4 = (flag2 ? flag3 : value2.expanded);
				if (value2.container != value2.foldoutGo.transform.AsRectTransform())
				{
					value2.container.gameObject.SetActive(flag3 && flag4);
				}
				value2.arrowText.text = (flag4 ? "\ue5c5" : "\ue5df");
			}
		}

		private void AdjustMenuSizingAndClamping()
		{
			RectTransform rectTransform = dropdownPanel.transform.AsRectTransform();
			rectTransform.pivot = new Vector2(0f, 1f);
			Canvas.ForceUpdateCanvases();
			if (categoriesContainer != null)
			{
				FixLayout.ForceRebuildRecursive(categoriesContainer);
			}
			RectTransform content = scrollRect.content;
			FixLayout.ForceRebuildRecursive(content);
			Canvas.ForceUpdateCanvases();
			float height = content.rect.height;
			float num = Mathf.Clamp(height + headerHeight + 1f, minMenuHeight, maxMenuHeight);
			Debug.Log($"[GraphContextMenu] AdjustSizing — contentHeight:{height} headerHeight:{headerHeight} targetHeight:{num} min:{minMenuHeight} max:{maxMenuHeight}");
			scrollRectLayout.minHeight = num;
			scrollRectLayout.preferredHeight = num;
			RectTransform rectTransform2 = scrollRect.transform.parent.AsRectTransform();
			FixLayout.ForceRebuildRecursive(rectTransform2);
			FixLayout.ForceRebuildRecursive(rectTransform);
			Canvas.ForceUpdateCanvases();
			rectTransform.sizeDelta = new Vector2(rectTransform2.rect.width, rectTransform2.rect.height);
			Vector2 vector = ClampToScreen(spawnPosition);
			Debug.Log($"[GraphContextMenu] ClampToScreen — spawnPos:{spawnPosition} clampedPos:{vector} panelSize:{rectTransform.sizeDelta} screen:({Screen.width}x{Screen.height})");
			rectTransform.position = vector;
		}

		private Vector2 ClampToScreen(Vector2 screenPos)
		{
			RectTransform rectTransform = dropdownPanel.transform.AsRectTransform();
			Vector3[] array = new Vector3[4];
			rectTransform.GetWorldCorners(array);
			float num = array[2].x - array[0].x;
			float num2 = array[1].y - array[0].y;
			float x = screenPos.x;
			float num3 = screenPos.x + num;
			float y = screenPos.y;
			float num4 = screenPos.y - num2;
			float num5 = 0f;
			if (num3 > (float)Screen.width - screenPadding)
			{
				num5 = (float)Screen.width - screenPadding - num3;
			}
			else if (x < screenPadding)
			{
				num5 = screenPadding - x;
			}
			float num6 = 0f;
			if (y > (float)Screen.height - screenPadding)
			{
				num6 = (float)Screen.height - screenPadding - y;
			}
			else if (num4 < screenPadding)
			{
				num6 = screenPadding - num4;
			}
			return new Vector2(screenPos.x + num5, screenPos.y + num6);
		}

		private bool IsOptionVisible(ContextMenuOption option, ContextMenuOpenSource args, string filter)
		{
			if (args.TryGetPin(out var resultPin))
			{
				bool flag = false;
				foreach (ContextMenuPinSetup compatibilityPin in option.compatibilityPins)
				{
					if (resultPin.IsCompatibleWith(compatibilityPin.direction, compatibilityPin.pinType, compatibilityPin.allowedConnectionTypes))
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					return false;
				}
			}
			if (!string.IsNullOrEmpty(filter) && option.label.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0 && option.optionId.ToString().IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0 && option.optionId.category.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
			{
				return false;
			}
			return true;
		}
	}
}
