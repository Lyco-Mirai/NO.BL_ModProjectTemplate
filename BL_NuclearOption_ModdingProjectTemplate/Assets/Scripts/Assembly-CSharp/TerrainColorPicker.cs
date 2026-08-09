using UnityEngine;

public class TerrainColorPicker : MonoBehaviour
{
	[SerializeField]
	private Color defaultColor;

	[SerializeField]
	private Renderer[] renderers;

	[SerializeField]
	private int[] submeshIndices;

	private void Start()
	{
		Color colorFromRaycast = GetColorFromRaycast();
		for (int i = 0; i < renderers.Length; i++)
		{
			int num = submeshIndices[i];
			renderers[i].materials[num].color = colorFromRaycast;
		}
		Object.Destroy(this, 5f);
	}

	private Color GetColorFromRaycast()
	{
		int layer = base.gameObject.layer;
		Color result = defaultColor;
		base.gameObject.layer = PhysicsLayers.IgnoreRaycast;
		if (NetworkSceneSingleton<LevelInfo>.i != null && NetworkSceneSingleton<LevelInfo>.i.TryGetTerrainColorAtCoordinate(base.transform.GlobalPosition(), out var sampledColor))
		{
			result = sampledColor;
		}
		base.gameObject.layer = layer;
		return result;
	}
}
