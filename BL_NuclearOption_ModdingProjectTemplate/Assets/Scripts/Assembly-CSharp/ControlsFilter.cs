using System;
using UnityEngine;
using UnityEngine.Serialization;

public class ControlsFilter : MonoBehaviour
{
	[Serializable]
	private class AutoTrimmer
	{
		public bool Enabled;

		[SerializeField]
		private float gLimit;

		[SerializeField]
		private float cornerSpeed;

		[SerializeField]
		private float maxRollRate;

		[SerializeField]
		private float maxRollRateSpeed;

		[SerializeField]
		private float aoaLimit;

		[SerializeField]
		private float aoaLimiterStrength;

		[SerializeField]
		private Vector3 correctionStrength;

		[SerializeField]
		private Vector3 trimP;

		[SerializeField]
		private Vector3 trimD;

		[SerializeField]
		private Vector3 trimLimit;

		[SerializeField]
		private LandingGear noseGear;

		private Vector3 trim;

		private Vector3 angularErrorPrev;

		public void Trim(Aircraft aircraft, ControlInputs inputs, Vector3 localAngularVelocity)
		{
			float num = 1.225f / aircraft.airDensity;
			float num2 = Mathf.Max(aircraft.speed * num, 10f);
			inputs.pitch *= Mathf.Min(num2 * num2 / cornerSpeed * cornerSpeed, 1f);
			float x = gLimit * 9.81f / Mathf.Max(aircraft.speed, cornerSpeed);
			float z = maxRollRate * Mathf.Clamp(aircraft.speed * num / maxRollRateSpeed, 0.2f, 1f);
			Vector3 vector = Vector3.Scale(new Vector3(x, 0f, z), new Vector3(inputs.pitch, inputs.yaw, inputs.roll));
			Vector3 vector2 = localAngularVelocity - vector;
			Vector3 a = (vector2 - angularErrorPrev) * (1f / Time.fixedDeltaTime);
			angularErrorPrev = vector2;
			trim += Vector3.Scale(-vector2, trimP) - Vector3.Scale(a, trimD);
			trim = new Vector3(Mathf.Clamp(trim.x, 0f - trimLimit.x, trimLimit.x), 0f, Mathf.Clamp(trim.z, 0f - trimLimit.z, trimLimit.z));
			if (noseGear.WeightOnWheel(0.05f))
			{
				trim = Vector3.zero;
			}
			Vector3 vector3 = Vector3.Scale(correctionStrength, vector2) * Mathf.Min(cornerSpeed / num2, 1f);
			inputs.pitch += trim.x - vector3.x;
			inputs.yaw += trim.y - vector3.y;
			inputs.roll += trim.z - vector3.z;
		}
	}

	[Serializable]
	public class FlyByWire
	{
		public bool Enabled;

		[SerializeField]
		private float gLimitPositive = 9f;

		[SerializeField]
		private float opacity = 1f;

		[FormerlySerializedAs("breakawaySpeed")]
		[SerializeField]
		private float cornerSpeed = 170f;

		[SerializeField]
		private float postStallManeuverSpeed = 180f;

		[SerializeField]
		private float maxRollSpeed = 300f;

		[SerializeField]
		private float takeoffSpeed = 40f;

		[SerializeField]
		private float pidTransitionSpeed = 150f;

		[SerializeField]
		private float maxPitchAngularVel = 1f;

		[SerializeField]
		private float maxRollAngularVel = 6f;

		[SerializeField]
		private float alphaLimiter = 25f;

		[SerializeField]
		private float alphaLimiterStrength = 0.05f;

		[SerializeField]
		private float pFactorFast = 10f;

		[SerializeField]
		private float iFactor = 0.01f;

		[SerializeField]
		private float dFactorFast = 1f;

		[SerializeField]
		private float rollTrimRate = 0.1f;

		[SerializeField]
		private float rollTrimLimit = 0.1f;

		[SerializeField]
		private float yawTightness = 1f;

		[SerializeField]
		private float yawWeathervaning;

		[SerializeField]
		private float rollTightness = 1f;

		[SerializeField]
		private Vector3 inputSmoothing;

		[SerializeField]
		private LandingGear noseGear;

		private float pFactorSlow;

		private float dFactorSlow;

		private Vector3 inputSmoothingVel;

		private Vector3 inputsSmoothed;

		private Vector3 rawInputs;

		private float pPrev;

		private float i;

		private float pitchAdjuster;

		private float rollTrim;

