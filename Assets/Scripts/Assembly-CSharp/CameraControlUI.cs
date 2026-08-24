using UnityEngine;
using UnityEngine.UI;

public class CameraControlUI : SceneSingleton<CameraControlUI>
{
	public static CameraStateManager cam;

	[Header("Main Container")]
	[SerializeField]
	private GameObject container;

	[Header("Camera States")]
	[SerializeField]
	private GameObject StatesPanel;

	[SerializeField]
	private Button cockpitButton;

	[SerializeField]
	private Button orbitButton;

	[SerializeField]
	private Button chaseButton;

	[SerializeField]
	private Button controlledButton;

	[SerializeField]
	private Button freeButton;

	[SerializeField]
	private Button flybyButton;

	private Image cockpitButtonImg;

	private Image orbitButtonImg;

	private Image chaseButtonImg;

	private Image controlledButtonImg;

	private Image freeButtonImg;

	private Image flybyButtonImg;

	[Header("Field of View")]
	[SerializeField]
	private GameObject FOVPanel;

	[SerializeField]
	private Slider camFOV;

	[SerializeField]
	private Slider camFOVSpeed;

	[SerializeField]
	private Slider camFOVInertia;

	[SerializeField]
	private Text camFOVValue;

	[SerializeField]
	private Text camFOVSpeedValue;

	[SerializeField]
	private Text camFOVInertiaValue;

	[Header("Inputs")]
	[SerializeField]
	private GameObject InputsPanel;

	[SerializeField]
	private Slider camTransSpeed;

	[SerializeField]
	private Slider camRotSpeed;

	[SerializeField]
	private Toggle camInputToggle;

	[SerializeField]
	private Text camTransSpeedValue;

	[SerializeField]
	private Text camRotSpeedValue;

	[Header("Time Factor")]
	[SerializeField]
	private GameObject TimePanel;

	[SerializeField]
	private Slider timeFactor;

	[SerializeField]
	private Text timeFactorValue;

	[Header("Pivot")]
	[SerializeField]
	private GameObject PivotPanel;

	[SerializeField]
	private Text pivotParent;

	[SerializeField]
	private Text pivotUp;

	[SerializeField]
	private InputField pivotPosX;

	[SerializeField]
	private InputField pivotPosY;

	[SerializeField]
	private InputField pivotPosZ;

	[SerializeField]
	private InputField pivotAngleX;

	[SerializeField]
	private InputField pivotAngleY;

	[SerializeField]
	private InputField pivotAngleZ;

	[Header("Camera Transform")]
	[SerializeField]
	private GameObject CameraPanel;

	[SerializeField]
	private Text camTarget;

	[SerializeField]
	private InputField camPosX;

	[SerializeField]
	private InputField camPosY;

	[SerializeField]
	private InputField camPosZ;

	[SerializeField]
	private InputField camAngleX;

	[SerializeField]
	private InputField camAngleY;

	[SerializeField]
	private InputField camAngleZ;

	[Header("Movement")]
	[SerializeField]
	private GameObject MovementPanel;

	[SerializeField]
	private InputField camTranslationX;

	[SerializeField]
	private InputField camTranslationY;

	[SerializeField]
	private InputField camTranslationZ;

	[SerializeField]
	private InputField camRotationX;

	[SerializeField]
	private InputField camRotationY;

	[SerializeField]
	private InputField camRotationZ;

	[Header("Current Values")]
	private Vector3 pivotPos = Vector3.zero;

	private Vector3 pivotAngle = Vector3.zero;

	private Vector3 camPos = Vector3.zero;

	private Vector3 camAngle = Vector3.zero;

	private Vector3 camTranslation = Vector3.zero;

	private Vector3 camRotation = Vector3.zero;

	public bool isOpen;

	public bool isMoving;

