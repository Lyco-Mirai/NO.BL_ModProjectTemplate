using UnityEngine;

public static class MaterialHelper
{
	public static Material CloneMaterial(Renderer renderer)
	{
		return renderer.sharedMaterial = Object.Instantiate(renderer.sharedMaterial);
	}
}
