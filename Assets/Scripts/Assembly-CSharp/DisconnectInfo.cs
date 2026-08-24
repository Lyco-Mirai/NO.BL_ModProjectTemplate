public class DisconnectInfo
{
	public string Message;

	public bool ShowReason;

	public DisconnectInfo(string message)
	{
		Message = message;
		ShowReason = true;
	}

	public static DisconnectInfo NoReason()
	{
		return new DisconnectInfo("")
		{
			ShowReason = false
		};
	}

	public void Merge(DisconnectInfo other)
	{
		Message = "\n" + other.Message;
	}
}
