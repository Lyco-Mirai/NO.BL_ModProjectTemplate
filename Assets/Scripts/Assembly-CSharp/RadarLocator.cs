using UnityEngine;

public class RadarLocator : MonoBehaviour
{
	[SerializeField]
	private Aircraft aircraft;

	[SerializeField]
	private UnitPart[] essentialParts;

	[SerializeField]
	private bool onlySurface;

	private float rewardAmount;

	private float rewardCount;

	private float rewardThreshold = 1f;

	private void Awake()
	{
		aircraft.onRadarWarning += RadarLocator_OnRadarWarning;
		UnitPart[] array = essentialParts;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].onParentDetached += RadarLocator_OnPartDetached;
		}
	}

	private void RadarLocator_OnRadarWarning(Aircraft.OnRadarWarning source)
	{
		if (!aircraft.IsServer || !(aircraft.NetworkHQ != null))
		{
			return;
		}
		if (onlySurface)
		{
			TypeIdentity typeIdentity = source.emitter.definition.typeIdentity;
			if (typeIdentity.air > 0f || typeIdentity.missile > 0f)
			{
				return;
			}
		}
		if (aircraft.Player != null && source.emitter.NetworkHQ != null && source.emitter.NetworkHQ != aircraft.NetworkHQ)
		{
			float num = 0f;
			if (!aircraft.NetworkHQ.trackingDatabase.ContainsKey(source.emitter.persistentID))
			{
				num = 0.01f;
			}
			else if (!aircraft.NetworkHQ.IsTargetPositionAccurate(source.emitter, 500f))
			{
				num = 0.005f;
			}
			rewardCount += num * Mathf.Sqrt(source.emitter.definition.value);
			rewardAmount += num * Mathf.Sqrt(source.emitter.definition.value);
			if (rewardCount > rewardThreshold)
			{
				aircraft.NetworkHQ.ReportReconAction(aircraft.Player, rewardAmount);
				rewardAmount = 0f;
				rewardCount = 0f;
			}
		}
		aircraft.NetworkHQ.CmdUpdateTrackingInfo(source.emitter.persistentID);
	}

	private void RadarLocator_OnPartDetached(UnitPart part)
	{
		aircraft.onRadarWarning -= RadarLocator_OnRadarWarning;
	}
}
