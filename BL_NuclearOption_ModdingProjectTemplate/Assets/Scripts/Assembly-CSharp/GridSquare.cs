using System.Collections.Generic;

public class GridSquare
{
	public readonly int X;

	public readonly int Y;

	public readonly List<Unit> units = new List<Unit>();

	public readonly List<Obstacle> obstacles = new List<Obstacle>();

	public GridSquare(int x, int y)
	{
		X = x;
		Y = y;
	}

	public override string ToString()
	{
		return $"GridSquare({X},{Y})";
	}
}
