public struct CompressedInputs
{
	public CompressedFloat pitch;

	public CompressedFloat roll;

	public CompressedFloat yaw;

	public CompressedFloat throttle;

	public CompressedFloat brake;

	public CompressedFloat customAxis1;

	public CompressedInputs(ControlInputs inputs)
	{
		pitch = inputs.pitch.Compress();
		roll = inputs.roll.Compress();
		yaw = inputs.yaw.Compress();
		throttle = inputs.throttle.Compress();
		brake = inputs.brake.Compress();
		customAxis1 = inputs.customAxis1.Compress();
	}

	public bool Valid(bool logErrors)
	{
		return (byte)(1u & (NetworkFloatHelper.Validate(pitch, logErrors, "pitch") ? 1u : 0u) & (NetworkFloatHelper.Validate(roll, logErrors, "roll") ? 1u : 0u) & (NetworkFloatHelper.Validate(yaw, logErrors, "yaw") ? 1u : 0u) & (NetworkFloatHelper.Validate(throttle, logErrors, "throttle") ? 1u : 0u) & (NetworkFloatHelper.Validate(brake, logErrors, "brake") ? 1u : 0u) & (NetworkFloatHelper.Validate(customAxis1, logErrors, "customAxis1") ? 1u : 0u)) != 0;
	}
}
