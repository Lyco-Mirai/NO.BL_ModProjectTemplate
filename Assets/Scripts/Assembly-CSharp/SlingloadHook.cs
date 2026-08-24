using UnityEngine;

public class SlingloadHook : Weapon
{
	public enum DeployState : byte
	{
		Retracted = 0,
		Deployed = 1,
		Connected = 2,
		Retracting = 3,
		RescuePilot = 4
	}

	public DeployState deployState;

	[SerializeField]
	public Transform winch;

	[SerializeField]
	public Transform hook;

	[SerializeField]
	private AudioSource winchAudioSource;

	[SerializeField]
	private AudioSource hookAudioSource;

	[SerializeField]
	private AudioClip hookSound;

	[SerializeField]
	private AudioClip unhookSound;

	[SerializeField]
	private AudioClip slingSnapSound;

	[SerializeField]
	private AudioClip winchStartSound;

	[SerializeField]
	private AudioClip winchStopSound;

	[SerializeField]
	private LineRenderer lineRenderer;

	[SerializeField]
	private float lineMaxLength = 20f;

	[SerializeField]
	private float reelingSpeed = 3f;

	[SerializeField]
	private float breakForce = 500000f;

	[SerializeField]
	private float breakAngle = 120f;

	private Aircraft aircraft;

	private float lastTransformSent;

	private float lineLength;

	private Unit suspendedUnit;

	private Rigidbody body;

	private ConfigurableJoint joint;

	public float loadForce;

	private Vector3 hookTargetPos;

	private bool loadInWater;

	public override void AttachToUnit(Unit unit)
	{
		base.AttachToUnit(unit);
		aircraft = attachedUnit as Aircraft;
		ammo = 1;
		body = GetComponentInParent<Rigidbody>();
		lineRenderer.enabled = false;
		lineRenderer.useWorldSpace = true;
	}

	public override int GetAmmoLoaded()
	{
		return 1;
	}

	public override int GetAmmoTotal()
	{
		return 1;
	}

	public override void Fire(Unit owner, Unit target, Vector3 inheritedVelocity, WeaponStation weaponStation, GlobalPosition aimpoint)
	{
		weaponStation.UpdateLastFired(0);
		lastFired = Time.timeSinceLevelLoad;
		SetState();
	}

	public void SetState()
	{
		switch (deployState)
		{
		case DeployState.Retracted:
			aircraft.SetSlingLoadAttachment(null, DeployState.Deployed);
			break;
		case DeployState.Connected:
			if (!(suspendedUnit is PilotDismounted))
			{
				hookAudioSource.PlayOneShot(unhookSound, 0.3f);
				if (GameManager.IsLocalAircraft(aircraft))
				{
					SceneSingleton<AircraftActionsReport>.i.ReportText("Load released - Retracting", 4f);
					aircraft.SetSlingLoadAttachment(suspendedUnit, DeployState.Retracting);
				}
			}
			else
			{
				if (GameManager.IsLocalAircraft(aircraft))
				{
					SceneSingleton<AircraftActionsReport>.i.ReportText("Start recovering pilot", 4f);
				}
				aircraft.SetSlingLoadAttachment(suspendedUnit, DeployState.RescuePilot);
				StartWinchSound();
			}
			break;
		case DeployState.RescuePilot:
			hookAudioSource.PlayOneShot(unhookSound, 0.3f);
			if (GameManager.IsLocalAircraft(aircraft))
			{
				SceneSingleton<AircraftActionsReport>.i.ReportText("Stop pilot recovery - Retracting", 4f);
				aircraft.SetSlingLoadAttachment(suspendedUnit, DeployState.RescuePilot);
			}
			break;
		case DeployState.Retracting:
			aircraft.SetSlingLoadAttachment(null, DeployState.Deployed);
			lineRenderer.enabled = true;
			break;
		case DeployState.Deployed:
			aircraft.SetSlingLoadAttachment(null, DeployState.Retracting);
			StartWinchSound();
			break;
		}
	}

