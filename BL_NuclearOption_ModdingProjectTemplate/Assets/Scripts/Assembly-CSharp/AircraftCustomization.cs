using NuclearOption.SavedMission;

public class AircraftCustomization
{
	public Loadout loadout;

	public float fuelLevel;

	public int livery;

	public AircraftCustomization(Loadout loadout, float fuelLevel, int livery)
	{
		this.loadout = loadout;
		this.fuelLevel = fuelLevel;
		this.livery = livery;
	}

	public void Update(Loadout loadout, float fuelLevel, int livery)
	{
		this.loadout = loadout;
		this.fuelLevel = fuelLevel;
		this.livery = livery;
	}
}
