//#define DEBUG_SHOW_IF_OPEN
//#define DEBUG_OPEN_URL

using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using Sisus.WebView;

namespace Sisus
{
	public sealed class PowerInspectorDocumentationWindow : EditorWindow, IHasCustomMenu
	{
		public static void OpenWindow([NotNull]string url = "", WebpageLoadFailed onFailToLoadUrl = null)
		{
			#if !UNITY_2020_1_OR_NEWER // WebView class no longer exists in Unity 2020.1 or later
			Open(false, url, onFailToLoadUrl);
			#endif
		}

		public static bool ShowPageIfWindowOpen([NotNull]string url, WebpageLoadFailed onFailToLoadUrl = null)
		{
			#if !UNITY_2020_1_OR_NEWER // WebView class no longer exists in Unity 2020.1 or later
			if(TryFindExistingInstance(out instance))
			{
				#if DEV_MODE
				Debug.Log("onFailToLoadNextUrl = "+StringUtils.ToString(onFailToLoadUrl));
				#endif

				if(!instance.autoUpdateEnabled)
				{
					#if DEV_MODE
					Debug.Log("PowerInspectorDocumentationWindow.ShowPageIfWindowOpen("+url+ ") - won't show because autoUpdateEnabled was false.");
					#endif
					return false;
				}

				instance.onFailToLoadNextUrl = onFailToLoadUrl;
				instance.OpenUrl(url);
				return true;
			}
			#if DEV_MODE && DEBUG_SHOW_IF_OPEN
			Debug.Log("PowerInspectorDocumentationWindow.ShowPageIfWindowOpen("+url+") - won't show because window was not open.");
			#endif

			#endif

			return false;
		}
		
		private const float sideBarWidth = 300f;
		private const float mainViewWidth = 660f;
		private const float width = sideBarWidth + mainViewWidth;
		private const float yOffset = 287f;
		private const float sideBarItemHeight = 25f;
		private const float height = 895f + 45f;
		private static readonly Color32 ViewInBrowserButtonColor = new Color32(255, 231, 165, 255);
		private static readonly Color32 AutoUpdateEnabledColor = new Color32(120, 252, 148, 255);
		private static readonly Color32 AutoUpdateDisabledColor = new Color32(255, 135, 135, 255);
		private static PowerInspectorDocumentationWindow instance;
				
		private static readonly KeyValuePair<string, string>[] Urls =
		{
			new KeyValuePair<string,string>("Opening The Power Inspector Window", "getting-started/opening-power-inspector-window"),
			new KeyValuePair<string,string>("Using The Power Inspector Window", "getting-started/using-the-power-inspector-window"),
			new KeyValuePair<string,string>("Class Member Visibility", "getting-started/class-member-visibility"),
			new KeyValuePair<string,string>("Editing Preferences", "getting-started/editing-preferences"),
			new KeyValuePair<string,string>("List Of Main Features", "features/list-of-main-features"),
			new KeyValuePair<string,string>("Toolbar", "features/toolbar"),
			new KeyValuePair<string,string>("Back And Forward Buttons", "features/back-and-forward-buttons"),
			new KeyValuePair<string,string>("View Menu", "features/view-menu"),
			new KeyValuePair<string,string>("Search Box", "features/search-box"),
			new KeyValuePair<string,string>("Split View", "features/split-view"),
			new KeyValuePair<string,string>("Keyboard Navigation", "features/keyboard-navigation"),
			new KeyValuePair<string,string>("Dynamic Prefix Column", "features/dynamic-prefix-column"),
			new KeyValuePair<string,string>("Copy-Paste", "features/copy-paste"),
			new KeyValuePair<string,string>("Reset", "features/reset"),
			new KeyValuePair<string,string>("Display Anything", "features/display-anything"),
			new KeyValuePair<string,string>("Context Menu Items", "features/context-menu-items"),
			new KeyValuePair<string,string>("Inspecting Static Members", "features/inspecting-static-members"),
			new KeyValuePair<string,string>("Debug Mode+", "features/debug-mode"),
			new KeyValuePair<string,string>("Quick Invoke Menu", "features/quick-invoke-menu"),
			new KeyValuePair<string,string>("Improved Tooltips", "features/tooltips"),
			new KeyValuePair<string,string>("Target Window", "features/target-window"),
			new KeyValuePair<string,string>("Multi-Editing Modes", "features/multi-editing-modes"),
			new KeyValuePair<string,string>("Prefab Quick Editing", "features/prefab-quick-editing"),
			new KeyValuePair<string,string>("Create Script Wizard", "features/create-script-wizard"),
			new KeyValuePair<string,string>("Hierarchy Folders", "features/hierarchy-folders"),
			
			new KeyValuePair<string,string>("Enum Drawer", "features/enum-drawer"),
			new KeyValuePair<string,string>("GameObject Drawer", "features/gameobject-drawer"),
			new KeyValuePair<string,string>("Object Reference Drawer", "features/object-reference-drawer"),
			new KeyValuePair<string,string>("Script Drawer", "features/script-drawer"),
			new KeyValuePair<string,string>("Transform Drawer", "features/transform-drawer"),
			new KeyValuePair<string,string>("Unity Object Drawer", "features/unity-object-drawer"),

			new KeyValuePair<string,string>("Preferences", "category/preferences"),

			new KeyValuePair<string,string>("Compatibility", "category/compatibility"),

			//new KeyValuePair<string,string>("Contacting Support", "troubleshooting/contacting-support"),
			//new KeyValuePair<string,string>("Supported Unity Versions", "troubleshooting/supported-unity-versions"),
			//new KeyValuePair<string,string>("Type Not Found Errors", "troubleshooting/type-not-found-errors"),
			new KeyValuePair<string,string>("Troubleshooting", "category/troubleshooting"),

			new KeyValuePair<string,string>("Extending Power Inspector", "category/extending-power-inspector"),

			new KeyValuePair<string,string>("Terminology", "category/terminology")
		};

