using UnityEngine;

namespace NuclearOption.Jobs
{
	public struct SampleGroundResult
	{
		public bool didHit;

		public Vector3 hitNormal;

		public Vector3 hitPoint;

		public bool hasHitRB;

		public bool onPaved;

		public Vector3 hitPointVelocity;
	}
}
