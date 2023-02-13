//#define DEBUG_MOUSE_UP
//#define DEBUG_DRAG_N_DROP
//#define DEBUG_SET_MOUSEOVERED_INSPECTOR
//#define DEBUG_ON_MOUSE_DOWN
//#define DEBUG_EXECUTE_COMMAND
#define DEBUG_GET_PREFERENCES

using JetBrains.Annotations;
using UnityEngine;
using UnityEditor;

namespace Sisus
{
	/// <summary>
	/// Keeps track of active InspectorManager and InspectorPreferences.
	/// Has method BeginInspectorDrawer which should be called at the
	/// beginning of every OnGUI call for InspectorDrawers, and methods
	/// BeginInspector and EndInspector which should be called at the
	/// beginning and end of every OnGUI call for Inspectors.
	/// </summary>
	public static class InspectorUtility
	{
		private static Vector2 inspectorBeginScreenPoint;

		/// <summary> Delegate for when a command is being executed. </summary>
		/// <param name="commandRecipient"> The inspector which is currently selected and thus the recipient for the command. </param>
		/// <param name="commandName"> Name of the command. </param>
		public delegate void ExecuteCommandCallback(IInspector commandRecipient, string commandName);

		/// <summary> Delegate for when the execution of the OnGUI function for an inspector has reached a specific point. </summary>
		/// <param name="inspector"> The inspector whose OnGUI event has reached a specific point. </param>
		public delegate void OnInspectorGUICallback(IInspector inspector);

		public static OnInspectorGUICallback OnInspectorGUIBegin;
		public static OnInspectorGUICallback OnInspectorGUIEnd;

		public static ExecuteCommandCallback OnExecuteCommand;

		private static IInspectorManager activeManager;
		private static IInspectorDrawer activeInspectorDrawer;
		private static InspectorPreferences activePreferences;

		private static string activeTooltip = "";
		private static Rect activeTooltipPosition;
		private static IInspector activeTooltipInspector;
		
		public static Vector2 InspectorBeginScreenPoint
		{
			get
			{
				return inspectorBeginScreenPoint;
			}
		}

		/// <summary>
		/// Manager of the active inspector.
		/// </summary>
		/// <value>
		/// The active inspector manager.
		/// </value>
		public static IInspectorManager ActiveManager
		{
			get
			{
				return activeManager;
			}

			set
			{
				activeManager = value;
			}
		}

		/// <summary>
		/// Inspector drawer which is now "active" or the drawer of inspector that was last active.
		/// An inspector can be set active when it is being drawn, edited or interacted with in some way.
		/// 
		/// Note that this is not cleared at the end of each OnGUI call, so it can also return the last active inspector drawer,
		/// even if it's not currently being interacted with.
		/// </summary>
		/// <value>
		/// The current or last active inspector drawer.
		/// </value>
		public static IInspectorDrawer ActiveInspectorDrawer
		{
			get
			{
				return activeInspectorDrawer;
			}
		}

		/// <summary>
		/// Inspector drawer whose OnGUI function is in progress right now.
		/// </summary>
		/// <value>
		/// The current or last active inspector drawer.
		/// </value>
		public static IInspectorDrawer NowDrawingInspectorDrawer
		{
			get
			{
				return NowDrawingInspectorPart != InspectorPart.None ? activeInspectorDrawer : null;
			}
		}

		/// <summary>
		/// Inspector which is now "active", i.e. currently being drawn, edited or interacted with in some way.
		/// Note that this is not cleared at the end of each OnGUI call, so it can also return the last active inspector,
		/// even if it's not currently being interacted with.
		/// </summary>
		/// <value>
		/// The current or last active inspector.
		/// </value>
		public static IInspector ActiveInspector
		{
			get
			{
				return activeManager == null ? null : activeManager.ActiveInspector;
			}
		}

		public static IInspector MouseoveredInspector
		{
			get
			{
				return activeManager == null ? null : activeManager.MouseoveredInspector;
			}
		}

		public static IInspector MouseoveredOrActiveInspector
		{
			get
			{
				if(activeManager == null)
				{
					return null;
				}
				var mouseovered = activeManager.MouseoveredInspector;
				if(mouseovered != null)
				{
					return mouseovered;
				}
				return activeManager.ActiveInspector;
			}
		}

