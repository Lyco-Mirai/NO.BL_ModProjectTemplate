using System;

[Flags]
public enum RunwayQueryType
{
	Any = 0,
	Landing = 1,
	Takeoff = 2,
	LandingOrTakeoff = 3,
	Vertical = 8
}
