using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WreckCollector : MonoBehaviour
{
	public enum CollectorState
	{
		Stop = 0,
		Wait = 1,
		GoToWreck = 2,
		GoToDepot = 3
	}

	[SerializeField]
	private Unit attachedUnit;

	[SerializeField]
	private float range;

	[SerializeField]
	private int maxWrecks = 10;

	[SerializeField]
	private int currentWrecks;

	private Wreckage currentTarget;

	private Building nearestDepot;

	public List<GameObject> storedWrecks = new List<GameObject>();

	public CollectorState currentState;

	private float defaultMass;

	private void Start()
	{
		attachedUnit.onInitialize += WreckCollector_OnInitialize;
	}

	private void WreckCollector_OnInitialize()
	{
		foreach (GameObject storedWreck in storedWrecks)
		{
			storedWreck.SetActive(value: false);
		}
		currentTarget = null;
		nearestDepot = null;
		defaultMass = attachedUnit.rb.mass;
		currentState = CollectorState.Wait;
		if (attachedUnit.IsServer)
		{
			this.StartSlowUpdate(1f, UpdateState);
		}
	}

	private void UpdateState()
	{
		if (!attachedUnit.disabled)
		{
			Debug.Log("WRECK REMOVAL " + attachedUnit.name + ", ENTER LOOP");
			switch (currentState)
			{
			case CollectorState.Wait:
				FindWreck();
				break;
			case CollectorState.GoToWreck:
				ReachedWreckCheck();
				break;
			case CollectorState.GoToDepot:
				ReachedDepotCheck();
				break;
			case CollectorState.Stop:
				break;
			}
		}
	}

	private void FindWreck()
	{
		List<Wreckage> list = BattlefieldGrid.GetWrecksInRangeEnumerable(base.transform.GlobalPosition(), 1000f).ToList();
		if (list.Count == 0)
		{
			return;
		}
		float num = 1000f;
		for (int i = 0; i < list.Count; i++)
		{
			Wreckage wreckage = list[i];
			if (FastMath.InRange(wreckage.transform.position, base.transform.position, num))
			{
				float num2 = Vector3.Distance(wreckage.transform.position, base.transform.position);
				if (num2 < num && num2 > 2f * range)
				{
					currentTarget = wreckage;
					num = num2;
				}
			}
		}
		if (currentTarget != null)
		{
			if (attachedUnit is GroundVehicle groundVehicle)
			{
				groundVehicle.UnitCommand.SetDestination(currentTarget.transform.GlobalPosition(), playerCommand: false);
				currentState = CollectorState.GoToWreck;
				Debug.Log($"WRECK {currentTarget.name} HAS BEEN FOUND BY {attachedUnit.name} DIST {num:F0}");
			}
		}
		else if (currentWrecks > 0)
		{
			Debug.Log("NO WRECK HAS BEEN FOUND BY " + attachedUnit.name + ", LOOK FOR DEPOT");
			FindDepot();
		}
	}

	private void FindDepot()
	{
		base.transform.GlobalPosition();
		nearestDepot = attachedUnit.NetworkHQ.GetNearestDepot(attachedUnit.transform.position, 10000f);
		if (nearestDepot != null && !nearestDepot.disabled)
		{
			if (attachedUnit is GroundVehicle groundVehicle)
			{
				groundVehicle.UnitCommand.SetDestination(nearestDepot.transform.GlobalPosition(), playerCommand: false);
				currentState = CollectorState.GoToDepot;
				Debug.Log(attachedUnit.name + " FOUND " + nearestDepot.name);
			}
		}
		else
		{
			currentState = CollectorState.Stop;
		}
	}

	private void ReachedWreckCheck()
	{
		if (currentState == CollectorState.GoToWreck && FastMath.InRange(currentTarget.transform.position, base.transform.position, range))
		{
			CollectWreck(currentTarget);
		}
	}

	private void CollectWreck(Wreckage wreck)
	{
		if (storedWrecks.Count > currentWrecks)
		{
			storedWrecks[currentWrecks].SetActive(value: true);
		}
		currentWrecks++;
		attachedUnit.rb.mass += 1000f;
		currentTarget = null;
		Debug.Log($"WRECK {wreck.name} HAS BEEN COLLECTED BY {attachedUnit.name} TOTAL MASS {attachedUnit.rb.mass}");
		wreck.Disintegrate();
		if (currentWrecks == maxWrecks)
		{
			FindDepot();
		}
		else
		{
			currentState = CollectorState.Wait;
		}
	}

	private void ReachedDepotCheck()
	{
		if (currentState == CollectorState.GoToDepot && FastMath.InRange(nearestDepot.transform.position, base.transform.position, nearestDepot.maxRadius))
		{
			EmptyWrecks();
		}
	}

	private void EmptyWrecks()
	{
		Debug.Log($"{attachedUnit.name} HAS REACHED {nearestDepot.name} UNLOADING {currentWrecks} WRECKS");
		foreach (GameObject storedWreck in storedWrecks)
		{
			storedWreck.SetActive(value: false);
		}
		RewardWreckCollection(currentWrecks);
		currentWrecks = 0;
		attachedUnit.rb.mass += defaultMass;
		currentTarget = null;
		nearestDepot = null;
		currentState = CollectorState.Wait;
	}

	private void RewardWreckCollection(int nbWrecks)
	{
		attachedUnit.NetworkHQ.AddScore(0.1f * (float)nbWrecks);
	}
}
