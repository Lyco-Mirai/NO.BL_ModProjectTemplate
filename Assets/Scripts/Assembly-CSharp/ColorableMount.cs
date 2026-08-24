using UnityEngine;

public class ColorableMount : MonoBehaviour
{
	[SerializeField]
	protected Renderer[] colorableRenderers;

	[SerializeField]
	protected Renderer[] skinnableRenderers;

	public void AttachToAircraft(Aircraft aircraft)
	{
		Renderer[] array = colorableRenderers;
		foreach (Renderer renderer in array)
		{
			aircraft.weaponManager.RegisterColorable(renderer);
		}
		array = skinnableRenderers;
		foreach (Renderer renderer2 in array)
		{
			aircraft.weaponManager.RegisterSkinnable(renderer2);
		}
		Object.Destroy(this);
	}
}
