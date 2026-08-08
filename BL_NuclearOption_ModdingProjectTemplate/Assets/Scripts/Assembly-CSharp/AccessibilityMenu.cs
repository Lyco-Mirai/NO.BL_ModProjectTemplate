using System;
using System.Collections.Generic;
using System.IO;
using NuclearOption.UI;
using NuclearOption.UIStyleSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AccessibilityMenu : MonoBehaviour
{
	[Header("Prefabs")]
	[SerializeField]
	private ColorPicker colorPickerPrefab;

	[SerializeField]
	private CategoryTitle categoryTitlePrefab;

	[Header("Theme Selection Bar")]
	[SerializeField]
	private TMP_Dropdown themeGroupDropdown;

	[SerializeField]
	private TMP_InputField themeGroupInputField;

	[SerializeField]
	private Button themeGroupNewButton;

	[SerializeField]
	private Button themeGroupSaveButton;

	[SerializeField]
	private TMP_Text themeGroupThemeName;

	[SerializeField]
	private Button themeGroupRenameButton;

	[SerializeField]
	private Button themeGroupDeleteButton;

	[SerializeField]
	private SliderToggle themeGroupSliderToggle;

	[SerializeField]
	private TMP_Text themeGroupSliderValue;

	[SerializeField]
	private TMP_Text themeGroupFirstTitle;

	[Header("Buttons")]
	[SerializeField]
	private Button openFolderButton;

	[Header("Tooltip")]
	[SerializeField]
	private GameObject tooltip;

	[SerializeField]
	private TMP_Text tooltipText;

	[Header("Theme Contexts")]
	[SerializeField]
	private Transform colorThemeTransform;

	[SerializeField]
	private Transform hudThemeTransform;

	[SerializeField]
	private Transform tacScreenThemeTransform;

	[SerializeField]
	private Transform menuThemeTransform;

	[Header("Color Pickers")]
	private readonly List<ColorPicker> colorThemePickers = new List<ColorPicker>();

	private readonly List<ColorPicker> hudThemePickers = new List<ColorPicker>();

	private readonly List<ColorPicker> tacScreenThemePickers = new List<ColorPicker>();

	private readonly List<ColorPicker> menuThemePickers = new List<ColorPicker>();

	private bool isThemeEdited;

	private bool isRenameInProgress;

	private Vector3 tooltipOffset;

	private void Awake()
	{
		tooltip.SetActive(value: false);
		themeGroupDropdown.onValueChanged.AddListener(OnThemeGroupChanged);
		themeGroupNewButton.onClick.AddListener(OnThemeGroupNew);
		themeGroupSaveButton.onClick.AddListener(OnThemeGroupSave);
		themeGroupRenameButton.onClick.AddListener(OnThemeGroupRename);
		themeGroupDeleteButton.onClick.AddListener(OnThemeGroupDelete);
		themeGroupInputField.gameObject.SetActive(value: false);
		themeGroupInputField.onEndEdit.AddListener(OnThemeNameEditEnd);
		themeGroupSliderToggle.onValueChanged.AddListener(OnSliderToggleChanged);
		openFolderButton.onClick.AddListener(OnOpenFolder);
		RefreshSliders();
		OnThemeGroupChanged(ThemeManager.GetCurrentThemeGroupIndex());
		RefreshDropdown();
	}

	private void Update()
	{
		if (tooltip.activeSelf)
		{
			tooltip.transform.position = Input.mousePosition + tooltipOffset;
		}
	}

	private void RefreshDropdown()
	{
		themeGroupDropdown.ClearOptions();
		themeGroupDropdown.AddOptions(ThemeManager.GetThemeGroupNames());
		themeGroupDropdown.SetValueWithoutNotify(ThemeManager.GetCurrentThemeGroupIndex());
	}

	private void RefreshButtons()
	{
		bool flag = ThemeManager.Active.Origin != ThemeGroup.ThemeOrigin.Scriptable_Object;
		themeGroupSaveButton.interactable = flag && isThemeEdited;
		themeGroupRenameButton.interactable = flag;
		themeGroupDeleteButton.interactable = flag;
		themeGroupNewButton.interactable = !isRenameInProgress;
	}

	private void RefreshSliders()
	{
		bool themeGroupEditorAdvancedMode = PlayerSettings.themeGroupEditorAdvancedMode;
		themeGroupSliderToggle.isOn = themeGroupEditorAdvancedMode;
		themeGroupSliderValue.text = (themeGroupEditorAdvancedMode ? "Simple\n<b>Advanced</b>" : "<b>Simple</b>\nAdvanced");
		themeGroupFirstTitle.enabled = themeGroupEditorAdvancedMode;
	}

	private void OnThemeGroupNew()
	{
		ThemeManager.SwitchToThemeGroup(ThemeManager.CopyActiveThemeGroupWithNewId());
		ThemeManager.SaveActiveThemeGroup();
		OnThemeGroupRename();
		RefreshButtons();
	}

	private void OnOpenFolder()
	{
		string text = Application.persistentDataPath + "/themes/";
		if (!Directory.Exists(text))
		{
			Directory.CreateDirectory(text);
		}
		Application.OpenURL(text);
	}

	private void OnThemeGroupSave()
	{
		List<Color> list = new List<Color>();
		List<Color> list2 = new List<Color>();
		List<Color> list3 = new List<Color>();
		List<Color> list4 = new List<Color>();
		if (themeGroupSliderToggle.isOn)
		{
			foreach (ColorPicker colorThemePicker in colorThemePickers)
			{
				list.Add(colorThemePicker.Color);
			}
			foreach (ColorPicker hudThemePicker in hudThemePickers)
			{
				list2.Add(hudThemePicker.Color);
			}
			foreach (ColorPicker tacScreenThemePicker in tacScreenThemePickers)
			{
				list3.Add(tacScreenThemePicker.Color);
			}
			foreach (ColorPicker menuThemePicker in menuThemePickers)
			{
				list4.Add(menuThemePicker.Color);
			}
		}
		else
		{
			List<Color> list5 = new List<Color>();
			foreach (ColorPicker colorThemePicker2 in colorThemePickers)
			{
				list5.Add(colorThemePicker2.Color);
			}
			ApplyBasicModeColorDerivation(list5, out var colorThemeList, out var hudThemeList, out var tacScreenThemeList, out var menuThemeList);
			list = colorThemeList;
			list2 = hudThemeList;
			list3 = tacScreenThemeList;
			list4 = menuThemeList;
		}
		ThemeManager.Active.ColorTheme.SetColors(list);
		ThemeManager.Active.HudTheme.SetColors(list2);
		ThemeManager.Active.TacScreenTheme.SetColors(list3);
		ThemeManager.Active.MenuTheme.SetColors(list4);
		ThemeManager.SaveActiveThemeGroup();
		ThemeManager.NotifyThemeGroupChanged();
		isThemeEdited = false;
		RefreshThemeName();
		RefreshButtons();
	}

	private void OnThemeGroupRename()
	{
		isRenameInProgress = true;
		themeGroupInputField.text = ThemeManager.Active.name;
		themeGroupInputField.gameObject.SetActive(value: true);
		themeGroupDropdown.gameObject.SetActive(value: false);
		RefreshButtons();
	}

	private void OnThemeGroupDelete()
	{
		string id = ThemeManager.GetThemeGroupByIndex(ThemeManager.GetCurrentThemeGroupIndex() - 1).Id;
		ThemeManager.DeleteActiveThemeGroup();
		ThemeManager.SwitchToThemeGroup(id);
		LoadColorPickers();
		isThemeEdited = false;
		RefreshThemeName();
		if (isRenameInProgress)
		{
			isRenameInProgress = false;
			themeGroupInputField.text = "";
			themeGroupInputField.gameObject.SetActive(value: false);
			themeGroupDropdown.gameObject.SetActive(value: true);
		}
		RefreshDropdown();
		RefreshButtons();
	}

	private void OnThemeGroupChanged(int value)
	{
		ThemeManager.SwitchToThemeGroup(ThemeManager.GetThemeGroupByIndex(value).Id);
		LoadColorPickers();
		isThemeEdited = false;
		RefreshThemeName();
		RefreshButtons();
	}

	private void OnThemeNameEditEnd(string newName)
	{
		if (isRenameInProgress)
		{
			isRenameInProgress = false;
			ThemeManager.RenameActiveThemeGroup(newName);
			RefreshDropdown();
			RefreshButtons();
			themeGroupInputField.text = "";
			themeGroupInputField.gameObject.SetActive(value: false);
			themeGroupDropdown.gameObject.SetActive(value: true);
		}
	}

	private void OnSliderToggleChanged(bool value)
	{
		PlayerSettings.themeGroupEditorAdvancedMode = value;
		PlayerPrefs.SetInt("ThemeGroupEditorAdvancedMode", value ? 1 : 0);
		RefreshSliders();
		LoadColorPickers();
		isThemeEdited = false;
		RefreshThemeName();
		RefreshButtons();
	}

	private void LoadColorPickers()
	{
		List<Tuple<string, Color, string>> currentTuples;
		List<Tuple<string, Color, string>> currentTuples2;
		List<Tuple<string, Color, string>> currentTuples3;
		List<Tuple<string, Color, string>> currentTuples4;
		if (themeGroupSliderToggle.isOn)
		{
			currentTuples = ThemeManager.Active.ColorTheme.GetColors();
			currentTuples2 = ThemeManager.Active.HudTheme.GetColors();
			currentTuples3 = ThemeManager.Active.TacScreenTheme.GetColors();
			currentTuples4 = ThemeManager.Active.MenuTheme.GetColors();
		}
		else
		{
			currentTuples = new List<Tuple<string, Color, string>>
			{
				Tuple.Create("Primary ", ThemeManager.Active.ColorTheme.AllClear, "Base color for static UI elements and dynamic HUD/TacScreen elements in the <b>All Clear</b> state (e.g., fuel level, AoA)."),
				Tuple.Create("Secondary ", ThemeManager.Active.ColorTheme.HudUnitFriendly, "Base color for friendly units.\n\nUsed to derive colors like <b>HUD Unit Friendly</b> and <b>Map Icon Friendly</b>."),
				Tuple.Create("Warning ", ThemeManager.Active.ColorTheme.Warning, "Base color for warning states.\n\nUsed to derive colors like <b>HUD Unit Flash</b> and <b>Map Icon Hostile Selected</b>."),
				Tuple.Create("Danger ", ThemeManager.Active.ColorTheme.Alert, "Base color for critical/danger states.\n\nUsed to derive colors like <b>HUD Unit Hostile</b> and <b>Target Ping</b>."),
				Tuple.Create("Neutral ", ThemeManager.Active.ColorTheme.HudUnitNeutral, "Base color for neutral elements.\n\nUsed to derive colors like <b>HUD Unit Neutral</b> and <b>Map Icon Neutral</b>.")
			};
			currentTuples2 = new List<Tuple<string, Color, string>>();
			currentTuples3 = new List<Tuple<string, Color, string>>();
			currentTuples4 = new List<Tuple<string, Color, string>>();
		}
		hudThemeTransform.gameObject.SetActive(themeGroupSliderToggle.isOn);
		tacScreenThemeTransform.gameObject.SetActive(themeGroupSliderToggle.isOn);
		menuThemeTransform.gameObject.SetActive(themeGroupSliderToggle.isOn);
		UpdateOrAddColorPickers(colorThemePickers, colorThemeTransform, currentTuples);
		if (themeGroupSliderToggle.isOn)
		{
			UpdateOrAddColorPickers(hudThemePickers, hudThemeTransform, currentTuples2);
			UpdateOrAddColorPickers(tacScreenThemePickers, tacScreenThemeTransform, currentTuples3);
			UpdateOrAddColorPickers(menuThemePickers, menuThemeTransform, currentTuples4);
		}
		LayoutRebuilder.ForceRebuildLayoutImmediate(colorThemeTransform as RectTransform);
	}

	private void UpdateOrAddColorPickers(List<ColorPicker> pickers, Transform parentTransform, List<Tuple<string, Color, string>> currentTuples)
	{
		if (pickers.Count == currentTuples.Count)
		{
			for (int i = 0; i < pickers.Count; i++)
			{
				pickers[i].SetValues(currentTuples[i].Item1, currentTuples[i].Item2, currentTuples[i].Item3);
			}
			return;
		}
		foreach (ColorPicker picker in pickers)
		{
			picker.OnColorChanged -= OnColorPickerChanged;
			picker.OnColorNameHoverIn -= OnColorNameHoverIn;
			picker.OnColorNameHoverOut -= OnColorNameHoveredOut;
			UnityEngine.Object.Destroy(picker.gameObject);
		}
		pickers.Clear();
		foreach (Tuple<string, Color, string> currentTuple in currentTuples)
		{
			ColorPicker colorPicker = UnityEngine.Object.Instantiate(colorPickerPrefab, parentTransform);
			colorPicker.SetValues(currentTuple.Item1, currentTuple.Item2, currentTuple.Item3);
			colorPicker.OnColorChanged += OnColorPickerChanged;
			colorPicker.OnColorNameHoverIn += OnColorNameHoverIn;
			colorPicker.OnColorNameHoverOut += OnColorNameHoveredOut;
			pickers.Add(colorPicker);
		}
	}

	private void OnColorPickerChanged(Color newColor)
	{
		isThemeEdited = true;
		RefreshThemeName();
		RefreshButtons();
	}

	private void OnColorNameHoverIn(string newTooltip)
	{
		tooltip.SetActive(value: true);
		tooltipText.text = newTooltip;
		tooltipOffset = new Vector3(0f, tooltipText.preferredHeight / 2f);
		LayoutRebuilder.ForceRebuildLayoutImmediate(tooltip.transform as RectTransform);
	}

	private void OnColorNameHoveredOut(string _)
	{
		tooltip.SetActive(value: false);
	}

	private void RefreshThemeName()
	{
		themeGroupThemeName.fontStyle = ((isThemeEdited && ThemeManager.Active.Origin != ThemeGroup.ThemeOrigin.Scriptable_Object) ? FontStyles.Italic : FontStyles.Normal);
	}

	private static void ApplyBasicModeColorDerivation(List<Color> baseColors, out List<Color> colorThemeList, out List<Color> hudThemeList, out List<Color> tacScreenThemeList, out List<Color> menuThemeList)
	{
		Color color = baseColors[0];
		Color color2 = baseColors[1];
		Color color3 = baseColors[2];
		Color color4 = baseColors[3];
		Color color5 = baseColors[4];
		Color.RGBToHSV(color, out var _, out var _, out var _);
		Color.RGBToHSV(color2, out var _, out var _, out var _);
		Color.RGBToHSV(color3, out var H3, out var S3, out var V3);
		Color.RGBToHSV(color4, out var H4, out var S4, out var V4);
		Color.RGBToHSV(color5, out var H5, out var _, out var _);
		colorThemeList = new List<Color>
		{
			color5,
			color2,
			Color.HSVToRGB((H4 * 255f + 255f + 2f) % 255f / 255f, Mathf.Clamp(S3 * 255f - 4f, 0f, 255f) / 255f, V4),
			color,
			Color.HSVToRGB((H3 * 255f + 255f - 4f) % 255f / 255f, Mathf.Clamp(S3 * 255f - 4f, 0f, 255f) / 255f, V3),
			color5,
			color2,
			Color.HSVToRGB((H4 * 255f + 2f) % 255f / 255f, Mathf.Clamp(S4 * 255f - 10f, 0f, 255f) / 255f, V4),
			Color.HSVToRGB((H3 * 255f + 255f - 4f) % 255f / 255f, Mathf.Clamp(S3 * 255f - 4f, 0f, 255f) / 255f, V3),
			color,
			Color.HSVToRGB((H3 * 255f + 255f - 4f) % 255f / 255f, Mathf.Clamp(S3 * 255f - 4f, 0f, 255f) / 255f, V3),
			color,
			color4.WithAlpha(ThemeManager.Active.ColorTheme.TargetPing.a),
			color3.WithAlpha(ThemeManager.Active.ColorTheme.DetectedPing.a),
			Color.HSVToRGB(H5, 0f, 1f).WithAlpha(ThemeManager.Active.ColorTheme.PassivePing.a),
			color,
			color3,
			color4,
			color5,
			color2,
			Color.HSVToRGB((H4 * 255f + 255f + 2f) % 255f / 255f, Mathf.Clamp(S3 * 255f - 4f, 0f, 255f) / 255f, V4),
			color
		};
		hudThemeList = new List<Color>
		{
			color,
			Color.HSVToRGB((H4 * 255f + 21f) % 255f / 255f, S4, V4),
			Color.HSVToRGB((H4 * 255f + 11f) % 255f / 255f, S4, V4),
			color,
			Color.HSVToRGB((H4 * 255f + 21f) % 255f / 255f, S4, V4),
			Color.HSVToRGB((H4 * 255f + 12f) % 255f / 255f, S4, V4),
			color3,
			Color.HSVToRGB((H4 * 255f + 18f) % 255f / 255f, S4, V4),
			Color.HSVToRGB((H3 * 255f + 255f - 11f) % 255f / 255f, S3, V3)
		};
		tacScreenThemeList = new List<Color>
		{
			color,
			Color.HSVToRGB(H3, S3, Mathf.Clamp(V3 * 255f - 111f, 0f, 255f) / 255f),
			color
		};
		menuThemeList = new List<Color> { color, color, color };
	}
}