	public void ApplyState(Unit suspendedUnit, DeployState deployState)
	{
		if (deployState == this.deployState)
		{
			return;
		}
		this.deployState = deployState;
		this.suspendedUnit = suspendedUnit;
		base.enabled = deployState != DeployState.Retracted;
		lineRenderer.enabled = base.enabled;
		switch (deployState)
		{
		case DeployState.Deployed:
			StartWinchSound();
			break;
		case DeployState.Retracting:
			if (attachedUnit.LocalSim && joint != null)
			{
				Object.Destroy(joint);
			}
			loadForce = 0f;
			StartWinchSound();
			break;
		case DeployState.Retracted:
			lineLength = 0f;
			UpdateRope();
			StopWinchSound();
			break;
		case DeployState.Connected:
			StopWinchSound();
			if (attachedUnit.LocalSim)
			{
				CreateJoint(suspendedUnit.rb, new Vector3(0f, 0.5f * suspendedUnit.definition.height, 0f));
			}
			if (GameManager.IsLocalAircraft(attachedUnit))
			{
				SceneSingleton<AircraftActionsReport>.i.ReportText("Load Connected", 4f);
			}
			hookAudioSource.PlayOneShot(hookSound, 0.3f);
			lineLength = (winch.position - suspendedUnit.transform.position + new Vector3(0f, 0.5f * suspendedUnit.definition.height, 0f)).magnitude;
			break;
		case DeployState.RescuePilot:
			StartWinchSound();
			break;
		}
	}

	private void StartWinchSound()
	{
		if (!winchAudioSource.isPlaying)
		{
			winchAudioSource.Play();
			winchAudioSource.pitch = 1f;
			winchAudioSource.volume = 1f;
			winchAudioSource.PlayOneShot(winchStartSound);
		}
	}

	private void StopWinchSound()
	{
		if (winchAudioSource.isPlaying)
		{
			winchAudioSource.Stop();
			winchAudioSource.pitch = 1f;
			winchAudioSource.volume = 1f;
			winchAudioSource.PlayOneShot(winchStopSound);
		}
	}

	private void RetractingState()
	{
		lineLength -= reelingSpeed * Time.deltaTime;
		winchAudioSource.pitch = 1f - 0.1f * lineLength / lineMaxLength;
		if (lineLength <= 0f)
		{
			aircraft.SetSlingLoadAttachment(null, DeployState.Retracted);
			lineLength = 0f;
			deployState = DeployState.Retracted;
			lineRenderer.enabled = false;
			StopWinchSound();
		}
	}

	private void DeployedState()
	{
		if (lineLength < lineMaxLength)
		{
			lineLength += reelingSpeed * Time.deltaTime;
			winchAudioSource.pitch = 0.95f + 0.1f * lineLength / lineMaxLength;
			if (lineLength >= lineMaxLength)
			{
				lineLength = lineMaxLength;
				StopWinchSound();
			}
		}
		if (!aircraft.LocalSim || aircraft.weaponManager.GetTargetList().Count == 0)
		{
			return;
		}
		Unit unit = aircraft.weaponManager.GetTargetList()[0];
		if (unit != null && unit.definition.CanSlingLoad && FastMath.InRange(body.velocity, unit.rb.velocity, 8f) && !unit.IsSlung())
		{
			hookTargetPos = unit.transform.position + 0.5f * unit.definition.height * unit.transform.up;
			if (FastMath.Distance(hook.position, hookTargetPos) + FastMath.Distance(winch.position, hookTargetPos) < lineLength)
			{
				aircraft.SetSlingLoadAttachment(unit, DeployState.Connected);
			}
		}
	}

	private void ConnectedState()
	{
		Vector3 to = winch.position - hook.position;
		float magnitude = to.magnitude;
		Vector3 vector = Vector3.zero;
		if (lineLength < lineMaxLength)
		{
			if (lineLength < magnitude)
			{
				float num = Mathf.Clamp01(magnitude - lineLength) * Mathf.Min(suspendedUnit.rb.mass, 20000f);
				vector = to.normalized * num;
			}
			winchAudioSource.pitch = Mathf.Min(reelingSpeed, magnitude - lineLength) / reelingSpeed;
			lineLength += reelingSpeed * Time.deltaTime;
		}
		if (lineLength > lineMaxLength)
		{
			lineLength = lineMaxLength;
			StopWinchSound();
		}
		if (!aircraft.LocalSim)
		{
			return;
		}
		loadForce = joint.currentForce.magnitude;
		if (attachedUnit.disabled || suspendedUnit.disabled || Physics.Linecast(winch.position, Vector3.Lerp(winch.position, hook.position, 0.8f), out var _, ~(int)PhysicsLayers.ExclusionZonesMask) || Vector3.Angle(attachedUnit.transform.up, to) > breakAngle || loadForce > breakForce)
		{
			BreakRope();
			return;
		}
		if (loadInWater)
		{
			Vector3 force = Vector3.up * suspendedUnit.rb.mass * 25f * Mathf.Clamp01(Datum.LocalSeaY - suspendedUnit.transform.position.y);
			Vector3 torque = Vector3.Cross(suspendedUnit.transform.up, Vector3.up) * suspendedUnit.rb.mass * 5f;
			suspendedUnit.rb.AddForce(force);
			suspendedUnit.rb.AddTorque(torque, ForceMode.Force);
		}
		suspendedUnit.rb.AddForceAtPosition(vector, hook.position);
		body.AddForceAtPosition(-vector, winch.position);
	}

