using NuclearOption.SavedMission;
using NuclearOption.SavedMission.Objectives;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts
{
	public class RunwayTab : MonoBehaviour
	{
		[SerializeField]
		private Button backButton;

		[SerializeField]
		private Button deleteButton;

		[SerializeField]
		private TMP_InputField nameField;

		[SerializeField]
		private Toggle reversableToggle;

		[SerializeField]
		private Toggle takeoffToggle;

		[SerializeField]
		private Toggle landingToggle;

		[SerializeField]
		private Toggle arrestorToggle;

		[SerializeField]
		private Toggle skiJumpToggle;

		[SerializeField]
		private Slider widthSlider;

		[SerializeField]
		private TextMeshProUGUI widthSliderLabel;

		[SerializeField]
		private Vector3DataField startField;

		[SerializeField]
		private Vector3DataField endField;

		[SerializeField]
		private PositionHandle positionHandlePrefab;

		private Airbase airbase;

		private SavedRunway runway;

		private PositionHandle startHandle;

		private PositionHandle endHandle;

		private ValueWrapperGlobalPosition startWrapper;

		private ValueWrapperGlobalPosition endWrapper;

		private void Awake()
		{
			nameField.onEndEdit.AddListener(NameChanged);
			reversableToggle.onValueChanged.AddListener(ReversableChanged);
			takeoffToggle.onValueChanged.AddListener(TakeoffChanged);
			landingToggle.onValueChanged.AddListener(LandingChanged);
			arrestorToggle.onValueChanged.AddListener(ArrestorChanged);
			skiJumpToggle.onValueChanged.AddListener(SkiJumpChanged);
			widthSlider.onValueChanged.AddListener(WidthChanged);
			(startWrapper, startHandle) = CreateHandle(startField, StartChanged, "Start", Color.green);
			(endWrapper, endHandle) = CreateHandle(endField, EndChanged, "End", Color.red);
			backButton.onClick.AddListener(BackClicked);
			deleteButton.onClick.AddListener(DeleteClicked);
		}

		private void BackClicked()
		{
			SceneSingleton<UnitSelection>.i.ClearSelection();
			SceneSingleton<UnitSelection>.i.SetSelection(airbase);
		}

		private void DeleteClicked()
		{
			airbase.SavedAirbase.runways.Remove(runway);
			BackClicked();
		}

		private void OnDestroy()
		{
			if (startHandle != null)
			{
				Object.Destroy(startHandle.gameObject);
			}
			if (endHandle != null)
			{
				Object.Destroy(endHandle.gameObject);
			}
		}

		public void Setup(Airbase airbase, SavedRunway runway)
		{
			this.airbase = airbase;
			this.runway = runway;
			nameField.SetTextWithoutNotify(runway.Name);
			reversableToggle.SetIsOnWithoutNotify(runway.Reversable);
			takeoffToggle.SetIsOnWithoutNotify(runway.Takeoff);
			landingToggle.SetIsOnWithoutNotify(runway.Landing);
			arrestorToggle.SetIsOnWithoutNotify(runway.Arrestor);
			skiJumpToggle.SetIsOnWithoutNotify(runway.SkiJump);
			widthSlider.SetValueWithoutNotify(runway.Width);
			widthSliderLabel.text = runway.Width.ToString();
			startWrapper.SetValue(runway.Start, this, invokeOnChangeOnly: false);
			endWrapper.SetValue(runway.End, this, invokeOnChangeOnly: false);
		}

		private (ValueWrapperGlobalPosition wrapper, PositionHandle handle) CreateHandle(Vector3DataField field, ValueWrapper<GlobalPosition>.OnChangeDelegate onChange, string label, Color color)
		{
			ValueWrapperGlobalPosition valueWrapperGlobalPosition = new ValueWrapperGlobalPosition();
			valueWrapperGlobalPosition.RegisterOnChange(this, onChange);
			PositionHandle positionHandle = Object.Instantiate(positionHandlePrefab);
			positionHandle.SetHue(color);
			positionHandle.Setup(valueWrapperGlobalPosition, () => label + "handle", null);
			field.Setup(label, valueWrapperGlobalPosition);
			return (wrapper: valueWrapperGlobalPosition, handle: positionHandle);
		}

		private void NameChanged(string value)
		{
			runway.Rename(value);
		}

		private void ReversableChanged(bool value)
		{
			runway.Reversable = value;
		}

		private void TakeoffChanged(bool value)
		{
			runway.Takeoff = value;
		}

		private void LandingChanged(bool value)
		{
			runway.Landing = value;
		}

		private void ArrestorChanged(bool value)
		{
			runway.Arrestor = value;
		}

		private void SkiJumpChanged(bool value)
		{
			runway.SkiJump = value;
		}

		private void WidthChanged(float value)
		{
			runway.Width = value;
			widthSliderLabel.text = value.ToString();
		}

		private void StartChanged(GlobalPosition value)
		{
			runway.Start = value;
		}

		private void EndChanged(GlobalPosition value)
		{
			runway.End = value;
		}
	}
}
