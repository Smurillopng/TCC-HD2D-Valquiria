#define DEBUG_ADD_MENU_ITEM
//#define DEBUG_OPEN_MENU

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;
using UnityEditor;

namespace Sisus
{
	/// <summary>
	/// Utility class that handles the opening of context menus and informing other systems of this taking place.
	/// </summary>
	[InitializeOnLoad]
	public static class ContextMenuUtility
	{
		/// <summary>
		/// Delegate for when a menu is opening.
		/// </summary>
		/// <param name="menu"> The menu that is opening. </param>
		/// <param name="inspector"> The inspector inside which the context menu is opening. Null if menu is not opening inside any inspector. </param>
		/// <param name="menuSubject"> The subject for which the context menu is opening. Null if menu target is not not a Drawer instance. </param>
		public delegate void MenuOpeningCallback([NotNull]Menu menu, [CanBeNull]IInspector inspector, [CanBeNull]IDrawer menuSubject);
		
		/// <summary>
		/// Delegate that is called right before a right click menu is being opened. Allows subscribers to put modify the contents of the menu before its opened.
		/// </summary>
		public static event MenuOpeningCallback OnMenuOpening;

		public static event Action<object> OnMenuClosed;

		private static IInspector openingMenuInspector;
		private static InspectorPart openingMenuInspectorPart;
		private static Menu openingMenu;
		private static DrawerTarget openingMenuSubject;
		private static object openingMenuPart;
		private static Rect? openingMenuPosition;
		private static bool disposeMenuAfterOpening;
		
		// A short delay is necessary between right click event happening,
		// and the context menu opening, as otherwise positioning of
		// mouseover effect graphics can be off.
		private static int openDelayCounter;

		public static bool MenuIsOpening
		{
			get
			{
				return openingMenu != null;
			}
		}

		public static Menu OpeningMenu
		{
			get
			{
				return openingMenu;
			}
		}
		
		private static bool IsSafeToChangeInspectorContents
		{
			get
			{
				return InspectorUtility.IsSafeToChangeInspectorContents;
			}
		}

		/// <summary>
		/// This is initialized on load due to the usage of the InitializeOnLoad attribute.
		/// </summary>
		static ContextMenuUtility()
		{
			EditorApplication.contextualPropertyMenu -= OnPropertyContextMenuOpen;
			EditorApplication.contextualPropertyMenu += OnPropertyContextMenuOpen;
		}

		/// <summary>
		/// Waits until the active inspector has finished rendering and then opens the menu.
		/// Delaying the opening of the menu like this can help avoid with some graphical glitches that can happen if the menu is opened in the middle of an inspector being drawn.
		/// 
		/// Once opened context menu has been closed it will be disposed, and the drawer for which the menu was opened will be selected (if not null).
		/// </summary>
		/// <param name="menu"> Menu that should be opened. </param>
		/// <param name="subject"> IDrawer whose context menu is being opened. Null if context menu doesn't belong to any IDrawer (e.g. toolbar menu item). </param>
		public static void Open([NotNull]Menu menu, [CanBeNull]IDrawer subject)
		{
			if(subject == null)
			{
				Open(menu, true, null, InspectorPart.None, null, null, null);
			}
			else
			{
				Open(menu, true, subject.Inspector, InspectorPart.Viewport, subject, subject.MouseoveredPart, SelectLastContextMenuSubject);
			}
		}

		/// <summary>
		/// Waits until the active inspector has finished rendering and then opens the menu.
		/// Delaying the opening of the menu like this can help avoid with some graphical glitches that can happen if the menu is opened in the middle of an inspector being drawn.
		/// 
		/// Once opened context menu has been closed it will be disposed, and the drawer for which the menu was opened will be selected (if not null).
		/// </summary>
		/// <param name="menu"> Menu that should be opened. </param>
		/// <param name="subject"> IDrawer whose context menu is being opened. Null if context menu doesn't belong to any IDrawer (e.g. toolbar menu item). </param>
		/// <param name="part"> Part of the subject for which the context menu is opened. E.g. reference to the targeted header toolbar button. Can be null. </param>
		public static void Open([NotNull]Menu menu, [CanBeNull]IDrawer subject, [CanBeNull]object part)
		{
			if(subject == null)
			{
				Open(menu, true, null, InspectorPart.None, null, part, null);
			}
			else
			{
				Open(menu, true, subject.Inspector, InspectorPart.Viewport, subject, part, SelectLastContextMenuSubject);
			}
		}

