using System.Collections.Generic;
using UnityEngine;

public static class Kinematics
{
	private static List<GameObject> visualizations = new List<GameObject>();

	public static GlobalPosition GetBallisticAimPoint(Missile missile, GlobalPosition knownPos, float timeToTarget, float maxTargetSpeed, float trajectoryError, Vector3 knownVel)
	{
		GlobalPosition globalPosition = missile.GlobalPosition() + missile.rb.velocity * 10000f;
		Vector3 vector = knownPos - missile.GlobalPosition();
		if (missile.rb.velocity.y < 0f || timeToTarget < 10f)
		{
			Vector3 targetVel = ((maxTargetSpeed < 1000f) ? Vector3.ClampMagnitude(knownVel, maxTargetSpeed) : knownVel);
			Vector3 leadVector = TargetCalc.GetLeadVector(knownPos, missile.GlobalPosition(), targetVel, missile.rb.velocity, 10f);
			float num = timeToTarget * timeToTarget * 4.905f;
			if (timeToTarget > 10f)
			{
				num = Mathf.Min(num, Mathf.Abs(vector.y * 0.5f));
			}
			return knownPos + leadVector + num * Vector3.up;
		}
		Vector3 vector2 = new Vector3(missile.rb.velocity.x, 0f, missile.rb.velocity.z);
		float num2 = missile.rb.velocity.y / vector2.magnitude;
		float num3 = Mathf.Clamp01((missile.timeSinceSpawn - 5f) * 0.01f);
		if (trajectoryError < 1f)
		{
			num2 += (trajectoryError - 1f) * num3;
		}
		vector.y = 0f;
		Vector3 normalized = vector.normalized;
		return missile.GlobalPosition() + (normalized + Vector3.up * num2) * 1000f;
	}

	public static float FallTime(float initialHeight, float initialVerticalVelocity)
	{
		float num = -4.905f;
		float num2 = initialVerticalVelocity * initialVerticalVelocity - 4f * num * initialHeight;
		if (num2 < 0f)
		{
			return -1f;
		}
		float num3 = (0f - initialVerticalVelocity + Mathf.Sqrt(num2)) / (2f * num);
		float num4 = (0f - initialVerticalVelocity - Mathf.Sqrt(num2)) / (2f * num);
		if (num3 < 0f)
		{
			num3 = num4;
		}
		if (num4 < 0f)
		{
			num4 = num3;
		}
		return Mathf.Min(num3, num4);
	}

	public static void TrajectorySim(bool debug, WeaponInfo weaponInfo, Vector3 initialVelocity, GlobalPosition initialPosition, GlobalPosition targetPos, Vector3 targetVel, Vector3 targetAccel, float timeStep, out Vector3 missVector, out float timeToTarget)
	{
		foreach (GameObject visualization in visualizations)
		{
			NetworkSceneSingleton<Spawner>.i.DestroyLocal(visualization, 0f);
		}
		visualizations.Clear();
		timeToTarget = 0f;
		GlobalPosition globalPosition = targetPos;
		Vector3 vector = targetVel;
		GlobalPosition globalPosition2 = initialPosition;
		Vector3 vector2 = initialVelocity;
		bool flag = false;
		int num = 0;
		while (!flag)
		{
			timeStep += 0.02f;
			GlobalPosition globalPosition3 = globalPosition2;
			GlobalPosition globalPosition4 = globalPosition;
			Vector3 vector3 = 9.81f * timeStep * weaponInfo.gravMult * Vector3.up + vector2.sqrMagnitude * weaponInfo.dragCoef * timeStep * vector2.normalized / weaponInfo.muzzleVelocity;
			vector += targetAccel * timeStep;
			globalPosition += vector * timeStep;
			vector2 -= vector3 * 0.3f;
			globalPosition2 += vector2 * timeStep;
			vector2 -= vector3 * 0.7f;
			if (Vector3.Dot(vector2, vector2 - vector) <= 0f)
			{
				flag = true;
			}
			timeToTarget += timeStep;
			if (Vector3.Dot(vector2, globalPosition - globalPosition2) <= 0f)
			{
				Vector3 vector4 = globalPosition2 - globalPosition;
				Vector3 onNormal = vector2 - vector;
				float num2 = Vector3.Project(vector4, onNormal).magnitude / onNormal.magnitude;
				timeToTarget -= num2;
				globalPosition2 -= vector2 * num2;
				globalPosition -= vector * num2;
				flag = true;
				if (debug)
				{
					GameObject gameObject = NetworkSceneSingleton<Spawner>.i.SpawnLocal(GameAssets.i.debugArrow, Datum.origin);
					gameObject.gameObject.GetComponent<MeshRenderer>().material.SetColor("_EmissionColor", Color.cyan);
					gameObject.transform.position = globalPosition2.ToLocalPosition();
					gameObject.transform.rotation = Quaternion.LookRotation(globalPosition - globalPosition2);
					gameObject.transform.localScale = new Vector3(2f, 2f, (globalPosition - globalPosition2).magnitude);
					visualizations.Add(gameObject);
				}
			}
			if (debug)
			{
				GameObject gameObject2 = NetworkSceneSingleton<Spawner>.i.SpawnLocal(GameAssets.i.debugArrow, Datum.origin);
				gameObject2.gameObject.GetComponent<MeshRenderer>().material.SetColor("_EmissionColor", Color.red * 4f);
				gameObject2.transform.position = globalPosition3.ToLocalPosition();
				gameObject2.transform.rotation = Quaternion.LookRotation(globalPosition2 - globalPosition3);
				gameObject2.transform.localScale = new Vector3(2f, 2f, (globalPosition2 - globalPosition3).magnitude);
				visualizations.Add(gameObject2);
				GameObject gameObject3 = NetworkSceneSingleton<Spawner>.i.SpawnLocal(GameAssets.i.debugArrow, Datum.origin);
				gameObject3.gameObject.GetComponent<MeshRenderer>().material.SetColor("_EmissionColor", Color.yellow * 0.5f);
				gameObject3.transform.position = globalPosition4.ToLocalPosition();
				gameObject3.transform.rotation = Quaternion.LookRotation(globalPosition - globalPosition4);
				gameObject3.transform.localScale = new Vector3(5f, 5f, (globalPosition - globalPosition4).magnitude);
				visualizations.Add(gameObject3);
			}
			num++;
			if (num > 100)
			{
				Debug.LogError($"max TrajectorySim iterations exceeded. InitialVelocity: {initialVelocity}, TargetVelocity: {targetVel}, TargetAccel: {targetAccel}, simVel: {vector2}");
				break;
			}
		}
		missVector = Vector3.ProjectOnPlane(globalPosition2 - globalPosition, vector2);
	}
}
