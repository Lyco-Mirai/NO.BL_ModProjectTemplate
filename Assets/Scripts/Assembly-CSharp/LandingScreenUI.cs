using UnityEngine;
using UnityEngine.UI;

public class LandingScreenUI : MonoBehaviour
{
	[SerializeField]
	private Canvas displayCanvas;

	[SerializeField]
	private Text typeText;

	[SerializeField]
	private Text altitude;

	[SerializeField]
	private Text vert_speed;

	[SerializeField]
	private Text speed;

	[SerializeField]
	private Text rel_speed;

	[SerializeField]
	private Text magText;

	[SerializeField]
	private Text modeText;

	[SerializeField]
	private Image velocity;

	private Camera cam;

	public void SetupCamera(Camera cam, Camera UICam)
	{
		displayCanvas.worldCamera = UICam;
		this.cam = cam;
	}

	private void OnDestroy()
	{
		if (displayCanvas != null)
		{
			Object.Destroy(displayCanvas.gameObject);
		}
	}

	public void SetInfo(float mag, bool IR)
	{
		if (!(displayCanvas == null))
		{
			magText.text = $"Mag x{mag:F1}";
			modeText.text = (IR ? "MODE: IR" : "MODE: COLOR");
		}
	}

	private void Start()
	{
	}

	private void LateUpdate()
	{
		if (SceneSingleton<CombatHUD>.i.aircraft != null)
		{
			altitude.text = "ALT " + UnitConverter.DistanceReading(SceneSingleton<CombatHUD>.i.aircraft.radarAlt);
			speed.text = "SPD " + UnitConverter.SpeedReading(SceneSingleton<CombatHUD>.i.aircraft.speed);
			vert_speed.text = $"V {SceneSingleton<CombatHUD>.i.aircraft.rb.velocity.y:F1}";
			float magnitude = new Vector3(SceneSingleton<CombatHUD>.i.aircraft.rb.velocity.x, 0f, SceneSingleton<CombatHUD>.i.aircraft.rb.velocity.z).magnitude;
			rel_speed.text = $"H {magnitude:F1}";
			if (Vector3.Dot(SceneSingleton<CombatHUD>.i.aircraft.transform.forward, SceneSingleton<CombatHUD>.i.aircraft.rb.velocity) > 0f)
			{
				if (!velocity.enabled)
				{
					velocity.enabled = true;
				}
				Vector3 position = cam.transform.position + SceneSingleton<CombatHUD>.i.aircraft.rb.velocity * 5f;
				Vector3 vector = Vector3.Scale(cam.WorldToScreenPoint(position), new Vector3(1f, 1f, 0f)) - new Vector3(180f, 100f, 0f);
				vector = new Vector3(Mathf.Clamp(vector.x, -180f, 180f), Mathf.Clamp(vector.y, -100f, 100f), 0f);
				velocity.transform.localPosition = vector;
			}
			else if (velocity.enabled)
			{
				velocity.enabled = false;
			}
		}
		else
		{
			base.gameObject.SetActive(value: false);
		}
	}
}
