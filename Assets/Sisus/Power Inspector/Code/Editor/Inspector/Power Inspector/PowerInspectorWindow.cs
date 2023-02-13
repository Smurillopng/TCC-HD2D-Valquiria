using System;
using UnityEditor;
using UnityEngine;
using JetBrains.Annotations;
using Object = UnityEngine.Object;

namespace Sisus
{
	/// <summary>
	/// Window responsible for drawing Power Inspector views.
	/// </summary>
	public sealed class PowerInspectorWindow : InspectorDrawerWindow<PowerInspectorWindow, PowerInspector>, IHasCustomMenu
	{
		/// <inheritdoc/>
		public override bool CanSplitView
		{
			get
			{
				return true;
			}
		}

		/// <inheritdoc/>
		public override Type InspectorType
		{
			get
			{
				return typeof(PowerInspector);
			}
		}
		
		/// <summary>
		/// Creates a new instance of the Power Inspector Window.
		/// If a Power Inspector window already exists, then
		/// creates the new window as a tab on the existing instance.
		/// </summary>
		/// <returns> The newly created window </returns>
		[MenuItem(PowerInspectorMenuItemPaths.NewWindow, false, PowerInspectorMenuItemPaths.NewWindowPriority), UsedImplicitly]
		[NotNull]
		private static PowerInspectorWindow CreateNewWindow()
		{
			return CreateNew(false, true);
		}

		/// <summary>
		/// Creates a new instance of the Power Inspector Window.
		/// If a Power Inspector window already exists, then
		/// creates the new window as a tab on the existing instance.
		/// </summary>
		/// <returns> The newly created window </returns>
		[MenuItem(PowerInspectorMenuItemPaths.FocusOrOpenWindow, false, PowerInspectorMenuItemPaths.FocusOrOpenWindowPriority), UsedImplicitly]
		[NotNull]
		private static PowerInspectorWindow FocusOrOpen()
		{
			var existingInspector = InspectorManager.InstanceExists() ? InspectorManager.Instance().LastSelectedActiveOrDefaultInspector() : null;
			if(existingInspector != null)
			{
				var existingWindow = existingInspector.InspectorDrawer as PowerInspectorWindow;
				if(existingWindow != null)
				{
					existingWindow.FocusWindow();
					return existingWindow;
				}
			}

			return CreateNew(true, true);
		}

		/// <summary>
		/// Creates a new instance of the Power Inspector Window.
		/// If a Power Inspector window already exists, then
		/// creates the new window as a tab on the existing instance.
		/// </summary>
		/// <returns> The newly created window </returns>
		[MenuItem(PowerInspectorMenuItemPaths.NewTab, false, PowerInspectorMenuItemPaths.NewTabPriority), UsedImplicitly]
		[NotNull]
		private static PowerInspectorWindow CreateNew()
		{
			return CreateNew(true, true);
		}

		/// <summary>
		/// Creates a new instance of the Power Inspector Window.
		/// If a Power Inspector window already exists, then
		/// creates the new window as a tab on the existing instance.
		/// </summary>
		/// <param name="addAsTab"> True to add as tab to existing window that was last interacted with. </param>
		/// <param name="show"> True to also show the window, false to leave it hidden so it can be shown manually later on. </param>
		/// <returns> The newly created window </returns>
		[NotNull]
		public static PowerInspectorWindow CreateNew(bool addAsTab, bool show = true)
		{
			var created = CreateNewWithoutShowing<PowerInspectorWindow>(addAsTab);
			created.minSize = new Vector2(280f, 130f);

			if(show)
			{
				created.Show();

				#if DEV_MODE
				Debug.Log("FocusWindow");
				#endif
				created.FocusWindow();
			}
			return created;
		}

		/// <summary>
		/// Creates a new instance of the Power Inspector Window.
		/// </summary>
		/// <param name="inspect"> The inspect. </param>
		/// <param name="lockView"> (Optional) True to lock, false to unlock the view. </param>
		/// <param name="addAsTab"> (Optional) True to add as tab. </param>
		/// <param name="minSize"> (Optional) Size of the minimum. </param>
		/// <returns> The newly created window </returns>
		[NotNull]
		public static PowerInspectorWindow CreateNew(Object[] inspect, bool lockView = false, bool addAsTab = true, Vector2 minSize = default(Vector2))
		{
			return CreateNew<PowerInspectorWindow>(DEFAULT_WINDOW_TITLE, inspect, lockView, addAsTab, minSize);
		}

