using UnityEngine;

public class OpticalSeekerCruiseMissile : MissileSeeker
{
	[SerializeField]
	private float armRange;

	[SerializeField]
	private float altitudeTarget;

	[SerializeField]
	private float formationSpacing;

	[SerializeField]
	private float terminalRange = 2000f;

	[SerializeField]
	private float terminalSearchRadius;

	[SerializeField]
	private float finDelay = 1f;

	[SerializeField]
	private float tangibleDelay = 2f;

	[SerializeField]
	private float guidanceDelay = 1f;

	[SerializeField]
	private float maxTargetSpeed;

	[SerializeField]
	private VLSBooster booster;

	private Transform targetPart;

	private bool terminalMode;

	private bool guidance;

	private bool finsDeployed;

	private bool initialBoostMode;

	private GlobalPosition knownPos;

	private GlobalPosition aimPos;

	private Vector3 knownVel;

	private Vector3 terrainClearVector;

	private float lastTerminalCheck;

	private float altitudeTrim;

	private FactionHQ targetHQAtLaunch;

	[SerializeField]
	private JinkEvasion jinkEvasion;

	[SerializeField]
	private TopAttack topAttack;

	[SerializeField]
	private TerminalBoost terminalBoost;

	public override void Initialize(Unit target, GlobalPosition aimpoint)
	{
		targetUnit = target;
		missile.onDisableUnit += OpticalSeekerCruiseMissile_OnMissileDisabled;
		if (missile.NetworkHQ != null)
		{
			missile.NetworkHQ.RegisterCruiseMissile(missile);
		}
		terrainClearVector = base.transform.forward * 1000f;
		Vector3 vector = missile.transform.forward;
		vector.y = 0f;
		knownPos = aimpoint;
		if (target != null && missile.NetworkHQ != null)
		{
			targetHQAtLaunch = target.NetworkHQ;
			if (missile.NetworkHQ.TryGetKnownPosition(target, out knownPos))
			{
				vector = knownPos - missile.GlobalPosition();
				vector.y = 0f;
			}
			if (topAttack.Amount > 0f)
			{
				float num = Random.Range(0f, 1f);
				if (target.maxRadius < 20f || FastMath.InRange(knownPos, missile.GlobalPosition(), topAttack.TooCloseRange) || num > topAttack.probability)
				{
					topAttack.Amount = 0f;
				}
			}
		}
		aimPos = missile.GlobalPosition() + (missile.transform.forward + vector.normalized * 0.1f) * 100000f;
		knownVel = Vector3.zero;
		missile.SetAimpoint(aimPos, Vector3.zero);
		this.StartSlowUpdateDelayed(1f, SlowChecks);
	}

	private void SlowChecks()
	{
		if (!missile.disabled)
		{
			missile.UpdateRadarAlt();
			if (missile.timeSinceSpawn > 10f && (missile.LosingGround() || missile.MissedTarget() || targetUnit == null || missile.speed < 100f))
			{
				missile.Detonate(missile.rb.velocity, hitArmor: false, hitTerrain: false);
			}
			if (!missile.IsTangible() && missile.timeSinceSpawn > 2f)
			{
				missile.SetTangible(tangible: true);
			}
		}
	}

	public override string GetSeekerType()
	{
		return "INS / Opt.";
	}

