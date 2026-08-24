using UnityEngine;

public static class AudioHelper
{
	public static float LinearToDecibel(float linear)
	{
		if (!(linear > 0f))
		{
			return -80f;
		}
		return 20f * Mathf.Log10(linear);
	}
}
