using RoadPathfinding;

public struct RoadPoint
{
	public Road road;

	public int index;

	public RoadPoint(Road road, int index)
	{
		this.road = road;
		this.index = index;
	}
}
