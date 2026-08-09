using UnityEngine;

namespace NuclearOption.Jobs
{
	public struct ShipPartFields
	{
		public Vector3 velocity;

		public Vector3 directionalDrag;

		public float partHeight;

		public float displacement;

		public float mass;

		public Vector3 forcePosition;

		public Vector3 force;

		public float submergedAmount;

		internal Vector3 centerOfMassWorld;
	}
}
