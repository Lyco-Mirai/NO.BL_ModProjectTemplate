using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MFDScreen : MonoBehaviour
{
	public VirtualMFD virtualMFD;

	public TextMeshProUGUI label;

	public Image highlight;

	public bool isActive;

	public string shortName;

	public GameObject displayPanel;

	public bool aircraftOnly;

	private void Start()
	{
		AdaptScale();
	}

	public void Setup(VirtualMFD mfd, string s)
	{
		virtualMFD = mfd;
		shortName = s;
		label.text = shortName;
	}

	private void AdaptScale()
	{
		if ((float)Screen.width / (float)Screen.height < 1.7f)
		{
			base.transform.localScale = 0.79f * Vector3.one;
		}
		else
		{
			base.transform.localScale = Vector3.one;
		}
	}

	public void CloseScreen(Vector3 posClose)
	{
		displayPanel.SetActive(value: false);
		base.transform.localPosition = posClose;
		highlight.enabled = false;
		isActive = false;
	}

	public void ShowScreen(Vector3 posShow)
	{
		displayPanel.SetActive(value: true);
		base.transform.localPosition = posShow;
		highlight.enabled = true;
		isActive = true;
	}
}
