using System.Collections.Generic;
using UnityEngine;

public class SetGlobalParticles : MonoBehaviour
{
	[SerializeField]
	private List<ParticleSystem> systems = new List<ParticleSystem>();

	private void Start()
	{
		foreach (ParticleSystem system in systems)
		{
			ParticleSystem.MainModule main = system.main;
			main.simulationSpace = ParticleSystemSimulationSpace.Custom;
			main.customSimulationSpace = Datum.origin;
		}
		Object.Destroy(this);
	}
}
