using Cysharp.Threading.Tasks;
using UnityEngine;

public class FixedTimer
{
	private float timer;

	public async UniTask Wait(float addTime)
	{
		timer += addTime;
		timer -= Time.deltaTime;
		while (timer > 0f)
		{
			await UniTask.Yield();
			timer -= Time.deltaTime;
		}
	}
}
