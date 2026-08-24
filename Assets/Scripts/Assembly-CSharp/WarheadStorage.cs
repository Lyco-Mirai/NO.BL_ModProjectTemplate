using System;
using Mirage;
using Mirage.Serialization;
using UnityEngine;

public class WarheadStorage : NetworkBehaviour
{
	public Unit attachedUnit;

	[SyncVar]
	public int number;

	[SerializeField]
	private UnitPart criticalPart;

	private bool selfDisabled;

	[NonSerialized]
	private const int SYNC_VAR_COUNT = 1;

	[NonSerialized]
	private const int RPC_COUNT = 0;

	public bool Disabled
	{
		get
		{
			if (!selfDisabled)
			{
				return attachedUnit.disabled;
			}
			return true;
		}
	}

	public int Networknumber
	{
		get
		{
			return number;
		}
		set
		{
			if (!SyncVarEqual(value, number))
			{
				int num = number;
				number = value;
				SetDirtyBit(1uL);
			}
		}
	}

	private void OnValidate()
	{
	}

	private void Awake()
	{
		base.Identity.OnStartServer.AddListener(OnStartServer);
	}

	private void OnStartServer()
	{
		attachedUnit.onDisableUnit += AttachedUnit_OnDisable;
		if (criticalPart != null)
		{
			criticalPart.onApplyDamage += Part_OnApplyDamage;
		}
		attachedUnit.TryGetComponent<Airbase>(out var component);
		if (component != null)
		{
			component.AddStorage(this);
		}
	}

	public bool IsFunctional()
	{
		if (!Disabled)
		{
			return base.transform.position.y > Datum.LocalSeaY;
		}
		return false;
	}

	public void Repair()
	{
		if (!(attachedUnit == null))
		{
			attachedUnit.Networkdisabled = false;
			attachedUnit.onDisableUnit += AttachedUnit_OnDisable;
		}
	}

	private void AttachedUnit_OnDisable(Unit unit)
	{
		if (!selfDisabled)
		{
			Disable();
		}
		attachedUnit.onDisableUnit -= AttachedUnit_OnDisable;
	}

	private void Part_OnApplyDamage(UnitPart.OnApplyDamage e)
	{
		if ((e.hitPoints < 0f || e.detached) && !selfDisabled)
		{
			Disable();
		}
	}

	private void Disable()
	{
		selfDisabled = true;
		if (number == 0)
		{
			return;
		}
		Airbase airbase = attachedUnit.GetAirbase();
		if (!(airbase == null))
		{
			if (NetworkSceneSingleton<MessageManager>.i != null)
			{
				NetworkSceneSingleton<MessageManager>.i.RpcWarheadDestroyedMessage(airbase, attachedUnit.NetworkHQ, number);
			}
			Networknumber = 0;
		}
	}

	public void CheckAttachCamera()
	{
		AttachCamera();
	}

	private void AttachCamera()
	{
		SceneSingleton<CameraStateManager>.i.followingUnit = attachedUnit;
		SceneSingleton<CameraStateManager>.i.SwitchState(SceneSingleton<CameraStateManager>.i.relativeState);
		SceneSingleton<CameraStateManager>.i.transform.position = base.transform.position;
		SceneSingleton<CameraStateManager>.i.transform.rotation = base.transform.rotation;
	}

	public Unit GetUnit()
	{
		return attachedUnit;
	}

	public int GetNumber()
	{
		return number;
	}

	public void AddWarhead(int n)
	{
		Networknumber = number + n;
	}

	public void RemoveWarhead(int n)
	{
		Networknumber = number - n;
		if (number < 0)
		{
			Networknumber = 0;
		}
	}

	private void OnDestroy()
	{
		selfDisabled = true;
	}

	private void MirageProcessed()
	{
	}

	public override bool SerializeSyncVars(NetworkWriter writer, bool initialize)
	{
		ulong syncVarDirtyBits = base.SyncVarDirtyBits;
		bool result = base.SerializeSyncVars(writer, initialize);
		if (initialize)
		{
			writer.WritePackedInt32(number);
			return true;
		}
		writer.Write(syncVarDirtyBits, 1);
		if ((syncVarDirtyBits & 1L) != 0L)
		{
			writer.WritePackedInt32(number);
			result = true;
		}
		return result;
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			number = reader.ReadPackedInt32();
			return;
		}
		ulong num = reader.Read(1);
		SetDeserializeMask(num, 0);
		if ((num & 1L) != 0L)
		{
			number = reader.ReadPackedInt32();
		}
	}

	protected override int GetRpcCount()
	{
		return 0;
	}
}
