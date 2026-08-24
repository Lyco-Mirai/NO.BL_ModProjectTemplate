using System;
using System.Collections.Generic;
using UnityEngine;

public class ObjectiveOverlayManager : MonoBehaviour
{
	[Serializable]
	private enum LimitMode
	{
		One = 0,
		Multiple = 1,
		NoLimit = 2
	}

	[SerializeField]
	private ObjectiveOverlay overlayPrefab;

	[Header("Limit")]
	[SerializeField]
	private LimitMode limitMode = LimitMode.Multiple;

	[SerializeField]
	private int multipleLimit = 3;

	[Header("Stop text overlap")]
	[SerializeField]
	private float textDistance = 30f;

	[SerializeField]
	private Vector2 textPush = new Vector2(2000f, 500f);

	[SerializeField]
	private float textLerp = 0.8f;

	[SerializeField]
	private float textNudgeDecrease = 0.5f;

	private Aircraft aircraft;

	private Transform iconLayer;

	private readonly List<ObjectiveOverlay> overlays = new List<ObjectiveOverlay>();

	private List<MissionPosition.PositionResult> resultCache = new List<MissionPosition.PositionResult>();

	public void Initialize(Aircraft aircraft, Transform iconLayer)
	{
		this.aircraft = aircraft;
		this.iconLayer = iconLayer;
	}

	private ObjectiveOverlay CreateItem()
	{
		ObjectiveOverlay objectiveOverlay = UnityEngine.Object.Instantiate(overlayPrefab, base.transform);
		objectiveOverlay.Initialize(iconLayer);
		return objectiveOverlay;
	}

	private void Update()
	{
		if (!(aircraft == null) && !(aircraft.NetworkHQ == null) && MissionManager.Runner != null)
		{
			UpdateOverlays();
			StopTextOverlap();
		}
	}

	private void UpdateOverlays()
	{
		List<MissionPosition.PositionResult> allPositions = GetAllPositions();
		while (overlays.Count < allPositions.Count)
		{
			overlays.Add(CreateItem());
		}
		for (int i = 0; i < overlays.Count; i++)
		{
			ObjectiveOverlay objectiveOverlay = overlays[i];
			if (i < allPositions.Count)
			{
				objectiveOverlay.UpdateOverlay(allPositions[i]);
			}
			else
			{
				objectiveOverlay.HideOverlay();
			}
		}
	}

	private List<MissionPosition.PositionResult> GetAllPositions()
	{
		switch (limitMode)
		{
		case LimitMode.One:
		{
			resultCache.Clear();
			if (MissionPosition.TryGetClosestObjectivePosition(aircraft, out var result))
			{
				resultCache.Add(result);
			}
			break;
		}
		case LimitMode.Multiple:
			MissionPosition.GetAllPositionsResults(aircraft, includeHidden: false, resultCache);
			if (resultCache.Count > multipleLimit)
			{
				resultCache.Sort(delegate(MissionPosition.PositionResult a, MissionPosition.PositionResult b)
				{
					float distance = a.Distance;
					return distance.CompareTo(b.Distance);
				});
				resultCache.RemoveRange(multipleLimit, resultCache.Count - multipleLimit);
			}
			break;
		case LimitMode.NoLimit:
			MissionPosition.GetAllPositionsResults(aircraft, includeHidden: false, resultCache);
			break;
		}
		return resultCache;
	}

	private void StopTextOverlap()
	{
		for (int i = 0; i < overlays.Count; i++)
		{
			ObjectiveOverlay objectiveOverlay = overlays[i];
			for (int j = i + 1; j < overlays.Count; j++)
			{
				ObjectiveOverlay objectiveOverlay2 = overlays[j];
				TextNoOverlap textNoOverlap = objectiveOverlay.TextNoOverlap;
				TextNoOverlap textNoOverlap2 = objectiveOverlay2.TextNoOverlap;
				Vector2 targetPosition = textNoOverlap.TargetPosition;
				Vector2 targetPosition2 = textNoOverlap2.TargetPosition;
				if (Vector2.Distance(targetPosition, targetPosition2) < textDistance)
				{
					Vector2 vector = targetPosition2 - targetPosition;
					if (vector.y < 0.1f)
					{
						vector.y += 5f;
					}
					vector.Normalize();
					float deltaTime = Time.deltaTime;
					Vector2 vector2 = new Vector2(vector.x * textPush.x * deltaTime, vector.y * textPush.y * deltaTime);
					textNoOverlap.NudgeOffset += vector2;
					textNoOverlap2.NudgeOffset -= vector2;
				}
			}
		}
		foreach (ObjectiveOverlay overlay in overlays)
		{
			TextNoOverlap textNoOverlap3 = overlay.TextNoOverlap;
			Vector2 vector3 = Vector2.Lerp(textNoOverlap3.PreviousPosition, textNoOverlap3.TargetPosition + textNoOverlap3.NudgeOffset, textLerp);
			textNoOverlap3.Text.transform.position = vector3;
			textNoOverlap3.PreviousPosition = vector3;
			textNoOverlap3.NudgeOffset *= textNudgeDecrease;
			textNoOverlap3.AutomaticlalySetPosition = false;
		}
	}
}