		/// <summary>
		/// Waits until the active inspector has finished rendering and then opens the menu.
		/// Delaying the opening of the menu like this can help avoid with some graphical glitches that can happen if the menu is opened in the middle of an inspector being drawn.
		/// </summary>
		/// <param name="menu"> Menu that should be opened. </param>
		/// <param name="disposeAfter"> Should the menu object be disposed after menu has been opened? Set this to false if the Menu is cached and reused, otherwise set this true. </param>
		/// <param name="inspector"> Inspector for which the context menu is being opened. Null if not opening for any inspector. </param>
		/// <param name="inspectorPart"> Part of the inspector for which the context menu is being opened. None if not opening for any inspector. </param>
		/// <param name="subject"> IDrawer for which the context menu is opened. Null if context menu doesn't belong to any IDrawer (e.g. toolbar menu item). </param>
		/// <param name="part"> Part of the subject for which the context menu is opened. E.g. reference to the targeted toolbar item. Can be null. </param>
		/// <param name="doOnMenuClosed"> Delegate to be called once menu has closed. Set to ContextMenuUtility.SelectLastContextMenuSubject to select the context menu subject once the context menu window has been closed. </param>
		public static void Open([NotNull]Menu menu, bool disposeAfter, [CanBeNull]IInspector inspector, InspectorPart inspectorPart, [CanBeNull]IDrawer subject, [CanBeNull]object part, Action<object> doOnMenuClosed = null)
		{
			#if DEV_MODE
			Debug.Assert(menu.Count > 0);
			Debug.Assert(inspector != null || subject == null);
			#endif

			openingMenu = menu;
			openingMenuInspector = inspector;
			openingMenuInspectorPart = inspectorPart;
			openingMenuSubject = new DrawerTarget(subject);
			openingMenuPart = part;
			openingMenuPosition = null;
			disposeMenuAfterOpening = disposeAfter;
			OnMenuClosed = doOnMenuClosed;

			if(IsSafeToChangeInspectorContents || inspector == null)
			{
				#if DEV_MODE
				Debug.Log("Opening context menu immediately");
				#endif
				OpenContextMenu();
			}
			else
			{
				openDelayCounter = 2;
			}
		}

		/// <summary>
		/// Waits until the active inspector has finished rendering and then opens the menu at the given position as a dropdown menu.
		/// Delaying the opening of the menu like this can help avoid with some graphical glitches that can happen if the menu is opened in the middle of an inspector being drawn.
		/// 
		/// Once opened context menu has been closed it will be disposed, and the drawer for which the menu was opened will be selected (if not null).
		/// </summary>
		/// <param name="menu"> Menu that should be opened. </param>
		/// <param name="position"> Position where menu should be opened. </param>
		/// <param name="subject"> IDrawer whose context menu is being opened. Null if context menu doesn't belong to any IDrawer (e.g. toolbar menu item). </param>
		public static void OpenAt([NotNull]Menu menu, Rect position, [CanBeNull]IDrawer subject)
		{
			OpenAt(menu, position, true, subject == null ? null : subject.Inspector, subject == null ? InspectorPart.None : InspectorPart.Viewport, subject, subject.SelectedPart, SelectLastContextMenuSubject);
		}

		/// <summary>
		/// Waits until the active inspector has finished rendering and then opens the menu at the given position as a dropdown menu.
		/// Delaying the opening of the menu like this can help avoid with some graphical glitches that can happen if the menu is opened in the middle of an inspector being drawn.
		/// 
		/// Once opened context menu has been closed it will be disposed, and the drawer for which the menu was opened will be selected (if not null).
		/// </summary>
		/// <param name="menu"> Menu that should be opened. </param>
		/// <param name="position"> Position where menu should be opened. </param>
		/// <param name="subject"> IDrawer whose context menu is being opened. Null if context menu doesn't belong to any IDrawer (e.g. toolbar menu item). </param>
		/// <param name="part"> Part of the subject for which the context menu is opened. E.g. reference to the targeted header toolbar button. Can be null. </param>
		public static void OpenAt([NotNull]Menu menu, Rect position, [CanBeNull]IDrawer subject, [CanBeNull]object part)
		{
			OpenAt(menu, position, true, subject == null ? null : subject.Inspector, subject == null ? InspectorPart.None : InspectorPart.Viewport, subject, part, SelectLastContextMenuSubject);
		}

		/// <summary>
		/// Waits until the active inspector has finished rendering and then opens the menu at the given position as a dropdown menu.
		/// Delaying the opening of the menu like this can help avoid with some graphical glitches that can happen if the menu is opened in the middle of an inspector being drawn
		/// 
		/// Once opened context menu has been the subject of the context menu will be selected (if necessary data is provided).
		/// </summary>
		/// <param name="menu"> Menu that should be opened. </param>
		/// <param name="position"> Position where menu should be opened. </param>
		/// <param name="disposeAfter">
		/// Should the menu object be disposed after menu has been opened?
		/// Set this to false if the Menu is cached and reused, otherwise set this true.
		/// </param>
		/// <param name="inspector"> Inspector for which the context menu is being opened. Null if not opening for any inspector. </param>
		/// <param name="inspectorPart"> Part of the inspector for which the context menu is being opened. None if not opening for any inspector. </param>
		/// <param name="subject"> IDrawer whose context menu is being opened. Null if context menu doesn't belong to any IDrawer (e.g. toolbar menu item). </param>
		/// <param name="part"> Can contain the part of the subject for which the context menu is opened. E.g. reference to the targeted toolbar item. </param>
		public static void OpenAt([NotNull]Menu menu, Rect position, bool disposeAfter, [CanBeNull]IInspector inspector, InspectorPart inspectorPart, [CanBeNull]IDrawer subject, [CanBeNull]object part)
		{
			OpenAt(menu, position, disposeAfter, inspector, inspectorPart, subject, part, SelectLastContextMenuSubject);
		}

