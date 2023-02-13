#define REPAINT_ON_UPDATE

#define DEBUG_SETUP
#define DEBUG_DESTROY

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEditor;

namespace Sisus
{
	/// <summary>
	/// Window that handles rendering popup menus opened via PopupMenuManager.Open in editor mode.
	/// </summary>
	public sealed class PopupMenuWindow : EditorWindow
	{
		private static PopupMenuWindow instance;

		//important that this is static, since it needs to be referenced after OnDestroy
		private static IInspector inspector;

		private PopupMenu drawer;
		private Action onClosed;
		private int isDirty;

		public static void SelectItem(string label)
		{
			if(instance == null)
			{
				#if DEV_MODE
				Debug.LogWarning("PopupMenu.SelectItem called but instance was null. Create should be called before calling SelectItem.");
				#endif
				return;
			}

			instance.drawer.SetSelectedMember(label);
		}

		/// <summary>
		/// Creates and opens an Add Component Menu Window that rolls open below or above unrollPosition.
		/// </summary>
		/// <param name="setInspector">   The inspector which is opening the menu. </param>
		/// <param name="items"> Root items of the menu. </param>
		/// <param name="groupsByLabel"> All groups in the menu flattened, with full label path of group as key in dictionary. </param>
		/// <param name="itemsByLabel"> All non-group leaf items in the menu flattened, with full label path of item as key in dictionary. </param>
		/// <param name="tickedItems"> items that are ticked in the menu. </param>
		/// <param name="canTickMultipleItems"> Is it possible to have mutliple items ticked in the menu simultaneously? </param>
		/// <param name="unrollPosition"> The position above or below which the menu should open. </param>
		/// <param name="onMenuItemClicked"> Action to invoke when a menu item is clicked. </param>
		/// <param name="onClosed"> Action to invoke when the menu is closed. </param>
		/// <param name="menuTitle"> Title for menu. </param>
		public static void Create([NotNull]IInspector setInspector, [NotNull]List<PopupMenuItem> items, [NotNull]Dictionary<string, PopupMenuItem> groupsByLabel, [NotNull]Dictionary<string, PopupMenuItem> itemsByLabel, [CanBeNull]List<PopupMenuItem> tickedItems, bool canTickMultipleItems, Rect unrollPosition, Action<PopupMenuItem> onMenuItemClicked, Action onClosed, GUIContent menuTitle)
		{
			if(instance != null)
			{
				instance.Setup(setInspector, items, groupsByLabel, itemsByLabel, tickedItems, unrollPosition, onMenuItemClicked, onClosed, menuTitle, canTickMultipleItems);
				return;
			}
			instance = CreateInstance<PopupMenuWindow>();
			instance.Setup(setInspector, items, groupsByLabel, itemsByLabel, tickedItems, unrollPosition, onMenuItemClicked, onClosed, menuTitle, canTickMultipleItems);
		}

		/// <summary>
		/// Setups the Add Component Menu Window instance for the given specs.
		/// </summary>
		/// <param name="setInspector">   The inspector which is opening the menu. </param>
		/// <param name="items"> Root items of the menu. </param>
		/// <param name="groupsByLabel"> All groups in the menu flattened, with full label path of group as key in dictionary. </param>
		/// <param name="itemsByLabel"> All non-group leaf items in the menu flattened, with full label path of item as key in dictionary. </param>
		/// <param name="tickedItems"> Items that are ticked in the menu. </param>
		/// <param name="unrollPosition"> The position where the menu should open. </param>
		/// <param name="onMenuItemClicked"> Action to invoke when a menu item is clicked. </param>
		/// <param name="onMenuClosed"> Action to invoke when the menu is closed. </param>
		/// <param name="menuTitle"> Title for menu. </param>
		/// <param name="canTickMultipleItems"> Is it possible for multiple items to be ticked in the menu simultaneously? </param>
		private void Setup([NotNull]IInspector setInspector, [NotNull]List<PopupMenuItem> items, [NotNull]Dictionary<string, PopupMenuItem> groupsByLabel, [NotNull]Dictionary<string, PopupMenuItem> itemsByLabel, [CanBeNull]List<PopupMenuItem> tickedItems, Rect unrollPosition, Action<PopupMenuItem> onMenuItemClicked, Action onMenuClosed, GUIContent menuTitle, bool canTickMultipleItems)
		{
			#if DEV_MODE && DEBUG_SETUP
			Debug.Log(GetType().Name + ".Setup() with instance=" + (instance == null ? "null" : "NotNull") + ", setInspector=" + setInspector);
			#endif
			
			inspector = setInspector;
			onClosed = onMenuClosed;
			
			inspector.InspectorDrawer.Manager.IgnoreAllMouseInputs = true;
			
			if(drawer == null)
			{
				drawer = PopupMenu.Create(inspector, items, groupsByLabel, itemsByLabel, unrollPosition, onMenuItemClicked, Close, menuTitle);
			}
			else
			{
				drawer.Setup(inspector, items, groupsByLabel, itemsByLabel, unrollPosition, onMenuItemClicked, Close, menuTitle);
			}

			drawer.SetTickedMembers(tickedItems, canTickMultipleItems);

			ShowAsDropDown(unrollPosition, new Vector2(unrollPosition.width, drawer.TotalHeight));

			#if DEV_MODE && PI_ASSERTATIONS
			if(drawer.OnCurrentViewItemCountChanged != null)
			{
				Debug.LogError("PopupMenuWindow.OnCurrentViewItemCountChanged ("+drawer.OnCurrentViewItemCountChanged.GetInvocationList().Length+") != null: "+StringUtils.ToString(drawer.OnCurrentViewItemCountChanged));
			}
			#endif
			
			//UPDATE: fix for bug where invocation list Count would keep growing
			//drawer.OnCurrentViewItemCountChanged += UpdateHeight;
			drawer.OnCurrentViewItemCountChanged = UpdateHeight;
		}

