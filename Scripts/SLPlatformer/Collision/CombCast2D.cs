using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;

namespace SLPlatformer
{
	//@TODO: Convert to IJobParallelFor?
	/// <summary>
	/// Performs raycasts between vertex1 and vertex2, returning an array of hits.
	/// </summary>
	public struct CombCast2D : IJob {
		[ReadOnly]
		public float2 _vertex1;
		[ReadOnly]
		public float2 _vertex2;
		[ReadOnly]
		public Vector2 _direction;
		[ReadOnly]
		public float _distance;
		[ReadOnly]
		public LayerMask _layerMask;
		[ReadOnly]
		public float _minDepth;
		[ReadOnly]
		public float _maxDepth;

		NativeArray<RaycastHit2D> _raycastOutput;

		public CombCast2D (
			NativeArray<RaycastHit2D> raycastOutput,
			float2 vertex1, float2 vertex2,
			Vector2 direction, float distance = float.Epsilon,
			int layerMask = -5,
			float minDepth = float.NegativeInfinity,
			float maxDepth = float.PositiveInfinity
		) {
			_raycastOutput = raycastOutput;

			_vertex1 = vertex1;
			_vertex2 = vertex2;
			_direction = direction;
			_distance = distance;
			_layerMask = layerMask;

			_minDepth = minDepth;
			_maxDepth = maxDepth;
		}
		
		public void Execute () {
			Vector2 intermediate = _vertex1;

			//My old division-avoidance method:
			//_vertex2 = Vector2.Lerp(vertex1, vertex2, 1 / (rayCount - 1));
			//And then in the for loop, before each raycast:
			//vertex1 += vertex2;
			
			for (int i = 0; i < _raycastOutput.Length; i++) {
				_raycastOutput[i] = Physics2D.Raycast(
					intermediate, //origin
					_direction, //direction
					_distance,
					_layerMask,
					_minDepth,
					_maxDepth
				);

				intermediate = math.lerp(_vertex1, _vertex2, i / _raycastOutput.Length);
			}
		}

	}
}
