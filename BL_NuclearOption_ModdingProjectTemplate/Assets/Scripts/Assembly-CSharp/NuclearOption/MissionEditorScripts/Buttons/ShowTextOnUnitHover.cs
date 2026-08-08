using UnityEngine;

namespace NuclearOption.MissionEditorScripts.Buttons
{
	public class ShowTextOnUnitHover : MonoBehaviour
	{
		[SerializeField]
		private UnitSelection unitSelection;

		[SerializeField]
		private HoverText hoverText;

		private SingleSelectionDetails details;

		private Camera _camera;

		private void Awake()
		{
			unitSelection.OnHover += OnHover;
			_camera = Camera.main;
		}

		private void OnDestroy()
		{
			unitSelection.OnHover -= OnHover;
		}

		private void OnHover(SingleSelectionDetails details)
		{
			this.details = details;
			if (details != null)
			{
				hoverText.Show(this, details.DisplayName);
			}
			else
			{
				hoverText.Hide(this);
			}
		}

		private void Update()
		{
			if (details != null && details.PositionWrapper != null)
			{
				Vector3 position = details.PositionWrapper.Value.ToLocalPosition();
				Vector3 vector = _camera.WorldToScreenPoint(position);
				hoverText.Move(this, vector);
			}
		}
	}
}
