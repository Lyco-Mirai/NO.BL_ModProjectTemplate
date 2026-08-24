namespace NuclearOption.NetworkTransforms
{
	public struct SnapshotTelemetry
	{
		public GlobalPosition globalPos;

		public double clientLocalTime;

		public double serverLocalTime;

		public float rawRTT;

		public float instantSpeed;

		public float averageSpeed;

		public RejectMask rejectMask;
	}
}
