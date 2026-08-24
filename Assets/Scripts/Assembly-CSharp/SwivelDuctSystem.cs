using System;
using Rewired;
using UnityEngine;

public class SwivelDuctSystem : MonoBehaviour, INozzleGauge
{
	private enum SwivelDuctMode
	{
		Forward = 0,
		ShortTakeoff = 1,
		ShortLanding = 2,
		Hover = 3,
		Manual = 4
	}

	[Serializable]
	private struct Thruster
	{
		[SerializeField]
		private Turbojet jet;

		[SerializeField]
		private UnitPart part;

		[SerializeField]
		private Transform thrustTransform;

		[SerializeField]
		private float maxThrust;

		[SerializeField]
		private float rollFactor;

		[SerializeField]
		private float minSwivel;

		[SerializeField]
		private float minAlt;

		[SerializeField]
		private float maxSpeed;

		public void Update(ControlInputs inputs, float swivelAmount, float alt, float speed)
		{
			if (!(swivelAmount < minSwivel) && !(speed > maxSpeed) && !(alt < minAlt))
			{
				float num = Mathf.Max(maxThrust * jet.GetThrustRatio() * inputs.roll * rollFactor, 0f);
				part.rb.AddForceAtPosition(num * thrustTransform.forward, thrustTransform.position);
			}
		}
	}

	[Serializable]
	private struct ThrustDoor
	{
		[SerializeField]
		private Transform transform;

		[SerializeField]
		private Vector3 minAngle;

		[SerializeField]
		private Vector3 maxAngle;

		public void Animate(float amount)
		{
			transform.localEulerAngles = Vector3.Lerp(minAngle, maxAngle, amount);
		}
	}

	[Serializable]
	private struct MovingPanel
	{
		[SerializeField]
		private Transform transform;

		[SerializeField]
		private Vector3 minPosition;

		[SerializeField]
		private Vector3 maxPosition;

		[SerializeField]
		private Vector3 minRotation;

		[SerializeField]
		private Vector3 maxRotation;

		[SerializeField]
		private float extraDrag;

		[SerializeField]
		private UnitPart unitPart;

		private float amountPrev;

		public void Animate(float amount)
		{
			if (amount != amountPrev)
			{
				transform.localPosition = Vector3.Lerp(minPosition, maxPosition, amount);
				transform.localEulerAngles = Vector3.Lerp(minRotation, maxRotation, amount);
			}
			amountPrev = amount;
		}

		public void ApplyDrag(float amount)
		{
			if (extraDrag > 0f)
			{
				Vector3 vector = unitPart.rb.velocity - unitPart.parentUnit.GetWindVelocity();
				unitPart.rb.AddForce(amount * unitPart.parentUnit.airDensity * vector.sqrMagnitude * -vector.normalized);
			}
		}
	}

	[Serializable]
	private struct Bearing
	{
		[SerializeField]
		private Transform transform;

		[SerializeField]
		private Vector3 minAngles;

		[SerializeField]
		private Vector3 maxAngles;

		[SerializeField]
		private Vector3 yawFactor;

		[SerializeField]
		private AnimationCurve adjustmentCurve;

		[SerializeField]
		private bool useCurve;

		private float amountPrev;

		private float yawPrev;

		public void Animate(float amount, ControlInputs inputs)
		{
			if (amountPrev != amount || (!(yawFactor == Vector3.zero) && yawPrev != inputs.yaw))
			{
				amountPrev = amount;
				yawPrev = inputs.yaw;
				if (useCurve)
				{
					amount = adjustmentCurve.Evaluate(amount);
				}
				transform.localEulerAngles = Vector3.Lerp(minAngles, maxAngles, amount) + yawFactor * inputs.yaw;
			}
		}
	}

	[Serializable]
	private class LiftFan
	{
		[SerializeField]
		private UnitPart part;

		[SerializeField]
		private Transform thrustTransform;

		[SerializeField]
		private Transform nozzleThrustTransform;

		[SerializeField]
		private Transform bladeTransform;

		[SerializeField]
		private Turbojet jet;

		[SerializeField]
		private float thrustProportion;

		[SerializeField]
		private float pitchFactor;

		[SerializeField]
		private float parasiticLoss;

		[SerializeField]
		private float rotateSpeed;

		private float lastCoMCheck;

		private float lastOffsetCalc;

		private float fanCoMDist;

		private float nozzleCoMDist;

		private Vector3 centerOfMass;

		private void CheckCoM()
		{
			if (!(Time.timeSinceLevelLoad - lastCoMCheck < 10f))
			{
				lastCoMCheck = Time.timeSinceLevelLoad;
				Vector3 vector = part.parentUnit.GetCenterOfMass();
				centerOfMass = part.parentUnit.transform.InverseTransformPoint(vector);
				fanCoMDist = Vector3.Dot(thrustTransform.position - vector, part.xform.forward);
			}
		}

