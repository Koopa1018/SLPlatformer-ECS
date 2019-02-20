using UnityEngine;
using Unity.Entities;

namespace SLPlatformer {
	[System.Serializable]
	public struct CollisionMask : IComponentData {
		public LayerMask SolidMask;
		public LayerMask SemiSolidMask;
	}
	public class CollisionMaskComponent : ComponentDataWrapper<CollisionMask> {}
}