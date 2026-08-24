using System;
using System.Collections.Generic;
using NuclearOption.UIStyleSystem;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TargetListSelector_ToggleButton : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerClickHandler, IPointerExitHandler
{
	public enum ButtonContext
	{
		FRIENDLY = 0,
		HOSTILE = 1,
		ALERT = 2,
		NEUTRAL = 3
	}

	public Image image;

	public TextMeshProUGUI label;

	public bool sameFaction = true;

	public List<UnitDefinition> listUnitTypes = new List<UnitDefinition>();

	public Type[] listUnitTypeCache;

	public List<UnitDefinition> listDefinitions = new List<UnitDefinition>();

	public bool isActive = true;

	public bool status = true;

	public bool onFocus;

	private Color colorIsOn = Color.green;

	private Color colorIsOff = Color.grey;

	[SerializeField]
	private ButtonContext context = ButtonContext.NEUTRAL;

	public event Action OnToggle;

	private void Awake()
	{
		listUnitTypeCache = new Type[listUnitTypes.Count];
		for (int i = 0; i < listUnitTypeCache.Length; i++)
		{
			listUnitTypeCache[i] = listUnitTypes[i].GetType();
		}
	}

	private void Start()
	{
		TargetListSelector_ToggleButton_OnThemeGroupChanged();
		ThemeManager.ThemeGroupChanged += TargetListSelector_ToggleButton_OnThemeGroupChanged;
		SetColor();
	}

	private void OnDestroy()
	{
		ThemeManager.ThemeGroupChanged -= TargetListSelector_ToggleButton_OnThemeGroupChanged;
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
		SceneSingleton<TargetListSelector>.i.NeedUpdateIcons();
		this.OnToggle?.Invoke();
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		onFocus = true;
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		if (onFocus)
		{
			if (eventData.button == PointerEventData.InputButton.Right)
			{
				SceneSingleton<TargetListSelector>.i.SetOnlyItem(this);
			}
			else if (eventData.button == PointerEventData.InputButton.Left)
			{
				Toggle();
			}
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		onFocus = false;
	}

	public bool CheckFactions(Unit u)
	{
		switch (DynamicMap.GetFactionMode(u.NetworkHQ))
		{
		case FactionMode.Friendly:
			if (sameFaction)
			{
				return !status;
			}
			break;
		case FactionMode.Enemy:
			if (!sameFaction)
			{
				return !status;
			}
			break;
		}
		return false;
	}

	public bool CheckUnitTypes(Unit u)
	{
		Type type = u.definition.GetType();
		for (int i = 0; i < listUnitTypes.Count; i++)
		{
			if (listUnitTypeCache[i] == type)
			{
				return !status;
			}
		}
		return false;
	}

	public bool CheckDefinitions(Unit u)
	{
		UnitDefinition definition = u.definition;
		for (int i = 0; i < listDefinitions.Count; i++)
		{
			if (listDefinitions[i] == definition)
			{
				return !status;
			}
		}
		return false;
	}

	public void SetTextIcon(string text, Sprite img)
	{
		label.text = text.Replace("_", "\n");
		image.sprite = img;
		base.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(30f, 35f);
		image.rectTransform.localPosition = new Vector3(0f, 5f, 0f);
		image.rectTransform.sizeDelta = new Vector2(0f, 25f);
		label.rectTransform.localPosition = new Vector3(0f, -15f, 0f);
		label.autoSizeTextContainer = true;
		label.fontSizeMax = 24f;
		label.fontSizeMin = 18f;
	}

	public void AddDefinition(UnitDefinition definition)
	{
		if (!listDefinitions.Contains(definition))
		{
			listDefinitions.Add(definition);
		}
	}

	private void TargetListSelector_ToggleButton_OnThemeGroupChanged()
	{
		colorIsOn = context switch
		{
			ButtonContext.FRIENDLY => ThemeManager.Active.ColorTheme.MapIconFriendly, 
			ButtonContext.HOSTILE => ThemeManager.Active.ColorTheme.MapIconHostile, 
			ButtonContext.ALERT => ThemeManager.Active.ColorTheme.Alert, 
			_ => ThemeManager.Active.ColorTheme.AllClear, 
		};
		SetColor();
	}
}
