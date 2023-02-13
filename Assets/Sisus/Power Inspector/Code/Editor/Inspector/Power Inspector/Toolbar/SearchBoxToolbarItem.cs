//#define ENABLE_PI_IN_DEFAULT_INSPECTOR
#define SAFE_MODE

//#define DEBUG_MOUSEOVERED_PART
#define DEBUG_FOCUS_FILTER_FIELD
//#define DEBUG_IS_SELECTED

using System;
using UnityEngine;
using Sisus.Attributes;
using JetBrains.Annotations;
using UnityEditor;

namespace Sisus
{
	[ToolbarItemFor(typeof(PowerInspectorToolbar), 40, ToolbarItemAlignment.Right, true)]
	[ToolbarItemFor(typeof(PreferencesToolbar), 10, ToolbarItemAlignment.Right, true)]
	#if ENABLE_PI_IN_DEFAULT_INSPECTOR
	[ToolbarItemFor(typeof(EmbeddedInspectorToolbar), 10, ToolbarItemAlignment.Right, true)]
	#endif
	public class SearchBoxToolbarItem : ToolbarItem, ISearchBoxToolbarItem
	{
		private const float LeftOffset = 5f;
		private const float RightOffset = 5f;
		private const float DropdownButtonWidth = 15f;

		private float ClearButtonWidth;

		private Rect dropdownButtonRect;
		private Rect filterFieldDrawRect;
		private Rect filterFieldClickRect;
		private Rect clearButtonRect;

		[NotNull, NonSerialized]
		private FilterField filterDrawer = new FilterField();
		protected InspectorGraphics graphics;
		
		private string setFilterNextLayoutValue;
		private SearchBoxPart mouseoveredPart;
		
		/// <inheritdoc/>
		public override bool IsSearchBox
		{
			get
			{
				return true;
			}
		}

		/// <inheritdoc/>
		public override float MinWidth
		{
			get
			{
				return 50f;
			}
		}

		/// <inheritdoc/>
		public override float MaxWidth
		{
			get
			{
				return 1000f;
			}
		}

		/// <summary>
		/// Current filter string text in its raw input form.
		/// </summary>
		/// <value> Raw filter text. </value>
		protected string FilterString
		{
			get
			{
				return inspector.State.filter.RawInput;
			}
		}

		/// <inheritdoc/>
		public override string DocumentationPageUrl
		{
			get
			{
				return PowerInspectorDocumentation.GetFeatureUrl("search-box");
			}
		}

		/// <inheritdoc/>
		protected override void Setup()
		{
			filterDrawer = new FilterField(OpenFilteringMethodMenu);

			graphics = inspector.Preferences.graphics;
			inspector.OnFilterChanging += SyncFilterFromInspectorState;

			if(ClearButtonWidth <= 0f)
			{
				#if SAFE_MODE
				if(Event.current == null)
				{
					#if DEV_MODE
					Debug.LogWarning(GetType().Name+".Setup called with Event.current null. This is dangerous because can't reference many GUI related methods.");
					#endif

					DrawGUI.OnNextBeginOnGUI(()=>ClearButtonWidth = InspectorPreferences.Styles.ToolbarCancel.CalcSize(GUIContent.none).x, false);
				}
				else
				#endif
				{
					ClearButtonWidth = InspectorPreferences.Styles.ToolbarCancel.CalcSize(GUIContent.none).x;
				}
			}

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(ClearButtonWidth > 0f, ClearButtonWidth);
			#endif
		}

		private void SyncFilterFromInspectorState(SearchFilter filter)
		{
			string setFilterString = filter.RawInput;
			if(setFilterString.Length == 0)
			{
				filterDrawer.Clear();
			}
			else
			{
				filterDrawer.Text = setFilterString;
			}
		}
		
		/// <inheritdoc/>
		protected override void UpdateDrawPositions(Rect itemPosition)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(ClearButtonWidth > 0f);
			#endif

			filterFieldDrawRect = itemPosition;
			filterFieldDrawRect.x += LeftOffset;
			filterFieldDrawRect.width -= LeftOffset + RightOffset;

			filterFieldDrawRect.y += 2f;
			filterFieldDrawRect.height -= 3f;

