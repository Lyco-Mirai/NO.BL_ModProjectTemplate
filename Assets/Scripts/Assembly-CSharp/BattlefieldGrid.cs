using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public static class BattlefieldGrid
{
	public static GridSquare[] gridLookup;

	private static float mapSize;

	private static float gridSize;

	private static int divisions;

	private static readonly List<Unit> inRangeUnitsCache = new List<Unit>(100);

	private static readonly List<Wreckage> inRangeWrecksCache = new List<Wreckage>(100);

	private static readonly List<GridSquare> gridCache = new List<GridSquare>(100);

	public static void GenerateGrid(float setMapSize, float setGridSize)
	{
		if (setMapSize == 0f)
		{
			Debug.LogError("Can set mapSize to 0");
			return;
		}
		if (setGridSize == 0f)
		{
			Debug.LogError("Can set gridSize to 0");
			return;
		}
		mapSize = setMapSize;
		gridSize = setGridSize;
		divisions = (int)(mapSize / gridSize);
		gridLookup = new GridSquare[divisions * divisions];
		for (int i = 0; i < divisions * divisions; i++)
		{
			int x = i / divisions;
			int y = i % divisions;
			gridLookup[i] = new GridSquare(x, y);
		}
	}

	public static void Clear()
	{
		gridLookup = null;
		inRangeUnitsCache.Clear();
		inRangeWrecksCache.Clear();
		gridCache.Clear();
		divisions = 0;
	}

	public static bool TryGetGridSquare(GlobalPosition coord, out GridSquare gridSquare)
	{
		if (TryGetGridXY(coord, out var gridCoordX, out var gridCoordY))
		{
			int num = gridCoordY * divisions + gridCoordX;
			gridSquare = gridLookup[num];
			return true;
		}
		gridSquare = null;
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryGetGridXY(GlobalPosition coord, out int gridCoordX, out int gridCoordY)
	{
		if (!float.IsFinite(coord.x) || !float.IsFinite(coord.z))
		{
			gridCoordX = 0;
			gridCoordY = 0;
			return false;
		}
		gridCoordX = (int)Mathf.Clamp((coord.x + mapSize * 0.5f) / gridSize, 0f, divisions - 1);
		gridCoordY = (int)Mathf.Clamp((coord.z + mapSize * 0.5f) / gridSize, 0f, divisions - 1);
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static GridSquare GetSquare(int gridCoordX, int gridCoordY)
	{
		return gridLookup[gridCoordY * divisions + gridCoordX];
	}

	public static void UpdateUnit(Unit unit, ref GridSquare gridSquare)
	{
		if (TryGetGridSquare(unit.transform.GlobalPosition(), out var gridSquare2) && gridSquare2 != gridSquare)
		{
			gridSquare?.units.Remove(unit);
			gridSquare = gridSquare2;
			gridSquare.units.Add(unit);
		}
	}

	public static List<GridSquare> GetGridSquaresInRange(GlobalPosition coord, float range)
	{
		List<GridSquare> list = new List<GridSquare>();
		GetGridSquaresInRangeNonAlloc(coord, range, list);
		return list;
	}

	public static void GetGridSquaresInRangeNonAlloc(GlobalPosition coord, float range, List<GridSquare> gridSquares)
	{
		gridSquares.Clear();
		if (!TryGetGridXY(coord, out var gridCoordX, out var gridCoordY))
		{
			return;
		}
		int num = Mathf.CeilToInt(range * (float)divisions / mapSize);
		int num2 = Mathf.Max(gridCoordX - num, 0);
		int num3 = Mathf.Min(gridCoordX + num, divisions);
		int num4 = Mathf.Max(gridCoordY - num, 0);
		int num5 = Mathf.Min(gridCoordY + num, divisions);
		for (int i = num2; i < num3; i++)
		{
			for (int j = num4; j < num5; j++)
			{
				gridSquares.Add(GetSquare(i, j));
			}
		}
	}

	public static IEnumerable<Unit> GetUnitsInRangeEnumerable(GlobalPosition coord, float range)
	{
		GetUnitsInRangeNonAlloc(coord, range, inRangeUnitsCache);
		return inRangeUnitsCache;
	}

	public static void GetUnitsInRangeNonAlloc(GlobalPosition coord, float range, List<Unit> units)
	{
		units.Clear();
		GetGridSquaresInRangeNonAlloc(coord, range, gridCache);
		foreach (GridSquare item2 in gridCache)
		{
			List<Unit> units2 = item2.units;
			if (units2.Count > 0)
			{
				for (int num = units2.Count - 1; num >= 0; num--)
				{
					Unit item = units2[num];
					units.Add(item);
				}
			}
		}
	}

	public static IEnumerable<Wreckage> GetWrecksInRangeEnumerable(GlobalPosition coord, float range)
	{
		GetWrecksInRangeNonAlloc(coord, range, inRangeWrecksCache);
		return inRangeWrecksCache;
	}

	public static void GetWrecksInRangeNonAlloc(GlobalPosition coord, float range, List<Wreckage> wrecks)
	{
		wrecks.Clear();
		GetGridSquaresInRangeNonAlloc(coord, range, gridCache);
		foreach (GridSquare item in gridCache)
		{
			List<Obstacle> obstacles = item.obstacles;
			if (obstacles.Count <= 0)
			{
				continue;
			}
			for (int num = obstacles.Count - 1; num >= 0; num--)
			{
				if (obstacles[num].Transform.TryGetComponent<Wreckage>(out var component))
				{
					wrecks.Add(component);
				}
			}
		}
	}
}
