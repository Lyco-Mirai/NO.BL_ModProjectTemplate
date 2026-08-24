using System;
using System.Collections.Generic;
using NuclearOption.Effects;
using NuclearOption.Networking;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Shockwave : MonoBehaviour
{
	private struct InfluencedObject
	{
		private Collider collider;

		private float averageRadius;

		private Rigidbody rb;

		private IDamageable damageable;

		public InfluencedObject(Collider collider)
		{
			this.collider = collider;
			rb = collider.attachedRigidbody;
			damageable = collider.gameObject.GetComponent<IDamageable>();
			averageRadius = (collider.bounds.extents.x + collider.bounds.extents.y + collider.bounds.extents.z) * 0.333f;
		}

		public bool IsInteractable()
		{
			if (!(rb != null))
			{
				return damageable != null;
			}
			return true;
		}

		public bool HasShockwaveReached(Vector3 blastOrigin, float blastPropagation, float overpressure, float blastYield, float blastPower, PersistentID ownerID)
		{
			if (collider == null)
			{
				return true;
			}
			blastPropagation += averageRadius;
			if (Vector3.SqrMagnitude(collider.bounds.center - blastOrigin) > blastPropagation * blastPropagation)
			{
				return false;
			}
			float num = MathF.PI * averageRadius * averageRadius;
			float num2 = ((rb != null) ? rb.mass : 0f);
			float num3 = Mathf.Clamp(Mathf.Max(blastPower * blastPower, blastPropagation * blastPropagation) / num, 0f, 10f);
			if (damageable != null)
			{
				ArmorProperties armorProperties = damageable.GetArmorProperties();
				num3 /= 1f + armorProperties.blastArmor * 0.02f;
				Unit unit = damageable.GetUnit();
				num2 = damageable.GetMass();
				if (unit is Building building)
				{
					building.RegisterRecentExplosion(blastOrigin.ToGlobalPosition(), blastYield);
				}
				if (NetworkManagerNuclearOption.i.Server.Active)
				{
					damageable.TakeDamage(0f, overpressure, Mathf.Clamp01(num3), 0f, 0f, ownerID);
				}
			}
			if (rb != null && num2 > 0f)
			{
				float num4 = num * Mathf.Clamp01(num3) * overpressure * 25f;
				num4 = Mathf.Min(num4, blastYield * 200f, 60f * num2);
				rb.AddForceAtPosition((collider.bounds.center - blastOrigin).normalized * num4, collider.bounds.center, ForceMode.Impulse);
			}
			return true;
		}
	}

	private static Collider[] colliderBuffer = new Collider[4096];

	[SerializeField]
	private float yieldKilotons;

	[SerializeField]
	private float maxOverpressure;

	private float blastPower;

	private float blastRadius;

	private float blastPropagation;

	[SerializeField]
	private GameObject groundDecal;

	[SerializeField]
	private GameObject waterDecal;

	private DecalProjector decalProjector;

	private float dustOpacity = 1f;

	private float cloudAlpha = 1f;

	[SerializeField]
	private GameObject vaporCloud;

	private Material vaporCloudMat;

	[SerializeField]
	private Light vaporCloudEmissiveLight;

	[SerializeField]
	private float vaporCloudEmissiveFactor;

	[SerializeField]
	private AnimationCurve vaporCloudAlpha;

	[SerializeField]
	private float vaporCloudDetailScale;

	private float blastTime;

	private float overpressure;

	private PersistentID ownerID;

	private List<InfluencedObject> influencedObjects = new List<InfluencedObject>();

	private static int id_decalSize = Shader.PropertyToID("_decalSize");

	private static int id_opacity = Shader.PropertyToID("_opacity");

	private static int id_shockwaveExpansion = Shader.PropertyToID("_shockwaveExpansion");

	private static int id_ShockwaveAlpha = Shader.PropertyToID("_ShockwaveAlpha");

	private static int id_Emission = Shader.PropertyToID("_Emission");

	private static int id_Size = Shader.PropertyToID("_Size");

	private static int id_ShockwaveSoftness = Shader.PropertyToID("_ShockwaveSoftness");

	public void SetOwner(PersistentID ownerID, float yield)
	{
		this.ownerID = ownerID;
		yieldKilotons = yield;
	}

	private void Start()
	{
		dustOpacity = 1f;
		blastPower = Mathf.Pow(yieldKilotons * 1000000f, 0.3333f);
		blastRadius = blastPower * 13f;
		blastPropagation = blastPower * 0.5f;
		if (vaporCloud != null)
		{
			vaporCloudMat = vaporCloud.GetComponent<Renderer>().material;
		}
		if (groundDecal != null)
		{
			if (Physics.Linecast(base.transform.position + blastRadius * 0.5f * Vector3.up, base.transform.position - blastRadius * 0.5f * Vector3.up, out var hitInfo, PhysicsLayers.StaticsMask))
			{
				groundDecal.transform.SetParent(Datum.origin);
				groundDecal.transform.rotation = Quaternion.LookRotation(Vector3.down);
				groundDecal.transform.position = hitInfo.point;
				decalProjector = groundDecal.GetComponent<DecalProjector>();
				decalProjector.size = new Vector3(blastRadius * 2f, blastRadius * 2f, blastRadius * 0.3f);
				decalProjector.material = new Material(decalProjector.material);
				decalProjector.material.SetFloat(id_decalSize, blastRadius * 2f);
				decalProjector.material.SetFloat(id_opacity, 1f);
			}
			else
			{
				UnityEngine.Object.Destroy(groundDecal);
			}
		}
		if (waterDecal != null && base.transform.position.y < Datum.LocalSeaY + blastRadius * 0.5f)
		{
			waterDecal.SetActive(value: true);
		}
		if (!(yieldKilotons >= 0.0002f))
		{
			return;
		}
		int num = Physics.OverlapSphereNonAlloc(base.transform.position, blastRadius * 2f, colliderBuffer);
		for (int i = 0; i < num; i++)
		{
			Collider collider = colliderBuffer[i];
			InfluencedObject item = new InfluencedObject(collider);
			if (item.IsInteractable())
			{
				influencedObjects.Add(item);
			}
		}
		if ((double)yieldKilotons > 0.01)
		{
			SceneSingleton<BlastManager>.i.AddBlast(base.transform.GlobalPosition(), blastRadius * 0.5f);
		}
	}

	private void Update()
	{
		blastPropagation += 340f * Time.deltaTime;
		blastTime += Time.deltaTime;
		if (groundDecal != null && decalProjector != null)
		{
			decalProjector.material.SetFloat(id_shockwaveExpansion, 1f * blastRadius / blastPropagation);
		}
		if (blastPropagation > blastRadius)
		{
			dustOpacity -= Time.deltaTime * 0.1f;
			if (decalProjector != null)
			{
				decalProjector.material.SetFloat(id_opacity, dustOpacity);
			}
			if (dustOpacity <= 0f)
			{
				UnityEngine.Object.Destroy(groundDecal);
				UnityEngine.Object.Destroy(this);
			}
			if (vaporCloud != null && cloudAlpha <= 0f)
			{
				UnityEngine.Object.Destroy(vaporCloud);
			}
		}
		float num = Mathf.Max(blastPropagation / blastPower, 1f);
		float num2 = 25000f / (num * num * num);
		if (num2 > 0.5f)
		{
			for (int num3 = influencedObjects.Count - 1; num3 >= 0; num3--)
			{
				if (influencedObjects[num3].HasShockwaveReached(base.transform.position, blastPropagation, num2, yieldKilotons * 1000000f, blastPower, ownerID))
				{
					influencedObjects.RemoveAt(num3);
				}
			}
		}
		else
		{
			influencedObjects.Clear();
		}
		if (vaporCloud != null)
		{
			vaporCloud.transform.LookAt(SceneSingleton<CameraStateManager>.i.transform.position);
			vaporCloud.transform.localScale = Vector3.one * blastPropagation;
			cloudAlpha = vaporCloudAlpha.Evaluate(blastTime);
			float num4 = ((vaporCloudEmissiveLight != null && vaporCloudEmissiveLight.isActiveAndEnabled) ? (vaporCloudEmissiveLight.intensity * vaporCloudEmissiveFactor) : 0f);
			vaporCloudMat.SetFloat(id_ShockwaveAlpha, cloudAlpha);
			if (num4 > 0f)
			{
				vaporCloudMat.SetFloat(id_Emission, num4);
			}
			vaporCloudMat.SetFloat(id_Size, blastPropagation / vaporCloudDetailScale);
			vaporCloudMat.SetFloat(id_ShockwaveSoftness, 4f / vaporCloud.transform.localScale.x);
			if (cloudAlpha <= 0f)
			{
				UnityEngine.Object.Destroy(vaporCloud);
			}
		}
	}
}
