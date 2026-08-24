using UnityEngine;

public static class LayerHelper
{
	public static void SetLayerRecursively(GameObject target, Component layerObject)
	{
		SetLayerRecursively(target, layerObject.gameObject.layer);
	}

	public static void SetLayerRecursively(GameObject target, GameObject layerObject)
	{
		SetLayerRecursively(target, layerObject.layer);
	}

	public static void SetLayerRecursively(GameObject target, int layer)
	{
		Transform[] componentsInChildren = target.GetComponentsInChildren<Transform>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].gameObject.layer = layer;
		}
	}
}
