//#define DEBUG_FIND_BY_LABEL
#define DEBUG_FIND_BY_LABEL_FAILED
//#define DEBUG_BUILD_SEARCHABLE_LIST
//#define DEBUG_SET_SELECTED_MEMBER

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Sisus
{
	/// <summary>
	/// Handles drawing popup menus.
	/// </summary>
	public class PopupMenu
	{
		public const float MinWidth = 228f;
		public const float TotalMaxHeightWithNavigationBar = TopPartHeightWithNavigationBar + ScrollAreaMaxHeight;

		private const int MaxFullyVisibleMembers = 18;
		private const int PageUpDownJumpAmount = MaxFullyVisibleMembers - 2;
		private const string PrefsString = "PowerInspectorPopupMenuSearchString";
		private const string ControlName = "PowerInspectorPopupMenuFilter";
		private const float FilterAreaHeight = 32f;
		private const float NavigationBarNormalHeight = 24f;
		private const float TopPartHeightWithNavigationBar = FilterAreaHeight + NavigationBarNormalHeight;
		private const float ScrollAreaMaxHeight = 300f;

		private GUIStyle backArrowStyle;

		public static bool isOpen;
		public static PopupMenu instance;
		
		public Action<int> OnCurrentViewItemCountChanged;
		
		private readonly static Color32 BgColorNavigationBarDark = new Color32(62, 62, 62, 255);
		#if UNITY_2019_3_OR_NEWER
		private readonly static Color32 BgColorNavigationBarLight = new Color32(222, 222, 222, 255);
		#else
		private readonly static Color32 BgColorNavigationBarLight = new Color32(170, 170, 170, 255);
		#endif

		public Type builtFromTypeContext = typeof(void);
		public List<PopupMenuItem> rootItems = new List<PopupMenuItem>();
		public Dictionary<string, PopupMenuItem> groupsByLabel = new Dictionary<string, PopupMenuItem>();
		public Dictionary<string, PopupMenuItem> itemsByLabel = new Dictionary<string, PopupMenuItem>();

		private int currentViewItemCount;
		private float width;
		
		private bool searchableListBuilt;
		private string lastAppliedFilter = "";
		private SearchableList searchableList;
		
		/// <summary>
		/// Header label for display at the top of the menu (when there's no active group)
		/// </summary>
		private GUIContent label;
		
		private string filter = "";
		private string setFilter;
		private bool goBackLevelNextLayout;
		private bool clearTextNextLayout;
		private Vector2 scrollPos;

		/// <summary>
		/// Currently active label
		/// </summary>
		private readonly GUIContent currentViewLabel = new GUIContent("");
		private Rect filterFieldRect;
		private Rect filterDiscardRect;
		private Rect dividerRect;
		private Rect headerRect;
		private Rect headerLabelRect;
		private Rect backArrowRect;
		private Rect backArrowBgRect;
		private Rect viewRect;
		private Rect contentRect;

		/// <summary> Index of the menu item which currently has focus, highlighted in blue. </summary>
		private int selectedMemberIndex;
		private int firstVisibleIndex;
		private int lastVisibleIndex = MaxFullyVisibleMembers;
		private bool clearingText;

		/// <summary> Callback for when a menu item is clicked. </summary>
		private Action<PopupMenuItem> onMenuItemClicked;
		/// <summary> Callback for when the menu closes. </summary>
		private Action onClosed;
		/// <summary> The inspector for which the menu was opened. </summary>
		private IInspector inspector;
		/// <summary> Currently open PopupMenuItem group, whose members are being listed. </summary>
		private PopupMenuItem activeGroup;
		private readonly List<PopupMenuItem> itemsFiltered = new List<PopupMenuItem>();
		private List<PopupMenuItem> currentViewItems;
		
		private List<PopupMenuItem> tickedItems;
		private bool canTickMultiple;
		
		private Vector2 localDrawAreaOffset;

		public static PopupMenuItem SelectedItem
		{
			get
			{
				if(instance == null)
				{
					return null;
				}
				int index = instance.selectedMemberIndex;
				return index < 0 || index >= instance.currentViewItemCount ? null : instance.currentViewItems[index];
			}
		}

		private Color32 BgColor
		{
			get
			{
				return inspector.Preferences.theme.Background;
			}
		}

		private static Color32 BgColorNavigationBar
		{
			get
			{
				return DrawGUI.IsProSkin ? BgColorNavigationBarDark : BgColorNavigationBarLight;
			}
		}

		private float ScrollAreaHeight
		{
			get
			{
				return Mathf.Min(currentViewItemCount * DrawGUI.SingleLineHeight, ScrollAreaMaxHeight);
			}
		}

		public float TotalHeight
		{
			get
			{
				return TopPartHeight + ScrollAreaHeight;
			}
		}

		private float NavigationBarHeight
		{
			get
			{
				return currentViewLabel.text.Length > 0 || activeGroup != null ? NavigationBarNormalHeight : 0f;
			}
		}

		private float TopPartHeight
		{
			get
			{
				return FilterAreaHeight + NavigationBarHeight;
			}
		}

		private float Width
		{
			get
			{
				return width;
			}
		}

		private string FilterString
		{
			get
			{
				return filter;
			}

			set
			{
				filter = value;
				setFilter = value;
				SetActiveItem(null);
			}
		}

		public static PopupMenu Create(IInspector setInspector, List<PopupMenuItem> setItems, Dictionary<string, PopupMenuItem> setGroupsByLabel, Dictionary<string, PopupMenuItem> setItemsByLabel, Rect openPosition, Action<PopupMenuItem> setOnMenuItemClicked, Action onClosed, GUIContent setLabel)
		{
			if(instance == null)
			{
				instance = new PopupMenu();
			}
			instance.Setup(setInspector, setItems, setGroupsByLabel, setItemsByLabel, openPosition, setOnMenuItemClicked, onClosed, setLabel);
			return instance;
		}

		private PopupMenu() { }
		
		public void Setup(IInspector setInspector, List<PopupMenuItem> setItems, Dictionary<string, PopupMenuItem> setGroupsByLabel, Dictionary<string, PopupMenuItem> setItemsByLabel, Rect openPosition, Action<PopupMenuItem> setOnMenuItemClicked, Action setOnClosed, GUIContent setLabel)
		{
			inspector = null;
			
			label = setLabel;

			//TO DO: Set active item using current value for fields
			activeGroup = null;

			itemsFiltered.Clear();
			lastAppliedFilter = "";

			filter = "";
			setFilter = "";
			currentViewLabel.text = label.text;
			currentViewLabel.tooltip = label.tooltip;

			onClosed = setOnClosed;
			onMenuItemClicked = setOnMenuItemClicked;
			inspector = setInspector;
			rootItems = setItems;
			groupsByLabel = setGroupsByLabel;
			itemsByLabel = setItemsByLabel;
			searchableListBuilt = false;
			
			RebuildIntructionsInChildren();

			Open(openPosition);
		}

		public void OnClosed()
		{
			DrawGUI.EditingTextField = false;
			isOpen = false;

			#if DEV_MODE && DEBUG_CLOSE
			Debug.Log("PopupMenu.OnClosed!");
			#endif
		}

		private void OnMenuItemClicked(PopupMenuItem item)
		{
			onMenuItemClicked(item);
			Close();
		}
		
		private void BuildSearchableList()
		{
			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.BeginSample("PopupMenu.BuildSearchableList");
			#endif

			searchableListBuilt = true;

			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.BeginSample("BuildSearchableList.GetFullLabelsInChildren");
			#endif
			
			int count = rootItems.Count;
			var pathBuilder = new List<string>(count);
			for(int n = count - 1; n >= 0; n--)
			{
				rootItems[n].GetFullLabelsInChildren(ref pathBuilder);
			}

			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.EndSample();
			#endif

			if(searchableList != null)
			{
				SearchableListPool.Dispose(ref searchableList);
			}
			searchableList = SearchableListPool.Create(pathBuilder.ToArray());
			
			#if DEV_MODE && DEBUG_BUILD_SEARCHABLE_LIST
			Debug.Log("BuildSearchableList result:\n"+StringUtils.ToString(searchableList.Items, "\n"));
			#endif
			

			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.EndSample();
			#endif
		}

		private void SetScrollPos(float setY, bool updateVisibleMembers)
		{
			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.BeginSample("PopupMenu.SetScrollPos");
			#endif

			GUI.changed = true;

			scrollPos.y = setY;

			firstVisibleIndex = Mathf.FloorToInt(scrollPos.y / DrawGUI.SingleLineHeight);
			lastVisibleIndex = Mathf.CeilToInt(scrollPos.y / DrawGUI.SingleLineHeight) + MaxFullyVisibleMembers;
			lastVisibleIndex = Mathf.Min(currentViewItemCount - 1, lastVisibleIndex);

			if(updateVisibleMembers)
			{
				UpdateVisibleMembers();
			}

			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.EndSample();
			#endif
		}
		
		private void RebuildIntructionsInChildren()
		{
			if(activeGroup == null)
			{
				currentViewItems = GetFiltered();
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(!currentViewItems.Contains(null), "GetFiltered(\""+filter+ "\") result had null children!\nsearchableList.Items:\n"+StringUtils.ToString(searchableList == null ? null : searchableList.Items, "\n")+"\n\n\nrootItems:\n"+StringUtils.ToString(rootItems, "\n"));
				#endif
			}
			else
			{
				currentViewItems = activeGroup.children;
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(!currentViewItems.Contains(null), "Group "+activeGroup.label+" had null children!");
				#endif
			}
			int countWas = currentViewItemCount;
			currentViewItemCount = currentViewItems.Count;
			
			UpdateContentRect();

			SetScrollPos(0f, false);
			selectedMemberIndex = -1;
			SetSelectedMember(0, false);
			UpdateVisibleMembers();
			
			if(countWas != currentViewItemCount && OnCurrentViewItemCountChanged != null)
			{
				OnCurrentViewItemCountChanged(currentViewItemCount);
			}
		}

		private List<PopupMenuItem> GetFiltered(int maxMismatchThreshold = 7, int maxNumberOfResults = 50)
		{
			int filterLength = filter.Length;
			if(filterLength == 0)
			{
				return rootItems;
			}
			
			if(!string.Equals(filter, lastAppliedFilter))
			{
				lastAppliedFilter = filter;

				if(!searchableListBuilt)
				{
					BuildSearchableList();
				}
				
				searchableList.Filter = filter;
				var matches = searchableList.GetValues(maxMismatchThreshold);
				int count = Mathf.Min(matches.Length, maxNumberOfResults);

				#if UNITY_EDITOR
				AssetPreview.SetPreviewTextureCacheSize(maxNumberOfResults);
				#endif

				itemsFiltered.Clear();

				if(!searchableListBuilt)
				{
					BuildSearchableList();
				}
				
				for(int n = 0; n < count; n++)
				{
					var item = itemsByLabel[matches[n]];
					itemsFiltered.Add(item);
					#if DEV_MODE && PI_ASSERTATIONS
					if(item == null) { Debug.LogError("FindByLabel(\""+ matches[n]+ "\") returned null\nwith rootItems:\n"+StringUtils.ToString(rootItems, "\n")); }
					#endif
				}
			}
			return itemsFiltered;
		}

		private void UpdateContentRect()
		{
			contentRect.height = currentViewItemCount * DrawGUI.SingleLineHeight;

			bool hasVerticalScrollBar = contentRect.height > ScrollAreaHeight;
			if(hasVerticalScrollBar)
			{
				//compensate for scrollbar width to avoid horizontal scrollbar appearing
				contentRect.width = Width - DrawGUI.ScrollBarWidth;
			}
			else
			{
				contentRect.width = Width;
			}

			// also update viewRect height since
			// it auto-shrinks if there's not enough
			// content to fill out ScrollAreaMaxHeight
			viewRect.height = ScrollAreaHeight;
		}

		private void UpdateVisibleMembers()
		{
			GUI.changed = true;
		}

		private void GetDrawPositions(Rect openPosition)
		{
			width = openPosition.width;
			
			viewRect = openPosition;
			viewRect.width = width;
			viewRect.height = TotalHeight;
			viewRect.x = 0f;
			viewRect.y = 0f;
			
			filterFieldRect = viewRect;
			filterFieldRect.height = FilterAreaHeight;
			filterFieldRect.x += 7f;
			filterFieldRect.y = 6f;
			filterFieldRect.width -= 16f + 14f;

			filterDiscardRect = filterFieldRect;
			filterDiscardRect.x += filterFieldRect.width;
			filterDiscardRect.width = 16f;
			filterDiscardRect.y += 1f;

			headerRect = viewRect;
			headerRect.y += FilterAreaHeight;
			headerRect.height = NavigationBarHeight;

			backArrowRect = headerRect;
			backArrowRect.width = backArrowStyle.normal.background.width;
			backArrowRect.height = backArrowStyle.normal.background.height;
			backArrowRect.x += 3f;
			backArrowRect.y += 5f;

			backArrowBgRect = headerRect;
			backArrowBgRect.width = backArrowRect.xMax + 2f;
			backArrowBgRect.y += 2f;
			backArrowBgRect.height -= 4f;

			headerLabelRect = headerRect;
			
			dividerRect = headerRect;
			dividerRect.y = TopPartHeight;
			dividerRect.height = 1f;
			dividerRect.width = width;

			viewRect.y = TopPartHeight;
			viewRect.height = ScrollAreaHeight;

			contentRect = viewRect;
			contentRect.y = 0f;
			contentRect.height = currentViewItemCount * DrawGUI.SingleLineHeight;

			bool hasVerticalScrollBar = contentRect.height > ScrollAreaHeight;
			if(hasVerticalScrollBar)
			{
				contentRect.width = width - DrawGUI.ScrollBarWidth;
			}

			viewRect.y = TopPartHeight;

			localDrawAreaOffset = DrawGUI.GetLocalDrawAreaOffset();
		}

		public bool OnGUI(ref bool menuItemWasClicked)
		{
			bool dirty = false;

			var e = Event.current;
			var eventType = e.type;
			switch(eventType)
			{
				case EventType.KeyDown:
					if(OnKeyboardInputGiven(e))
					{
						return true;
					}
					break;
				case EventType.MouseDown:
				case EventType.MouseMove:
					dirty = true;
					break;
				case EventType.Layout:
					if(goBackLevelNextLayout)
					{
						goBackLevelNextLayout = false;
						GoBackLevel();
					}
					else if(clearTextNextLayout)
					{
						clearTextNextLayout = false;
						ClearText();
					}
					break;
			}

			if(Draw(ref menuItemWasClicked))
			{
				dirty = true;
			}

			return dirty;
		}
		
		private bool Draw(ref bool menuItemWasClicked)
		{
			if(instance == null)
			{
				return false;
			}

			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.BeginSample("PopupMenu.Draw");
			#endif

			PopupMenuItemUtility.drawWithFullPath = filter.Length > 0;

			bool dirty = false;
			var e = Event.current;
			//using raw type, since we consume all click events
			var eventType = e.rawType;
			bool lmbClick = eventType == EventType.MouseDown && e.button == 0;
			bool mmbClick = eventType == EventType.MouseDown && e.button == 2;

			if(clearingText)
			{
				if(eventType == EventType.Layout)
				{
					clearingText = false;
				}
				GUI.changed = true;
				dirty = true;
			}
			else
			{
				if(!DrawGUI.EditingTextField)
				{
					DrawGUI.EditingTextField = true;
					GUI.changed = true;
					dirty = true;
				}
				
				if(!string.Equals(GUI.GetNameOfFocusedControl(), ControlName))
				{
					DrawGUI.FocusControl(ControlName);
					GUI.changed = true;
					dirty = true;
				}
			}

			if(DrawFilterField())
			{
				dirty = true;
			}
			
			EditorGUI.DrawRect(headerRect, BgColorNavigationBar);
			//DrawGUI.DrawRect(headerRect, new Color32(24, 24, 24, 255), localDrawAreaOffset);

			bool hasFilter = FilterString.Length > 0;
			bool drawBackArrow = !hasFilter && activeGroup != null;
			bool drawLabelField = currentViewLabel.text.Length > 0;
			
			if(drawBackArrow || drawLabelField)
			{
				DrawGUI.DrawRect(headerRect, new Color32(24, 24, 24, 255), localDrawAreaOffset);

				if(drawLabelField)
				{
					GUI.Label(headerLabelRect, currentViewLabel, InspectorPreferences.Styles.PopupMenuTitle);
				}

				if(drawBackArrow)
				{
					DrawGUI.Active.ColorRect(backArrowBgRect, BgColorNavigationBar);
					DrawGUI.Active.AddCursorRect(backArrowRect, MouseCursor.Link);
					GUI.Label(backArrowRect, GUIContent.none, backArrowStyle);
					if(lmbClick && backArrowRect.Contains(e.mousePosition))
					{
						goBackLevelNextLayout = true;
						dirty = true;
					}
				}

				GUI.DrawTexture(dividerRect, InspectorUtility.Preferences.graphics.horizontalSplitterBg);
			}

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(contentRect.x == 0f, "contentRect.x="+ contentRect.x);
			Debug.Assert(contentRect.y == 0f, "contentRect.y="+ contentRect.y);
			Debug.Assert(contentRect.height == currentViewItemCount * DrawGUI.SingleLineHeight, "contentRect.height="+ contentRect.height+ ", currentViewItemCount="+ currentViewItemCount);
			Debug.Assert(currentViewItemCount == currentViewItems.Count, "currentViewItemCount=" + currentViewItemCount + ", currentViewItems.Count="+ currentViewItems.Count);
			Debug.Assert(viewRect.height == contentRect.height || viewRect.height == ScrollAreaMaxHeight, "viewRect.height ("+ viewRect.height+ ") != contentRect.height (" + contentRect.height+ ") NOR ScrollAreaMaxHeight (" + ScrollAreaMaxHeight+")");
			#endif

			var setScrollPos = GUI.BeginScrollView(viewRect, scrollPos, contentRect);
			{
				if(setScrollPos.y != scrollPos.y)
				{
					SetScrollPos(setScrollPos.y, true);
					dirty = true;
				}
				
				var memberRect = contentRect;
				DrawGUI.Active.ColorRect(memberRect, BgColor);

				memberRect.height = DrawGUI.SingleLineHeight;

				//only start drawing from first visible member
				memberRect.y += firstVisibleIndex * DrawGUI.SingleLineHeight;

				int last = lastVisibleIndex;
				if(last >= currentViewItemCount)
				{
					#if DEV_MODE
					Debug.LogWarning(ToString()+" - last ("+last+") >= visibleCount ("+ (lastVisibleIndex - firstVisibleIndex + 1) + ") with firstVisibleIndex="+ firstVisibleIndex+ " and lastVisibleIndex="+ lastVisibleIndex);
					#endif
					last = currentViewItemCount - 1;
				}

				for(int n = firstVisibleIndex; n <= last; n++)
				{
					var item = currentViewItems[n];
					bool selected = n == selectedMemberIndex;

					if(selected)
					{
						DrawGUI.Active.ColorRect(memberRect, InspectorUtility.Preferences.theme.BackgroundSelected);
					}

					if(memberRect.Contains(e.mousePosition))
					{
						if(!selected)
						{
							SetSelectedMember(n, false);
							dirty = true;
						}

						if(lmbClick)
						{
							dirty = true;
							if(item.IsGroup)
							{
								SetActiveItem(item);
								break;
							}

							menuItemWasClicked = true;
							inspector.OnNextLayout(()=> OnMenuItemClicked(item));
							break;
						}
						
						if(mmbClick)
						{
							if(item.IsGroup)
							{
								var unityObjects = item.children.Select(menuItem => menuItem.IdentifyingObject as Object).ToArray();
								if(unityObjects.Length > 0)
								{
									DrawGUI.Ping(unityObjects);
								}
							}
							else
							{
								var itemValue = item.IdentifyingObject;
								var unityObject = itemValue as Object;
								if(unityObject != null)
								{
									DrawGUI.Ping(unityObject);
								}
								var unityObjects = itemValue as Object[];
								if(unityObjects != null)
								{
									DrawGUI.Ping(unityObjects);
								}
							}
						}
					}

					if(PopupMenuItemUtility.Draw(memberRect, selectedMemberIndex == n, item))
					{
						#if DEV_MODE
						Debug.Log(GetType().Name + " - member " + item + " clicked with eventType " + eventType + "! (member returned true)");
						#endif

						dirty = true;
						
						if(item.IsGroup)
						{
							SetActiveItem(item);
							break;
						}
						
						menuItemWasClicked = true;
						inspector.OnNextLayout(() => OnMenuItemClicked(item));
						DrawGUI.Use(e);
						break;
					}

					if(!item.IsGroup)
					{
						bool ticked = tickedItems != null && tickedItems.Contains(item);

						var toggleRect = memberRect;
						toggleRect.width = 16f;
						toggleRect.x += memberRect.width - toggleRect.width;

						if(canTickMultiple)
						{
							bool setTicked = GUI.Toggle(toggleRect, ticked, GUIContent.none, InspectorUtility.Preferences.GetStyle("OL Toggle"));
							if(setTicked != ticked)
							{
								if(ticked)
								{
									tickedItems.Add(item);
								}
								else
								{
									tickedItems.Remove(item);
								}

								menuItemWasClicked = true;
								inspector.OnNextLayout(() => OnMenuItemClicked(item));
								DrawGUI.Use(e);
								break;
							}
						}
						else if(ticked)
						{
							GUI.Toggle(toggleRect, true, GUIContent.none, InspectorUtility.Preferences.GetStyle("OL Toggle"));
						}
					}

					GUI.enabled = true;
					memberRect.y += DrawGUI.SingleLineHeight;
				}

				///after visible part, count * DrawGUI.singleLineHeight 
				GUI.EndScrollView();
			}
			
			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.EndSample();
			#endif

			return dirty;
		}
		
		private bool DrawFilterField()
		{
			GUI.SetNextControlName(ControlName);
			setFilter = DrawGUI.Active.TextField(filterFieldRect, setFilter, "SearchTextField");
			bool dirty = false;

			if(setFilter.Length == 0)
			{
				GUI.Label(filterDiscardRect, GUIContent.none, "SearchCancelButtonEmpty");
			}
			else
			{
				GUI.Label(filterDiscardRect, GUIContent.none, "SearchCancelButton");
				if(Event.current.type == EventType.MouseDown && Event.current.button == 0 && filterDiscardRect.Contains(Event.current.mousePosition))
				{
					clearTextNextLayout = true;
					dirty = true;
				}
			}

			if(!string.Equals(setFilter, FilterString))
			{
				if(Event.current.type == EventType.Layout)
				{
					FilterString = setFilter;
					dirty = true;
				}
				else
				{
					dirty = true;
				}
			}
			
			return dirty;
		}
		
		private void ClearText()
		{
			#if DEV_MODE
			Debug.Log(GetType().Name + ".ClearText()");
			#endif

			clearingText = true;
			FilterString = "";
			KeyboardControlUtility.KeyboardControl = 0;
			DrawGUI.EditingTextField = false;
			GUI.changed = true;
		}
		
		private void Open(Rect openPosition)
		{
			#if DEV_MODE && DEBUG_OPEN_POSITION
			Debug.Log("PoupMenuDrawer.Open @ "+openPosition);
			#endif

			backArrowStyle = "AC LeftArrow";

			isOpen = true;

			if(openPosition.width < MinWidth)
			{
				openPosition.width = MinWidth;
			}
			GetDrawPositions(openPosition);
			UpdateVisibleMembers();
			FilterString = Platform.Active.GetPrefs(PrefsString, FilterString);
			KeyboardControlUtility.KeyboardControl = 0;
			GUI.changed = true;
		}
		
		/// <summary>
		/// called when this is the selected control and 
		/// keyboard input is given
		/// </summary>
		private bool OnKeyboardInputGiven(Event inputEvent)
		{
			switch(inputEvent.keyCode)
			{
				case KeyCode.RightArrow:
					if(FilterString.Length > 0)
					{
						return false;
					}

					GUI.changed = true;
					DrawGUI.Use(inputEvent);
					if(selectedMemberIndex >= 0 && currentViewItemCount > selectedMemberIndex)
					{
						var item = currentViewItems[selectedMemberIndex];

						//if selected member is a category, open the category
						if(item.IsGroup)
						{
							SetActiveItem(item);
							return true;
						}
						//else call the on item clicked callback
						OnMenuItemClicked(item);
						return true;
					}
					return false;
				case KeyCode.LeftArrow:
				case KeyCode.Backspace:
					if(FilterString.Length == 0 && selectedMemberIndex >= 0)
					{
						goBackLevelNextLayout = true;
						GUI.changed = true;
						return true;
					}
					return false;
				case KeyCode.Return:
				case KeyCode.KeypadEnter:
					GUI.changed = true;
					DrawGUI.Use(inputEvent);
					if(selectedMemberIndex >= 0 && currentViewItemCount > selectedMemberIndex)
					{
						var item = currentViewItems[selectedMemberIndex];

						//if selected member is a category, open the category
						if(item.IsGroup)
						{
							SetActiveItem(item);
							return true;
						}
						//else call the on item clicked callback
						OnMenuItemClicked(item);
						return true;
					}
					return false;
				case KeyCode.UpArrow:
					if(selectedMemberIndex > 0)
					{
						SetSelectedMember(selectedMemberIndex - 1, true);
						return true;
					}
					return false;
				case KeyCode.DownArrow:
					int nextIndex = selectedMemberIndex + 1;
					if(nextIndex < currentViewItemCount)
					{
						SetSelectedMember(nextIndex, true);
						return true;
					}
					return false;
				case KeyCode.Home:
					if(selectedMemberIndex > 0)
					{
						SetSelectedMember(0, true);
						return true;
					}
					return false;
				case KeyCode.End:
					int lastIndex = currentViewItemCount - 1;
					if(selectedMemberIndex < lastIndex)
					{
						SetSelectedMember(lastIndex, true);
						return true;
					}
					return false;
				case KeyCode.PageUp:
					if(selectedMemberIndex > 0)
					{
						int select = selectedMemberIndex - PageUpDownJumpAmount;
						if(select < 0)
						{
							select = 0;
						}
						SetSelectedMember(select, true);
						return true;
					}
					return false;
				case KeyCode.PageDown:
					lastIndex = currentViewItemCount - 1;
					if(selectedMemberIndex < lastIndex)
					{
						int select = selectedMemberIndex + PageUpDownJumpAmount;
						if(select > lastIndex)
						{
							select = lastIndex;
						}
						SetSelectedMember(select, true);
						return true;
					}
					return false;
				case KeyCode.Escape:
					DrawGUI.Use(inputEvent);
					if(FilterString.Length > 0)
					{
						clearTextNextLayout = true;
					}
					else if(activeGroup != null)
					{
						goBackLevelNextLayout = true;
						GUI.changed = true;
						//GoBackLevel();
					}
					else
					{
						Close();
					}
					KeyboardControlUtility.KeyboardControl = 0;
					DrawGUI.EditingTextField = false;
					return true;
			}
			return false;
		}

		public void SetTickedMembers(List<PopupMenuItem> setTickedItems, bool setCanTickMultipleMembers)
		{
			#if DEV_MODE
			Debug.Log("PopupMenu.SetTickedMembers("+StringUtils.ToString(setTickedItems)+")");
			#endif

			tickedItems = setTickedItems;
			canTickMultiple = setCanTickMultipleMembers;

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(tickedItems == null || tickedItems.Count <= 1 || canTickMultiple);
			#endif
		}
		
		public void SetSelectedMember(string selectByLabel)
		{
			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.BeginSample("PopupMenu.SetSelectedMember");
			#endif
			
			#if DEV_MODE && DEBUG_SET_SELECTED_MEMBER
			Debug.Log("PopupMenu.SetSelectedMember(\""+selectByLabel+"\")");
			#endif

			if(!searchableListBuilt)
			{
				BuildSearchableList();
			}

			PopupMenuItem item;
			if(itemsByLabel.TryGetValue(selectByLabel, out item))
			{
				SetActiveItem(item.parent);
				var items = activeGroup == null ? rootItems : activeGroup.children;
				int index = items.IndexOf(item);
				SetSelectedMember(index, true);
			}
			else if(groupsByLabel.TryGetValue(selectByLabel, out item))
			{
				SetActiveItem(item);
				SetSelectedMember(0, true);
			}
			else
			{
				#if DEV_MODE
				Debug.LogWarning("SetSelectedMember("+StringUtils.ToString(selectByLabel)+"): item not found among "+StringUtils.ToString(itemsByLabel.Count) +" items:\n"+StringUtils.ToString(rootItems, "\n"));
				#endif
			}

			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.EndSample();
			#endif
		}
		
		private void SetSelectedMember(int index, bool scrollToShow)
		{
			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.BeginSample("PopupMenu.SetSelectedMember");
			#endif

			#if DEV_MODE && DEBUG_SET_SELECTED_MEMBER
			Debug.Log("SetSelectedMember("+index+", "+ scrollToShow+ ") with currentViewItemCount="+ currentViewItemCount+ ", selectedMemberIndex="+ selectedMemberIndex);
			#endif

			if(currentViewItemCount == 0)
			{
				selectedMemberIndex = 0;

				#if DEV_MODE
				Profiler.EndSample();
				#endif
				return;
			}

			if(index <= 0)
			{
				index = 0;
			}
			else if(index >= currentViewItemCount)
			{
				index = currentViewItemCount - 1;
			}

			//new test
			if(index == selectedMemberIndex)
			{
				#if DEV_MODE
				Debug.LogWarning("Ignoring SetSelectedMember("+index+", "+StringUtils.True+") because selected already matched index");
				#endif
				return;
			}

			GUI.changed = true;
			
			selectedMemberIndex = index;

			if(scrollToShow)
			{
				float yMin = scrollPos.y;
				float yTargetMin = selectedMemberIndex * DrawGUI.SingleLineHeight;

				if(yTargetMin < yMin)
				{
					SetScrollPos(yTargetMin, true);
				}
				else
				{
					//this seemed to cause a bug where the menu would start scrolling towards the bottom upon being opened
					//but why is this even called?
					float yMax = scrollPos.y + viewRect.height + DrawGUI.SingleLineHeight;
					float yTargetMax = yTargetMin + DrawGUI.SingleLineHeight;

					if(yTargetMax >= yMax)
					{
						float setY = yTargetMax - viewRect.height;
						#if DEV_MODE
						Debug.Log("yTargetMax (" + yTargetMax + ") > yMax (" + yMax + "): setting scrollPos to " + setY + " (from " + scrollPos.y + ")");
						#endif
						SetScrollPos(setY, true);
					}
				}
			}

			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.EndSample();
			#endif
		}
		
		private void Close()
		{
			#if DEV_MODE && DEBUG_CLOSE
			Debug.Log(GetType().Name+ ".Close() with onClosed="+StringUtils.ToString(onClosed));
			#endif
			
			DrawGUI.EditingTextField = false;
			isOpen = false;
			selectedMemberIndex = -1;
			
			if(onClosed != null)
			{
				var callback = onClosed;
				onClosed = null;
				callback();
			}
		}

		private void SetActiveItem(PopupMenuItem value)
		{
			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.BeginSample("PopupMenu.SetActiveItem");
			#endif

			activeGroup = value;
			UpdateCurrentViewLabel();
			RebuildIntructionsInChildren();
			GUI.changed = true;

			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.EndSample();
			#endif
		}

		private void UpdateCurrentViewLabel()
		{
			if(FilterString.Length > 0 && currentViewLabel.text.Length > 0)
			{
				currentViewLabel.text = "Search";
				currentViewLabel.tooltip = "";
			}
			else if(activeGroup != null)
			{
				currentViewLabel.text = activeGroup.label;
				currentViewLabel.tooltip = activeGroup.secondaryLabel;
			}
			else
			{
				currentViewLabel.text = label.text;
				currentViewLabel.tooltip = label.tooltip;
			}
		}
		
		private void GoBackLevel()
		{
			if(activeGroup == null)
			{
				return;
			}
			SetActiveItem(activeGroup.parent);
		}
		
		public void DisposeItems()
		{
			builtFromTypeContext = Types.Void;
			for(int n = rootItems.Count - 1; n >= 0; n--)
			{
				rootItems[n].Dispose();
			}
			rootItems.Clear();
			currentViewItemCount = -1;
		}
	}
}