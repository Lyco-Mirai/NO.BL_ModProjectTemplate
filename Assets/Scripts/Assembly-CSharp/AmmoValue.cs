public readonly struct AmmoValue
{
	public readonly float Current;

	public readonly float Total;

	public float Missing => Total - Current;

	public AmmoValue(float currentValue, float totalValue)
	{
		Current = currentValue;
		Total = totalValue;
	}
}
