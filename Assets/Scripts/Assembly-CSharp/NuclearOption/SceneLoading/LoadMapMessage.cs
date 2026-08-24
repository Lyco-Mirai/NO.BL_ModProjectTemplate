using Mirage;

namespace NuclearOption.SceneLoading
{
	[NetworkMessage]
	public struct LoadMapMessage
	{
		public MapKey Key;
	}
}
