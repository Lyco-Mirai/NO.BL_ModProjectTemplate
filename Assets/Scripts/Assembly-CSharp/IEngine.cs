using System;
using UnityEngine;

public interface IEngine
{
	Transform transform { get; }

	event Action OnEngineDisable;

	event Action OnEngineDamage;

	float GetThrust();

	float GetMaxThrust();

	float GetRPM();

	float GetRPMRatio();

	void SetInteriorSounds(bool useInteriorSound);
}
