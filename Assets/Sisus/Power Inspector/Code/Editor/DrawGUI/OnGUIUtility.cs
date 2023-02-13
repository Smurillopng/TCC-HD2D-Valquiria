using System;
using JetBrains.Annotations;

namespace Sisus
{
	public delegate void EnsureOnGUICallbacks([NotNull]Func<bool> returnsTrueWhileOnGUICallbacksAreNeeded, bool needsLayoutEvent);

	public static class OnGUIUtility
	{
		// Should be assigned by OnGUIUtilityToOnGUIEventHelperAttacher
		public static EnsureOnGUICallbacks OnRequestingOpenOnGUIEventHelper;
		
		public static void EnsureOnGUICallbacks([NotNull]Func<bool> returnsTrueWhileOnGUICallbacksAreNeeded, bool needsLayoutEvent)
		{
			if(OnRequestingOpenOnGUIEventHelper != null)
			{
				OnRequestingOpenOnGUIEventHelper(returnsTrueWhileOnGUICallbacksAreNeeded, needsLayoutEvent);
			}
			#if DEV_MODE
			else { UnityEngine.Debug.LogError("OnGUIUtility.OnRequestingOpenOnGUIEventHelper was null"); }
			#endif
		}
	}
}