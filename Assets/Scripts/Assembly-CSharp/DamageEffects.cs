using NuclearOption.Networking;
using UnityEngine;

public static class DamageEffects
{
	public static Collider[] hitColliders = new Collider[4096];

	public static void ArmorPenetrate(Vector3 position, Vector3 velocity, float muzzleVelocity, float pierceDamage, float blastDamage, PersistentID dealerID)
	{
		int num = 0;
		float num2 = 0f;
		Vector3 vector = position;
		Vector3 vector2 = velocity;
		if (blastDamage > 0f)
		{
			BlastFrag(blastDamage, position, dealerID, PersistentID.None);
		}
		RaycastHit hitInfo;
		while (num < 10 && Physics.Linecast(vector, vector + vector2 * 0.1f, out hitInfo, ~(int)PhysicsLayers.ExclusionZonesMask) && vector2.sqrMagnitude > muzzleVelocity * muzzleVelocity * 0.1f)
		{
			num++;
			IDamageable component = hitInfo.collider.gameObject.GetComponent<IDamageable>();
			if (component != null)
			{
				num2 = Mathf.Max(Vector3.Dot(vector2.normalized, -hitInfo.normal), 0.5f) * pierceDamage * (vector2.magnitude / muzzleVelocity);
				component.TakeDamage(num2, 1f, 0f, 0f, 0f, dealerID);
				ArmorProperties armorProperties = component.GetArmorProperties();
				if (num2 > armorProperties.pierceArmor)
				{
					vector2 *= (num2 - armorProperties.pierceArmor * 2f) / num2;
					vector = hitInfo.point + 0.1f * vector2.normalized;
					continue;
				}
				break;
			}
			break;
		}
	}

	public static void BlastFrag(float blastYield, Vector3 blastPosition, PersistentID dealerID, PersistentID missileID)
	{
		float num = Mathf.Pow(blastYield, 0.3333f);
		float num2 = num * 20f;
		Transform transform = Datum.origin;
		if (PlayerSettings.debugVis)
		{
			transform = (UnitRegistry.TryGetNearestUnit(blastPosition.ToGlobalPosition(), out var nearestUnit, 100f) ? nearestUnit.transform : Datum.origin);
			GameObject gameObject = NetworkSceneSingleton<Spawner>.i.SpawnLocal(GameAssets.i.blastRadiusDebug, transform);
			gameObject.GetComponent<MeshRenderer>().material.SetColor("_EmissionColor", new Color(1f, 0.5f, 0f, 1f));
			gameObject.transform.position = blastPosition;
			gameObject.transform.localScale = Vector3.one * num;
			NetworkSceneSingleton<Spawner>.i.DestroyLocal(gameObject, 10f);
		}
		int num3 = Physics.OverlapSphereNonAlloc(blastPosition, num2, hitColliders);
		for (int i = 0; i < num3; i++)
		{
			Collider collider = hitColliders[i];
			if (collider.gameObject.TryGetComponent<IDamageable>(out var _))
			{
				FragTrace(blastPosition, (collider.transform.position - blastPosition).normalized * num2, blastYield, num, collider, dealerID, transform);
			}
		}
	}

	public static void FragTrace(Vector3 origin, Vector3 fragVector, float blastYield, float blastPower, Collider fragTarget, PersistentID dealerID, Transform debugTransform)
	{
		float num = 0f;
		Vector3 vector = origin;
		float num2 = 0f;
		int num3 = 0;
		RaycastHit hitInfo;
		while (num3 < 10 && Physics.Linecast(vector, vector + fragVector, out hitInfo, ~((int)PhysicsLayers.ExclusionZonesMask | (int)PhysicsLayers.IgnoreCollisionsMask)))
		{
			num3++;
			num += hitInfo.distance;
			if (!hitInfo.collider.gameObject.TryGetComponent<IDamageable>(out var component))
			{
				if (hitInfo.collider.sharedMaterial != GameAssets.i.terrainMaterial && hitInfo.collider.sharedMaterial != null)
				{
					vector = hitInfo.point + fragVector.normalized * 0.1f;
					continue;
				}
				break;
			}
			ArmorProperties armorProperties = component.GetArmorProperties();
			if (armorProperties == null)
			{
				break;
			}
			float num4 = Mathf.Max((num + num2) / blastPower, 1f);
			float num5 = 25000f / (num4 * num4 * num4);
			if (num5 <= 0f)
			{
				break;
			}
			if (hitInfo.collider == fragTarget)
			{
				float num6 = (hitInfo.collider.bounds.extents.x + hitInfo.collider.bounds.extents.y + hitInfo.collider.bounds.extents.z) * 0.3333f;
				float num7 = num6 * num6;
				float num8 = Mathf.Clamp(Mathf.Max(num * num, blastPower * blastPower) / num7, 0f, 10f);
				num8 /= 1f + armorProperties.blastArmor * 0.05f;
				if (blastPower >= 0.5f)
				{
					component.TakeShockwave(origin, num5, blastPower);
				}
				float num9 = Mathf.Clamp01((blastPower * 500f - armorProperties.blastArmor) / (armorProperties.blastArmor * 2f));
				num5 *= num9;
				num5 -= armorProperties.blastArmor;
				if (num5 > 0f)
				{
					if (NetworkManagerNuclearOption.i.Server.Active)
					{
						component.TakeDamage(0f, num5, Mathf.Clamp01(num8), 0f, 0f, dealerID);
					}
					if (PlayerSettings.debugVis)
					{
						GameObject gameObject = NetworkSceneSingleton<Spawner>.i.SpawnLocal(GameAssets.i.debugArrow, debugTransform);
						gameObject.GetComponent<MeshRenderer>().material.SetColor("_EmissionColor", new Color(1f, 0.5f, 0f, 1f));
						gameObject.transform.position = origin;
						gameObject.transform.rotation = Quaternion.LookRotation(hitInfo.point - origin);
						gameObject.transform.localScale = new Vector3(0.5f, 0.5f, num);
						NetworkSceneSingleton<Spawner>.i.DestroyLocal(gameObject, 10f);
					}
				}
				break;
			}
			num2 += armorProperties.blastArmor * 0.1f;
			vector = hitInfo.point + fragVector.normalized * 0.1f;
		}
	}
}