			filterFieldClickRect = filterFieldDrawRect;
			filterFieldClickRect.x += DropdownButtonWidth;
			filterFieldClickRect.width -= DropdownButtonWidth;
			filterFieldClickRect.width += 2f; //extends a little bit over the the clear button draw rect.
			
			dropdownButtonRect = filterFieldDrawRect;
			dropdownButtonRect.width = DropdownButtonWidth;

			clearButtonRect = filterFieldDrawRect;
			clearButtonRect.x += filterFieldDrawRect.width;
			clearButtonRect.x -= ClearButtonWidth; //in Unity 2019.3's modern UI the clear button is drawn inside the filter field, while in previous versions it's drawn right after it.
			clearButtonRect.width = ClearButtonWidth;
		}

		/// <inheritdoc/>
		protected override void OnLayout(Rect itemPosition)
		{
			#if DEV_MODE && DEBUG_MOUSEOVERED_PART
			var wasMouseovered = mouseoveredPart;
			#endif

			var mousePos = Event.current.mousePosition;
			if(!itemPosition.Contains(mousePos))
			{
				mouseoveredPart = SearchBoxPart.None;
			}
			else
			{
				// NOTE: It is important to check clearButtonRect before filterFieldClickRect
				// since the clear button is drawn on top of the filter field since Unity 2019.3
				if(clearButtonRect.Contains(mousePos))
				{
					mouseoveredPart = SearchBoxPart.ClearButton;
				}
				else if(dropdownButtonRect.Contains(mousePos))
				{
					mouseoveredPart = SearchBoxPart.Dropdown;
				}
				else if(filterFieldClickRect.Contains(mousePos))
				{
					mouseoveredPart = SearchBoxPart.TextField;
				}				
				else
				{
					mouseoveredPart = SearchBoxPart.None;
				}
			}

			#if DEV_MODE && DEBUG_MOUSEOVERED_PART
			if(wasMouseovered != mouseoveredPart) { Debug.Log(StringUtils.ToColorizedString("mouseoveredPart = ", mouseoveredPart, " (was: ", wasMouseovered, ") with itemPosition=", itemPosition, ", mousePos=", mousePos, ", filterFieldClickRect=", filterFieldClickRect)); }
			#endif
		}

		/// <inheritdoc/>
		public override void DrawBackground(Rect itemPosition, GUIStyle toolbarBackgroundStyle) { }

		/// <inheritdoc/>
		protected override void OnGUI(Rect itemPosition, bool mouseovered)
        {
			#if DEV_MODE && PI_ASSERTATIONS
            Debug.Assert(!inspector.State.WantsSearchBoxDisabled);
			if(IsSelected) { Debug.Assert(DrawGUI.EditingTextField == UnityEditor.EditorGUIUtility.editingTextField, "DrawGUI.EditingTextField (" + DrawGUI.EditingTextField + ") != EditorGUIUtility.editingTextField (" + UnityEditor.EditorGUIUtility.editingTextField + ")"); }
			#endif

            var rawFilterInput = inspector.State.filter.RawInput;
            bool filterChanged;
            bool isSelected = IsSelected;

			#if DEV_MODE && DEBUG_IS_SELECTED
			if(isSelected) { Debug.Log(StringUtils.ToColorizedString("Search Box is isSelected=", isSelected, ", EditingTextField=", DrawGUI.EditingTextField," EditorGUIUtility.editingTextField=", UnityEditor.EditorGUIUtility.editingTextField, ", KeyboardControl=", KeyboardControlUtility.KeyboardControl, ".")); }
			#endif

            if(mouseovered)
            {
                if(mouseoveredPart == SearchBoxPart.ClearButton)
                {
                    if(rawFilterInput.Length > 0)
                    {
                        DrawGUI.Active.SetCursor(MouseCursor.Link);
                    }
                }
                else if(mouseoveredPart == SearchBoxPart.Dropdown)
                {
                    DrawGUI.Active.SetCursor(MouseCursor.Arrow);
                }
            }

            // This fixes a weird issue with text fields sometimes not being editable for some reason. Especially inside custom editors.
            if(!mouseovered && !isSelected)
            {
				GUIUtility.GetControlID(FocusType.Passive);

				if(Event.current.type != EventType.Repaint)
                {
                    return;
                }
                InspectorPreferences.Styles.FilterField.Draw(filterFieldDrawRect, GUIContentPool.Temp(rawFilterInput), false, false, false, false);
				DrawClearButton(itemPosition, rawFilterInput);
				return;
            }

            // Using EditorGUI.TextField had problems where it would lose focus every time the number of displayed Components would change,
            // so had to create a custom solution to gain more control of the field focusing behaviour.
            string setFilter = filterDrawer.Draw(inspector, filterFieldDrawRect, rawFilterInput, isSelected, out filterChanged);

            DrawClearButton(itemPosition, setFilter);

            if(filterChanged)
            {
                setFilterNextLayoutValue = setFilter;
                inspector.OnNextLayout(ApplyUnappliedFilterToInspectorState);

                if(IsSelected)
                {
                    //when we detect a change in the filter field contents
                    //make sure that the filter field still has focus
                    //for some reason the field always keeps losing focus when Components
                    //appear or disappear in the inspector due to the filter changing
                    filterDrawer.RestoreCursorPositions();
                    inspector.OnNextLayout(filterDrawer.RestoreCursorPositions);
                    inspector.OnNextLayout(() => inspector.OnNextLayout(filterDrawer.RestoreCursorPositions));
                }
            }
        }

