using NuclearOption.SavedMission.Objectives;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts
{
	[CreateAssetMenu(menuName = "NuclearOption/UI Prefabs")]
	public class UIPrefabs : ScriptableObject
	{
		public TextMeshProUGUI TextPrefab;

		public RectTransform GroupBoxPrefab;

		public Vector3DataField VectorFieldPrefab;

		public FloatDataField FloatFieldPrefab;

		public StringDataField StringFieldPrefab;

		public BoolDataField BoolFieldPrefab;

		public ReferenceList ReferenceListPrefab;

		public ReferenceDataField ReferenceDataPrefab;

		public EmptyDataList DataListPrefab;

		public DropdownDataField Dropdown;

		public WaypointObjectiveHandle WaypointEditor;

		public OverrideDataField OverrideField;

		public FactionDataField FactionDataPrefab;

		public HorizontalLayoutGroup HorizontalGroupPrefab;

		public VerticalLayoutGroup VerticalGroupPrefab;

		public float GroupBoxPadding = 20f;
	}
}
