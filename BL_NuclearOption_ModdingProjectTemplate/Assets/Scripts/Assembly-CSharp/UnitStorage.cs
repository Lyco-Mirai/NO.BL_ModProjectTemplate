using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NuclearOption.SavedMission;
using UnityEngine;

public class UnitStorage : MonoBehaviour
{
	[Serializable]
	private class Door
	{
		[SerializeField]
		private Transform transform;

		[SerializeField]
		private bool move;

		[SerializeField]
		private bool rotate;

		[SerializeField]
		private float openSpeed;

		[SerializeField]
		private Vector3 positionClosed;

		[SerializeField]
		private Vector3 positionOpen;

		[SerializeField]
		private Vector3 rotationClosed;

		[SerializeField]
		private Vector3 rotationOpen;

		private float currentPosition;

		public bool IsOpen()
		{
			return currentPosition == 1f;
		}

		public bool IsNotClosed()
		{
			return currentPosition > 0f;
		}

		public Transform GetTransform()
		{
			return transform;
		}

		public void Animate(float target)
		{
			currentPosition += Mathf.Clamp(target - currentPosition, (0f - openSpeed) * Time.deltaTime, openSpeed * Time.deltaTime);
			currentPosition = Mathf.Clamp01(currentPosition);
			if (move)
			{
				transform.localPosition = Vector3.Lerp(positionClosed, positionOpen, currentPosition);
			}
			if (rotate)
			{
				transform.localEulerAngles = Vector3.Lerp(rotationClosed, rotationOpen, currentPosition);
			}
		}

		public bool Finished()
		{
			if (currentPosition != 1f)
			{
				return currentPosition == 0f;
			}
			return true;
		}
	}

	public UnitPart UnitPart;

	public float MassLimit;

	private float currentMass;

	private float volumeLimit;

	private float currentVolume;

	[SerializeField]
	private Vector3 dimensions;

	[SerializeField]
	private SavedInventory inventory = new SavedInventory();

	[SerializeField]
	private Transform deployTransform;

	[SerializeField]
	private Transform approachTransform;

	[SerializeField]
	private float deployClearance = 20f;

	[SerializeField]
	private float approachDistance;

	[SerializeField]
	private float doorsLastOpened;

	[SerializeField]
	private Door[] doors;

	[SerializeField]
	private List<UnitDefinition> deployableTypes = new List<UnitDefinition>();

	private Unit lastDeployedUnit;

	private List<Unit> incoming = new List<Unit>();

	private List<UnitDefinition> deploying = new List<UnitDefinition>();

	private bool deployingUnits;

	private bool doorsClosed;

	[SerializeField]
	private bool deployRail;

	private void Awake()
	{
		doorsClosed = true;
		base.enabled = false;
		volumeLimit = dimensions.x * dimensions.y * dimensions.z;
		LinkWithDelay().Forget();
	}

	private async UniTask LinkWithDelay()
	{
		CancellationToken cancel = base.destroyCancellationToken;
		await UniTask.Yield();
		await UniTask.Yield();
		await UniTask.Yield();
		await UniTask.Yield();
		await UniTask.Yield();
		if (!cancel.IsCancellationRequested && InventoryFoundInMission(out var foundSavedInventory))
		{
			inventory = foundSavedInventory;
		}
	}

	public bool CanFit(UnitDefinition unitDefinition)
	{
		if (unitDefinition.length > dimensions.z || unitDefinition.width > dimensions.x || unitDefinition.height > dimensions.y)
		{
			return false;
		}
		return true;
	}

	public bool ContainsUnit(UnitDefinition unitDefinition)
	{
		if (inventory == null || inventory.StoredList == null)
		{
			return false;
		}
		foreach (UnitCount stored in inventory.StoredList)
		{
			if (stored.UnitType == unitDefinition.jsonKey)
			{
				return true;
			}
		}
		return false;
	}

	public void DeployUnits()
	{
		if (!deployingUnits)
		{
			DeployAllUnits().Forget();
		}
	}

	public bool HasUnits()
	{
		if (inventory == null)
		{
			return false;
		}
		return inventory.StoredList.Count > 0;
	}

