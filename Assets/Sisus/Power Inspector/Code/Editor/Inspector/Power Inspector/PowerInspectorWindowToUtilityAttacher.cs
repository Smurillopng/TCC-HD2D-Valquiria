using JetBrains.Annotations;
using UnityEditor;

namespace Sisus
{
	/// <summary>
	/// Helper class that binds the opening of the PowerInspectorWindow to PowerInspectorWindowUtility.
	/// </summary>
	[InitializeOnLoad]
	internal static class PowerInspectorWindowToUtilityAttacher
	{
		[UsedImplicitly]
		static PowerInspectorWindowToUtilityAttacher()
		{
			PowerInspectorWindowUtility.RegisterCreateNewWindowDelegate(PowerInspectorWindow.CreateNew);
		}
	}
}