		[UsedImplicitly, MenuItem(PowerInspectorMenuItemPaths.CloseTab, false, PowerInspectorMenuItemPaths.CloseTabPriority)]
		private static void CloseTabFromMenu()
		{
			#if DEV_MODE
			Debug.Log("Close tab from menu");
			#endif

			var manager = InspectorUtility.ActiveManager;
			if(manager == null)
			{
				return;
			}

			var inspector = manager.LastSelectedActiveOrDefaultInspector();
			if(inspector == null)
			{
				#if DEV_MODE
				Debug.Log("LastSelectedActiveOrDefaultInspector null");
				#endif
				return;
			}

			#if DEV_MODE
			Debug.Log("Closing Power Inspector Tab");
			#endif

			var inspectorDrawer = inspector.InspectorDrawer as PowerInspectorWindow;
			if(inspectorDrawer == null)
			{
				return;
			}
			inspectorDrawer.CloseTab();
		}
		
		[UsedImplicitly, MenuItem(PowerInspectorMenuItemPaths.CloseTab, true)]
		private static bool CloseTabFromMenuIsPossible()
		{
			var manager = InspectorUtility.ActiveManager;
			if(manager == null)
			{
				return false;
			}

			var inspector = manager.LastSelectedActiveOrDefaultInspector();
			if(inspector == null)
			{
				return false;
			}

			var inspectorDrawer = inspector.InspectorDrawer as PowerInspectorWindow; 
			if(inspectorDrawer == null)
			{
				return false;
			}

			return true;
		}

		#if !UNITY_2017_1_OR_NEWER
		[UnityEditor.Callbacks.DidReloadScripts, UsedImplicitly]
		private static void DidReloadScripts()
		{
			var manager = InspectorUtility.ActiveManager;
			if(manager == null)
			{
				return;
			}

			var inspectors = manager.ActiveInstances;
			int count = inspectors.Count;
			switch(count)
			{
				case 0:
					return;
				case 1:
					var powerInspectorWindow = inspectors[0].InspectorDrawer as PowerInspectorWindow;
					if(powerInspectorWindow != null)
					{
						powerInspectorWindow.OnScriptsReloaded();
					}
					return;
				default:
					var called = new System.Collections.Generic.HashSet<PowerInspectorWindow>();
					for(int n = 0; n < count; n++)
					{
						powerInspectorWindow = inspectors[n].InspectorDrawer as PowerInspectorWindow;
						if(powerInspectorWindow != null && !called.Add(powerInspectorWindow))
						{
							powerInspectorWindow.OnScriptsReloaded();
						}
					}
					return;
			}
		}
		#endif

