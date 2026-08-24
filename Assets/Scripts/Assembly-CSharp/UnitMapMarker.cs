public abstract class UnitMapMarker : MapMarker
{
	public UnitMapIcon Icon { get; private set; }

	public Unit GetUnit()
	{
		if (!(Icon != null))
		{
			return null;
		}
		return Icon.unit;
	}

	protected abstract void ExtraSetup();

	public void Setup(UnitMapIcon icon)
	{
		Icon = icon;
		ExtraSetup();
	}
}