		private float limitFactorSmoothed;

		private float remapFactor;

		private float targetPitchAngVel;

		private float pitchAngVel;

		public (bool, float[]) GetParameters()
		{
			float[] item = new float[15]
			{
				0f, maxPitchAngularVel, cornerSpeed, postStallManeuverSpeed, pidTransitionSpeed, 0f, pFactorSlow, dFactorSlow, 0f, pFactorFast,
				dFactorFast, rollTrimRate, rollTrimLimit, yawTightness, rollTightness
			};
			return (Enabled, item);
		}

		public void ApplyParameters(bool enabled, float[] parameters)
		{
			Enabled = enabled;
			maxPitchAngularVel = parameters[1];
			cornerSpeed = parameters[2];
			postStallManeuverSpeed = parameters[3];
			pidTransitionSpeed = parameters[4];
			pFactorSlow = parameters[6];
			dFactorSlow = parameters[7];
			pFactorFast = parameters[9];
			dFactorFast = parameters[10];
			rollTrimRate = parameters[11];
			rollTrimLimit = parameters[12];
			yawTightness = parameters[13];
			rollTightness = parameters[14];
		}

		public float GetRemapFactor()
		{
			return remapFactor;
		}

		public float GetPitchTrim()
		{
			return pitchAdjuster;
		}

		public float GetPitchAngVel()
		{
			return pitchAngVel;
		}

		public float GetTargetPitchAngVel()
		{
			return targetPitchAngVel;
		}

		public float GetRawPitch()
		{
			return rawInputs.x;
		}

		public void Filter(Aircraft aircraft, ControlInputs inputs, Vector3 localAngularVelocity, bool stabilityAssist)
		{
			if (!stabilityAssist && aircraft.IsAutoHoverEnabled())
			{
				stabilityAssist = true;
			}
			float num = cornerSpeed * cornerSpeed * 1.225f;
			float num2 = aircraft.speed * aircraft.speed * aircraft.airDensity / num;
			remapFactor = 1f / Mathf.Max(num2, 1f);
			limitFactorSmoothed = Mathf.Lerp(limitFactorSmoothed, (stabilityAssist || (double)num2 > 1.2) ? 1 : 0, Time.fixedDeltaTime);
			rawInputs = new Vector3(inputs.pitch, inputs.yaw, inputs.roll);
			if (inputSmoothing != Vector3.zero)
			{
				inputsSmoothed.x = Mathf.SmoothDamp(inputsSmoothed.x, inputs.pitch, ref inputSmoothingVel.x, inputSmoothing.x);
				inputsSmoothed.y = Mathf.SmoothDamp(inputsSmoothed.y, inputs.yaw, ref inputSmoothingVel.y, inputSmoothing.y);
				inputsSmoothed.z = Mathf.SmoothDamp(inputsSmoothed.z, inputs.roll, ref inputSmoothingVel.z, inputSmoothing.z);
				inputs.pitch = inputsSmoothed.x;
				inputs.roll = inputsSmoothed.y;
				inputs.roll = inputsSmoothed.z;
			}
			pitchAngVel = localAngularVelocity.x;
			targetPitchAngVel = inputs.pitch * gLimitPositive * 9.81f / Mathf.Max(aircraft.speed, cornerSpeed * 0.75f);
			if (num2 < 1f)
			{
				targetPitchAngVel *= Mathf.Clamp(num2, 0.3f, 1f);
				Vector3 vector = Quaternion.Inverse(aircraft.transform.rotation) * aircraft.rb.velocity;
				float f = Mathf.Atan2(vector.y, vector.z) * 57.29578f;
				if (Mathf.Abs(f) > alphaLimiter && Mathf.Sign(f) == Mathf.Sign(targetPitchAngVel))
				{
					float value = Mathf.Abs(f) - alphaLimiter;
					targetPitchAngVel *= 1f - Mathf.Clamp(value, 0f, 10f) * alphaLimiterStrength;
				}
			}
			targetPitchAngVel = Mathf.Lerp(inputs.pitch * maxPitchAngularVel, targetPitchAngVel, limitFactorSmoothed);
			if (aircraft.gearDeployed && aircraft.radarAlt < 0.1f && aircraft.speed < takeoffSpeed * 1.5f)
			{
				if (aircraft.speed < takeoffSpeed)
				{
					float num3 = Mathf.Lerp(1f - 4f * Time.fixedDeltaTime, 1f, aircraft.speed / takeoffSpeed);
					pitchAdjuster *= num3;
				}
				if (noseGear != null && noseGear.WeightOnWheel(0.05f))
				{
					targetPitchAngVel = Mathf.Clamp(targetPitchAngVel, -0.2f, localAngularVelocity.x);
				}
			}
			float num4 = Mathf.Clamp(localAngularVelocity.x - targetPitchAngVel, -0.25f, 0.25f);
			i = Mathf.Clamp(i + num4 * Time.fixedDeltaTime, -0.2f, 0.2f);
			float num5 = (num4 - pPrev) / Time.fixedDeltaTime;
			pPrev = num4;
			float value2 = (0f - (num4 * pFactorFast + i * iFactor + num5 * dFactorFast)) * remapFactor;
			pitchAdjuster += Mathf.Clamp(value2, -2f, 2f) * Time.fixedDeltaTime;
			pitchAdjuster = Mathf.Clamp(pitchAdjuster, -1f, 1f);
			inputs.pitch = Mathf.Clamp(pitchAdjuster, -1f, 1f);
			if (opacity < 1f)
			{
				inputs.pitch = Mathf.Lerp(rawInputs.x, inputs.pitch, opacity);
				inputs.yaw = Mathf.Lerp(rawInputs.y, inputs.yaw, opacity);
				inputs.roll = Mathf.Lerp(rawInputs.z, inputs.roll, opacity);
			}
			float num6 = yawTightness * remapFactor * (localAngularVelocity.y - inputs.yaw);
			if (yawWeathervaning > 0f)
			{
				float angleOnAxis = TargetCalc.GetAngleOnAxis(aircraft.transform.forward, aircraft.rb.velocity, aircraft.transform.up);
				num6 -= yawWeathervaning * angleOnAxis;
			}
			inputs.yaw = Mathf.Clamp(0f - num6, -1f, 1f);
			float num7 = localAngularVelocity.z - inputs.roll * (0f - maxRollAngularVel) * Mathf.Clamp(num2 / maxRollSpeed, 0.5f, 1f);
			if (Mathf.Abs(localAngularVelocity.x) < 0.1f && aircraft.radarAlt > 0.5f)
			{
				rollTrim += Mathf.Clamp(Mathf.Clamp(num7, -0.1f, 0.1f), (0f - rollTrimRate) * Time.fixedDeltaTime, rollTrimRate * Time.fixedDeltaTime);
			}
			rollTrim = Mathf.Clamp(rollTrim, 0f - rollTrimLimit, rollTrimLimit);
			if (num2 > maxRollSpeed)
			{
				inputs.roll *= maxRollSpeed * maxRollSpeed / (num2 * num2);
			}
			inputs.roll = Mathf.Lerp(inputs.roll, num7 * rollTightness, remapFactor);
			inputs.roll = Mathf.Clamp(inputs.roll + rollTrim * remapFactor, -1f, 1f);
		}
	}

