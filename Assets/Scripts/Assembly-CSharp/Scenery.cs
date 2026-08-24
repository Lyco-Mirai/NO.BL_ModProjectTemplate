using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using NuclearOption.Networking;
using UnityEngine;

public class Scenery : Unit
{
	private float collapseTime = 10f;

	[NonSerialized]
	private const int SYNC_VAR_COUNT = 9;

	[NonSerialized]
	private const int RPC_COUNT = 21;

	public new SceneryDefinition definition => (SceneryDefinition)base.definition;

	public override void Awake()
	{
		base.Awake();
		base.Identity.OnStartClient.AddListener(OnStartClient);
		if (NetworkManagerNuclearOption.i != null && NetworkManagerNuclearOption.i.Server.Active)
		{
			SetLocalSim(localSim: true);
			base.NetworkunitName = definition.unitName;
			base.NetworkstartPosition = base.transform.position.ToGlobalPosition();
		}
	}

	private void OnStartClient()
	{
		if (GameManager.gameState != GameState.Encyclopedia)
		{
			base.transform.position = startPosition.ToLocalPosition();
			RegisterUnit(null);
			InitializeUnit();
		}
	}

	public override void UnitDisabled(bool oldState, bool newState)
	{
		base.UnitDisabled(oldState, newState);
		if (GameManager.gameState != GameState.Editor && newState)
		{
			Collapse().Forget();
		}
	}

	private async UniTask Collapse()
	{
		Vector3 velocity = Vector3.zero;
		CancellationToken cancel = base.destroyCancellationToken;
		while (collapseTime > 0f)
		{
			await UniTask.Yield();
			if (cancel.IsCancellationRequested)
			{
				return;
			}
			velocity += Vector3.down * 9.81f * Time.deltaTime;
			collapseTime -= Time.deltaTime;
			base.transform.position += velocity * Time.deltaTime;
		}
		if (NetworkManagerNuclearOption.i.Server.Active)
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
