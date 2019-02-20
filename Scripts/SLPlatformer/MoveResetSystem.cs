using Unity.Entities;

namespace SLPlatformer {
	[UpdateAfter(typeof(MoveFinalizerGroup))]
	public sealed class MoveResetSystem : ComponentSystem {
		struct Data {
			public ComponentDataArray<Movement> movement;
		}
		[Inject] Data data;

		protected override void OnUpdate() {
			for (int i = 0; i < data.movement.Length; i++) {
				data.movement[i] = new Movement();
			}
		}
	}
	
}