	[Serializable]
	protected class AutoHover
	{
		public bool Enabled;

		public bool Active;

		[SerializeField]
		private bool setFlightAssistOff;

		[SerializeField]
		private float customAxis1Position;

		[SerializeField]
		private float errorGain = 0.1f;

		[SerializeField]
		private float sensitivity;

		[SerializeField]
		private float hoverBaseThrottle = 0.5f;

		[SerializeField]
		private float climbSensitivity = 8f;

		[SerializeField]
		private float customAxisSlowDown;

		[SerializeField]
		private float correctionStrength = 0.5f;

		[SerializeField]
		private float maxSpeed = 30f;

		[SerializeField]
		private PIDFactors attitudePIDFactors;

		[SerializeField]
		private PIDFactors altitudePIDFactors;

		[SerializeField]
		private float lastShipCheck;

		public bool storedFlightAssistState;

		private float throttleOverride;

		private PID pitchPID;

		private PID rollPID;

		private PID altitudePID;

		private AircraftParameters aircraftParameters;

		protected Vector3 surfaceVelocity;

		private void CheckNearbyShip(FactionHQ faction, GlobalPosition position)
		{
			if (!(Time.timeSinceLevelLoad - lastShipCheck < 3f))
			{
				lastShipCheck = Time.timeSinceLevelLoad;
				if (faction != null && faction.TryGetNearestShip(position, out var nearestShip, out var nearestDistance) && nearestDistance < 250000f)
				{
					surfaceVelocity = nearestShip.rb.velocity;
				}
				else
				{
					surfaceVelocity = Vector3.zero;
				}
			}
		}

