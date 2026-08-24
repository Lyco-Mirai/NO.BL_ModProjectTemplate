using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace NuclearOption.UIStyleSystem
{
	[CreateAssetMenu(fileName = "NewColorTheme", menuName = "UI Style System/Color Theme")]
	public class ColorTheme : ScriptableObject
	{
		[SerializeField]
		private string id;

		[Header("HUD")]
		[SerializeField]
		private Color hudUnitNeutral;

		[SerializeField]
		private Color hudUnitFriendly;

		[SerializeField]
		private Color hudUnitHostile;

		[SerializeField]
		private Color hudUnitSelected;

		[SerializeField]
		private Color hudUnitFlash;

		[Header("Map")]
		[SerializeField]
		private Color mapIconNeutral;

		[SerializeField]
		private Color mapIconFriendly;

		[SerializeField]
		private Color mapIconHostile;

		[SerializeField]
		private Color mapIconNeutralSelected;

		[SerializeField]
		private Color mapIconFriendlySelected;

		[SerializeField]
		private Color mapIconHostileSelected;

		[SerializeField]
		private Color mapBackground;

		[Header("Radar Pings")]
		[SerializeField]
		private Color targetPing;

		[SerializeField]
		private Color detectedPing;

		[SerializeField]
		private Color passivePing;

		[Header("TacScreen & HUD")]
		[SerializeField]
		private Color allClear;

		[SerializeField]
		private Color warning;

		[SerializeField]
		private Color alert;

		[Header("Chat")]
		[SerializeField]
		private Color chatNeutral;

		[SerializeField]
		private Color chatFriendly;

		[SerializeField]
		private Color chatHostile;

		[SerializeField]
		private Color chatSystem;

		public string Id
		{
			get
			{
				return id;
			}
			set
			{
				id = value;
			}
		}

		public Color HudUnitNeutral => hudUnitNeutral;

		public Color HudUnitFriendly => hudUnitFriendly;

		public Color HudUnitHostile => hudUnitHostile;

		public Color HudUnitSelected => hudUnitSelected;

		public Color HudUnitFlash => hudUnitFlash;

		public Color MapIconNeutral => mapIconNeutral;

		public Color MapIconFriendly => mapIconFriendly;

		public Color MapIconHostile => mapIconHostile;

		public Color MapIconNeutralSelected => mapIconNeutralSelected;

		public Color MapIconFriendlySelected => mapIconFriendlySelected;

		public Color MapIconHostileSelected => mapIconHostileSelected;

		public Color MapBackground => mapBackground;

		public Color TargetPing => targetPing;

		public Color DetectedPing => detectedPing;

		public Color PassivePing => passivePing;

		public Color AllClear => allClear;

		public Color Warning => warning;

		public Color Alert => alert;

		public Color ChatNeutral => chatNeutral;

		public Color ChatFriendly => chatFriendly;

		public Color ChatHostile => chatHostile;

		public Color ChatSystem => chatSystem;

		private void PopulateMissingColors()
		{
			List<Tuple<string, Color, string>> colors = GetColors();
			List<Tuple<string, Color, string>> colors2 = ThemeManager.Vanilla.ColorTheme.GetColors();
			List<Color> list = new List<Color>();
			for (int i = 0; i < colors.Count; i++)
			{
				list.Add((colors[i].Item2 == Color.clear) ? colors2[i].Item2 : colors[i].Item2);
			}
			SetColors(list);
		}

		public List<Tuple<string, Color, string>> GetColors()
		{
			return new List<Tuple<string, Color, string>>
			{
				Tuple.Create("HUD Unit Neutral", hudUnitNeutral, "Color of neutral unit icons on the HUD."),
				Tuple.Create("HUD Unit Friendly", hudUnitFriendly, "Color of friendly unit icons on the HUD."),
				Tuple.Create("HUD Unit Hostile", hudUnitHostile, "Color of hostile unit icons on the HUD."),
				Tuple.Create("HUD Unit Selected", hudUnitSelected, "Color of selected unit icons on the HUD."),
				Tuple.Create("HUD Unit Flash", hudUnitFlash, "Color of flashing unit icons on the HUD.\n\n<i>(Example: Radar Warnings)</i>"),
				Tuple.Create("Map Icon Neutral", mapIconNeutral, "Color of neutral unit icons on the map."),
				Tuple.Create("Map Icon Friendly", mapIconFriendly, "Color of friendly unit icons on the map."),
				Tuple.Create("Map Icon Hostile", mapIconHostile, "Color of hostile unit icons on the map."),
				Tuple.Create("Map Icon Neutral Selected", mapIconNeutralSelected, "Color of selected neutral unit icons on the map."),
				Tuple.Create("Map Icon Friendly Selected", mapIconFriendlySelected, "Color of selected friendly unit icons on the map."),
				Tuple.Create("Map Icon Hostile Selected", mapIconHostileSelected, "Color of selected hostile unit icons on the map."),
				Tuple.Create("Map Background", mapBackground, "Color of the map background."),
				Tuple.Create("Target Ping", targetPing, "Color of a radar ping from a unit targeting you."),
				Tuple.Create("Detected Ping", detectedPing, "Color of a radar ping from a unit that is detecting you."),
				Tuple.Create("Passive Ping", passivePing, "Color of a radar ping from a unit that is not detecting you."),
				Tuple.Create("All Clear", allClear, "Color used by all HUD & Tac Screen elements that represent a dynamic state, here the <b>All Clear</b> state.\n\n<i>(Example: fuel level, AoA, etc...)</i>"),
				Tuple.Create("Warning", warning, "Color used by all HUD & Tac Screen elements that represent a dynamic state, here the <b>Warning</b> state.\n\n<i>(Example: fuel level, AoA, etc...)</i>"),
				Tuple.Create("Danger", alert, "Color used by all HUD & Tac Screen elements that represent a dynamic state, here the <b>Danger</b> state.\n\n<i>(Example: fuel level, AoA, etc...)</i>"),
				Tuple.Create("Chat Neutral", chatNeutral, "Color used for neutral units/players in chat."),
				Tuple.Create("Chat Friendly", chatFriendly, "Color used for friendly units/players in chat."),
				Tuple.Create("Chat Hostile", chatHostile, "Color used for hostile units/players in chat."),
				Tuple.Create("Chat System", chatSystem, "Color used for system messages in chat.")
			};
		}

		public void SetColors(List<Color> colorMap)
		{
			if (colorMap == null || colorMap.Count != 22)
			{
				throw new ArgumentException("Number of colors must match 22");
			}
			ColorTheme colorTheme = ThemeManager.Vanilla.ColorTheme;
			hudUnitNeutral = colorMap[0].WithAlpha(colorTheme.hudUnitNeutral.a);
			hudUnitFriendly = colorMap[1].WithAlpha(colorTheme.hudUnitFriendly.a);
			hudUnitHostile = colorMap[2].WithAlpha(colorTheme.hudUnitHostile.a);
			hudUnitSelected = colorMap[3].WithAlpha(colorTheme.hudUnitSelected.a);
			hudUnitFlash = colorMap[4].WithAlpha(colorTheme.hudUnitFlash.a);
			mapIconNeutral = colorMap[5].WithAlpha(colorTheme.mapIconNeutral.a);
			mapIconFriendly = colorMap[6].WithAlpha(colorTheme.mapIconFriendly.a);
			mapIconHostile = colorMap[7].WithAlpha(colorTheme.mapIconHostile.a);
			mapIconNeutralSelected = colorMap[8].WithAlpha(colorTheme.mapIconNeutralSelected.a);
			mapIconFriendlySelected = colorMap[9].WithAlpha(colorTheme.mapIconFriendlySelected.a);
			mapIconHostileSelected = colorMap[10].WithAlpha(colorTheme.mapIconHostileSelected.a);
			mapBackground = colorMap[11].WithAlpha(colorTheme.mapBackground.a);
			targetPing = colorMap[12].WithAlpha(colorTheme.targetPing.a);
			detectedPing = colorMap[13].WithAlpha(colorTheme.detectedPing.a);
			passivePing = colorMap[14].WithAlpha(colorTheme.passivePing.a);
			allClear = colorMap[15].WithAlpha(colorTheme.allClear.a);
			warning = colorMap[16].WithAlpha(colorTheme.warning.a);
			alert = colorMap[17].WithAlpha(colorTheme.alert.a);
			chatNeutral = colorMap[18].WithAlpha(colorTheme.chatNeutral.a);
			chatFriendly = colorMap[19].WithAlpha(colorTheme.chatFriendly.a);
			chatHostile = colorMap[20].WithAlpha(colorTheme.chatHostile.a);
			chatSystem = colorMap[21].WithAlpha(colorTheme.chatSystem.a);
		}

		[ContextMenu("Save to JSON")]
		public void SaveToJson()
		{
			string text = Application.persistentDataPath + "/themes/" + ThemeManager.Active.name;
			if (!Directory.Exists(text))
			{
				Directory.CreateDirectory(text);
			}
			string path = text + "/COLOR.json";
			string contents = JsonUtility.ToJson(this, prettyPrint: true);
			File.WriteAllText(path, contents);
			ColorLog<ColorTheme>.Info("Successfully saved : COLOR.json");
		}

		public static ColorTheme LoadFromJson(string jsonPath)
		{
			if (!File.Exists(jsonPath))
			{
				throw new FileNotFoundException(jsonPath + " is not a valid color theme.");
			}
			string json = File.ReadAllText(jsonPath);
			ColorTheme colorTheme = ScriptableObject.CreateInstance<ColorTheme>();
			JsonUtility.FromJsonOverwrite(json, colorTheme);
			if (colorTheme == null)
			{
				throw new Exception("Failed to deserialize JSON: " + jsonPath);
			}
			colorTheme.name = colorTheme.id;
			colorTheme.PopulateMissingColors();
			return colorTheme;
		}

		public Gradient Gradient(List<float> sourcePositions = null, List<float> outputPositions = null)
		{
			if (sourcePositions == null)
			{
				sourcePositions = new List<float> { 0f, 0.5f, 1f };
			}
			if (outputPositions == null)
			{
				outputPositions = new List<float> { 0f, 0.5f, 1f };
			}
			if (sourcePositions.Count != outputPositions.Count)
			{
				throw new ArgumentException("sourcePositions and outputPositions must have the same length");
			}
			if (sourcePositions.Count > 8)
			{
				throw new ArgumentException("Too many positions, maximum is 8");
			}
			Gradient gradient = new Gradient();
			GradientColorKey[] array = new GradientColorKey[sourcePositions.Count];
			for (int i = 0; i < sourcePositions.Count; i++)
			{
				float num = Mathf.Clamp01(sourcePositions[i]);
				Color col = ((num < 0.5f) ? Color.Lerp(Alert, Warning, num * 2f) : Color.Lerp(Warning, AllClear, (num - 0.5f) * 2f));
				array[i] = new GradientColorKey(col, Mathf.Clamp01(outputPositions[i]));
			}
			gradient.SetKeys(array, Array.Empty<GradientAlphaKey>());
			return gradient;
		}
	}
}
