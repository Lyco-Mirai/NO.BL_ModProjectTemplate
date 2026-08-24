namespace NuclearOption
{
	public struct OwnedAirframe
	{
		public readonly AircraftDefinition Definition;

		public readonly bool Reserved;

		public OwnedAirframe(AircraftDefinition definition, bool reserved)
		{
			Definition = definition;
			Reserved = reserved;
		}
	}
}
