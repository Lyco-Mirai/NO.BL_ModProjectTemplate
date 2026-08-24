using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Mirage;
using Mirage.Serialization;
using NuclearOption.DebugScripts;
using NuclearOption.Networking;
using Unity.Profiling;
using UnityEngine;

public class Capture : NetworkBehaviour
{
	private struct CapturingUnits
	{
		public float Strength;

		public float Defense;
	}

	private static readonly ProfilerMarker captureMarker = new ProfilerMarker("Airbase.Capture");

	private static readonly ProfilerMarker unitsMarker = new ProfilerMarker("Airbase.Capture.UpdateUnits");

	private static readonly ProfilerMarker changeMarker = new ProfilerMarker("Airbase.Capture.GetChange");

	private static readonly ProfilerMarker debugVisMarker = new ProfilerMarker("Airbase.Capture.Debug");

	[Tooltip("Drop in control if capturing and no units in range (and no faction in control). Effected by airbase defense")]
	[SerializeField]
	private float passiveDrop = 1f;

	[Tooltip("What balance capturing starts at when going from neutral to capturing")]
	[SerializeField]
	private float startingCapture = 0.1f;

	[Tooltip("How often to update balance. Numbers are still per second, so larger interval means bigger jumps in balance")]
	[SerializeField]
	private float checkInterval = 1f;

	private readonly Dictionary<FactionHQ, CapturingUnits> capturingFactions = new Dictionary<FactionHQ, CapturingUnits>();

	private ICapturable target;

	private float lastCaptureCheck;

	[CompilerGenerated]
	[SyncVar]
	private float _003CcontrolBalance_003Ek__BackingField;

	private bool capturable;

	private DebugText debugText;

	private static StringBuilder debugBuilder = new StringBuilder();

	protected Dictionary<PersistentID, float> captureCredit;

	[NonSerialized]
	private const int SYNC_VAR_COUNT = 1;

	[NonSerialized]
	private const int RPC_COUNT = 0;

	public float controlBalance
	{
		[CompilerGenerated]
		get
		{
			return _003CcontrolBalance_003Ek__BackingField;
		}
		[CompilerGenerated]
		private set
		{
			Network_003CcontrolBalance_003Ek__BackingField = value;
		}
	}

	public FactionHQ capturingHQ { get; private set; }

	public float Network_003CcontrolBalance_003Ek__BackingField
	{
		get
		{
			return controlBalance;
		}
		set
		{
			if (!SyncVarEqual(value, controlBalance))
			{
				float num = controlBalance;
				controlBalance = value;
				SetDirtyBit(1uL);
			}
		}
	}

	private void Awake()
	{
		target = GetComponent<ICapturable>();
		if (target == null)
		{
			Debug.LogError("Capture could not find ICapturable on " + base.name);
		}
	}

	public void ForceCapture(FactionHQ hq)
	{
		Network_003CcontrolBalance_003Ek__BackingField = 1f;
		target.OnCapture(hq);
		capturingHQ = null;
		ReportTakingControl();
	}

	public void SetCapturable(bool capturable)
	{
		this.capturable = capturable;
		Network_003CcontrolBalance_003Ek__BackingField = ((target.CurrentHQ != null) ? 1 : 0);
	}

	public void Update()
	{
		if (GameManager.gameState == GameState.Editor || !NetworkManagerNuclearOption.i.Server.Active || target.gridSquares == null)
		{
			return;
		}
		if (Time.realtimeSinceStartup - lastCaptureCheck > checkInterval)
		{
			float num = ((lastCaptureCheck == 0f) ? checkInterval : (Time.realtimeSinceStartup - lastCaptureCheck));
			lastCaptureCheck = Time.realtimeSinceStartup;
			if (capturable && !target.disabled)
			{
				if (Time.timeSinceLevelLoad < 10f)
				{
					num *= 100f;
				}
				CheckForCapture(num);
			}
		}
		if (PlayerSettings.debugVis)
		{
			debugVis();
		}
	}