		public void Hover(ControlInputs inputs, Aircraft aircraft)
		{
			if (!Enabled || !Active)
			{
				return;
			}
			if (aircraft.radarAlt < 0.1f)
			{
				aircraft.GetControlsFilter().SetAutoHover(enabled: false);
				return;
			}
			CheckNearbyShip(aircraft.NetworkHQ, aircraft.GlobalPosition());
			if (pitchPID == null)
			{
				aircraftParameters = aircraft.GetAircraftParameters();
				pitchPID = new PID(attitudePIDFactors);
				rollPID = new PID(attitudePIDFactors);
				altitudePID = new PID(altitudePIDFactors);
			}
			Vector3 vector = surfaceVelocity;
			vector.y = 0f;
			Vector3 vector2 = new Vector3(aircraft.rb.velocity.x, 0f, aircraft.rb.velocity.z) - vector;
			float num = Vector3.Dot(vector2, aircraft.transform.forward);
			vector2 = Vector3.ClampMagnitude(vector2, Mathf.Sqrt(Mathf.Min(vector2.magnitude, maxSpeed)));
			Vector3 other = Vector3.up - vector2 * errorGain;
			float angleOnAxis = TargetCalc.GetAngleOnAxis(aircraft.transform.up, other, aircraft.transform.right);
			float angleOnAxis2 = TargetCalc.GetAngleOnAxis(aircraft.transform.up, other, -aircraft.transform.forward);
			float num2 = Mathf.Clamp(pitchPID.GetOutput(angleOnAxis * sensitivity, 1f, Time.fixedDeltaTime, new Vector3(attitudePIDFactors.P, attitudePIDFactors.I, attitudePIDFactors.D)), 0f - correctionStrength, correctionStrength);
			float num3 = Mathf.Clamp(rollPID.GetOutput(angleOnAxis2 * sensitivity, 1f, Time.fixedDeltaTime, new Vector3(attitudePIDFactors.P, attitudePIDFactors.I, attitudePIDFactors.D)), 0f - correctionStrength, correctionStrength);
			inputs.pitch = Mathf.Clamp(inputs.pitch + num2, -1f, 1f);
			inputs.roll = Mathf.Clamp(inputs.roll + num3, -1f, 1f);
			inputs.customAxis1 = Mathf.Clamp01(customAxis1Position + num * customAxisSlowDown);
			float num4 = (inputs.throttle - hoverBaseThrottle) * climbSensitivity;
			if (Mathf.Abs(inputs.throttle - hoverBaseThrottle) < 0.1f)
			{
				num4 = 0f;
			}
			float num5 = aircraft.rb.velocity.y - num4;
			float output = altitudePID.GetOutput(0f - num5, 2f, Time.fixedDeltaTime, new Vector3(altitudePIDFactors.P, altitudePIDFactors.I, altitudePIDFactors.D));
			throttleOverride = Mathf.Clamp01(throttleOverride + output);
			inputs.throttle = throttleOverride;
		}

		public void Set(Aircraft aircraft, bool enabled)
		{
			Active = enabled;
		}

		public void Toggle(Aircraft aircraft)
		{
			Active = !Active;
			if (setFlightAssistOff)
			{
				if (Active)
				{
					storedFlightAssistState = aircraft.flightAssist;
					aircraft.SetFlightAssist(enabled: false);
				}
				else
				{
					aircraft.SetFlightAssist(storedFlightAssistState);
				}
			}
			if (Active)
			{
				throttleOverride = aircraft.GetInputs().throttle;
			}
			if (GameManager.IsLocalAircraft(aircraft))
			{
				string report = (Active ? "Auto Hover <b>Enabled</b>" : "Auto Hover <b>Disabled</b>");
				SceneSingleton<AircraftActionsReport>.i.ReportText(report, 5f);
			}
		}
	}

	[Serializable]
	protected class GLimiter
	{
		public bool Enabled;

		[SerializeField]
		private float gLimit;

		[SerializeField]
		private float limitStrength = 1f;

		[SerializeField]
		private float predictionStrength = 0.5f;

		[SerializeField]
		private float predictionTime = 1f;

		[SerializeField]
		private float smoothing = 0.05f;

		[SerializeField]
		private float rollonRate = 1f;

		[SerializeField]
		private float rolloffRate = 0.1f;

		private float gPrev;

		private float gRateSmoothed;

		private float smoothingVel;

