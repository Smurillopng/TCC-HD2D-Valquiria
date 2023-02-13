using JetBrains.Annotations;
using System;

namespace Sisus
{
	/// <summary>
	/// Class that enables requesting PowerInspectorPreferencesWindow to open from non-Editor classes.
	/// </summary>
	public static class PowerInspectorPreferences
	{
		public static Func<PreferencesDrawer> RequestGetExistingWindow;
		public static Func<PreferencesDrawer> RequestGetExistingOrCreateNewWindow;

		/// <summary>
		/// Gets Preferences Drawer from currently open PowerInspectorPreferences window, if any.
		/// </summary>
		/// <returns> Preferences Drawer or null. </returns>
		[CanBeNull]
		public static PreferencesDrawer GetExistingWindow()
		{
			return RequestGetExistingWindow();
		}

		/// <summary>
		/// Gets Preferences Drawer from currently open PowerInspectorPreferences window, or if one is currently not open,
		/// opens a new one and returns the Preferences Drawer from that.
		/// 
		/// This can return null if no Preferences Drawer is currently open and the method is called outside of OnGUI
		/// (being called during OnGUI is required for the setup process), or if the method is called during moments
		/// such as when assemblies are being built or the asset database is still loading.
		/// </summary>
		/// <returns> Preferences Drawer or null. </returns>
		[CanBeNull]
		public static PreferencesDrawer GetExistingOrOpenNewWindow()
		{
			return RequestGetExistingOrCreateNewWindow();
		}

		/// <summary>
		/// If a Power Inspector preferences window is currently open, focuses it. Otherwise opens a new window and focuses that.
		/// 
		/// Exactly same as calling GetExistingOrOpenNewWindow, except this has no return value, making it easy to use as an Action delegate.
		/// </summary>
		public static void OpenIfNotOpenAndFocus()
		{
			RequestGetExistingOrCreateNewWindow();
		}
	}
}