		/// <summary>
		/// Returns preferences for currently active inspector.
		/// If no inspector is active, returns preferences for currently selected inspector.
		/// If no inspector is selected, returns setings for currently active
		/// inspector manager's default view.
		/// If no inspector manager is active, returns default preferences asset.
		/// </summary>
		/// <value>
		/// Set or get the preferences asset for the current context
		/// </value>
		public static InspectorPreferences Preferences
		{
			get
			{
				if(activePreferences == null)
				{
					if(activeManager != null)
					{
						var inspector = activeManager.ActiveSelectedOrDefaultInspector();
						if(inspector != null)
						{
							activePreferences = inspector.Preferences;
							if(activePreferences != null)
							{
								return activePreferences;
							}
						}
					}
					#if DEV_MODE && DEBUG_GET_PREFERENCES
					Debug.Log("Preferences was called with activePreferences and activeManager null - returning default preferences asset");
					#endif

					#if DEV_MODE
					if(Event.current == null) { Debug.LogWarning("InspectorUtility.Preferences called with Event.current null. Can't run Setup on preferences when it's loaded."); }
					#endif

					activePreferences = InspectorPreferences.GetDefaultPreferences();
				}
				return activePreferences;
			}

			set
			{
				activePreferences = value;
			}
		}

		public static float LastInputTime
		{
			get
			{
				return ActiveInspector.LastInputTime;
			}
		}

		public static bool IsSafeToChangeInspectorContents
		{
			get
			{
				return NowDrawingInspectorPart == InspectorPart.None && Event.current != null && Event.current.type != EventType.Repaint;
			}
		}

		/// <summary> Gets the part of the active Inspector that is currently being drawn during an OnGUI event. </summary>
		/// <value> During the OnGUI event of an inspector returns the part of the inspector being drawn, otherwise returns None. </value>
		public static InspectorPart NowDrawingInspectorPart
		{
			get
			{
				var inspector = ActiveInspector;
				return inspector != null ? inspector.NowDrawingPart : InspectorPart.None;
			}
		}

		/// <summary>
		/// This should be called at the beginning of OnGUI of every instance of classes that implement IInspectorDrawer.
		/// 
		/// SEE ALSO: DrawGUI.BeginOnGUI and InspectorUtility.BeginInspector.
		/// </summary>
		/// <param name="inspectorDrawer"> The inspector drawer instance. </param>
		/// <param name="splittable"> The inspector drawer if it implements ISplittableInspectorDrawer, otherwise null. </param>
		public static void BeginInspectorDrawer([NotNull]IInspectorDrawer inspectorDrawer, [CanBeNull]ISplittableInspectorDrawer splittable)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(inspectorDrawer != null);
			#endif

			var manager = inspectorDrawer.Manager;
			activeInspectorDrawer = inspectorDrawer;
			ActiveManager = manager;
			activeManager.ActiveInspector = inspectorDrawer.MainView;
			
			if(!inspectorDrawer.SetupDone)
			{
				return;
			}

			var e = Event.current;

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(e != null);
			#endif

			var mouseDownInfo = manager.MouseDownInfo;

			// Events like MouseUp, DragUpdated and DragPerformed are normally ignored when the cursor is outside EditorWindow bounds
			// but when a field is being dragged we still need those events (especially important for the MouseUp event!)
			var type = mouseDownInfo.GetEventTypeForMouseUpDetection(e);

