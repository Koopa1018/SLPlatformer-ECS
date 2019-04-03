using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using Unity.Jobs;

namespace SLPlatformer {
	//@TODO: No idea if SharedComponentData is the way to go here.
	//From what I hear, doesn't it allocate a full 16kb block for
	//every SCD instance? Seems like a waste for a float.
	[System.Serializable]
	public struct CharacterControllerComponent : ISharedComponentData {
		public float maxSlopeAngle;
	}

	[RequireComponent(typeof(RaycastControllerBehaviour), typeof(CollisionMaskComponent), typeof(MovementComponent))]
	public class CharacterController2D : SharedComponentDataWrapper<CharacterControllerComponent> {}

	//These two components can be added by other systems to alter the behavior of CharacterController2D.
	public struct ForceGrounded : IComponentData {}
	public struct FallThroughSemiSolid : IComponentData {}

	[UpdateInGroup(typeof(MoveFinalizerGroup))]
	public class CharacterControllerSystem : ComponentSystem {
		struct Data {
			public readonly int Length;
			public ComponentDataArray<Movement> movement;
			public ComponentDataArray<Position> position;

			[WriteOnly]
			public ComponentDataArray<CollisionInfo> collisions;

			[ReadOnly]
			public SharedComponentDataArray<CharacterControllerComponent> characterController;
			[ReadOnly]
			public ComponentDataArray<RaycastControllerComponent> raycastController;
			[ReadOnly]
			public ComponentDataArray<CollisionMask> collisionMask;

			[ReadOnly]
			public SubtractiveComponent<FrozenComponent> frozenCounterTag;
			
			/// <summary>
			/// Allows checking for any behavior-modifying tags.
			/// </summary>
			public EntityArray entities;
		}
		[Inject] Data data;

		//@TODO: Replace raycast calls with CombCast2D jobs. All will have same length, but can filter by distance to same effect.
		//@TODO: Variable down direction. Not just good for gravity games, but also for SONIC!

		protected override void OnUpdate () {
			//Slightly more efficient to use GetExistingManager.
			//As long as you make sure it actually is existing before update!
			EntityManager em = World.Active.GetExistingManager<EntityManager>();

			NativeArray<RaycastHit2D> hCast;
			NativeArray<RaycastHit2D> vCast;

			for (int i = 0; i < data.Length; i++) {
				//Working copy of the movement value.
				float2 moveAmount = data.movement[i].Value;
				//For when we need an unaltered copy later.
				float2 moveAmountPreChange = moveAmount;

				//Layer mask.
				LayerMask mask = data.collisionMask[i].SolidMask;
				//@NEW: This is my implementation of semi-solid platforms.
				{
					bool checkSemiSolid = data.collisions[i].below | moveAmount.y < 0;
					checkSemiSolid &= em.HasComponent(data.entities[i], typeof(FallThroughSemiSolid));
					mask |= math.select(
						0,
						data.collisionMask[i].SemiSolidMask,
						checkSemiSolid
					);
				}
				//EXPLANATION: This merges in a second layer mask conditionally.
				//Specifically, if grounded or going down.
				//The intuitive thing would be to occlude going-up ONLY when
				//checking vertical, but doing that makes for some problems when
				//trying to jump through semi-solid slopes.
				//And we have the grounded check because you technically are
				//going up while climbing slopes--so otherwise, we would fall
				//straight through upward slopes, or even weirder behavior!


				//float maxSlopeAngleInverse;

				//Create a new collision info to be populated.
				CollisionInfo collisions = new CollisionInfo();

				//Covered in a prior system.
				//UpdateRaycastOrigins();

				//Descending slope check.
				//@TODO: Apparently, a better solution is to do an initial groundward cast
				//and run this method if (groundward_cast.hit).
				//The result of this could also replace collisions.below in the above select statement.
				if (moveAmount.y < 0) {
					//I'm not sure if I copied the sliding-down code correctly when I first
					//followed the Sebastian Lague tutorials, so I've no guarantee that this
					//implementation shown here matches his implementation.
					//Works fine without, in any case.
					CheckIfSlidingDownSlope(
						ref moveAmount,
						ref collisions,
						data.raycastController[i].bottomCorners,
						data.characterController[i].maxSlopeAngle,
						data.raycastController[i].skinWidth,
						mask
					);
					CheckIfDescendingSlope(
						ref moveAmount,
						data.characterController[i].maxSlopeAngle,
						ref collisions,
						data.raycastController[i].skinWidth,
						data.raycastController[i].bottomCorners,
						mask,
						collisions.slidingDownSlope
					);
				}
				//Needs to be before setting the move direction, lest the X collision check
				//toward the slope rather than in the direction of any possible collisions (i.e. our move direction).

				//Store last move direction
				if (moveAmount.x != 0) {
					collisions.faceDirection = (int)math.sign(moveAmount.x);
				}
				//Collisions here
				//Do horizontal collisions unconditionally.
				HorizontalCollisions(
					ref moveAmount, ref collisions,
					data.raycastController[i],
					data.characterController[i].maxSlopeAngle,
					moveAmountPreChange,
					mask
				);
				//Do vertical collisions only if moving vertically.
				if (moveAmount.y != 0) {
					VerticalCollisions(
						ref moveAmount,
						ref collisions,
						data.raycastController[i],
						mask
					);
					CounteractJointCatching(
						ref moveAmount,
						ref collisions,
						data.raycastController[i].skinWidth,
						data.raycastController[i].bottomCorners,
						mask
					);
				}
				//Update position.
				{
					float3 position = data.position[i].Value;
					position.x += moveAmount.x;
					position.y += moveAmount.y;
					data.position[i] = new Position {Value = position};
				}

				collisions.below |= em.HasComponent(data.entities[i], typeof(ForceGrounded));

				//Apply components' new data.
				float3 positionOutput = data.position[i].Value;
				positionOutput.x += moveAmount.x;
				positionOutput.y += moveAmount.y;
				data.position[i] = new Position {Value = positionOutput};
				//Movement component's job is done, and it will be blanked by MoveResetSystem.
				//Output this for next frame.
				data.collisions[i] = collisions;
			}
		}

