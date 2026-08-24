using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ObjectiveOverlay : MonoBehaviour
{
	[SerializeField]
	private Image objectivePointer;

	[SerializeField]
	private Image objectiveDot;

	[SerializeField]
	private Image sizeIndicator;

	[SerializeField]
	private TextMeshProUGUI objectiveInfo;

	[SerializeField]
	private Transform pointerTail;

	private bool hidden;

	public TextNoOverlap TextNoOverlap;

	private void Awake()
	{
		TextNoOverlap = new TextNoOverlap(objectiveInfo);
	}

	public void Initialize(Transform iconLayer)
	{
		objectivePointer.transform.SetParent(iconLayer);
		objectiveInfo.transform.SetParent(iconLayer);
		hidden = false;
	}

	public void UpdateOverlay(MissionPosition.PositionResult result)
	{
		hidden = false;
		objectivePointer.enabled = true;
		objectiveInfo.enabled = true;
		Camera mainCamera = SceneSingleton<CameraStateManager>.i.mainCamera;
		Vector3 a = mainCamera.WorldToScreenPoint(result.Position.ToLocalPosition());
		Vector3 vector = new Vector3((float)Screen.width * 0.5f, (float)Screen.height * 0.5f, 0f);
		Vector3 vector2 = vector;
		a -= vector;
		if (a.z < 0f)
		{
			a *= -1f;
		}
		a = Vector3.Scale(a, new Vector3(1f, 1f, 0f));
		float num = Mathf.Atan2(a.y, a.x);
		float num2 = Mathf.Tan(num);
		float num3 = Vector3.Angle(SceneSingleton<CameraStateManager>.i.transform.forward, result.Direction);
		if (num3 > 90f || Mathf.Abs(a.x) > (float)Screen.width * 0.5f || Mathf.Abs(a.y) > (float)Screen.height * 0.5f)
		{
			a = ((!(a.x > 0f)) ? new Vector3(0f - vector2.x, (0f - vector2.x) * num2, 0f) : new Vector3(vector2.x, vector2.x * num2, 0f));
			if (a.y > vector2.y)
			{
				a = new Vector3(vector2.y / num2, vector2.y, 0f);
			}
			else if (a.y < 0f - vector2.y)
			{
				a = new Vector3((0f - vector2.y) / num2, 0f - vector2.y, 0f);
			}
			sizeIndicator.enabled = false;
		}
		else
		{
			sizeIndicator.enabled = true;
		}
		a += vector;
		objectivePointer.transform.position = a;
		objectiveDot.transform.position = a;
		objectivePointer.transform.localEulerAngles = new Vector3(0f, 0f, num * 57.29578f - 90f);
		if (num3 > 10f)
		{
			objectivePointer.enabled = true;
			objectiveDot.enabled = false;
			TextNoOverlap.SetTarget(pointerTail.position);
		}
		else
		{
			objectivePointer.enabled = false;
			objectiveDot.enabled = true;
			TextNoOverlap.SetTarget(objectiveDot.transform.position - Vector3.up * 25f);
		}
		float num4 = 0f;
		if (result.Range.HasValue)
		{
			num4 = result.Range.Value;
		}
		float num5 = 1f / ((result.Distance != 0f) ? result.Distance : 0.01f);
		float num6 = ((RectTransform)sizeIndicator.canvas.transform).rect.height / sizeIndicator.rectTransform.rect.height / Mathf.Tan(mainCamera.fieldOfView * (MathF.PI / 180f) * 0.5f);
		sizeIndicator.transform.localScale = Vector3.one * (num6 * num4 * num5);
		sizeIndicator.transform.position = objectivePointer.transform.position;
		sizeIndicator.transform.eulerAngles = new Vector3(0f, 0f, 0f - mainCamera.transform.eulerAngles.z);
		float value = num4 * 20f * num5 - 0.5f;
		sizeIndicator.color = sizeIndicator.color.WithAlpha(Mathf.Clamp01(value));
		string text = ((result.Objective != null) ? result.Objective.SavedObjective.DisplayName : "Waypoint");
		objectiveInfo.text = text + " " + UnitConverter.DistanceReading(result.Distance);
		objectiveInfo.fontSize = (int)PlayerSettings.overlayTextSize;
	}

	public void HideOverlay()
	{
		if (!hidden)
		{
			hidden = true;
			objectivePointer.enabled = false;
			objectiveDot.enabled = false;
			objectiveInfo.enabled = false;
			sizeIndicator.enabled = false;
		}
	}

	public void SetColor(Color color)
	{
		objectivePointer.color = color;
		objectiveDot.color = color;
		sizeIndicator.color = color;
		objectiveInfo.color = color;
	}

	public void SetRaycastTarget(bool enabled)
	{
		objectivePointer.raycastTarget = enabled;
		objectiveDot.raycastTarget = enabled;
		sizeIndicator.raycastTarget = enabled;
		objectiveInfo.raycastTarget = enabled;
	}
}