			if(mouseDownInfo.NowReordering)
			{
				var reordering = mouseDownInfo.Reordering;
				
				var dropTarget = reordering.MouseoveredDropTarget;
				
				// When reordering and cursor is not above a valid drop target, set DragAndDropVisualMode to rejected,
				// unless dragging Object references outside window bounds, where want to allow dragging into other
				// EditorWindows.
				// UPDATE: This broke drag n drop e.g. from Component header to object reference field inside a custom editor.
				//if(dropTarget.Parent == null && ((DrawGUI.Active.DragAndDropObjectReferences.Length == 0 || activeManager.MouseoveredInspector != null)))
				if(dropTarget.Parent == null && DrawGUI.Active.DragAndDropObjectReferences.Length == 0)
				{
					#if DEV_MODE
					Debug.LogWarning("Reordering Rejected because dropTarget.Parent=null && DrawGUI.Active.DragAndDropObjectReferences="+StringUtils.ToString(DrawGUI.Active.DragAndDropObjectReferences)+", activeManager.MouseoveredInspector="+StringUtils.ToString(activeManager.MouseoveredInspector));
					#endif

					// Update: only do this once cursor has moved, otherwise it can look strange during simple click events
					if(mouseDownInfo.CursorMovedAfterMouseDown)
					{
						DrawGUI.Active.DragAndDropVisualMode = DragAndDropVisualMode.Rejected;
					}
				}
				DrawGUI.Active.AddCursorRect(new Rect(0f, 0f, 100000f, 100000f), MouseCursor.MoveArrow);
			}
			else if(mouseDownInfo.NowDraggingPrefix)
			{
				DrawGUI.Active.SetCursor(MouseCursor.SlideArrow);
			}

			switch(type)
			{
				case EventType.Repaint:
					if(inspectorDrawer.MainView.RequiresConstantRepaint() || (splittable != null && splittable.ViewIsSplit && splittable.SplitView.RequiresConstantRepaint()))
					{
						inspectorDrawer.Repaint();
					}
					break;
				case EventType.Layout:
					manager.OnLayout();
					UpdateDimensions(inspectorDrawer, splittable);
					break;
				case EventType.ValidateCommand:
					inspectorDrawer.OnValidateCommand(e);
					break;
				case EventType.ExecuteCommand:
					if(OnExecuteCommand != null)
					{
						if(inspectorDrawer == manager.FirstInspectorDrawer)
						{
							#if DEV_MODE && DEBUG_EXECUTE_COMMAND
							Debug.Log("ExecuteCommand("+e.commandName+")");
							#endif
							OnExecuteCommand(manager.LastSelectedActiveOrDefaultInspector(), e.commandName);
						}
						#if DEV_MODE && DEBUG_EXECUTE_COMMAND
						else { Debug.Log("ExecuteCommand("+e.commandName+") not sent with e="+StringUtils.ToString(e)+ ", OnExecuteCommand="+StringUtils.ToString(OnExecuteCommand)); }
						#endif
					}
					inspectorDrawer.OnExecuteCommand(e);
					break;
				case EventType.MouseUp:
				case EventType.DragPerform:
					#if DEV_MODE && (DEBUG_MOUSE_UP || DEBUG_DRAG_N_DROP)
					Debug.Log(type + " with IsUnityObjectDrag="+StringUtils.ToColorizedString(DrawGUI.IsUnityObjectDrag)+", mouseDownInfo.Inspector=" + mouseDownInfo.Inspector + ", activeInspectorDrawer.MouseIsOver=" + (inspectorDrawer == null ? StringUtils.Null : StringUtils.ToColorizedString(inspectorDrawer.MouseIsOver)));
					#endif
					inspectorDrawer.Repaint();
					mouseDownInfo.OnMouseUp(manager);
					break;
				case EventType.DragExited:
					//IMPORTANT NOTE: DragExited gets called when during DragNDrop cursor leaves EditorWindow bounds!
					#if DEV_MODE && UNITY_EDITOR
					Debug.Log(type+" with LastInputEvent().type="+DrawGUI.LastInputEvent().type+", IsUnityObjectDrag="+StringUtils.ToColorizedString(DrawGUI.IsUnityObjectDrag)+", EditorWindow.focusedWindow="+UnityEditor.EditorWindow.focusedWindow+", activeManager.MouseoveredInspector="+activeManager.MouseoveredInspector+", mouseoveredDrawer.position="+(activeManager.MouseoveredInspector == null || activeManager.MouseoveredInspector.InspectorDrawer == null ? StringUtils.Null : activeManager.MouseoveredInspector.InspectorDrawer.position.ToString())+", MouseDownInfo.Inspector="+activeManager.MouseDownInfo.Inspector+", Drawer.MouseIsOver="+(activeInspectorDrawer == null ? StringUtils.Null : StringUtils.ToColorizedString(activeInspectorDrawer.MouseIsOver)));
					#endif
					
					if(mouseDownInfo.IsDragExitedReallyMouseLeaveWindowEvent())
					{
						#if DEV_MODE
						Debug.LogWarning("Ignoring DragExited call because IsDragExitedReallyMouseLeaveWindowEvent="+StringUtils.True+" - cursor probably just left EditorWindow bounds.");
						#endif
						break;
					}
					inspectorDrawer.Repaint();
					mouseDownInfo.OnMouseUp(manager);
					break;
				case EventType.MouseDown:
					mouseDownInfo.OnMouseDown();
					#if DEV_MODE && DEBUG_ON_MOUSE_DOWN
					Debug.Log("MouseDown with button="+e.button+", keyCode="+e.keyCode+ ", MouseoveredSelectable=" + StringUtils.ToString(ActiveManager.MouseoveredSelectable));
					#endif
					break;
				case EventType.KeyDown:
					inspectorDrawer.OnKeyDown(e);
					break;
				case EventType.KeyUp:
					inspectorDrawer.Manager.OnKeyUp(e);
					break;
			}
		}

