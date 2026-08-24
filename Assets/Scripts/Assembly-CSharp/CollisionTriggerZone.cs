using System;
using UnityEngine;

public class CollisionTriggerZone : MonoBehaviour
{
	public event Action OnTriggerEntered;

	private void OnTriggerEnter(Collider other)
	{
		this.OnTriggerEntered?.Invoke();
	}
}
