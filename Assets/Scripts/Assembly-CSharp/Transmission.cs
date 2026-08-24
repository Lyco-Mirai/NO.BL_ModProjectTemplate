using System.Collections.Generic;
using UnityEngine;

public class Transmission : MonoBehaviour
{
	[SerializeField]
	private GameObject[] powerSources;

	[SerializeField]
	private float governorSmoothing = 0.5f;

	private IPowerSource[] sourceInterfaces;

	private List<IPowerOutput> outputInterfaces = new List<IPowerOutput>();

	private List<float> powerRequests = new List<float>();

	private float totalPowerRequested;

	private float maxPowerOutput;

	private float throttlePosition;

	private float throttleSmoothingVel;

	private void Awake()
	{
		sourceInterfaces = new IPowerSource[powerSources.Length];
		for (int i = 0; i < powerSources.Length; i++)
		{
			sourceInterfaces[i] = powerSources[i].GetComponent<IPowerSource>();
			maxPowerOutput += sourceInterfaces[i].GetMaxPower();
		}
	}

	public float GetMaxPower()
	{
		return maxPowerOutput;
	}

	public void RequestPower(IPowerOutput output, float powerRequested)
	{
		outputInterfaces.Add(output);
		powerRequests.Add(powerRequested);
		totalPowerRequested += powerRequested;
	}

	private void FixedUpdate()
	{
		_ = (maxPowerOutput - totalPowerRequested) / maxPowerOutput;
		float num = 0f;
		IPowerSource[] array = sourceInterfaces;
		foreach (IPowerSource powerSource in array)
		{
			powerSource.Throttle(throttlePosition);
			num += powerSource.GetPower();
		}
		float num2 = num / Mathf.Max(totalPowerRequested, 1f);
		for (int j = 0; j < outputInterfaces.Count; j++)
		{
			outputInterfaces[j].SendPower(powerRequests[j] * num2);
		}
		throttlePosition = Mathf.SmoothDamp(throttlePosition, totalPowerRequested / maxPowerOutput, ref throttleSmoothingVel, governorSmoothing);
		outputInterfaces.Clear();
		powerRequests.Clear();
		totalPowerRequested = 0f;
	}
}