	private void debugVis()
	{
		using (debugVisMarker.Auto())
		{
			if (debugText == null)
			{
				debugText = UnityEngine.Object.Instantiate(GameAssets.i.debugText, target.center.transform);
			}
			string text = ((target.CurrentHQ != null) ? (target.CurrentHQ.faction.factionName + " controlled").AddColor(target.CurrentHQ.faction.color) : ((!(capturingHQ != null)) ? "no faction".AddColor(Color.white * 0.8f) : (capturingHQ.faction.factionName + " capturing").AddColor(capturingHQ.faction.color)));
			string text2 = (target.disabled ? "Disabled" : (capturable ? MissionManagerDebugGui.CreateProgressBar(debugBuilder, controlBalance) : "Not Capturable"));
			debugText.Text.text = text + "\n" + text2;
		}
	}

	private void CheckForCapture(float timeDelta)
	{
		using (captureMarker.Auto())
		{
			GetInRangeUnits();
			FactionHQ highestHQ;
			float change = GetChange(out highestHQ) * timeDelta;
			ApplyChange(change, highestHQ);
		}
	}

	private void GetInRangeUnits()
	{
		using (unitsMarker.Auto())
		{
			capturingFactions.Clear();
			float captureRange = target.CaptureRange;
			Vector3 position = target.center.position;
			bool flag = true;
			foreach (GridSquare gridSquare in target.gridSquares)
			{
				foreach (Unit unit in gridSquare.units)
				{
					FactionHQ networkHQ = unit.NetworkHQ;
					if (unit == null || unit.disabled || networkHQ == null || FastMath.OutOfRange(unit.transform.position, position, captureRange))
					{
						continue;
					}
					float num = unit.CaptureStrength;
					float num2 = unit.CaptureDefense;
					if (target.CurrentHQ != unit.NetworkHQ)
					{
						RecordCapture(unit.persistentID, unit.CaptureStrength);
					}
					if (num < 0f)
					{
						num = 0f;
					}
					if (num2 < 0f)
					{
						num2 = 0f;
					}
					if (num != 0f || num2 != 0f)
					{
						if (num != 0f)
						{
							flag = false;
						}
						capturingFactions.TryGetValue(networkHQ, out var value);
						value.Strength += num;
						value.Defense += num2;
						capturingFactions[networkHQ] = value;
					}
				}
			}
			if (flag)
			{
				capturingFactions.Clear();
			}
		}
	}

	private float GetChange(out FactionHQ highestHQ)
	{
		using (changeMarker.Auto())
		{
			if (capturingFactions.Count == 0)
			{
				highestHQ = null;
				if (capturingHQ != null)
				{
					return passiveDrop / target.CaptureDefense;
				}
				return 0f;
			}
			highestHQ = null;
			float num = 0f;
			float num2 = 0f;
			foreach (KeyValuePair<FactionHQ, CapturingUnits> capturingFaction in capturingFactions)
			{
				FactionHQ key = capturingFaction.Key;
				CapturingUnits value = capturingFaction.Value;
				if (target.CurrentHQ != null)
				{
					if (target.CurrentHQ == key)
					{
						num = value.Strength;
					}
					else
					{
						num2 += value.Strength;
					}
				}
				else if (capturingHQ != null)
				{
					if (capturingHQ == key)
					{
						num = value.Strength;
					}
					else
					{
						num2 += value.Strength;
					}
				}
				else if (value.Strength > num)
				{
					num2 += num;
					highestHQ = capturingFaction.Key;
					num = value.Strength;
				}
				else
				{
					num2 += value.Strength;
				}
			}
			float num3 = num - num2;
			float num4 = 0f;
			if (target.CurrentHQ != null)
			{
				capturingFactions.TryGetValue(target.CurrentHQ, out var value2);
				num4 += value2.Defense;
			}
			else
			{
				foreach (Unit defenseUnit in target.GetDefenseUnits())
				{
					if (!(defenseUnit == null) && !defenseUnit.disabled)
					{
						num4 += defenseUnit.CaptureDefense;
					}
				}
			}
			float num5 = target.CaptureDefense + num4;
			float result = num3 / num5;
			if (!(controlBalance < 1f))
			{
				_ = 0f;
			}
			return result;
		}
	}