		internal static void UpdateDimensions(IInspectorDrawer inspectorDrawer, ISplittableInspectorDrawer splittable)
		{
			var inspectorWindowRect = inspectorDrawer.position;
			inspectorWindowRect.x = 0f;
			inspectorWindowRect.y = 0f;

			var inspector = inspectorDrawer.MainView;
			inspectorDrawer.Manager.ActiveInspector = inspector;

			if(splittable != null && splittable.ViewIsSplit)
			{
				inspectorWindowRect.height *= 0.5f;
				
				inspector.State.UpdateDimensions(inspectorWindowRect, inspector.ToolbarHeight, inspector.PreviewAreaHeight);

				inspector = splittable.SplitView;
				if(inspector == null)
				{
					#if DEV_MODE
					Debug.LogWarning("Splittable.ViewIsSplit was true but SplitView was null. Fixing now!");
					#endif
					splittable.SetSplitView(true);
					inspector = splittable.SplitView;
					#if DEV_MODE && PI_ASSERTATIONS
					Debug.Assert(inspector != null, inspectorDrawer+" SplitView still null after calling SetSplitView(true)!");
					#endif
				}
				inspectorDrawer.Manager.ActiveInspector = inspector;
				inspector.State.UpdateDimensions(inspectorWindowRect, inspector.ToolbarHeight, inspector.PreviewAreaHeight);
			}
			else
			{
				inspector.State.UpdateDimensions(inspectorWindowRect, inspector.ToolbarHeight, inspector.PreviewAreaHeight);
			}
		}


