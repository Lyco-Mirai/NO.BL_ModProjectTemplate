using System;
using UnityEngine;

[Serializable]
[CreateAssetMenu(fileName = "MoveablePartAnimation", menuName = "ScriptableObjects/MoveablePartAnimation", order = 9)]
public class MoveablePartAnimation : ScriptableObject
{
	[SerializeField]
	private Vector3 rotationVector = Vector3.zero;

	[SerializeField]
	private float rotationSpeed;

	[SerializeField]
	private float rotationMax;

	private float rotationVariableSpeed;

	[SerializeField]
	private float rotationRandomSpeed;

	[SerializeField]
	private float windRotationEffect;

	[SerializeField]
	private bool windHeadingDependent;

	[SerializeField]
	private bool windRotationSpeedDependent;

	[SerializeField]
	private Vector3 translationVector = Vector3.zero;

	[SerializeField]
	private float translationSpeed;

	[SerializeField]
	private float translationMax;

	public virtual void Initialize(Transform transform)
	{
		rotationVariableSpeed = rotationSpeed + UnityEngine.Random.Range(0f - rotationRandomSpeed, rotationRandomSpeed);
		transform.localEulerAngles = rotationVector * UnityEngine.Random.Range(0f - rotationMax, rotationMax);
	}

	public virtual void Animate(Transform transform)
	{
		if (!(transform != null) || !(NetworkSceneSingleton<LevelInfo>.i != null))
		{
			return;
		}
		if (windHeadingDependent)
		{
			float num = 180f + NetworkSceneSingleton<LevelInfo>.i.GetWindHeading();
			if (num > 360f)
			{
				num -= 360f;
			}
			float y = transform.localEulerAngles.y;
			y = Mathf.MoveTowardsAngle(y, num, windRotationEffect * NetworkSceneSingleton<LevelInfo>.i.windVelocity.magnitude * Time.fixedDeltaTime);
			y = Mathf.Clamp(y, 0f - rotationMax, rotationMax);
			transform.localEulerAngles = rotationVector * y;
		}
		else if (windRotationSpeedDependent)
		{
			rotationVariableSpeed = Mathf.Lerp(rotationVariableSpeed, windRotationEffect * NetworkSceneSingleton<LevelInfo>.i.windSpeed, 0.5f);
			rotationVariableSpeed = Mathf.Clamp(rotationVariableSpeed, 0f, rotationMax);
			rotationVariableSpeed = Mathf.Max(0f, rotationVariableSpeed);
			transform.localEulerAngles += rotationVector * rotationVariableSpeed * Time.deltaTime;
		}
		else
		{
			transform.localEulerAngles += rotationVector * rotationVariableSpeed * Time.deltaTime;
		}
	}
}
