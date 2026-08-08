using System.Runtime.InteropServices;

namespace NuclearOption.Effects
{
	[StructLayout(LayoutKind.Sequential, Size = 16)]
	public struct GroupCount
	{
		public uint groupX;

		public uint groupY;

		public uint groupZ;

		public uint total;
	}
}
