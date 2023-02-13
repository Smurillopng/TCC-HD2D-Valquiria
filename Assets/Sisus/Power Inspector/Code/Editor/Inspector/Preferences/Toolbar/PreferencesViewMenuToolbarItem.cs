#if !POWER_INSPECTOR_LITE
using UnityEngine;
using Sisus.Attributes;

namespace Sisus
{
	[ToolbarItemFor(typeof(PreferencesToolbar), 30, ToolbarItemAlignment.Left, true)]
	public class PreferencesViewMenuToolbarItem : ViewMenuToolbarItem
	{
		/// <inheritdoc/>
		protected override void BuildContextMenu(ref Menu menu, bool extendedMenu)
		{
			#if DEV_MODE
			menu.Add("Debug Mode+/Off", "Disable Debug Mode For All Inspected Targets", inspector.DisableDebugMode, !inspector.State.DebugMode);
			menu.Add("Debug Mode+/On", "Enable Debug Mode For All Inspected Targets", inspector.EnableDebugMode, inspector.State.DebugMode);
			menu.AddSeparator();
			#endif
			
			menu.Add("Preferences Documentation", PowerInspectorDocumentation.ShowCategory, "preferences");
			menu.AddSeparator();
			menu.Add("Help/Documentation", PowerInspectorDocumentation.Show);
			menu.Add("Help/Forum", OpenUrlFromContextMenu, "https://forum.unity.com/threads/released-power-inspector-full-inspector-overhaul.736022/");
			menu.AddSeparator("Help/");
			menu.Add("Help/Toolbar/Toolbar", PowerInspectorDocumentation.ShowFeature, "toolbar");
			menu.Add("Help/Toolbar/Back And Forward Buttons", PowerInspectorDocumentation.ShowFeature, "back-and-forward-buttons");
			menu.Add("Help/Toolbar/Search Box", PowerInspectorDocumentation.ShowFeature, "search-box");
			menu.Add("Help/Features/Copy-Paste", PowerInspectorDocumentation.ShowFeature, "copy-paste");
			menu.Add("Help/Features/Reset", PowerInspectorDocumentation.ShowFeature, "reset");
			menu.Add("Help/Features/Context Menu", PowerInspectorDocumentation.ShowFeature, "context-menu-items");
			menu.AddSeparator("Help/");
			menu.Add("Help/Troubleshooting/Troubleshooting Documentation", PowerInspectorDocumentation.Show, "category/troubleshooting/");
			menu.Add("Help/Troubleshooting/Issue Tracker", OpenUrlFromContextMenu, "https://github.com/SisusCo/Power-Inspector/issues");
		}

		private static void OpenUrlFromContextMenu(object url)
		{
			Application.OpenURL((string)url);
		}
	}
}
#endif