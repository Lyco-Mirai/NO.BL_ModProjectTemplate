using UnityEngine;

public class MissileSeeker : MonoBehaviour
{
	[SerializeField]
	protected Missile missile;

	protected Unit targetUnit;

	public bool triggerMissileWarning = true;

	public bool proximityFuse;

	public virtual string GetSeekerType()
	{
		return string.Empty;
	}

	public virtual float GetSeekerThreat()
	{
		return 1f;
	}

	public virtual float GetMinSpeed()
	{
		return 200f;
	}

	public virtual GlobalPosition GetEvasionPoint()
	{
		return missile.GlobalPosition();
	}

	public virtual void Seek()
	{
	}

	public virtual void Initialize(Unit target, GlobalPosition aimpoint)
	{
	}
}
