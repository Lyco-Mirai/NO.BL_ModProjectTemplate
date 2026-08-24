using System;
using UnityEngine;

[Serializable]
public class HangarDoor
{
	public Transform transform;

	public AnimatedPhysicsSurface animatedPhysicsSurface;

	private Vector3 baseAngle;

	public Vector3 openAngle;

	private Vector3 basePos;

	public Vector3 openPos;

	private float openAmountPrev;

	public void Initialize(float openAmount)
	{
		baseAngle = transform.localEulerAngles;
		basePos = transform.localPosition;
		Move(openAmount);
	}

	public void Move(float openAmount)
	{
		transform.localEulerAngles = Vector3.Lerp(baseAngle, baseAngle + openAngle, openAmount);
		transform.localPosition = Vector3.Lerp(basePos, basePos + openPos, openAmount);
		if (animatedPhysicsSurface != null && Time.deltaTime > 0f && openAmount != openAmountPrev)
		{
			animatedPhysicsSurface.SetAnimationVelocity(openPos * ((openAmount - openAmountPrev) / Time.fixedDeltaTime));
		}
		openAmountPrev = openAmount;
	}
}
