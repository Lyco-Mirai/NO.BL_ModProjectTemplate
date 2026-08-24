using UnityEngine;

public class ConstrainToSeaLevel : MonoBehaviour
{
	private Transform xform;

	private void Start()
	{
		xform = base.transform;
	}

	private void Update()
	{
		xform.position = new Vector3(xform.position.x, Datum.LocalSeaY, xform.position.z);
		xform.rotation = Quaternion.identity;
	}
}
