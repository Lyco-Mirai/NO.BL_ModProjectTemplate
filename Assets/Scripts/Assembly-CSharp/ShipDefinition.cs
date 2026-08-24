using UnityEngine;

[CreateAssetMenu(fileName = "New Ship", menuName = "ScriptableObjects/ShipDefinition", order = 8)]
public class ShipDefinition : UnitDefinition
{
	public ShipInfo shipInfo;

	public ShipType shipType;
}
