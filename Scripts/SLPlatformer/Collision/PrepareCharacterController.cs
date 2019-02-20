using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;

namespace SLPlatformer {
	[UpdateBefore(typeof(CharacterControllerSystem))]
	public class CharacterControllerPrepare : ComponentSystem {
		struct UnpreparedData {
			public readonly int Length;
			[ReadOnly]
			public SharedComponentDataArray<CharacterControllerComponent> characterController;

			[ReadOnly]
			public ComponentDataArray<Movement> movement;
			[ReadOnly]
			public ComponentDataArray<RaycastControllerComponent> raycastController;

			[ReadOnly]
			public SubtractiveComponent<CollisionInfo> collisions;

			public EntityArray entities;
		}
		[Inject] UnpreparedData data;

		protected override void OnUpdate () {
			//Prepare all components without the correct data.
			for (int i = 0; i < data.Length; i++) {
				//Add the collision info. (NOTE: this does mean no
				//archetyping CollisionInfo in!)
				CollisionInfo cols = new CollisionInfo();
				cols.faceDirection = 1;
				PostUpdateCommands.AddComponent(data.entities[i], cols);

				//Configure inverseMaxSlope to be truly inverse of maxSlopeAngle.
				//CharacterControllerComponent controller = data.characterController[i];
				//If inverseMaxSlope is set, don't do the calculation again.
				//if (controller.maxSlopeInverse == default(float)) {
				//	controller.maxSlopeInverse = 90 / controller.maxSlopeAngle;
				//	PostUpdateCommands.SetSharedComponent(data.entities[i], controller);
				//}
			}
		}
	}
}