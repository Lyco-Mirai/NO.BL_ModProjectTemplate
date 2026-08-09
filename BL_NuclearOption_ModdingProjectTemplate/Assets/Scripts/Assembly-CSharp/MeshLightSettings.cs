using UnityEngine;

public class MeshLightSettings : MonoBehaviour
{
	[Range(0f, 10f)]
	public float blink_freq;

	[Range(0f, 2f)]
	public float on_off_freq;

	[Range(0f, 1f)]
	public float delay;

	public bool randomized_delay;

	private Material meshLightBlink;

	private void Awake()
	{
		meshLightBlink = GetComponent<MeshRenderer>().material;
		float value = (randomized_delay ? Random.Range(0f, delay) : delay);
		meshLightBlink.SetFloat("_Delay", value);
		meshLightBlink.SetFloat("_Frequency", blink_freq);
		meshLightBlink.SetFloat("_OnOffFrequency", on_off_freq);
		Object.Destroy(this);
	}
}