	private void Start()
	{
		cam = SceneSingleton<CameraStateManager>.i;
		camFOV.value = cam.desiredFOV;
		camFOVSpeed.value = 0.5f;
		camFOVInertia.value = 0.5f;
		camTransSpeed.value = 1f;
		camRotSpeed.value = 1f;
		camInputToggle.isOn = true;
		cam.onSwitchCamera += SetCamState;
		camPosX.contentType = InputField.ContentType.DecimalNumber;
		camPosY.contentType = InputField.ContentType.DecimalNumber;
		camPosZ.contentType = InputField.ContentType.DecimalNumber;
		camAngleX.contentType = InputField.ContentType.DecimalNumber;
		camAngleY.contentType = InputField.ContentType.DecimalNumber;
		camAngleZ.contentType = InputField.ContentType.DecimalNumber;
		camTranslationX.contentType = InputField.ContentType.DecimalNumber;
		camTranslationY.contentType = InputField.ContentType.DecimalNumber;
		camTranslationZ.contentType = InputField.ContentType.DecimalNumber;
		container.SetActive(value: false);
		isOpen = false;
		isMoving = false;
		cockpitButtonImg = cockpitButton.GetComponent<Image>();
		orbitButtonImg = orbitButton.GetComponent<Image>();
		freeButtonImg = freeButton.GetComponent<Image>();
		flybyButtonImg = flybyButton.GetComponent<Image>();
		chaseButtonImg = chaseButton.GetComponent<Image>();
		controlledButtonImg = controlledButton.GetComponent<Image>();
	}

	public void Update()
	{
		if (SceneSingleton<CameraStateManager>.i.currentState != SceneSingleton<CameraStateManager>.i.selectionState && GameManager.playerInput.GetButtonTimedPressDown("Switch View", PlayerSettings.pressDelay + 0.2f))
		{
			if (!isOpen)
			{
				OpenMenu();
			}
			else
			{
				CloseMenu();
			}
		}
		if (isOpen && Input.GetKeyDown(KeyCode.Escape))
		{
			CloseMenu();
		}
		if (Input.GetKeyDown(KeyCode.Space))
		{
			if (isMoving)
			{
				PauseMovement();
			}
			else
			{
				ApplyMovement();
			}
		}
	}

	public void OpenMenu()
	{
		container.SetActive(value: true);
		SetCamState();
		if (GameManager.gameState == GameState.SinglePlayer)
		{
			PauseTime();
		}
		CursorManager.SetFlag(CursorFlags.CameraControlUI, value: true);
		isOpen = true;
	}

	public void CloseMenu()
	{
		CursorManager.SetFlag(CursorFlags.CameraControlUI, value: false);
		container.SetActive(value: false);
		isOpen = false;
	}

	public void ApplyTimeFactor()
	{
		timeFactor.value = Mathf.Round(100f * timeFactor.value) / 100f;
		TimeScaleManager.Scale = timeFactor.value;
	}

	public void PauseTime()
	{
		timeFactor.value = 0f;
		TimeScaleManager.Scale = timeFactor.value;
	}

	public void PlayTime()
	{
		timeFactor.value = 1f;
		TimeScaleManager.Scale = timeFactor.value;
	}

	public void ResetFOV()
	{
		cam.SetDesiredFoV(PlayerSettings.defaultFoV, 0f);
		camFOVSpeed.value = 0.5f;
		camFOVInertia.value = 0.5f;
		cam.fovChangeSpeed = camFOVSpeed.value;
		cam.fovChangeInertia = camFOVInertia.value;
	}

	public void ApplyFOV()
	{
		cam.SetDesiredFoV(camFOV.value, 0f);
	}

	public void ApplyFOVSpeed()
	{
		cam.fovChangeSpeed = Mathf.Clamp(camFOVSpeed.value, 0.001f, 1f);
	}

	public void ApplyFOVInertia()
	{
		cam.fovChangeInertia = Mathf.Clamp(camFOVInertia.value, 0f, 1f);
	}

	public void ApplyInputSpeed()
	{
		cam.SetDesiredSpeed(camTransSpeed.value, camRotSpeed.value);
	}

	public void ApplyInputsToggle()
	{
		cam.allowInputs = camInputToggle.isOn;
	}

