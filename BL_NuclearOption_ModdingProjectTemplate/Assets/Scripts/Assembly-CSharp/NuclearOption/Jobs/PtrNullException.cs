using System;

namespace NuclearOption.Jobs
{
	public class PtrNullException<T> : Exception
	{
		public static void Throw()
		{
			throw new PtrNullException<T>();
		}
	}
}
