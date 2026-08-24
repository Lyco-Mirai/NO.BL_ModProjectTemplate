using System;
using System.Collections.Generic;
using NuclearOption.SavedMission;
using UnityEngine;

public class MissionRunner
{
	private readonly MissionObjectives objectives;

	public readonly List<Objective> ActiveObjectives = new List<Objective>();

	public readonly Dictionary<FactionHQ, List<Objective>> activeByFaction = new Dictionary<FactionHQ, List<Objective>>();

	private readonly List<Objective> completeTemp = new List<Objective>();

	public event Action<Objective> OnObjectiveStart;

	public event Action<Objective> OnObjectiveCompleted;

	public MissionRunner(MissionObjectives objectives)
	{
		this.objectives = objectives;
	}

	public void OnMissionStart()
	{
		StartObjective(objectives.StartObjective);
	}

	public void Update()
	{
		completeTemp.Clear();
		foreach (Objective activeObjective in ActiveObjectives)
		{
			if (activeObjective.UpdateAndCheck())
			{
				completeTemp.Add(activeObjective);
			}
		}
		foreach (Objective item in completeTemp)
		{
			CompleteObjective(item);
		}
	}

	public void ClientOnlyUpdate()
	{
		foreach (Objective activeObjective in ActiveObjectives)
		{
			activeObjective.ClientOnlyUpdate();
		}
	}

	internal void StartObjective(Objective obj, bool addToActive = true)
	{
		if (obj.Status == ObjectiveStatus.NotStarted)
		{
			obj.Status = ObjectiveStatus.Running;
			if (addToActive)
			{
				AddActiveObjective(obj);
			}
			this.OnObjectiveStart?.Invoke(obj);
		}
	}

	public void StopObjective(Objective obj)
	{
		if (obj.Status != ObjectiveStatus.Complete)
		{
			obj.Status = ObjectiveStatus.Complete;
			RemoveActiveObjective(obj);
		}
	}

	public void CompleteObjective(Objective obj)
	{
		if (obj.Status != ObjectiveStatus.Complete)
		{
			obj.Status = ObjectiveStatus.Complete;
			obj.Complete();
			obj.Cleanup();
			RemoveActiveObjective(obj);
			this.OnObjectiveCompleted?.Invoke(obj);
		}
	}

	private void AddActiveObjective(Objective obj)
	{
		FactionHQ factionHQ = obj.FactionHQ;
		if (obj.NeedsFaction && factionHQ == null)
		{
			Debug.LogError($"{obj.SavedObjective.ObjectiveTypeEnum} needs a faction");
			return;
		}
		ActiveObjectives.Add(obj);
		if (factionHQ != null)
		{
			if (!activeByFaction.TryGetValue(factionHQ, out var value))
			{
				value = new List<Objective>();
				activeByFaction[factionHQ] = value;
			}
			value.Add(obj);
		}
		obj.OnStart();
	}

	private void RemoveActiveObjective(Objective obj)
	{
		ActiveObjectives.Remove(obj);
		if (obj.FactionHQ != null)
		{
			activeByFaction[obj.FactionHQ].Remove(obj);
		}
	}

	public void SetAllRemoteActiveObjectives(IReadOnlyList<string> names, IReadOnlyDictionary<string, List<int>> dataLookup)
	{
		ActiveObjectives.Clear();
		activeByFaction.Clear();
		foreach (string name in names)
		{
			Objective objective = objectives.GetObjective(name);
			objective.Status = ObjectiveStatus.Running;
			AddActiveObjective(objective);
			if (dataLookup.TryGetValue(name, out var value))
			{
				objective.ReceiveNetworkData(value);
			}
		}
	}

	public void ClearRemoteActiveObjectives()
	{
		ActiveObjectives.Clear();
		activeByFaction.Clear();
	}

	public Objective AddRemoteActiveObjective(string name)
	{
		Objective objective = objectives.GetObjective(name);
		objective.Status = ObjectiveStatus.Running;
		AddActiveObjective(objective);
		return objective;
	}

	public Objective RemoveRemoteActiveObjective(string name)
	{
		Objective objective = objectives.GetObjective(name);
		objective.Status = ObjectiveStatus.Complete;
		RemoveActiveObjective(objective);
		return objective;
	}

	public void Cleanup()
	{
		foreach (Objective activeObjective in ActiveObjectives)
		{
			activeObjective.Cleanup();
		}
	}
}
