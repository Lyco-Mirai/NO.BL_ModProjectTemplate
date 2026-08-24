using UnityEngine;

[CreateAssetMenu(fileName = "New Aircraft", menuName = "ScriptableObjects/AircraftDefinition", order = 7)]
public class AircraftDefinition : UnitDefinition
{
	public AircraftParameters aircraftParameters;

	public AircraftInfo aircraftInfo;

	public Vector3 restRotation;
}
