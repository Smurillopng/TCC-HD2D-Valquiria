#if !UNITY_2020_1_OR_NEWER // WebView class no longer exists in Unity 2020.1 or later
using JetBrains.Annotations;
using UnityEditor;

namespace Sisus
{
	/// <summary>
	/// Helper class that lets the PowerInspectorDocumentation class communicate to the PowerInspectorDocumentationWindow class that exists in the editor assembly.
	/// </summary>
	[InitializeOnLoad]
	public static class PowerInspectorDocumentationToWindowAttacher
	{
		[UsedImplicitly]
		static PowerInspectorDocumentationToWindowAttacher()
		{
			PowerInspectorDocumentation.OnRequestingOpenWindow = PowerInspectorDocumentationWindow.OpenWindow;
			PowerInspectorDocumentation.OnRequestingShowPageIfWindowOpen = PowerInspectorDocumentationWindow.ShowPageIfWindowOpen;
		}
	}
}
#endif