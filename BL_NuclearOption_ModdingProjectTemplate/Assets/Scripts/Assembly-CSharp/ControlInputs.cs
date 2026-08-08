public class ControlInputs
{
	public float pitch;

	public float roll;

	public float yaw;

	public float throttle;

	public float brake;

	public float customAxis1;

	public override string ToString()
	{
		return $"Inputs({pitch:0.00},{roll:0.00},{yaw:0.00},{throttle:0.00},{brake:0.00},{customAxis1:0.00})";
	}
}
