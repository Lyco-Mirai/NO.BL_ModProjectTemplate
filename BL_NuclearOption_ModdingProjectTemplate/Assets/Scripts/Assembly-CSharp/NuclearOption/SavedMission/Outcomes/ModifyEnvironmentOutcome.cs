using System;
using NuclearOption.MissionEditorScripts;
using NuclearOption.NodeGraph;

namespace NuclearOption.SavedMission.Outcomes
{
	internal class ModifyEnvironmentOutcome : Outcome
	{
		private static readonly MissionEnvironment defaultSettings = new MissionEnvironment();

		private ValueWrapperOverride<float> timeOfDay = new ValueWrapperOverride<float>();

		private ValueWrapperOverride<float> weather = new ValueWrapperOverride<float>();

		private ValueWrapperOverride<float> cloudAltitude = new ValueWrapperOverride<float>();

		private ValueWrapperOverride<float> windSpeed = new ValueWrapperOverride<float>();

		private ValueWrapperOverride<float> windTurbulence = new ValueWrapperOverride<float>();

		private ValueWrapperOverride<float> windHeading = new ValueWrapperOverride<float>();

		public ModifyEnvironmentSavedOutcome Saved => (ModifyEnvironmentSavedOutcome)SavedOutcome;

		public ModifyEnvironmentOutcome(ModifyEnvironmentSavedOutcome savedOutcome)
			: base(savedOutcome)
		{
		}

		public override void CopyFrom(Outcome original)
		{
			base.CopyFrom(original);
			ModifyEnvironmentOutcome modifyEnvironmentOutcome = (ModifyEnvironmentOutcome)original;
			timeOfDay.SetValue(modifyEnvironmentOutcome.timeOfDay.Value, this);
			weather.SetValue(modifyEnvironmentOutcome.weather.Value, this);
			cloudAltitude.SetValue(modifyEnvironmentOutcome.cloudAltitude.Value, this);
			windSpeed.SetValue(modifyEnvironmentOutcome.windSpeed.Value, this);
			windTurbulence.SetValue(modifyEnvironmentOutcome.windTurbulence.Value, this);
			windHeading.SetValue(modifyEnvironmentOutcome.windHeading.Value, this);
		}

		public override void Load(MissionLookups lookups)
		{
			base.Load(lookups);
			timeOfDay.SetValue(Saved.timeOfDay, this);
			weather.SetValue(Saved.weather, this);
			cloudAltitude.SetValue(Saved.cloudAltitude, this);
			windSpeed.SetValue(Saved.windSpeed, this);
			windTurbulence.SetValue(Saved.windTurbulence, this);
			windHeading.SetValue(Saved.windHeading, this);
			SetIfNoOverride<float>(timeOfDay, defaultSettings.timeOfDay);
			SetIfNoOverride<float>(weather, defaultSettings.weatherIntensity);
			SetIfNoOverride<float>(cloudAltitude, defaultSettings.cloudAltitude);
			SetIfNoOverride<float>(windSpeed, defaultSettings.windSpeed);
			SetIfNoOverride<float>(windTurbulence, defaultSettings.windTurbulence);
			SetIfNoOverride<float>(windHeading, defaultSettings.windHeading);
			static void SetIfNoOverride<T>(ValueWrapperOverride<T> wrapper, T value) where T : IEquatable<T>
			{
				if (!wrapper.Value.IsOverride)
				{
					wrapper.SetValue(new Override<T>(isOverride: false, value), null);
				}
			}
		}

		public override void Save()
		{
			base.Save();
			Saved.timeOfDay = timeOfDay.Value;
			Saved.weather = weather.Value;
			Saved.cloudAltitude = cloudAltitude.Value;
			Saved.windSpeed = windSpeed.Value;
			Saved.windTurbulence = windTurbulence.Value;
			Saved.windHeading = windHeading.Value;
		}

		public override void ReferenceDestroyed(ISaveableReference reference)
		{
		}

		public override void Complete(Objective completedObjective)
		{
			LevelInfo level = NetworkSceneSingleton<LevelInfo>.i;
			timeOfDay.IfOverride(delegate(float value)
			{
				level.SetTimeOfDay(value);
			});
			weather.IfOverride(delegate(float value)
			{
				level.Networkconditions = value;
			});
			cloudAltitude.IfOverride(delegate(float value)
			{
				level.NetworkcloudHeight = value;
			});
			windSpeed.IfOverride(delegate(float value)
			{
				level.SetWindSpeed(value);
			});
			windTurbulence.IfOverride(delegate(float value)
			{
				level.SetWindTurbulence(value);
			});
			windHeading.IfOverride(delegate(float value)
			{
				level.SetWindHeading(value);
			});
		}

		public override void DrawData(DataDrawer drawer)
		{
			FloatDataField item = drawer.DrawOverride("Time of Day", timeOfDay, drawer.Prefabs.FloatFieldPrefab).Item2;
			item.SetSliderSettings(new FloatDataField.FloatSlider(0f, 24f));
			item.SetSteps(0.1f);
			FloatDataField item2 = drawer.DrawOverride("Weather", weather, drawer.Prefabs.FloatFieldPrefab).Item2;
			item2.SetSliderSettings(new FloatDataField.FloatSlider(0f, 1f));
			item2.SetSteps(0.25f);
			FloatDataField item3 = drawer.DrawOverride("Cloud Height", cloudAltitude, drawer.Prefabs.FloatFieldPrefab).Item2;
			item3.SetSliderSettings(new FloatDataField.FloatSlider(500f, 4000f));
			item3.SetSteps(0.1f);
			FloatDataField item4 = drawer.DrawOverride("Wind Speed", windSpeed, drawer.Prefabs.FloatFieldPrefab).Item2;
			item4.SetSliderSettings(new FloatDataField.FloatSlider(0f, 20f));
			item4.SetSteps(0.1f);
			FloatDataField item5 = drawer.DrawOverride("Wind Turbulence", windTurbulence, drawer.Prefabs.FloatFieldPrefab).Item2;
			item5.SetSliderSettings(new FloatDataField.FloatSlider(0f, 1f));
			item5.SetSteps(0.001f);
			FloatDataField item6 = drawer.DrawOverride("Wind Direction", windHeading, drawer.Prefabs.FloatFieldPrefab).Item2;
			item6.SetSliderSettings(new FloatDataField.FloatSlider(0f, 360f));
			item6.SetSteps(0.1f);
		}

		public override void AddPins(GraphNodeData data)
		{
			data.InputElements.Add(new GraphFloatFieldData
			{
				PinId = new PinId("Time of Day"),
				DisplayName = "Time of Day",
				ValueWrapper = timeOfDay
			});
			data.InputElements.Add(new GraphFloatFieldData
			{
				PinId = new PinId("Weather"),
				DisplayName = "Weather",
				ValueWrapper = weather
			});
			data.InputElements.Add(new GraphFloatFieldData
			{
				PinId = new PinId("Cloud Height"),
				DisplayName = "Cloud Height",
				ValueWrapper = cloudAltitude
			});
			data.InputElements.Add(new GraphFloatFieldData
			{
				PinId = new PinId("Wind Speed"),
				DisplayName = "Wind Speed",
				ValueWrapper = windSpeed
			});
			data.InputElements.Add(new GraphFloatFieldData
			{
				PinId = new PinId("Wind Turbulence"),
				DisplayName = "Wind Turbulence",
				ValueWrapper = windTurbulence
			});
			data.InputElements.Add(new GraphFloatFieldData
			{
				PinId = new PinId("Wind Direction"),
				DisplayName = "Wind Direction",
				ValueWrapper = windHeading
			});
		}
	}
}