	private void ApplyChange(float change, FactionHQ highestHQ)
	{
		float num = controlBalance + change;
		if (num > 1f)
		{
			num = 1f;
		}
		if (num == controlBalance)
		{
			return;
		}
		Network_003CcontrolBalance_003Ek__BackingField = num;
		if (change > 0f)
		{
			LogChange("increasing control", change);
		}
		else
		{
			LogChange("decreasing control", change);
		}
		if (target.CurrentHQ != null)
		{
			if (controlBalance <= 0f)
			{
				Network_003CcontrolBalance_003Ek__BackingField = 0f;
				target.OnCapture(null);
			}
		}
		else if (capturingHQ != null)
		{
			if (controlBalance >= 1f)
			{
				ForceCapture(capturingHQ);
			}
			else if (controlBalance <= 0f)
			{
				LogChange("stop capture", change);
				Network_003CcontrolBalance_003Ek__BackingField = 0f;
				capturingHQ = null;
			}
		}
		else if (change > 0f)
		{
			capturingHQ = highestHQ;
			Network_003CcontrolBalance_003Ek__BackingField = startingCapture;
			LogChange("start capture", change);
		}
		static void LogChange(string msg, float num2)
		{
		}
	}

	public void RecordCapture(PersistentID lastCapturedBy, float captureAmount)
	{
		if (captureCredit == null)
		{
			captureCredit = new Dictionary<PersistentID, float>();
		}
		captureCredit.TryGetValue(lastCapturedBy, out var value);
		captureCredit[lastCapturedBy] = value + captureAmount;
	}

	public virtual void ReportTakingControl()
	{
		if (!(target is Airbase airbase) || captureCredit == null)
		{
			return;
		}
		float num = 0f;
		float num2 = 0f;
		for (int i = 0; i < airbase.buildings.Count; i++)
		{
			if (!airbase.buildings[i].disabled)
			{
				num2 += airbase.buildings[i].definition.value;
				if (airbase.buildings[i].GetComponent<WarheadStorage>() != null)
				{
					num2 += 28f * (float)airbase.buildings[i].GetComponent<WarheadStorage>().number;
				}
			}
		}
		num2 = 25f + 2f * Mathf.Sqrt(num2);
		foreach (KeyValuePair<PersistentID, float> item in captureCredit)
		{
			if (UnitRegistry.TryGetPersistentUnit(item.Key, out var persistentUnit) && !(persistentUnit.GetHQ() != airbase.CurrentHQ))
			{
				num += item.Value;
			}
		}
		Debug.Log($"AIRBASE {airbase.SavedAirbase.DisplayName} TOTAL CAPTURE {num}");
		Dictionary<Player, float> dictionary = new Dictionary<Player, float>();
		foreach (KeyValuePair<PersistentID, float> item2 in captureCredit)
		{
			if (!UnitRegistry.TryGetPersistentUnit(item2.Key, out var persistentUnit2))
			{
				continue;
			}
			float num3 = item2.Value / num;
			if (num3 < 0.01f)
			{
				continue;
			}
			FactionHQ hQ = persistentUnit2.GetHQ();
			if (hQ != airbase.CurrentHQ)
			{
				continue;
			}
			float num4 = num2 * num3;
			float num5 = num4 * hQ.killReward;
			hQ.AddScore(num4);
			hQ.AddFunds(num5 * hQ.playerTaxRate);
			if (persistentUnit2.player != null)
			{
				if (!dictionary.ContainsKey(persistentUnit2.player))
				{
					dictionary.Add(persistentUnit2.player, 0f);
				}
				dictionary[persistentUnit2.player] += num3;
			}
		}
		foreach (KeyValuePair<Player, float> item3 in dictionary)
		{
			//item3.Key.HQ.ReportCaptureLocationAction(item3.Key, airbase, item3.Value * num2);
		}
		captureCredit = null;
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
			writer.WriteSingleConverter(controlBalance);
			return true;
		}
		writer.Write(syncVarDirtyBits, 1);
		if ((syncVarDirtyBits & 1L) != 0L)
		{
			writer.WriteSingleConverter(controlBalance);
			result = true;
		}
		return result;
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			controlBalance = reader.ReadSingleConverter();
			return;
		}
		ulong num = reader.Read(1);
		SetDeserializeMask(num, 0);
		if ((num & 1L) != 0L)
		{
			controlBalance = reader.ReadSingleConverter();
		}
	}

	protected override int GetRpcCount()
	{
		return 0;
	}
}
