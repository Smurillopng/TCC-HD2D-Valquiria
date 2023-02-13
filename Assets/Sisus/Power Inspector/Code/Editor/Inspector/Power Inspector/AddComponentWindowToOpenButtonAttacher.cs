using JetBrains.Annotations;
using UnityEditor;

namespace Sisus
{
	/// <summary>
	/// Helper class that binds the opening of the add component menu window
	/// to when the Add Component button is clicked in editor mode.
	/// </summary>
	[InitializeOnLoad]
	public class AddComponentWindowToOpenButtonAttacher
	{
		[UsedImplicitly]
		static AddComponentWindowToOpenButtonAttacher()
		{
			AddComponentButtonDrawer.onOpen += AddComponentMenuWindow.CreateIfInEditorMode;
		}
	}
}