	public void ApplyCamPos()
	{
		if ((0u | (float.TryParse(camPosX.text, out var result) ? 1u : 0u) | (float.TryParse(camPosY.text, out var result2) ? 1u : 0u) | (float.TryParse(camPosZ.text, out var result3) ? 1u : 0u)) != 0)
		{
			camPos = new Vector3(result, result2, result3);
			camPosX.text = $"{result:F2}";
			camPosY.text = $"{result2:F2}";
			camPosZ.text = $"{result3:F2}";
			if (cam.currentState == cam.controlledState)
			{
				cam.controlledState.SetCustomTransform(camPos, camAngle);
			}
			else if (cam.currentState == cam.chaseState)
			{
				cam.chaseState.SetCustomTransform(camPos, camAngle);
			}
			else if (cam.currentState == cam.TVState)
			{
				cam.TVState.SetCustomTransform(camPos, camAngle);
			}
			GetValues();
		}
	}

	public void ApplyCamAngle()
	{
		if ((0u | (float.TryParse(camAngleX.text, out var result) ? 1u : 0u) | (float.TryParse(camAngleY.text, out var result2) ? 1u : 0u) | (float.TryParse(camAngleZ.text, out var result3) ? 1u : 0u)) != 0)
		{
			camAngle = new Vector3(result, result2, result3);
			camAngleX.text = $"{result:F0}";
			camAngleY.text = $"{result2:F0}";
			camAngleZ.text = $"{result3:F0}";
			if (cam.currentState == cam.controlledState)
			{
				cam.controlledState.SetCustomTransform(camPos, camAngle);
			}
			else if (cam.currentState == cam.chaseState)
			{
				cam.chaseState.SetCustomTransform(camPos, camAngle);
			}
			GetValues();
		}
	}

	public void ToggleCameraTarget()
	{
		if (!(cam.followingUnit == null) && cam.currentState == cam.controlledState)
		{
			Transform transform = (cam.controlledState.IsLookingAt() ? null : cam.followingUnit.transform);
			cam.controlledState.SetLookAt(transform);
			camTarget.text = ((transform != null) ? cam.followingUnit.UniqueName : "-");
			GetValues();
		}
	}

	public void ApplyPivotPos()
	{
		if ((0u | (float.TryParse(pivotPosX.text, out var result) ? 1u : 0u) | (float.TryParse(pivotPosY.text, out var result2) ? 1u : 0u) | (float.TryParse(pivotPosZ.text, out var result3) ? 1u : 0u)) != 0)
		{
			pivotPos = new Vector3(result, result2, result3);
			pivotPosX.text = $"{result:F2}";
			pivotPosY.text = $"{result2:F2}";
			pivotPosZ.text = $"{result3:F2}";
			if (cam.currentState == cam.controlledState)
			{
				cam.controlledState.SetCustomPivot(pivotPos, pivotAngle);
			}
			GetValues();
		}
	}

	public void ApplyPivotAngle()
	{
		if ((0u | (float.TryParse(pivotAngleX.text, out var result) ? 1u : 0u) | (float.TryParse(pivotAngleY.text, out var result2) ? 1u : 0u) | (float.TryParse(pivotAngleZ.text, out var result3) ? 1u : 0u)) != 0)
		{
			pivotAngle = new Vector3(result, result2, result3);
			pivotAngleX.text = $"{result:F2}";
			pivotAngleY.text = $"{result2:F2}";
			pivotAngleZ.text = $"{result3:F2}";
			if (cam.currentState == cam.controlledState)
			{
				cam.controlledState.SetCustomPivot(pivotPos, pivotAngle);
			}
			GetValues();
		}
	}

	public void ReturnPivotToUnit()
	{
		if (!(cam.followingUnit == null))
		{
			pivotPos = Vector3.zero;
			pivotPosX.text = $"{0:F2}";
			pivotPosY.text = $"{0:F2}";
			pivotPosZ.text = $"{0:F2}";
			pivotAngle = Vector3.zero;
			pivotAngleX.text = $"{0:F2}";
			pivotAngleY.text = $"{0:F2}";
			pivotAngleZ.text = $"{0:F2}";
			cam.controlledState.SetCustomPivot(Vector3.zero, Vector3.zero);
		}
	}

	public void SwitchPivotUp()
	{
		if (!(cam.followingUnit == null))
		{
			cam.controlledState.SwitchPivotUp();
			pivotUp.text = cam.controlledState.GetPivotUp();
		}
	}