		/// <summary>
		/// This should be called at the beginning of OnGUI of every instance of classes that implement IInspector.
		/// 
		/// Handles things like setting the ActiveInspector, updating mouseovered inspector part, and broadcasting various events.
		/// 
		/// SEE ALSO: DrawGUI.BeginOnGUI and InspectorUtility.BeginInspectorDrawer.
		/// </summary>
		public static void BeginInspector([NotNull]IInspector inspector, ref bool anyInspectorPartMouseovered)
		{
			inspectorBeginScreenPoint = GUIUtility.GUIToScreenPoint(Vector2.zero);
			GUISpace.Current = Space.Window;

			activeInspectorDrawer = inspector.InspectorDrawer;
			ActiveManager = activeInspectorDrawer.Manager;

			#if DEV_MODE
			Debug.Assert(activeManager.ActiveInstances.Contains(inspector));
			#endif

			var e = Event.current;
			
			var setMouseoveredPart = inspector.GetMouseoveredPartUpdated(ref anyInspectorPartMouseovered);
			if(setMouseoveredPart != InspectorPart.None)
			{
				inspector.InspectorDrawer.Manager.SetMouseoveredInspector(inspector, setMouseoveredPart);
			}
			else if(activeManager.MouseoveredInspector == inspector)
			{
				#if DEV_MODE && DEBUG_SET_MOUSEOVERED_INSPECTOR
				Debug.Log(StringUtils.ToColorizedString("SetMouseoveredInspector(", StringUtils.Null, ") with anyInspectorPartMouseovered=", anyInspectorPartMouseovered, ", IgnoreAllMouseInputs=", activeManager.IgnoreAllMouseInputs, ", Cursor.CanRequestLocalPosition=", Cursor.CanRequestLocalPosition, ", Cursor.LocalPosition=", Cursor.LocalPosition, ", windowRect=", inspector.State.WindowRect));
				#endif

				activeManager.SetMouseoveredInspector(null, InspectorPart.None);
			}
			
			activeManager.ActiveInspector = inspector;
			
			var state = inspector.State;

			inspector.NowDrawingPart = InspectorPart.Other;

			if(OnInspectorGUIBegin != null)
			{
				OnInspectorGUIBegin(inspector);
			}

			var mouseDownInfo = activeManager.MouseDownInfo;

			// Events like MouseUp, DragUpdated and DragPerformed are normally ignored when the cursor is outside EditorWindow bounds
			// but when a field is being dragged we still need those events (especially important for the MouseUp event!)
			var type = mouseDownInfo.GetEventTypeForMouseUpDetection(e);

			switch(type)
			{
				case EventType.MouseUp:
				case EventType.DragPerform:
				case EventType.DragExited:
					#if DEV_MODE && DEBUG_DRAG_N_DROP
					Debug.Log(StringUtils.ToColorizedString("BeginInspector: "+e.type+ " with ObjectReferences=", DrawGUI.Active.DragAndDropObjectReferences, ", MouseDownEventWasUsed=", mouseDownInfo.MouseDownEventWasUsed, ", IgnoreAllMouseInputs=", activeManager.IgnoreAllMouseInputs, ", mouseoveredPart=", setMouseoveredPart, ", button=", Event.current.button+ ", mouseDownInfo.Inspector=", mouseDownInfo.Inspector, ", MouseDownOverControl=", mouseDownInfo.MouseDownOverDrawer));
					#endif

					if(e.type == EventType.DragExited && mouseDownInfo.IsDragExitedReallyMouseLeaveWindowEvent())
					{
						#if DEV_MODE
						Debug.LogWarning("Ignoring DragExited call because IsDragExitedReallyMouseLeaveWindowEvent="+StringUtils.True+" - cursor probably just left EditorWindow bounds.");
						#endif
						break;
					}

					var mouseoveredInspector = activeManager.MouseoveredInspector;

					if(!activeManager.IgnoreAllMouseInputs && mouseoveredInspector != null)
					{
						if(e.type == EventType.DragPerform || (e.type == EventType.MouseUp && Event.current.button == 0))
						{
							mouseoveredInspector.LastInputTime = Platform.Time;

							var reordering = mouseDownInfo.Reordering;
							var reorderingTo = reordering.MouseoveredDropTarget.Parent;
							if(reorderingTo != null && !mouseDownInfo.IsClick)
							{
								reorderingTo.OnMemberDragNDrop(mouseDownInfo, DrawGUI.Active.DragAndDropObjectReferences);
							}
							#if DEV_MODE && DEBUG_DRAG_N_DROP
							else { Debug.Log(StringUtils.ToColorizedString("Won't call OnMemberDragNDrop. reorderingTo=", reorderingTo, ", mouseDownInfo.IsClick=", mouseDownInfo.IsClick)); }
							#endif
						}
						#if DEV_MODE && DEBUG_DRAG_N_DROP
						else { Debug.Log(StringUtils.ToColorizedString("Won't call OnMemberDragNDrop. currentEvent.type=", e.type, ", Event.current.button=", Event.current.button)); }
						#endif
					}
					#if DEV_MODE && DEBUG_DRAG_N_DROP
					else { Debug.Log(StringUtils.ToColorizedString("Won't call OnMemberDragNDrop. IgnoreAllMouseInputs=", activeManager.IgnoreAllMouseInputs, ", setMouseoveredPart=", setMouseoveredPart)); }
					#endif

					if(mouseDownInfo.Inspector == inspector)
					{
						if(e.type != EventType.MouseUp || Event.current.button == 0)
						{
							if(mouseDownInfo.MouseDownOverDrawer != null)
							{
								#if DEV_MODE
								if(!mouseDownInfo.IsClick && !mouseDownInfo.CursorMovedAfterMouseDown && mouseDownInfo.Inspector.InspectorDrawer.HasFocus)
								{
									Debug.LogWarning("Calling OnMouseUpAfterDownOverControl with IsClick="+StringUtils.False+ ", but CursorMovedAfterMouseDown was also false. Bug?");
								}
								#endif

								mouseDownInfo.MouseDownOverDrawer.OnMouseUpAfterDownOverControl(e, mouseDownInfo.IsClick);
							}
							mouseDownInfo.Clear(true);
						}
					}
					break;
				case EventType.MouseDown:
					#if DEV_MODE && DEBUG_ON_MOUSE_DOWN
					Debug.Log(StringUtils.ToColorizedString("BeginInspector("+inspector+") - MouseDown with IgnoreAllMouseInputs=", activeManager.IgnoreAllMouseInputs, ", setMouseoveredPart=", setMouseoveredPart));
					#endif

					if(!activeManager.IgnoreAllMouseInputs && setMouseoveredPart != InspectorPart.None)
					{
						inspector.LastInputTime = Platform.Time;
						inspector.OnMouseDown(e);
						
						//give controls time to react to selection changes, editing text field changes etc.
						//before cached values are updated
					}
					break;
				case EventType.KeyDown:
					inspector.LastInputTime = Platform.Time;
					break;
				case EventType.ContextClick:
					if(!activeManager.IgnoreAllMouseInputs && setMouseoveredPart != InspectorPart.None)
					{
						inspector.OnContextClick(e);
					}
					break;
			}
			
			if(state.keyboardControlLastFrame != KeyboardControlUtility.KeyboardControl)
			{
				state.previousKeyboardControl = state.keyboardControlLastFrame;
				state.keyboardControlLastFrame = KeyboardControlUtility.KeyboardControl;

				#if UNITY_EDITOR
				state.previousKeyboardControlRect = state.keyboardRectLastFrame;
				state.keyboardRectLastFrame = KeyboardControlUtility.Info.KeyboardRect;
				#endif
			}
		}

