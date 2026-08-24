using UnityEngine;

public class AnimatedPhysicsSurface : MonoBehaviour
{
	private Vector3 animationVelocity;

	public void SetAnimationVelocity(Vector3 animationVelocity)
	{
		this.animationVelocity = animationVelocity;
	}

	public Vector3 GetVelocity()
	{
		return animationVelocity;
	}
}
