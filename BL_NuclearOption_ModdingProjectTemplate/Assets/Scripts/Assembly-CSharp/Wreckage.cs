using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class Wreckage : MonoBehaviour
{
	private Rigidbody debrisRB;

	[SerializeField]
	private float mass = 7000f;

	private void Start()
	{
		NetworkSceneSingleton<MissionManager>.i.listWrecks.Add(this);
		if (!(NetworkSceneSingleton<MissionManager>.i.wrecksDecayTime > 0f))
		{
			return;
		}
		UniTask.Delay((int)(1f + NetworkSceneSingleton<MissionManager>.i.wrecksDecayTime * 60f) * 1000).ContinueWith(delegate
		{
			if (this != null)
			{
				Disintegrate();
			}
		}).Forget();
	}

	private void OnCollisionEnter(Collision collision)
	{
		if (debrisRB == null)
		{
			Vector3 velocity = new Vector3(Mathf.Clamp(collision.relativeVelocity.x * 0.5f, -10f, 10f), Mathf.Clamp(collision.relativeVelocity.y * 0.5f, -10f, 10f), Mathf.Clamp(collision.relativeVelocity.z * 0.5f, -10f, 10f));
			if (collision.body != null && collision.body is Rigidbody rigidbody)
			{
				rigidbody.velocity = velocity;
			}
			mass -= 500f;
			debrisRB = base.gameObject.AddComponent<Rigidbody>();
			debrisRB.mass = Mathf.Max(mass, 500f);
			debrisRB.velocity = velocity;
			WaitSleepPhysics().Forget();
			if (mass < 500f)
			{
				Disintegrate();
			}
		}
	}

	private async UniTask WaitSleepPhysics()
	{
		CancellationToken cancel = base.destroyCancellationToken;
		await UniTask.WaitForSeconds(5);
		while (debrisRB != null && !cancel.IsCancellationRequested)
		{
			if (debrisRB.velocity.sqrMagnitude < 1f || debrisRB.position.z < Datum.LocalSeaY)
			{
				Object.Destroy(debrisRB);
			}
			await UniTask.WaitForSeconds(5);
		}
	}

	public void Disintegrate()
	{
		Collider[] components = GetComponents<Collider>();
		for (int i = 0; i < components.Length; i++)
		{
			Object.Destroy(components[i]);
		}
		Object.Destroy(Object.Instantiate(GameAssets.i.vehicleWreckDestroyed, base.transform.position + Vector3.up, base.transform.rotation), 10f);
		base.transform.position += new Vector3(0f, -1f, 0f);
		base.transform.eulerAngles += new Vector3(Random.Range(15f, -15f), 0f, Random.Range(30f, -30f));
		if (!BattlefieldGrid.TryGetGridSquare(base.transform.GlobalPosition(), out var gridSquare))
		{
			return;
		}
		for (int j = 0; j < gridSquare.obstacles.Count; j++)
		{
			if (gridSquare.obstacles[j].Transform == base.transform)
			{
				gridSquare.obstacles.RemoveAt(j);
				break;
			}
		}
		NetworkSceneSingleton<MissionManager>.i.listWrecks.Remove(this);
		Object.Destroy(base.gameObject, 1f);
	}
}