		private float limiterStrength;

		private float inputMagnitudeAtOverG;

		public void LimitG(ControlInputs inputs, Aircraft aircraft, float inverseDynamicPressure)
		{
			if (!Enabled)
			{
				gPrev = 0f;
				return;
			}
			Mathf.Clamp01(1f - inverseDynamicPressure);
			float num = Vector3.Dot(aircraft.accel + Vector3.up * 1f, aircraft.transform.up);
			float target = (num - gPrev) / Time.fixedDeltaTime;
			gRateSmoothed = FastMath.SmoothDamp(gRateSmoothed, target, ref smoothingVel, smoothing);
			float num2 = Mathf.Abs(num) - gLimit;
			float num3 = Mathf.Abs(num + gRateSmoothed * predictionTime) - gLimit;
			if (num2 + num3 > 0f)
			{
				limiterStrength += (num2 * limitStrength + num3 * predictionStrength) * rollonRate * Time.fixedDeltaTime;
			}
			else
			{
				limiterStrength -= rolloffRate * Time.fixedDeltaTime;
			}
			if (num2 > 0f)
			{
				inputMagnitudeAtOverG = Mathf.Abs(inputs.pitch);
			}
			else
			{
				inputMagnitudeAtOverG = Mathf.Lerp(inputMagnitudeAtOverG, 1f, 0.5f * Time.fixedDeltaTime);
			}
			limiterStrength = Mathf.Clamp01(limiterStrength);
			if (gPrev != 0f)
			{
				inputs.pitch *= 1f - limiterStrength;
			}
			inputs.pitch = Mathf.Clamp(inputs.pitch, 0f - inputMagnitudeAtOverG, inputMagnitudeAtOverG);
			gPrev = num;
		}
	}

	[Serializable]
	protected class AngularVelocityDamper
	{
		public bool Enabled;

		[SerializeField]
		private float pitchDamping;

		[SerializeField]
		private float pitchDampingLimit;

		[SerializeField]
		private float rollDamping;

		[SerializeField]
		private float rollDampingLimit;

		[SerializeField]
		private float yawDamping;

		[SerializeField]
		private float yawDampingLimit;

		[SerializeField]
		private float yawDampingGround;

		[Range(0f, 2f)]
		[SerializeField]
		private float assistOffOpacity;

		[Range(0f, 2f)]
		[SerializeField]
		private float assistOnOpacity;

		[Range(1f, 3f)]
		[SerializeField]
		private float gearDownMultiplier;

		public void DampAngularVelocity(ControlInputs inputs, Vector3 localAngularVel, float inverseDynamicPressure, bool assist, float gearDownFactor, bool onGround)
		{
			if (Enabled)
			{
				float num = (assist ? assistOnOpacity : assistOffOpacity);
				num *= Mathf.Lerp(1f, gearDownMultiplier, gearDownFactor);
				inputs.pitch -= pitchDamping * localAngularVel.x * Mathf.Min(inverseDynamicPressure, pitchDampingLimit) * num;
				inputs.yaw -= (onGround ? yawDampingGround : yawDamping) * localAngularVel.y * Mathf.Min(inverseDynamicPressure, yawDampingLimit);
				inputs.roll -= (0f - rollDamping) * localAngularVel.z * Mathf.Min(inverseDynamicPressure, rollDampingLimit);
			}
		}
	}

	[Serializable]
	protected class SpeedRemap
	{
		public bool Enabled;

		[SerializeField]
		private float referenceSpeed = 160f;

		[SerializeField]
		private float zeroEffectSpeed = 200f;

		[SerializeField]
		private float fullEffectSpeed = 340f;

		[SerializeField]
		private bool remapRoll;

		public void Remap(ControlInputs inputs, float dynamicPressure, float speed)
		{
			float num = Mathf.Clamp01(referenceSpeed * referenceSpeed * 1.2f / dynamicPressure);
			float num2 = zeroEffectSpeed * zeroEffectSpeed * 1.2f;
			float num3 = fullEffectSpeed * fullEffectSpeed * 1.2f;
			float t = (dynamicPressure - num2) / (num3 - num2);
			inputs.pitch = Mathf.Lerp(inputs.pitch, inputs.pitch * num, t);
			if (remapRoll)
			{
				inputs.roll = Mathf.Lerp(inputs.roll, inputs.roll * num, t);
			}
		}
	}

	[Serializable]
	protected class ResponseRateLimiter
	{
		public bool Enabled;

