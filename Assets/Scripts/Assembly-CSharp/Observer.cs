using System;
using NuclearOption.Networking;
using UnityEngine;

public class Observer : Unit
{
	[SerializeField]
	private float lifetime;

	private float spawnTime;

	[NonSerialized]
	private const int SYNC_VAR_COUNT = 9;

	[NonSerialized]
	private const int RPC_COUNT = 21;

	private new void Awake()
	{
		base.Identity.OnStartClient.AddListener(OnStartClient);
	}

	private void OnStartClient()
	{
		spawnTime = Time.timeSinceLevelLoad;
	}

	private void Update()
	{
		if (NetworkManagerNuclearOption.i.Server.Active && Time.timeSinceLevelLoad - spawnTime > lifetime)
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	private void MirageProcessed()
	{
	}

	protected override int GetRpcCount()
	{
		return 21;
	}
}