	public void ApplyMovement()
	{
		if ((0u | (float.TryParse(camTranslationX.text, out var result) ? 1u : 0u) | (float.TryParse(camTranslationY.text, out var result2) ? 1u : 0u) | (float.TryParse(camTranslationZ.text, out var result3) ? 1u : 0u) | (float.TryParse(camRotationX.text, out var result4) ? 1u : 0u) | (float.TryParse(camRotationY.text, out var result5) ? 1u : 0u) | (float.TryParse(camRotationZ.text, out var result6) ? 1u : 0u)) == 0)
		{
			return;
		}
		camTranslation = new Vector3(result, result2, result3);
		camTranslationX.text = $"{result:F0}";
		camTranslationY.text = $"{result2:F0}";
		camTranslationZ.text = $"{result3:F0}";
		camRotation = new Vector3(result4, result5, result6);
		camRotationX.text = $"{result4:F0}";
		camRotationY.text = $"{result5:F0}";
		camRotationZ.text = $"{result6:F0}";
		if (camTranslation.magnitude != 0f || camRotation.magnitude != 0f)
		{
			if (cam.currentState == cam.controlledState)
			{
				cam.controlledState.SetCustomMovement(camTranslation, camRotation, cancel: false);
				isMoving = true;
			}
			GetValues();
		}
	}

	public void PauseMovement()
	{
		if (cam.currentState == cam.controlledState)
		{
			cam.controlledState.SetCustomMovement(Vector3.zero, Vector3.zero, cancel: false);
			isMoving = false;
		}
	}

	public void CancelMovement()
	{
		if (cam.currentState == cam.controlledState)
		{
			cam.controlledState.SetCustomMovement(Vector3.zero, Vector3.zero, cancel: true);
			isMoving = false;
		}
	}

	public void ReverseMovement()
	{
		if ((0u | (float.TryParse(camTranslationX.text, out var result) ? 1u : 0u) | (float.TryParse(camTranslationY.text, out var result2) ? 1u : 0u) | (float.TryParse(camTranslationZ.text, out var result3) ? 1u : 0u) | (float.TryParse(camRotationX.text, out var result4) ? 1u : 0u) | (float.TryParse(camRotationY.text, out var result5) ? 1u : 0u) | (float.TryParse(camRotationZ.text, out var result6) ? 1u : 0u)) != 0 && (camTranslation.magnitude != 0f || camRotation.magnitude != 0f))
		{
			camTranslation = new Vector3(result, result2, result3);
			camTranslationX.text = $"{result:F0}";
			camTranslationY.text = $"{result2:F0}";
			camTranslationZ.text = $"{result3:F0}";
			camRotation = new Vector3(result4, result5, result6);
			camRotationX.text = $"{result4:F0}";
			camRotationY.text = $"{result5:F0}";
			camRotationZ.text = $"{result6:F0}";
			camTranslation = -camTranslation;
			camRotation = -camRotation;
			if (cam.currentState == cam.controlledState)
			{
				cam.controlledState.SetCustomMovement(camTranslation, camRotation, cancel: false);
				isMoving = true;
			}
		}
	}

	public void GetValues()
	{
		if (cam.followingUnit != null)
		{
			pivotParent.text = cam.followingUnit.UniqueName;
			pivotPos = cam.cameraPivot.localPosition;
			pivotAngle = cam.cameraPivot.localEulerAngles;
		}
		else
		{
			pivotParent.text = "-";
			pivotPos = cam.cameraPivot.position;
			pivotAngle = cam.cameraPivot.eulerAngles;
		}
		camPos = cam.transform.localPosition;
		camAngle = cam.transform.localEulerAngles;
		camFOV.SetValueWithoutNotify(cam.mainCamera.fieldOfView);
		camFOVSpeed.SetValueWithoutNotify(cam.fovChangeSpeed);
		camFOVInertia.SetValueWithoutNotify(cam.fovChangeInertia);
		timeFactor.value = Time.timeScale;
		UpdateValues();
	}

