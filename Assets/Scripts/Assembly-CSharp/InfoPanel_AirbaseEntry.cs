using NuclearOption.UIStyleSystem;
using TMPro;
using UnityEngine;

internal class InfoPanel_AirbaseEntry : MonoBehaviour
{
	private Airbase airbase;

	private InfoPanel_Faction factionInfoPanel;

	[SerializeField]
	private TextMeshProUGUI airbaseName;

	[SerializeField]
	private TextMeshProUGUI helipad;

	[SerializeField]
	private TextMeshProUGUI revetment;

	[SerializeField]
	private TextMeshProUGUI medium;

	[SerializeField]
	private TextMeshProUGUI shelter;

	[SerializeField]
	private TextMeshProUGUI warheads;

	[SerializeField]
	private TextMeshProUGUI carrier;

	private float lastRefresh;

	private float refreshRate = 1f;

	private Color positiveColor = Color.green;

	private void Start()
	{
		InfoPanel_AirbaseEntry_OnThemeGroupChanged();
		ThemeManager.ThemeGroupChanged += InfoPanel_AirbaseEntry_OnThemeGroupChanged;
	}

	private void Awake()
	{
		lastRefresh = Time.timeSinceLevelLoad;
	}

	private void Update()
	{
		if (Time.timeSinceLevelLoad > lastRefresh + refreshRate)
		{
			RefreshAirbase();
			lastRefresh = Time.timeSinceLevelLoad;
		}
	}

	private void OnDestroy()
	{
		ThemeManager.ThemeGroupChanged -= InfoPanel_AirbaseEntry_OnThemeGroupChanged;
	}

	public void SetAirbase(Airbase a, InfoPanel_Faction panel)
	{
		airbase = a;
		airbaseName.text = airbase.SavedAirbase.DisplayName;
		factionInfoPanel = panel;
		RefreshAirbase();
		airbase.onLostControl += Airbase_onCapture;
	}

	private void Airbase_onCapture()
	{
		factionInfoPanel.listAirbases.Remove(airbase);
		airbase.onLostControl -= Airbase_onCapture;
		if (this != null)
		{
			Object.Destroy(base.gameObject);
		}
	}

	public Airbase GetAirbase()
	{
		return airbase;
	}

	public void RefreshAirbase()
	{
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		int num5 = 0;
		int num6 = 0;
		for (int i = 0; i < airbase.hangars.Count; i++)
		{
			if (!airbase.hangars[i].Disabled)
			{
				if (airbase.hangars[i].attachedUnit.definition.code == "HPAD")
				{
					num++;
				}
				else if (airbase.hangars[i].attachedUnit.definition.code == "REV")
				{
					num2++;
				}
				else if (airbase.hangars[i].attachedUnit.definition.code == "HGR-M")
				{
					num3++;
				}
				else if (airbase.hangars[i].attachedUnit.definition.code == "HGR-H")
				{
					num4++;
				}
				else if (airbase.hangars[i].attachedUnit.definition.code == "SHP")
				{
					num5++;
				}
			}
			num6 = airbase.GetWarheads();
		}
		if (helipad != null)
		{
			helipad.text = num.ToString();
			helipad.color = ((num > 0) ? positiveColor : Color.grey);
		}
		if (revetment != null)
		{
			revetment.text = num2.ToString();
			revetment.color = ((num2 > 0) ? positiveColor : Color.grey);
		}
		if (medium != null)
		{
			medium.text = num3.ToString();
			medium.color = ((num3 > 0) ? positiveColor : Color.grey);
		}
		if (shelter != null)
		{
			shelter.text = num4.ToString();
			shelter.color = ((num4 > 0) ? positiveColor : Color.grey);
		}
		if (carrier != null)
		{
			carrier.text = num5.ToString();
			carrier.color = ((num5 > 0) ? positiveColor : Color.grey);
		}
		if (warheads != null)
		{
			warheads.text = num6.ToString();
			warheads.color = ((num6 > 0) ? positiveColor : Color.grey);
		}
	}

	private void InfoPanel_AirbaseEntry_OnThemeGroupChanged()
	{
		positiveColor = ThemeManager.Active.ColorTheme.AllClear;
	}
}
