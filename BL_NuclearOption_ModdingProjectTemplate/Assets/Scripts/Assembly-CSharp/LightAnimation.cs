using UnityEngine;

public class LightAnimation : MonoBehaviour
{
	[SerializeField]
	private float lightIntensity;

	[SerializeField]
	private Light animatedLight;

	[SerializeField]
	private AnimationCurve intensityCurve;

	[SerializeField]
	private Gradient colorAnimation;

	[SerializeField]
	private Transform lightAnchor;

	[SerializeField]
	private float verticalOffset;

	private float timeSinceSpawn;

	private void OnEnable()
	{
		if (lightIntensity > 10000f)
		{
			ExposureController.RegisterBrightLight(animatedLight);
		}
	}

	private void Update()
	{
		animatedLight.intensity = lightIntensity * intensityCurve.Evaluate(timeSinceSpawn);
		animatedLight.color = colorAnimation.Evaluate(timeSinceSpawn);
		if (lightAnchor != null)
		{
			animatedLight.gameObject.transform.position = lightAnchor.position + Vector3.up * verticalOffset;
		}
		timeSinceSpawn += Time.deltaTime;
		if (animatedLight.intensity <= 0f)
		{
			Object.Destroy(animatedLight.gameObject);
			Object.Destroy(this);
		}
	}
}