	public GlobalPosition TerrainWaypoint(GlobalPosition destination)
	{
		destination.y = Mathf.Max(destination.y, Datum.LocalSeaY + altitudeTarget);
		Vector3 target = destination - missile.GlobalPosition();
		Vector3 velocity = missile.rb.velocity;
		velocity.y = 0f;
		target = Vector3.RotateTowards(velocity.normalized, target, 0.17453292f, 0f);
		float num = 1f;
		if (missile.NetworkHQ != null)
		{
			foreach (Missile cruiseMissile in missile.NetworkHQ.GetCruiseMissiles())
			{
				if (FastMath.InRange(cruiseMissile.GlobalPosition(), base.transform.GlobalPosition(), 5000f))
				{
					Vector3 vector = base.transform.position - cruiseMissile.transform.position;
					Vector3 normalized = vector.normalized;
					float a = formationSpacing * formationSpacing / vector.sqrMagnitude;
					a = Mathf.Min(a, 0.03f);
					vector.y = Mathf.Max(vector.y, 0f);
					target += vector.normalized * a;
					float num2 = Vector3.Dot(-normalized, base.transform.forward);
					num += num2 * 0.03f;
				}
			}
		}
		missile.SetThrottle(Mathf.Clamp(num, 0.8f, 1f));
		float num3 = Mathf.Max(missile.speed, 100f) * 6f;
		target.y = 0f;
		Vector3 vector2 = base.transform.position + target.normalized * num3;
		vector2.y = Datum.LocalSeaY;
		if (Physics.Linecast(vector2 + Vector3.up * 5000f, vector2 - Vector3.up * 5000f, out var hitInfo, (int)PhysicsLayers.StaticsMask | (int)PhysicsLayers.ExclusionZonesMask))
		{
			vector2 = hitInfo.point;
			vector2.y = Mathf.Max(vector2.y, Datum.LocalSeaY);
		}
		else
		{
			vector2.y = Datum.LocalSeaY;
		}
		vector2 += Vector3.up * altitudeTarget;
		if (missile.radarAlt < altitudeTarget * 2f)
		{
			float num4 = altitudeTarget - (missile.radarAlt + missile.rb.velocity.y * 4f);
			altitudeTrim += num4;
			altitudeTrim = Mathf.Max(altitudeTrim, 0f);
			vector2 += altitudeTrim * Vector3.up;
		}
		else
		{
			altitudeTrim = 0f;
		}
		Vector3 vector3 = Vector3.Lerp(terrainClearVector, vector2 - base.transform.position, 0.8f);
		if (!Physics.Linecast(base.transform.position - Vector3.up * 2f, base.transform.position + vector3 * 0.9f, (int)PhysicsLayers.StaticsMask | (int)PhysicsLayers.ExclusionZonesMask))
		{
			terrainClearVector = vector3;
		}
		else
		{
			terrainClearVector = Vector3.Lerp(terrainClearVector, Vector3.up * 1000f, 0.1f);
		}
		float num5 = base.transform.position.y - Datum.LocalSeaY;
		terrainClearVector.y = Mathf.Max(terrainClearVector.y, 0f - (num5 - altitudeTarget));
		if (PlayerSettings.debugVis)
		{
			GameObject obj = Object.Instantiate(GameAssets.i.waypointDebug, Datum.origin);
			obj.transform.position = base.transform.position + terrainClearVector;
			obj.transform.LookAt(base.transform);
			Object.Destroy(obj, 0.5f);
			GameObject obj2 = Object.Instantiate(GameAssets.i.debugArrow, base.transform);
			obj2.transform.rotation = Quaternion.LookRotation(vector3);
			obj2.transform.localScale = new Vector3(1f, 1f, vector3.magnitude);
			Object.Destroy(obj2, 0.5f);
			GameObject obj3 = Object.Instantiate(GameAssets.i.debugArrowGreen, base.transform);
			obj3.transform.rotation = Quaternion.LookRotation(terrainClearVector);
			obj3.transform.localScale = new Vector3(1f, 1f, terrainClearVector.magnitude);
			Object.Destroy(obj3, 0.5f);
		}
		return missile.GlobalPosition() + terrainClearVector;
	}

	private void OpticalSeekerCruiseMissile_OnMissileDisabled(Unit unit)
	{
		missile.onDisableUnit -= OpticalSeekerCruiseMissile_OnMissileDisabled;
		if (missile.NetworkHQ != null)
		{
			missile.NetworkHQ.DeregisterCruiseMissile(missile);
		}
	}

	public void PreTerminalMode()
	{
		if (Time.timeSinceLevelLoad - lastTerminalCheck < 0.5f)
		{
			return;
		}
		lastTerminalCheck = Time.timeSinceLevelLoad;
		if (CheckWaypoint())
		{
			aimPos = knownPos;
		}
		GlobalPosition aimPoint = TerrainWaypoint(aimPos);
		missile.SetAimpoint(aimPoint, Vector3.zero);
		if (missile.timeSinceSpawn > 6f && FastMath.InRange(base.transform.GlobalPosition(), knownPos, terminalRange))
		{
			if (targetUnit != null && !targetUnit.disabled)
			{
				targetPart = targetUnit.GetRandomPart();
				terminalMode = true;
				missile.Arm();
			}
			else
			{
				missile.Detonate(missile.rb.velocity, hitArmor: false, hitTerrain: false);
			}
		}
	}