		void CheckIfSlidingDownSlope (
			ref float2 moveAmount,
			ref CollisionInfo collisions,
			in float2x2 bottomCorners,//@TODO: Do variables passed as in interfere with cache friendliness?
			float maxSlopeAngle,
			float skinWidth,
			LayerMask mask
		) {
			RaycastHit2D maxSlopeHitLeft = Physics2D.Raycast(
				bottomCorners.c0, //bottom left
				Vector2.down,
				math.abs(moveAmount.y) + skinWidth,
				mask
			);
			RaycastHit2D maxSlopeHitRight = Physics2D.Raycast(
				bottomCorners.c1, //bottom right
				Vector2.down,
				math.abs(moveAmount.y) + skinWidth,
				mask
			);
			//To prevent jittering on very small max slopes, we only run the code if EXACTLY ONE ray returns positive.
			if (maxSlopeHitLeft ^ maxSlopeHitRight) {
				SlideDownMaxSlope(ref moveAmount, ref collisions, maxSlopeHitLeft, maxSlopeAngle);
				SlideDownMaxSlope(ref moveAmount, ref collisions, maxSlopeHitRight, maxSlopeAngle);
			}
		}

		void SlideDownMaxSlope(
			ref float2 moveAmount,
			ref CollisionInfo collisions,
			RaycastHit2D hit,
			float maxSlopeAngle
		) {
			if (!hit) {
				return;
			}
			float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
			if (slopeAngle > maxSlopeAngle) {
				moveAmount.x = math.sign(hit.normal.x) * (math.abs(moveAmount.y) - hit.distance) 
					/ math.tan (math.radians(slopeAngle));

				collisions.slopeAngle = slopeAngle;
				collisions.slidingDownSlope = true;
				collisions.slopeNormal = hit.normal;
			}
		}
		
		void CheckIfDescendingSlope (
			ref float2 moveAmount,
			float maxSlopeAngle,
			ref CollisionInfo collisions,
			float skinWidth,
			in float2x2 bottomCorners, //@TODO: Do variables passed as in interfere with cache friendliness?
			LayerMask mask,
			bool isSlidingDown
		) {
			if (isSlidingDown)
				return;
			float moveDirX = math.sign(moveAmount.x);
			Vector2 charBack = (moveDirX == -1) ? bottomCorners.c0 : bottomCorners.c1;
			//See, when descending a slope, the back is the part that touches the ground....
			RaycastHit2D hit = Physics2D.Raycast(
				charBack,
				Vector2.down,
				float.PositiveInfinity,
				mask
			);

			if (hit) {
				DoDescendSlope(
					ref moveAmount,
					ref collisions,
					moveDirX,
					maxSlopeAngle,
					skinWidth,
					hit.distance,
					hit.normal
				);
			}
		}
		