	private void RescuePilotState()
	{
		Vector3 to = winch.position - hook.position;
		float magnitude = to.magnitude;
		if (magnitude < 1f && aircraft.IsServer && suspendedUnit != null)
		{
			lineLength = 1f;
			PilotDismounted rescuedPilot = suspendedUnit as PilotDismounted;
			RescuePilot(rescuedPilot);
		}
		else
		{
			if (!aircraft.LocalSim)
			{
				return;
			}
			if (suspendedUnit == null)
			{
				aircraft.SetSlingLoadAttachment(null, DeployState.Retracting);
				return;
			}
			loadForce = joint.currentForce.magnitude;
			if (lineLength > 4f)
			{
				bool flag = attachedUnit.disabled || suspendedUnit.disabled || Vector3.Angle(attachedUnit.transform.up, to) > breakAngle || loadForce > breakForce;
				if (Physics.Linecast(Vector3.Lerp(winch.position, hook.position, 0.1f), Vector3.Lerp(winch.position, hook.position, 0.9f), out var hitInfo, ~(int)PhysicsLayers.ExclusionZonesMask) && (!hitInfo.collider.gameObject.TryGetComponent<IDamageable>(out var component) || (component.GetUnit() != aircraft && component.GetUnit() != suspendedUnit)))
				{
					flag = true;
				}
				if (flag)
				{
					BreakRope();
					return;
				}
			}
			float num = 20f;
			Vector3 vector = new Vector3(to.normalized.x, 0f, to.normalized.z);
			Vector3 vector2 = Vector3.zero;
			if (suspendedUnit.rb.velocity.y > attachedUnit.rb.velocity.y + 3f)
			{
				num /= 2f;
			}
			Vector3 vector3 = num * suspendedUnit.rb.mass * to.normalized;
			if (GetRopeAngle() > 5f)
			{
				Vector3 vector4 = new Vector3(attachedUnit.rb.velocity.x - suspendedUnit.rb.velocity.x, 0f, attachedUnit.rb.velocity.z - suspendedUnit.rb.velocity.z);
				vector2 = 10f * suspendedUnit.rb.mass * vector + 0.1f * suspendedUnit.rb.mass * vector4 / Time.fixedDeltaTime;
			}
			Vector3 torque = suspendedUnit.rb.mass * 4f * Vector3.Cross(suspendedUnit.transform.up, Vector3.up);
			if (loadInWater)
			{
				Vector3 force = suspendedUnit.rb.mass * 25f * Mathf.Clamp01(Datum.LocalSeaY - suspendedUnit.transform.position.y) * Vector3.up;
				suspendedUnit.rb.AddForce(force);
			}
			vector3 += vector2;
			suspendedUnit.rb.AddForceAtPosition(vector3, hook.position);
			body.AddForceAtPosition(-vector3, winch.position);
			suspendedUnit.rb.AddTorque(torque, ForceMode.Force);
			lineLength = magnitude;
			winchAudioSource.pitch = 0.6f - lineLength / lineMaxLength * 0.1f;
		}
	}

	public void FixedUpdate()
	{
		switch (deployState)
		{
		case DeployState.Retracting:
			RetractingState();
			break;
		case DeployState.Deployed:
			DeployedState();
			break;
		case DeployState.Connected:
			ConnectedState();
			break;
		case DeployState.RescuePilot:
			RescuePilotState();
			break;
		}
		if (suspendedUnit != null && aircraft.LocalSim && !aircraft.IsServer && Time.timeSinceLevelLoad - lastTransformSent > 0.1f)
		{
			lastTransformSent = Time.timeSinceLevelLoad;
			aircraft.CmdSendSlungTransform(aircraft.transform.InverseTransformPoint(suspendedUnit.transform.position), suspendedUnit.transform.rotation);
		}
	}