        private void DrawClearButton(Rect itemPosition, string setFilter)
        {
            if(Event.current.type == EventType.Repaint)
            {
                if(setFilter.Length > 0)
                {
                    GUI.Label(clearButtonRect, string.Empty, InspectorPreferences.Styles.ToolbarCancel);

                    if(!IsSelected)
                    {
                        var clicked = inspector.Manager.MouseDownInfo.MouseDownOverDrawer as IParentDrawer;
                        bool clickingFoldoutArrow = clicked != null && !clicked.DrawInSingleRow && inspector.Manager.MouseDownInfo.MouseButtonIsDown && !inspector.Manager.MouseDownInfo.CursorMovedAfterMouseDown && (clicked.MouseoveredPart == Part.Base || clicked.MouseoveredPart == Part.FoldoutArrow);

                        // If mouseovered parent drawers can't be unfolded because of the filter make this more apparent by drawing a red line below the search box.
                        // Otherwise draw a yellow line below the search box to indicate that it has a filter.
                        var color = clickingFoldoutArrow ? inspector.Preferences.theme.InvalidAction : inspector.Preferences.theme.FilterHighlight;

                        // don't draw any highlighting if user has configured alpha to be zero
                        if(color.a > 0f)
                        {
                            // draw color as fully opaque even if invalid action is semi-transparent
                            color.a = 1f;
                            DrawSelectionRect(itemPosition, color);
                        }
                    }
                }
                else
                {
                    GUI.Label(clearButtonRect, string.Empty, InspectorPreferences.Styles.ToolbarCancelEmpty);
                }
            }
        }

        private void ApplyUnappliedFilterToInspectorState()
		{
			SetFilter(setFilterNextLayoutValue);
			setFilterNextLayoutValue = "";
		}

		/// <inheritdoc/>
		public override void OnSelected(ReasonSelectionChanged reason)
		{
			StartEditing();
		}

		public void SetFilter(string setFilterValue)
		{
			inspector.State.filter.SetFilter(setFilterValue, inspector);
		}
		
		public void StartEditing()
		{
			#if DEV_MODE && DEBUG_FOCUS_FILTER_FIELD
			Debug.Log("SearchBox.StartEditing");
			#endif

			if(!IsSelected)
			{
				inspector.Manager.Select(inspector, InspectorPart.Toolbar, ReasonSelectionChanged.Command);
				toolbar.SetSelectedItem(this, ReasonSelectionChanged.Command);
			}

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(IsSelected);
			inspector.OnNextLayout(()=>Debug.Assert(IsSelected, GetType().Name + " not selected one frame after SearchBox.StartEditing was called."));
			inspector.OnNextLayout(()=>Debug.Assert(DrawGUI.EditingTextField, "EditingTextField false one frame after SearchBox.StartEditing was called."));
			inspector.OnNextLayout(()=>inspector.OnNextLayout(()=>Debug.Assert(IsSelected, "SearchBox not selected one frame after StartEditing was called")));
			inspector.OnNextLayout(()=>inspector.OnNextLayout(()=>Debug.Assert(DrawGUI.EditingTextField, "EditingTextField false two frames after SearchBox.StartEditing was called.")));
			#endif

			DrawGUI.EditingTextField = true;

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(inspector.Manager.SelectedInspectorPart == InspectorPart.Toolbar, "SelectedInspectorPart was "+ InspectorUtility.ActiveManager.SelectedInspectorPart);
			Debug.Assert(inspector.Manager.SelectedInspector == inspector, "Inspector was not the SelectedInspector");
			Debug.Assert(inspector.InspectorDrawer.HasFocus, "InspectorDrawer of " + inspector + " did not have focus");
			Debug.Assert(inspector.FocusedDrawer == null);
			#endif
		}