		private int activeView;
		private WebViewHook webView;
		private string url = "https://docs.sisus.co/power-inspector/getting-started/opening-power-inspector-window/";
		private bool reload;

		private Rect viewInBrowserRect;
		private Rect autoUpdateRect;
		private Rect separator1;
		private Rect separator2;
		private WebpageLoadFailed onFailToLoadNextUrl;

		[MenuItem(PowerInspectorMenuItemPaths.Documentation, false, PowerInspectorMenuItemPaths.DocumentationPriority), UsedImplicitly]
		public static void Open()
		{
			#if !UNITY_2020_1_OR_NEWER // WebView class no longer exists in Unity 2020.1 or later
			Open(true, "", null);
			#else
			PowerInspectorDocumentation.Show();
			#endif
		}

		private static bool TryFindExistingInstance(out PowerInspectorDocumentationWindow existingInstance)
		{
			if(instance != null)
			{
				existingInstance = instance;
				return true;
			}

			var existingInstances = Resources.FindObjectsOfTypeAll<PowerInspectorDocumentationWindow>();
			if(existingInstances.Length > 0)
			{
				existingInstance = existingInstances[0];
				return true;
			}
			existingInstance = null;
			return false;
		}

		public static void Open(bool focus, [CanBeNull]string url, [CanBeNull]WebpageLoadFailed onFailToLoadUrl)
		{
			if(instance == null && !TryFindExistingInstance(out instance))
			{
				instance = CreateInstance<PowerInspectorDocumentationWindow>();
				instance.titleContent = new GUIContent("Power Inspector Documentation");
				instance.minSize = new Vector2(width, height);
				instance.maxSize = new Vector2(width, height);
				instance.ShowUtility();
			}

			if(focus)
			{
				#if DEV_MODE
				Debug.Log("Focusing PowerInspectorDocumentationWindow");
				#endif
				instance.Focus();
			}

			if(!string.IsNullOrEmpty(url))
			{
				#if DEV_MODE
				Debug.Log("onFailToLoadNextUrl = "+StringUtils.ToString(onFailToLoadUrl));
				#endif

				instance.onFailToLoadNextUrl = onFailToLoadUrl;
				instance.OpenUrl(url);
			}
		}
		
