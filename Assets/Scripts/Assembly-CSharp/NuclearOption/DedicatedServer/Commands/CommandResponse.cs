using System;
using System.Text;
using UnityEngine;

namespace NuclearOption.DedicatedServer.Commands
{
	public struct CommandResponse
	{
		[Serializable]
		public struct MessageResponse
		{
			public string message;
		}

		public StatusCode StatusCode;

		public byte[] Body;

		public void SetBody<T>(T body)
		{
			string s = JsonUtility.ToJson(body);
			Body = Encoding.UTF8.GetBytes(s);
		}

		public static CommandResponse Create(StatusCode statusCode)
		{
			return new CommandResponse
			{
				StatusCode = statusCode
			};
		}

		public static CommandResponse Create(StatusCode statusCode, string message)
		{
			return Create(statusCode, new MessageResponse
			{
				message = message
			});
		}

		public static CommandResponse Create<T>(StatusCode statusCode, T body) where T : struct
		{
			CommandResponse result = default(CommandResponse);
			result.StatusCode = statusCode;
			result.SetBody(body);
			return result;
		}
	}
}