		/// <summary>
		/// Waits until the active inspector has finished rendering and then opens the menu
		/// at the given position as a dropdown menu.
		/// Delaying the opening of the menu like this can help avoid with some graphical glitches that
		/// can happen if the menu is opened in the middle of an inspector being drawn
		/// </summary>
		/// <param name="menu"> Menu that should be opened. </param>
		/// <param name="position"> Position where menu should be opened. </param>
		/// <param name="disposeAfter">
		/// Should the menu object be disposed after menu has been opened?
		/// Set this to false if the Menu is cached and reused, otherwise set this true.
		/// </param>
		/// <param name="inspector"> Inspector for which the context menu is being opened. Null if not opening for any inspector. </param>
		/// <param name="inspectorPart"> Part of the inspector for which the context menu is being opened. None if not opening for any inspector. </param>
		/// <param name="subject"> IDrawer whose context menu is being opened. Null if context menu doesn't belong to any IDrawer (e.g. toolbar menu item). </param>
		/// <param name="part"> Can contain the part of the subject for which the context menu is opened. E.g. reference to the targeted toolbar item. </param>
		/// <param name="doOnMenuClosed"> Delegate to be called once menu has closed. Set to ContextMenuUtility.SelectLastContextMenuSubject to select the context menu subject once the context menu window has been closed. Can be null. </param>
		public static void OpenAt([NotNull]Menu menu, Rect position, bool disposeAfter, [CanBeNull]IInspector inspector, InspectorPart inspectorPart, [CanBeNull]IDrawer subject, [CanBeNull]object part, [CanBeNull]Action<object> doOnMenuClosed)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(menu != null);
			Debug.Assert(menu.Count > 0);
			Debug.Assert(position.x >= 0f);
			Debug.Assert(position.y >= 0f);
			Debug.Assert(position.x < Screen.currentResolution.width);
			Debug.Assert(position.y < Screen.currentResolution.height);
			if(subject != null)
			{
				Debug.Assert(inspector != null);
				Debug.Assert(inspector == subject.Inspector);
				Debug.Assert(inspectorPart == InspectorPart.Viewport);
			}
			Debug.Assert(inspectorPart != InspectorPart.None || inspector == null);
			Debug.Assert(inspectorPart == InspectorPart.None || inspector != null);
			#endif
			
			openingMenu = menu;
			openingMenuInspector = inspector;
			openingMenuInspectorPart = inspectorPart;
			openingMenuSubject = new DrawerTarget(subject);
			openingMenuPart = part;
			disposeMenuAfterOpening = disposeAfter;
			OnMenuClosed = doOnMenuClosed;

			var openAtLocalPoint = position.position;
			var openAtScreenPoint = GUIUtility.GUIToScreenPoint(openAtLocalPoint);
			position.position = openAtScreenPoint;
			openingMenuPosition = position;

			if(IsSafeToChangeInspectorContents || inspector == null)
			{
				#if DEV_MODE &&  DEBUG_OPEN_MENU
				Debug.Log("Opening context menu @ "+position+" immediately.");
				#endif
				OpenContextMenu();
			}
			else
			{
				#if DEV_MODE &&  DEBUG_OPEN_MENU
				Debug.Log("Opening context menu @ "+position+" delayed... inspector.State.WindowRect.y="+inspector.State.WindowRect.y+", LocalDrawAreaOffset="+DrawGUI.GetLocalDrawAreaOffset());
				#endif

				openDelayCounter = 2;
				inspector.RefreshView();
			}
			
		}
		
		public static void OnInspectorGUIEnd(IInspector inspector)
		{
			if(openingMenu != null && openingMenuInspector == inspector)
			{
				HandleOpeningContextMenu();
			}

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(DrawGUI.IndentLevel == 0, "IndentLevel was "+ DrawGUI.IndentLevel + " when EndInspector was called");
			#endif
		}
		
		private static void HandleOpeningContextMenu()
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(openingMenu != null);
			Debug.Assert(openingMenuInspector != null);
			#endif