		private void OpenUrl(string urlToOpen)
		{
			if(urlToOpen.Length == 0 || string.Equals(urlToOpen, url))
			{
				return;
			}

			#if DEV_MODE && DEBUG_OPEN_URL
			Debug.Log("OPENING URL: "+ urlToOpen+". Current url="+ url);
			#endif

			reload = true;
			url = urlToOpen;

			int startLength = PowerInspectorDocumentation.BaseUrl.Length;
			if(url.Length > startLength)
			{
				var urlEnding = url.Substring(startLength);

				#if DEV_MODE && DEBUG_OPEN_URL
				Debug.Log("urlEnding: "+urlEnding);
				#endif

				for(int n = 0, count = Urls.Length; n < count; n++)
				{
					if(string.Equals(Urls[n].Value, urlEnding))
					{
						activeView = n;

						#if DEV_MODE && DEBUG_OPEN_URL
						Debug.Log("activeView: "+n);
						#endif
					}
				}
			}

			GUI.changed = true;
			Repaint();
		}

		private void SetUrl(string newUrl)
		{
			if(newUrl.EndsWith("/"))
			{
				newUrl = newUrl.Substring(0, newUrl.Length - 1);
			}

			if(string.Equals(url, newUrl))
			{
				#if DEV_MODE
				Debug.Log("SetUrl same as existing url: "+url);
				#endif
				return;
			}
			
			reload = true;
			GUI.changed = true;
			Repaint();

			url = newUrl;
			
			activeView = -1;

			int startLength = PowerInspectorDocumentation.BaseUrl.Length;
			if(url.Length > startLength)
			{
				var urlEnding = url.Substring(startLength);

				#if DEV_MODE
				Debug.Log("urlEnding: "+urlEnding);
				#endif

				for(int n = 0, count = Urls.Length; n < count; n++)
				{
					if(string.Equals(Urls[n].Value, urlEnding))
					{
						activeView = n;

						#if DEV_MODE
						Debug.Log("activeView: "+n);
						#endif
						return;
					}
				}
			}
			#if DEV_MODE
			else { Debug.Log("Url was too short to be a subpage of PowerInspectorDocumentation.BaseUrl: "+url); }
			#endif

			#if DEV_MODE
			if(activeView == -1) { Debug.Log("activeView set to -1 because loaded url "+newUrl+" not found in list"); }
			#endif
		}
		
		[UsedImplicitly]
		private void OnEnable()
		{
			if(!webView)
			{
				webView = CreateInstance<WebViewHook>();
				webView.LoadError = OnFailedToLoadWebpage;
				webView.LocationChanged = OnLoadedWebpage;
			}

			viewInBrowserRect = sideBarRect;
			viewInBrowserRect.y = sideBarItemHeight * Urls.Length + 10f;
			viewInBrowserRect.x = sideBarWidth * 0.5f - 130f;
			viewInBrowserRect.width = 120f;
			viewInBrowserRect.height = 20f;

			autoUpdateRect = viewInBrowserRect;
			autoUpdateRect.x += viewInBrowserRect.width + 20f;

			separator1 = Rect.zero;
			separator1.width = 1f;
			separator1.height = height;

			separator2 = separator1;
			separator2.x = sideBarWidth - 1f;
		}
		
		private void OnFailedToLoadWebpage(string failedToLoadUrl)
		{
			if(onFailToLoadNextUrl != null)
			{
				var callback = onFailToLoadNextUrl;

				#if DEV_MODE
				Debug.Log("onFailToLoadNextUrl = null");
				#endif

				onFailToLoadNextUrl = null;
				callback(failedToLoadUrl);
				return;
			}

			DrawGUI.Active.DisplayDialog("Failed to Load Webpage", "Failed to load webpage:\n"+failedToLoadUrl+".\n\nPlease make sure you are connected to the internet.", "Ok");

			OpenUrl(PowerInspectorDocumentation.PreferencesUrl);
		}

		private void OnLoadedWebpage(string loadedUrl)
		{
			SetUrl(loadedUrl);
		}

		[UsedImplicitly]
		private void OnBecameInvisible()
		{
			if(webView)
			{
				// signal the browser to unhook
				webView.Detach();
			}
		}

		[UsedImplicitly]
		private void OnDestroy()
		{
			DestroyImmediate(webView);
		}

		private readonly Rect sideBarRect = new Rect(0f, 0f, sideBarWidth, height);
	
