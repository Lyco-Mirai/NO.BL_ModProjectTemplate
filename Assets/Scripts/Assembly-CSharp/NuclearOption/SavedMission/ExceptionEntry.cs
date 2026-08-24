using System;

namespace NuclearOption.SavedMission
{
	public readonly struct ExceptionEntry
	{
		public readonly string Message;

		public readonly Exception Exception;

		public ExceptionEntry(Exception exception, string message = null)
		{
			Exception = exception;
			Message = message;
		}

		public override string ToString()
		{
			if (string.IsNullOrEmpty(Message))
			{
				return Exception?.ToString() ?? "";
			}
			return $"{Message}\n{Exception}";
		}
	}
}
