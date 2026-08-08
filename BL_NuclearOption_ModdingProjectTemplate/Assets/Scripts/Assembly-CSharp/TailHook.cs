using UnityEngine;

public class TailHook : MonoBehaviour
{
	[SerializeField]
	private Transform hinge;

	[SerializeField]
	private Transform hook;

	[SerializeField]
	private Transform castPoint;

	public Transform hookEnd;

	public UnitPart unitPart;

	[SerializeField]
	private float mass;

	[SerializeField]
	private float stowedAngle;

	[SerializeField]
	private float deployedAngle;

	private float deployedAmount;

	private bool deployed;

	private float hookLength;

	private Aircraft aircraft;

	private bool hooked;

	private void Awake()
	{
		hookLength = Vector3.Distance(hook.position, hookEnd.position);
	}

	public float GetMass()
	{
		return mass;
	}

	private void Start()
	{
		unitPart = base.gameObject.GetComponentInParent<UnitPart>();
		aircraft = unitPart.parentUnit as Aircraft;
		this.StartSlowUpdate(1f, CheckDeployConditions);
	}

	private void CheckDeployConditions()
	{
		if (aircraft.radarAlt > 30f && aircraft.radarAlt < 120f && aircraft.speed < 100f && aircraft.rb.velocity.y < 0f && !deployed)
		{
			if (aircraft == SceneSingleton<CombatHUD>.i.aircraft)
			{
				SceneSingleton<AircraftActionsReport>.i.ReportText("Tail Hook Deployed", 4f);
			}
			deployed = true;
			base.enabled = true;
		}
		if ((aircraft.speed > 100f || aircraft.speed < 2f) && deployed)
		{
			if (aircraft == SceneSingleton<CombatHUD>.i.aircraft)
			{
				SceneSingleton<AircraftActionsReport>.i.ReportText("Tail Hook Retracted", 4f);
			}
			deployed = false;
			if (hooked)
			{
				Unhook();
			}
		}
	}

	public void ApplyForce(Vector3 force)
	{
		if (PlayerSettings.debugVis)
		{
			GameObject obj = Object.Instantiate(GameAssets.i.debugArrow, hookEnd);
			obj.transform.rotation = Quaternion.LookRotation(force);
			obj.transform.localScale = new Vector3(1f, 1f, force.magnitude);
			Object.Destroy(obj, 0.1f);
		}
		float angleOnAxis = TargetCalc.GetAngleOnAxis(castPoint.forward, force, castPoint.right);
		deployedAmount -= Mathf.Clamp(angleOnAxis * 0.01f, -2f * Time.deltaTime, 2f * Time.deltaTime);
		deployedAmount = Mathf.Clamp(deployedAmount, 0.01f, 1f);
		unitPart.rb.AddForceAtPosition(force, hookEnd.position);
	}

	public void Unhook()
	{
		hooked = false;
	}

	private void FixedUpdate()
	{
		if (!hooked)
		{
			deployedAmount += (deployed ? (0.5f * Time.fixedDeltaTime) : (-0.5f * Time.fixedDeltaTime));
			deployedAmount = Mathf.Clamp01(deployedAmount);
			if (Physics.Linecast(hinge.position, hookEnd.position, out var hitInfo) && hitInfo.distance < hookLength)
			{
				ArrestorGear component = hitInfo.collider.GetComponent<ArrestorGear>();
				if (component != null && component.Hook(this))
				{
					hooked = true;
					aircraft.ShakeAircraft(0.5f, 0f);
					if (aircraft == SceneSingleton<CombatHUD>.i.aircraft)
					{
						if (component.wireNumber == 1)
						{
							SceneSingleton<AircraftActionsReport>.i.ReportText("Caught 1st Wire", 4f);
						}
						else if (component.wireNumber == 2)
						{
							SceneSingleton<AircraftActionsReport>.i.ReportText("Caught 2nd Wire", 4f);
						}
						else if (component.wireNumber == 3)
						{
							SceneSingleton<AircraftActionsReport>.i.ReportText("Caught 3rd Wire", 4f);
						}
						else if (component.wireNumber == 4)
						{
							SceneSingleton<AircraftActionsReport>.i.ReportText("Caught 4th Wire", 4f);
						}
					}
				}
				deployedAmount -= 4f * Time.deltaTime;
				if (hitInfo.collider.sharedMaterial != GameAssets.i.terrainMaterial)
				{
					aircraft.ThrowSparks(hitInfo.point, Vector3.zero);
				}
			}
		}
		hook.localEulerAngles = new Vector3(Mathf.Lerp(stowedAngle, deployedAngle, deployedAmount), 0f, 0f);
		if (deployedAmount == 0f)
		{
			base.enabled = false;
		}
	}
}