		void ClimbingSlope (
			ref float2 moveAmount,
			float2 moveAmountPreChange,
			float slopeAngle,
			ref CollisionInfo collisions,
			float hitDistance, Vector2 hitNormal,
			float skinWidth,
			int faceDir
		) {
			//To not slow down in v-shaped valleys.
			if (collisions.slopeState == SlopeState.Descending) {
				//Why unset a bool here when you can just use a state enum?
				moveAmount = moveAmountPreChange;
			}
			//To stick to the slope rather than float above slightly.
			float distanceToSlopeStart = 0;
			if (slopeAngle != collisions.slopeAngleOld) {
				distanceToSlopeStart = hitDistance - skinWidth;
				moveAmount.x -= distanceToSlopeStart * faceDir;
			}

			DoClimbSlope(ref moveAmount, ref collisions, slopeAngle, hitNormal);

			//To ensure that the stick-to-slope thing doesn't affect us past climbing.
			moveAmount.x += distanceToSlopeStart * faceDir;
		}

		void DoDescendSlope(
			ref float2 moveAmount,
			ref CollisionInfo collisions,
			float moveDirX,
			float maxSlopeAngle,
			float skinWidth,
			float hitDistance,
			Vector2 hitNormal
		) {
			float slopeAngle = Vector2.Angle(hitNormal, Vector2.up);
			if (slopeAngle == 0 || slopeAngle > maxSlopeAngle) {
				return;
			}
			if (math.sign(hitNormal.x) != moveDirX) {
				return;
			}

			float tangent = math.tan(math.radians(slopeAngle)) * math.abs(moveAmount.x);
			//Again, Sebastian's screen is big enough for him to just insert this tangent
			//stuff directly into the if condition, and mine isn't.
			//Also he doesn't have the explorer open in MonoDevelop. That probably helps.
			if (hitDistance - skinWidth <= tangent) {
				//NOTE TO SELF: DO NOT forget to update this using moveAmount.x's UNMODIFIED value.
				moveAmount.y -= math.sin(math.radians(slopeAngle)) * math.abs(moveAmount.x);
				moveAmount.x = math.cos(math.radians(slopeAngle)) * math.abs(moveAmount.x) * math.sign(moveAmount.x);

				collisions.slopeAngle = slopeAngle;
				collisions.slopeState = SlopeState.Descending;
				collisions.below = true;
				collisions.slopeNormal = hitNormal;
			}
		}

		void DoClimbSlope(
			ref float2 moveAmount,
			ref CollisionInfo collisions,
			float slopeAngle,
			Vector2 slopeNormal
		) {
			//moveAmount X is the distance we want to go up the slope in total.
			//Our goal is to treat the surface as  flat regardless of angle.
			//That way, we can apply speed scaling afterwards.

			float moveDistance = math.abs(moveAmount.x);
			float climbmoveAmountY = math.sin(math.radians(slopeAngle) ) * moveDistance;

			if (moveAmount.y <= climbmoveAmountY) { //To allow us to jump on slopes.
				moveAmount.y = climbmoveAmountY;
				moveAmount.x = math.cos(math.radians(slopeAngle)) * moveDistance * math.sign(moveAmount.x);

				collisions.below = true;
				//Because since we're climbing, moveAmount.y is positive, so it only checks upwards!
				collisions.slopeState = SlopeState.Climbing;
				collisions.slopeAngle = slopeAngle;
				collisions.slopeNormal = slopeNormal;
			} //Otherwise, we're jumping.
		}

		
		void HorizontalCollisions (
			ref float2 moveAmount,
			ref CollisionInfo collisions,
			in RaycastControllerComponent rCon,
			float maxSlopeAngle,
			float2 moveAmountPreChange,
			LayerMask mask
		) {
			int faceDir = collisions.faceDirection;
			
			float rayLength = math.abs(moveAmount.x) + rCon.skinWidth;

			if (math.abs(moveAmount.x) < rCon.skinWidth) {
				rayLength = 2 * rCon.skinWidth;
			}

			//Original version had this declaration in the loop, but...why?
			//Why would you need to declare this every time you iterate?
			//Seems like a horrendous waste of GC resources to me.
			float2 rayOrigin = math.select(
				rCon.bottomCorners.c0,
				rCon.bottomCorners.c1,
				faceDir == -1
			);
			
			for (int i = 0; i < rCon.rayCount.x; i++) {
				RaycastHit2D hit = Physics2D.Raycast(
					rayOrigin,
					Vector2.right * faceDir,
					rayLength,
					mask
				);
				//Draw horizontal collision rays.
				Debug.DrawRay(
					(Vector2)rayOrigin, //Casting to Vec2 lets Unity cast to Vec3...bleh.
					Vector2.right * faceDir,// * rayLength,
					Color.red
				);
				
				//Make sure the next ray is higher than this one.
				rayOrigin.y += rCon.raySeparation.x;

				//If we didn't get a hit, nothing to do here.
				if (!hit) {
					continue;
				}
				//If we're in a wall, just ignore it...?
				if (hit.distance == 0) {
					continue;
				}

				float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);

				if (i == 0 && slopeAngle <= maxSlopeAngle) {
					ClimbingSlope(
						ref moveAmount, moveAmountPreChange,
						slopeAngle,
						ref collisions, hit.distance, hit.normal,
						rCon.skinWidth,
						faceDir
					);
				}

				if (collisions.slopeState != SlopeState.Climbing || slopeAngle > maxSlopeAngle) {
					//NEW 1/17/2019: check if the change is less than current movement.
					//Should fix the problem of sporadically stopping when changing slopes.
					//Thanks Jose Manuel Larios Alonso (U2B comment, vid 4 "climbing slopes")!
					//Originals on the left, additions on the right.
					moveAmount.x = math.min(hit.distance - rCon.skinWidth, math.abs(moveAmount.x)) * faceDir;
					rayLength = math.min(hit.distance, math.abs(moveAmount.x) + rCon.skinWidth);

					//To prevent the wallhug jitters~
					if (collisions.slopeState == SlopeState.Climbing) {
						moveAmount.y = math.tan(math.radians(collisions.slopeAngle)) *
							math.abs(moveAmount.x);
						//Can't use slopeAngle, as that's out of date as of everything past 0.
					}

					collisions.left = faceDir == -1;
					collisions.right = faceDir == 1;
				}
			}
		}

