using UnityEngine;

using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;

namespace SLPlatformer {
	[System.Serializable]
	public struct UpdateRaycastController : IComponentData {
		public float skinWidth; //Default = 0.015f;
		public float2 defaultRaySpacing; //Default = (0.02f, 0.02f)
	}

	public struct RaycastControllerComponent : IComponentData {
		public float skinWidth; //Default = 0.015f;
		public float2x2 bottomCorners;
		public float height;
		public int2 rayCount; //Default = (4,4)
		public float2 raySeparation;
	}

	public class RaycastController : ComponentSystem {
		struct Data {
			public readonly int Length;
			[ReadOnly]
			public ComponentDataArray<UpdateRaycastController> info;
			public ComponentDataArray<RaycastControllerComponent> state;
			public EntityArray entities;
			public GameObjectArray gameObjects;
		}
		[Inject] Data data;
		struct UnpreparedData {
			public readonly int Length;
			[ReadOnly]
			public ComponentDataArray<UpdateRaycastController> info;
			public SubtractiveComponent<RaycastControllerComponent> state;
			public EntityArray entities;
			public GameObjectArray gameObjects;
		}
		[Inject] UnpreparedData unprepared;

		protected override void OnUpdate() {
			EntityManager em = World.Active.GetOrCreateManager<EntityManager>();

			for (int i = 0; i < data.Length; i++) {
				data.state[i] = doUpdateRaycast(
					data.gameObjects[i],
					data.info[i]
				);
				PostUpdateCommands.RemoveComponent<UpdateRaycastController>(data.entities[i]);
			}

			for (int i = 0; i < unprepared.Length; i++) {
				PostUpdateCommands.AddComponent(
					unprepared.entities[i],
					doUpdateRaycast(
						unprepared.gameObjects[i],
						unprepared.info[i]
					)
				);
				PostUpdateCommands.RemoveComponent<UpdateRaycastController>(unprepared.entities[i]);
			}
		}

		RaycastControllerComponent doUpdateRaycast (
			GameObject go,
			UpdateRaycastController info
		) {
			//For starters, here's the collider.
			//@TODO: When ECS has a built-in Collider2D of its own, use that.
			BoxCollider2D collider = go.GetComponent<BoxCollider2D>();
			//Have to have this, or we can't do anything.
			if (collider == null) {
				return new RaycastControllerComponent();
			}

			//Convert collider bounds into an ECS format.
			float2x2 bounds = getBounds(collider, info.skinWidth);

			//Update the ray spacing data.
			RaycastControllerComponent state = new RaycastControllerComponent();
			CalculateRaySpacing(
				bounds,
				info.defaultRaySpacing,
				ref state
#if UNITY_EDITOR
				, go
#endif
			);
			UpdateRaycastOrigins(bounds, ref state);
			return state;
		}

		float2x2 getBounds (BoxCollider2D collider, float skinWidth) {
			float2x2 bounds = new float2x2(
				new float2(collider.bounds.min.x, collider.bounds.min.y),
				new float2(collider.bounds.max.x, collider.bounds.max.y)
			);
			float2 doubleSkinWidth = skinWidth;
			bounds.c0 += doubleSkinWidth;
			bounds.c1 -= doubleSkinWidth;
//			Debug.Log("Bounds are " + bounds);
			return bounds;
		}

		void UpdateRaycastOrigins (float2x2 bounds, ref RaycastControllerComponent state) {
			state.bottomCorners.c0 = bounds.c0;
			state.bottomCorners.c1 = new float2(bounds.c1.x, bounds.c0.y);
			//Because there's really no compelling reason to store four extra
			//floats rather than just one.
			state.height = bounds.c1.y - bounds.c0.y;
		}

		void CalculateRaySpacing (
			float2x2 bounds,
			float2 defaultRaySpacing,
			ref RaycastControllerComponent state
#if UNITY_EDITOR
			, GameObject context
#endif
		) {
			float2 size = new float2(bounds.c1 - bounds.c0);
//			Debug.Log("Size is: " + size);

			//Sanity check: abort if ray spacing is 0 on either axis.
			Debug.Assert(
				defaultRaySpacing.x != 0 && defaultRaySpacing.y != 0,
				"This raycast controller has ray spacing of 0 on some axis.",
				context
			);
			float2 rayCountF = size / defaultRaySpacing;
			//NOTE: asint doesn't cast to int, it directly reinterprets.
			state.rayCount = (int2)math.ceil(rayCountF);
#if UNITY_EDITOR
			//Sanity check: never have >50 rays at any point, ever.
			//For ease of reading, ensure the magnitude is never above (50,50).magnitude.
			if (math.lengthsq(state.rayCount) > 2500) {
				Debug.LogError(
					"Somehow ended up with too many rays to check.",
					context
				);
				Debug.Log("Ray spacing calculation is: " + rayCountF);
				Debug.Log("Take the ceiling of that, it's: " + math.ceil(rayCountF));
				Debug.Log("Ray count, then, is: " + state.rayCount);
			}
			state.rayCount = math.min(state.rayCount, new int2(50,50));
			//If you read that thread I started about when ECS is gonna get Physics2D,
			//you may recall that I was accidentally casting >13,000,000 rays in my
			//CharacterController2D updates. This is where that number crept in, so
			//this is where I put the sanity check.
#endif

			state.raySeparation.x = size.y / (state.rayCount.x - 1);
			state.raySeparation.y = size.x / (state.rayCount.y - 1);
		}
	}
}