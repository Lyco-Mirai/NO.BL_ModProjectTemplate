using System;
using System.Collections.Generic;
using NuclearOption.UIStyleSystem;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

public class MapOptions_ToggleButton : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerClickHandler, IPointerExitHandler
{
	[Serializable]
	public class CallBackEvent : UnityEvent
	{
	}

	public CallBackEvent OnToggleMethod;

	[SerializeField]
	private MonoBehaviour script;

	[SerializeField]
	private Image image;

	[SerializeField]
	private TextMeshProUGUI label;

	[SerializeField]
	private string variableName;

	public bool isActive = true;

	public bool status;

	public bool onFocus;

	private Color colorIsOn = Color.green;

	private Color colorIsOff = Color.grey;

	[SerializeField]
	private List<MapOptions_ToggleButton> otherButtons = new List<MapOptions_ToggleButton>();

	private void Start()
	{
		if (script != null && variableName != "")
		{
			status = (bool)script.GetType().GetField(variableName).GetValue(script);
		}
		MapOptions_ToggleButton_OnThemeGroupChanged();
		ThemeManager.ThemeGroupChanged += MapOptions_ToggleButton_OnThemeGroupChanged;
		Set(status);
	}

	private void OnDestroy()
	{
		ThemeManager.ThemeGroupChanged -= MapOptions_ToggleButton_OnThemeGroupChanged;
	}

	public void Toggle()
	{
		if (OnToggleMethod != null)
		{
			OnToggleMethod.Invoke();
		}
		if (otherButtons.Count > 0)
		{
			Set(arg: true);
			{
				foreach (MapOptions_ToggleButton otherButton in otherButtons)
				{
					otherButton.Set(arg: false);
				}
				return;
			}
		}
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
		if (onFocus && eventData.button == PointerEventData.InputButton.Left)
		{
			Toggle();
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		onFocus = false;
	}

	private void MapOptions_ToggleButton_OnThemeGroupChanged()
	{
		colorIsOn = ThemeManager.Active.ColorTheme.AllClear;
		SetColor();
	}
}
