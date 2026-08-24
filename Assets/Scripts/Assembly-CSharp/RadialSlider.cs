using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

public class RadialSlider : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
	private bool focus;

	private bool grab;

	[SerializeField]
	private Image arrow;

	[SerializeField]
	private Image head;

	[SerializeField]
	private TextMeshProUGUI valueLabel;

	public float value;

	public UnityEvent OnValueChanged;

	private void Update()
	{
		if (grab)
		{
			Vector3 vector = Input.mousePosition - base.transform.position;
			SetValue(Vector3.SignedAngle(Vector3.up, vector.normalized, -Vector3.forward));
		}
	}

	public void OnPointerEnter(PointerEventData pointer)
	{
		focus = true;
	}

	public void OnPointerExit(PointerEventData pointer)
	{
		focus = false;
	}

	public void OnPointerDown(PointerEventData pointer)
	{
		if (focus)
		{
			grab = true;
			arrow.color = Color.green;
			head.color = Color.green;
		}
	}

	public void OnPointerUp(PointerEventData pointer)
	{
		if (grab)
		{
			grab = false;
			arrow.color = Color.white;
			head.color = Color.white;
			OnValueChanged?.Invoke();
		}
	}

	public void SetValue(float input)
	{
		value = input;
		if (value < 0f)
		{
			value += 360f;
		}
		arrow.transform.localEulerAngles = new Vector3(0f, 0f, 0f - value);
		valueLabel.text = $"{value:F0}";
		valueLabel.transform.eulerAngles = Vector3.zero;
	}
}
