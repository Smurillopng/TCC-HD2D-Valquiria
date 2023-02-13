using JetBrains.Annotations;
using UnityEditor;

namespace Sisus
{
	/// <summary>
	/// Helper class that binds the opening of the OnGUIEventHelper window to when OnGUIUtility.EnsureOnGUICallbacks is called.
	/// This decoupling is necessary because OnGUIUtility can't directly see OnGUIEventHelper which exists in an Editor only assembly.
	/// </summary>
	[InitializeOnLoad]
	public static class OnGUIUtilityToOnGUIEventHelperAttacher
	{
		[UsedImplicitly]
		static OnGUIUtilityToOnGUIEventHelperAttacher()
		{
			OnGUIUtility.OnRequestingOpenOnGUIEventHelper = OnGUIEventHelper.EnsureOnGUICallbacks;
		}
	}
}