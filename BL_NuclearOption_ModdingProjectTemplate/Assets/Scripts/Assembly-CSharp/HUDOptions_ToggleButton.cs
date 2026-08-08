using System.Collections.Generic;
using NuclearOption.UIStyleSystem;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HUDOptions_ToggleButton : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerClickHandler, IPointerExitHandler
{
	public enum ButtonContext
	{
		NEUTRAL = 0,
		FRIENDLY = 1,
		HOSTILE = 2
	}

	[SerializeField]
	private HUDOptions_Category category;

	[SerializeField]
	private Image image;

	[SerializeField]
	private TextMeshProUGUI label;

	public bool status;

	public bool onFocus;

	private Color colorIsOn = Color.green;

	private Color colorIsOff = Color.grey;

	[SerializeField]
	private ButtonContext context;

	public List<UnitDefinition> listDefinitions = new List<UnitDefinition>();

	public HUDOptions_Priorities settings;

	private void Start()
	{
		HUDOptions_ToggleButton_OnThemeGroupChanged();
		ThemeManager.ThemeGroupChanged += HUDOptions_ToggleButton_OnThemeGroupChanged;
	}

	private void OnDestroy()
	{
		ThemeManager.ThemeGroupChanged -= HUDOptions_ToggleButton_OnThemeGroupChanged;
	}

	public void Toggle()
	{
		if (status)
		{
			Set(arg: false);
		}
		else
		{
			Set(arg: true);
		}
	}

	public void Set(bool arg)
	{
		status = arg;
		SetColor();
		if (settings != null && status)
		{
			SceneSingleton<HUDOptions>.i.ApplySettings(settings);
		}
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		onFocus = true;
	}

	public void SetColor()
	{
		if (image != null)
		{
			image.color = (status ? colorIsOn : colorIsOff);
		}
		if (label != null)
		{
			label.color = (status ? colorIsOn : colorIsOff);
		}
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		if (!onFocus)
		{
			return;
		}
		if (eventData.button == PointerEventData.InputButton.Left)
		{
			if (settings != null)
			{
				SceneSingleton<HUDOptions>.i.ToggleButtons(this);
			}
			else
			{
				Toggle();
				if (category != null)
				{
					category.Set(status);
				}
			}
		}
		if (eventData.button == PointerEventData.InputButton.Right && listDefinitions.Count > 0)
		{
			SceneSingleton<HUDOptions>.i.ToggleButtons(this);
		}
		SceneSingleton<HUDOptions>.i.SaveSettings();
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		onFocus = false;
	}

	public bool CheckDefinition(UnitDefinition definition)
	{
		bool result = false;
		if (listDefinitions.Count > 0)
		{
			for (int i = 0; i < listDefinitions.Count; i++)
			{
				if (definition == listDefinitions[i])
				{
					result = true;
				}
			}
		}
		return result;
	}

	public void LoadValues()
	{
		settings.ReadFromJson();
	}

	public void SaveValues()
	{
		settings.SaveToJson();
	}

	public void SetTextIcon(string text, Sprite img)
	{
		label.text = text.Replace("_", "\n");
		image.sprite = img;
		base.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(40f, 30f);
		image.rectTransform.localPosition = new Vector3(0f, 10f, 0f);
		image.rectTransform.sizeDelta = new Vector2(-5f, 25f);
		label.rectTransform.localPosition = new Vector3(0f, -10f, 0f);
		label.autoSizeTextContainer = true;
		label.fontSizeMax = 14f;
		label.fontSizeMin = 8f;
	}

	public void AddDefinition(UnitDefinition definition)
	{
		if (!listDefinitions.Contains(definition))
		{
			listDefinitions.Add(definition);
		}
	}

	private void HUDOptions_ToggleButton_OnThemeGroupChanged()
	{
		colorIsOn = context switch
		{
			ButtonContext.FRIENDLY => ThemeManager.Active.ColorTheme.MapIconFriendly, 
			ButtonContext.HOSTILE => ThemeManager.Active.ColorTheme.MapIconHostile, 
			_ => ThemeManager.Active.ColorTheme.AllClear, 
		};
		SetColor();
	}
}
