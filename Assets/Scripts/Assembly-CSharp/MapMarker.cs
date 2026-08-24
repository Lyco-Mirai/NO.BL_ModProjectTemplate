using UnityEngine;
using UnityEngine.UI;

public abstract class MapMarker : MonoBehaviour
{
	[SerializeField]
	public Image markerImg;

	public Color color = Color.white;

	public float lastRefresh;

	public float refreshDelay = 1f;

	public virtual void Remove()
	{
		if (this != null)
		{
			SceneSingleton<DynamicMap>.i.mapMarkers.Remove(this);
			Object.Destroy(base.gameObject);
		}
	}

	public virtual void DynamicHide()
	{
	}

	public virtual void Mask()
	{
	}

	public virtual void Show(bool value)
	{
	}
}