		void VerticalCollisions (
			ref float2 moveAmount,
			ref CollisionInfo collisions,
			in RaycastControllerComponent rCon,
			LayerMask mask
		) {
			float directionY = math.sign(moveAmount.y);
			float rayLength = math.abs(moveAmount.y) + rCon.skinWidth;

			//Again: no reason to do this in the for loop.
			//Find the bottom left corner of the raycast box...
			Vector2 rayOrigin = rCon.bottomCorners.c0;
			//...and if necessary, convert it to the top left corner.
			rayOrigin.y += math.select(
				rCon.height,
				0,
				directionY == -1
			);
			//Compensate for horizontal movement.
			rayOrigin.x += moveAmount.x;
			
			for (int i = 0; i < rCon.rayCount.y; i++) {
				RaycastHit2D hit = Physics2D.Raycast(
					rayOrigin,
					Vector2.up * directionY,
					rayLength,
					mask
				);
				//Draw downward collision rays.
				Debug.DrawRay(
					rayOrigin,
					Vector2.up * directionY * rayLength,
					Color.red
				);
				
				//Make sure the next ray is farther along than this one
				rayOrigin.x += rCon.raySeparation.y;

				if (hit) {
					return;
				}
				moveAmount.y = (hit.distance - rCon.skinWidth) * directionY;
				rayLength = hit.distance;

				//To hold off the ceiling jitters!
				if (collisions.slopeState == SlopeState.Climbing) {
					moveAmount.x = moveAmount.y / math.tan(math.radians(collisions.slopeAngle)) *
						math.sign(moveAmount.x);
				}

				collisions.below = directionY == -1;
				collisions.above = directionY == 1;
			}
		}

		///<summary>
		/// To counteract "catching on" the join between two different angles of slope.
		/// </summary>
		void CounteractJointCatching (
			ref float2 moveAmount,
			ref CollisionInfo collisions,
			float skinWidth,
			in float2x2 bottomCorners,
			LayerMask mask
		) {
			if (collisions.slopeState != SlopeState.Climbing) {
				return;
			}
			float directionX = math.sign(moveAmount.x);
			float rayLength = math.abs(moveAmount.x + skinWidth);

			float2 rayOrigin = math.select(
				bottomCorners.c0,
				bottomCorners.c1,
				directionX == -1
			);
			rayOrigin.y += moveAmount.y;

			RaycastHit2D hit = Physics2D.Raycast(
				rayOrigin,
				Vector2.right * directionX,
				rayLength,
				mask
			);
			if (hit) {
				float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
				if (slopeAngle != collisions.slopeAngle) {
					//Hit a new slope!
					moveAmount.x = (hit.distance - skinWidth) * directionX;
					collisions.slopeAngle = slopeAngle;
					collisions.slopeNormal = hit.normal;
				}
			}
		}

	}
}
