using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class MountedMissile : Weapon
{
	public enum RailDirection
	{
		Forward = 0,
		Down = 1,
		Right = 2,
		Left = 3,
		Backward = 4,
		Up = 5
	}

	[SerializeField]
	private RailDirection railDirection;

	[SerializeField]
	private float railLength;

	[SerializeField]
	private float railSpeed;

	[SerializeField]
	private float railDelay;

	[SerializeField]
	private AudioClip deploySound;

	[SerializeField]
	private ParticleSystem deployParticles;

	[SerializeField]
	private float deployVolume;

	[SerializeField]
	private float doorOpenDuration = 0.5f;

	[SerializeField]
	private BayDoor[] bayDoors;

	[SerializeField]
	private Transform rotaryTransform;

	[SerializeField]
	private float rotaryDegrees;

	[SerializeField]
	private float rotarySpeed;

	[SerializeField]
	private float rotaryDelay;

	private float railPosition;

	private Vector3 mountedPosition;

	private bool fired;

	private Vector3 railVector;

	public override void AttachToHardpoint(Aircraft aircraft, Hardpoint hardpoint, WeaponMount weaponMount)
	{
		base.AttachToHardpoint(aircraft, hardpoint, weaponMount);
		hardpoint.ModifyMass(info.massPerRound);
		hardpoint.ModifyDrag(mount.GetDragPerRound());
		hardpoint.ModifyRCS(mount.GetRCSPerRound());
		mountedPosition = base.transform.localPosition;
		ammo = 1;
	}

	public override void Fire(Unit owner, Unit target, Vector3 inheritedVelocity, WeaponStation weaponStation, GlobalPosition aimpoint)
	{
		if (fired)
		{
			return;
		}
		Aircraft aircraft = owner as Aircraft;
		RemoveFromHardpoint();
		fired = true;
		ammo = 0;
		switch (railDirection)
		{
		case RailDirection.Forward:
			railVector = new Vector3(0f, 0f, 1f);
			break;
		case RailDirection.Backward:
			railVector = new Vector3(0f, 0f, -1f);
			break;
		case RailDirection.Up:
			railVector = new Vector3(0f, 1f, 0f);
			break;
		case RailDirection.Down:
			railVector = new Vector3(0f, -1f, 0f);
			break;
		case RailDirection.Left:
			railVector = new Vector3(-1f, 0f, 0f);
			break;
		case RailDirection.Right:
			railVector = new Vector3(1f, 0f, 0f);
			break;
		}
		if (aircraft.IsServer)
		{
			aircraft.RpcLaunchMissile(weaponStation.Number, target, aimpoint);
		}
		else if (aircraft.HasAuthority)
		{
			aircraft.CmdLaunchMissile(weaponStation.Number, target, aimpoint);
		}
		if (bayDoors.Length != 0)
		{
			BayDoor[] array = bayDoors;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].OpenDoor(doorOpenDuration);
			}
		}
		else if (hardpoint != null)
		{
			hardpoint.SpringOpenBayDoors();
		}
		if (!hardpoint.part.IsDetached())
		{
			RailLaunch(owner, target).Forget();
			if (rotaryTransform != null)
			{
				RotaryIncrement().Forget();
			}
			TrackFiringVisibility().Forget();
		}
	}

	private async UniTask RailLaunch(Unit owner, Unit target)
	{
		CancellationToken cancel = base.destroyCancellationToken;
		await UniTask.Delay((int)(railDelay * 1000f));
		if (deploySound != null)
		{
			PlayLaunchSound();
		}
		if (deployParticles != null)
		{
			deployParticles.transform.position = base.transform.position;
			deployParticles.Play();
		}
		Vector3 vector = default(Vector3);
		while (railPosition < railLength)
		{
			await UniTask.Yield();
			if (cancel.IsCancellationRequested || owner == null)
			{
				return;
			}
			vector = (railVector.z * base.transform.forward + railVector.y * base.transform.up + railVector.x * base.transform.right) * railSpeed;
			base.transform.position += vector * Time.deltaTime;
			railPosition += railSpeed * Time.deltaTime;
		}
		if (owner.IsServer)
		{
			NetworkSceneSingleton<Spawner>.i.SpawnMissile(info.weaponPrefab, base.transform.position, base.transform.rotation, owner.rb.GetPointVelocity(base.transform.position) + vector, target, owner);
		}
		base.gameObject.SetActive(value: false);
		if (owner != null && owner.NetworkHQ != null && owner.IsServer)
		{
			owner.NetworkHQ.missionStatsTracker.MunitionCost(owner, info.costPerRound);
		}
	}

	private async UniTask RotaryIncrement()
	{
		CancellationToken cancel = base.destroyCancellationToken;
		_ = rotaryDegrees / rotarySpeed;
		float degreesRotated = 0f;
		await UniTask.WaitForSeconds(rotaryDelay);
		if (cancel.IsCancellationRequested)
		{
			return;
		}
		while (degreesRotated < Mathf.Abs(rotaryDegrees))
		{
			await UniTask.Yield();
			if (cancel.IsCancellationRequested)
			{
				break;
			}
			float num = Mathf.Sign(rotaryDegrees) * rotarySpeed * Time.deltaTime;
			degreesRotated += Mathf.Abs(num);
			if (degreesRotated > Mathf.Abs(rotaryDegrees))
			{
				num -= Mathf.Sign(rotaryDegrees) * (degreesRotated - Mathf.Abs(rotaryDegrees));
			}
			rotaryTransform.Rotate(new Vector3(0f, 0f, num), Space.Self);
		}
	}

	public override int GetAmmoLoaded()
	{
		if (!fired)
		{
			return 1;
		}
		return 0;
	}

	public override int GetAmmoTotal()
	{
		if (!fired)
		{
			return 1;
		}
		return 0;
	}

	private void OnDestroy()
	{
		if (!fired && hardpoint != null)
		{
			RemoveFromHardpoint();
		}
	}

	private void RemoveFromHardpoint()
	{
		if (hardpoint != null)
		{
			hardpoint.ModifyMass(0f - info.massPerRound);
			hardpoint.ModifyDrag(0f - mount.GetDragPerRound());
			hardpoint.ModifyRCS(0f - mount.GetRCSPerRound());
		}
	}

	public override void Rearm(int ammoToRearm, WeaponStation weaponStation)
	{
		if (fired)
		{
			base.weaponStation = weaponStation;
			base.transform.localPosition = mountedPosition;
			base.gameObject.SetActive(value: true);
			railPosition = 0f;
			fired = false;
			ammo = 1;
			if (hardpoint != null)
			{
				hardpoint.ModifyMass(info.massPerRound);
				hardpoint.ModifyDrag(mount.GetDragPerRound());
				hardpoint.ModifyRCS(mount.GetRCSPerRound());
			}
			if (rotaryTransform != null)
			{
				rotaryTransform.localRotation = Quaternion.identity;
			}
			ReportReloading(reloading: false);
		}
	}

	private void PlayLaunchSound()
	{
		AudioSource audioSource = base.transform.parent.gameObject.AddComponent<AudioSource>();
		audioSource.outputAudioMixerGroup = SoundManager.i.EffectsMixer;
		audioSource.bypassListenerEffects = true;
		audioSource.clip = deploySound;
		audioSource.volume = deployVolume;
		audioSource.pitch = Random.Range(0.8f, 1.2f);
		audioSource.spatialBlend = 1f;
		audioSource.dopplerLevel = 0f;
		audioSource.spread = 5f;
		audioSource.maxDistance = 40f;
		audioSource.minDistance = 5f;
		audioSource.Play();
		Object.Destroy(audioSource, 5f);
	}
}