		private void ClearFilterField(bool focusFieldAfterCleared)
		{
			if(IsSelected && DrawGUI.EditingTextField)
			{
				// deselect the field
				KeyboardControlUtility.KeyboardControl = 0;
				DrawGUI.EditingTextField = false;
			}

			// clear the filter
			setFilterNextLayoutValue = "";
			inspector.OnNextLayout(ApplyUnappliedFilterToInspectorState);

			if(focusFieldAfterCleared && IsSelected)
			{
				inspector.OnNextLayout(()=>inspector.OnNextLayout(StartEditing));
			}
		}

		/// <inheritdoc/>
		public override bool OnKeyboardInputGiven(Event inputEvent, KeyConfigs keys)
		{
			switch(inputEvent.keyCode)
			{
				case KeyCode.F2:
					StartEditing();
					return true;
				case KeyCode.Escape:
					DrawGUI.Use(inputEvent);
					ClearFilterField(false);
					return true;
				case KeyCode.Return:
				case KeyCode.KeypadEnter:
					if(!DrawGUI.EditingTextField)
					{
						StartEditing();
					}
					var drawer = inspector.State.drawers.FirstVisible();
					if(drawer != null)
					{
						var select = drawer.GetNextSelectableDrawerRight(true, null);
						
						if(select == null)
						{
							return false;
						}

						for(int n = 0; n < 20; n++) //arbitrarily chosen depth; not using while(true) just to prevent infinite loops from being possible
						{
							var parent = select as IParentDrawer;
							if(parent == null)
							{
								break;
							}

							if(parent.SelfPassesSearchFilter(inspector.State.filter))
							{
								break;
							}

							var visibleMembers = parent.VisibleMembers;
							if(visibleMembers.Length == 0)
							{
								break;
							}
							
							select = visibleMembers[0];
						}

						select.Select(ReasonSelectionChanged.SelectNextControl);
					}
					return true;
				case KeyCode.LeftArrow:
					if(DrawGUI.EditingTextField && FilterString.Length > 0)
					{
						// Return true to "consume" the click, so that toolbar won't react to the input (moving to next toolbar item).
						// However don't actually use the Event, so that filterDrawer can react to input.
						return true;
					}
					// Return false to let toolbar know that it can react to the input (moving to the next toolbar item).
					return false;
				case KeyCode.RightArrow:
					if(DrawGUI.EditingTextField && FilterString.Length > 0)
					{
						// Return true to "consume" the click, so that toolbar won't react to the input (moving to next toolbar item).
						// However don't actually use the Event, so that filterDrawer can react to input.
						return true;
					}
					// Return false to let toolbar know that it can react to the input (moving to the next toolbar item).
					return false;
			}
			return false;
		}

