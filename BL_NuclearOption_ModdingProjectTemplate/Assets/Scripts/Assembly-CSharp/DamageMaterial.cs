using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DamageMaterial
{
	public float threshold;

	public List<Renderer> renderers = new List<Renderer>();

	public byte[] indices;
}