		[SerializeField]
		private float pitchRateCenter = 1f;

		[SerializeField]
		private float pitchRateMid = 0.5f;

		[SerializeField]
		private float pitchRateLimits = 0.1f;

		private float pitchPrev;

		public void LimitResponseRate(ControlInputs inputs)
		{
			if (Enabled)
			{
				float num = pitchRateCenter;
				float num2 = Mathf.Abs(pitchPrev);
				num = ((!(num2 < 0.5f)) ? Mathf.Lerp(pitchRateMid, pitchRateLimits, num2 * 2f - 1f) : Mathf.Lerp(pitchRateCenter, pitchRateMid, num2 * 2f));
				float num3 = inputs.pitch - pitchPrev;
				if (inputs.pitch > 0f && num3 > 0f)
				{
					inputs.pitch = Mathf.Min(inputs.pitch, pitchPrev + num * Time.fixedDeltaTime);
				}
				if (inputs.pitch < 0f && num3 < 0f)
				{
					inputs.pitch = Mathf.Max(inputs.pitch, pitchPrev - num * Time.fixedDeltaTime);
				}
				pitchPrev = inputs.pitch;
			}
		}
	}

	[Serializable]
	protected class AoALimiter
	{
		public bool Enabled;

		[SerializeField]
		private float maxAlpha = 30f;

		[SerializeField]
		private float pGain = 2f;

		[SerializeField]
		private float dGain = 0.1f;

		[SerializeField]
		private float minSpeed = 60f;

		private float alphaPrev;

		public void LimitAoA(ControlInputs inputs, Aircraft aircraft, bool assist)
		{
			if (Enabled && assist && !(aircraft.speed < minSpeed))
			{
				Vector3 vector = aircraft.cockpit.transform.InverseTransformDirection(aircraft.cockpit.rb.velocity);
				float num = Mathf.Max(Mathf.Atan2(vector.y, vector.z) * -57.29578f, 0f);
				float num2 = pGain * (num - maxAlpha) / maxAlpha;
				if (num2 > 0f)
				{
					inputs.pitch += Mathf.Clamp01(num2);
				}
				alphaPrev = num;
			}
		}
	}

	[Serializable]
	protected class AimAssist
	{
		public bool Enabled;

		private Vector3 correction;

		private Vector3 smoothedCorrection;

		private Vector3 correctionSmoothingVel;

		private Vector3 CCIPCorrection;

		private Vector3 CCIPCorrectionSmoothed;

		private Vector3 CCIPCorrectionVel;

		private Vector3 targetVector;

		private Vector3 ccipVector;

		private Vector3 targetVelPrev;

		private Vector3 targetAccelSmoothed;

		private Vector3 targetAccelSmoothingVel;

		private GlobalPosition? accurateAimpoint;

		private GlobalPosition? accurateImpactPoint;

		private float lastSim;

		private float lastAimRequest;

		private float targetDist;

		private float targetSpeed;

		private float timeToTargetCorrection;

		private Unit currentTarget;

		[SerializeField]
		private PIDFactors pitchPID;

		[SerializeField]
		private PIDFactors yawPID;

		[SerializeField]
		private PIDFactors rollPID;

		private PID xPID;

		private PID yPID;

		private PID zPID;

		private ControlInputs inputs;

		public void GetAim(Unit target, Aircraft aircraft, out GlobalPosition? aimPoint, out GlobalPosition? impactPoint)
		{
			if (target != currentTarget)
			{
				currentTarget = target;
				if (inputs == null)
				{
					inputs = aircraft.GetInputs();
					xPID = new PID(pitchPID);
					yPID = new PID(yawPID);
					zPID = new PID(rollPID);
				}
			}
			CalcAim(target, aircraft);
			aimPoint = accurateAimpoint;
			impactPoint = accurateImpactPoint;
		}

