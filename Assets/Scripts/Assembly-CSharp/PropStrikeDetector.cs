using System;
using UnityEngine;

public class PropStrikeDetector : MonoBehaviour
{
	[SerializeField]
	private float currentRadius;

	[SerializeField]
	private Collider propCollider;

	public event Action<float> OnStrike;

	private void OnTriggerStay(Collider other)
	{
		Transform transform = other.transform;
		if (Physics.ComputePenetration(propCollider, base.transform.position, base.transform.rotation, other, transform.position, transform.rotation, out var _, out var distance))
		{
			this.OnStrike?.Invoke(distance);
			if (distance < currentRadius)
			{
				currentRadius = distance;
				base.transform.localScale = Vector3.one * distance / currentRadius;
			}
		}
	}
}
