using UnityEngine;

public class VaporEffect : MonoBehaviour
{
	[SerializeField]
	private Aircraft aircraft;

	[SerializeField]
	private VaporEmitter[] emitters;

	private float alpha;

	private float airspeed;

	private void OnEnable()
	{
		for (int i = 0; i < emitters.Length; i++)
		{
			emitters[i].Initialize();
		}
	}

	private void FixedUpdate()
	{
		if (aircraft.rb != null)
		{
			airspeed = aircraft.speed;
			alpha = TargetCalc.GetAngleOnAxis(base.transform.forward, aircraft.rb.velocity, base.transform.right);
			for (int i = 0; i < emitters.Length; i++)
			{
				emitters[i].Emit(alpha, airspeed, aircraft.rb.velocity, aircraft.transform.GlobalPosition().y, aircraft.displayDetail);
			}
		}
	}
}
