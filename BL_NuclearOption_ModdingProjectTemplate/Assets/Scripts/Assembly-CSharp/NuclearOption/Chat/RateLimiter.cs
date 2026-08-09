using System.Collections.Generic;

namespace NuclearOption.Chat
{
	public class RateLimiter
	{
		private readonly int limit;

		private readonly float timeSpan;

		private readonly Queue<float> messages = new Queue<float>();

		public RateLimiter(int limit, float timeSpan)
		{
			this.limit = limit;
			this.timeSpan = timeSpan;
		}

		public void OnSend(float now)
		{
			messages.Enqueue(now);
		}

		public bool ShouldLimit(float now)
		{
			while (messages.Count > 0 && messages.Peek() < now - timeSpan)
			{
				messages.Dequeue();
			}
			return messages.Count >= limit;
		}
	}
}
