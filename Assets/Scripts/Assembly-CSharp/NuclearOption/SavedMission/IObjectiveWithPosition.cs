using System.Collections.Generic;

namespace NuclearOption.SavedMission
{
	public interface IObjectiveWithPosition
	{
		IReadOnlyList<ObjectivePosition> Positions { get; }
	}
}