			if(Event.current.type == EventType.Layout)
			{
				if(openDelayCounter > 0)
				{
					openDelayCounter--;
					return;
				}

				OpenContextMenu();
			}
			else
			{
				openingMenuInspector.InspectorDrawer.RefreshView();
			}
		}

		public static void CancelOpenContextMenu()
		{
			if(openingMenu == null)
			{
				return;
			}

			if(disposeMenuAfterOpening)
			{
				openingMenu.Dispose();
			}

			openingMenu = null;
		}

		private static void OpenContextMenu()
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(openingMenu != null);
			#endif

			if(OnMenuOpening != null)
			{
				OnMenuOpening(openingMenu, openingMenuInspector, openingMenuSubject.Target);

				// Handle case where OnMenuOpening listener called CancelOpenContextMenu.
				if(openingMenu == null)
				{
					return;
				}
			}
			
			// new test
			if(!ApplicationUtility.HasFocus)
			{
				#if DEV_MODE
				Debug.LogWarning("Aborting display of context menu because Unity application does not have focus any longer.");
				#endif
				
				if(disposeMenuAfterOpening)
				{
					openingMenu.Dispose();
				}

				openingMenu = null;

				if(OnMenuClosed != null)
				{
					var invoke = OnMenuClosed;
					OnMenuClosed = null;
					invoke(openingMenuPart);
				}
				return;
			}

			var genericMenu = new GenericMenu();

			AddBuiltInContextMenuItems(genericMenu);

			openingMenu.AddToGenericMenu(ref genericMenu);

			if(disposeMenuAfterOpening)
			{
				openingMenu.Dispose();
			}

			openingMenu = null;

			if(openingMenuPosition.HasValue)
			{
				var openMenuAtPosition = openingMenuPosition.Value;
				openingMenuPosition = null;

				var openAtScreenPoint = openMenuAtPosition.position;
				var openAtLocalPoint = GUIUtility.ScreenToGUIPoint(openAtScreenPoint);
				openMenuAtPosition.position = openAtLocalPoint;
				
				#if DEV_MODE &&  DEBUG_OPEN_MENU
				Debug.Log("Opening context menu @ "+openMenuAtPosition+" now.");
				#endif

				genericMenu.DropDown(openMenuAtPosition);
			}
			else
			{
				#if DEV_MODE && DEBUG_OPEN_MENU
				Debug.Log("Opening context menu at cursor position now.");
				#endif

				genericMenu.ShowAsContext();
			}

			if(OnMenuClosed != null)
			{
				var invoke = OnMenuClosed;
				OnMenuClosed = null;
				invoke(openingMenuPart);
			}
		}

		private static void AddBuiltInContextMenuItems(GenericMenu genericMenu)
        {
			if(!openingMenuSubject.HasValidInstanceReference())
			{
				return;
			}
            var linkedMember = openingMenuSubject.Target.MemberInfo;
			if(linkedMember == null)
			{
				return;
			}
            var serializedProperty = linkedMember.SerializedProperty;
			if(serializedProperty == null)
			{
				return;
			}
            var editorGUI = typeof(EditorGUI);
            var addUnityInternalPropertyMenuItems = editorGUI.GetMethod("FillPropertyContextMenu", BindingFlags.Static | BindingFlags.NonPublic);
			if(addUnityInternalPropertyMenuItems == null)
			{
				#if DEV_MODE
				Debug.LogWarning("Could not find method EditorGUI.FillPropertyContextMenu.");
				#endif
				return;
			}

            var parameters = ArrayPool<object>.Create(3);
            parameters[0] = serializedProperty;
            parameters[1] = null;
            parameters[2] = genericMenu;
            addUnityInternalPropertyMenuItems.Invoke(null, parameters);
        }

		/// <summary>
		/// Removes all items from <paramref name="genericMenu"/> whose name is found on the <paramref name="itemNames"/> list.
		/// </summary>
		/// <param name="genericMenu"> GenericMenu from which items should be removed. </param>
		/// <param name="itemNames"> List of names. </param>
		public static void RemoveItems(GenericMenu genericMenu, IEnumerable<string> itemNames)
        {
			int count = genericMenu.GetItemCount();
			if(count == 0)
			{
				return;
			}

            var menuItemsField = typeof(GenericMenu).GetField("menuItems", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if(menuItemsField == null)
			{
				#if DEV_MODE
				Debug.LogWarning("Could not find field GenericMenu.menuItems.");
				#endif
				return;
			}

            var menuItems = menuItemsField.GetValue(genericMenu) as IList;
			if(menuItems == null)
			{
				#if DEV_MODE
				Debug.LogWarning("GenericMenu.menuItems was null after being cast to IList.");
				#endif
				return;
			}

			FieldInfo contentField = null;

			for(int i = count - 1; i >= 0; i--)
            {
				var menuItem = menuItems[i];

				if(contentField == null)
				{
					contentField = menuItem.GetType().GetField("content", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					if(contentField == null)
					{
						#if DEV_MODE
						Debug.LogWarning("Could not find field GenericMenu.menuItems.");
						#endif
						return;
					}
				}

				var content = contentField.GetValue(menuItem) as GUIContent;
				if(content == null)
                {
					#if DEV_MODE
					Debug.LogWarning("MenuItem.content field value was null after being cast to GUIContent.");
					#endif
					continue;
                }

				foreach(var itemName in itemNames)
				{
					if(string.Equals(content.text, itemName))
					{
						menuItems.RemoveAt(i);
						break;
					}
				}
			}
		}

		/// <summary>
		/// Attempts to select the last subject of an opened context menu.
		/// </summary>
		public static void SelectLastContextMenuSubject()
		{
			SelectLastContextMenuSubject(openingMenuPart);
		}

		/// <summary>
		/// Attempts to select the last subject of an opened context menu with the given part being given focus.
		/// </summary>
		public static void SelectLastContextMenuSubject(object part)
		{
			if(openingMenuInspector != null)
			{
				if(openingMenuSubject.HasValidInstanceReference())
				{
					var selectDrawer = openingMenuSubject.Target;

					if(selectDrawer.Inactive)
					{
						Debug.LogWarning("ContextMenuUtility.SelectLastContextMenuSubject - openingMenuSubject Inactive was " + StringUtils.True + ". Won't select.");
						return;
					}

					if(!selectDrawer.Selectable)
					{
						Debug.LogWarning("ContextMenuUtility.SelectLastContextMenuSubject - openingMenuSubject Selectable was " + StringUtils.False + ". Won't select.");

						// select parent instead?
						for(selectDrawer = selectDrawer.Parent; selectDrawer != null && (!selectDrawer.Selectable || !selectDrawer.ShouldShowInInspector); selectDrawer = selectDrawer.Parent);
						if(selectDrawer == null)
						{
							return;
						}
					}

					if(!selectDrawer.ShouldShowInInspector)
					{
						Debug.LogWarning("ContextMenuUtility.SelectLastContextMenuSubject - openingMenuSubject ShowInInspector was "+StringUtils.False+". Won't select.");

						// select parent instead?
						for(selectDrawer = selectDrawer.Parent; selectDrawer != null && (!selectDrawer.Selectable || !selectDrawer.ShouldShowInInspector); selectDrawer = selectDrawer.Parent);
						if(selectDrawer == null)
						{
							return;
						}
					}
				}

				openingMenuInspector.Manager.Select(openingMenuInspector, openingMenuInspectorPart, openingMenuSubject.Target, ReasonSelectionChanged.GainedFocus);

				if(openingMenuPart != null)
				{
					if(openingMenuInspectorPart == InspectorPart.Toolbar)
					{
						var toolbarPart = openingMenuPart as IInspectorToolbarItem;
						
						#if DEV_MODE && PI_ASSERTATIONS
						Debug.Assert(openingMenuInspectorPart == InspectorPart.Toolbar);
						Debug.Assert(openingMenuInspector.Manager.SelectedInspectorPart == InspectorPart.Toolbar);
						#endif

						openingMenuInspector.Toolbar.SetSelectedItem(toolbarPart, ReasonSelectionChanged.GainedFocus);
					}
				}
			}
		}

		private static void OnPropertyContextMenuOpen(GenericMenu menu, SerializedProperty property)
		{
			#if DEV_MODE
			Debug.Log("OnPropertyContextMenuOpen with MenuIsOpening="+MenuIsOpening);
			#endif

			// If this ContextMenu was opened internally by Power Inspector, ignore it.
			// We only want to slip in these menu items when context menus are opeend from inside Custom Editors and PropertyDrawers.
			if(MenuIsOpening)
			{
				return;
			}

			var inspectorManager = InspectorUtility.ActiveManager;
			if(inspectorManager == null)
			{
				return;
			}

			// Only add menu items if a Power Inspector window has focus, to keep the default inspector isolated from any changes,
			// unless specified differently in Power Inspector preferences.
			var lastSelectedInspector = inspectorManager.LastSelectedActiveOrDefaultInspector();
			FieldContextMenuItems enabledItems;
			if(lastSelectedInspector == null || !lastSelectedInspector.InspectorDrawer.HasFocus)
			{
				// When context menu is being opened from outside a Power Inspector window, then only add context menu enhancements enabled in preferences.
				enabledItems = InspectorUtility.Preferences.defaultInspector.enhanceFieldContextMenu;
				if(enabledItems == FieldContextMenuItems.None)
				{
					#if DEV_MODE
					Debug.LogWarning("Won't add copy paste items to opening context menu for property \"" + property.name + "\" of type " + property.type + " because window doesn't have focus.");
					#endif
					return;
				}
			}
			else
			{
				// When context menu is being opened from a Custom Editor or Property Drawer inside a Power Inspector window, then add all context menu enhancements
				enabledItems = (FieldContextMenuItems)FieldContextMenuItems.None.SetAllFlags();
			}

			#if DEV_MODE
			Debug.Log("Opening menu for property \""+property.name+"\" of type "+property.type+" and propertyType="+property.propertyType+", isArray="+property.isArray+", currentField="+ StringUtils.ToString(DrawerUtility.currentField));
			#endif

			if(property.propertyType == SerializedPropertyType.ObjectReference)
			{
				if(enabledItems.HasFlag(FieldContextMenuItems.Peek))
				{
					if(property.objectReferenceValue != null)
					{
						var manager = InspectorUtility.ActiveManager;
						if(manager != null)
						{
							var inspector = manager.LastSelectedActiveOrDefaultInspector(property.objectReferenceValue.IsSceneObject() ? InspectorTargetingMode.Hierarchy : InspectorTargetingMode.Project, InspectorSplittability.IsSplittable);
							if(inspector != null)
							{
								var splittableDrawer = (ISplittableInspectorDrawer)inspector.InspectorDrawer;
								menu.AddItem(new GUIContent("Peek", "Open target in split view in Power Inspector."), false, ()=>splittableDrawer.ShowInSplitView(property.objectReferenceValue, true));
							}
						}
					}
				}
				else
				{
					#if DEV_MODE
					Debug.Log("Won't add Peek to object reference field because enabledItems was "+StringUtils.ToString(enabledItems));
					#endif
				}

				AddMenuItems(enabledItems, ref menu, property.name, Types.Bool, property.editable,
					()=>Clipboard.ObjectReference = property.objectReferenceValue,
					()=>
					{
						property.objectReferenceValue = Clipboard.ObjectReference;
						property.serializedObject.ApplyModifiedProperties();
					},
					()=>
					{
						property.objectReferenceValue = null;
						property.serializedObject.ApplyModifiedProperties();
					});
			}
			else if(property.isArray)
			{
				#if DEV_MODE
				Debug.Log("property.arrayElementType="+property.arrayElementType);
				#endif

				AddMenuItems(enabledItems, ref menu, property.name, Types.Bool, property.editable,
						()=>Clipboard.Copy(property.boolValue, Types.Bool),
						()=>
						{
							if(Clipboard.CopiedType.IsArray)
							{
								object array = null;
								if(Clipboard.TryPaste(Clipboard.CopiedType, ref array))
								{
									var pastedArray = array as Array;
									if(pastedArray == null)
									{
										property.arraySize = pastedArray.Length;
										/*
										for(int n = pastedArray.Length - 1; n >= 0; n--)
										{
											var element = property.GetArrayElementAtIndex(n);
											//TO DO: do a switch and handle all different types similar to non-array copy-paste
											//element.intValue = 
										}
										*/
										property.serializedObject.ApplyModifiedProperties();
									}
								}
							}
						},
						()=>
						{
							property.arraySize = 0;
							property.serializedObject.ApplyModifiedProperties();
						});
			}
			else
			{
				switch(property.type)
				{
					case "bool":
						AddMenuItems(enabledItems, ref menu, property.name, Types.Bool, property.editable,
							()=>Clipboard.Copy(property.boolValue, Types.Bool),
							()=>
							{
								property.boolValue = Clipboard.Paste<bool>();
								property.serializedObject.ApplyModifiedProperties();
							},
							() =>
							{
								property.boolValue = false;
								property.serializedObject.ApplyModifiedProperties();
							});
						break;
					case "int":
						AddMenuItems(enabledItems, ref menu, property.name, Types.Int, property.editable,
							()=>Clipboard.Copy(property.intValue, Types.Int),
							()=>
							{
								property.intValue = Clipboard.Paste<int>();
								property.serializedObject.ApplyModifiedProperties();
							},
							()=>property.intValue = 0);
						break;
					case "string":
						AddMenuItems(enabledItems, ref menu, property.name, Types.String, property.editable,
							()=>Clipboard.Copy(property.stringValue, Types.String),
							()=>
							{
								property.stringValue = Clipboard.Paste<string>();
								property.serializedObject.ApplyModifiedProperties();
							},
							()=>property.stringValue = "");
						break;
					case "Vector3":
							AddMenuItems(enabledItems, ref menu, property.name, Types.Vector3, property.editable,
							()=>Clipboard.Copy(property.vector3Value, Types.Vector3),
							()=>
							{
								property.vector3Value = Clipboard.Paste<Vector3>();
								property.serializedObject.ApplyModifiedProperties();
							},
							()=>
							{
								if(property.serializedObject.targetObject is Transform)
								{
									if(string.Equals("m_LocalScale", property.name))
									{
										#if DEV_MODE
										Debug.Log("Detected scale target. Resetting to (1,1,1).");
										#endif
										property.vector3Value = Vector3.one;
									}
									else
									{
										#if DEV_MODE
										Debug.Log("Resetting transform member \""+property.propertyPath+"\".");
										#endif
										property.vector3Value = Vector3.zero;
									}
								}
								else
								{
									property.vector3Value = Vector3.zero;
								}
								property.serializedObject.ApplyModifiedProperties();
							});
						break;
					case "Quaternion":
						AddMenuItems(enabledItems, ref menu, property.name, Types.Quaternion, property.editable,
							()=>Clipboard.Copy(property.quaternionValue, Types.Quaternion),
							()=>
							{
								property.quaternionValue = Clipboard.Paste<Quaternion>();
								property.serializedObject.ApplyModifiedProperties();
							},
							()=>
							{
								#if DEV_MODE
								Debug.Log("Resetting Quaternion \""+property.name+"\" of type "+property.type+" and propertyType="+property.propertyType+", isArray="+property.isArray+", currentField="+ StringUtils.ToString(DrawerUtility.currentField));
								#endif

								// This seems to sometimes set Transform rotation values to (180,180,180) or (360,360,360) for some reason
								property.quaternionValue = Quaternion.identity;
								property.serializedObject.ApplyModifiedProperties();
							});
						break;
					#if UNITY_2017_2_OR_NEWER
					case "Vector3Int":
							AddMenuItems(enabledItems, ref menu, property.name, Types.Vector3Int, property.editable,
							()=>Clipboard.Copy(property.vector3IntValue, Types.Vector3Int),
							()=>
							{
								property.vector3IntValue = Clipboard.Paste<Vector3Int>();
								property.serializedObject.ApplyModifiedProperties();
							},
							() =>
							{
								property.vector3IntValue = Vector3Int.zero;
								property.serializedObject.ApplyModifiedProperties();
							});
						break;
					case "Vector2Int":
						AddMenuItems(enabledItems, ref menu, property.name, Types.Vector2Int, property.editable,
							()=>Clipboard.Copy(property.vector2IntValue, Types.Vector2Int),
							()=>
							{
								property.vector2IntValue = Clipboard.Paste<Vector2Int>();
								property.serializedObject.ApplyModifiedProperties();
							},
							() =>
							{
								property.vector2IntValue = Vector2Int.zero;
								property.serializedObject.ApplyModifiedProperties();
							});
						break;
					#endif
					case "Vector2":
						AddMenuItems(enabledItems, ref menu, property.name, Types.Vector2, property.editable,
							()=>Clipboard.Copy(property.vector2Value, Types.Vector2),
							()=>
							{
								property.vector2Value = Clipboard.Paste<Vector2>();
								property.serializedObject.ApplyModifiedProperties();
							},
							() =>
							{
								property.vector2Value = Vector2.zero;
								property.serializedObject.ApplyModifiedProperties();
							});
						break;
					case "Rect":
						AddMenuItems(enabledItems, ref menu, property.name, Types.Rect, property.editable,
							()=>Clipboard.Copy(property.rectValue, Types.Rect),
							()=>
							{
								property.rectValue = Clipboard.Paste<Rect>();
								property.serializedObject.ApplyModifiedProperties();
							},
							() =>
							{
								property.rectValue = default(Rect);
								property.serializedObject.ApplyModifiedProperties();
							});
						break;
					case "float":
						AddMenuItems(enabledItems, ref menu, property.name, Types.Float, property.editable,
							()=>Clipboard.Copy(property.floatValue, Types.Float),
							()=>
							{
								object setValue = property.floatValue;
								if(Clipboard.TryPaste<float>(ref setValue))
								{
									#if DEV_MODE
									Debug.Log("Resetting float field \""+property.propertyPath+"\".");
									#endif
									property.floatValue = (float)setValue;
								}
								else if(Clipboard.TryPaste<Vector3>(ref setValue))
								{
									if(lastSelectedInspector != null)
									{
										var focusController = new EditorKeyboardFocusController();
										float fullEditorWidth = lastSelectedInspector.State.WindowRect.width;
										float prefixLabelWidth = DrawGUI.PrefixLabelWidth;
										int member3Index = focusController.GetMouseoveredControlIndexInRowWith3Controls(fullEditorWidth, prefixLabelWidth);
										#if DEV_MODE
										Debug.Log("Vector3 was pasted to float field. Detected member3Index: "+member3Index);
										#endif
										if(member3Index < 0)
										{
											member3Index = 0;
										}
										property.floatValue = ((Vector3)setValue)[member3Index];
									}
									else
									{
										property.floatValue = ((Vector3)setValue).x;
									}
								}
								property.serializedObject.ApplyModifiedProperties();
							},
							()=>
							{
								if(property.serializedObject.targetObject is Transform)
								{
									if(property.propertyPath.IndexOf("m_LocalScale", StringComparison.Ordinal) != -1)
									{
										#if DEV_MODE
										Debug.Log("Detected Transform scale float \""+property.propertyPath+"\". Resetting to 1f.");
										#endif
										property.floatValue = 1f;
									}
									else
									{
										#if DEV_MODE
										Debug.Log("Resetting Transform float \""+property.propertyPath+"\" to 0f.");
										#endif

										property.floatValue = 0f;
									}
								}
								else
								{
									property.floatValue = 0f;
								}
							});
						break;
					case "Enum":
						AddMenuItems(enabledItems, ref menu, property.name, Types.Int, property.editable,
							//enumValueIndex seems to throw errors when used with EnumMaskField,
							//but intValue seems to work
							//()=> Clipboard.Copy(property.enumValueIndex), 
							()=>Clipboard.Copy(property.intValue, Types.Int),
							()=>
							{
								property.enumValueIndex = Clipboard.Paste<int>();
								property.serializedObject.ApplyModifiedProperties();
							},
							() =>
							{
								property.enumValueIndex = 0;
								property.serializedObject.ApplyModifiedProperties();
							});
						break;
					default:
						MemberInfo memberInfo;
						object owner;
						property.GetMemberInfoAndOwner(out memberInfo, out owner);
						if(memberInfo != null)
						{
							var fieldInfo = memberInfo as FieldInfo;
							if(fieldInfo != null)
							{
								var type = fieldInfo.FieldType;
								AddMenuItems(enabledItems, ref menu, property.name, type, property.editable,
									()=>Clipboard.Copy(fieldInfo.GetValue(owner), type),
									()=>
									{
										fieldInfo.SetValue(owner, Clipboard.Paste(type));
										property.serializedObject.Update();
									},
									()=> fieldInfo.SetValue(owner, type.DefaultValue()));
							}
							else
							{
								var propertyInfo = memberInfo as PropertyInfo;
								if(propertyInfo != null)
								{
									var type = propertyInfo.PropertyType;
									AddMenuItems(enabledItems, ref menu, property.name, type, property.editable,
										()=>Clipboard.Copy(propertyInfo.GetValue(owner, null), type),
										()=>
										{
											if(type.IsUnityObject())
											{
												property.objectReferenceValue = Clipboard.PasteObjectReference(type);
												property.serializedObject.ApplyModifiedProperties();
											}
											else
											{
												propertyInfo.SetValue(owner, Clipboard.Paste(type), null);
												property.serializedObject.Update();
											}
										},
										()=> propertyInfo.SetValue(owner, type.DefaultValue(), null));
								}
							}
						}
						break;
				}
			}
		}

		private static void AddMenuItems(FieldContextMenuItems enabledItems, ref GenericMenu menu, string targetName, [NotNull]Type type, bool editable, [NotNull]GenericMenu.MenuFunction copy, [NotNull]GenericMenu.MenuFunction paste, [CanBeNull]GenericMenu.MenuFunction reset)
		{
			bool addSeparator = false;
			if(enabledItems.HasFlag(FieldContextMenuItems.Reset) && reset != null)
			{
				if(editable)
				{
					#if DEV_MODE && DEBUG_ADD_MENU_ITEM
					Debug.Log("Adding Reset to context menu for property \""+targetName+"\" of type "+type.Name);
					#endif

					menu.AddItem(GUIContentPool.Create("Reset"), false, ()=>
					{
						reset();
						DrawerUtility.SendResetMessage(StringUtils.SplitPascalCaseToWords(targetName));
					});
				}
				else
				{
					menu.AddDisabledItem(GUIContentPool.Create("Reset"));
				}

				addSeparator = true;
			}

			if(enabledItems.HasFlag(FieldContextMenuItems.CopyPaste))
			{
				if(addSeparator)
				{
					menu.AddSeparator("");
				}
				addSeparator = true;

				#if DEV_MODE && DEBUG_ADD_MENU_ITEM
				Debug.Log("Adding Copy and Paste to context menu for property \""+targetName+"\" of type "+type.Name);
				#endif

				menu.AddItem(GUIContentPool.Create("Copy"), false, ()=>
				{
					copy();
					Clipboard.SendCopyToClipboardMessage(targetName);
				});

				if(editable)
				{
					if(type.IsUnityObject())
					{
						if(Clipboard.HasObjectReference())
						{
							menu.AddItem(GUIContentPool.Create("Paste"), false, () =>
							{
								paste();
								Clipboard.SendPasteFromClipboardMessage(targetName);
							});
						}
						else
						{
							menu.AddDisabledItem(GUIContentPool.Create("Paste"));
						}
					}
					else if(Clipboard.Content.Length > 0 && Clipboard.CanPasteAs(type))
					{
						menu.AddItem(GUIContentPool.Create("Paste"), false, ()=>
						{
							paste();
							Clipboard.SendPasteFromClipboardMessage(targetName);
						});
					}
					else
					{
						menu.AddDisabledItem(GUIContentPool.Create("Paste"));
					}
				}
			}

			if(enabledItems.HasFlag(FieldContextMenuItems.InspectStaticMembers))
			{
				if(addSeparator)
				{
					menu.AddSeparator("");
				}

				menu.AddItem(GUIContentPool.Create("Inspect "+StringUtils.ToStringSansNamespace(type)+" Static Members"), false, ()=>
				{
					var inspector = InspectorUtility.ActiveManager.LastSelectedActiveOrDefaultInspector();
					if(inspector == null)
					{
						DrawGUI.ExecuteMenuItem(PowerInspectorMenuItemPaths.NewWindow);
						inspector = InspectorUtility.ActiveManager.LastSelectedActiveOrDefaultInspector();
					}
					inspector.RebuildDrawers(null, type);
				});
			}
		}
	}
}