		private void UpdateHeight(int currentViewItemCount)
		{
			var pos = position;
			pos.height = drawer.TotalHeight;
			var size = pos.size;
			minSize = size;
			maxSize = size;
			position = pos;
		}

		
		[UsedImplicitly]
		private void OnGUI()
		{
			GUI.depth = -100;

			//handle assembly reload causing drawer to go null, resulting in null reference exceptions
			if(drawer == null)
			{
				Close();
				return;
			}

			DrawGUI.BeginOnGUI(inspector.Preferences, true);

			bool addedComponent = false;
			EditorGUI.BeginChangeCheck();
			{
				if(drawer.OnGUI(ref addedComponent))
				{
					GUI.changed = true;
					Repaint();
					isDirty = 3;
				}
			}
			if(EditorGUI.EndChangeCheck())
			{
				isDirty = 3;
			}

			if(addedComponent)
			{
				Close();
				return;
			}
			
			if(isDirty > 0)
			{
				GUI.changed = true;
				Repaint();
			}

			var rect = position;
			rect.x = 0f;
			rect.y = 0f;
			DrawGUI.DrawRect(rect, Color.grey);
		}

		#if REPAINT_ON_UPDATE
		[UsedImplicitly]
		private void Update()
		{
			Repaint();
		}
		#else
		[UsedImplicitly]
		private void Update()
		{
			if(isDirty > 0)
			{
				isDirty--;
				GUI.changed = true;
				Repaint();
			}
		}
		#endif

		[UsedImplicitly]
		private void OnLostFocus()
		{
			#if DEV_MODE && DEBUG_FOCUS
			Debug.Log(GetType().Name + ".OnLostFocus() with instance="+(instance == null ? "null" : "NotNull")+", event="+StringUtils.ToString(Event.current));
			#endif
		}

		[UsedImplicitly]
		private void OnDestroy()
		{
			#if DEV_MODE && DEBUG_DESTROY
			Debug.Log(GetType().Name + ".OnDestroy() with instance=" + (instance == null ? "null" : "NotNull") + ", event=" + StringUtils.ToString(Event.current));
			#endif

			RestoreClickControlsAfterTwoFrames();
			Dispose();
		}

		private static void RestoreClickControlsAfterTwoFrames()
		{
			if(inspector != null)
			{
				inspector.OnNextLayout(RestoreClickControlsAfterAFrame);
			}
		}

		private static void RestoreClickControlsAfterAFrame()
		{
			if(inspector != null)
			{
				inspector.OnNextLayout(RestoreClickControls);
			}
		}

		private static void RestoreClickControls()
		{
			if(inspector != null)
			{
				inspector.InspectorDrawer.Manager.IgnoreAllMouseInputs = false;
			}
		}

		private void Dispose()
		{
			if(instance != null)
			{
				instance = null;

				if(drawer != null)
				{
					#if DEV_MODE && PI_ASSERTATIONS
					Debug.Assert(drawer.OnCurrentViewItemCountChanged.GetInvocationList().Length == 1, "PopupMenuWindow.OnCurrentViewItemCountChanged.Length != 1: "+StringUtils.ToString(drawer.OnCurrentViewItemCountChanged));
					#endif
					drawer.OnCurrentViewItemCountChanged = null;
					drawer.OnClosed();
				}

				if(onClosed != null)
				{
					var callback = onClosed;
					onClosed = null;
					callback();
				}
			}
		}
	}
}