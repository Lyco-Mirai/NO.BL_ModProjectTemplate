namespace NuclearOption.NetworkTransforms
{
	public interface ISnapshotBuffer
	{
		void RemoveOld(double timestamp);
	}
}
