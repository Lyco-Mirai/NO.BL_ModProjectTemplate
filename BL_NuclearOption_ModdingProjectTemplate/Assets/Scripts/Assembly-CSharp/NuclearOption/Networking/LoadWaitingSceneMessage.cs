using System.Runtime.InteropServices;
using Mirage;

namespace NuclearOption.Networking
{
	[StructLayout(LayoutKind.Sequential, Size = 1)]
	[NetworkMessage]
	public struct LoadWaitingSceneMessage
	{
	}
}