		/// <summary> Adds Power Inspector related items to opening EditorWindow context menu. </summary>
		/// <param name="menu"> The opening EditorWindow context menu. </param>
		public void AddItemsToMenu(GenericMenu menu)
		{
			// NOTE: This causes a small issue with the opening menu containing two separator lines at the bottom
			// once Unity adds its own items to the menu, but I think the usefulness of this functionality still
			// makes it a worthy trade-off.
			menu.AddItem(new GUIContent("Add Tab/Power Inspector"), false, () => CreateNew());
			menu.AddSeparator("");

			var view = SelectedOrDefaultView();

			menu.AddItem(new GUIContent("Debug Mode+/Off"), !view.State.DebugMode, view.DisableDebugMode);
			menu.AddItem(new GUIContent("Debug Mode+/On"), view.State.DebugMode, view.EnableDebugMode);

			menu.AddItem(new GUIContent("Target Window/All", "Display information for targets selected in all windows."), inspectorTargetingMode == InspectorTargetingMode.All, SetTargetingModeAll);
			menu.AddItem(new GUIContent("Target Window/Hierarchy", "Display information only for targets selected in Hierarchy window."), inspectorTargetingMode == InspectorTargetingMode.Hierarchy, SetTargetingModeHierarchy);
			menu.AddItem(new GUIContent("Target Window/Project", "Display information only for targets selected in Project window."), inspectorTargetingMode == InspectorTargetingMode.Project, SetTargetingModeProject);

			var windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
			var windowSelectionManager = SelectionManager as TargetWindowSelectionManager;

			for(int n = windows.Length - 1; n >= 0; n--)
			{
				var window = windows[n];
				if(window == this || window.titleContent == null || string.IsNullOrEmpty(window.titleContent.text) || typeof(IInspectorDrawer).IsAssignableFrom(window.GetType()))
				{
					continue;
				}
				
				menu.AddItem(new GUIContent("Target Window/Custom/" + window.titleContent.text + " (" + window.GetType().Name + ")"), windowSelectionManager != null && windowSelectionManager.TargetWindow == window, SetTargetingModeUserDefined, window);
			}

			menu.AddItem(new GUIContent("Documentation", "View Power Inspector Online Documentation."), false, PowerInspectorDocumentation.Show);

			if((Event.current != null && Event.current.shift) || DevMode.Enabled)
			{
				var devModeLabel = new GUIContent("Developer Mode/Enabled", "Enable development mode for Power Inspector to gain access to debug logs and various developer tools.");
				if(DevMode.Enabled)
				{
					menu.AddItem(devModeLabel, true, DevMode.Disable);
				}
				else
				{
					menu.AddItem(devModeLabel, false, DevMode.Enable);
				}
			}

			#if DEV_MODE || PI_ENABLE_MINIMIZE //WIP FEATURE
			minimizer.AddMinimizeItemsToMenu(menu);
			#endif
		}

        protected override ISelectionManager GetDefaultSelectionManager()
		{
			switch(InspectorTargetingMode)
			{
				case InspectorTargetingMode.Hierarchy:
					return HierarchySelectionManager.Instance();
				case InspectorTargetingMode.Project:
					return ProjectSelectionManager.Instance();
				default:
					return EditorSelectionManager.Instance();
			}
		}

		private void SetTargetingModeAll()
		{
			if(inspectorTargetingMode != InspectorTargetingMode.All)
			{
				inspectorTargetingMode = InspectorTargetingMode.All;
				SelectionManager = GetDefaultSelectionManager();
				minimizer.Setup(this, SelectionManager, minimizer.AutoMinimize);
				UpdateWindowIcon();
				RebuildDrawerIfTargetsChanged();
			}
		}
		
		private void SetTargetingModeHierarchy()
		{
			if(inspectorTargetingMode != InspectorTargetingMode.Hierarchy)
			{
				inspectorTargetingMode = InspectorTargetingMode.Hierarchy;
				SelectionManager = HierarchySelectionManager.Instance();
				titleContent = GetTitleContent(InspectorTargetingMode.Hierarchy, true);
				minimizer.Setup(this, SelectionManager, minimizer.AutoMinimize);
				RebuildDrawerIfTargetsChanged();
			}
		}

		private void SetTargetingModeProject()
		{
			if(inspectorTargetingMode != InspectorTargetingMode.Project)
			{
				inspectorTargetingMode = InspectorTargetingMode.Project;
				SelectionManager = ProjectSelectionManager.Instance();
				minimizer.Setup(this, SelectionManager, minimizer.AutoMinimize);
				titleContent = GetTitleContent(InspectorTargetingMode.Project, true);
				RebuildDrawerIfTargetsChanged();
			}
		}

		private void SetTargetingModeUserDefined(object parameter)
		{
			var window = parameter as EditorWindow;

			if(inspectorTargetingMode != InspectorTargetingMode.UserDefined && window)
			{
				inspectorTargetingMode = InspectorTargetingMode.UserDefined;
				SelectionManager = new TargetWindowSelectionManager(window);

				minimizer.Setup(this, SelectionManager, minimizer.AutoMinimize);
				titleContent = GetTitleContent(InspectorTargetingMode.UserDefined, true);
				RebuildDrawerIfTargetsChanged();
			}
		}
	}
}