		public void CalcAim(Unit target, Aircraft aircraft)
		{
			accurateAimpoint = null;
			accurateImpactPoint = null;
			WeaponStation currentWeaponStation = aircraft.weaponManager.currentWeaponStation;
			if (currentWeaponStation.WeaponInfo.muzzleVelocity == 0f || currentWeaponStation.HasTurret() || !aircraft.NetworkHQ.TryGetKnownPosition(target, out var knownPosition) || FastMath.OutOfRange(knownPosition, aircraft.GlobalPosition(), currentWeaponStation.WeaponInfo.targetRequirements.maxRange))
			{
				return;
			}
			Weapon weapon = currentWeaponStation.Weapons[0];
			if (!(Vector3.Angle(weapon.transform.forward, knownPosition - aircraft.GlobalPosition()) > 40f))
			{
				lastAimRequest = Time.timeSinceLevelLoad;
				GlobalPosition globalPosition = weapon.transform.GlobalPosition();
				targetDist = FastMath.Distance(knownPosition, globalPosition);
				targetSpeed = target.speed;
				Vector3 vector = ((targetSpeed < 1f) ? Vector3.zero : target.rb.velocity);
				Vector3 target2 = (vector - targetVelPrev) / Time.fixedDeltaTime;
				targetVelPrev = vector;
				float num = Vector3.Dot(aircraft.rb.velocity - vector + weapon.transform.forward * weapon.info.muzzleVelocity, FastMath.NormalizedDirection(globalPosition, knownPosition));
				float num2 = targetDist / num;
				float num3 = num2 + timeToTargetCorrection;
				targetAccelSmoothed = Vector3.SmoothDamp(targetAccelSmoothed, target2, ref targetAccelSmoothingVel, 0.25f);
				Vector3 vector2 = num3 * vector + 0.5f * num3 * num3 * targetAccelSmoothed;
				Vector3 vector3 = num3 * num3 * 4.905f * Vector3.up;
				GlobalPosition globalPosition2 = knownPosition + vector2 + vector3;
				Vector3 initialVelocity = aircraft.rb.velocity + weapon.transform.forward * currentWeaponStation.WeaponInfo.muzzleVelocity;
				if (Time.timeSinceLevelLoad - lastSim > 0.1f)
				{
					lastSim = Time.timeSinceLevelLoad;
					float timeStep = Mathf.Lerp(0.02f, 0.1f, targetDist * 0.0003f);
					Vector3 initialVelocity2 = aircraft.rb.velocity + FastMath.NormalizedDirection(globalPosition, globalPosition2) * weapon.info.muzzleVelocity;
					Kinematics.TrajectorySim(PlayerSettings.debugVis && FastMath.InRange(globalPosition, SceneSingleton<CameraStateManager>.i.transform.GlobalPosition(), 100f), currentWeaponStation.WeaponInfo, initialVelocity2, globalPosition, knownPosition, vector, targetAccelSmoothed, timeStep, out var missVector, out var timeToTarget);
					Kinematics.TrajectorySim(debug: false, currentWeaponStation.WeaponInfo, initialVelocity, globalPosition, knownPosition, vector, targetAccelSmoothed, timeStep, out var missVector2, out var _);
					CCIPCorrection = missVector2;
					correction = -missVector;
					timeToTargetCorrection = timeToTarget - num2;
				}
				smoothedCorrection = Vector3.SmoothDamp(smoothedCorrection, correction, ref correctionSmoothingVel, 0.15f);
				CCIPCorrectionSmoothed = Vector3.SmoothDamp(CCIPCorrectionSmoothed, CCIPCorrection, ref CCIPCorrectionVel, 0.15f);
				accurateAimpoint = globalPosition2 + smoothedCorrection;
				accurateImpactPoint = knownPosition + CCIPCorrectionSmoothed;
				targetVector = knownPosition - globalPosition;
				ccipVector = accurateImpactPoint.Value - globalPosition;
			}
		}

		public void Assist(Aircraft aircraft)
		{
			if (Time.timeSinceLevelLoad - lastAimRequest > 0.2f)
			{
				if (currentTarget != null)
				{
					currentTarget = null;
					if (inputs != null)
					{
						xPID.Reseti();
						yPID.Reseti();
						zPID.Reseti();
					}
				}
				return;
			}
			float angleOnAxis = TargetCalc.GetAngleOnAxis(ccipVector, targetVector, aircraft.transform.right);
			float angleOnAxis2 = TargetCalc.GetAngleOnAxis(ccipVector, targetVector, aircraft.transform.up);
			float num = 2f - (angleOnAxis + angleOnAxis2) * 0.25f;
			num *= targetDist * 0.001f - 1f;
			if (targetSpeed < 40f && Mathf.Abs(angleOnAxis) < 2f && Mathf.Abs(angleOnAxis2) < 2f && targetDist > 500f)
			{
				num = Mathf.Clamp01(num);
				inputs.pitch *= Mathf.Lerp(1f, 0.2f, num);
				inputs.yaw *= Mathf.Lerp(1f, 0.3f, num);
				Vector3 other = Vector3.up + aircraft.transform.right * angleOnAxis2 * 0.1f;
				float currentError = 0f - TargetCalc.GetAngleOnAxis(aircraft.transform.up, other, aircraft.transform.forward);
				float output = xPID.GetOutput(angleOnAxis, Time.fixedDeltaTime);
				float output2 = yPID.GetOutput(angleOnAxis2, Time.fixedDeltaTime);
				float output3 = zPID.GetOutput(currentError, Time.fixedDeltaTime);
				inputs.pitch += Mathf.Clamp(output * num, -0.1f, 0.1f);
				inputs.yaw += Mathf.Clamp(output2 * num, -0.1f, 0.1f);
				inputs.roll += Mathf.Clamp(output3 * num, -0.05f, 0.05f);
			}
		}
	}

