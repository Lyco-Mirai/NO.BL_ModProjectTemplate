using System;
using NuclearOption.ModScripts;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/Aircraft/Livery")]
[CopyToModProject(CopyCreateAssetMenu = false)]
public class LiveryData : ScriptableObject
{
	[Serializable]
	[CopyToModProject]
	public struct TextureColor
	{
		public Color32 Color;

		public int Count;
	}

	public Texture2D Texture;

	public float Glossiness;

	public TextureColor[] Colors;
}
