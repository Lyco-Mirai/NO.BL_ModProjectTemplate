using System;

namespace NuclearOption.Networking
{
	public class SpectatorPlayer : BasePlayer
	{
		[NonSerialized]
		private const int SYNC_VAR_COUNT = 1;

		[NonSerialized]
		private const int RPC_COUNT = 0;

		private void MirageProcessed()
		{
		}

		protected override int GetRpcCount()
		{
			return 0;
		}
	}
}