		[UsedImplicitly]
		private void OnGUI()
		{
			if(!InspectorUtility.Preferences.SetupDone)
			{
				InspectorUtility.Preferences.Setup();
			}

			#if DEV_MODE
			if(Event.current.type == EventType.KeyDown)
			{
				Debug.Log(StringUtils.ToString(Event.current));
			}
			if(Event.current.type == EventType.MouseDown)
			{
				Debug.Log(StringUtils.ToString(Event.current));
			}
			#endif

			DrawSideBar(sideBarRect);

			if(webView.Hook(this) || reload)
			{
				reload = false;
				webView.LoadURL(url);
				GUI.changed = true;
				Repaint();
			}

			switch(Event.current.type)
			{
				case EventType.KeyDown:
					if(this == focusedWindow)
					{
						switch(Event.current.keyCode)
						{
							case KeyCode.Escape:
								Close();
								break;
							case KeyCode.F5:
								webView.Reload();
								break;
							case KeyCode.UpArrow:
								SetActiveView(activeView - 1);
								break;
							case KeyCode.DownArrow:
								SetActiveView(activeView + 1);
								break;
							case KeyCode.Home:
								SetActiveView(0);
								break;
							case KeyCode.End:
								SetActiveView(Urls.Length - 1);
								break;
							case KeyCode.PageUp:
								SetActiveView(activeView - 10);
								break;
							case KeyCode.PageDown:
								SetActiveView(activeView + 10);
								break;
						}
					}
					break;
				case EventType.Repaint:
					webView.OnGUI(new Rect(sideBarWidth, -yOffset, 1002f, position.height + yOffset));
					break;
			}
		}

		private void DrawSideBar(Rect sideBarRect)
		{
			var guiColorWas = GUI.color;

			var drawRect = sideBarRect;
			drawRect.height = sideBarItemHeight;
			for(int n = 0, count = Urls.Length; n < count; n++)
			{
				var header = Urls[n].Key;
				if(n == activeView)
				{
					GUI.color = InspectorUtility.Preferences.theme.BackgroundSelected;
					GUI.Label(drawRect, header, InspectorPreferences.Styles.SideBarItem);
				}
				else
				{
					GUI.color = guiColorWas;
					if(GUI.Button(drawRect, header, InspectorPreferences.Styles.SideBarItem))
					{
						SetActiveView(n);
					}
				}
				drawRect.y += sideBarItemHeight;
			}
			
			DrawGUI.DrawLine(separator1, Color.black);
			DrawGUI.DrawLine(separator2, Color.black);

			GUI.color = ViewInBrowserButtonColor;
			viewInBrowserLabel.tooltip = url;
			if(GUI.Button(viewInBrowserRect, viewInBrowserLabel, InspectorPreferences.Styles.SideBarItem))
			{
				OpenInBrowser();
			}

			GUI.color = autoUpdateEnabled ? AutoUpdateEnabledColor : AutoUpdateDisabledColor;
			if(GUI.Button(autoUpdateRect, autoUpdateEnabled ? disableAutoUpdate : enableAutoUpdate, InspectorPreferences.Styles.SideBarItem))
			{
				autoUpdateEnabled = !autoUpdateEnabled;
			}

			GUI.color = guiColorWas;
		}

		[SerializeField]
		private bool autoUpdateEnabled = true;
		private readonly GUIContent viewInBrowserLabel = new GUIContent("View in Browser");
		private readonly GUIContent enableAutoUpdate = new GUIContent("Auto Update : Off", "Enable auto update to automatically change displayed page to ones you might find relevant.");
		private readonly GUIContent disableAutoUpdate = new GUIContent("Auto Update : On", "Disable auto update if you don't want the displayed page to automatically change to ones might find relevant.");

		private void SetActiveView(int index)
		{
			if(index < 0)
			{
				index = 0;
			}
			else if(index >= Urls.Length)
			{
				index = Urls.Length - 1;
			}

			activeView = index;
			url = PowerInspectorDocumentation.GetUrl(Urls[index].Value);

			#if DEV_MODE
			Debug.Log("OPENING URL: "+url);
			#endif

			webView.LoadURL(url);
			GUI.changed = true;
			Repaint();
		}

		public void AddItemsToMenu(GenericMenu menu)
		{
			menu.AddItem(new GUIContent("View In Browser", url), false, OpenInBrowser);
			
			#if DEV_MODE
			var monoScript = MonoScript.FromScriptableObject(this);
			if(monoScript != null)
			{
				menu.AddItem(new GUIContent("Edit "+GetType().Name+".cs"), false, ()=>AssetDatabase.OpenAsset(monoScript));
			}
			#endif
		}

		private void OpenInBrowser()
		{
			Application.OpenURL(url);
		}
	}
}