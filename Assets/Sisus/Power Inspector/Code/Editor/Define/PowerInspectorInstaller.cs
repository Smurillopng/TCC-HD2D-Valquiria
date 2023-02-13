using JetBrains.Annotations;
using UnityEditor;

namespace Sisus
{
	[InitializeOnLoad]
	public static class PowerInspectorInstaller
	{
		public const string Define = "POWER_INSPECTOR";

		[UsedImplicitly]
		static PowerInspectorInstaller()
		{
			EditorApplication.delayCall += AddDefineIfMissingAndShowWelcomeScreenIfNotShown;
		}

		private static void AddDefineIfMissingAndShowWelcomeScreenIfNotShown()
		{
			if(!ApplicationUtility.IsReady())
			{
				EditorApplication.delayCall += AddDefineIfMissingAndShowWelcomeScreenIfNotShown;
				return;
			}

			if(!ScriptingDefines.Contains(Define))
			{
				#if DEV_MODE
				EditorPrefs.SetBool("PI.WelcomeScreenShown", false); // this makes testing welcome screen showing up easier
				#endif

				UnityEngine.Debug.Log("Detected install of Power Inspector. Adding " + Define + " to Scripting Define Symbols in Player Settings.");
				ScriptingDefines.Add(Define);
				return;
			}

			if(ShouldShowWelcomeScreen())
			{
				// A NullReferenceException would occur in Unity 2019.3 beta when ShowUtility was called for the Welcome Screen without this delay
				EditorApplication.delayCall += ShowWelcomeScreen;
			}
		}

		private static bool ShouldShowWelcomeScreen()
		{
			return !EditorPrefs.GetBool("PI.WelcomeScreenShown", false);
		}

		private static bool showWelcomeScreenErrorsEncountered;

		private static void ShowWelcomeScreen()
		{
			showWelcomeScreenErrorsEncountered = false;
			UnityEngine.Application.logMessageReceived += OnMessageLoggedToConsole;

			try
			{
				DrawGUI.ExecuteMenuItem(PowerInspectorMenuItemPaths.WelcomeScreen);
			}
			#if DEV_MODE
			catch(System.Exception e)
			{
				UnityEngine.Debug.LogError(e);
			#else
			catch
			{
			#endif

				showWelcomeScreenErrorsEncountered = true;
			}

			UnityEngine.Application.logMessageReceived -= OnMessageLoggedToConsole;

			// if any errors possibly prevented welcome screen from being shown, then don't set WelcomeScreenShown true
			if(!showWelcomeScreenErrorsEncountered)
			{
				EditorPrefs.SetBool("PI.WelcomeScreenShown", true);
			}
		}

		private static void OnMessageLoggedToConsole(string condition, string stackTrace, UnityEngine.LogType logType)
		{
			if(logType == UnityEngine.LogType.Exception || logType == UnityEngine.LogType.Error)
			{
				showWelcomeScreenErrorsEncountered = true;
			}

			UnityEngine.Application.logMessageReceived -= OnMessageLoggedToConsole;
		}
	}
}