	private async UniTask DeployAllUnits()
	{
		CancellationToken cancel = base.destroyCancellationToken;
		deployingUnits = true;
		if (inventory == null || inventory.StoredList.Count == 0)
		{
			return;
		}
		while (DoorsNotClosed())
		{
			await UniTask.Delay(5000);
			if (cancel.IsCancellationRequested || UnitPart.parentUnit.disabled)
			{
				return;
			}
		}
		while (CheckIncomingCount() > 0)
		{
			OpenDoors();
			await UniTask.Delay(5000);
			if (cancel.IsCancellationRequested || UnitPart.parentUnit.disabled)
			{
				return;
			}
		}
		deploying.Clear();
		for (int num = inventory.StoredList.Count - 1; num >= 0; num--)
		{
			string unitType = inventory.StoredList[num].UnitType;
			UnitDefinition unitDefinition = Encyclopedia.Lookup[unitType];
			int count = inventory.StoredList[num].Count;
			if (deployableTypes.Count <= 0 || deployableTypes.Contains(unitDefinition))
			{
				for (int i = 0; i < count; i++)
				{
					deploying.Add(unitDefinition);
				}
				inventory.AddOrRemove(unitDefinition, -count);
			}
		}
		for (int i2 = deploying.Count - 1; i2 >= 0; i2--)
		{
			UnitDefinition definition = deploying[i2];
			while (!DoorsOpen() || (lastDeployedUnit != null && !lastDeployedUnit.disabled && FastMath.InRange(lastDeployedUnit.transform.position, deployTransform.position, deployClearance)))
			{
				OpenDoors();
				await UniTask.Delay(1000);
				if (cancel.IsCancellationRequested || UnitPart.parentUnit.disabled)
				{
					return;
				}
			}
			lastDeployedUnit = NetworkSceneSingleton<Spawner>.i.SpawnUnit(definition, deployTransform.position, deployTransform.rotation, UnitPart.parentUnit.rb.GetPointVelocity(deployTransform.position), UnitPart.parentUnit, null);
			if (lastDeployedUnit.gameObject.TryGetComponent<UnitStorage>(out var component))
			{
				component.TryFillFromStorage(this);
			}
			base.enabled = true;
			if (lastDeployedUnit is GroundVehicle groundVehicle)
			{
				groundVehicle.MoveFromDepot();
			}
			if (lastDeployedUnit is Ship ship)
			{
				ship.Launch();
			}
			deploying.RemoveAt(i2);
		}
		while (lastDeployedUnit != null && !lastDeployedUnit.disabled && FastMath.InRange(lastDeployedUnit.transform.position, deployTransform.position, deployClearance * 2f))
		{
			OpenDoors();
			await UniTask.Delay(1000);
			if (cancel.IsCancellationRequested || UnitPart.parentUnit.disabled)
			{
				return;
			}
		}
		deployingUnits = false;
	}

	public bool DoorsOpen()
	{
		Door[] array = doors;
		for (int i = 0; i < array.Length; i++)
		{
			if (!array[i].IsOpen())
			{
				return false;
			}
		}
		return true;
	}