	public void UpdateValues()
	{
		timeFactorValue.text = $"{timeFactor.value:F1}";
		camFOVValue.text = $"{camFOV.value:F0}";
		camFOVSpeedValue.text = $"{100f * camFOVSpeed.value:F0}%";
		camFOVInertiaValue.text = $"{100f * camFOVInertia.value:F0}%";
		camTransSpeedValue.text = $"{camTransSpeed.value:F1}";
		camRotSpeedValue.text = $"{camRotSpeed.value:F1}";
		pivotParent.text = ((cam.followingUnit != null) ? cam.followingUnit.UniqueName : "-");
		pivotPosX.SetTextWithoutNotify($"{pivotPos.x:F2}");
		pivotPosY.SetTextWithoutNotify($"{pivotPos.y:F2}");
		pivotPosZ.SetTextWithoutNotify($"{pivotPos.z:F2}");
		pivotAngleX.SetTextWithoutNotify($"{pivotAngle.x:F0}");
		pivotAngleY.SetTextWithoutNotify($"{pivotAngle.y:F0}");
		pivotAngleZ.SetTextWithoutNotify($"{pivotAngle.z:F0}");
		camPosX.SetTextWithoutNotify($"{camPos.x:F2}");
		camPosY.SetTextWithoutNotify($"{camPos.y:F2}");
		camPosZ.SetTextWithoutNotify($"{camPos.z:F2}");
		camAngleX.SetTextWithoutNotify($"{camAngle.x:F0}");
		camAngleY.SetTextWithoutNotify($"{camAngle.y:F0}");
		camAngleZ.SetTextWithoutNotify($"{camAngle.z:F0}");
	}

	public void SetCamFree()
	{
		cam.SwitchState(cam.freeState);
	}

	public void SetCamCockpit()
	{
		if (!(cam.followingUnit == null) && !(cam.followingUnit.cockpitViewPoint == null))
		{
			cam.SwitchState(cam.cockpitState);
		}
	}

	public void SetCamOrbit()
	{
		if (!(cam.followingUnit == null))
		{
			cam.SwitchState(cam.orbitState);
		}
	}

	public void SetCamChase()
	{
		if (!(cam.followingUnit == null))
		{
			cam.SwitchState(cam.chaseState);
		}
	}

	public void SetCamControlled()
	{
		cam.SwitchState(cam.controlledState);
	}

	public void SetCamFlyby()
	{
		if (!(cam.followingUnit == null))
		{
			cam.SwitchState(cam.TVState);
		}
	}

