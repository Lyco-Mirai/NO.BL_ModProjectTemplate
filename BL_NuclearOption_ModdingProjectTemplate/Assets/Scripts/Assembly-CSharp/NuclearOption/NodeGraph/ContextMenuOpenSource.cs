namespace NuclearOption.NodeGraph
{
	public struct ContextMenuOpenSource
	{
		private readonly GraphPin pin;

		public ContextMenuOpenSource(GraphPin pin)
		{
			this.pin = pin;
		}

		public bool TryGetPin(out GraphPin resultPin)
		{
			resultPin = pin;
			return pin != null;
		}

		public override string ToString()
		{
			return "ContextMenuArgs(Pin: " + ((pin != null) ? pin.ToString() : "null") + ")";
		}
	}
}
