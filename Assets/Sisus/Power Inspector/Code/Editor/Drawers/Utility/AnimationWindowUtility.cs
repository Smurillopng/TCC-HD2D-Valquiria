using System.Reflection;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Sisus
{
	/// <summary> Utility class for determining whether the Animation window is currently recording or playing.
	/// Outside of the editor all values will always return false. </summary>
	public static class AnimationWindowUtility
	{
		#if UNITY_EDITOR
		private static EditorWindow animationWindow;
		private static object animationWindowState;
		private static PropertyInfo recordingProperty;
		private static PropertyInfo playingProperty;
		#endif
		private static bool recording;
		private static bool playing;
		private static double valuesCachedTime;
		
		public static bool Recording()
		{
			#if UNITY_EDITOR
			if(AnimationMode.InAnimationMode() && valuesCachedTime + 0.03d < EditorApplication.timeSinceStartup)
			{
				UpdateCachedValues();
			}
			return recording;
			#else
			return false;
			#endif
		}

		public static bool Playing()
		{
			#if UNITY_EDITOR
			if(AnimationMode.InAnimationMode() && valuesCachedTime + 0.03d < EditorApplication.timeSinceStartup)
			{
				UpdateCachedValues();
			}
			return playing;
			#else
			return false;
			#endif
		}

		#if UNITY_EDITOR
		private static void UpdateCachedValues()
		{
			valuesCachedTime = EditorApplication.timeSinceStartup;

			if(animationWindow == null)
			{
				var animationWindowType = Types.GetInternalEditorType("UnityEditor.AnimationWindow");
				animationWindow = EditorWindow.GetWindow(animationWindowType);
				var stateProperty = animationWindowType.GetProperty("state", BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.NonPublic);
				animationWindowState = stateProperty.GetValue(animationWindow, null);
				recordingProperty = animationWindowState.GetType().GetProperty("recording", BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public);
				playingProperty = animationWindowState.GetType().GetProperty("playing", BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public);
			}
			recording = (bool)recordingProperty.GetValue(animationWindowState, null);
			playing = (bool)playingProperty.GetValue(animationWindowState, null);
		}
		#endif
	}
}