using System;
using UnityEngine;

public class Parachute : MonoBehaviour
{
	[SerializeField]
	private Unit attachedUnit;

	[SerializeField]
	private UnitPart attachedUnitPart;

	[SerializeField]
	private GameObject canopy;

	[SerializeField]
	private GameObject lines;

	[SerializeField]
	private float canopyMass;

	[SerializeField]
	private float lineSpring;

	[SerializeField]
	private float lineSlackLength;

	[SerializeField]
	private float maxRadius;

	[SerializeField]
	private float maxDrag;

	[SerializeField]
	private float damping = 1f;

	[SerializeField]
	private float openDelayMin;

	[SerializeField]
	private float openDelayMax;

	[SerializeField]
	private float openAltitudeMin;

	[SerializeField]
	private float openAltitudeMax;

	[SerializeField]
	private float openSpeedMin;

	[SerializeField]
	private float openSpeedMax;

	[SerializeField]
	private Renderer parachuteRenderer;

	private Material parachuteMaterial;

	private float radarAlt;

	private Vector3 lineVector;

	private Vector3 groundNormal = Vector3.up;

	private float lineLength;

	private float lineTension;

	private float openAmount;

	private float canopyDisplacement;

	private Vector3 canopyVel;

	private Vector3 canopyForce;

	private Vector3 repelForce;

	[SerializeField]
	private AnimationCurve chuteDrag;

	[SerializeField]
	private AnimationCurve chuteScaleVertical;

	[SerializeField]
	private AnimationCurve chuteScaleHorizontal;

	[SerializeField]
	private AnimationCurve wrinkleStrength;

	private float landedTime;

	private float timeSinceSpawn;

	private static int id_wrinkleDisplacement = Shader.PropertyToID("_wrinkleDisplacement");

	private static int id_wrinkleStrength = Shader.PropertyToID("_wrinkleStrength");

	private Rigidbody rb;

	public Action onUnitLanded;

	public void SetAttachedUnit(Unit unit)
	{
		attachedUnit = unit;
	}

	public void SetAttachedPart(UnitPart unitPart)
	{
		attachedUnit = null;
		attachedUnitPart = unitPart;
		Debug.Log("Setting parachute attached part to " + unitPart.gameObject.name);
		rb = unitPart.rb;
	}

	public void DeployChute()
	{
		radarAlt = 10000f;
		if (attachedUnit != null)
		{
			rb = attachedUnit.rb;
		}
		parachuteMaterial = parachuteRenderer.material;
		repelForce = Vector3.zero;
		groundNormal = Vector3.zero;
		canopy.SetActive(value: true);
		lines.SetActive(value: true);
		canopy.transform.SetParent(null);
		canopyVel = rb.velocity;
		canopy.transform.position = base.transform.position - rb.velocity.normalized * 2f;
	}

	private void CheckRadarAlt()
	{
		if (attachedUnitPart != null)
		{
			radarAlt = attachedUnitPart.xform.GlobalPosition().y;
			if (Physics.Raycast(attachedUnitPart.xform.position, -Vector3.up, out var hitInfo, 20000f, (int)PhysicsLayers.StaticsMask | (int)PhysicsLayers.ShipsMask))
			{
				radarAlt = Mathf.Min(hitInfo.distance, radarAlt);
			}
		}
		else if (attachedUnit != null)
		{
			attachedUnit.CheckRadarAlt();
			radarAlt = attachedUnit.radarAlt;
		}
		else
		{
			radarAlt = base.transform.GlobalPosition().y;
			if (Physics.Raycast(base.transform.position, -Vector3.up, out var hitInfo2, 20000f, (int)PhysicsLayers.StaticsMask | (int)PhysicsLayers.ShipsMask))
			{
				radarAlt = Mathf.Min(hitInfo2.distance, radarAlt);
			}
		}
	}

