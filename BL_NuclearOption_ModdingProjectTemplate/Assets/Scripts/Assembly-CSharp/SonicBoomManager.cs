using System.Collections.Generic;
using UnityEngine;

public static class SonicBoomManager
{
	private class ManagedSonicBoom
	{
		public readonly Unit supersonicUnit;

		private AudioSource source;

		private GameObject sourceObject;

		private GlobalPosition nearestSoundFront;

		private bool supersonic;

		private bool inMachCone;

		private float lastSupersonic;

		public ManagedSonicBoom(Unit supersonicUnit)
		{
			this.supersonicUnit = supersonicUnit;
			lastSupersonic = 10000000f;
			sourceObject = new GameObject("Sonic Boom");
			sourceObject.transform.SetParent(Datum.origin);
			sourceObject.transform.position = supersonicUnit.transform.position;
			nearestSoundFront = supersonicUnit.GlobalPosition();
			source = sourceObject.AddComponent<AudioSource>();
			source.outputAudioMixerGroup = SoundManager.i.HeavyEffectsMixer;
			source.spatialBlend = 1f;
			source.minDistance = 1000f;
			source.dopplerLevel = 0f;
			source.spread = 20f;
			source.maxDistance = 5000f;
		}

		public bool Manage(GlobalPosition cameraPos)
		{
			if (supersonicUnit == null)
			{
				Object.Destroy(sourceObject);
				return false;
			}
			GlobalPosition globalPosition = supersonicUnit.GlobalPosition();
			float speedOfSound = LevelInfo.GetSpeedOfSound(supersonicUnit.GlobalPosition().y);
			if (supersonicUnit.speed > speedOfSound)
			{
				float num = supersonicUnit.speed / speedOfSound;
				float num2 = Mathf.Asin(1f / num) * 57.29578f;
				float num3 = Vector3.Angle(globalPosition - cameraPos, supersonicUnit.rb.velocity);
				if (FastMath.InRange(globalPosition, cameraPos, supersonicUnit.maxRadius))
				{
					num2 = 180f;
				}
				if (num3 < num2)
				{
					if (!inMachCone)
					{
						GlobalPosition globalPosition2 = globalPosition + Vector3.Project(globalPosition - cameraPos, -supersonicUnit.rb.velocity);
						inMachCone = true;
						supersonicUnit.SetSoundsMuted(muted: false);
						sourceObject.transform.position = globalPosition2.ToLocalPosition();
						if (num2 < 180f && FastMath.OutOfRange(SceneSingleton<CameraStateManager>.i.cameraVelocity, supersonicUnit.rb.velocity, 100f))
						{
							float value = 1000f / FastMath.Distance(globalPosition2, cameraPos);
							source.PlayOneShot(GameAssets.i.sonicBoom, 1f);
							SceneSingleton<CameraStateManager>.i.ShakeCamera(Mathf.Clamp01(value), 0f);
						}
					}
				}
				else if (inMachCone)
				{
					inMachCone = false;
					supersonicUnit.SetSoundsMuted(muted: true);
				}
			}
			else if (!inMachCone)
			{
				inMachCone = true;
				supersonicUnit.SetSoundsMuted(muted: false);
			}
			return true;
		}
	}

	private static List<ManagedSonicBoom> managedBooms;

	private static GlobalPosition cameraPosPrev;

	public static void RegisterUnit(Unit supersonicUnit)
	{
		if (managedBooms == null)
		{
			managedBooms = new List<ManagedSonicBoom>();
		}
		foreach (ManagedSonicBoom managedBoom in managedBooms)
		{
			if (supersonicUnit == managedBoom.supersonicUnit)
			{
				return;
			}
		}
		managedBooms.Add(new ManagedSonicBoom(supersonicUnit));
	}

	public static void ManageSonicBooms(GlobalPosition cameraPos)
	{
		if (managedBooms == null)
		{
			return;
		}
		for (int num = managedBooms.Count - 1; num >= 0; num--)
		{
			if (!managedBooms[num].Manage(cameraPos))
			{
				managedBooms.RemoveAt(num);
			}
		}
	}
}
