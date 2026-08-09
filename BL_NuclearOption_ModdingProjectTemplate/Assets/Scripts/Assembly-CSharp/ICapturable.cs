using System.Collections.Generic;
using UnityEngine;

public interface ICapturable
{
	Capture Capture { get; }

	bool disabled { get; }

	Transform center { get; }

	float CaptureRange { get; }

	float CaptureDefense { get; }

	FactionHQ CurrentHQ { get; }

	List<GridSquare> gridSquares { get; }

	void OnCapture(FactionHQ value);

	IEnumerable<Unit> GetDefenseUnits();
}
