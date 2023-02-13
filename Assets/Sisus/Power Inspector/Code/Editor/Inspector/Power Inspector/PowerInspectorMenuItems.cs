// ApplyContextMenuPreferences method will make changes to this region based on InspectorPreferences.
#region ContextMenuPreferences
#define PI_ENABLE_CONTEXT_INSPECT
#define PI_ENABLE_CONTEXT_PEEK
#define PI_ENABLE_MENU_PEEK
#define PI_ENABLE_MENU_RESET
#endregion

using System;
using System.IO;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object; 

namespace Sisus
{
	[InitializeOnLoad]
	public static class PowerInspectorMenuItems
	{
		/// <summary>
		/// This is initialized on load due to the usage of the InitializeOnLoad attribute.
		/// </summary>
		static PowerInspectorMenuItems()
		{
			EditorApplication.delayCall += ApplyPreferencesWhenAssetDatabaseReady;
		}

		private static void ApplyPreferencesWhenAssetDatabaseReady()
		{
			if(!ApplicationUtility.IsReady())
			{
				EditorApplication.delayCall += ApplyPreferencesWhenAssetDatabaseReady;
				return;
			}

			DrawGUI.OnNextBeginOnGUI(ApplyContextMenuPreferences, false);
		}

		private static void ApplyContextMenuPreferences()
		{
			// Currently cannot call Setup for InspectorPreferences because ApplyContextMenuPreferences is called outside of OnGUI. 
			InspectorPreferences preferences;
			try
			{
				preferences = InspectorUtility.Preferences;
			}
			#if DEV_MODE
			catch(NullReferenceException e)
			{
				Debug.LogError("PowerInspectorMenuItems failed to find Power Inspector Preferences asset.\n"+e);
			#else
			catch(Exception)
			{
			#endif
				return;
			}
			
			if(preferences == null)
			{
				#if DEV_MODE
				Debug.LogWarning("PowerInspectorMenuItems failed to find Power Inspector Preferences asset.");
				#endif
				return;
			}

			var scriptFile = FileUtility.FindScriptFile(typeof(PowerInspectorMenuItems));
			if(scriptFile == null)
			{
				Debug.LogError("PowerInspectorMenuItems failed to find script asset for itself.");
				return;
			}
			
			var scriptText = scriptFile.text;
			
			int preferencesStart = scriptText.IndexOf("#region ContextMenuPreferences", StringComparison.Ordinal) + 30;
			if(preferencesStart == -1)
			{
				throw new InvalidDataException("#region ContextMenuPreferences missing from PowerInspectorMenuItems.cs");
			}
			int preferencesEnd = scriptText.IndexOf("#endregion", preferencesStart, StringComparison.Ordinal);
			if(preferencesEnd == -1)
			{
				throw new InvalidDataException("#endregion missing from PowerInspectorMenuItems.cs");
			}
			string beforePreferences = scriptText.Substring(0, preferencesStart);
			string afterPreferences = scriptText.Substring(preferencesEnd);
			string menuPreferences = scriptText.Substring(preferencesStart , preferencesEnd - preferencesStart);
			
			bool scriptChanged = false;

			var enabledMenuItems = preferences.defaultInspector.enhanceUnityObjectContextMenu;
			SetContextMenuItemEnabled(ref menuPreferences, enabledMenuItems.HasFlag(ObjectContextMenuItems.ViewInPowerInspector), "#define PI_ENABLE_CONTEXT_INSPECT", ref scriptChanged);
			SetContextMenuItemEnabled(ref menuPreferences, enabledMenuItems.HasFlag(ObjectContextMenuItems.PeekInPowerInspector), "#define PI_ENABLE_CONTEXT_PEEK", ref scriptChanged);
			
			SetContextMenuItemEnabled(ref menuPreferences, !preferences.disabledMenuItems.HasFlag(MenuItems.Peek), "#define PI_ENABLE_MENU_PEEK", ref scriptChanged);
			SetContextMenuItemEnabled(ref menuPreferences, !preferences.disabledMenuItems.HasFlag(MenuItems.Reset), "#define PI_ENABLE_MENU_RESET", ref scriptChanged);

			if(scriptChanged)
			{
				string localPath = AssetDatabase.GetAssetPath(scriptFile);
				string fullPath = FileUtility.LocalToFullPath(localPath);
				File.WriteAllText(fullPath, beforePreferences + menuPreferences + afterPreferences);
				EditorUtility.SetDirty(scriptFile);
				AssetDatabase.Refresh();
			}
		}

		private static void SetContextMenuItemEnabled(ref string scriptText, bool setEnabled, string define, ref bool changed)
		{
			int isDisabled = scriptText.IndexOf("//"+define, StringComparison.Ordinal);
			
			//if menu item should be set to enabled
			if(setEnabled)
			{
				// if menu item is currently disabled
				if(isDisabled != -1)
				{
					//enable menu item by removing comment characters from in front of define
					scriptText = scriptText.Substring(0, isDisabled) + scriptText.Substring(isDisabled + 2);
					changed = true;
				}
			}
			// if menu item is currently enabled
			else if(isDisabled == -1)
			{
				//disable menu item by commenting define out
				int i = scriptText.IndexOf(define, StringComparison.Ordinal);
				if(i != -1)
				{
					scriptText = scriptText.Substring(0, i) + "//" + scriptText.Substring(i);
					changed = true;
				}
				else
				{
					Debug.LogError("Failed to find \""+define+"\" in PowerInspectorMenuItems.cs");
				}
			}
		}

		[UsedImplicitly, MenuItem(PowerInspectorMenuItemPaths.Preferences, false, PowerInspectorMenuItemPaths.PreferencesPriority)]
		private static void OpenPreferences()
		{
			PowerInspectorPreferencesWindow.GetExistingOrCreateNewWindow();
		}

		/// <summary>
		/// Opens Create Script Wizard and resets various configuration options to their default initial values.
		/// </summary>
		[UsedImplicitly, MenuItem(PowerInspectorMenuItemPaths.CreateScriptWizard, false, PowerInspectorMenuItemPaths.CreateScriptWizardPriority)]
		private static void OpenCreateScriptWizard()
		{
			Platform.Active.SetPrefs("PI.CreateScriptWizard/Namespace", InspectorUtility.Preferences.defaultNamespace);
			Platform.Active.SetPrefs("PI.CreateScriptWizard/SaveIn", InspectorUtility.Preferences.defaultScriptPath);
			Platform.Active.SetPrefs("PI.CreateScriptWizard/Template", "MonoBehaviour");
			Platform.Active.DeletePrefs("PI.CreateScriptWizard/AttachTo");

			DrawGUI.ExecuteMenuItem(PowerInspectorMenuItemPaths.CreateScriptWizardFromCreateMenu);
		}

		[UsedImplicitly, MenuItem(PowerInspectorMenuItemPaths.CheckForUpdates, false, PowerInspectorMenuItemPaths.CheckForUpdatesPriority)]
		private static void CheckForUpdates()
		{
			Application.OpenURL("http://u3d.as/1sNc");
		}
		
		[UsedImplicitly, MenuItem(PowerInspectorMenuItemPaths.Forums, false, PowerInspectorMenuItemPaths.ForumsPriority)]
		private static void OpenForums()
		{
			Application.OpenURL("https://forum.unity.com/threads/released-power-inspector-full-inspector-overhaul.736022/");
		}

		[UsedImplicitly, MenuItem(PowerInspectorMenuItemPaths.DemoScene, false, PowerInspectorMenuItemPaths.DemoScenePriority)]
		private static void OpenDemoScene()
		{
			var sceneGuids = AssetDatabase.FindAssets("Power Inspector Demo t:SceneAsset");
			if(sceneGuids.Length == 0)
			{
				var installerGuids = AssetDatabase.FindAssets("Power Inspector Demo Installer");
				if(installerGuids.Length == 0)
				{
					if(DrawGUI.Active.DisplayDialog("Demo Package Not Found", "Power Inspector Demo package was not found at path\nSisus/Power Inspector/Demo/Power Inspector Demo Installer.unitypackage.\n\nWould you like to visit the Asset Store page from where you can reinstall Power Inspector along with the demo scene?", "Open Store Page", "Cancel"))
					{
						Application.OpenURL("http://u3d.as/1sNc");
					}
					return;
				}

				if(DrawGUI.Active.DisplayDialog("Install Demo Scene?", "The demo scene is not installed. Would you like to install it now?", "Install", "Cancel"))
				{
					var installerPath = AssetDatabase.GUIDToAssetPath(installerGuids[0]);
					AssetDatabase.ImportPackage(installerPath, false);
				}
				return;
			}

			var scenePath = AssetDatabase.GUIDToAssetPath(sceneGuids[0]);
			EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
		}

		[UsedImplicitly, MenuItem(PowerInspectorMenuItemPaths.IssueTracker, false, PowerInspectorMenuItemPaths.IssueTrackerPriority)]
		private static void OpenIssueTracker()
		{
			Application.OpenURL("https://github.com/SisusCo/Power-Inspector/issues");
		}

		[UsedImplicitly, MenuItem(PowerInspectorMenuItemPaths.ContactSupport, false, PowerInspectorMenuItemPaths.ContactSupportPriority)]
		private static void ContactSupport()
		{
			Application.OpenURL("https://www.sisus.co/contact/");
		}
		
		#if PI_ENABLE_MENU_RESET
		[UsedImplicitly, MenuItem(PowerInspectorMenuItemPaths.Reset, false, 105)]
		private static void Reset()
		{
			var e = new Event();
			e.commandName = "Reset";
			e.type = EventType.ValidateCommand;
			InspectorUtility.ActiveInspector.SendEvent(e);
		}
		
		[UsedImplicitly, MenuItem(PowerInspectorMenuItemPaths.Reset, true)]
		private static bool ShouldDisplayReset()
		{
			var manager = InspectorUtility.ActiveManager;
			if(manager == null)
			{
				return false;
			}

			var inspector = manager.SelectedInspector;
			if(inspector == null)
			{
				return false;
			}

			return inspector.FocusedDrawer != null;
		}
		#endif

		#if PI_ENABLE_MENU_PEEK
		[UsedImplicitly, MenuItem(PowerInspectorMenuItemPaths.Peek, false, 142)]
		private static void ShowInSplitView()
		{
			var selected = Selection.objects;
			
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(selected.Length > 0);
			#endif

			var manager = InspectorUtility.ActiveManager;
			if(manager == null)
			{
				manager = InspectorManager.Instance();

				if(!InspectorUtility.Preferences.SetupDone && Event.current == null)
				{
					DrawGUI.OnNextBeginOnGUI(ShowInSplitView, true);
					return;
				}
			}

			var inspector = manager.LastSelectedActiveOrDefaultInspector(selected[0].IsSceneObject() ? InspectorTargetingMode.Hierarchy : InspectorTargetingMode.Project, InspectorSplittability.IsSplittable);

			ISplittableInspectorDrawer splittableDrawer;
			if(inspector == null)
			{
				splittableDrawer = PowerInspectorWindow.CreateNew(selected, true, false);
			}
			else
			{
				splittableDrawer = (ISplittableInspectorDrawer)inspector.InspectorDrawer;
				splittableDrawer.ShowInSplitView(selected);

				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(splittableDrawer != null);
				Debug.Assert(selected[0].IsSceneObject() ? splittableDrawer.InspectorTargetingMode != InspectorTargetingMode.Hierarchy : splittableDrawer.InspectorTargetingMode != InspectorTargetingMode.Project);
				#endif
			}
		}

		[UsedImplicitly, MenuItem(PowerInspectorMenuItemPaths.Peek, true)]
		private static bool ShouldDisplayShowInSplitView()
		{
			if(Selection.objects.Length == 0) // || InspectorUtility.ActiveManager == null || InspectorUtility.ActiveManager.GetLastSelectedInspectorDrawer(typeof(ISplittableInspectorDrawer)) == null)
			{
				return false;
			}

			return true;
		}
		#endif

		#if PI_ENABLE_CONTEXT_INSPECT
		[UsedImplicitly, MenuItem(PowerInspectorMenuItemPaths.ViewInPowerInspector)]
		private static void ViewObjectInPowerInspector(MenuCommand command)
		{
			ViewObjectInPowerInspector(command, false);
		}
		#endif
		
		#if PI_ENABLE_CONTEXT_PEEK
		[UsedImplicitly, MenuItem(PowerInspectorMenuItemPaths.PeekInPowerInspector)]
		private static void PeekObjectInPowerInspector(MenuCommand command)
		{
			ViewObjectInPowerInspector(command, true);
		}
		#endif

		#if PI_ENABLE_CONTEXT_PEEK || PI_ENABLE_CONTEXT_INSPECT
		private static void ViewObjectInPowerInspector(MenuCommand command, bool useSplitView)
		{
			var manager = InspectorUtility.ActiveManager;
			if(manager == null)
			{
				manager = InspectorManager.Instance();
			}

			var target = command.context;
			var gameObject = target.GameObject();
			if(gameObject != null)
			{
				var inspector = manager.LastSelectedActiveOrDefaultInspector(gameObject.scene.IsValid() ? InspectorTargetingMode.Hierarchy : InspectorTargetingMode.Project);
				if(inspector == null)
				{
					var window = PowerInspectorWindow.CreateNew(false);
					inspector = window.MainView;
					manager.ActiveInspector = inspector;
				}
				
				if(useSplitView)
				{
					var splittableDrawer = inspector.InspectorDrawer as ISplittableInspectorDrawer;
					if(splittableDrawer != null)
					{
						splittableDrawer.ShowInSplitView(ArrayPool<Object>.CreateWithContent(gameObject));
						inspector.ScrollToShow(target);
						return;
					}
				}
				else
				{
					inspector = inspector.InspectorDrawer.MainView;
				}
				
				inspector.RebuildDrawers(ArrayPool<Object>.CreateWithContent(gameObject), true);
				inspector.ScrollToShow(target);
			}
			else
			{
				var inspector = manager.LastSelectedActiveOrDefaultInspector(target.IsSceneObject() ? InspectorTargetingMode.Hierarchy : InspectorTargetingMode.Project);
				if(inspector == null)
				{
					var window = PowerInspectorWindow.CreateNew(false);
					inspector = window.MainView;
					manager.ActiveInspector = inspector;
				}
				
				if(useSplitView)
				{
					var splittableDrawer = inspector.InspectorDrawer as ISplittableInspectorDrawer;
					if(splittableDrawer != null)
					{
						splittableDrawer.ShowInSplitView(ArrayPool<Object>.CreateWithContent(target));
						inspector.ScrollToShow(target);
						return;
					}
				}

				inspector.RebuildDrawers(ArrayPool<Object>.CreateWithContent(target), true);
				inspector.ScrollToShow(target);
			}
		}
		#endif
	}
}