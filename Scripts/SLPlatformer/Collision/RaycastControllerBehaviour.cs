using UnityEngine;

using Unity.Entities;

namespace SLPlatformer {
	
	[AddComponentMenu("Raycast Controllers/Raycast Controller Base")]
	[RequireComponent(typeof(BoxCollider2D))]
	public class RaycastControllerBehaviour : ComponentDataWrapper<UpdateRaycastController> {}
}