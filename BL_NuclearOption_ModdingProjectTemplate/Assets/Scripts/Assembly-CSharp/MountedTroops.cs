using UnityEngine;

public class MountedTroops : Weapon
{
	private float mass;

	[SerializeField]
	private float captureStrength;

	private Aircraft attachedAircraft;

	private UnitPart attachedPart;

	private bool disabled;

	private bool captureActive;

	private void Awake()
	{
		ammo = (int)captureStrength;
	}

	public override void AttachToHardpoint(Aircraft aircraft, Hardpoint hardpoint, WeaponMount mount)
	{
		base.AttachToHardpoint(aircraft, hardpoint, mount);
		attachedPart = hardpoint.part;
		attachedPart.onPartDetached += MountedTroops_OnPartDetached;
		attachedAircraft = aircraft;
		mass = (float)ammo * info.massPerRound + mount.emptyMass;
		base.hardpoint = hardpoint;
		hardpoint.ModifyMass(mass);
		this.StartSlowUpdateDelayed(2f, CheckCaptureConditions);
	}

	public override int GetAmmoLoaded()
	{
		return (int)captureStrength;
	}

	public override int GetAmmoTotal()
	{
		return (int)captureStrength;
	}

	public override void Fire(Unit owner, Unit target, Vector3 inheritedVelocity, WeaponStation weaponStation, GlobalPosition aimpoint)
	{
		if (hardpoint != null)
		{
			hardpoint.SpringOpenBayDoors();
		}
		if (attachedAircraft.IsServer)
		{
			attachedAircraft.RpcLaunchMissile(weaponStation.Number, target, aimpoint);
		}
		else if (attachedAircraft.HasAuthority)
		{
			attachedAircraft.CmdLaunchMissile(weaponStation.Number, target, aimpoint);
		}
	}

	private void MountedTroops_OnPartDetached(UnitPart part)
	{
		disabled = true;
		Object.Destroy(this);
	}

	private void OnDestroy()
	{
		if (attachedPart != null)
		{
			attachedPart.onPartDetached -= MountedTroops_OnPartDetached;
			hardpoint.ModifyMass(0f - mass);
		}
		if (attachedAircraft != null && captureActive)
		{
			attachedAircraft.ModifyCaptureStrength(0f - captureStrength);
		}
	}

	private void CheckCaptureConditions()
	{
		bool flag = attachedAircraft.speed < 2f && attachedAircraft.radarAlt < 1f;
		if (flag != captureActive)
		{
			captureActive = flag;
			attachedAircraft.ModifyCaptureStrength(captureActive ? captureStrength : (0f - captureStrength));
		}
	}
}