		/// <inheritdoc/>
		protected override bool OnActivated(Event inputEvent, bool isClick)
		{
			if(isClick)
			{
				switch(mouseoveredPart)
				{
					case SearchBoxPart.None:
						DrawGUI.Use(inputEvent);
						return false;
					case SearchBoxPart.Dropdown:
						DrawGUI.Use(inputEvent);
						OpenFilteringMethodMenu(dropdownButtonRect);
						return true;
					case SearchBoxPart.TextField:
						// if wasn't yet editing text field then start editing
						if(!DrawGUI.EditingTextField)
						{
							DrawGUI.Use(inputEvent);
							StartEditing();
							GUIUtility.ExitGUI();
							return true;
						}
						// else don't consume click event, let filterDrawer handle that
						return false;
					case SearchBoxPart.ClearButton:
						DrawGUI.Use(inputEvent);
						ClearFilterField(false);
						return true;
					default:
						#if DEV_MODE
						throw new IndexOutOfRangeException(mouseoveredPart.ToString());
						#else
						DrawGUI.Use(inputEvent);
						return false;
						#endif
				}
			}

			DrawGUI.Use(inputEvent);

			// If pressed return and wasn't yet editing text field then start editing.
			if(!DrawGUI.EditingTextField)
			{
				StartEditing();
				GUIUtility.ExitGUI();
				return true;
			}

			// new test: If return is pressed while editing the text field, select first item inside inspector view which has passed the filter test.
			var drawer = inspector.State.drawers.FirstVisible();
			if(drawer != null)
			{
				var select = drawer.GetNextSelectableDrawerRight(true, null);

				if(select == null)
				{
					return false;
				}

				for(int n = 0; n < 20; n++) //arbitrarily chosen depth; not using while(true) just to prevent infinite loops from being possible
				{
					var parent = select as IParentDrawer;
					if(parent == null)
					{
						break;
					}

					if(parent.SelfPassesSearchFilter(inspector.State.filter))
					{
						break;
					}

					var visibleMembers = parent.VisibleMembers;
					if(visibleMembers.Length == 0)
					{
						break;
					}
					
					select = visibleMembers[0];
				}

				select.Select(ReasonSelectionChanged.SelectNextControl);
			}
			
			return true;
		}

		/// <inheritdoc/>
		public override bool OnRightClick(Event inputEvent)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(!inspector.IgnoreToolbarMouseInputs(), GetType().Name+ ".HandleOnBeingActivated called with IgnoreToolbarMouseInputs "+StringUtils.True);
			Debug.Assert(inputEvent != null);
			Debug.Assert(inputEvent.type != EventType.Used);
			#endif

