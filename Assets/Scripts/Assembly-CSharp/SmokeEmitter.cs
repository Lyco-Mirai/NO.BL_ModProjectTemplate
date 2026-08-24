using System;
using UnityEngine;

[Serializable]
public class SmokeEmitter
{
	public ParticleSystem particles;

	private ParticleSystem.MainModule main;

	public Transform[] emitTransforms;

	public float emitFrequency;

	public float minSpeed = 20f;

	public float opacity = 1f;

	public Color color = Color.red;

	[SerializeField]
	private bool localSpace;

	private float emitCounter;

	private ParticleSystem.EmitParams emitParams;

	public void Initialize()
	{
		main = particles.main;
		if (!localSpace)
		{
			main.simulationSpace = ParticleSystemSimulationSpace.Custom;
			main.customSimulationSpace = Datum.origin;
		}
	}

	public void Emit(bool active, float airspeed, Vector3 velocity)
	{
		if (!active)
		{
			return;
		}
		emitCounter += Time.deltaTime * Mathf.Min(emitFrequency, airspeed / minSpeed * emitFrequency);
		if (!(emitCounter > 1f))
		{
			return;
		}
		emitCounter = 0f;
		for (int i = 0; i < emitTransforms.Length; i++)
		{
			if (emitTransforms[i] != null)
			{
				if (!localSpace)
				{
					emitParams.position = emitTransforms[i].position.ToGlobalPosition().AsVector3();
					emitParams.velocity = velocity + main.startSpeed.constant * new Vector3(UnityEngine.Random.Range(-1, 1), UnityEngine.Random.Range(-1, 1), UnityEngine.Random.Range(-1, 1));
					particles.Emit(emitParams, 1);
				}
				else
				{
					emitTransforms[i].transform.rotation = Quaternion.LookRotation(-velocity);
					particles.Play();
				}
			}
		}
	}
}
