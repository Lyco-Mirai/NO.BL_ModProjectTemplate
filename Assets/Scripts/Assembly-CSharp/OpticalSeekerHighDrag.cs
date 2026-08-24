using System.Collections.Generic;
using UnityEngine;

public class OpticalSeekerHighDrag : MissileSeeker
{
	private GlobalPosition knownPos;

	private Vector3 knownVel;

	private Transform targetTransform;

	private bool terminalMode;

	[SerializeField]
	private List<Transform> listPetals = new List<Transform>();

	[SerializeField]
	private float openDelay;

	[SerializeField]
	private float openAltitude;

	[SerializeField]
	private float openAngle = -60f;

	[SerializeField]
	private float openSpeed = 1f;

	[SerializeField]
	private bool deployed;

	private float timeSinceSpawn;

	private float currentAngle;

	private float openRatio;

	public override void Initialize(Unit target, GlobalPosition aimpoint)
	{
		knownPos = aimpoint;
		if (UnitRegistry.TryGetUnit(missile.targetID, out target))
		{
			targetUnit = target;
			targetTransform = target.GetRandomPart();
			knownPos = missile.NetworkHQ.GetKnownPosition(target) ?? (missile.GlobalPosition() + missile.transform.forward * 10000f);
			knownVel = ((target.rb != null) ? target.rb.velocity : Vector3.zero);
			missile.NetworkseekerMode = Missile.SeekerMode.passive;
		}
		else
		{
			knownPos = missile.GlobalPosition() + missile.transform.forward * 10000f;
		}
		missile.SetAimpoint(knownPos, knownVel);
		if (openAngle < 0f)
		{
			openAngle += 360f;
		}
	}

	public override string GetSeekerType()
	{
		return "Optical";
	}

	public override void Seek()
	{
		timeSinceSpawn += Time.fixedDeltaTime;
		if (targetUnit != null && !targetUnit.disabled)
		{
			GlobalPosition? knownPosition = missile.NetworkHQ.GetKnownPosition(targetUnit);
			if (knownPosition.HasValue)
			{
				knownPos = knownPosition.Value;
				knownVel = ((targetUnit.rb != null) ? targetUnit.rb.velocity : Vector3.zero);
			}
		}
		if (!deployed)
		{
			if (missile.radarAlt < openAltitude && timeSinceSpawn > openDelay)
			{
				deployed = true;
			}
			return;
		}
		foreach (Transform listPetal in listPetals)
		{
			currentAngle = Mathf.LerpAngle(listPetal.localEulerAngles.x, openAngle, openSpeed * Time.fixedDeltaTime);
			openRatio = Mathf.Lerp(openRatio, 1f, openSpeed * Time.fixedDeltaTime);
			listPetal.localEulerAngles = new Vector3(currentAngle, 0f, 0f);
		}
	}

	private void OnDestroy()
	{
		foreach (Transform listPetal in listPetals)
		{
			if (listPetal != null)
			{
				Object.Destroy(listPetal.gameObject);
			}
		}
	}
}