		private void CalcOffsets()
		{
			if (!(Time.timeSinceLevelLoad - lastOffsetCalc < 1f))
			{
				lastOffsetCalc = Time.timeSinceLevelLoad;
				Vector3 vector = part.parentUnit.transform.InverseTransformPoint(nozzleThrustTransform.position);
				nozzleCoMDist = Vector3.Dot(centerOfMass - vector, Vector3.forward);
			}
		}

		public void Update(float pitchInput, float doorsOpenAmount, float swivelAmount)
		{
			if (doorsOpenAmount > 0f)
			{
				CheckCoM();
				CalcOffsets();
				bladeTransform.Rotate(Vector3.up * rotateSpeed * Time.deltaTime);
				Vector3 vector = part.parentUnit.transform.TransformPoint(centerOfMass);
				Vector3 lhs = jet.GetThrust() * nozzleThrustTransform.forward;
				Vector3 vector2 = nozzleThrustTransform.position - vector;
				Vector3 vector3 = Vector3.Cross(lhs, -vector2);
				Vector3 vector4 = part.parentUnit.transform.InverseTransformVector(vector3);
				Vector3 vector5 = Vector3.Cross(rhs: -(thrustTransform.position - vector), lhs: thrustTransform.forward);
				Vector3 vector6 = part.parentUnit.transform.InverseTransformVector(vector5);
				float num = (0f - vector4.x) / vector6.x;
				num += doorsOpenAmount * pitchInput * pitchFactor * jet.GetRPMRatio();
				part.rb.AddForceAtPosition(Mathf.Max(num, 0f) * thrustTransform.forward, thrustTransform.position);
				jet.SetParasiticLoss(parasiticLoss * swivelAmount);
			}
			else
			{
				jet.SetParasiticLoss(0f);
			}
		}
	}

	private SwivelDuctMode swivelDuctMode;

	[SerializeField]
	private Aircraft aircraft;

	[SerializeField]
	private Bearing[] bearings;

	[SerializeField]
	private LiftFan[] liftFans;

	[SerializeField]
	private MovingPanel[] movingPanels;

	[SerializeField]
	private ThrustDoor[] thrustDoors;

	[SerializeField]
	private Thruster[] thrusters;

	[SerializeField]
	private float maxSwivelAirspeed = 139f;

	[SerializeField]
	private float swivelSpeed;

	[SerializeField]
	private float thrustDoorSpeed;

	[SerializeField]
	private float shortLandingSpeed;

	[SerializeField]
	private float shortTakeoffSpeed;

	private float swivelPosition;

	private float swivelPositionPrev;

	private float thrustDoorPosition;

	private ControlInputs inputs;

	private float timeOnGround;

	private float timeAirborne;

	private Player player;

	public float GetNozzleAngle()
	{
		return swivelPosition * 90f;
	}

	private void CheckForManualInput(bool autoHoverEnabled)
	{
		if (aircraft.Player == null)
		{
			swivelDuctMode = SwivelDuctMode.Manual;
		}
		else if (!autoHoverEnabled && GameManager.IsLocalAircraft(aircraft))
		{
			float num = Mathf.Clamp(player.GetAxisRaw("Custom Axis 1"), -1f, 1f);
			float num2 = Mathf.Clamp(player.GetAxisRawPrev("Custom Axis 1"), -1f, 1f);
			float num3 = Mathf.Abs(num - num2);
			bool flag = player.GetButton("Axis Modifier") && player.GetAxisRaw("Throttle") != 0f;
			if ((num3 > 0f && num3 < 0.5f) || Mathf.Abs(num) > 0.5f || flag)
			{
				swivelDuctMode = SwivelDuctMode.Manual;
			}
		}
	}

	private void Awake()
	{
		inputs = aircraft.GetInputs();
		inputs.customAxis1 = 1f;
		swivelDuctMode = SwivelDuctMode.Forward;
		player = ReInput.players.GetPlayer(0);
	}

	private void AutoSwivelMode(bool autoHoverEnabled)
	{
		if (timeOnGround > 1f)
		{
			if (swivelDuctMode == SwivelDuctMode.Hover)
			{
				if (inputs.brake < 0.5f && inputs.throttle < 0.3f)
				{
					swivelDuctMode = SwivelDuctMode.Forward;
				}
			}
			else if (aircraft.speed > shortTakeoffSpeed && aircraft.speed < shortTakeoffSpeed * 1.5f && inputs.throttle > 0.9f)
			{
				swivelDuctMode = SwivelDuctMode.ShortTakeoff;
			}
			else if (aircraft.speed < shortTakeoffSpeed)
			{
				swivelDuctMode = SwivelDuctMode.Forward;
			}
		}
		else
		{
			if (!(timeAirborne > 5f))
			{
				return;
			}
			float num = Vector3.Dot(aircraft.transform.forward, aircraft.rb.velocity);
			if (aircraft.gearDeployed)
			{
				if (swivelDuctMode == SwivelDuctMode.Forward)
				{
					swivelDuctMode = SwivelDuctMode.ShortLanding;
				}
				if (swivelDuctMode == SwivelDuctMode.Hover && inputs.throttle > 0.95f && num > 30f)
				{
					swivelDuctMode = SwivelDuctMode.ShortTakeoff;
				}
			}
			else if (aircraft.speed > 60f)
			{
				swivelDuctMode = SwivelDuctMode.Forward;
			}
		}
	}

