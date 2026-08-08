using UnityEngine;

[CreateAssetMenu(fileName = "New Missile", menuName = "ScriptableObjects/MissileDefinition", order = 7)]
public class MissileDefinition : UnitDefinition
{
	private new float? mass;

	public float GetMass()
	{
		if (!mass.HasValue)
		{
			mass = unitPrefab.GetComponent<Missile>().GetMass();
		}
		return mass.Value;
	}
}