			switch(mouseoveredPart)
			{
				case SearchBoxPart.Dropdown:
					DrawGUI.Use(inputEvent);
					OpenFilteringMethodMenu(dropdownButtonRect);
					return true;
				case SearchBoxPart.TextField:
					// don't consume right click event, let filterDrawer handle that
					return false;
				case SearchBoxPart.ClearButton:
					DrawGUI.Use(inputEvent);
					ClearFilterField(false);
					return true;
				default:
					return base.OnRightClick(inputEvent);
			}
		}

		/// <inheritdoc/>
		public override bool OnFindCommandGiven()
		{
			StartEditing();
			return true;
		}

		/// <inheritdoc/>
		protected override void OnCutCommandGiven()
		{
			if(FilterString.Length > 0)
			{
				#if DEV_MODE
				Debug.Log(inspector+"."+ToString()+".Cut");
				#endif
				GUI.changed = true;
				DrawGUI.Use(Event.current);
				Clipboard.Content = FilterString;
				SetFilter("");
			}
		}

		/// <inheritdoc/>
		protected override void OnCopyCommandGiven()
		{
			if(FilterString.Length > 0)
			{
				#if DEV_MODE
				Debug.Log(inspector+"."+ToString()+".Copy");
				#endif
				GUI.changed = true;
				DrawGUI.Use(Event.current);
				Clipboard.Content = FilterString;
				Clipboard.SendCopyToClipboardMessage("Copied{0} Box content", "Search Box");
			}
		}

		/// <inheritdoc/>
		protected override void OnPasteCommandGiven()
		{
			if(FilterString.Length > 0)
			{
				#if DEV_MODE
				Debug.Log(inspector+"."+ToString()+".Paste");
				#endif

				GUI.changed = true;
				DrawGUI.Use(Event.current);
				SetFilter(Clipboard.Content);
			}
		}

		/// <summary>
		/// Determines whether or not this item currently can be activated
		/// by clicking or by keyboard commands.
		/// </summary>
		/// <returns> True if can be activated, false if not. </returns>
		protected virtual bool CanBeActivated()
		{
			return inspector.State.selectionHistory.HasPreviousItems();
		}

		/// <summary> Opens menu to specify search type. </summary>
		/// <param name="position"> The position of the menu. </param>
		private void OpenFilteringMethodMenu(Rect position)
		{
			#if DEV_MODE
			Debug.Log("OpenFilteringMethodMenu");
			#endif

			var menu = Menu.Create();

			var currentFilteringMethod = inspector.State.filter.FilteringMethod;

			menu.Add("Any", SetFilteringMethodToAll, currentFilteringMethod == FilteringMethod.Any);
			menu.AddSeparator();
			menu.Add("Label", SetFilteringMethodToLabel, currentFilteringMethod == FilteringMethod.Label);
			menu.Add("Type", SetFilteringMethodToType, currentFilteringMethod == FilteringMethod.Type);
			menu.Add("Value", SetFilteringMethodToValue, currentFilteringMethod == FilteringMethod.Value);
			menu.AddSeparator();
			menu.Add("Class", SetFilteringMethodToClass, currentFilteringMethod == FilteringMethod.Class);
			menu.Add("Scene", SetFilteringMethodToScene, currentFilteringMethod == FilteringMethod.Scene);
			menu.Add("Asset", SetFilteringMethodToAsset, currentFilteringMethod == FilteringMethod.Asset);
			menu.Add("Window", SetFilteringMethodToWindow, currentFilteringMethod == FilteringMethod.Window);
			menu.Add("Icon", SetFilteringMethodToIcon, currentFilteringMethod == FilteringMethod.Icon);
			menu.AddSeparator();
			menu.Add("Help", OpenDocumentationPage);

			ContextMenuUtility.OpenAt(menu, position, true, inspector, InspectorPart.Toolbar, null, SearchBoxPart.Dropdown, OnFilteringMethodMenuClosed);
		}

		private void OnFilteringMethodMenuClosed(object part)
		{
			StartEditing();
		}
			
		private string GetRawFilterWithoutTypePrefix()
		{
			var filter = inspector.State.filter.RawInput;
			if(filter.Length >= 2 && filter[1] == ':')
			{
				return filter.Substring(2);
			}
			return filter;
		}

		private void SetFilteringMethodToAll()
		{
			SetFilteringPrefix("");
		}

		private void SetFilteringMethodToLabel()
		{
			SetFilteringPrefix("l:");
		}

		private void SetFilteringMethodToType()
		{
			SetFilteringPrefix("t:");
		}

		private void SetFilteringMethodToValue()
		{
			SetFilteringPrefix("v:");
		}

		private void SetFilteringMethodToClass()
		{
			SetFilteringPrefix("c:");
		}

		private void SetFilteringMethodToScene()
		{
			SetFilteringPrefix("s:");
		}

		private void SetFilteringMethodToAsset()
		{
			SetFilteringPrefix("a:");
		}

		private void SetFilteringMethodToWindow()
		{
			SetFilteringPrefix("w:");
		}

		private void SetFilteringMethodToIcon()
		{
			SetFilteringPrefix("i:");
		}

		private void SetFilteringPrefix(string prefix)
		{
			inspector.State.filter.SetFilter(prefix + GetRawFilterWithoutTypePrefix(), inspector);
			filterDrawer.MoveCursorToEnd();
		}

		/// <inheritdoc/>
		public override bool ShouldShow()
		{
			return !inspector.State.WantsSearchBoxDisabled;
		}

		/// <inheritdoc/>
		public override void OnBecameInvisible()
		{
			mouseoveredPart = SearchBoxPart.None;

			if(inspector.State.filter.IsNotEmpty)
			{
				ClearFilterField(false);
			}
		}

		/// <inheritdoc/>
		public override bool OnKeyboardInputGivenWhenNotSelected([NotNull]Event inputEvent, [NotNull]KeyConfigs keys)
		{
			// For some reason ValidateCommand with "Find" does not always get sent, so detecting Ctrl+F also manually.
			if(inputEvent.keyCode == KeyCode.F && inputEvent.modifiers == EventModifiers.Control)
            {
				return OnFindCommandGiven();
            }
			return false;
		}

		/// <inheritdoc/>
		public override void Dispose()
		{
			inspector.OnFilterChanging -= SyncFilterFromInspectorState;

			base.Dispose();
		}
	}
}