public interface IPowerSource
{
	float GetMaxPower();

	float GetPower();

	void Throttle(float throttle);
}
