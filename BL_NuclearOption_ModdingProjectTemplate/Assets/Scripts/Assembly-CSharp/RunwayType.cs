using System;

[Flags]
public enum RunwayType
{
	None = 0,
	Landing = 1,
	Takeoff = 2,
	LandingOrTakeoff = 3,
	Vertical = 8
}
