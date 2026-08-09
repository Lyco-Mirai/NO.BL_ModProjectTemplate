using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class DecalSpawner : MonoBehaviour
{
	private int layerMask = PhysicsLayers.StaticsMask;

	private RaycastHit hit;

	[SerializeField]
	private float decalSize;

	[SerializeField]
	private Material decalMaterial;

	[SerializeField]
	private float fadeInTime;

	[SerializeField]
	private bool verticalProjection;

	private void Start()
	{
		if (Physics.Linecast(base.transform.position + Vector3.up * decalSize * 0.5f, base.transform.position - Vector3.up * decalSize * 0.5f, out hit, layerMask))
		{
			GameObject gameObject = Object.Instantiate(GameAssets.i.scorchMarkDecal, Datum.origin);
			DecalProjector component = gameObject.GetComponent<DecalProjector>();
			component.size = new Vector3(decalSize, decalSize, decalSize * 0.2f);
			gameObject.transform.rotation = (verticalProjection ? Quaternion.LookRotation(Vector3.up) : Quaternion.LookRotation(-hit.normal));
			gameObject.transform.position = hit.point;
			component.material = decalMaterial;
			SceneSingleton<EffectManager>.i.AddEffect(gameObject);
			if (fadeInTime > 0f)
			{
				DecalFadeIn(component).Forget();
			}
		}
		if (fadeInTime == 0f)
		{
			Object.Destroy(this);
		}
	}

	private async UniTask DecalFadeIn(DecalProjector decalProjector)
	{
		CancellationToken cancel = base.destroyCancellationToken;
		float fadeInRate = 1f / fadeInTime;
		decalProjector.fadeFactor = 0f;
		while (decalProjector.fadeFactor < 1f)
		{
			await UniTask.Yield();
			if (cancel.IsCancellationRequested)
			{
				return;
			}
			decalProjector.fadeFactor += fadeInRate * Time.deltaTime;
		}
		decalProjector.fadeFactor = 1f;
		Object.Destroy(this);
	}
}