		/// <summary>
		/// This should be called at the end of OnGUI of subject that implements IInspector.
		/// 
		/// This should not be called when ExitGUIException is encountered. However you should
		/// set NowDrawingPart to InspectorPart.None manually.
		/// </summary>
		/// <param name="inspector"> The inspector. </param>
		public static void EndInspector(IInspector inspector)
		{
			GUISpace.Current = Space.Window;

			if(inspector == activeTooltipInspector)
			{
				RendererActiveTooltip();
			}

			ActiveInspector.NowDrawingPart = InspectorPart.None;
			
			if(OnInspectorGUIEnd != null)
			{
				OnInspectorGUIEnd(inspector);
			}

			#if DEV_MODE
			Debug.Assert(DrawGUI.IndentLevel == 0, "IndentLevel was "+ DrawGUI.IndentLevel + " when EndInspector was called");
			#endif
		}
		
		public static void OnResettingFieldValue(IDrawer field)
		{
			if(activeManager.MouseDownInfo.MouseDownOverDrawer != null)
			{
				#if DEV_MODE
				Debug.Log("Clearing MouseDownInfo because "+field+" value is being reset");
				#endif
				//fixes problem where double-clicking a numeric field resets the value
				//but then the value gets changed by the prefix being dragged
				//UPDATE: this is now also done during DisplayDialog, so it should already be handled there
				activeManager.MouseDownInfo.Clear();
			}
		}

		public static void SetActiveTooltip(IInspector inspector, Rect position, string tooltip)
		{
			activeTooltipInspector = inspector;
			activeTooltipPosition = position;
			activeTooltip = tooltip;
		}

		private static void RendererActiveTooltip()
		{
			if(activeTooltip.Length > 0)
			{
				DrawGUI.Active.TooltipBox(activeTooltipPosition, activeTooltip);
				activeTooltip = "";
			}
		}

		public static Vector2 GetInspectorLocalDrawAreaOffset()
		{
			var currentAreaBeginScreenPoint = GUIUtility.GUIToScreenPoint(Vector2.zero);
			#if DEV_MODE
			Debug.Log("GetLocalDrawAreaOffset: "+(currentAreaBeginScreenPoint - inspectorBeginScreenPoint)+" with inspectorBeginScreenPoint="+inspectorBeginScreenPoint+", currentAreaBeginScreenPoint="+currentAreaBeginScreenPoint);
			#endif
			return currentAreaBeginScreenPoint - inspectorBeginScreenPoint;
		}
	}
}