	public void TerminalMode()
	{
		if (targetUnit == null)
		{
			missile.Detonate(missile.rb.velocity, hitArmor: false, hitTerrain: false);
			return;
		}
		Vector3 vector = knownPos - missile.GlobalPosition();
		vector.y = 0f;
		float magnitude = vector.magnitude;
		if (targetUnit.LineOfSight(base.transform.position, 1000f))
		{
			if (magnitude < armRange)
			{
				missile.Arm();
			}
			TargetCalc.GetLeadFromMaxTargetSpeed(targetUnit, targetPart, base.transform, knownPos, maxTargetSpeed, out knownPos, out knownVel);
			knownPos.y = Mathf.Max(knownPos.y, 1f);
			aimPos = knownPos;
			if (jinkEvasion.amount > 0f)
			{
				aimPos += jinkEvasion.ApplyJink(base.transform.GlobalPosition(), targetUnit.GlobalPosition(), missile.speed, magnitude);
			}
			if (topAttack.Amount > 0f)
			{
				aimPos += topAttack.ApplyTopAttack(missile.GlobalPosition(), knownPos, missile.speed);
			}
			if (terminalBoost.Amount > 0f)
			{
				terminalBoost.ApplyTerminalBoost(missile, missile.GlobalPosition(), knownPos);
			}
			float num = Mathf.Max(magnitude / Mathf.Max(missile.speed, 10f));
			if (num < 4f)
			{
				aimPos += num * num * 4.905f * Vector3.up;
			}
			else if (missile.radarAlt < altitudeTarget)
			{
				aimPos += (altitudeTarget - 2f * (Mathf.Min(missile.rb.velocity.y, 0f) * num)) * Vector3.up;
			}
			missile.SetAimpoint(aimPos, knownVel);
		}
		else
		{
			missile.SetAimpoint(knownPos + Vector3.up * magnitude * 0.5f, knownVel);
		}
	}

	private void UpdateTargetParameters()
	{
		if (targetUnit != null && !targetUnit.disabled)
		{
			if (targetUnit.NetworkHQ != targetHQAtLaunch && targetUnit.NetworkHQ == missile.NetworkHQ && missile.timeSinceSpawn > 10f)
			{
				missile.Detonate(missile.rb.velocity, hitArmor: false, hitTerrain: false);
			}
			GlobalPosition? knownPosition = missile.NetworkHQ.GetKnownPosition(targetUnit);
			if (knownPosition.HasValue)
			{
				knownPos = knownPosition.Value;
			}
		}
	}

	public override void Seek()
	{
		if (!finsDeployed && missile.timeSinceSpawn > finDelay)
		{
			missile.DeployFins();
			finsDeployed = true;
		}
		if (!guidance)
		{
			if (missile.timeSinceSpawn > guidanceDelay)
			{
				guidance = true;
				aimPos = knownPos;
			}
		}
		else
		{
			UpdateTargetParameters();
			if (!terminalMode)
			{
				PreTerminalMode();
			}
			else
			{
				TerminalMode();
			}
		}
	}

	protected void OpticalSeekerCruiseMissile_ProcessSetDestination(ref UnitCommand.Command command)
	{
		if (command.FromPlayer && (command.player.Aircraft == null || command.player.Aircraft != missile.owner))
		{
			Debug.Log("Impossible to set waypoint for " + missile.unitName + " : not owner of missile");
			return;
		}
		if (FastMath.InRange(base.transform.GlobalPosition(), command.position, 0.5f * terminalRange) || FastMath.InRange(base.transform.GlobalPosition(), knownPos, 0.5f * terminalRange))
		{
			Debug.Log("[Cruise Missile] Impossible to set waypoint for " + missile.unitName + " : too close to missile or target");
			return;
		}
		Debug.Log("[Cruise Missile] Setting waypoint for " + missile.unitName + " at " + command.position);
		aimPos = command.position;
		aimPos.y = Mathf.Max(aimPos.y, 1f);
	}

	public bool CheckWaypoint()
	{
		if (!FastMath.InRange(missile.GlobalPosition(), aimPos, 1000f))
		{
			return Vector3.Dot(aimPos - missile.GlobalPosition(), missile.rb.velocity) < 0f;
		}
		return true;
	}
}
