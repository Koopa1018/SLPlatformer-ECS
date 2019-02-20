using Unity.Entities;
using Unity.Mathematics;

namespace SLPlatformer {
	public enum SlopeState {None = 0, Climbing, Descending, SlidingDown};

	public struct CollisionInfo : IComponentData {
		public bool2b vCollisions;
		public bool2b hCollisions;

		public int faceDirection;

		public float slopeAngle, slopeAngleOld;
		public float2 slopeNormal;

		public SlopeState slopeState;
		public bool1b slidingDownSlope;

		public bool left {
			get {
				return hCollisions.x;
			} set {
				hCollisions.x = value;
			}
		}
		public bool right {
			get {
				return hCollisions.y;
			} set {
				hCollisions.y = value;
			}
		}
		public bool above {
			get {
				return vCollisions.x;
			} set {
				vCollisions.x = value;
			}
		}
		public bool below {
			get {
				return vCollisions.y;
			} set {
				vCollisions.y = value;
			}
		}

		public void Reset () {
			vCollisions = false;
			hCollisions = false;
			
			slopeAngleOld = slopeAngle;
			slopeAngle = 0;
			slopeNormal = float2.zero;
			slopeState = SlopeState.None;
			
			slidingDownSlope = false;
		}
	}
}