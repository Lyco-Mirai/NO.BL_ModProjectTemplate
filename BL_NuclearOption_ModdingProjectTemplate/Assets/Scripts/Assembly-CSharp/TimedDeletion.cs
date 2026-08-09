using Cysharp.Threading.Tasks;
using UnityEngine;

public class TimedDeletion : MonoBehaviour
{
	[SerializeField]
	private float deleteDelay;

	private void Start()
	{
		DeleteTime().Forget();
	}

	private async UniTask DeleteTime()
	{
		await UniTask.Delay((int)(deleteDelay * 1000f));
		if (!(this == null))
		{
			Object.Destroy(base.gameObject);
		}
	}
}
