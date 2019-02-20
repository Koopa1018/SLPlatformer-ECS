using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;

namespace Clouds.ECS {
	public abstract class EnsureHasComponent<TPredicate, TAdded> : ComponentSystem 
		where TPredicate : struct, IComponentData
		where TAdded : struct, IComponentData
	{
		//@TODO: Chunk filtering breaks things.
		struct Data {
			[ReadOnly]
			public ComponentDataArray<TPredicate> hasThese;
			[ReadOnly]
			public SubtractiveComponent<TAdded> hasNotThese;
			public EntityArray entities;
		}
		[Inject] Data data;

		//protected override void OnCreateManager() {
		//	filter = GetComponentGroup(typeof(TPredicate), ComponentType.Subtractive(typeof(TAdded)) );
		//	eType = GetArchetypeChunkEntityType();
		//}

		protected override void OnUpdate() {
			//NativeArray<ArchetypeChunk> arry = filter.CreateArchetypeChunkArray(Allocator.TempJob);

			for (int i = 0; i < data.entities.Length; i++) {
				PostUpdateCommands.AddComponent(data.entities[i], new TAdded() );
			}
		}

	}
}