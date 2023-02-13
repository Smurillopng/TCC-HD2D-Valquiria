#define SAFE_MODE

//#define DEBUG_SET_INSPECTOR
//#define DEBUG_SET_DRAWER
//#define DEBUG_SET_PART
//#define DEBUG_DISPOSE
//#define DEBUG_ON_SELECTION_EVENT

using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Sisus
{
	public delegate void SelectionChange(IInspector inspector, InspectorPart inspectorPart, IDrawer focusedDrawer);

	public class Selected
	{
		private IDrawer focusedDrawer;
		private IInspector inspector;
		private InspectorPart inspectorPart;
		private int rowIndex;
		private int rowElementCount;
		private IInspector lastSelectedInspector;

		private readonly List<IDrawer> multiSelection = new List<IDrawer>(0);

		private bool isMultiSelection;

		public SelectionChange OnSelectionChanged { get; set; }

		private bool suppressOnSelectionChangedEvents;

		private Dictionary<Type, IInspector> lastSelectedInspectors = new Dictionary<Type, IInspector>(1);
		private Dictionary<Type, IInspectorDrawer> lastSelectedInspectorDrawers = new Dictionary<Type, IInspectorDrawer>(1);
		private IInspectorDrawer lastSelectedEditorWindow;
	
		public bool IsMultiSelection
		{
			get
			{
				return isMultiSelection;
			}
		}

		/// <summary>
		/// Gets the drawer that currently has focus, i.e. the drawer that will receive given keyboard inputs.
		/// </summary>
		/// <value>
		/// The focused drawer.
		/// </value>
		[CanBeNull]
		public IDrawer FocusedDrawer
		{
			get
			{
				#if DEV_MODE && PI_ASSERTATIONS
				if(focusedDrawer != null)
				{
					if(!focusedDrawer.Selectable)
					{
						UnityEngine.Debug.LogWarning("focusedDrawer " + focusedDrawer + " was not Selectable with inactive=" + focusedDrawer.Inactive + ". This is normal if drawer is just now being disposed.");
					}
					UnityEngine.Debug.Assert(focusedDrawer.ShouldShowInInspector, "focusedDrawer " + focusedDrawer + ".ShouldShowInInspector was false!");
				}
				#endif
				return focusedDrawer;
			}
		}

		[NotNull]
		public List<IDrawer> MultiSelection
		{
			get
			{
				return multiSelection;
			}
		}

		[CanBeNull]
		public IInspector Inspector
		{
			get
			{
				return inspector;
			}

			private set
			{
				#if DEV_MODE && DEBUG_SET_INSPECTOR
				if(inspector != value){ UnityEngine.Debug.Log(StringUtils.ToColorizedString("Selected.Inspector = ", value, " (was: ", inspector, ")")); }
				#endif

				inspector = value;

				if(value != null)
				{
					lastSelectedInspector = value;

					lastSelectedInspectors[value.GetType()] = value;
					
					var inspectorDrawer = value.InspectorDrawer;
					var inspectorDrawerType = inspectorDrawer.GetType();
					lastSelectedInspectorDrawers[inspectorDrawerType] = inspectorDrawer;
					
					#if UNITY_EDITOR
					if(Types.EditorWindow.IsAssignableFrom(inspectorDrawerType))
					{
						lastSelectedEditorWindow = inspectorDrawer;
					}
					#endif
				}
			}
		}

		[CanBeNull]
		public IInspector LastSelectedInspector
		{
			get
			{
				return lastSelectedInspector;
			}
		}

		public InspectorPart InspectorPart
		{
			get
			{
				return inspectorPart;
			}

			private set
			{
				#if DEV_MODE && DEBUG_SET_PART
				if(inspectorPart != value){ UnityEngine.Debug.Log(StringUtils.ToColorizedString("Selected.InspectorPart = ", value, " (was: ", inspectorPart, ")")); }
				#endif

				inspectorPart = value;
			}
		}

		/// <summary>
		/// Gets the zero-based index of the focused drawer among all the drawers occupying the same row.
		/// </summary>
		/// <value>
		/// Zero-based index.
		/// </value>
		public int RowIndex
		{
			get
			{
				return rowIndex;
			}
		}

		/// <summary>
		/// Gets the number of drawers occupying the row of the focused drawer.
		/// </summary>
		/// <value>
		/// Count.
		/// </value>
		public int RowElementCount
		{
			get
			{
				return rowElementCount;
			}
		}

		public bool IsSelected([NotNull]IDrawer drawer)
		{
			return isMultiSelection ? IsMultiSelected(drawer) : focusedDrawer == drawer;
		}

		public bool IsMultiSelected([NotNull]IDrawer drawer)
		{
			return multiSelection.Contains(drawer);
		}

		public bool IsFocusedButNotSelected([NotNull]IDrawer drawer)
		{
			return IsMultiSelection && drawer == focusedDrawer && !IsMultiSelected(drawer);
		}

		public void Set([CanBeNull]IInspector setInspector, InspectorPart setPart, [CanBeNull]IDrawer setDrawer, int multiSelect, ReasonSelectionChanged reason)
		{
			bool suppressed = suppressOnSelectionChangedEvents;
			suppressOnSelectionChangedEvents = true;

			SetInspectorAndPart(setInspector, setPart, reason);
			SetFocusedDrawer(setDrawer, multiSelect, reason);

			suppressOnSelectionChangedEvents = suppressed;
			HandleOnSelectionEvent();
		}

		/// <summary>
		/// Sets focused drawer and handles calling OnDeselected (first) and OnSelected (second) for setDrawer
		/// and any previously focused drawer that is no longer selected. Also makes sure that selected inspector
		/// and inspector part are valid.
		/// </summary>
		/// <param name="setDrawer"> The drawer to set as the focused drawer. Can be null if focused drawer should be set to none. </param>
		/// <param name="multiSelect"> If 0 this is not a multi selection, if -1 remove from multi-selection, and if 1 add to multi-selection. </param>
		/// <param name="reason">Reason why focus is changing.</param>
		private void SetFocusedDrawer([CanBeNull]IDrawer setDrawer, int multiSelect, ReasonSelectionChanged reason)
		{
			var focusedDrawerWas = focusedDrawer;

			#if DEV_MODE && PI_ASSERTATIONS
			UnityEngine.Debug.Assert(multiSelect == 0 || setDrawer != null); //multi-selecting doesn't make sense for a null drawer
			UnityEngine.Debug.Assert(multiSelect == 0 || setDrawer.Parent is ICollectionDrawer); //for now multi-selection is only supported for collection members
			UnityEngine.Debug.Assert(multiSelect != -1 || isMultiSelection); //can't remove from multi-selection if there is no multi-selection
			UnityEngine.Debug.Assert(!isMultiSelection || multiSelection.Count >= 2); //multi-selection requires at least two selected drawers
			#endif
			
			#if SAFE_MODE || DEV_MODE
			if(setDrawer != null && !setDrawer.Selectable)
			{
				#if DEV_MODE
				UnityEngine.Debug.LogError("SetFocusedDrawer("+StringUtils.ToString(setDrawer)+").Selectable was "+StringUtils.False);
				#endif
				for(setDrawer = setDrawer.Parent; setDrawer != null && !setDrawer.Selectable; setDrawer = setDrawer.Parent);
			}
			#endif

			#if SAFE_MODE || DEV_MODE
			if(setDrawer != null)
			{
				#if DEV_MODE && PI_ASSERTATIONS
				UnityEngine.Debug.Assert(setDrawer.Selectable, "SetFocusedDrawer("+StringUtils.ToString(setDrawer)+").Selectable was "+StringUtils.False);
				UnityEngine.Debug.Assert(!setDrawer.Inactive, "SetFocusedDrawer("+StringUtils.ToString(setDrawer)+").Inactive was "+StringUtils.True);
				UnityEngine.Debug.Assert(setDrawer.ShouldShowInInspector, "SetFocusedDrawer("+StringUtils.ToString(setDrawer)+").ShowInInspector was "+StringUtils.False);
				UnityEngine.Debug.Assert(inspector != null, "SetFocusedDrawer(" + StringUtils.ToString(setDrawer) + ") inspector was " + StringUtils.Null);
				UnityEngine.Debug.Assert(inspectorPart == InspectorPart.Viewport, "SetFocusedDrawer(" + StringUtils.ToString(setDrawer) + ") inspectorPart was " + inspectorPart);
				UnityEngine.Debug.Assert(!ObjectPicker.IsOpen, "SetFocusedDrawer("+StringUtils.ToString(setDrawer)+") ObjectPicker.IsOpen was "+StringUtils.True);
				//UnityEngine.Debug.Assert(inspector.InspectorDrawer.HasFocus); //this can be true if an item is right cicked
				#endif

				if(inspector == null)
				{
					Inspector = InspectorUtility.ActiveInspector;
				}
				inspectorPart = InspectorPart.Viewport;

				//this can be true if an item was just right-clicked
				if(!inspector.InspectorDrawer.HasFocus)
				{
					#if DEV_MODE
					UnityEngine.Debug.Log("Manually focusing window! Event="+StringUtils.ToString(UnityEngine.Event.current));
					#endif
					inspector.InspectorDrawer.FocusWindow();
				}
			}
			#endif

			#if DEV_MODE && DEBUG_SET_DRAWER
			UnityEngine.Debug.Log(StringUtils.ToColorizedString("Selected.SetFocusedDrawer(", setDrawer, ", multiSelect=", multiSelect, ", reason=", reason, ")"));
			#endif

			switch(multiSelect)
			{
				case 0:
					focusedDrawer = setDrawer;
					ClearMultiSelection(reason, setDrawer);
					if(setDrawer != focusedDrawerWas)
					{
						if(focusedDrawerWas != null && !focusedDrawerWas.Inactive)
						{
							focusedDrawerWas.OnDeselected(reason, setDrawer);
						}

						if(setDrawer != null)
						{
							var textDrawer = setDrawer as ITextFieldDrawer;
							if((textDrawer == null || !textDrawer.CanEditField()) && !(setDrawer is ICustomEditorDrawer))
							{
								DrawGUI.EditingTextField = false;
							}
							setDrawer.OnSelected(reason, focusedDrawerWas, false);
						}
						else
						{
							DrawGUI.EditingTextField = false;
						}

						if(inspector != null)
						{
							// This can possibly help in ensuring that selection rects are drawn in the right place
							// and in ensuring that the inspector feels very responsive.
							inspector.OnNextLayout(inspector.RefreshView);
						}
					}
					break;
				case 1:
					DrawGUI.EditingTextField = false;

					bool wasSelected = IsSelected(setDrawer);
					focusedDrawer = setDrawer;

					multiSelection.AddIfDoesNotContain(focusedDrawerWas);
					multiSelection.AddIfDoesNotContain(setDrawer);

					UpdateMultiSelection();
					
					if(!wasSelected)
					{
						setDrawer.OnSelected(reason, focusedDrawerWas, isMultiSelection);
					}

					if(inspector != null)
					{
						// This can possibly help in ensuring that selection rects are drawn in the right place
						// and in ensuring that the inspector feels very responsive.
						inspector.OnNextLayout(inspector.RefreshView);
					}

					break;
				case -1:
					multiSelection.Remove(setDrawer);
					if(setDrawer == focusedDrawer)
					{
						int selectedCount = multiSelection.Count;
						focusedDrawer = selectedCount == 0 ? setDrawer : multiSelection[selectedCount - 1];
					}
					UpdateMultiSelection();
					
					if(setDrawer != focusedDrawer && !setDrawer.Inactive)
					{
						setDrawer.OnDeselected(reason, focusedDrawer);
					}

					if(!isMultiSelection)
					{
						DrawGUI.EditingTextField = false;
					}
					else if(focusedDrawer != null)
					{
						if(!(setDrawer is ITextFieldDrawer))
						{
							DrawGUI.EditingTextField = false;
						}
					}
					else
					{
						DrawGUI.EditingTextField = false;
					}

					if(inspector != null)
					{
						// This can possibly help in ensuring that selection rects are drawn in the right place
						// and in ensuring that the inspector feels very responsive.
						inspector.OnNextLayout(inspector.RefreshView);
					}

					break;
			}
			
			if(focusedDrawer != null)
			{
				rowIndex = focusedDrawer.GetSelectedRowIndex();
				rowElementCount = setDrawer.GetRowSelectableCount();
			}

			#if DEV_MODE && PI_ASSERTATIONS
			UnityEngine.Debug.Assert(multiSelect != 0 || !isMultiSelection);
			UnityEngine.Debug.Assert(!isMultiSelection || multiSelection.Count >= 2);
			#endif
		}

		private void HandleOnSelectionEvent()
		{
			#if DEV_MODE && DEBUG_ON_SELECTION_EVENT
			UnityEngine.Debug.Log("HandleOnSelectionEvent with suppressOnSelectionChangedEvents="+suppressOnSelectionChangedEvents+", OnSelectionChanged="+StringUtils.ToString(OnSelectionChanged));
			#endif

			if(!suppressOnSelectionChangedEvents && OnSelectionChanged != null)
			{
				OnSelectionChanged(inspector, inspectorPart, focusedDrawer);
			}
		}

		public void OnInspectorDrawerGainedFocus(IInspectorDrawer inspectorDrawer)
		{
			if(focusedDrawer != null)
			{
				if(inspector.InspectorDrawer == inspectorDrawer)
				{
					focusedDrawer.OnInspectorGainedFocusWhileSelected();

					if(isMultiSelection)
					{
						for(int n = multiSelection.Count - 1; n >= 0; n--)
						{
							var multiSelected = multiSelection[n];
							if(multiSelected != focusedDrawer)
							{
								multiSelected.OnInspectorGainedFocusWhileSelected();
							}
						}
					}
				}
			}
		}

		public void OnInspectorDrawerLostFocus(IInspectorDrawer inspectorDrawer)
		{
			if(focusedDrawer == null)
			{
				return;
			}

			if(inspector.InspectorDrawer != inspectorDrawer)
			{
				return;
			}

			focusedDrawer.OnInspectorLostFocusWhileSelected();

			if(!isMultiSelection)
			{
				return;
			}

			for(int n = multiSelection.Count - 1; n >= 0; n--)
			{
				var multiSelected = multiSelection[n];
				if(multiSelected != focusedDrawer)
				{
					multiSelected.OnInspectorLostFocusWhileSelected();
				}
			}
		}

		private void UpdateMultiSelection()
		{
			switch(multiSelection.Count)
			{
				case 0:
					isMultiSelection = false;
					break;
				case 1:
					focusedDrawer = multiSelection[0];
					multiSelection.Clear();
					isMultiSelection = false;
					break;
				default:
					isMultiSelection = true;
					KeyboardControlUtility.KeyboardControl = 0;
					DrawGUI.EditingTextField = false;
					break;
			}
		}

		public void SetInspectorAndPart(IInspector setInspector, InspectorPart setPart, ReasonSelectionChanged reason)
		{
			bool suppressed = suppressOnSelectionChangedEvents;
			suppressOnSelectionChangedEvents = true;

			var fromInspector = inspector;
			var fromPart = inspectorPart;
			
			bool inspectorChanged = fromInspector != setInspector;
			bool partChanged = inspectorChanged || fromPart != setPart;

			#if DEV_MODE && DEBUG_SET_INSPECTOR
			UnityEngine.Debug.Log(StringUtils.ToColorizedString("Selected.SetInspectorAndPart(", setInspector, ", part=", setPart, ", reason=", reason, ") with inspectorChanged=", inspectorChanged, ", partChanged=", partChanged));
			#endif

			if(partChanged)
			{
				if(setPart != InspectorPart.Viewport)
				{
					ClearSelectedDrawers(reason, null);
				}

				InspectorPart = setPart;
				SetInspectorOnly(setInspector);

				if(!inspectorChanged)
				{
					if(setInspector != null)
					{
						setInspector.OnSelectedPartChanged(fromPart, setPart, reason);
					}
				}
				else
				{
					if(fromInspector != null)
					{
						fromInspector.OnSelectedPartChanged(fromPart, InspectorPart.None, reason);
					}

					if(setInspector != null)
					{
						setInspector.OnSelectedPartChanged(InspectorPart.None, setPart, reason);
					}
				}
			}

			suppressOnSelectionChangedEvents = suppressed;
			HandleOnSelectionEvent();
		}

		/// <summary> Gets instance of type that implements IInspector that was last selected (if any). </summary>
		/// <param name="inspectorType"> Type of the inspector. This cannot be null. </param>
		/// <returns> The last selected instance, or null if no inspector of type exists that has been previously selected. </returns>
		[CanBeNull]
		public IInspector GetLastSelectedInspector([NotNull]Type inspectorType)
		{
			IInspector result;
			if(lastSelectedInspectors.TryGetValue(inspectorType, out result))
			{
				return result;
			}

			// Since last selection data is lost every time the assembly is reloaded as fallback just return some instance.
			var inspectorManager = InspectorUtility.ActiveManager != null ? InspectorUtility.ActiveManager : InspectorManager.Instance();
			var inspectors = inspectorManager.ActiveInstances;
			for(int n = inspectors.Count - 1; n >= 0; n--)
			{
				var inspector = inspectors[n];
				if(inspector.GetType() == inspectorType)
				{
					return inspector;
				}
			}

			return null;
		}

		/// <summary> Gets instance of type that implements IInspectorDrawer that was last selected (if any). </summary>
		/// <param name="inspectorDrawerType"> Type of the inspector. This cannot be null. </param>
		/// <returns> The last selected instance, or null if no inspector of type exists that has been previously selected. </returns>
		[CanBeNull]
		public IInspectorDrawer GetLastSelectedInspectorDrawer([NotNull]Type inspectorDrawerType)
		{
			IInspectorDrawer result;
			if(lastSelectedInspectorDrawers.TryGetValue(inspectorDrawerType, out result))
			{
				return result;
			}

			// Since last selection data is lost every time the assembly is reloaded as fallback just return some instance.
			var inspectorManager = InspectorUtility.ActiveManager != null ? InspectorUtility.ActiveManager : InspectorManager.Instance();
			var inspectors = inspectorManager.ActiveInstances;
			for(int n = inspectors.Count - 1; n >= 0; n--)
			{
				var inspectorDrawer = inspectors[n].InspectorDrawer;
				if(inspectorDrawer != null && inspectorDrawer.GetType() == inspectorDrawerType)
				{
					return inspectorDrawer;
				}
			}

			return null;
		}

		#if UNITY_EDITOR
		/// <summary> Gets instance of EditorWindow that is currently or last selected (if any). </summary>
		/// <returns> The last selected editor window, or null if EditorWindow has been previously selected. </returns>
		[CanBeNull]
		public UnityEditor.EditorWindow GetLastSelectedEditorWindow()
		{
			if(UnityEditor.EditorWindow.focusedWindow != null)
			{
				return UnityEditor.EditorWindow.focusedWindow;
			}

			if(lastSelectedEditorWindow != null)
			{
				return lastSelectedEditorWindow as UnityEditor.EditorWindow;
			}

			// Since last selection data is lost every time the assembly is reloaded as fallback just return some instance.
			var inspectorManager = InspectorUtility.ActiveManager != null ? InspectorUtility.ActiveManager : InspectorManager.Instance();
			var inspectors = inspectorManager.ActiveInstances;
			for(int n = inspectors.Count - 1; n >= 0; n--)
			{
				var editorWindow = inspectors[n].InspectorDrawer as UnityEditor.EditorWindow;
				if(editorWindow != null)
				{
					return editorWindow;
				}
			}

			return null;
		}
		#endif

		public void OnDisposing([NotNull]IInspector disposing)
		{
			if(lastSelectedInspector == disposing)
			{
				lastSelectedInspector = null;
			}

			if(inspector == disposing)
			{
				inspector = null;
				focusedDrawer = null;
				inspectorPart = InspectorPart.None;
				rowIndex = 0;
				rowElementCount = 0;
			}

			var type = disposing.GetType();
			var lastSelectedInspectorOfSameType = GetLastSelectedInspector(type);
			if(lastSelectedInspectorOfSameType == disposing)
			{
				lastSelectedInspectors.Remove(type);
			}

			var inspectorDrawer = disposing.InspectorDrawer;
			
			if(inspectorDrawer != null)
			{
				if(inspectorDrawer.NowClosing)
				{
					#if DEV_MODE && DEBUG_DISPOSE
					UnityEngine.Debug.Log("OnDisposing("+disposing+"): NowClosing was "+StringUtils.True);
					#endif

					var inspectorDrawerType = inspectorDrawer.GetType();
					var lastSelectedInspectorDrawer = GetLastSelectedInspectorDrawer(inspectorDrawerType);
					if(lastSelectedInspectorDrawer == inspectorDrawer)
					{
						#if DEV_MODE && DEBUG_DISPOSE
						UnityEngine.Debug.Log("OnDisposing("+disposing+"): removed "+inspectorDrawer.GetType().Name+" from lastSelectedInspectorDrawers");
						#endif
						lastSelectedInspectorDrawers.Remove(inspectorDrawerType);
					}

					#if UNITY_EDITOR
					if(lastSelectedEditorWindow == inspectorDrawer)
					{
						#if DEV_MODE && DEBUG_DISPOSE
						UnityEngine.Debug.Log("OnDisposing("+disposing+"): setting lastSelectedEditorWindow to null");
						#endif
						lastSelectedEditorWindow = null;
					}
					#endif
				}
				#if DEV_MODE && DEBUG_DISPOSE
				else { UnityEngine.Debug.Log("OnDisposing("+disposing+"): NowClosing was "+StringUtils.False); }
				#endif
			}
			else
			{
				#if DEV_MODE && DEBUG_DISPOSE
				UnityEngine.Debug.Log("OnDisposing("+disposing+"): inspectorDrawer was null!");
				#endif

				CleanUpLastSelectedInspectorDrawers();
			}
		}

		private void CleanUpLastSelectedInspectorDrawers()
		{
			foreach(var inspectorDrawer in lastSelectedInspectorDrawers)
			{
				var testForNull = inspectorDrawer.Value;
				if(testForNull is UnityEngine.Object)
				{
					if((UnityEngine.Object)testForNull != null)
					{
						#if DEV_MODE
						UnityEngine.Debug.Log("Object not null...");
						#endif
						continue;
					}
				}
				else if(testForNull != null)
				{
					#if DEV_MODE
					UnityEngine.Debug.Log("Non-Object not null...");
					#endif
					continue;
				}
				
				#if DEV_MODE
				UnityEngine.Debug.Log("Removing null!");
				#endif


				lastSelectedInspectorDrawers.Remove(inspectorDrawer.Key);
				CleanUpLastSelectedInspectorDrawers(); //repeat process until all nulls have been removed
				return;
			}
		}

		public void Clear(ReasonSelectionChanged reason, [CanBeNull]IDrawer losingFocusTo)
		{
			bool suppressed = suppressOnSelectionChangedEvents;
			suppressOnSelectionChangedEvents = true;

			ClearSelectedDrawers(reason, losingFocusTo);
			Inspector = null;

			suppressOnSelectionChangedEvents = suppressed;
			HandleOnSelectionEvent();
		}

		private void ClearSelectedDrawers(ReasonSelectionChanged reason, [CanBeNull]IDrawer losingFocusTo)
		{
			if(isMultiSelection)
			{
				ClearMultiSelection(reason, losingFocusTo);
			}
			else
			{
				ClearFocusedDrawer(reason, losingFocusTo);
			}
		}

		/// <summary> Calls OnDeselected in focused drawer, removes is from multi-selection if it's there, sets it to null. </summary>
		/// <param name="reason"> The reason why drawer is losing focus. </param>
		/// <param name="losingFocusTo"> The drawer to which focus is moving to. This may be null. </param>
		private void ClearFocusedDrawer(ReasonSelectionChanged reason, [CanBeNull]IDrawer losingFocusTo)
		{
			if(focusedDrawer != null)
			{
				var focusedDrawerWas = focusedDrawer;
				focusedDrawer = null;
				if(isMultiSelection && multiSelection.Remove(focusedDrawer))
				{
					if(multiSelection.Count == 0)
					{
						isMultiSelection = false;
					}
				}

				if(!focusedDrawerWas.Inactive)
				{
					focusedDrawerWas.OnDeselected(reason, losingFocusTo);
				}
			}
		}

		/// <summary> Clears all multi-selected drawers and calls OnDeselected on all of them EXCEPT for the focused drawer. </summary>
		/// <param name="reason"> The reason why selection is cleared. </param>
		/// <param name="losingFocusTo"> The drawer to which focus is moving to. This may be null. </param>
		private void ClearMultiSelection(ReasonSelectionChanged reason, [CanBeNull]IDrawer losingFocusTo)
		{
			if(isMultiSelection)
			{
				// Broadcasting OnDeselected to focused drawer is handled by ClearFocusedDrawer
				// so we remove it from the list before the for loop.
				if(focusedDrawer != null)
				{
					multiSelection.Remove(focusedDrawer);
				}

				for(int n = multiSelection.Count - 1; n >= 0; n--)
				{
					var deselect = multiSelection[n];
					if(deselect != null && !deselect.Inactive)
					{
						deselect.OnDeselected(reason, losingFocusTo);
					}
				}
				multiSelection.Clear();
				isMultiSelection = false;
			}
		}
		
		private void SetInspectorOnly(IInspector setInspector)
		{
			#if DEV_MODE && DEBUG_SET_INSPECTOR
			UnityEngine.Debug.Log(StringUtils.ToColorizedString("Selected.SetInspectorOnly(", setInspector, ")"));
			#endif

			Inspector = setInspector;

			#if UNITY_EDITOR
			//new test
			if(inspector != null && !inspector.InspectorDrawer.HasFocus)
			{
				inspector.InspectorDrawer.FocusWindow();
			}
			#endif
		}
	}
}