	public void FixedUpdate()
	{
		timeSinceSpawn += Time.fixedDeltaTime;
		CheckRadarAlt();
		if (!canopy.activeSelf)
		{
			if (rb == null && attachedUnit != null)
			{
				rb = attachedUnit.rb;
			}
			float num = ((rb != null) ? rb.velocity.magnitude : 0f);
			if (radarAlt > openAltitudeMin && radarAlt < openAltitudeMax && timeSinceSpawn > openDelayMin && timeSinceSpawn < openDelayMax && num > openSpeedMin && num < openSpeedMax)
			{
				DeployChute();
				if (attachedUnit is PilotDismounted pilotDismounted)
				{
					pilotDismounted.DeployChute();
				}
			}
			if (rb == null || rb.isKinematic)
			{
				UnityEngine.Object.Destroy(base.gameObject);
			}
			return;
		}
		float num2 = GameAssets.i.airDensityAltitude.Evaluate(base.transform.position.GlobalY() * 0.001f);
		lineVector = canopy.transform.position - base.transform.position;
		lineLength = lineVector.magnitude;
		lineTension = Mathf.Max(lineLength - lineSlackLength, 0f) * lineSpring;
		Vector3 vector = canopyVel - NetworkSceneSingleton<LevelInfo>.i.GetWind(base.transform.GlobalPosition());
		float magnitude = vector.magnitude;
		canopyDisplacement += magnitude * Time.deltaTime;
		float num3 = maxDrag * chuteDrag.Evaluate(openAmount) * num2;
		float value = (0f - wrinkleStrength.Evaluate(openAmount)) / Mathf.Clamp(lineTension * 0.0005f, 0.5f, 2f);
		canopyForce = -Vector3.up * canopyMass * 9.81f - lineVector.normalized * lineTension - vector * num3;
		if (landedTime == 0f)
		{
			openAmount += Mathf.Min(magnitude * 0.03f, 1f) * Time.deltaTime;
			canopyForce -= repelForce;
			lineTension = Mathf.Min(lineTension, rb.mass * 200f);
			rb.AddForceAtPosition(lineVector.normalized * lineTension, base.transform.position);
			rb.AddTorque(Vector3.Cross(-rb.transform.up, -lineVector.normalized * lineTension) * rb.mass * 0.001f * damping, ForceMode.Force);
			rb.angularDrag = 2f;
			canopy.transform.LookAt(base.transform.position, rb.transform.forward);
		}
		canopyVel += canopyForce / canopyMass * Time.deltaTime;
		canopy.transform.position += canopyVel * Time.deltaTime;
		parachuteMaterial.SetFloat("_wrinkleDisplacment", canopyDisplacement);
		parachuteMaterial.SetFloat("_wrinkleStrength", value);
	}

	public void Update()
	{
		if (rb == null)
		{
			if (!(attachedUnit != null))
			{
				UnityEngine.Object.Destroy(base.gameObject);
				return;
			}
			rb = attachedUnit.rb;
		}
		if (!canopy.activeSelf)
		{
			return;
		}
		if (radarAlt < 2f)
		{
			landedTime += Time.deltaTime;
			if (groundNormal.magnitude == 0f)
			{
				CheckGroundNormal();
			}
			if (landedTime > 5f)
			{
				onUnitLanded?.Invoke();
				UnityEngine.Object.Destroy(base.gameObject);
			}
			openAmount = Mathf.Lerp(openAmount, 0f, 0.5f * Time.deltaTime);
			maxDrag = Mathf.Lerp(maxDrag, 0f, Time.deltaTime);
			Vector3 vector = canopy.transform.position - rb.transform.position;
			vector += NetworkSceneSingleton<LevelInfo>.i.GetWind(base.transform.GlobalPosition()).normalized;
			vector.y = 0f;
			Vector3 vector2 = Vector3.Cross(groundNormal, vector.normalized);
			if (vector2 != Vector3.zero)
			{
				Quaternion b = Quaternion.LookRotation(vector2);
				canopy.transform.rotation = Quaternion.Slerp(canopy.transform.rotation, b, 0.5f * Time.deltaTime);
			}
			canopy.transform.position += (vector.normalized - Vector3.up) * Time.deltaTime / (1f + landedTime);
		}
		float num = maxRadius * chuteScaleHorizontal.Evaluate(openAmount);
		canopy.transform.localScale = new Vector3(num, num, maxRadius * chuteScaleVertical.Evaluate(openAmount));
		lines.transform.localScale = new Vector3(num, num, lineLength);
		lines.transform.LookAt(canopy.transform.position, rb.transform.forward);
	}

	public void CutCanopy()
	{
		canopy.SetActive(value: false);
		lines.SetActive(value: false);
	}

	public bool IsOpen()
	{
		return canopy.activeSelf;
	}

	public void CheckGroundNormal()
	{
		if (Physics.Raycast(base.transform.position, -Vector3.up, out var hitInfo, float.MaxValue, (int)PhysicsLayers.StaticsMask | (int)PhysicsLayers.ShipsMask))
		{
			groundNormal = hitInfo.normal;
		}
	}

	protected void OnDestroy()
	{
		if (canopy != null)
		{
			UnityEngine.Object.Destroy(canopy);
		}
		if (lines != null)
		{
			UnityEngine.Object.Destroy(lines);
		}
	}

	public float GetCurrentRadius()
	{
		return maxRadius * chuteScaleHorizontal.Evaluate(openAmount);
	}

	public Vector3 GetCanopyPosition()
	{
		if (!(canopy == null))
		{
			return canopy.transform.position;
		}
		return Vector3.zero;
	}

	public void AddRepelForce(Vector3 force)
	{
		repelForce = force;
	}
}
