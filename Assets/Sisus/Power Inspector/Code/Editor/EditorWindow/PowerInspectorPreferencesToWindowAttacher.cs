using JetBrains.Annotations;
using UnityEditor;

namespace Sisus
{
	/// <summary>
	/// Helper class that binds the fetching or opening of the PowerInspectorPreferencesWindow to when a method in PowerInspectorPreferences is called.
	/// This decoupling is necessary because PowerInspectorPreferences can't directly see PowerInspectorPreferencesWindow which exists in an Editor only assembly.
	/// </summary>
	[InitializeOnLoad]
	public static class PowerInspectorPreferencesToWindowAttacher
	{
		[UsedImplicitly]
		static PowerInspectorPreferencesToWindowAttacher()
		{
			PowerInspectorPreferences.RequestGetExistingWindow = PowerInspectorPreferencesWindow.GetExistingWindow;
			PowerInspectorPreferences.RequestGetExistingOrCreateNewWindow = PowerInspectorPreferencesWindow.GetExistingOrCreateNewWindow;
		}
	}
}