using UnityEngine;

public class RelaxedStabilityController : MonoBehaviour
{
	[SerializeField]
	private float canardRange;

	[SerializeField]
	private GameObject engine;

	private IEngine engineInterface;

	private float effectiveness = 1f;

	private void Awake()
	{
		engineInterface = engine.GetComponent<IEngine>();
		engineInterface.OnEngineDisable += RelaxedStabilityController_OnEngineDisable;
	}

	public void FilterInput(ControlInputs inputs, Rigidbody rb, float gForce, float rawPitch)
	{
		if (effectiveness != 0f)
		{
			float a = TargetCalc.GetAngleOnAxis(rb.transform.forward, rb.velocity, rb.transform.right) / canardRange;
			if (rb.velocity.sqrMagnitude > 900f)
			{
				inputs.pitch = Mathf.Lerp(a, rawPitch, Mathf.Abs(rawPitch));
			}
		}
	}

	private void RelaxedStabilityController_OnEngineDisable()
	{
		effectiveness = 0f;
	}
}
