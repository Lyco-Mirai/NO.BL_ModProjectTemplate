using System;

namespace NuclearOption.ModScripts
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
	public class CopyToModProjectAttribute : Attribute
	{
		public bool CopyCreateAssetMenu = true;
	}
}
