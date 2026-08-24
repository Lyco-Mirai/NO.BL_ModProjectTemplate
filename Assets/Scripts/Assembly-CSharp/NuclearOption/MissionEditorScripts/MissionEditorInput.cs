using NuclearOption.NodeGraph;
using Rewired;
using RuntimeHandle;
using UnityEngine;

namespace NuclearOption.MissionEditorScripts
{
	public class MissionEditorInput : MonoBehaviour
	{
		[SerializeField]
		private EditorTabs editorTabs;

		[SerializeField]
		private UnitSelection unitSelection;

		private Player player;

		public static bool IsEscapeDown => Input.GetKeyDown(KeyCode.Escape);

		public static bool IsShift => Input.GetKey(KeyCode.LeftShift);

		public static bool Isctrl => Input.GetKey(KeyCode.LeftControl);

		public static bool IsAlt => Input.GetKey(KeyCode.LeftAlt);

		public static bool IsCDown => Input.GetKeyDown(KeyCode.C);

		public static bool IsVDown => Input.GetKeyDown(KeyCode.V);

		public static bool IsMouse0 => Input.GetMouseButton(0);

		public static bool IsMouse0Down => Input.GetMouseButtonDown(0);

		public static Vector2 MousePosition => Input.mousePosition;

		public static Vector2 MouseScrollDelta => Input.mouseScrollDelta;

		private void Awake()
		{
			player = ReInput.players.GetPlayer(0);
		}

		private void Update()
		{
			if (InputFieldChecker.InsideInputField || GraphEditor.i != null)
			{
				return;
			}
			if (player.GetButtonDown("FocusUnit"))
			{
				unitSelection.SelectionDetails?.Focus();
			}
			if (player.GetButtonDown("DeleteUnit"))
			{
				SelectionDetails selectionDetails = unitSelection.SelectionDetails;
				if (selectionDetails != null && selectionDetails.Delete())
				{
					unitSelection.ClearSelection();
				}
			}
			if (player.GetButtonDown("SelectUnitMode"))
			{
				unitSelection.handle.SetMode(HandleType.NONE);
			}
			else if (player.GetButtonDown("TranslateUnitMode"))
			{
				unitSelection.handle.SetMode(HandleType.POSITION);
			}
			else if (player.GetButtonDown("RotateUnitMode"))
			{
				unitSelection.handle.SetMode(HandleType.ROTATION);
			}
			else if (player.GetButtonDown("ToggleLeftPanel"))
			{
				editorTabs.ToggleLeftPanel();
			}
		}
	}
}
