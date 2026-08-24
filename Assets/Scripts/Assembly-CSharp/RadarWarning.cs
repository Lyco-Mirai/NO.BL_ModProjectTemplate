using System.Collections.Generic;
using NuclearOption.UIStyleSystem;
using UnityEngine;
using UnityEngine.UI;

public class RadarWarning : MonoBehaviour
{
	private class RadarWarningIcon
	{
		private readonly Image image;

		private readonly Vector3 direction;

		private readonly float creationTime;

		private readonly Color color;

		private readonly float life;

		public RadarWarningIcon(Vector3 direction, GameObject iconPrefab, Transform iconLayer, Color color, float life)
		{
			this.direction = direction.normalized;
			this.color = color;
			image = NetworkSceneSingleton<Spawner>.i.SpawnLocal(iconPrefab, iconLayer).GetComponent<Image>();
			creationTime = Time.timeSinceLevelLoad;
			this.life = life;
		}

		public bool Position()
		{
			Transform transform = SceneSingleton<CameraStateManager>.i.transform;
			image.enabled = Vector3.Dot(direction, transform.forward) > 0f;
			image.transform.position = Vector3.Scale(SceneSingleton<CameraStateManager>.i.mainCamera.WorldToScreenPoint(transform.position + direction * 10000f), new Vector3(1f, 1f, 0f));
			float num = Time.timeSinceLevelLoad - creationTime;
			image.color = color * (1f - num / life);
			if (num >= 4f)
			{
				NetworkSceneSingleton<Spawner>.i.DestroyLocal(image.gameObject, 0f);
				return false;
			}
			return true;
		}

		public void Remove()
		{
			NetworkSceneSingleton<Spawner>.i.DestroyLocal(image.gameObject, 0f);
		}
	}

	private class JammingIcon
	{
		private readonly Image image;

		public readonly Unit unit;

		private float lastJam;

		private readonly Text text;

		public JammingIcon(Unit unit, GameObject iconPrefab, Transform iconLayer, Color color)
		{
			this.unit = unit;
			image = NetworkSceneSingleton<Spawner>.i.SpawnLocal(iconPrefab, iconLayer).GetComponent<Image>();
			text = image.gameObject.GetComponentInChildren<Text>();
			image.color = color;
			lastJam = Time.timeSinceLevelLoad;
		}

		public void Refresh()
		{
			lastJam = Time.timeSinceLevelLoad;
		}

		public bool Position()
		{
			Transform transform = SceneSingleton<CameraStateManager>.i.transform;
			Vector3 lhs = unit.transform.position - transform.position;
			image.enabled = Vector3.Dot(lhs, transform.forward) > 0f;
			text.enabled = image.enabled;
			image.transform.position = Vector3.Scale(SceneSingleton<CameraStateManager>.i.mainCamera.WorldToScreenPoint(unit.transform.position), new Vector3(1f, 1f, 0f));
			if (Time.timeSinceLevelLoad - lastJam > 1f)
			{
				NetworkSceneSingleton<Spawner>.i.DestroyLocal(image.gameObject, 0f);
				return false;
			}
			return true;
		}

		public void Remove()
		{
			NetworkSceneSingleton<Spawner>.i.DestroyLocal(image.gameObject, 0f);
		}
	}

	private Aircraft aircraft;

	[SerializeField]
	private AudioClip radarWarningExisting;

	[SerializeField]
	private AudioClip radarWarningNew;

	[SerializeField]
	private GameObject radarWarningIconPrefab;

	[SerializeField]
	private GameObject jammingIconPrefab;

	private readonly List<RadarWarningIcon> radarWarningIcons = new List<RadarWarningIcon>();

	private readonly List<JammingIcon> jammingIcons = new List<JammingIcon>();

	private readonly Dictionary<Unit, JammingIcon> jammingIconLookup = new Dictionary<Unit, JammingIcon>();

	private void Start()
	{
		aircraft = SceneSingleton<CombatHUD>.i.aircraft;
		aircraft.onDisableUnit += RadarWarning_OnUnitDisable;
		aircraft.onRadarWarning += RadarWarning_OnRadarWarning;
		aircraft.onJam += RadarWarning_OnJammed;
	}

	private void OnDestroy()
	{
		aircraft.onDisableUnit -= RadarWarning_OnUnitDisable;
		aircraft.onRadarWarning -= RadarWarning_OnRadarWarning;
		foreach (RadarWarningIcon radarWarningIcon in radarWarningIcons)
		{
			radarWarningIcon.Remove();
		}
		foreach (JammingIcon jammingIcon in jammingIcons)
		{
			jammingIcon.Remove();
		}
	}

	private void RadarWarning_OnUnitDisable(Unit unit)
	{
		Object.Destroy(base.gameObject);
	}

	private void ShowDirectionalWarning(Vector3 direction)
	{
		radarWarningIcons.Add(new RadarWarningIcon(direction, radarWarningIconPrefab, SceneSingleton<CombatHUD>.i.iconLayer, Color.Lerp(ThemeManager.Active.ColorTheme.HudUnitFlash, ThemeManager.Active.ColorTheme.HudUnitHostile, 0.5f), 4f));
		base.enabled = true;
	}

	private void RadarWarning_OnRadarWarning(Aircraft.OnRadarWarning source)
	{
		SoundManager.PlayRadarWarningOneShot(aircraft.KnownRadarWarning(source.emitter) ? radarWarningExisting : radarWarningNew);
		SceneSingleton<DynamicMap>.i.ShowRadarPing(source);
		if (source.detected)
		{
			if (SceneSingleton<CombatHUD>.i.MarkerExists(source.emitter))
			{
				SceneSingleton<CombatHUD>.i.HighlightMarker(source.emitter);
			}
			else
			{
				ShowDirectionalWarning(source.emitter.transform.position - aircraft.transform.position);
			}
		}
	}

	private void RadarWarning_OnJammed(Unit.JamEventArgs e)
	{
		base.enabled = true;
		if (!jammingIconLookup.ContainsKey(e.jammingUnit))
		{
			JammingIcon jammingIcon = new JammingIcon(e.jammingUnit, jammingIconPrefab, SceneSingleton<CombatHUD>.i.iconLayer, Color.Lerp(ThemeManager.Active.ColorTheme.HudUnitFlash, ThemeManager.Active.ColorTheme.HudUnitHostile, 0.5f));
			jammingIcons.Add(jammingIcon);
			jammingIconLookup.Add(e.jammingUnit, jammingIcon);
		}
		else
		{
			jammingIconLookup[e.jammingUnit].Refresh();
		}
	}

	private void Update()
	{
		if (radarWarningIcons.Count == 0 && jammingIcons.Count == 0)
		{
			base.enabled = false;
			return;
		}
		for (int num = radarWarningIcons.Count - 1; num >= 0; num--)
		{
			if (!radarWarningIcons[num].Position())
			{
				radarWarningIcons.RemoveAt(num);
			}
		}
		for (int num2 = jammingIcons.Count - 1; num2 >= 0; num2--)
		{
			if (!jammingIcons[num2].Position())
			{
				jammingIconLookup.Remove(jammingIcons[num2].unit);
				jammingIcons.RemoveAt(num2);
			}
		}
	}
}
