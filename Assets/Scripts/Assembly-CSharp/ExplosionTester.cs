using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ExplosionTester : MonoBehaviour
{
	[SerializeField]
	private TMP_Text blastYieldDisplay;

	[SerializeField]
	private TMP_InputField xCoords;

	[SerializeField]
	private TMP_InputField yCoords;

	[SerializeField]
	private TMP_InputField zCoords;

	[SerializeField]
	private GameObject explosionPointPrefab;

	[SerializeField]
	private Slider yieldSlider;

	private GameObject explosionPoint;

	private void Awake()
	{
		xCoords.SetTextWithoutNotify($"{0}");
		yCoords.SetTextWithoutNotify($"{0}");
		zCoords.SetTextWithoutNotify($"{0}");
	}

	private void Start()
	{
		base.gameObject.SetActive(PlayerSettings.debugVis);
		if (!base.gameObject.activeSelf)
		{
			base.enabled = false;
			return;
		}
		explosionPoint = Object.Instantiate(explosionPointPrefab, null);
		explosionPoint.transform.position = Vector3.zero;
	}

	public void UIInput()
	{
		blastYieldDisplay.text = $"{yieldSlider.value * yieldSlider.value:F2} kg TNT";
		explosionPoint.transform.position = new Vector3(float.Parse(xCoords.text), float.Parse(yCoords.text), float.Parse(zCoords.text));
	}

	private void Update()
	{
		if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
		{
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			if (Physics.Raycast(ray, out var hitInfo, 100000f, ~(int)PhysicsLayers.ExclusionZonesMask))
			{
				explosionPoint.transform.position = hitInfo.point - ray.direction.normalized * 0.2f;
				xCoords.text = $"{explosionPoint.transform.position.x:F2}";
				yCoords.text = $"{explosionPoint.transform.position.y:F2}";
				zCoords.text = $"{explosionPoint.transform.position.z:F2}";
			}
		}
	}

	public void Detonate()
	{
		DamageEffects.BlastFrag(yieldSlider.value * yieldSlider.value, explosionPoint.transform.position, PersistentID.None, PersistentID.None);
	}
}
