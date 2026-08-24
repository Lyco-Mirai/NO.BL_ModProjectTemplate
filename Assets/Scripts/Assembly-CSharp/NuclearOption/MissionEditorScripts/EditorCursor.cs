using System;
using System.Collections.Generic;
using UnityEngine;

namespace NuclearOption.MissionEditorScripts
{
	public class EditorCursor : MonoBehaviour
	{
		[Serializable]
		public enum ColorMode
		{
			OnSelect = 0,
			OnHover = 1
		}

		[SerializeField]
		private UnitSelection unitSelection;

		[SerializeField]
		private Renderer _renderer;

		[SerializeField]
		private Renderer heightBarRenderer;

		[SerializeField]
		private Renderer groundMarkerRenderer;

		[SerializeField]
		private ColorMode colorMode;

		[SerializeField]
		private bool multiSelect;

		[SerializeField]
		private bool disableIfNoneSelected;

		[SerializeField]
		private bool followUnit;

		[Header("Hover Colors")]
		[SerializeField]
		private Color noUnit;

		[SerializeField]
		private Color noFaction;

		[SerializeField]
		private float emissionMultiplier = 1f;

		private Unit unit;

		private Material material;

		[NonSerialized]
		private readonly List<EditorCursor> multiSelectClones = new List<EditorCursor>(0);

		public bool IsClone { get; private set; }

		private void Awake()
		{
			material = _renderer.material;
			if ((bool)heightBarRenderer)
			{
				heightBarRenderer.sharedMaterial = material;
			}
			if ((bool)groundMarkerRenderer)
			{
				groundMarkerRenderer.sharedMaterial = material;
			}
		}

		private void Start()
		{
			if (!IsClone)
			{
				switch (colorMode)
				{
				case ColorMode.OnSelect:
					unitSelection.OnSelect += UnitSelection_OnSelect;
					break;
				case ColorMode.OnHover:
					unitSelection.OnHover += UnitSelection_OnSelect;
					break;
				}
				UnitSelection_OnSelect(unitSelection.SelectionDetails);
			}
		}

		private void OnDestroy()
		{
			switch (colorMode)
			{
			case ColorMode.OnSelect:
				unitSelection.OnSelect -= UnitSelection_OnSelect;
				break;
			case ColorMode.OnHover:
				unitSelection.OnHover -= UnitSelection_OnSelect;
				break;
			}
			DestroyClones();
		}

		private void DestroyClones()
		{
			foreach (EditorCursor multiSelectClone in multiSelectClones)
			{
				UnityEngine.Object.Destroy(multiSelectClone.gameObject);
			}
			multiSelectClones.Clear();
		}

		private void UnitSelection_OnSelect(SelectionDetails selectionDetails)
		{
			DestroyClones();
			if (multiSelect && selectionDetails is MultiSelectSelectionDetails multiSelectSelectionDetails && multiSelectSelectionDetails.SelectionType == typeof(UnitSelectionDetails))
			{
				if (disableIfNoneSelected)
				{
					base.gameObject.SetActive(value: true);
				}
				MultiSelect(multiSelectSelectionDetails);
			}
			else if (selectionDetails is UnitSelectionDetails unitSelectionDetails)
			{
				if (disableIfNoneSelected)
				{
					base.gameObject.SetActive(value: true);
				}
				SetUnitColor(unitSelectionDetails.Unit, unitSelectionDetails.Faction);
			}
			else
			{
				if (disableIfNoneSelected)
				{
					base.gameObject.SetActive(value: false);
				}
				SetUnitColor(null, null);
			}
		}

