using UnityEngine;

public class AirCushion : MonoBehaviour, IThrustSource
{
	[SerializeField]
	private Unit attachedUnit;

	[SerializeField]
	private float spring;

	[SerializeField]
	private float damp;

	[SerializeField]
	private float aligningStrength;

	[SerializeField]
	private float maxHeight;

	[SerializeField]
	private float sinkRate;

	[SerializeField]
	private float dragForward;

	[SerializeField]
	private float dragLateral;

	[SerializeField]
	private float steerAxisDamping;

	[SerializeField]
	private float conditionMin;

	[SerializeField]
	private Transform castTransform;

	[SerializeField]
	private UnitPart[] criticalParts;

	private float currentThrust;

	private float condition;

	private float inflatedSpring;

	private float inflatedDamp;

	private float lastSurfaceSample;

	private bool sinking;

	private bool deflating;

	private bool inflating;

	private bool overLand;

	private Vector3 thrustApplyOffset;

	private Plane surfacePlane;

	public float GetMaxThrust()
	{
		return spring * maxHeight;
	}

	public float GetThrust()
	{
		return currentThrust;
	}

	private void Awake()
	{
		CalcThrustOffset();
		attachedUnit.onDisableUnit += AirCushion_OnUnitDisabled;
		UnitPart[] array = criticalParts;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].onApplyDamage += AirCushion_OnCriticalPartDamage;
		}
		inflatedSpring = spring;
		inflatedDamp = damp;
	}

	private void AirCushion_OnCriticalPartDamage(UnitPart.OnApplyDamage e)
	{
		CalcThrustOffset();
	}

	private void CalcThrustOffset()
	{
		float num = 0f;
		thrustApplyOffset = Vector3.zero;
		UnitPart[] array = criticalParts;
		foreach (UnitPart unitPart in array)
		{
			num += unitPart.hitPoints;
			thrustApplyOffset += attachedUnit.transform.InverseTransformPoint(unitPart.transform.position) * unitPart.hitPoints;
		}
		int num2 = 100 * criticalParts.Length;
		float num3 = Mathf.Max(num / (float)num2, 0f);
		condition = (num3 - conditionMin) / (1f - conditionMin);
		thrustApplyOffset /= (float)(criticalParts.Length * 100);
		thrustApplyOffset.y = 0f;
	}

	public bool Landed()
	{
		return overLand;
	}

	public void Deflate()
	{
		deflating = true;
		inflating = false;
	}

	public void Inflate()
	{
		inflating = true;
		deflating = false;
	}

	private void AirCushion_OnUnitDisabled(Unit unit)
	{
		sinking = true;
	}

	private void SampleSurface()
	{
		if (!(Time.timeSinceLevelLoad - lastSurfaceSample < 0.2f))
		{
			lastSurfaceSample = Time.timeSinceLevelLoad;
			surfacePlane.SetNormalAndPosition(Vector3.up, Vector3.zero);
			float enter;
			bool num = surfacePlane.Raycast(new Ray(castTransform.position.ToGlobalPosition().AsVector3(), -castTransform.up), out enter) && castTransform.position.y > Datum.LocalSeaY && enter > 0f && enter < maxHeight;
			RaycastHit hitInfo;
			bool flag = Physics.Linecast(castTransform.position, castTransform.position - castTransform.up * maxHeight, out hitInfo);
			float num2 = (num ? enter : maxHeight);
			overLand = flag && hitInfo.distance < num2;
			if (overLand)
			{
				surfacePlane.SetNormalAndPosition(hitInfo.normal, hitInfo.point.ToGlobalPosition().AsVector3());
			}
		}
	}

	private void FixedUpdate()
	{
		if (sinking || deflating)
		{
			spring -= sinkRate * Time.fixedDeltaTime;
			base.transform.localScale = new Vector3(1f, Mathf.Lerp(0.5f, 1f, spring / inflatedSpring), 1f);
			if (spring <= 0f)
			{
				spring = 0f;
				damp = 0f;
				deflating = false;
			}
		}
		if (inflating)
		{
			spring += sinkRate * Time.fixedDeltaTime;
			damp = inflatedDamp;
			base.transform.localScale = new Vector3(1f, Mathf.Lerp(0.5f, 1f, spring / inflatedSpring), 1f);
			if (spring >= inflatedSpring)
			{
				base.transform.localScale = Vector3.one;
				spring = inflatedSpring;
				inflating = false;
			}
		}
		if (spring <= 0f)
		{
			return;
		}
		SampleSurface();
		if (surfacePlane.Raycast(new Ray(castTransform.position.ToGlobalPosition().AsVector3(), -castTransform.up), out var enter) && enter < maxHeight && enter > 0f)
		{
			Vector3 vector = castTransform.InverseTransformDirection(attachedUnit.rb.angularVelocity);
			float num = Mathf.Max(maxHeight - enter, 0f) * spring;
			float num2 = Vector3.Dot(attachedUnit.rb.velocity, -surfacePlane.normal);
			float num3 = damp * num2;
			float num4 = (0f - vector.y) * steerAxisDamping * attachedUnit.rb.mass;
			float num5 = Vector3.Dot(attachedUnit.rb.velocity, castTransform.forward);
			float num6 = Vector3.Dot(attachedUnit.rb.velocity, castTransform.right);
			Vector3 vector2 = Mathf.Abs(num5) * num5 * castTransform.forward * dragForward;
			vector2 += Mathf.Abs(num6) * num6 * castTransform.right * dragLateral;
			if (overLand)
			{
				vector2 *= 2f;
			}
			Vector3 vector3 = 50f * -Vector3.Cross(surfacePlane.normal, castTransform.up);
			vector3 -= 15f * attachedUnit.rb.angularVelocity;
			currentThrust = Mathf.Max(num + num3, 0f);
			attachedUnit.rb.AddForceAtPosition((currentThrust * surfacePlane.normal - vector2) * condition, castTransform.TransformPoint(thrustApplyOffset));
			attachedUnit.rb.AddTorque((vector3 * attachedUnit.rb.mass * aligningStrength + castTransform.up * num4) * condition);
		}
	}
}
