using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public abstract class MapIcon : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler, IPointerClickHandler
{
	public enum ClickSource
	{
		Mouse = 0,
		Controller = 1
	}

	public Image iconImage;

	protected Vector3 globalPosition;

	protected bool isSelected;

	protected abstract FactionHQ GetHQ();

	protected abstract Color GetColor();

	protected abstract bool IsLocalPlayerAircraft();

	public abstract void ClickIcon(ClickSource clickSource);

	protected abstract void OnSelectIcon();

	protected abstract void OnDeselectIcon();

	protected abstract void OnRemoveIcon();

	public abstract string GetInfoText();

	public abstract void UpdateIcon(float mapDisplayFactor, float mapInverseScale, Transform mapTransform, bool mapMaximized);

	public void UpdateColor()
	{
		Color color = GetColor();
		iconImage.color = color;
		if (IsLocalPlayerAircraft())
		{
			HighlightIcon();
		}
	}

	public void HighlightIcon()
	{
		iconImage.color = Color.white;
		iconImage.raycastTarget = false;
	}

	public void SelectIcon()
	{
		isSelected = true;
		iconImage.raycastTarget = false;
		UpdateColor();
		OnSelectIcon();
	}

	public void DeselectIcon()
	{
		isSelected = false;
		iconImage.raycastTarget = true;
		UpdateColor();
		OnDeselectIcon();
	}

	public void RemoveIcon()
	{
		OnRemoveIcon();
		if (this != null)
		{
			Object.Destroy(base.gameObject);
		}
	}

	void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
	{
		if (eventData.button == PointerEventData.InputButton.Left)
		{
			ClickIcon(ClickSource.Mouse);
		}
	}

	void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
	{
		SceneSingleton<DynamicMap>.i.DisplayTooltip(this);
	}

	void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
	{
		SceneSingleton<DynamicMap>.i.HideTooltip();
	}
}
