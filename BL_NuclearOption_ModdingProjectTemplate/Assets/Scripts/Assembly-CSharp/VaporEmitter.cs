using System;
using UnityEngine;

[Serializable]
public class VaporEmitter
{
	public ParticleSystem particles;

	private ParticleSystem.MainModule main;

	public Transform[] emitTransforms;

	public float emitFrequency;

	public float minSpeed = 20f;

	public float opacity = 1f;

	public AnimationCurve alphaThresholdVelocity;

	[SerializeField]
	private bool localSpace;

	[SerializeField]
	private bool transonic;

	[SerializeField]
	private bool contrail;

	private float emitCounter;

	private float currentOpacity = -0.5f;

	private ParticleSystem.EmitParams emitParams;

	private float minAltitude = 7500f;

	private float maxAltitude = 12500f;

	public void Initialize()
	{
		main = particles.main;
		if (!localSpace)
		{
			main.simulationSpace = ParticleSystemSimulationSpace.Custom;
			main.customSimulationSpace = Datum.origin;
		}
	}

	public void Emit(float alpha, float airspeed, Vector3 velocity, float altitude, float detail)
	{
		bool flag = false;
		if (transonic)
		{
			float speedOfSound = LevelInfo.GetSpeedOfSound(altitude);
			Keyframe[] keys = alphaThresholdVelocity.keys;
			keys[3].time = speedOfSound;
			keys[2].time = speedOfSound - 40f;
			keys[4].time = speedOfSound + 40f;
			alphaThresholdVelocity.keys = keys;
		}
		flag = ((!contrail) ? (detail > 0.5f && Mathf.Abs(alpha) > alphaThresholdVelocity.Evaluate(airspeed)) : (altitude > minAltitude && altitude < maxAltitude));
		float num = (flag ? 1f : (-0.5f));
		currentOpacity += Mathf.Clamp(num - currentOpacity, 0f - Time.fixedDeltaTime, Time.fixedDeltaTime);
		if (!flag && !(currentOpacity > -0.5f))
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
					emitParams.startColor = new Color(1f, 1f, 1f, currentOpacity);
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
