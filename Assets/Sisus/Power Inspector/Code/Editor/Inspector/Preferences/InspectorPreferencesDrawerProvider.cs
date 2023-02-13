//#define DEBUG_SETUP_TIME

using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Sisus
{
	public static class InspectorPreferencesDrawerProvider
	{
		#if UNITY_2019_1_OR_NEWER
		[SettingsProvider, UsedImplicitly]
		private static SettingsProvider CreateInspectorPreferencesDrawer()
		{
			var provider = new SettingsProvider("Preferences/Power Inspector", SettingsScope.User)
			{
				label = "Power Inspector",
				guiHandler = DrawPreferencesGUI,

				// Populate the search keywords to enable smart search filtering and label highlighting
				keywords = new System.Collections.Generic.HashSet<string>(new[] { "Version " + Version.Current , "Edit Preferences" })
			};

			return provider;
		}
		
		private static void DrawPreferencesGUI(string searchContext)
		{
			DrawPreferencesGUI();
		}
		#endif

		#if !UNITY_2019_1_OR_NEWER
		[PreferenceItem("Power Inspector"), UsedImplicitly]
		#endif
		private static void DrawPreferencesGUI()
		{
			if(!DrawGUI.setupDone)
			{
				Setup();
			}
			
			var versionRect = new Rect(12f, 0f, 130f, 16f);
			GUI.Label(versionRect, "Version " + Version.Current);

			var buttonRect = versionRect;
			buttonRect.y = 20f;
			buttonRect.height = 30f;
			if(GUI.Button(buttonRect, "Edit Preferences"))
			{
				OpenPreferencesWindow();
			}
		}

		private static void OpenPreferencesWindow()
		{
			PowerInspectorPreferencesWindow.Open();
		}

		/// <summary>
		/// Sets up DrawGUI and Default Inspector Preferences
		/// </summary>
		private static void Setup()
		{
			#if DEV_MODE && DEBUG_SETUP_TIME
			var setupTimer = new ExecutionTimeLogger();
			setupTimer.Start("InspectorPreferencesDrawerProvider.Setup");
			#endif
			
			var settings = InspectorPreferences.GetDefaultPreferences();
			settings.Setup();
			DrawGUI.Setup(settings);

			#if DEV_MODE && DEBUG_SETUP_TIME
			setupTimer.FinishAndLogResults();
			#endif
		}
	}
}