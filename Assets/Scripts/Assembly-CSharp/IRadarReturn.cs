using UnityEngine;

public interface IRadarReturn
{
	float GetRadarReturn(Vector3 source, Radar radar, Unit emitter, float dist, float clutter, RadarParams radarParams, bool triggerWarning);

	float GetECMIntensity();
}
