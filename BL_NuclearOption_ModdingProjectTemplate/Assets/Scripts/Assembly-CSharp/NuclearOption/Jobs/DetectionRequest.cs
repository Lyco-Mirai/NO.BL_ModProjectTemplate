using UnityEngine;

namespace NuclearOption.Jobs
{
	public readonly struct DetectionRequest
	{
		public readonly TargetDetector Detector;

		public readonly Unit target;

		private readonly Transform scanPoint;

		private readonly IRadarReturn radarReturn;

		private readonly float dist;

		private readonly float clutterFactor;

		public DetectionRequest(TargetDetector detector, Unit target, IRadarReturn radarReturn, float dist, float clutterFactor)
		{
			Detector = detector;
			this.target = target;
			this.radarReturn = radarReturn;
			this.dist = dist;
			this.clutterFactor = clutterFactor;
			scanPoint = detector.GetScanPoint();
		}

		public RaycastCommand GetRaycastCommand()
		{
			Vector3 position = scanPoint.position;
			Vector3 vector = target.transform.position + 0.4f * target.definition.height * Vector3.up;
			Vector3 direction = FastMath.NormalizedDirection(position, vector);
			QueryParameters queryParameters = new QueryParameters(PhysicsLayers.StaticsMask, hitMultipleFaces: false, QueryTriggerInteraction.Ignore);
			float distance = FastMath.Distance(position, vector);
			return new RaycastCommand(position, direction, queryParameters, distance);
		}

		public void ProcessResult(RaycastHit hit)
		{
			if (target == null || target.disabled || Detector == null || (hit.collider != null && FastMath.OutOfRange(hit.point.ToGlobalPosition(), target.GlobalPosition(), target.maxRadius * 1.5f)))
			{
				return;
			}
			if (radarReturn != null)
			{
				Radar radar = Detector as Radar;
				if (radar.CanSeeRadarReturn(radarReturn, dist, clutterFactor))
				{
					radar.DetectTarget(target);
				}
			}
			else
			{
				Detector.DetectTarget(target);
			}
		}
	}
}
