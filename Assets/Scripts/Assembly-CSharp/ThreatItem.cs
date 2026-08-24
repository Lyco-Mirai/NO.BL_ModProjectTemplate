using NuclearOption.UIStyleSystem;
using UnityEngine;
using UnityEngine.UI;

public class ThreatItem : MonoBehaviour
{
	[SerializeField]
	private Text text;

	[SerializeField]
	private GameObject vectorLinePrefab;

	private Missile missile;

	private GameObject notchLine;

	private GameObject vectorLine;

	private GameObject notchIndicator;

	private Image notchIndicatorBox;

	private Text notchIndicatorLabel;

	private Transform playerAircraftIconTransform;

	private Transform missileIconTransform;

	public void SetItem(PersistentID missileID)
	{
		missile = (UnitRegistry.TryGetUnit(missileID, out var unit) ? (unit as Missile) : null);
		base.gameObject.SetActive(value: false);
	}

	public bool FoundIcon()
	{
		if (missileIconTransform != null)
		{
			return true;
		}
		if (!DynamicMap.TryGetMapIcon(SceneSingleton<CombatHUD>.i.aircraft, out var mapIcon) || !DynamicMap.TryGetMapIcon(missile, out var mapIcon2))
		{
			return false;
		}
		playerAircraftIconTransform = mapIcon.transform;
		missileIconTransform = mapIcon2.transform;
		if (missile.GetSeekerType() == "ARH" || missile.GetSeekerType() == "SARH")
		{
			notchLine = SceneSingleton<DynamicMap>.i.ShowNotchLine();
			notchIndicator = SceneSingleton<CombatHUD>.i.ShowNotchIndicator();
			notchIndicatorBox = notchIndicator.GetComponent<Image>();
			notchIndicatorLabel = notchIndicator.GetComponentInChildren<Text>();
		}
		vectorLine = Object.Instantiate(vectorLinePrefab, SceneSingleton<DynamicMap>.i.iconLayer.transform);
		vectorLine.GetComponent<Image>().color = ThemeManager.Active.ColorTheme.MapIconHostile.WithAlpha(0.5f);
		vectorLine.SetActive(value: false);
		base.gameObject.SetActive(value: true);
		return true;
	}

	public void AnimateItem()
	{
		if (!FoundIcon())
		{
			base.gameObject.SetActive(value: false);
			return;
		}
		if (missile.seekerMode == Missile.SeekerMode.activeSearch)
		{
			text.color = ThemeManager.Active.ColorTheme.Warning;
			vectorLine.SetActive(value: false);
		}
		else
		{
			text.color = ThemeManager.Active.ColorTheme.Alert + ThemeManager.Active.ColorTheme.AllClear * Mathf.Sin(Time.realtimeSinceStartup * 20f);
			AlignVectorLine();
		}
		text.text = "Missile [" + missile.GetSeekerType() + "] " + UnitConverter.DistanceReading(Vector3.Distance(SceneSingleton<CombatHUD>.i.aircraft.transform.position, missile.transform.position));
		if (!(notchLine == null) && !(playerAircraftIconTransform == null))
		{
			Vector3 evasionVector = missile.GetEvasionPoint() - SceneSingleton<CombatHUD>.i.aircraft.GlobalPosition();
			AlignNotchLine(evasionVector);
			AlignNotchIndicator(evasionVector);
		}
	}

	private void OnDisable()
	{
		if (notchLine != null)
		{
			notchLine.SetActive(value: false);
		}
		if (vectorLine != null)
		{
			vectorLine.SetActive(value: false);
		}
	}

	private void OnEnable()
	{
		if (notchLine != null)
		{
			notchLine.SetActive(value: true);
		}
	}

	private void OnDestroy()
	{
		if (notchLine != null)
		{
			Object.Destroy(notchLine);
		}
		if (vectorLine != null)
		{
			Object.Destroy(vectorLine);
		}
		if (notchIndicator != null)
		{
			Object.Destroy(notchIndicator);
		}
	}

	private void AlignVectorLine()
	{
		if (!(missileIconTransform == null) && !(playerAircraftIconTransform == null))
		{
			vectorLine.SetActive(value: true);
			vectorLine.transform.position = playerAircraftIconTransform.position;
			Vector3 vector = missileIconTransform.position - playerAircraftIconTransform.position;
			float z = (0f - Mathf.Atan2(vector.x, vector.y)) * 57.29578f;
			vectorLine.transform.eulerAngles = new Vector3(0f, 0f, z);
			vectorLine.transform.localScale = (Vector3.one + Vector3.up * vector.magnitude) / SceneSingleton<DynamicMap>.i.iconLayer.transform.lossyScale.x;
		}
	}

	private void AlignNotchLine(Vector3 evasionVector)
	{
		Vector3 rhs = Vector3.Cross(evasionVector, SceneSingleton<CombatHUD>.i.aircraft.rb.velocity);
		Vector3 vector = Vector3.Cross(missile.GetEvasionPoint() - SceneSingleton<CombatHUD>.i.aircraft.GlobalPosition(), rhs);
		if (Vector3.Dot(SceneSingleton<CombatHUD>.i.aircraft.transform.forward, vector) < 0f)
		{
			vector *= -1f;
		}
		vector.y = 0f;
		Quaternion quaternion = Quaternion.LookRotation(vector, Vector3.up);
		notchLine.transform.position = playerAircraftIconTransform.position;
		notchLine.transform.eulerAngles = new Vector3(0f, 0f, SceneSingleton<DynamicMap>.i.mapImage.transform.eulerAngles.z - quaternion.eulerAngles.y);
	}

	private void AlignNotchIndicator(Vector3 evasionVector)
	{
		if (!(notchIndicator == null))
		{
			Vector3 to = new Vector3(evasionVector.x, 0f, evasionVector.z);
			Vector3 rhs = Vector3.Cross(evasionVector, SceneSingleton<CombatHUD>.i.aircraft.rb.velocity);
			Vector3 vector = Vector3.Cross(evasionVector, rhs);
			if (Vector3.Dot(SceneSingleton<CombatHUD>.i.aircraft.transform.forward, vector) < 0f)
			{
				vector *= -1f;
			}
			Vector3 b = new Vector3(1f, 1f, 0f);
			GlobalPosition position = SceneSingleton<CombatHUD>.i.aircraft.GlobalPosition() + 1000f * vector;
			float num = Vector3.SignedAngle(evasionVector, to, Vector3.up);
			notchIndicator.transform.position = Vector3.Scale(SceneSingleton<CameraStateManager>.i.mainCamera.WorldToScreenPoint(position.ToLocalPosition()), b);
			notchIndicator.transform.eulerAngles = new Vector3(0f, 0f, num - SceneSingleton<CombatHUD>.i.aircraft.transform.eulerAngles.z);
			float distance = FastMath.Distance(SceneSingleton<CombatHUD>.i.aircraft.transform.position, missile.transform.position);
			Color color = Color.green;
			if (missile.seekerMode == Missile.SeekerMode.activeLock)
			{
				color = Color.Lerp(Color.yellow, Color.red, Mathf.Sin(Time.timeSinceLevelLoad * 20f) + 0.5f);
			}
			else if (missile.seekerMode == Missile.SeekerMode.activeSearch)
			{
				color = Color.Lerp(Color.green, Color.yellow, Mathf.Sin(Time.timeSinceLevelLoad * 10f) + 0.5f);
			}
			notchIndicatorBox.color = color;
			notchIndicatorLabel.text = "[" + missile.GetSeekerType() + "] " + UnitConverter.DistanceReading(distance);
			notchIndicatorLabel.color = color;
		}
	}
}
