using JamesFrowen;
using UnityEngine;

[NestedObjectProperties(CreateSelf = true, DrawScriptField = false, IndentLevel = 2)]
public class WeatherSet : ScriptableObject
{
	public string displayName;

	public float coverage;

	public Texture2D mask;

	public Texture2D particleSampler;

	public Texture2D[] cookies;

	public bool lightning;
}
