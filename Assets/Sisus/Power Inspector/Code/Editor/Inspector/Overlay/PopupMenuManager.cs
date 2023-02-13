#define SAFE_MODE

#define DEBUG_OPEN

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace Sisus
{
	/// <summary>
	/// Manager class for popup menus.
	/// Requests to open popup menus are done through this class.
	/// </summary>
	public static class PopupMenuManager
	{
		public delegate void OpeningMenuData([NotNull]List<PopupMenuItem> items, [CanBeNull]IDrawer menuSubject);
		public delegate void OpenRequest([NotNull]IInspector inspector, [NotNull]List<PopupMenuItem> items, [NotNull]Dictionary<string, PopupMenuItem> groupsByLabel, [NotNull]Dictionary<string, PopupMenuItem> itemsByLabel, [CanBeNull]List<PopupMenuItem> tickedItems, bool canTickMultipleItems, Rect unrollPosition, Action<PopupMenuItem> onMenuItemClicked, Action onClosed, GUIContent label);
		public delegate void SelectItemRequest(string itemFullLabel);

		public static OpeningMenuData OnPopupMenuOpening;

		private static IInspector lastmenuOpenedForInspector;
		private static IDrawer lastmenuOpenedForDrawer;

		public static bool IsOpen
		{
			get
			{
				return PopupMenu.isOpen;
			}
		}

		public static IInspector LastmenuOpenedForInspector
		{
			get
			{
				return lastmenuOpenedForInspector;
			}
		}

		public static IDrawer LastmenuOpenedForDrawer
		{
			get
			{
				return lastmenuOpenedForDrawer;
			}
		}

		public static void RegisterPopupMenuDrawer(IPopupMenuAttacher attacher)
		{
			openMenu += attacher.OnRequestingOpen;
			selectItem += attacher.OnRequestingSelectItem;
		}

		/// <summary>
		/// Delegate that allows PopupMenu displayers (like PopupMenuWindow) to listen for an menu opening request.
		/// </summary>
		private static OpenRequest openMenu;

		/// <summary>
		/// Delegate that allows PopupMenu displayers (like PopupMenuWindow) to listen for an menu item select request
		/// </summary>
		private static SelectItemRequest selectItem;

		public static void Open([NotNull]IInspector inspector, [NotNull]List<PopupMenuItem> rootItems, Rect unrollPosition, [NotNull]Action<PopupMenuItem> onMenuItemClicked, Action onClosed = null)
		{
			int count = rootItems.Count;
			var groupsByLabel = new Dictionary<string, PopupMenuItem>(count);
			var itemsByLabel = new Dictionary<string, PopupMenuItem>(count);
			PopupMenuUtility.GenerateByLabelDictionaries(rootItems, groupsByLabel, itemsByLabel);
			Open(inspector, rootItems, groupsByLabel, itemsByLabel, null, false, unrollPosition, onMenuItemClicked, onClosed, GUIContent.none, null);
		}

		public static void Open([NotNull]IInspector inspector, [NotNull]List<PopupMenuItem> rootItems, [NotNull]Dictionary<string, PopupMenuItem> groupsByLabel, [NotNull]Dictionary<string, PopupMenuItem> itemsByLabel, Rect unrollPosition, [NotNull]Action<PopupMenuItem> onMenuItemClicked, Action onClosed = null)
		{
			Open(inspector, rootItems, groupsByLabel, itemsByLabel, null, false, unrollPosition, onMenuItemClicked, onClosed, GUIContent.none, null);
		}

		public static void Open([NotNull]IInspector inspector, [NotNull]List<PopupMenuItem> rootItems, Rect unrollPosition, [NotNull]Action<PopupMenuItem> onMenuItemClicked, [CanBeNull]Action onClosed, string label, [CanBeNull]IDrawer subject)
		{
			int count = rootItems.Count;
			var groupsByLabel = new Dictionary<string, PopupMenuItem>(count);
			var itemsByLabel = new Dictionary<string, PopupMenuItem>(count);
			PopupMenuUtility.GenerateByLabelDictionaries(rootItems, groupsByLabel, itemsByLabel);
			Open(inspector, rootItems, groupsByLabel, itemsByLabel, null, false, unrollPosition, onMenuItemClicked, onClosed, GUIContentPool.Create(label), subject);
		}

		public static void Open([NotNull]IInspector inspector, [NotNull]List<PopupMenuItem> rootItems, [NotNull]Dictionary<string, PopupMenuItem> groupsByLabel, [NotNull]Dictionary<string, PopupMenuItem> itemsByLabel, Rect unrollPosition, [NotNull]Action<PopupMenuItem> onMenuItemClicked, [CanBeNull]Action onClosed, string label, [CanBeNull]IDrawer subject)
		{
			Open(inspector, rootItems, groupsByLabel, itemsByLabel, null, false, unrollPosition, onMenuItemClicked, onClosed, GUIContentPool.Create(label), subject);
		}
		
		public static void Open([NotNull]IInspector inspector, [NotNull]List<PopupMenuItem> rootItems, Rect unrollPosition, Action<PopupMenuItem> onMenuItemClicked, [CanBeNull]Action onClosed, GUIContent label, [CanBeNull]IDrawer subject)
		{
			int count = rootItems.Count;
			var groupsByLabel = new Dictionary<string, PopupMenuItem>(count);
			var itemsByLabel = new Dictionary<string, PopupMenuItem>(count);
			PopupMenuUtility.GenerateByLabelDictionaries(rootItems, groupsByLabel, itemsByLabel);
			Open(inspector, rootItems, groupsByLabel, itemsByLabel, null, false, unrollPosition, onMenuItemClicked, onClosed, label, subject);
		}

		public static void Open([NotNull]IInspector inspector, [NotNull]List<PopupMenuItem> rootItems, [NotNull]Dictionary<string, PopupMenuItem> groupsByLabel, [NotNull]Dictionary<string, PopupMenuItem> itemsByLabel, Rect unrollPosition, Action<PopupMenuItem> onMenuItemClicked, [CanBeNull]Action onClosed, GUIContent label, [CanBeNull]IDrawer subject)
		{
			Open(inspector, rootItems, groupsByLabel, itemsByLabel, null, false, unrollPosition, onMenuItemClicked, onClosed, label, subject);
		}
		
		public static void Open([NotNull]IInspector inspector, [NotNull]List<PopupMenuItem> rootItems, [NotNull]Dictionary<string, PopupMenuItem> groupsByLabel, [NotNull]Dictionary<string, PopupMenuItem> itemsByLabel, [CanBeNull]List<PopupMenuItem> tickedItems, bool canTickMultipleItems, Rect clickedItemRect, [NotNull]Action<PopupMenuItem> onMenuItemClicked, [CanBeNull]Action onClosed, GUIContent label, [CanBeNull]IDrawer subject)
		{
			#if DEV_MODE && SAFE_MODE
			Debug.Assert(rootItems.TrueForAll(item=>item.parent == null));
			Debug.Assert(groupsByLabel.Select(pair => pair.Value).ToList().TrueForAll(item=>item.IsGroup));
			Debug.Assert(itemsByLabel.Select(pair => pair.Value).ToList().TrueForAll(item=>!item.IsGroup));
			Debug.Assert(itemsByLabel.Count > 0);
			Debug.Assert(groupsByLabel.Count > 0 || rootItems.Count == itemsByLabel.Count);
			#endif

			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.BeginSample("PopupMenuManager.Open");
			#endif
			
			lastmenuOpenedForDrawer = subject;
			lastmenuOpenedForInspector = inspector;

			if(OnPopupMenuOpening != null)
			{
				OnPopupMenuOpening(rootItems, subject);
			}

			var unrollPos = clickedItemRect;

			// menu should open underneath the clicked item
			//unrollPos.y += clickedItemRect.height;

			// add inspector y-axis position to open position so that if this is a split view
			// the menu is opened with the correct offset
			unrollPos.y += inspector.State.WindowRect.y;
			
			#if DEV_MODE && DEBUG_OPEN
			Debug.Log(inspector+" WindowRect.y="+ inspector.State.WindowRect.y+", subject="+(subject==null?"null":subject.ToString())); ;
			#endif

			var inspectorDrawer = inspector.InspectorDrawer;

			// when converting unroll position to screen space,
			// if menu is opening for Drawer inside the inspector viewport,
			// then we need to consider things like current scroll amounts
			// of the view 
			if(subject != null)
			{
				// Not sure where why this offset is needed. Perhaps EditorWindows
				// introduce some padding to the position values?
				unrollPos.y += 8f;

				// if menu was opened outside the current view rect of the viewport
				// (e.g. Add Component menu was opened via a shortcut key) then
				// adjust the opening position so that it doesn't get opened outside
				// the bounds of the window (alt solution would be to always scroll
				// down to the button before opening the menu)
				if(inspector.IsOutsideViewport(unrollPos))
				{
					#if DEV_MODE
					Debug.LogWarning(unrollPos + " Outside Viewport. Maybe you want to scroll to show the target ("+ subject + ") before opening the menu?");
					#endif
					//unrollPosScreenSpace.y = inspectorDrawer.position.y + inspectorDrawer.position.height - PopupMenu.TotalMaxHeightWithNavigationBar;
					unrollPos.y = inspectorDrawer.position.height - PopupMenu.TotalMaxHeightWithNavigationBar;
				}
				else
				{
					#if DEV_MODE
					Debug.Log("<color=green>"+ unrollPos + " Not Outside Viewport. Adding inspectorDrawer.pos.y ("+ inspectorDrawer.position.y+") - scrollPos.y ("+ inspector.State.ScrollPos.y+") + 68f</color>" );
					#endif
					unrollPos.y += inspectorDrawer.position.y - inspector.State.ScrollPos.y + 15f;
				}

				var screenHeight = Screen.currentResolution.height;

				// If there's not enough screen estate to draw the menu below target position then draw it above.
				if(unrollPos.y + PopupMenu.TotalMaxHeightWithNavigationBar > screenHeight)
				{
					#if DEV_MODE
					Debug.Log("<color=red>Not enough space to draw below. screenHeight="+ screenHeight+ ", unrollPos.y=" + unrollPos + ", TotalMaxHeightWithNavigationBar="+ PopupMenu.TotalMaxHeightWithNavigationBar+"</color>" );
					#endif
					unrollPos.y -= PopupMenu.TotalMaxHeightWithNavigationBar + clickedItemRect.height + 2f;
				}

				if(clickedItemRect.width < PopupMenu.MinWidth)
				{
					// if menu width is too small, roll it open towards the left
					// this is so that when controls are clicked, the menu is less likely to expand
					// outside the bounds of the inspector, which would look ugly and could result
					// int the menu clipping outside the screen bounds
					float width = PopupMenu.MinWidth;
					unrollPos.x = inspectorDrawer.position.x + clickedItemRect.xMax - width;
					unrollPos.width = width;
				}
				else
				{
					float width = Mathf.Round(clickedItemRect.width);
					unrollPos.x = inspectorDrawer.position.x + unrollPos.x;
					unrollPos.width = width;
				}
			}
			// if menu is being opened from outside the inspector viewport
			// then converting to screen space is as simple as adding
			// the screenspace positions of the inspector drawer to it
			else
			{
				// This is necessary.
				unrollPos.x += inspectorDrawer.position.x;

				// Not sure where why this offset is needed. Perhaps EditorWindows
				// introduce some padding to the position values?
				unrollPos.y += inspectorDrawer.position.y; // + 5f;

				unrollPos.width = Mathf.Max(unrollPos.width, PopupMenu.MinWidth);
			}

			#if !UNITY_2019_3_OR_NEWER
			// if the inspector window is docked, the positions need to be adjusted somewhat to be accurate
			var inspectorWindow = inspectorDrawer as UnityEditor.EditorWindow;
			if(inspectorWindow != null)
			{
				if(inspectorWindow.IsDocked())
				{
					unrollPos.x += 2f;
					unrollPos.y -= 4f;
				}
			}
			#endif

			openMenu(inspector, rootItems, groupsByLabel, itemsByLabel, tickedItems, canTickMultipleItems, unrollPos, onMenuItemClicked, onClosed, label);

			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.EndSample();
			#endif
		}

		public static void SelectItem(string itemFullLabel)
		{
			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.BeginSample("PopupMenuManager.SelectItem");
			#endif

			selectItem(itemFullLabel);

			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.EndSample();
			#endif
		}
	}
}