	private void Update()
	{
		UpdateRope();
		if (suspendedUnit == null)
		{
			return;
		}
		if (!loadInWater)
		{
			if (suspendedUnit.transform.position.y < Datum.LocalSeaY)
			{
				LoadEnterWater();
			}
		}
		else if (suspendedUnit.transform.position.y > Datum.LocalSeaY)
		{
			LoadExitWater();
		}
	}

	private void LoadEnterWater()
	{
		loadInWater = true;
		suspendedUnit.rb.drag = 0.1f;
		suspendedUnit.rb.angularDrag = 0.2f;
	}

	private void LoadExitWater()
	{
		loadInWater = false;
		suspendedUnit.rb.drag = 0.02f;
		suspendedUnit.rb.angularDrag = 0.1f;
	}

	private void BreakRope()
	{
		hookAudioSource.PlayOneShot(slingSnapSound);
		if (GameManager.IsLocalAircraft(aircraft))
		{
			SceneSingleton<AircraftActionsReport>.i.ReportText("Load Lost - Retracting", 4f);
		}
		aircraft.SetSlingLoadAttachment(suspendedUnit, DeployState.Retracting);
	}

	private void RescuePilot(PilotDismounted rescuedPilot)
	{
		if (aircraft.IsServer)
		{
			rescuedPilot.Capture(attachedUnit);
		}
		if (GameManager.IsLocalAircraft(aircraft))
		{
			SceneSingleton<AircraftActionsReport>.i.ReportText("Pilot rescued", 4f);
		}
	}

	private void CreateJoint(Rigidbody target, Vector3 attachPoint)
	{
		joint = body.gameObject.AddComponent<ConfigurableJoint>();
		joint.autoConfigureConnectedAnchor = false;
		joint.anchor = winch.localPosition;
		joint.connectedAnchor = attachPoint;
		joint.connectedBody = target;
		SoftJointLimit linearLimit = default(SoftJointLimit);
		SoftJointLimitSpring linearLimitSpring = default(SoftJointLimitSpring);
		linearLimit.limit = lineMaxLength;
		linearLimitSpring.spring = suspendedUnit.rb.mass * 100f;
		linearLimitSpring.damper = 1000f;
		joint.linearLimit = linearLimit;
		joint.linearLimitSpring = linearLimitSpring;
		joint.xMotion = ConfigurableJointMotion.Limited;
		joint.yMotion = ConfigurableJointMotion.Limited;
		joint.zMotion = ConfigurableJointMotion.Limited;
		joint.angularXMotion = ConfigurableJointMotion.Free;
		joint.angularYMotion = ConfigurableJointMotion.Free;
		joint.angularZMotion = ConfigurableJointMotion.Free;
	}

	private void UpdateRope()
	{
		if (deployState == DeployState.Connected || deployState == DeployState.RescuePilot)
		{
			if (suspendedUnit != null)
			{
				hookTargetPos = suspendedUnit.transform.position + 0.5f * suspendedUnit.definition.height * suspendedUnit.transform.up;
				hook.position = hookTargetPos;
			}
		}
		else
		{
			float radarAlt = lineLength;
			if (radarAlt > attachedUnit.radarAlt)
			{
				radarAlt = attachedUnit.radarAlt;
			}
			hook.position = winch.position + Vector3.down * radarAlt;
		}
		for (int i = 0; i < 10; i++)
		{
			lineRenderer.SetPosition(i, Vector3.Lerp(winch.position, hook.position, (float)i * 0.111f));
		}
	}

	public float GetLineLength()
	{
		return lineLength;
	}

	public float GetLineMaxLength()
	{
		return lineMaxLength;
	}

	public Unit GetSuspendedUnit()
	{
		return suspendedUnit;
	}

	public float GetRopeFactor()
	{
		return Mathf.Clamp01(joint.currentForce.magnitude / breakForce);
	}

	public float GetRopeAngle()
	{
		Vector3 to = suspendedUnit.transform.position - attachedUnit.transform.position;
		return Vector3.Angle(-attachedUnit.transform.up, to);
	}
}
