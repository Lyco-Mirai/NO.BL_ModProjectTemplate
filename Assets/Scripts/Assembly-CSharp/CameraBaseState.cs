public abstract class CameraBaseState
{
	public abstract void EnterState(CameraStateManager cam);

	public abstract void LeaveState(CameraStateManager cam);

	public abstract void UpdateState(CameraStateManager cam);

	public abstract void FixedUpdateState(CameraStateManager cam);
}
