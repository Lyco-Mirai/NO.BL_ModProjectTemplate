using System;
using System.Collections.Generic;
using System.Text;

namespace NuclearOption.SavedMission
{
	public class LoadErrors
	{
		public readonly List<string> Warnings = new List<string>();

		public readonly List<string> Errors = new List<string>();

		public readonly List<ExceptionEntry> Exceptions = new List<ExceptionEntry>();

		public int ErrorAndExceptionsCount => Errors.Count + Exceptions.Count;

		public void AddException(Exception e, string message = null)
		{
			Exceptions.Add(new ExceptionEntry(e, message));
		}

		public void AddError(string message)
		{
			Warnings.Add(message);
		}

		public void AddWarn(string message)
		{
			Warnings.Add(message);
		}

		public void LogAllErrors(string missionName = null)
		{
			string arg = (string.IsNullOrEmpty(missionName) ? "" : ("'" + missionName + "' "));
			if (Warnings.Count > 0)
			{
				ColorLog<LoadErrors>.InfoWarn($"{arg}{Warnings.Count} load warnings");
				foreach (string warning in Warnings)
				{
					ColorLog<LoadErrors>.InfoWarn(warning);
				}
			}
			if (Errors.Count > 0)
			{
				ColorLog<LoadErrors>.LogError($"{arg}{Errors.Count} load errors");
				foreach (string error in Errors)
				{
					ColorLog<LoadErrors>.LogError(error);
				}
			}
			if (Exceptions.Count <= 0)
			{
				return;
			}
			ColorLog<LoadErrors>.LogError($"{arg}{Exceptions.Count} load exceptions");
			foreach (ExceptionEntry exception in Exceptions)
			{
				if (!string.IsNullOrEmpty(exception.Message))
				{
					ColorLog<LoadErrors>.LogError($"{exception.Message}: {exception.Exception}");
				}
				else
				{
					ColorLog<LoadErrors>.LogError(exception.Exception.ToString());
				}
			}
		}

		public string CreateErrorSummary(string missionName)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("Mission ");
			stringBuilder.Append(missionName);
			if (ErrorAndExceptionsCount > 0)
			{
				stringBuilder.Append(" failed to load because of error(s)");
			}
			else
			{
				stringBuilder.Append(" loaded with warning(s)");
			}
			if (Warnings.Count > 0)
			{
				stringBuilder.Append($"\nWarnings: {Warnings.Count}");
			}
			if (Errors.Count > 0)
			{
				stringBuilder.Append($"\nErrors: {Errors.Count}");
			}
			if (Exceptions.Count > 0)
			{
				stringBuilder.Append($"\nExceptions: {Exceptions.Count}");
			}
			return stringBuilder.ToString();
		}

		public string CreateDetailedList(string missionName)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("Mission ");
			stringBuilder.Append(missionName);
			if (ErrorAndExceptionsCount > 0)
			{
				stringBuilder.Append(" failed to load because of error(s)");
			}
			else
			{
				stringBuilder.Append(" loaded with warning(s)");
			}
			if (Warnings.Count > 0)
			{
				stringBuilder.Append($"\nWarnings: {Warnings.Count}");
			}
			if (Errors.Count > 0)
			{
				stringBuilder.Append($"\nErrors: {Errors.Count}");
			}
			if (Exceptions.Count > 0)
			{
				stringBuilder.Append($"\nExceptions: {Exceptions.Count}");
			}
			if (Warnings.Count > 0)
			{
				stringBuilder.Append("\n\n**Warnings**");
				foreach (string warning in Warnings)
				{
					stringBuilder.Append("\n- " + warning);
				}
			}
			if (Errors.Count > 0)
			{
				stringBuilder.Append("\n\n**Errors**");
				foreach (string error in Errors)
				{
					stringBuilder.Append("\n- " + error);
				}
			}
			if (Exceptions.Count > 0)
			{
				stringBuilder.Append("\n\n**Exceptions**");
				foreach (ExceptionEntry exception in Exceptions)
				{
					stringBuilder.Append($"\n- {exception}");
				}
			}
			return stringBuilder.ToString();
		}

		public bool AnyMessages()
		{
			if (Warnings.Count <= 0 && Errors.Count <= 0)
			{
				return Exceptions.Count > 0;
			}
			return true;
		}
	}
}
