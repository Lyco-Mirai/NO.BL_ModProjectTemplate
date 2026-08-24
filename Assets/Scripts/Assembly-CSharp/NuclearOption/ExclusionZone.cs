namespace NuclearOption
{
	public readonly struct ExclusionZone
	{
		public readonly PersistentID sourceId;

		public readonly GlobalPosition position;

		public readonly float radius;

		public ExclusionZone(Unit unit, GlobalPosition position, float radius)
		{
			sourceId = unit.persistentID;
			this.position = position;
			this.radius = radius;
		}
	}
}
