using UnityEngine;

[CreateAssetMenu(fileName = "GraphicsOptions", menuName = "ScriptableObjects/GraphicsOptions", order = 998)]
public class GraphicsHelperSO : ScriptableObject
{
	[Header("Default values")]
	public int _mipmapLevel;

	public int _shadowQuality = 3;

	public bool _vsync;

	public int _fpsLimit = 60;

	public float _cloudDetail = 1f;

	public int _antiAliasing = 1;

	public int _shadowDistance = 2000;

	public bool _softshadows = true;

	public float _lodBias = 2f;

	public int _anisotropicFiltering = 4;

	public int _maxLights = 2;

	[Header("ShadowCascade")]
	public float shortRangeThreshold = 1500f;

	[Tooltip("2 Cascades: Near split percentage")]
	public float cascade2Split = 0.25f;

	[Space]
	[Tooltip("3 Cascades: Near and Mid split percentages")]
	public float midRangeThreshold = 4000f;

	public Vector2 cascade3Split = new Vector2(0.05f, 0.2f);

	[Space]
	[Tooltip("4 Cascades: Near, Mid-1, and Mid-2 split percentages")]
	public Vector3 cascade4Split = new Vector3(0.02f, 0.1f, 0.25f);
}