	public void SetCamState()
	{
		if (cam.currentState == cam.cockpitState)
		{
			cockpitButtonImg.color = Color.green;
			orbitButtonImg.color = Color.white;
			chaseButtonImg.color = Color.white;
			controlledButtonImg.color = Color.white;
			freeButtonImg.color = Color.white;
			flybyButtonImg.color = Color.white;
			InputsPanel.gameObject.SetActive(value: false);
			FOVPanel.gameObject.SetActive(value: true);
			TimePanel.gameObject.SetActive(GameManager.gameState == GameState.SinglePlayer);
			PivotPanel.gameObject.SetActive(value: false);
			CameraPanel.gameObject.SetActive(value: false);
			MovementPanel.gameObject.SetActive(value: false);
		}
		else if (cam.currentState == cam.orbitState)
		{
			cockpitButtonImg.color = Color.white;
			orbitButtonImg.color = Color.green;
			chaseButtonImg.color = Color.white;
			controlledButtonImg.color = Color.white;
			freeButtonImg.color = Color.white;
			flybyButtonImg.color = Color.white;
			InputsPanel.gameObject.SetActive(value: false);
			FOVPanel.gameObject.SetActive(value: true);
			TimePanel.gameObject.SetActive(GameManager.gameState == GameState.SinglePlayer);
			PivotPanel.gameObject.SetActive(value: false);
			CameraPanel.gameObject.SetActive(value: false);
			MovementPanel.gameObject.SetActive(value: false);
			camTarget.text = cam.followingUnit.UniqueName;
		}
		else if (cam.currentState == cam.chaseState)
		{
			cockpitButtonImg.color = Color.white;
			orbitButtonImg.color = Color.white;
			chaseButtonImg.color = Color.green;
			controlledButtonImg.color = Color.white;
			freeButtonImg.color = Color.white;
			flybyButtonImg.color = Color.white;
			InputsPanel.gameObject.SetActive(value: false);
			FOVPanel.gameObject.SetActive(value: true);
			TimePanel.gameObject.SetActive(GameManager.gameState == GameState.SinglePlayer);
			PivotPanel.gameObject.SetActive(value: false);
			CameraPanel.gameObject.SetActive(value: true);
			MovementPanel.gameObject.SetActive(value: false);
			camTarget.text = cam.followingUnit.UniqueName;
			pivotUp.text = "U";
		}
		else if (cam.currentState == cam.controlledState)
		{
			cockpitButtonImg.color = Color.white;
			orbitButtonImg.color = Color.white;
			chaseButtonImg.color = Color.white;
			controlledButtonImg.color = Color.green;
			freeButtonImg.color = Color.white;
			flybyButtonImg.color = Color.white;
			InputsPanel.gameObject.SetActive(value: true);
			FOVPanel.gameObject.SetActive(value: true);
			TimePanel.gameObject.SetActive(GameManager.gameState == GameState.SinglePlayer);
			PivotPanel.gameObject.SetActive(value: true);
			CameraPanel.gameObject.SetActive(value: true);
			MovementPanel.gameObject.SetActive(value: true);
			camTarget.text = (cam.controlledState.IsLookingAt() ? cam.followingUnit.UniqueName : "-");
			pivotUp.text = cam.controlledState.GetPivotUp();
		}
		else if (cam.currentState == cam.freeState)
		{
			cockpitButtonImg.color = Color.white;
			orbitButtonImg.color = Color.white;
			chaseButtonImg.color = Color.white;
			controlledButtonImg.color = Color.white;
			freeButtonImg.color = Color.green;
			flybyButtonImg.color = Color.white;
			InputsPanel.gameObject.SetActive(value: true);
			FOVPanel.gameObject.SetActive(value: true);
			TimePanel.gameObject.SetActive(GameManager.gameState == GameState.SinglePlayer);
			PivotPanel.gameObject.SetActive(value: false);
			CameraPanel.gameObject.SetActive(value: false);
			MovementPanel.gameObject.SetActive(value: false);
			pivotParent.text = "-";
		}
		else if (cam.currentState == cam.TVState)
		{
			cockpitButtonImg.color = Color.white;
			orbitButtonImg.color = Color.white;
			chaseButtonImg.color = Color.white;
			controlledButtonImg.color = Color.white;
			freeButtonImg.color = Color.white;
			flybyButtonImg.color = Color.green;
			InputsPanel.gameObject.SetActive(value: false);
			FOVPanel.gameObject.SetActive(value: false);
			TimePanel.gameObject.SetActive(GameManager.gameState == GameState.SinglePlayer);
			PivotPanel.gameObject.SetActive(value: false);
			CameraPanel.gameObject.SetActive(value: false);
			MovementPanel.gameObject.SetActive(value: false);
		}
		else
		{
			cockpitButtonImg.color = Color.white;
			orbitButtonImg.color = Color.white;
			chaseButtonImg.color = Color.white;
			controlledButtonImg.color = Color.white;
			freeButtonImg.color = Color.white;
			flybyButtonImg.color = Color.white;
			InputsPanel.gameObject.SetActive(value: false);
			FOVPanel.gameObject.SetActive(value: true);
			TimePanel.gameObject.SetActive(GameManager.gameState == GameState.SinglePlayer);
			PivotPanel.gameObject.SetActive(value: false);
			CameraPanel.gameObject.SetActive(value: false);
			MovementPanel.gameObject.SetActive(value: false);
		}
		cockpitButton.interactable = cam.followingUnit != null && cam.followingUnit.cockpitViewPoint != null;
		chaseButton.interactable = cam.followingUnit != null;
		orbitButton.interactable = cam.followingUnit != null;
		flybyButton.interactable = cam.followingUnit != null && cam.followingRB != null && cam.followingRB.velocity.magnitude > 0f;
		GetValues();
	}
}