	[SerializeField]
	private float minSpeed = 25f;

	[SerializeField]
	private float minAlt = 1f;

	[SerializeField]
	protected string flightAssistName;

	[SerializeField]
	protected FlyByWire flyByWire;

	[SerializeField]
	protected AutoHover autoHover;

	[SerializeField]
	protected AimAssist aimAssist;

	[SerializeField]
	private bool flightAssistDefault;

	public bool ReverseThrust;

	protected bool autoHoverActive;

	protected AircraftParameters aircraftParameters;

	protected Aircraft aircraft;

	protected Autopilot autopilot;

	protected float gearDownSmoothed;

	protected float gearDownSmoothingVel;

	protected float landingSpeed;

	private GlobalPosition? hoverTarget;

	public event Action OnSetAutoHover;

	public bool HasAutoHover()
	{
		return autoHover.Enabled;
	}

	public void ToggleAutoHover()
	{
		if (autoHover.Enabled && !(aircraft.radarAlt < 1f))
		{
			autoHover.Toggle(aircraft);
			this.OnSetAutoHover?.Invoke();
		}
	}

	public void SetAutoHover(bool enabled)
	{
		if (autoHover.Enabled)
		{
			if (aircraft.radarAlt < 1f)
			{
				enabled = false;
			}
			autoHover.Set(aircraft, enabled);
			this.OnSetAutoHover?.Invoke();
		}
	}

	public bool IsAutoHoverEnabled()
	{
		return autoHover.Active;
	}

	public FlyByWire GetFlyByWire()
	{
		return flyByWire;
	}

	public (bool, float[]) GetFlyByWireParameters()
	{
		return flyByWire.GetParameters();
	}

	public void SetFlyByWireParameters(bool enabled, float[] parameters)
	{
		flyByWire.ApplyParameters(enabled, parameters);
	}

	public void SetFlightAssist(bool enabled, Aircraft aircraft)
	{
		this.aircraft = aircraft;
		if (HasFlightAssist() && GameManager.IsLocalAircraft(aircraft))
		{
			string report = (enabled ? (flightAssistName + " <b>Enabled</b>") : (flightAssistName + " <b>Disabled</b>"));
			SceneSingleton<AircraftActionsReport>.i.ReportText(report, 5f);
		}
	}

	public bool HasFlightAssist()
	{
		return !string.IsNullOrEmpty(flightAssistName);
	}

	public bool FlightAssistDefault()
	{
		return flightAssistDefault;
	}

	public void GetAim(Unit target, out GlobalPosition? aimPoint, out GlobalPosition? impactPoint)
	{
		aimAssist.GetAim(target, aircraft, out aimPoint, out impactPoint);
	}

	public virtual void Filter(ControlInputs inputs, Vector3 rawInputs, Rigidbody rb, float gForce, bool flightAssist)
	{
		if (!(aircraft.speed < minSpeed) && !(aircraft.radarAlt < minAlt))
		{
			gearDownSmoothed = FastMath.SmoothDamp(gearDownSmoothed, (aircraft.gearDeployed && aircraft.speed < 140f) ? 1 : 0, ref gearDownSmoothingVel, 1f);
			Vector3 localAngularVelocity = rb.transform.InverseTransformDirection(rb.angularVelocity);
			if (autoHover.Enabled && autoHover.Active)
			{
				autoHover.Hover(inputs, aircraft);
			}
			if (aimAssist.Enabled && flightAssist)
			{
				aimAssist.Assist(aircraft);
			}
			if (flyByWire.Enabled)
			{
				flyByWire.Filter(aircraft, inputs, localAngularVelocity, flightAssist);
			}
		}
	}
}
