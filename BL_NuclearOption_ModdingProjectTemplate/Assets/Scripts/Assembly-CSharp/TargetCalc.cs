using UnityEngine;

public static class TargetCalc
{
	public static Quaternion FromToRotation(Quaternion start, Quaternion end)
	{
		return Quaternion.Inverse(start) * end;
	}

	public static float GetAngleOnAxis(Vector3 self, Vector3 other, Vector3 axis)
	{
		Vector3 vector = Vector3.Cross(axis, self);
		Vector3 to = Vector3.Cross(axis, other);
		return Vector3.SignedAngle(vector, to, axis);
	}

	public static Vector3 GetLeadVector(GlobalPosition targetPos, GlobalPosition platformPos, Vector3 targetVel, Vector3 platformVel, float maxLead)
	{
		float a = Vector3.Dot((targetPos - platformPos).normalized, platformVel - targetVel);
		return Mathf.Clamp(FastMath.Distance(platformPos, targetPos) / Mathf.Max(a, 10f), 0f, maxLead) * targetVel;
	}

	public static Vector3 GetLeadVectorWithAccel(GlobalPosition targetPos, GlobalPosition platformPos, Vector3 targetVel, Vector3 platformVel, Vector3 targetAccel, float maxLead)
	{
		targetAccel = Vector3.ClampMagnitude(targetAccel, 100f) + 9.81f * Vector3.up;
		float a = Vector3.Dot((targetPos - platformPos).normalized, platformVel - targetVel);
		float value = FastMath.Distance(platformPos, targetPos) / Mathf.Max(a, 10f);
		value = Mathf.Clamp(value, 0f, maxLead);
		return value * targetVel + Mathf.Min(value * value, 1f) * 0.5f * targetAccel;
	}

	public static float TargetLeadTime(Unit target, GameObject gun, Rigidbody muzzleRB, float muzzleVelocity, float dragCoef, int iterations)
	{
		Vector3 vector = ((target.rb != null) ? target.rb.velocity : Vector3.zero);
		float num = muzzleVelocity;
		if (muzzleRB != null)
		{
			num += Vector3.Dot(muzzleRB.velocity, gun.transform.forward);
		}
		float num2 = 0f;
		for (int i = 0; i < iterations; i++)
		{
			float num3 = Vector3.Distance(target.transform.position + vector * num2, gun.transform.position);
			num2 = (Mathf.Pow(2.71828f, dragCoef * num3 / num) - 1f) / dragCoef;
			if (!float.IsFinite(num2) || num2 > 120f)
			{
				return 120f;
			}
		}
		return num2;
	}

	public static void GetLeadFromMaxTargetSpeed(Unit target, Transform targetTransform, Transform fromTransform, GlobalPosition oldKnownPos, float maxTargetSpeed, out GlobalPosition newKnownPos, out Vector3 knownVel)
	{
		Vector3 vector = oldKnownPos.ToLocalPosition();
		Vector3 position = targetTransform.position;
		newKnownPos = position.ToGlobalPosition();
		knownVel = ((target.rb != null) ? target.rb.velocity : Vector3.zero);
		if (target.speed > maxTargetSpeed && Vector3.ProjectOnPlane(target.rb.velocity, (position - fromTransform.position).normalized).sqrMagnitude > maxTargetSpeed * maxTargetSpeed)
		{
			position = vector + maxTargetSpeed * Time.fixedDeltaTime * (position - vector).normalized;
			position = Vector3.Lerp(position, targetTransform.position, 0.5f * Time.fixedDeltaTime);
			newKnownPos = position.ToGlobalPosition();
			knownVel = (position - vector) * Time.fixedDeltaTime;
		}
	}

	public static bool LineOfSight(Transform source, Transform destination, float radius)
	{
		if (Physics.Linecast(source.position, destination.position, out var hitInfo, PhysicsLayers.StaticsMask))
		{
			return FastMath.InRange(hitInfo.point, destination.position, radius);
		}
		return true;
	}

	public static bool ClosestPointsOnTwoLines(out Vector3 closestPointLine1, out Vector3 closestPointLine2, Vector3 linePoint1, Vector3 lineVec1, Vector3 linePoint2, Vector3 lineVec2)
	{
		closestPointLine1 = Vector3.zero;
		closestPointLine2 = Vector3.zero;
		float num = Vector3.Dot(lineVec1, lineVec1);
		float num2 = Vector3.Dot(lineVec1, lineVec2);
		float num3 = Vector3.Dot(lineVec2, lineVec2);
		float num4 = num * num3 - num2 * num2;
		if (num4 != 0f)
		{
			Vector3 rhs = linePoint1 - linePoint2;
			float num5 = Vector3.Dot(lineVec1, rhs);
			float num6 = Vector3.Dot(lineVec2, rhs);
			float num7 = (num2 * num6 - num5 * num3) / num4;
			float num8 = (num * num6 - num5 * num2) / num4;
			closestPointLine1 = linePoint1 + lineVec1 * num7;
			closestPointLine2 = linePoint2 + lineVec2 * num8;
			return true;
		}
		return false;
	}
}