		private void MultiSelect(MultiSelectSelectionDetails multi)
		{
			UnitSelectionDetails unitSelectionDetails = (UnitSelectionDetails)multi.Items[0];
			SetUnitColor(unitSelectionDetails.Unit, unitSelectionDetails.Faction);
			int num = multi.Items.Count - 1;
			for (int i = 0; i < num; i++)
			{
				EditorCursor editorCursor = UnityEngine.Object.Instantiate(this, base.transform.parent);
				editorCursor.gameObject.SetActive(value: true);
				multiSelectClones.Add(editorCursor);
				editorCursor.IsClone = true;
				UnitSelectionDetails unitSelectionDetails2 = (UnitSelectionDetails)multi.Items[i + 1];
				editorCursor.SetUnitColor(unitSelectionDetails2.Unit, unitSelectionDetails2.Faction);
			}
		}

		public void SetUnitColor(Unit unit, Faction faction)
		{
			Color color = ((unit == null) ? noUnit : ((!(faction == null)) ? faction.color : noFaction));
			material.SetColor("_EmissionColor", color * emissionMultiplier);
			if (followUnit)
			{
				if (unit != null)
				{
					this.unit = unit;
					_renderer.transform.localScale = Vector3.one * (unit.definition.length / 7f);
				}
				else
				{
					this.unit = null;
				}
			}
		}

		private void LateUpdate()
		{
			if (followUnit)
			{
				MoveSelectionSquare();
			}
		}

		private void MoveSelectionSquare()
		{
			if (unit == null)
			{
				return;
			}
			unit.transform.GetPositionAndRotation(out var position, out var rotation);
			Quaternion rotation2 = Quaternion.Euler(0f, rotation.eulerAngles.y, 0f);
			base.transform.SetPositionAndRotation(position, rotation2);
			if (!TryGetGroundHit(out var hit))
			{
				if ((bool)heightBarRenderer)
				{
					heightBarRenderer.enabled = false;
				}
				if ((bool)groundMarkerRenderer)
				{
					groundMarkerRenderer.enabled = false;
				}
			}
			else if ((bool)heightBarRenderer && (bool)groundMarkerRenderer)
			{
				heightBarRenderer.enabled = true;
				groundMarkerRenderer.enabled = true;
				Vector3 vector = base.transform.InverseTransformPoint(hit.point);
				float num = (0f - vector.y) * 0.5f;
				Camera mainCamera = SceneSingleton<CameraStateManager>.i.mainCamera;
				float num2 = Vector3.Dot(hit.point - mainCamera.transform.position, mainCamera.transform.forward);
				float num3 = Mathf.Lerp(1f, 20f, num2 / (num2 + 3000f));
				float num4 = 0.05f * num3;
				heightBarRenderer.transform.localPosition = Vector3.down * num;
				heightBarRenderer.transform.localScale = new Vector3(num4, num, num4);
				groundMarkerRenderer.transform.localPosition = vector + unit.definition.spawnOffset / 2f;
				groundMarkerRenderer.transform.localScale = Vector3.one * num3;
			}
		}

		private bool TryGetGroundHit(out RaycastHit hit)
		{
			Ray ray = new Ray(base.transform.position, Vector3.down);
			int layerMask = (int)PhysicsLayers.StaticsMask | (int)PhysicsLayers.ShipsMask | PhysicsLayers.Water;
			float num = 0f;
			hit = default(RaycastHit);
			for (int i = 0; i < 3; i++)
			{
				if (!Physics.Raycast(ray, out hit, 10000f - num, layerMask))
				{
					break;
				}
				hit.distance += num;
				if (!hit.collider.transform.IsChildOf(unit.transform))
				{
					break;
				}
				num = hit.distance + 0.01f;
				ray.origin = base.transform.position + Vector3.down * num;
				hit = default(RaycastHit);
			}
			bool flag = hit.collider != null;
			if (Datum.WaterPlane().Raycast(new Ray(base.transform.position, Vector3.down), out var enter) && enter < 10000f && (!flag || enter < hit.distance))
			{
				hit.point = base.transform.position + Vector3.down * enter;
				hit.normal = Vector3.up;
				hit.distance = enter;
				return true;
			}
			return flag;
		}
	}
}