	public bool DoorsNotClosed()
	{
		Door[] array = doors;
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].IsNotClosed())
			{
				return true;
			}
		}
		return false;
	}

	public void OpenDoors()
	{
		doorsLastOpened = Time.timeSinceLevelLoad;
		if (doorsClosed)
		{
			MoveDoors().Forget();
		}
	}

	private async UniTask MoveDoors()
	{
		CancellationToken cancel = base.destroyCancellationToken;
		doorsClosed = false;
		while (!doorsClosed)
		{
			if (cancel.IsCancellationRequested || UnitPart.parentUnit.disabled)
			{
				return;
			}
			doorsClosed = true;
			Door[] array = doors;
			foreach (Door door in array)
			{
				int num = ((Time.timeSinceLevelLoad - doorsLastOpened < 5f) ? 1 : 0);
				door.Animate(num);
				if (num == 1 || !door.Finished())
				{
					doorsClosed = false;
				}
			}
			await UniTask.Yield();
		}
		doorsClosed = true;
	}

	public bool HasFinishedDeploying()
	{
		return !deployingUnits;
	}

	public Unit GetUnit()
	{
		return UnitPart.parentUnit;
	}

	public Transform GetDoorTransform()
	{
		if (doors.Length != 0)
		{
			return doors[0].GetTransform();
		}
		return deployTransform;
	}

	public GlobalPosition GetApproachTarget(Unit approachingUnit)
	{
		Vector3 vector = approachTransform.position + approachTransform.forward * approachDistance;
		float num = FastMath.Distance(approachTransform.GlobalPosition(), approachingUnit.GlobalPosition());
		Vector3 target = approachingUnit.transform.position - approachTransform.position;
		float t = Vector3.Dot(target.normalized, approachTransform.forward);
		Vector3 a = vector + Vector3.RotateTowards(approachTransform.forward * approachDistance, target, MathF.PI / 2f, 0f);
		Vector3 b = approachTransform.position + approachTransform.forward * num * 0.7f;
		b = Vector3.Lerp(a, b, t);
		return b.ToGlobalPosition();
	}

	public void RegisterIncoming(Unit incomingUnit)
	{
		if (!incoming.Contains(incomingUnit))
		{
			incoming.Add(incomingUnit);
		}
	}

	public int CheckIncomingCount()
	{
		for (int num = incoming.Count - 1; num >= 0; num--)
		{
			Unit unit = incoming[num];
			if (unit == null || unit.disabled || !FastMath.InRange(unit.transform.position, approachTransform.position, 1000f))
			{
				incoming.RemoveAt(num);
			}
		}
		return incoming.Count;
	}

	public void Transfer(UnitStorage fromInventory)
	{
		inventory.Transfer(fromInventory.inventory);
	}

	public void TryFillFromStorage(UnitStorage fromStorage)
	{
		SavedInventory savedInventory = fromStorage.inventory;
		for (int num = savedInventory.StoredList.Count - 1; num >= 0; num--)
		{
			UnitCount unitCount = savedInventory.StoredList[num];
			UnitDefinition unitDefinition = Encyclopedia.Lookup[unitCount.UnitType];
			if (CanStoreUnit(unitDefinition, unitCount.Count, out var numAllowed) && numAllowed > 0)
			{
				fromStorage.AddOrRemoveUnit(unitDefinition, -numAllowed);
				AddOrRemoveUnit(unitDefinition, numAllowed);
			}
		}
	}

	public bool CanStoreUnit(UnitDefinition definition, int numberToStore, out int numAllowed)
	{
		numAllowed = 0;
		if (!CanFit(definition))
		{
			return false;
		}
		float num = MassLimit - currentMass;
		numAllowed = Mathf.Min(Mathf.FloorToInt(num / definition.mass), numberToStore);
		float num2 = volumeLimit - currentVolume;
		numAllowed = Mathf.Min(numAllowed, Mathf.FloorToInt(num2 / (definition.length + definition.width + definition.height)));
		return numAllowed > 0;
	}

	public void Store(Unit unit)
	{
		if (unit.LocalSim)
		{
			incoming.Remove(unit);
			inventory.AddOrRemove(unit.definition, 1);
			unit.NetworkunitState = Unit.UnitState.Returned;
			unit.Networkdisabled = true;
			UnityEngine.Object.Destroy(unit.gameObject, 3f);
		}
	}

	public List<UnitCount> GetStoredList()
	{
		return inventory?.StoredList;
	}

	public void AddOrRemoveUnitEditor(SavedUnit savedUnit, UnitDefinition unitDefinition, int change)
	{
		if (GameManager.gameState != GameState.Editor)
		{
			Debug.LogError("AddOrRemoveUnitEditor should only be used in editor");
			return;
		}
		SavedInventory orCreateStorageList = GetOrCreateStorageList(savedUnit);
		orCreateStorageList.AddOrRemove(unitDefinition, change);
		if (orCreateStorageList.StoredList.Count <= 0)
		{
			savedUnit.inventory = null;
		}
	}

	public void AddOrRemoveUnit(UnitDefinition unitDefinition, int amount)
	{
		if (GameManager.gameState == GameState.Editor)
		{
			Debug.LogError("AddOrRemoveUnit should not be used in editor");
			return;
		}
		inventory.AddOrRemove(unitDefinition, amount);
		currentMass += (float)amount * unitDefinition.mass;
		currentVolume += (float)amount * (unitDefinition.length * unitDefinition.height * unitDefinition.width);
	}

	public bool InventoryFoundInMission(out SavedInventory foundSavedInventory)
	{
		foundSavedInventory = null;
		if (UnitPart == null || UnitPart.parentUnit == null)
		{
			Debug.LogError("UnitStorage on " + base.gameObject.name + " has missing UnitPart or parentUnit reference", this);
			return false;
		}
		SavedUnit savedUnit = UnitPart.parentUnit.SavedUnit;
		if (savedUnit != null && savedUnit.inventory != null)
		{
			foundSavedInventory = savedUnit.inventory;
			return true;
		}
		return false;
	}

	public SavedInventory GetOrCreateStorageList(SavedUnit owner)
	{
		if (owner.inventory == null)
		{
			owner.inventory = new SavedInventory();
		}
		inventory = owner.inventory;
		return inventory;
	}

	private void FixedUpdate()
	{
		if (!deployRail || lastDeployedUnit == null || !FastMath.InRange(lastDeployedUnit.transform.position, deployTransform.position, deployClearance))
		{
			base.enabled = false;
			return;
		}
		Vector3 vector = deployTransform.position + Vector3.Project(lastDeployedUnit.transform.position - deployTransform.position, deployTransform.forward);
		Vector3 vector2 = lastDeployedUnit.transform.position - vector;
		vector2 -= 0.2f * deployTransform.forward;
		lastDeployedUnit.rb.AddForce(-vector2 * lastDeployedUnit.rb.mass * 10f);
		Vector3 vector3 = Vector3.up * (0f - TargetCalc.GetAngleOnAxis(deployTransform.forward, lastDeployedUnit.transform.forward, Vector3.up));
		lastDeployedUnit.rb.AddTorque(vector3 * lastDeployedUnit.rb.mass * 40f);
	}

	public Transform GetApproachTransform()
	{
		return approachTransform;
	}
}
