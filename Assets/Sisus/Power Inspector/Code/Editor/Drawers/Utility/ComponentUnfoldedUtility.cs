//#define DEBUG_SET_UNFOLDED

using JetBrains.Annotations;
using UnityEngine;
#if UNITY_EDITOR
#if !UNITY_2019_3_OR_NEWER
using System.Reflection;
#endif
#else
using System.Collections.Generic;
#endif

namespace Sisus
{
	/// <summary>
	/// Utility for getting and setting the unfolded state of UnityEngine.Objects (Components in practice).
	/// In the editor this uses InternalEditorUtility behind the scenes, and the unfolded state is persistent
	/// between sessions. At runtime, the states are cached only for the duration of the session.
	/// </summary>
	public static class ComponentUnfoldedUtility
	{
		#if UNITY_EDITOR

		#if !UNITY_2019_3_OR_NEWER
		private static MethodInfo getIsExpanded;
		private static MethodInfo setIsExpanded;
		#endif

		#else
		private static Dictionary<int, bool> UnfoldedStatesCache = new Dictionary<int, bool>();
		#endif
		
		public static bool GetIsUnfolded([NotNull]Object obj)
		{
			#if UNITY_EDITOR

			#if UNITY_2019_3_OR_NEWER
			return UnityEditorInternal.InternalEditorUtility.GetIsInspectorExpanded(obj);
			#else
			if(getIsExpanded == null)
			{
				var editorUtilityType = Types.GetInternalEditorInternalType("UnityEditorInternal.InternalEditorUtility");
				getIsExpanded = editorUtilityType.GetMethod("GetIsInspectorExpanded", BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.Public);
			}
			return (bool)getIsExpanded.InvokeWithParameter(null, obj);
			#endif

			#else
			bool unfolded;
			return UnfoldedStatesCache.TryGetValue(obj.GetInstanceID(), out unfolded) && unfolded;
			#endif
		}

		public static void SetIsUnfolded([NotNull]Object obj, bool unfolded)
		{
			#if DEV_MODE && DEBUG_SET_UNFOLDED
			Debug.Log("SetIsUnfolded("+obj.GetType().Name+", "+StringUtils.ToColorizedString(unfolded)+")");
			#endif

			#if UNITY_EDITOR

			#if UNITY_2019_3_OR_NEWER
			UnityEditorInternal.InternalEditorUtility.SetIsInspectorExpanded(obj, unfolded);
			#else
			if(setIsExpanded == null)
			{
				var editorUtilityType = Types.GetInternalEditorInternalType("UnityEditorInternal.InternalEditorUtility");
				setIsExpanded = editorUtilityType.GetMethod("SetIsInspectorExpanded", BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.Public);
			}
			setIsExpanded.InvokeWithParameters(null, obj, unfolded);
			#endif

			#else
			UnfoldedStatesCache[obj.GetInstanceID()] = unfolded;
			#endif
		}
	}
}