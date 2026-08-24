using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class CargoDeploymentSystem : MonoBehaviour
{
	[SerializeField]
	private Unit attachedUnit;

	[SerializeField]
	private List<Parachute> listParachutes;

	[SerializeField]
	private float radialStiffness = 100000f;

	[SerializeField]
	private Vector3 baseFrameDimensions;

	[SerializeField]
	private GameObject liftingFrame;

	private bool initialized;

	private bool chuteOpen;

	public void Initialize(Unit attachedUnit)
	{
		this.attachedUnit = attachedUnit;
		attachedUnit.onDisableUnit += CargoDeploymentSystem_OnUnitDisabled;
		if (attachedUnit is GroundVehicle groundVehicle)
		{
			groundVehicle.SetWheelsLocked(wheelsLocked: true);
			StowTurrets(groundVehicle).Forget();
		}
		UnitDefinition definition = attachedUnit.definition;
		base.transform.localPosition = -definition.spawnOffset;
		liftingFrame.transform.localScale = new Vector3(definition.width * 1.1f / baseFrameDimensions.x, definition.height * 1.1f / baseFrameDimensions.y, definition.length * 1.1f / baseFrameDimensions.z);
		foreach (Parachute listParachute in listParachutes)
		{
			listParachute.transform.localScale = new Vector3(1f / liftingFrame.transform.localScale.x, 1f / liftingFrame.transform.localScale.y, 1f / liftingFrame.transform.localScale.z);
			listParachute.SetAttachedUnit(attachedUnit);
			listParachute.gameObject.SetActive(value: true);
			listParachute.onUnitLanded = (Action)Delegate.Combine(listParachute.onUnitLanded, new Action(CargoDeploymentSystem_OnUnitLanded));
		}
		initialized = true;
	}

	private async UniTask StowTurrets(GroundVehicle vehicle)
	{
		CancellationToken cancel = base.destroyCancellationToken;
		await UniTask.Delay(100);
		if (!cancel.IsCancellationRequested)
		{
			vehicle.StowTurrets(stowed: true);
		}
	}

	private void FixedUpdate()
	{
		for (int i = 0; i < listParachutes.Count; i++)
		{
			if (listParachutes[i] == null || !listParachutes[i].IsOpen())
			{
				continue;
			}
			if (!chuteOpen)
			{
				chuteOpen = true;
			}
			Vector3 zero = Vector3.zero;
			for (int j = 0; j < listParachutes.Count; j++)
			{
				if (i != j)
				{
					Vector3 vector = listParachutes[i].GetCanopyPosition() - listParachutes[j].GetCanopyPosition();
					float num = 4f * (listParachutes[i].GetCurrentRadius() + listParachutes[j].GetCurrentRadius());
					if (vector.magnitude < num)
					{
						zero -= radialStiffness * (num - vector.magnitude) * vector.normalized;
					}
					if (vector.magnitude == 0f)
					{
						zero -= radialStiffness * listParachutes[i].transform.right;
					}
				}
			}
			listParachutes[i].AddRepelForce(zero);
		}
	}

	public void CargoDeploymentSystem_OnUnitDisabled(Unit unit)
	{
		UnityEngine.Object.Destroy(base.gameObject);
	}

	public void CargoDeploymentSystem_OnUnitLanded()
	{
		UnityEngine.Object.Destroy(base.gameObject);
	}

	protected void OnDestroy()
	{
		if (!initialized)
		{
			return;
		}
		attachedUnit.onDisableUnit -= CargoDeploymentSystem_OnUnitDisabled;
		foreach (Parachute listParachute in listParachutes)
		{
			listParachute.onUnitLanded = (Action)Delegate.Remove(listParachute.onUnitLanded, new Action(CargoDeploymentSystem_OnUnitLanded));
		}
		if (attachedUnit is GroundVehicle groundVehicle)
		{
			groundVehicle.SetWheelsLocked(wheelsLocked: false);
			groundVehicle.StowTurrets(stowed: false);
		}
		if (!attachedUnit.disabled && !attachedUnit.enabled)
		{
			attachedUnit.enabled = true;
		}
	}
}
