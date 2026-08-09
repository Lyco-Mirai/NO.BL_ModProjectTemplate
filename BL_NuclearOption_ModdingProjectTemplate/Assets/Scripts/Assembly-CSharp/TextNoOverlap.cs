using TMPro;
using UnityEngine;

public class TextNoOverlap
{
	public readonly TextMeshProUGUI Text;

	public Vector2 TargetPosition;

	public Vector2 PreviousPosition;

	public Vector2 NudgeOffset;

	public bool AutomaticlalySetPosition;

	public TextNoOverlap(TextMeshProUGUI objectiveInfo)
	{
		Text = objectiveInfo;
	}

	public void SetTarget(Vector2 target)
	{
		TargetPosition = target;
		if (AutomaticlalySetPosition)
		{
			Text.transform.position = target;
		}
	}
}
