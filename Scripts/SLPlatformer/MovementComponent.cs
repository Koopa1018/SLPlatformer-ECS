using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;

namespace SLPlatformer {
	[System.Serializable]
	public struct Movement : IComponentData {
		[UnityEngine.HideInInspector] public float2 Value;
		public Movement (float2 value) {
			Value = value;
		}
	}
	public class MovementComponent : ComponentDataWrapper<Movement> {}

	[UpdateAfter(typeof(MoveControllerGroup))]
	[UpdateBefore(typeof(InputProcessingGroup))]
	/// <summary>
	/// Runs after input is received, adding MovementComponent to entities which need it.
	/// </summary>
	public abstract class EnsureMovementComponentExists <T> : ComponentSystem 
		where T : struct, IComponentData
	{
		struct Data {
			public readonly int Length;
			[ReadOnly]
			public ComponentDataArray<T> inputs;
			[ReadOnly]
			public SubtractiveComponent<Movement> movement;

			public EntityArray entities;
		}
		[Inject] Data data;

		protected override void OnUpdate() {
			for (int i = 0; i < data.Length; i++) {
				PostUpdateCommands.AddComponent(data.entities[i], new Movement(0));
			}
		}
	}

}