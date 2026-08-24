using System;
using UnityEngine;

public class HighLiftDevice : MonoBehaviour
{
	[Serializable]
	private class MovingPart
	{
		[SerializeField]
		private bool move;

		[SerializeField]
		private bool rotate;

		[SerializeField]
		private Transform transform;

		[SerializeField]
		private Vector3 positionRetracted;

		[SerializeField]
		private Vector3 positionDeployed;

		[SerializeField]
		private Vector3 anglesRetracted;

		[SerializeField]
		private Vector3 anglesDeployed;

		public void Animate(float deployedAmount)
		{
			if (move)
			{
				transform.localPosition = Vector3.Lerp(positionRetracted, positionDeployed, deployedAmount);
			}
			if (rotate)
			{
				transform.localEulerAngles = Vector3.Lerp(anglesRetracted, anglesDeployed, deployedAmount);
			}
		}
	}

	[SerializeField]
	private AeroPart aeroPart;

	[SerializeField]
	private SwingWingController swingWingController;

	[SerializeField]
	private float speedDeployed;

	[SerializeField]
	private float speedRetracted;

	[SerializeField]
	private float alphaMin;

	[SerializeField]
	private float alphaFactor;

	[SerializeField]
	private float deployedArea;

	[SerializeField]
	private MovingPart[] movingParts;

	private float partAreaDeployed;

	private float partAreaRetracted;

	private float position;

	private Aircraft aircraft;

	private void Start()
	{
		aircraft = aeroPart.parentUnit as Aircraft;
		partAreaRetracted = aeroPart.GetWingArea();
		partAreaDeployed = partAreaRetracted + deployedArea;
	}

	private void FixedUpdate()
	{
		float num = Mathf.Clamp01(1f - Mathf.Max(aircraft.speed - speedDeployed, 0f) / (speedRetracted - speedDeployed));
		if (swingWingController != null && swingWingController.GetSwingPosition() > 0f)
		{
			num = 0f;
		}
		if (alphaFactor > 0f)
		{
			Vector3 vector = base.transform.InverseTransformDirection(aircraft.rb.velocity);
			float f = Mathf.Atan2(vector.y, vector.z) * -57.29578f;
			num += Mathf.Clamp01(Mathf.Abs(f) - alphaMin) * alphaFactor;
		}
		if (Mathf.Abs(num - position) > 0.001f)
		{
			position += Mathf.Clamp(num - position, 0f - Time.fixedDeltaTime, Time.fixedDeltaTime);
			aeroPart.SetWingArea(Mathf.Lerp(partAreaRetracted, partAreaDeployed, position));
			MovingPart[] array = movingParts;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Animate(position);
			}
		}
	}
}
