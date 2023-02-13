using UnityEngine;

namespace Sisus
{
	public static class VectorExtensions
	{
		public static bool IsZero(this Vector3 v)
		{
			return v.x == 0f && v.y == 0f && v.z == 0f;
		}
	}
}