	private void LocalSimFixedUpdate()
	{
		timeAirborne = ((aircraft.radarAlt > 0.2f) ? (timeAirborne + Time.fixedDeltaTime) : 0f);
		timeOnGround = ((aircraft.radarAlt < 0.2f) ? (timeOnGround + Time.fixedDeltaTime) : 0f);
		SwivelDuctMode swivelDuctMode = this.swivelDuctMode;
		bool flag = aircraft.IsAutoHoverEnabled();
		if (aircraft.speed > maxSwivelAirspeed)
		{
			this.swivelDuctMode = SwivelDuctMode.Forward;
		}
		else
		{
			CheckForManualInput(flag);
			if (flag)
			{
				this.swivelDuctMode = SwivelDuctMode.Hover;
			}
			else if (this.swivelDuctMode != SwivelDuctMode.Manual && aircraft.LocalSim)
			{
				AutoSwivelMode(flag);
			}
		}
		if (this.swivelDuctMode != swivelDuctMode && SceneSingleton<CombatHUD>.i?.aircraft == aircraft)
		{
			SceneSingleton<AircraftActionsReport>.i.ReportText($"Vectoring Mode set to {this.swivelDuctMode}", 4f);
		}
		if (!aircraft.LocalSim)
		{
			this.swivelDuctMode = SwivelDuctMode.Manual;
		}
		switch (this.swivelDuctMode)
		{
		case SwivelDuctMode.Forward:
			inputs.customAxis1 = 1f;
			if (aircraft.radarAlt < 1f && aircraft.speed < shortTakeoffSpeed * 1.5f)
			{
				float b = Mathf.Lerp(1f, 0.5f, (aircraft.speed - 20f) / (shortTakeoffSpeed - 20f));
				inputs.customAxis1 = Mathf.Min(inputs.customAxis1, b);
			}
			break;
		case SwivelDuctMode.ShortTakeoff:
			inputs.customAxis1 = 0.5f;
			break;
		case SwivelDuctMode.Hover:
			inputs.customAxis1 = 0f;
			break;
		case SwivelDuctMode.ShortLanding:
		{
			float t = (aircraft.speed - shortLandingSpeed * 0.9f) / (shortLandingSpeed * 1.1f - shortLandingSpeed * 0.9f);
			inputs.customAxis1 = Mathf.Lerp(0.2f, 0f, t);
			break;
		}
		}
	}

	private void FixedUpdate()
	{
		if (aircraft.LocalSim)
		{
			LocalSimFixedUpdate();
		}
		float num = 1f - inputs.customAxis1;
		swivelPosition += Mathf.Clamp(num - swivelPosition, (0f - swivelSpeed) * Time.fixedDeltaTime, swivelSpeed * Time.fixedDeltaTime);
		Bearing[] array = bearings;
		foreach (Bearing bearing in array)
		{
			bearing.Animate(swivelPosition, inputs);
		}
		if (swivelPosition != swivelPositionPrev)
		{
			MovingPanel[] array2 = movingPanels;
			foreach (MovingPanel movingPanel in array2)
			{
				movingPanel.Animate(swivelPosition);
			}
		}
		if (swivelPosition > 0f)
		{
			MovingPanel[] array2 = movingPanels;
			foreach (MovingPanel movingPanel2 in array2)
			{
				movingPanel2.ApplyDrag(swivelPosition);
			}
		}
		int num2 = ((inputs.customAxis1 < 1f) ? 1 : 0);
		thrustDoorPosition += Mathf.Clamp((float)num2 - thrustDoorPosition, (0f - thrustDoorSpeed) * Time.fixedDeltaTime, thrustDoorSpeed * Time.fixedDeltaTime);
		ThrustDoor[] array3 = thrustDoors;
		foreach (ThrustDoor thrustDoor in array3)
		{
			thrustDoor.Animate(thrustDoorPosition);
		}
		LiftFan[] array4 = liftFans;
		for (int i = 0; i < array4.Length; i++)
		{
			array4[i].Update(inputs.pitch, thrustDoorPosition, swivelPosition);
		}
		Thruster[] array5 = thrusters;
		foreach (Thruster thruster in array5)
		{
			thruster.Update(inputs, swivelPosition, aircraft.radarAlt, aircraft.speed);
		}
		swivelPositionPrev = swivelPosition;
	}
}
