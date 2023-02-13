using JetBrains.Annotations;

namespace Sisus
{
	/// <summary>
	/// Helper class that makes ContextMenuUtility subscribe to the OnInspectorGUIIEnd callback of InspectorUtility.
	/// </summary>
	#if UNITY_EDITOR
	[UnityEditor.InitializeOnLoad]
	#endif
	public class ContextMenuUtilityToInspectorUtilityAttacher
	{
		#if UNITY_EDITOR
		[UsedImplicitly]
		static ContextMenuUtilityToInspectorUtilityAttacher()
		{
			InspectorUtility.OnInspectorGUIEnd += ContextMenuUtility.OnInspectorGUIEnd;
		}
		#endif

		#if !UNITY_EDITOR
		[UnityEngine.RuntimeInitializeOnLoadMethod, UsedImplicitly]
		#endif
		private static void AttachContextMenuUtilityToInspectorUtility()
		{
			InspectorUtility.OnInspectorGUIEnd += ContextMenuUtility.OnInspectorGUIEnd;
		}
	}
}