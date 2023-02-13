#define DEBUG_ENABLED

#define DEBUG_ENSURE_ON_GUI_REQUESTS
//#define DEBUG_IS_STILL_NEEDED

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Sisus
{
	/// <summary>
	/// Class that helps get around issue of not having access to OnGUI event functions or Event.current when
	/// there are no EditorWindows open, which makes it impossible to call certain methods that require the OnGUI context.
	/// 
	/// This class temporarily creates an off-screen window so that it can call DrawGUI.BeginOnGUI and InspectorManager.Instance().OnLayout.
	/// </summary>
	internal class OnGUIEventHelper : EditorWindow
	{
		private static OnGUIEventHelper instance;

		private static readonly List<Func<bool>> isStillNeeded = new List<Func<bool>>();
		private static InspectorPreferences preferences;
		private static bool getsOnGUIEvents;
		private static bool getsLayoutEvents;
		private static EditorWindow hierarchyWindow;
		private static EditorWindow projectWindow;
		private static bool setupDone = false;

		private static OnGUIEventHelper Instance()
		{
			if(instance == null)
			{
				instance = GetWindow<OnGUIEventHelper>(false);
				if(!setupDone)
				{
					instance.Setup();
				}
			}

			return instance;
		}

		private void Setup()
		{
			getsOnGUIEvents = true;
			getsLayoutEvents = true;

			if(instance != null)
			{
				if(instance != this)
				{
					instance.Close();
				}
			}
			instance = this;

			minSize = new Vector2(0f,0f);
			maxSize = new Vector2(0f,0f);
			position = new Rect(100000f, 100000f, 0f, 0f);

			Show();
			
			#if UNITY_2018_1_OR_NEWER
			EditorApplication.quitting += instance.OnApplicationQuitting;
			AssemblyReloadEvents.beforeAssemblyReload += instance.OnBeforeAssemblyReload;
			#endif

			Repaint();
			GUI.changed = true;
		}

		private static void HierarchyOnGUI(int instanceId, Rect selectionRect)
		{
			if(OnGUIStatic())
			{
				EditorApplication.projectWindowItemOnGUI -= ProjectOnGUI;
				EditorApplication.hierarchyWindowItemOnGUI -= HierarchyOnGUI;
				getsOnGUIEvents = false;
				getsLayoutEvents = false;
			}
			else if(hierarchyWindow != null)
			{
				hierarchyWindow.Repaint();
				GUI.changed = true;
			}
		}

		private static void ProjectOnGUI(string guid, Rect selectionRect)
		{
			if(OnGUIStatic())
			{
				EditorApplication.projectWindowItemOnGUI -= ProjectOnGUI;
				EditorApplication.hierarchyWindowItemOnGUI -= HierarchyOnGUI;
				getsOnGUIEvents = false;
				getsLayoutEvents = false;
			}
			else if(projectWindow != null)
			{
				projectWindow.Repaint();
				GUI.changed = true;
			}
		}

		internal static void EnsureOnGUICallbacks([NotNull]Func<bool> returnsTrueWhileOnGUICallbacksAreNeeded, bool needsLayoutEvent)
		{
			if(isStillNeeded.Contains(returnsTrueWhileOnGUICallbacksAreNeeded) && getsOnGUIEvents)
			{
				if(projectWindow != null || hierarchyWindow != null)
				{
					if(projectWindow != null)
					{
						projectWindow.Repaint();
					}
					if(hierarchyWindow != null)
					{
						hierarchyWindow.Repaint();
					}
				}
				else
				{
					Instance().Repaint();
				}
				return;
			}

			if(isStillNeeded.Count >= 25)
			{
				#if DEV_MODE
				Debug.LogError("OnGUIEventHelper.isStillNeeded.Count has reached 25! Ignoring EnsureOnGUICallbacks.");
				#endif
				return;
			}

			if(returnsTrueWhileOnGUICallbacksAreNeeded())
			{
				#if DEV_MODE && DEBUG_ENSURE_ON_GUI_REQUESTS
				Debug.Log("EnsureOnGUICallbacks - adding as isStillNeeded #" + (isStillNeeded.Count + 1) + ": " + StringUtils.ToString(returnsTrueWhileOnGUICallbacksAreNeeded));
				#endif

				if(!getsOnGUIEvents || (needsLayoutEvent && !getsLayoutEvents))
				{
					if(!needsLayoutEvent)
					{
						// Only create the OnGUIEVentHelper EditorWindow if absolutely necessary. If the user has a project or hierarchy window open, we can use projectWindowItemOnGUI / hierarchyWindowItemOnGUI callbacks instead.
						var editorWindows = Resources.FindObjectsOfTypeAll<EditorWindow>();
						for(int n = editorWindows.Length - 1; n >= 0; n--)
						{
							var window = editorWindows[n];
							if(string.Equals(window.GetType().Name, "SceneHierarchyWindow", StringComparison.OrdinalIgnoreCase) && window.IsVisible())
							{
								hierarchyWindow = window;
								hierarchyWindow.Repaint();
							}
							else if(string.Equals(window.GetType().Name, "ProjectBrowser", StringComparison.OrdinalIgnoreCase) && window.IsVisible())
							{
								projectWindow = window;
								projectWindow.Repaint();
							}
						}
					}

					if(hierarchyWindow != null || projectWindow != null)
					{
						getsOnGUIEvents = true;

						if(projectWindow != null)
						{
							EditorApplication.projectWindowItemOnGUI += ProjectOnGUI;
						}
						if(hierarchyWindow != null)
						{
							EditorApplication.hierarchyWindowItemOnGUI += HierarchyOnGUI;
						}
					}
					else
					{
						Instance().Repaint();
					}
				}
				isStillNeeded.Add(returnsTrueWhileOnGUICallbacksAreNeeded);				
			}
			#if DEV_MODE && DEBUG_ENABLED
			else { Debug.Log(StringUtils.ToColorizedString("EnsureOnGUICallbacks called but returnsTrueWhileOnGUICallbacksAreNeeded returned ", false)); }
			#endif
		}
		
		/// <returns> True if should stop receiving OnGUIEvents. </returns>
		private static bool OnGUIStatic()
		{
			if(preferences == null)
			{
				#if UNITY_EDITOR
				if(EditorApplication.isCompiling)
				{
					#if DEV_MODE
					Debug.Log("OnGUIEventHelper - waiting before fetching preferences asset because still compiling scripts...");
					#endif
					return false;
				}

				if(EditorApplication.isUpdating)
				{
					#if DEV_MODE
					Debug.Log("OnGUIEventHelper - waiting before fetching preferences asset because still updating asset database...");
					#endif
					return false;
				}
				#endif

				try
				{
					preferences = InspectorUtility.Preferences;
				}
				#if DEV_MODE
				catch(NullReferenceException e)
				{
					Debug.LogWarning("OnGUIEventHelper.OnGUI failed to fetch preferences asset. "+e);
				#else
				catch(NullReferenceException)
				{
				#endif
					return false;
				}

				if(preferences == null)
				{
					#if DEV_MODE
					Debug.LogWarning("OnGUIEventHelper.OnGUI failed to find preferences asset.");
					#endif
					return false;
				}
			}
			
			preferences.Setup();
			
			#if DEV_MODE && DEBUG_IS_STILL_NEEDED
			Debug.Log(StringUtils.ToColorizedString("EnsureOnGUICallbacks calling BeginOnGUI with isStillNeeded: "+ isStillNeeded.Count+", Event: ", Event.current));
			#endif

			DrawGUI.BeginOnGUI(preferences, true);
			
			if(Event.current.type == EventType.Layout)
			{
				InspectorManager.Instance().OnLayout();
				
				for(int n = isStillNeeded.Count - 1; n >= 0; n--)
				{
					if(isStillNeeded[n]())
					{
						return false;
					}
				}
				#if DEV_MODE && DEBUG_ENABLED
				Debug.Log("EnsureOnGUICallbacks closing because all "+ isStillNeeded.Count + " isStillNeeded returned false");
				#endif

				return true;
			}

			return false;
		}

		[UsedImplicitly]
		private void OnGUI()
		{
			if(!setupDone)
			{
				Setup();
			}

			if(OnGUIStatic())
			{
				Close();
			}
		}

		#if UNITY_2018_1_OR_NEWER
		private void OnApplicationQuitting()
		{
			Close();
		}

		private void OnBeforeAssemblyReload()
		{
			Close();
		}
		#endif

		[UsedImplicitly]
		private void OnDestroy()
		{
			#if DEV_MODE && DEBUG_ENABLED
			Debug.Log("OnGUIEventHelper.OnDestroy");
			#endif

			#if UNITY_2018_1_OR_NEWER
			EditorApplication.quitting -= OnApplicationQuitting;
			AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
			#endif

			getsOnGUIEvents = false;
			getsLayoutEvents = false;

			#if DEV_MODE && PI_ASSERTATIONS
			var instances = Resources.FindObjectsOfTypeAll<OnGUIEventHelper>();
			Debug.Assert(instances.Length == 0 || (instances.Length == 1 && instances[0] == this), StringUtils.ToString(instances.Length));
			#endif
		}		
	}
}
#endif