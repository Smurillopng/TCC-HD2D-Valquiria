//#define DEBUG_RAYCAST
//#define DEBUG_GROUND

using JetBrains.Annotations;
using System.Linq;
using UnityEngine;

namespace Sisus
{
	public static class GroundUtility
	{
		[Pure, NotNull]
		public static RaycastHit?[] RaycastGround([NotNull] Transform[] transforms)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(transforms != null);
			#endif

			int count = transforms.Length;
			var results = new RaycastHit?[count];
			for(int n = count - 1; n >= 0; n--)
			{
				results[n] = RaycastGround(transforms[n]);
			}
			return results;
		}

		[Pure, CanBeNull]
		public static RaycastHit? RaycastGround([NotNull] Transform transform)
		{
			if(transform == null)
			{
				#if DEV_MODE
				Debug.LogWarning("GroundUtility.RaycastGround called with empty transform argument.");
				#endif
				return null;
			}

			var worldPosition = transform.position;

			var halfExtents = new Vector3(0.5f, 0.5f, 0.5f);

			var hits = Physics.BoxCastAll(worldPosition + Vector3.up * 2f, halfExtents, Vector3.down);

			RaycastHit? result = null;

			float smallestDistance = Mathf.Infinity;
			for(int h = hits.Length - 1; h >= 0; h--)
			{
				var hit = hits[h];
				if(ReferenceEqualsTransformOrAnyChildOfTransform(hit.transform, transform))
				{
					continue;
				}

				var point = hit.point;

				// For colliders that overlap the box at the start of the sweep, RaycastHit.normal is set opposite to the direction of the sweep, RaycastHit.distance is set to zero, and the zero vector gets returned in RaycastHit.point.
				// We skip these entires.
				if(hit.distance == 0f && point == Vector3.zero && hit.normal.normalized == Vector3.up)
				{
					continue;
				}

				var distance = Vector3.SqrMagnitude(worldPosition - point);
				if(distance <= smallestDistance)
				{
					#if DEV_MODE && DEBUG_RAYCAST
					Debug.Log("hit "+(h+1)+"/"+hits.Length+": from "+worldPosition+" to "+ point + " with distance " +Mathf.Sqrt(distance) +", hit="+ hit.transform.name);
					#endif

					result = hit;
					smallestDistance = distance;
				}
			}

			#if DEV_MODE && DEBUG_RAYCAST
			if(result != null)
			{
				Debug.Log("FINAL: from "+worldPosition+" to "+ result.Value.point+" with distance " + Mathf.Sqrt(smallestDistance)+", hit="+ result.Value.transform.name);
				Debug.DrawLine(worldPosition + Vector3.up * 2f, result.Value.point);
			}
			#endif

			return result;
		}

		private static bool ReferenceEqualsTransformOrAnyChildOfTransform(Transform reference, Transform transform)
		{
			if(reference == transform)
			{
				return true;
			}
			foreach(Transform child in transform)
			{
				if(ReferenceEqualsTransformOrAnyChildOfTransform(reference, child))
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Casts a ray downward from current position, and if the ray hits a collider
		/// on the way down, the target Transforms are moved to the point of collision.
		/// </summary>
		public static bool Ground([NotNull] Transform[] transforms)
		{
			if(transforms == null)
			{
				#if DEV_MODE
				Debug.LogWarning("GroundUtility.Ground called with empty transforms argument.");
				#endif
				return false;
			}

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(transforms.Length > 0);
			#endif

			bool changed = false;
			for(int n = transforms.Length - 1; n >= 0; n--)
			{
				var transform = transforms[n];
				if(Ground(transform))
				{
					changed = true;
				}
			}

			return changed;
		}

		/// <summary>
		/// Casts a ray downward from current position, and if the ray hits a collider
		/// on the way down, the target Transforms are moved to the point of collision.
		/// </summary>
		public static bool Ground([NotNull] Transform transform)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(transform != null);
			#endif

			var hit = RaycastGround(transform);
			if(!hit.HasValue)
			{
				return false;
			}

			var point = hit.Value.point;
			var worldPosition = transform.position;

			var setWorldPosition = worldPosition;
			setWorldPosition.y = point.y;

			float? adjustYPosition = null;

			var colliders = transform.GetComponentsInChildren<Collider>().Where((c)=>c.enabled && !c.isTrigger);

			foreach(var collider in colliders)
			{
				// The world space bounding volume of the collider.
				var worldBounds = collider.bounds;

				float baseOffset = worldPosition.y - worldBounds.min.y;
				if(!adjustYPosition.HasValue || baseOffset < adjustYPosition)
				{
					adjustYPosition = baseOffset;
				}

				#if DEV_MODE && DEBUG_GROUND
				Debug.Log("adjustPosition="+ adjustYPosition + ", point=" + point + ", position.y="+worldPosition.y+", worldBounds.max.y=" + worldBounds.max.y);
				#endif
			}

			if(!colliders.Any())
			{
				foreach(var renderer in transform.GetComponentsInChildren<Renderer>())
				{
					var worldBounds = renderer.bounds;

					float baseOffset = worldPosition.y - worldBounds.min.y;
					if(!adjustYPosition.HasValue || baseOffset < adjustYPosition)
					{
						adjustYPosition = baseOffset;
					}

					#if DEV_MODE && DEBUG_GROUND
					Debug.Log("adjustYPosition=" + adjustYPosition + ", point=" + point + ", position.y="+worldPosition.y+", worldBounds.max.y=" + worldBounds.max.y);
					#endif
				}
			}

			if(adjustYPosition.HasValue)
			{
				setWorldPosition.y += adjustYPosition.Value;
			}

			if(worldPosition == setWorldPosition)
			{
				return false;
			}

			#if DEV_MODE && DEBUG_GROUND
			Debug.Log(transform.name + ".position =" + setWorldPosition+" (was: "+worldPosition+") with raycast.point="+point+", adjustYPosition="+adjustYPosition);
			#endif

			transform.position = setWorldPosition;

			UnityEditor.EditorUtility.SetDirty(transform);

			return true;
		}
	}
}