using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class OpticalLandingSystem : MonoBehaviour
{
	[SerializeField]
	private Airbase airbase;

	[SerializeField]
	private GameObject meatball;

	[SerializeField]
	private GameObject datumLights;

	[SerializeField]
	private GameObject waveOffLights;

	[SerializeField]
	private GameObject redLight;

	[SerializeField]
	private GameObject whiteLight;

	[SerializeField]
	private Transform meatballPos;

	[SerializeField]
	private Transform meatballPosLow;

	[SerializeField]
	private Transform meatballPosHigh;

	[SerializeField]
	private float meatballMeasuredRange;

	[SerializeField]
	private UnitPart attachedPart;

	private Airbase.Runway runway;

	private readonly List<Aircraft> guidingAircraft = new List<Aircraft>();

	private Aircraft currentlyGuidingAircraft;

	private float errorTime;

	private Airbase.Runway.RunwayUsage runwayUsage;

	private void Start()
	{
		attachedPart.onApplyDamage += OpticalLandingSystem_OnPartDamage;
		runway = airbase.GetLandingRunway();
		runway.OnRegisterLanding += OpticalLandingSystem_OnRegisterLanding;
		Shutdown();
	}

	private void OnDestroy()
	{
		attachedPart.onApplyDamage -= OpticalLandingSystem_OnPartDamage;
		runway.OnRegisterLanding -= OpticalLandingSystem_OnRegisterLanding;
	}

	private void OpticalLandingSystem_OnPartDamage(UnitPart.OnApplyDamage e)
	{
		if (!(e.hitPoints > 50f))
		{
			attachedPart.onApplyDamage -= OpticalLandingSystem_OnPartDamage;
			Shutdown();
			Object.Destroy(this);
		}
	}

	private void OpticalLandingSystem_OnRegisterLanding(Aircraft aircraft)
	{
		guidingAircraft.Insert(0, aircraft);
		base.enabled = true;
		meatball.SetActive(value: true);
		datumLights.SetActive(value: true);
	}

	private void Shutdown()
	{
		base.enabled = false;
		meatball.SetActive(value: false);
		redLight.SetActive(value: false);
		whiteLight.SetActive(value: false);
		datumLights.SetActive(value: false);
		waveOffLights.SetActive(value: false);
	}

	private async UniTask WaveOff()
	{
		meatball.SetActive(value: false);
		redLight.SetActive(value: false);
		whiteLight.SetActive(value: false);
		datumLights.SetActive(value: false);
		waveOffLights.SetActive(value: true);
		await UniTask.Delay(5000);
		waveOffLights.SetActive(value: false);
		meatball.SetActive(value: true);
		datumLights.SetActive(value: true);
	}

	private void Update()
	{
		if (waveOffLights.activeSelf)
		{
			return;
		}
		for (int num = guidingAircraft.Count - 1; num >= 0; num--)
		{
			if (guidingAircraft[num] == null || guidingAircraft[num].disabled || Vector3.Dot((guidingAircraft[num].transform.position - base.transform.position).normalized, -base.transform.forward) < 0.8f)
			{
				guidingAircraft.RemoveAt(num);
			}
		}
		if (guidingAircraft.Count == 0)
		{
			Shutdown();
			return;
		}
		Aircraft aircraft = currentlyGuidingAircraft;
		List<Aircraft> list = guidingAircraft;
		if (aircraft != list[list.Count - 1])
		{
			List<Aircraft> list2 = guidingAircraft;
			currentlyGuidingAircraft = list2[list2.Count - 1];
			errorTime = 0f;
			runwayUsage = new Airbase.Runway.RunwayUsage(runway, reverse: false);
		}
		Vector3 vector = runway.Start.position - currentlyGuidingAircraft.transform.position;
		float magnitude = vector.magnitude;
		_ = currentlyGuidingAircraft.transform.position;
		_ = runway.Start.position;
		_ = currentlyGuidingAircraft.definition.spawnOffset;
		float num2 = Vector3.Dot(currentlyGuidingAircraft.rb.velocity - runway.GetVelocity(), vector.normalized);
		float num3 = Mathf.Min(magnitude / num2, 30f);
		if (runway.GetVelocity() != Vector3.zero)
		{
			magnitude = (runway.Start.position + runway.GetVelocity() * num3 - currentlyGuidingAircraft.transform.position).magnitude;
		}
		float num4 = (runwayUsage.GetGlideslopeError(currentlyGuidingAircraft, num3) * (1000f / Mathf.Max(magnitude, 100f)) + meatballMeasuredRange * 0.5f) / meatballMeasuredRange;
		meatballPos.position = Vector3.Lerp(meatballPosLow.position, meatballPosHigh.position, num4);
		if (num4 < 0.2f)
		{
			whiteLight.SetActive(value: false);
			if (num4 < 0f)
			{
				redLight.SetActive(Mathf.Sin(Time.timeSinceLevelLoad * 16f) > 0f);
			}
			else
			{
				redLight.SetActive(value: true);
			}
		}
		if (num4 > 0.8f)
		{
			redLight.SetActive(value: false);
			if (num4 > 1f)
			{
				whiteLight.SetActive(Mathf.Sin(Time.timeSinceLevelLoad * 16f) > 0f);
			}
			else
			{
				whiteLight.SetActive(value: true);
			}
		}
		if (num4 >= 0.2f && num4 <= 0.8f)
		{
			meatball.SetActive(value: true);
			whiteLight.SetActive(value: false);
			redLight.SetActive(value: false);
		}
		if (num4 < 0f || num4 > 1f)
		{
			errorTime += Time.deltaTime;
			if (errorTime > 7f && num3 < 10f)
			{
				WaveOff().Forget();
				guidingAircraft.Remove(currentlyGuidingAircraft);
			}
		}
		else
		{
			errorTime = 0f;
		}
	}
}
