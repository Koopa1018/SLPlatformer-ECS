using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;

namespace SLPlatformer {
	/// <summary>
	/// The Move Controller group generates input for the Move Processing group. (Player input goes here.)
	/// </summary>
	[UpdateBefore(typeof(InputProcessingGroup))]
	public sealed class MoveControllerGroup { }
	
	[UpdateAfter(typeof(MoveControllerGroup))]
	[UpdateBefore(typeof(InputProcessingGroup))]
	//this to make sure that MonoBehaviour can use player input components if needed.
	[UpdateBefore(typeof(UnityEngine.Experimental.PlayerLoop.Update))]
	public sealed class MoveControllerBarrier : BarrierSystem {}

	/// <summary>
	/// The Input Processing group converts input from Move Controller group into movement for Move Finalizer group to process.
	/// </summary>
	[UpdateAfter(typeof(MoveControllerGroup))]
	[UpdateBefore(typeof(MoveFinalizerGroup))]
	public class InputProcessingGroup {}

	[UpdateAfter(typeof(InputProcessingGroup))]
	[UpdateBefore(typeof(MoveFinalizerGroup))]
	public sealed class InputProcessingBarrier : BarrierSystem {}

	/// <summary>
	/// The Move Finalizer group applies the movement computed in the Move Processing group.
	/// </summary>
	[UpdateAfter(typeof(MoveControllerGroup))]
	[UpdateAfter(typeof(InputProcessingGroup))]
	[UpdateBefore(typeof(CopyTransformToGameObjectSystem))]
	public sealed class MoveFinalizerGroup {}
}