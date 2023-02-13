using System;
using JetBrains.Annotations;
using Object = UnityEngine.Object;
#if !UNITY_EDITOR
using UnityEngine;
using UnityEngine.SceneManagement;
#endif

namespace Sisus
{
	public static class InstanceIdUtility
	{
		[CanBeNull]
		public static Object IdToObject(int instanceId, [NotNull]Type type)
		{
			return UnityEditor.EditorUtility.InstanceIDToObject(instanceId);
		}
	}
}