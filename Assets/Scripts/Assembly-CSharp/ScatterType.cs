using System;
using System.Collections.Generic;
using JamesFrowen;
using UnityEngine;

[Serializable]
public class ScatterType
{
	public string Name;

	[Label("Position data used by TreeRenderer", true)]
	public TextAsset SavedData;

	public float maxDensity;

	public Texture2D densityMap;

	public float heightVariation;

	public float sampleRadius;

	public int samplePoints = 1;

	[NonSerialized]
	public List<GlobalPosition> GeneratedPositions;
}
