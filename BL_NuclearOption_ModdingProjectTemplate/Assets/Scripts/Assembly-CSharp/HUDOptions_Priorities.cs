using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
[CreateAssetMenu(fileName = "New HUDOptions Setting", menuName = "ScriptableObjects/HUDOptionsSetting", order = 9)]
public class HUDOptions_Priorities : ScriptableObject
{
	[Serializable]
	public class Setting
	{
		public string typeName;

		public bool typePriority = true;
	}

	public List<Setting> listCategories = new List<Setting>();

	public List<Setting> listVehicles = new List<Setting>();

	public List<Setting> listBuildings = new List<Setting>();

	public Encyclopedia encyclopedia;

	[ContextMenu("Update From Encyclopedia")]
	private void UpdateFromEncyclopedia()
	{
		listVehicles.Clear();
		foreach (Encyclopedia.UnitType vehicleType in encyclopedia.vehicleTypes)
		{
			listVehicles.Add(new Setting
			{
				typeName = vehicleType.typeName,
				typePriority = true
			});
		}
		listBuildings.Clear();
		foreach (Encyclopedia.UnitType buildingType in encyclopedia.buildingTypes)
		{
			listBuildings.Add(new Setting
			{
				typeName = buildingType.typeName,
				typePriority = true
			});
		}
	}

	[ContextMenu("Save to JSON")]
	public void SaveToJson()
	{
		string text = Application.persistentDataPath + "/HUDOptions";
		if (!Directory.Exists(text))
		{
			Directory.CreateDirectory(text);
		}
		string path = text + "/" + base.name + ".json";
		string contents = JsonUtility.ToJson(this, prettyPrint: true);
		File.WriteAllText(path, contents);
		ColorLog<HUDOptions_Priorities>.Info("Successfully saved : " + base.name + ".json");
	}

	[ContextMenu("Read from JSON")]
	public void ReadFromJson()
	{
		string text = Application.persistentDataPath + "/HUDOptions";
		string path = text + "/" + base.name + ".json";
		if (Directory.Exists(text) && File.Exists(path))
		{
			JsonUtility.FromJsonOverwrite(File.ReadAllText(path), this);
			ColorLog<HUDOptions_Priorities>.Info("Successfully loaded : " + base.name + ".json");
		}
	}
}
