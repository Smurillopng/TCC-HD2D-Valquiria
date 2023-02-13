#define DEBUG_ON_SELECTED
//#define DEBUG_SELECT_NEXT_PART
#define DEBUG_SET_SELECTED_ITEM
#define DEBUG_KEYBOARD_INPUT

using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sisus
{
	public abstract class InspectorToolbar : IInspectorToolbar
	{
		private static List<IInspectorToolbarItem> reusableItemsList = new List<IInspectorToolbarItem>();
		[NotNull]
		private static readonly List<float> reusableFloatsList = new List<float>();

		private int selectedItemIndex = -1;
		[NotNull]
		private IInspectorToolbarItem[] items;
		[NotNull]
		private IInspectorToolbarItem[] visibleItems;
		protected IInspector inspector;
		protected int mouseoverVisibleItemIndex = -1;
		private bool updateItemBounds = true;

		/// <inheritdoc/>
		public IInspectorToolbarItem SelectedItem
		{
			get
			{
				return selectedItemIndex == -1 ? null : items[selectedItemIndex];
			}
		}
		
		protected int SelectedVisibleItemIndex
		{
			get
			{
				return selectedItemIndex == -1 ? -1 : Array.IndexOf(visibleItems, items[selectedItemIndex]);
			}
		}

		/// <inheritdoc/>
		public IInspectorToolbarItem[] Items
		{
			get
			{
				return items;
			}
		}

		/// <summary> Gets a value indicating whether the toolbar has currently keyboard focus. </summary>
		/// <value> True if toolbar is selected, false if not. </value>
		private bool IsSelected
		{
			get
			{
				return inspector.Manager.SelectedInspectorPart == InspectorPart.Toolbar;
			}
		}

		/// <summary> Gets the inspector that contains the toolbar. </summary>
		/// <value> The containing inspector. </value>
		protected IInspector Inspector
		{
			get
			{
				return inspector;
			}
		}

		/// <summary> Gets the current height of the toolbar. </summary>
		/// <value> Toolbar height. </value>
		public abstract float Height { get; }
		
		/// <inheritdoc/>
		public TToolbarItemType GetItem<TToolbarItemType>() where TToolbarItemType : class, IInspectorToolbarItem
		{
			for(int n = items.Length - 1; n >= 0; n--)
			{
				var item = items[n] as TToolbarItemType;
				if(item != null)
				{
					return item;
				}
			}
			return null;
		}

		public TToolbarItemType GetVisibleItem<TToolbarItemType>() where TToolbarItemType : class, IInspectorToolbarItem
		{
			for(int n = visibleItems.Length - 1; n >= 0; n--)
			{
				var item = visibleItems[n] as TToolbarItemType;
				if(item != null)
				{
					return item;
				}
			}
			return null;
		}

		public TToolbarItem GetSelectableItem<TToolbarItem>() where TToolbarItem : class, IInspectorToolbarItem
		{
			for(int n = visibleItems.Length - 1; n >= 0; n--)
			{
				var item = visibleItems[n] as TToolbarItem;
				if(item != null && item.Selectable)
				{
					return item;
				}
			}
			return null;
		}

		public IInspectorToolbarItem GetItemByType(Type itemType)
		{
			for(int n = visibleItems.Length - 1; n >= 0; n--)
			{
				var item = visibleItems[n];
				if(item != null && item.GetType() == itemType)
				{
					return item;
				}
			}
			return null;
		}

		public void SetSelectedItem(IInspectorToolbarItem item, ReasonSelectionChanged reason)
		{
			#if DEV_MODE && DEBUG_SET_SELECTED_ITEM
			Debug.Log(StringUtils.ToColorizedString("SetSelectedItem(", item, "), was: ", SelectedItem));
			#endif

			#if DEV_MODE && PI_ASSERTATIONS
			if(item != null && inspector.Manager.SelectedInspectorPart != InspectorPart.Toolbar) { Debug.LogError(GetType().Name+".SetSelectedItem("+item.GetType().Name+ ") called but SelectedInspectorPart was "+ inspector.Manager.SelectedInspectorPart); }
			#endif

			if(item == null)
			{
				if(selectedItemIndex != -1)
				{
					items[selectedItemIndex].OnDeselected(reason);
					selectedItemIndex = -1;
				}
				return;
			}

			int setIndex = Array.IndexOf(items, item);

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(setIndex != -1);
			#endif

			if(selectedItemIndex != setIndex)
			{
				if(selectedItemIndex != -1)
				{
					items[selectedItemIndex].OnDeselected(reason);
				}
				selectedItemIndex = setIndex;

				var select = items[setIndex];

				var documentationUrl = select.DocumentationPageUrl;
				if(documentationUrl.Length > 0)
				{
					PowerInspectorDocumentation.ShowFeatureIfWindowOpen(documentationUrl);
				}

				select.OnSelected(reason);
			}
		}

		public void SetSelectedItem(int setIndex, ReasonSelectionChanged reason)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(setIndex >= -1);
			Debug.Assert(setIndex < items.Length);
			#endif

			#if DEV_MODE && DEBUG_SET_SELECTED_ITEM
			Debug.Log(StringUtils.ToColorizedString("SetSelectedItem(", setIndex, "), was: ", selectedItemIndex));
			#endif

			if(setIndex != selectedItemIndex)
			{
				if(selectedItemIndex != -1)
				{
					items[selectedItemIndex].OnDeselected(reason);
				}

				selectedItemIndex = setIndex;
			
				if(setIndex != -1)
				{
					var select = items[setIndex];
					if(!select.Selectable)
					{
						#if DEV_MODE
						Debug.LogWarning("SetSelectedItem called for item that was not selectable: "+select.GetType().Name);
						#endif
						SetSelectedItem(-1, reason);
						return;
					}
					items[setIndex].OnSelected(reason);
				}
			}
		}
		
		/// <inheritdoc/>
		public virtual void Setup(IInspector setInspector)
		{
			inspector = setInspector;
			inspector.State.OnInspectedTargetsChanged -= OnInspectedTargetsChanged;
			inspector.State.OnInspectedTargetsChanged += OnInspectedTargetsChanged;

			items = ToolbarUtility.GetItemsForToolbar(setInspector, this);
			visibleItems = items;
			UpdateVisibleItems();
			updateItemBounds = true;
			inspector.State.OnWidthChanged += UpdateItemBounds;
		}
		
		private void OnInspectedTargetsChanged(Object[] inspected, DrawerGroup drawers)
		{
			UpdateVisibleItems();
		}

		/// <summary> Called during ValidateCommand event when toolbar is selected. </summary>
		/// <param name="e"> An Event to process. </param>
		public virtual void OnValidateCommand(Event e)
		{
			if(selectedItemIndex != -1)
			{
				items[selectedItemIndex].OnValidateCommand(e);
			}
		}

		/// <inheritdoc/>
		public virtual void OnSelected(ReasonSelectionChanged reason)
		{
			#if DEV_MODE && DEBUG_ON_SELECTED
			Debug.Log(GetType().Name+".OnSelected("+reason+")");
			#endif

			switch(reason)
			{
				case ReasonSelectionChanged.ThisClicked:
				case ReasonSelectionChanged.ControlClicked:
					if(mouseoverVisibleItemIndex != -1)
					{
						var item = visibleItems[mouseoverVisibleItemIndex];
						if(item.Selectable)
						{
							SetSelectedItem(item, reason);
							return;
						}
						SetSelectedItem(-1, reason);
						return;
					}
					if(TrySelectSearchBox(reason))
					{
						return;
					}
					SelectFirstSelectableItem(reason);
					return;
				case ReasonSelectionChanged.SelectNextControl:
				case ReasonSelectionChanged.SelectControlRight:
					SelectFirstSelectableItem(reason);
					return;
				case ReasonSelectionChanged.SelectPrevControl:
				case ReasonSelectionChanged.SelectControlLeft:
					SelectLastSelectableItem(reason);
					return;
				case ReasonSelectionChanged.SelectControlUp:
				case ReasonSelectionChanged.SelectControlDown:
				case ReasonSelectionChanged.SelectPrevComponent:
				case ReasonSelectionChanged.SelectNextComponent:
					if(TrySelectSearchBox(reason))
					{
						return;
					}
					SelectFirstSelectableItem(reason);
					return;				
				default:
					inspector.OnNextLayout(()=>
					{
						if(selectedItemIndex != -1)
						{
							return;
						}
						if(TrySelectSearchBox(reason))
						{
							return;
						}
						SelectFirstSelectableItem(reason);
					});
					return;
			}
		}

		/// <summary> Attempts to select search box, if toolbar has one that is currently selectable. </summary>
		/// <param name="reason"> The reason. </param>
		/// <returns> True if it succeeds, false if it fails. </returns>
		protected bool TrySelectSearchBox(ReasonSelectionChanged reason)
		{
			if(inspector.State.WantsSearchBoxDisabled)
			{
				return false;
			}

			var searchBox = GetSelectableItem<ISearchBoxToolbarItem>();
			if(searchBox != null)
			{
				SetSelectedItem(searchBox, reason);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Selects left-most selectable item on the toolbar.
		/// If toolbar has no selectable items, then sets selected item to null.
		/// </summary>
		protected void SelectFirstSelectableItem(ReasonSelectionChanged reason)
		{
			for(int n = 0, count = visibleItems.Length; n < count; n++)
			{
				var item = visibleItems[n];
				if(item.Selectable)
				{
					SetSelectedItem(item, reason);
					return;
				}
			}
			SetSelectedItem(-1, reason);
		}

		/// <summary>
		/// Selects right-most selectable item on the toolbar.
		/// If toolbar has no selectable items, then sets selected item to null.
		/// </summary>
		private void SelectLastSelectableItem(ReasonSelectionChanged reason)
		{
			for(int n = visibleItems.Length - 1; n >= 0; n--)
			{
				var item = visibleItems[n];
				if(item.Selectable)
				{
					SetSelectedItem(item, reason);
					return;
				}
			}
			SetSelectedItem(-1, reason);
		}

		/// <inheritdoc/>
		public virtual void OnDeselected(ReasonSelectionChanged reason)
		{
			if(selectedItemIndex != -1)
			{
				items[selectedItemIndex].OnDeselected(reason);
			}
			selectedItemIndex = -1;
			GUI.changed = true;
		}
		
		private void UpdateVisibleItemBounds(Rect toolbarPosition)
		{
			UpdateVisibleItemWidths(toolbarPosition, reusableFloatsList);

			int count = visibleItems.Length;
			int n = 0;
			var rect = toolbarPosition;
			for(n = 0; n < count; n++)
			{
				var item = visibleItems[n];
				if(item.Alignment == ToolbarItemAlignment.Right)
				{
					break;
				}

				rect.width = reusableFloatsList[n];
				item.Bounds = rect;
				rect.x += rect.width;
			}

			rect = toolbarPosition;
			rect.x += rect.width;
			int stop = n;
			for(n = count - 1; n >= stop; n--)
			{
				var item = visibleItems[n];
				rect.width = reusableFloatsList[n];
				rect.x -= rect.width;
				item.Bounds = rect;
			}
		}

		private void UpdateVisibleItemWidths(Rect toolbarPosition, [NotNull]List<float> itemWidths)
		{
			itemWidths.Clear();

			float remainingWidth = toolbarPosition.width;

			int wantsMoreSpaceCount = 0;

			// first pass: remove min widths from total available width
			for(int n = 0, count = visibleItems.Length; n < count; n++)
			{
				var item = visibleItems[n];
				float minWidth = item.MinWidth;

				float maxWidth = item.MaxWidth;

				if(maxWidth > minWidth)
				{
					wantsMoreSpaceCount++;
				}

				remainingWidth -= minWidth;
				itemWidths.Add(minWidth);
			}

			// Evenly distribute remaining width amongst all controls where max width > min width.
			// Repeat until all available width has been used up or no more items desire more width.
			while(remainingWidth > 0f && wantsMoreSpaceCount > 0)
			{
				float availableSpacePerControl = remainingWidth / wantsMoreSpaceCount;
				
				for(int n = 0, count = visibleItems.Length; n < count; n++)
				{
					var item = visibleItems[n];
					float minWidth = item.MinWidth;
					float maxWidth = item.MaxWidth;
					float wantsMoreSpace = maxWidth - minWidth;
					if(wantsMoreSpace > 0f)
					{
						float giveMoreSpace;
						if(availableSpacePerControl > maxWidth)
						{
							wantsMoreSpaceCount--;
							giveMoreSpace = maxWidth;
						}
						else
						{
							giveMoreSpace = availableSpacePerControl;
						}
						float width = itemWidths[n];
						width += giveMoreSpace;
						remainingWidth -= giveMoreSpace;
						itemWidths[n] = width;
					}
				}
			}
		}

		/// <summary>
		/// Gets the style used when drawing the background of the toolbar and the background of each button on the toolbar by default.
		/// </summary>
		[NotNull]
		protected virtual GUIStyle BackgroundStyle
		{
			get
			{
				return InspectorPreferences.Styles.Toolbar;
			}
		}

		/// <inheritdoc/>
		public virtual void Draw(Rect toolbarPosition)
		{
			var e = Event.current;
			var eventType = e.type;

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(BackgroundStyle != null);
			Debug.Assert(toolbarPosition.x == 0f && toolbarPosition.y == 0f, toolbarPosition);
			#endif

			var backgroundStyle = BackgroundStyle;

			GUI.Label(toolbarPosition, GUIContent.none, backgroundStyle);

			bool isLayoutEvent = eventType == EventType.Layout;
			bool toolbarMouseovered = inspector.MouseoveredPart == InspectorPart.Toolbar;
			
			if(updateItemBounds)
			{
				UpdateVisibleItemBounds(toolbarPosition);

				if(isLayoutEvent)
				{
					updateItemBounds = false;
				}
			}
			
			int count = visibleItems.Length;

			bool detectMouseover = toolbarMouseovered && isLayoutEvent;
			if(detectMouseover)
			{
				mouseoverVisibleItemIndex = -1;
				for(int n = 0; n < count; n++)
				{
					var item = visibleItems[n];
					var bounds = item.Bounds;
					if(bounds.Contains(e.mousePosition))
					{
						mouseoverVisibleItemIndex = n;
					}
				}
			}

			// new: style all items with toolbar style by default
			if(eventType == EventType.Repaint)
			{
				for(int n = 0; n < count; n++)
				{
					var item = visibleItems[n];
					var bounds = item.Bounds;
					item.DrawBackground(bounds, backgroundStyle);
				}
			}

			for(int n = 0; n < count; n++)
			{
				var item = visibleItems[n];
				var bounds = item.Bounds;
				item.Draw(bounds, mouseoverVisibleItemIndex == n);
			}

			if(selectedItemIndex != -1)
			{
				var item = items[selectedItemIndex];
				item.DrawSelectionRect(item.Bounds);
			}
			
			#if DEV_MODE
			if(e.control && (eventType == EventType.ContextClick || (eventType == EventType.MouseDown && e.button == 1)) && toolbarPosition.Contains(e.mousePosition))
			{
				var menu = Menu.Create();

				if(mouseoverVisibleItemIndex != -1)
				{
					var item = visibleItems[mouseoverVisibleItemIndex];
					menu.Add("Edit " + item.GetType().Name + ".cs", () =>
					{
						var script = FileUtility.FindScriptFile(item.GetType());
						if(script != null)
						{
							UnityEditor.AssetDatabase.OpenAsset(script);
						}
						else
						{
							Debug.LogError("FileUtility.FindScriptFilepath could not find file " + GetType().Name + ".cs");
						}
					});
				}

				menu.Add("Edit "+GetType().Name+".cs", ()=>
				{
					var script = FileUtility.FindScriptFile(GetType());
					if(script != null)
					{
						UnityEditor.AssetDatabase.OpenAsset(script);
					}
					else
					{
						Debug.LogError("FileUtility.FindScriptFilepath could not find file "+GetType().Name+".cs");
					}
				});

				menu.Add("Print Info", ()=>
				{
					DebugUtility.PrintFullStateInfo(this);
				});

				menu.Open();
			}
			#endif

			// Prevent controls in the inspector viewport's scroll view from reacting to mouse inputs behind the toolbar.
			if((eventType == EventType.MouseDown || eventType == EventType.ContextClick) && toolbarPosition.Contains(e.mousePosition))
			{
				#if DEV_MODE
				Debug.LogWarning("Consumed click event behind toolbar via GUI.Button: "+StringUtils.ToString(Event.current));
				#endif
				DrawGUI.Use(e);
			}
		}

		/// <inheritdoc/>
		public virtual bool OnClick(Event inputEvent)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(!inspector.IgnoreToolbarMouseInputs());
			#endif

			if(mouseoverVisibleItemIndex != -1)
			{
				if(!IsSelected)
				{
					inspector.Manager.Select(inspector, InspectorPart.Toolbar, ReasonSelectionChanged.ControlClicked);
				}
				var item = visibleItems[mouseoverVisibleItemIndex];
				if(item.Selectable && SelectedItem != item)
				{
					SetSelectedItem(item, ReasonSelectionChanged.ControlClicked);
				}

				if(item.Clickable && item.OnClick(inputEvent))
				{
					GUIUtility.ExitGUI();
					return true;
				}
				return false;
			}
			else
			{
				DrawGUI.Use(inputEvent);
				KeyboardControlUtility.SetKeyboardControl(0, 3);
			}

			inspector.Manager.Select(inspector, InspectorPart.Toolbar, ReasonSelectionChanged.ThisClicked);
			SetSelectedItem(-1, ReasonSelectionChanged.ThisClicked);
			return true;
		}
		
		/// <inheritdoc/>
		public bool OnKeyboardInputGivenWhenNotSelected([NotNull]Event inputEvent, [NotNull]KeyConfigs keys)
		{
			for(int n = visibleItems.Length - 1; n >= 0; n--)
			{
				if(visibleItems[n].OnKeyboardInputGivenWhenNotSelected(inputEvent, keys))
				{
					return true;
				}
			}
			return false;
		}

		/// <inheritdoc/>
		public virtual bool OnKeyboardInputGiven(Event inputEvent, KeyConfigs keys)
		{
			#if DEV_MODE && DEBUG_KEYBOARD_INPUT
			Debug.Log("OnKeyboardInputGiven("+StringUtils.ToString(inputEvent)+ ") with SelectedItem="+StringUtils.ToString(SelectedItem) +", EditingTextField=" + DrawGUI.EditingTextField);
			#endif
			
			if(selectedItemIndex != -1)
			{
				if(items[selectedItemIndex].OnKeyboardInputGiven(inputEvent, keys))
				{
					return true;
				}
			}
			
			if(keys.nextFieldLeft.DetectAndUseInput(inputEvent))
			{
				DrawGUI.Use(inputEvent);
				SelectNextPartLeft(false);
				return true;
			}

			if(keys.nextFieldRight.DetectAndUseInput(inputEvent))
			{
				DrawGUI.Use(inputEvent);
				SelectNextPartRight(false);
				return true;
			}
			
			switch(inputEvent.keyCode)
			{
				case KeyCode.LeftArrow:
					DrawGUI.Use(inputEvent);
					SelectNextPartLeft(false);
					return true;
				case KeyCode.RightArrow:
					DrawGUI.Use(inputEvent);
					SelectNextPartRight(false);
					return true;
				case KeyCode.UpArrow:
					DrawGUI.Use(inputEvent);
					var mainView = inspector.InspectorDrawer.MainView;
					if(inspector != mainView)
					{
						GUI.changed = true;
						var nextUp = mainView.State.drawers.GetNextSelectableDrawerUp(0, null);
						if(nextUp != null)
						{
							inspector.Manager.Select(mainView, InspectorPart.Viewport, nextUp, ReasonSelectionChanged.SelectControlUp);
						}
						return true;
					}
					return false;
				case KeyCode.DownArrow:
					DrawGUI.Use(inputEvent);
					GUI.changed = true;

					var nextDown = inspector.State.drawers.GetNextSelectableDrawerDown(0, null);
					if(nextDown != null)
					{
						inspector.Manager.Select(inspector, InspectorPart.Viewport, nextDown, ReasonSelectionChanged.SelectControlDown);
						return true;
					}

					var splittable = inspector.InspectorDrawer as ISplittableInspectorDrawer;
					if(splittable != null && splittable.ViewIsSplit)
					{
						mainView = splittable.MainView;
						if(inspector == mainView)
						{
							inspector.Manager.Select(splittable.SplitView, InspectorPart.Toolbar, ReasonSelectionChanged.SelectControlDown);
						}
						else
						{
							inspector.Manager.Select(mainView, InspectorPart.Toolbar, ReasonSelectionChanged.SelectControlDown);
						}
					}
					return false;
				case KeyCode.Tab:
					if(inputEvent.modifiers == EventModifiers.None)
					{
						DrawGUI.Use(inputEvent);
						SelectNextPartRight(true);
						return true;
					}
					if(inputEvent.modifiers == EventModifiers.Shift)
					{
						DrawGUI.Use(inputEvent);
						SelectNextPartLeft(true);
						return true;
					}
					return false;
				case KeyCode.Menu:
					var menuItem = GetVisibleItem<IMenuToolbarItem>();
					if(menuItem != null && menuItem.Clickable)
					{
						menuItem.OnClick(inputEvent);
						return true;
					}
					return false;
			}
			return false;
		}

		private void SelectNextPartRight(bool moveToNextControlAfterReachingEnd)
		{
			int visibleItemCount = visibleItems.Length;
			int selectedVisibleItemIndex = SelectedVisibleItemIndex;

			if(selectedVisibleItemIndex == visibleItemCount - 1 && !moveToNextControlAfterReachingEnd)
			{
				return;
			}

			#if DEV_MODE && DEBUG_SELECT_NEXT_PART
			Debug.Log(StringUtils.ToColorizedString("SelectNextPartRight(", moveToNextControlAfterReachingEnd, ") with selectedItemIndex=", selectedItemIndex, ", selectedVisibleItemIndex=", selectedVisibleItemIndex, ", visibleItems=", visibleItems));
			#endif
			
			for(int n = selectedVisibleItemIndex + 1; n < visibleItemCount; n++)
			{
				var item = visibleItems[n];
				if(item.Selectable)
				{
					SetSelectedItem(item, moveToNextControlAfterReachingEnd ? ReasonSelectionChanged.SelectNextControl : ReasonSelectionChanged.SelectControlRight);
					return;
				}
			}

			if(moveToNextControlAfterReachingEnd)
			{
				var drawers = inspector.State.drawers;
				if(drawers.Length > 0)
				{
					var select = drawers.GetNextSelectableDrawerRight(true, null);
					if(select != null)
					{
						inspector.Select(select, ReasonSelectionChanged.SelectNextControl);
						return;
					}
				}

				if(inspector.InspectorDrawer.CanSplitView)
				{
					var splittable = (ISplittableInspectorDrawer)inspector.InspectorDrawer;
					var splitView = splittable.SplitView;
					if(splitView != null)
					{
						splittable.Manager.Select(splitView, InspectorPart.Toolbar, null, ReasonSelectionChanged.SelectNextControl);
						return;
					}
				}
			}
			SelectFirstSelectableItem(moveToNextControlAfterReachingEnd ? ReasonSelectionChanged.SelectNextControl : ReasonSelectionChanged.SelectControlRight);
		}

		private void SelectNextPartLeft(bool moveToNextControlAfterReachingEnd)
		{
			int selectedVisibleItemIndex = SelectedVisibleItemIndex;

			if(selectedVisibleItemIndex == 0 && !moveToNextControlAfterReachingEnd)
			{
				return;
			}

			int visibleItemCount = visibleItems.Length;			
			
			#if DEV_MODE && DEBUG_SELECT_NEXT_PART
			Debug.Log(StringUtils.ToColorizedString("SelectNextPartLeft(", moveToNextControlAfterReachingEnd, ") with selectedItemIndex=", selectedItemIndex, ", selectedVisibleItemIndex=", selectedVisibleItemIndex, ", visibleItems=", visibleItems));
			#endif
			
			for(int n = selectedVisibleItemIndex - 1; n >= 0; n--)
			{
				var item = visibleItems[n];
				if(item.Selectable)
				{
					SetSelectedItem(item, moveToNextControlAfterReachingEnd ? ReasonSelectionChanged.SelectPrevControl : ReasonSelectionChanged.SelectControlLeft);
					return;
				}
			}

			if(moveToNextControlAfterReachingEnd)
			{
				var inspectorDrawer = inspector.InspectorDrawer;
				var mainView = inspectorDrawer.MainView;
				if(mainView != inspector)
				{
					var drawers = mainView.State.drawers;
					if(drawers.Length > 0)
					{
						var select = drawers.GetNextSelectableDrawerLeft(true, null);
						if(select != null)
						{
							inspectorDrawer.Manager.Select(mainView, InspectorPart.Viewport, select, ReasonSelectionChanged.SelectPrevControl);
							return;
						}
						inspectorDrawer.Manager.Select(mainView, InspectorPart.Toolbar, null, ReasonSelectionChanged.SelectPrevControl);
						return;
					}
				}

				var splittable = inspector.InspectorDrawer as ISplittableInspectorDrawer;
				if(splittable != null && splittable.ViewIsSplit)
				{
					var splitView = splittable.SplitView;
					var drawers = splitView.State.drawers;
					if(drawers.Length > 0)
					{
						var select = drawers.GetNextSelectableDrawerLeft(true, null);
						if(select != null)
						{
							inspectorDrawer.Manager.Select(splitView, InspectorPart.Viewport, select, ReasonSelectionChanged.SelectPrevControl);
							return;
						}
						inspectorDrawer.Manager.Select(splitView, InspectorPart.Toolbar, null, ReasonSelectionChanged.SelectPrevControl);
						return;
					}
				}
				else
				{
					#if DEV_MODE
					Debug.Log("Will now try to find instruction to select...");
					#endif
					var drawers = mainView.State.drawers;
					if(drawers.Length > 0)
					{
						var select = drawers.GetNextSelectableDrawerLeft(true, null);
						if(select != null)
						{
							#if DEV_MODE
							Debug.Log("selecting drawers: "+select);
							#endif
							inspectorDrawer.Manager.Select(mainView, InspectorPart.Viewport, select, ReasonSelectionChanged.SelectPrevControl);
							return;
						}
					}
				}
			}
			SelectLastSelectableItem(moveToNextControlAfterReachingEnd ? ReasonSelectionChanged.SelectNextControl : ReasonSelectionChanged.SelectControlRight);
		}

		/// <inheritdoc/>
		public virtual bool OnFindCommandGiven()
		{
			var selected = SelectedItem;
			if(selected != null)
			{
				if(selected.OnFindCommandGiven())
				{
					GUIUtility.ExitGUI(); // Intentionally throws an exception; don't catch this!
					return true;
				}
			}

			var searchBox = GetSelectableItem<ISearchBoxToolbarItem>();
			if(searchBox != null)
			{
				searchBox.OnFindCommandGiven();
				return true;
			}

			return false;
		}

		public virtual bool OnRightClick([NotNull] Event inputEvent)
		{
			#if DEV_MODE
			Debug.Log(GetType().Name+".OnRightClick with mouseoverVisibleItemIndex: "+ mouseoverVisibleItemIndex);
			#endif

			if(mouseoverVisibleItemIndex != -1)
			{
				var item = visibleItems[mouseoverVisibleItemIndex];
				SetSelectedItem(item, ReasonSelectionChanged.ThisClicked);
				return item.OnRightClick(inputEvent);
			}
			return false;
		}

		public bool OnMiddleClick([NotNull] Event inputEvent)
		{
			#if DEV_MODE
			Debug.Log(GetType().Name+ ".OnMiddleClick with mouseoverVisibleItemIndex: " + mouseoverVisibleItemIndex);
			#endif

			if(mouseoverVisibleItemIndex != -1)
			{
				var item = visibleItems[mouseoverVisibleItemIndex];
				return item.OnMiddleClick(inputEvent);
			}
			return false;
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			selectedItemIndex = -1;
			mouseoverVisibleItemIndex = -1;
			updateItemBounds = true;
			visibleItems = ArrayPool<IInspectorToolbarItem>.ZeroSizeArray;
		}

		/// <summary>
		/// Updates visible items array by polling ShouldShow on each item.
		/// 
		/// This is called every time the contents of the inspector view change.
		/// </summary>
		private void UpdateVisibleItems()
		{
			for(int n = 0, count = items.Length; n < count; n++)
			{
				var item = items[n];
				if(item.ShouldShow())
				{
					reusableItemsList.Add(item);
				}
			}

			if(!visibleItems.ContentsMatch(reusableItemsList))
			{
				var selectedItem = SelectedItem;
				for(int n = 0, count = visibleItems.Length; n < count; n++)
				{
					var item = items[n];
					if(item == selectedItem)
					{
						SetSelectedItem(-1, ReasonSelectionChanged.BecameInvisible);
					}
					if(!reusableItemsList.Contains(item))
					{
						item.OnBecameInvisible();
					}
				}
				visibleItems = reusableItemsList.ToArray();
				UpdateItemBounds();
			}
			reusableItemsList.Clear();
		}

		private void UpdateItemBounds()
		{
			updateItemBounds = true;
			inspector.RefreshView();
		}
	}
}