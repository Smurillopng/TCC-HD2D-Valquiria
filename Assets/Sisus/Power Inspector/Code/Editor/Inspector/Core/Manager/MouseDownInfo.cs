//#define DEBUG_ON_MOUSEOVERED_INSPECTOR_CHANGED
//#define DEBUG_CURSOR_MOVED_AFTER_MOUSE_DOWN
//#define DEBUG_SET_MOUSE_DOWN_OVER_CONTROL
//#define DEBUG_SET_MOUSE_BUTTON_IS_DOWN
#define DEBUG_MOUSE_DOWN_USED
//#define DEBUG_IS_CLICK
//#define DEBUG_CLEAR

using JetBrains.Annotations;
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sisus
{
	/// <summary>
	/// Contains information related to left mouse button being pressed down.
	/// Helps determine if it's a click event, a drag event, and which drawer and
	/// inspector were under the cursor when the left mouse button was pressed down.
	/// 
	/// The Reordering property will hold data related to reordering after the left mouse
	/// button has been pressed down over a reorderable drawer.
	/// </summary>
	public sealed class MouseDownInfo : IDisposable
	{
		/// <summary>
		/// The drawer over which the cursor was when left mouse button was pressed down
		/// </summary>
		private IDrawer mouseDownOverDrawer;

		/// <summary>
		/// The inspector over which the mouse button was pressed down
		/// </summary>
		private IInspector inspector;

		/// <summary>
		/// Reorderable drawer currently being dragged as well as its current parent and inspector.
		/// </summary>
		private readonly ReorderInfo reordering = new ReorderInfo();
		
		private IDraggablePrefix draggingPrefixOfDrawer;
		
		private bool isClick;
		private bool cursorMovedAfterMouseDown;
		private bool mouseDownEventWasUsed;
		private bool mouseButtonIsDown;

		private Vector2 mouseDownPos;

		private bool mouseoveredInspectorLeftDuringDrag;

		public delegate void MouseUpCallback(IDrawer mouseDownOverDrawer, bool isClick);

		public MouseUpCallback onMouseUp;

		/// <summary>
		/// Returns true during the frames that the left mouse button is pressed down,
		/// and during the frame that it is released, as long as the cursor position has
		/// remained unchanged throughout this interaction.
		/// </summary>
		/// <value>
		/// True if left mouse button is now being clicked, or was just clicked, otherwise false.
		/// </value>
		public bool IsClick
		{
			get
			{
				return isClick;
			}

			private set
			{
				#if DEV_MODE && DEBUG_IS_CLICK
				if(isClick && !value) { Debug.Log("IsClick = "+StringUtils.False); }
				#endif

				isClick = value;
			}
		}
		
		/// <summary>
		/// Gets a value indicating whether the mouse button is currently pressed down.
		/// 
		/// This returns true even if MouseDown event was used, or this is a mouse drag event, unlike IsClick.
		/// </summary>
		/// <value> True if mouse button is pressed down, false if not. </value>
		public bool MouseButtonIsDown
		{
			get
			{
				#if DEV_MODE && PI_ASSERTATIONS
				// If IsDrag is true but MouseButtonIsDown log error.
				// This currently happens if a mouse drag is started outside of the bounds of Power Inspector windows.
				// It should get fixed when the mouse enters the bounds of the window though.
				if(!mouseButtonIsDown && IsDrag() && DrawGUI.LastInputEventType != EventType.DragPerform && DrawGUI.LastInputEventType != EventType.DragExited /* && (inspector != null || InspectorUtility.ActiveManager.MouseoveredInspector != null)*/)
				{ Debug.LogError(StringUtils.ToColorizedString("MouseButtonIsDown was ", false, " even though IsDrag() was ", true, " with ObjectReferences=", StringUtils.ToString(DrawGUI.Active.DragAndDropObjectReferences), ", isClick=", isClick, ", LastInputEventType=", DrawGUI.LastInputEventType+", LastInputEvent="+StringUtils.ToString(DrawGUI.LastInputEvent()))); }
				#endif

				return mouseButtonIsDown || mouseoveredInspectorLeftDuringDrag;
			}

			private set
			{
				#if DEV_MODE && DEBUG_SET_MOUSE_BUTTON_IS_DOWN
				if(mouseButtonIsDown != value) { Debug.Log("MouseButtonIsDown = "+StringUtils.ToColorizedString(value)); }
				#endif

				mouseButtonIsDown = value;
			}
		}

		/// <summary>
		/// Gets a value indicating whether or not the cursor moved during the time that
		/// the left mouse button was last held down.
		/// 
		/// If left mouse button is currently held down, value indicates whether or not
		/// this is a drag event.
		/// 
		/// During mouse up event this can be used to determine whether or not this is a click event.
		/// </summary>
		/// <value> True if cursor moved while left mouse button was last held down. </value>
		public bool CursorMovedAfterMouseDown
		{
			get
			{
				return cursorMovedAfterMouseDown;
			}

			private set
			{
				#if DEV_MODE && DEBUG_CURSOR_MOVED_AFTER_MOUSE_DOWN
				if(cursorMovedAfterMouseDown != value) { Debug.Log("cursorMovedAfterMouseDown = "+StringUtils.ToColorizedString(value)); }
				#endif

				cursorMovedAfterMouseDown = value;
			}
		}

		/// <summary>
		/// This is set to false whenever the left mouse button is pressed down (via InspectorUtility.BeginInspector),
		/// and to false
		/// Gets a value indicating whether the current or previous mouse down event was used.
		/// </summary>
		/// <value> True if mouse down event was used, false if not. </value>
		public bool MouseDownEventWasUsed
		{
			get
			{
				return mouseDownEventWasUsed;
			}

			private set
			{
				#if DEV_MODE && DEBUG_MOUSE_DOWN_USED
				if(mouseDownEventWasUsed != value) { Debug.Log("MouseDownEventWasUsed = "+StringUtils.ToColorizedString(value) +" with Event="+StringUtils.ToString(Event.current)); }
				#endif

				mouseDownEventWasUsed = value;
			}
		}
		
		public IDrawer MouseDownOverDrawer
		{
			get
			{
				return mouseDownOverDrawer;
			}
		}

		public IDraggablePrefix DraggingPrefixOfDrawer
		{
			get
			{
				return draggingPrefixOfDrawer;
			}
		}

		public IInspector Inspector
		{
			get
			{
				return inspector;
			}

			private set
			{
				if(inspector != value)
				{
					if(inspector != null)
					{
						inspector.CancelOnNextInspectedChanged(Clear);
					}
					if(value != null)
					{
						value.CancelOnNextInspectedChanged(Clear);
						value.OnNextInspectedChanged(Clear);
					}
					inspector = value;
				}
			}
		}

		/// <summary>
		/// Reorderable currently being dragged
		/// </summary>
		public ReorderInfo Reordering
		{
			get
			{
				return reordering;
			}
		}

		public bool NowReordering
		{
			get
			{
				return !isClick && reordering.Drawer != null;
			}
		}

		public bool NowDraggingPrefix
		{
			get
			{
				return draggingPrefixOfDrawer != null;
			}
		}

		public Vector2 MouseDownPos
		{
			get
			{
				return GUIUtility.ScreenToGUIPoint(mouseDownPos);
			}

			private set
			{
				mouseDownPos = GUIUtility.GUIToScreenPoint(value);
			}
		}

		public object MouseDownOverDrawerValue
		{
			get
			{
				return MouseDownOverDrawerValues.Length > 0 ? MouseDownOverDrawerValues[0] : null;
			}
		}

		public object[] MouseDownOverDrawerValues
		{
			get;
			private set;
		}

		public MouseDownInfo()
		{
			Cursor.OnPositionChanged += OnCursorPositionChanged;
			DrawGUI.OnEveryBeginOnGUI(OnBeginOnGUI, false);
		}

		private void HandleExternallyStartedDrag()
		{
			if(DrawGUI.IsUnityObjectDrag && !mouseButtonIsDown && !MouseDownEventWasUsed)
			{
				OnDetectedExternallyStartedDrag();
			}
		}
		
		private void OnDetectedExternallyStartedDrag()
		{
			#if DEV_MODE
			Debug.LogWarning("Detected UnityObject drag that probably originated from another window. Inspector="+StringUtils.ToString(Inspector));
			#endif

			MouseButtonIsDown = true;
			CursorMovedAfterMouseDown = true;
			MouseDownEventWasUsed = false;
			IsClick = false;
			OnDragStarted(inspector, DrawGUI.Active.DragAndDropObjectReferences);
		}

		private void OnBeginOnGUI()
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(InspectorUtility.ActiveManager != null, "OnBeginOnGUI called with ActiveManager null");
			#endif

			var e = Event.current;
			
			switch(e.type)
			{
				case EventType.Layout:
					HandleExternallyStartedDrag();
					break;
				// Not sure if MouseDrag and DragUpdated are necessary...
				case EventType.MouseDrag:
				case EventType.DragUpdated:
					if(e.button == 0 && !mouseButtonIsDown)
					{
						// new!
						if(DrawGUI.IsUnityObjectDrag)
						{
							OnDetectedExternallyStartedDrag();
						}
						else
						{
							MouseButtonIsDown = true;
							CursorMovedAfterMouseDown = true;
							MouseDownEventWasUsed = false;
							IsClick = false;
						}
					}
					break;
				case EventType.MouseDown:
					if(e.button == 0)
					{
						IsClick = true;
						MouseButtonIsDown = true;
						CursorMovedAfterMouseDown = false;
						MouseDownEventWasUsed = false;
						DrawGUI.OnEventUsed += OnEventUsed;
					}
					break;
				case EventType.MouseUp:
					InspectorUtility.ActiveManager.OnNextLayout(Clear);
					break;
			}
		}

		private void OnCursorPositionChanged(Vector2 newPosition)
		{
			IsClick = false;

			if(mouseButtonIsDown)
			{
				if(!cursorMovedAfterMouseDown)
				{
					CursorMovedAfterMouseDown = true;

					var setReorderable = mouseDownOverDrawer as IReorderable;
					if(setReorderable != null && setReorderable.MouseDownOverReorderArea)
					{
						var setReorderableParent = setReorderable.Parent as IReorderableParent;
						if(setReorderableParent != null && setReorderableParent.MemberIsReorderable(setReorderable))
						{
							reordering.OnReorderableDragStarted(setReorderable, setReorderableParent, inspector);
							return;
						}
					}
				}
			}			
		}

		public void OnPressingMouseDown(IInspector setInspector, IDrawer overControl)
		{
			MouseDownEventWasUsed = false;

			#if DEV_MODE && DEBUG_SET_MOUSE_DOWN_OVER_CONTROL
			Debug.Log("mouseDownOverControl = "+StringUtils.ToString(overControl)+" with Event="+StringUtils.ToString(Event.current));
			#endif

			mouseDownOverDrawer = overControl;
			Inspector = setInspector;
		}

		public void OnPressedMouseDown(IInspector setInspector, IDrawer overControl)
		{
			if(overControl != null)
			{
				var e = Event.current;
				MouseDownPos = e.mousePosition;

				var overField = overControl as IFieldDrawer;
				if(overField != null && overField.CanReadFromFieldWithoutSideEffects)
				{
					MouseDownOverDrawerValues = overControl.GetValues();
				}
				else
				{
					MouseDownOverDrawerValues = ArrayPool<object>.ZeroSizeArray;
				}

				var setDraggingPrefixOfControl = overControl as IDraggablePrefix;
				if(setDraggingPrefixOfControl != null && setDraggingPrefixOfControl.DraggingPrefix)
				{
					draggingPrefixOfDrawer = setDraggingPrefixOfControl;
					draggingPrefixOfDrawer.OnPrefixDragStart(e);
				}
			}
		}
		
		private void OnEventUsed(EventType type, Event clickEvent)
		{
			switch(type)
			{
				case EventType.MouseDown:
					if(clickEvent.button == 0)
					{
						MouseDownEventWasUsed = true;
					}
					break;
				case EventType.DragExited:
				case EventType.DragPerform:
				case EventType.MouseDrag:
					MouseDownEventWasUsed = true;
					IsClick = false;
					MouseButtonIsDown = false;
					break;
			}
			DrawGUI.OnEventUsed -= OnEventUsed;
		}

		public void OnDragStarted([CanBeNull]IInspector mouseoveredInspector, [NotNull]Object[] draggedObjects)
		{
			// Fix issue where OnPressingMouseDown would not get called if an UnityObject drag was started off-screen.
			if(inspector == null && mouseoveredInspector == null)
			{
				OnPressingMouseDown(null, null);
			}

			reordering.OnUnityObjectDragOverInspectorStarted(mouseoveredInspector, draggedObjects);

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(MouseButtonIsDown);
			Debug.Assert(IsDrag());
			Debug.Assert(DrawGUI.IsUnityObjectDrag);
			Debug.Assert(DrawGUI.Active.DragAndDropObjectReferences.ContentsMatch(draggedObjects));
			#endif
		}
		
		public void OnMouseoveredInspectorChanged(IInspector from, IInspector to)
		{
			// DragExited, DrawPerformed, MouseUp etc. are not called when it happens outside EditorWindow bounds.
			// Because of this we partially clear MouseDown info when cursor leaves EditorWindow bounds, and restore
			// it if/when it returns.
			
			#if DEV_MODE && DEBUG_ON_MOUSEOVERED_INSPECTOR_CHANGED
			Debug.Log(StringUtils.ToColorizedString("MouseDownInfo.OnMouseoveredInspectorChanged(", from," => ", to, " with MouseButtonIsDown=", MouseButtonIsDown, ", mouseoveredInspectorLeftDuringDrag=", mouseoveredInspectorLeftDuringDrag, ", IsClick=", IsClick));
			#endif

			if(to == null)
			{
				if(mouseButtonIsDown)
				{
					#if DEV_MODE
					Debug.LogWarning("Cursor left "+from+" bounds with MouseButtonIsDown="+StringUtils.True+". Setting IsClick and MouseButtonIsDown to "+StringUtils.False+" for now.");
					#endif

					mouseoveredInspectorLeftDuringDrag = true;

					IsClick = false;
					MouseButtonIsDown = false;
				}
				else
				{
					mouseoveredInspectorLeftDuringDrag = false;
				}
			}
			else if(mouseoveredInspectorLeftDuringDrag || DrawGUI.IsUnityObjectDrag)
			{
				#if DEV_MODE && DEBUG_ON_MOUSEOVERED_INSPECTOR_CHANGED
				Debug.LogError("HandleDragAfterCursorEnteredInspector started!!!!!!!!!!!!!!");
				#endif
				Cursor.OnPositionChanged += HandleDragAfterCursorEnteredInspector;
				HandleDragAfterCursorEnteredInspector(Cursor.LocalPosition);
			}
		}

		/// <summary>
		/// This is called every frame after cursor has left the bounds of the Inspector window during a drag-n-drop event.
		/// Each frame we try and firure out whether or not a drag n drop is still happening, and once we know it,
		/// adjust MouseDownInfo state accordingly.
		/// </summary>
		/// <param name="newPosition"> The new position. </param>
		private void HandleDragAfterCursorEnteredInspector(Vector2 newPosition)
		{
			var e = Event.current;
			if(e == null)
			{
				#if DEV_MODE
				Debug.LogWarning("HandleDragAfterCursorEnteredInspector - ignoring because not mouse event: "+StringUtils.ToString(e));
				#endif
				return;
			}

			switch(e.type)
			{
				case EventType.DragUpdated:
				case EventType.MouseDrag:
					// it's a drag!
					
					#if DEV_MODE
					Debug.LogWarning("HandleDragAfterCursorEnteredInspector - restoring Drag because event="+StringUtils.ToString(e));
					#endif

					#if DEV_MODE
					Debug.LogError("STOPPED LISTENING - IS DRAG!!!");
					#endif

					Cursor.OnPositionChanged -= HandleDragAfterCursorEnteredInspector;
					mouseoveredInspectorLeftDuringDrag = false;
					MouseButtonIsDown = true;
					IsClick = false;
					var draggedObjects = DrawGUI.Active.DragAndDropObjectReferences;
					if(draggedObjects != null)
					{
						OnDragStarted(inspector, draggedObjects);
					}
					break;
				//case EventType.Layout: //I think that DragUpdated should always get called instead of Layout during Drags. UPDATE: Noup. At least gets called during frame that cursor enters a new inspector during a drag.
				case EventType.MouseMove:
				case EventType.MouseDown:
				case EventType.MouseUp:
					#if DEV_MODE
					Debug.LogError("STOPPED LISTENING - NOT DRAG!!!");
					#endif
					
					// it's no longer a drag...
					Cursor.OnPositionChanged -= HandleDragAfterCursorEnteredInspector;
					
					#if DEV_MODE
					Debug.LogWarning("HandleDragAfterCursorEnteredInspector - discarding Drag because event="+StringUtils.ToString(e));
					#endif

					var lastInputEvent = DrawGUI.LastInputEvent();
					if(lastInputEvent != null)
					{
						var type = lastInputEvent.type;
						if(type != EventType.DragExited && type != EventType.MouseUp)
						{
							var setLastInputEvent = new Event(lastInputEvent);
							setLastInputEvent.type = mouseoveredInspectorLeftDuringDrag || DrawGUI.IsUnityObjectDrag ? EventType.DragExited : EventType.MouseUp;
							DrawGUI.Use(setLastInputEvent);
						}
					}

					Clear(true);

					mouseoveredInspectorLeftDuringDrag = false;
					
					#if DEV_MODE && PI_ASSERTATIONS
					Debug.Assert(!IsClick, "IsClick was true after MouseDownInfo.Clear was called during event " + StringUtils.ToString(e));
					Debug.Assert(!MouseButtonIsDown, "MouseButtonIsDown was true after MouseDownInfo.Clear was called during event " + StringUtils.ToString(e));
					Debug.Assert(!IsDrag(), "IsDrag was true after MouseDownInfo.Clear was called during event " + StringUtils.ToString(e));
					#endif

					break;
			}
		}

		public void OnCursorEnteredInspectorViewportDuringDrag(IInspector newlyMouseoveredInspector)
		{
			reordering.OnMouseoveredInspectorChanged(newlyMouseoveredInspector);
		}

		public void OnCursorLeftInspectorViewportDuringDrag()
		{
			reordering.OnMouseoveredInspectorChanged(null);
		}

		public void OnMouseDown() { }
		
		public void OnMouseUp(IInspectorManager inspectorManager)
		{
			if(onMouseUp != null)
			{
				onMouseUp(MouseDownOverDrawer, IsClick);
			}

			inspectorManager.OnNextLayout(Clear);
		}

		/// <summary>
		/// Determines whether current event is a mouse drag event.
		/// 
		/// True if has DragAndDropObjectReferences.
		/// 
		/// True if mouse has moved since mouse button was pressed down and last input event type is MouseDrag, DragUpdated, or MouseDown for left mouse button.
		/// 
		/// True if mouse has moved since mouse button was pressed down and current input event type is DragPerform, DragExited.
		/// 
		/// NOTE: This can return true for the remaining duration of the the frame, even after MouseDownInfo.Clear(true) has been called.
		/// 
		/// NOTE: This can return true, even if mouse down event was used.
		/// </summary>
		/// <returns> True if event is a mouse drag event, otherwise returns false. </returns>
		public bool IsDrag()
		{
			//UPDATE: The DragAndDropObjectReferences check is important
			//as without it the check would fail at random times when dragging
			//a MonoScript from the Project view on the custom InspectorWindow
			if(DrawGUI.IsUnityObjectDrag)
			{
				return true;
			}

			if(isClick)
			{
				return false;
			}

			switch(DrawGUI.LastInputEventType)
			{
				case EventType.DragPerform:
				case EventType.DragExited:
					return Event.current != null && Event.current.Equals(DrawGUI.LastInputEvent());
				case EventType.MouseDrag:
				case EventType.DragUpdated:
					return true;
				case EventType.MouseDown:
					var lastInputEvent = DrawGUI.LastInputEvent();
					return lastInputEvent != null && lastInputEvent.button == 0;
				default:
					return false;
			}
		}
		
		/// <summary>
		/// Clears this object to its blank/initial state.
		/// This should be called each time the left mouse button is released.
		/// </summary>
		public void Clear()
		{
			Clear(true);
		}

		/// <summary>
		/// Clears this object to its blank/initial state.
		/// This should be called each time the left mouse button is released.
		/// </summary>
		/// <param name="clearDragAndDropObjectReferences"> True to also clear DragAndDropObjectReferences. </param>
		public void Clear(bool clearDragAndDropObjectReferences)
		{
			#if DEV_MODE && DEBUG_CLEAR
			if(mouseDownOverDrawer != null || Platform.Active.GUI.DragAndDropObjectReferences.Length > 0) { Debug.Log("MouseDownInfo.Clear called with clearDragAndDropObjectReferences=" + StringUtils.ToColorizedString(clearDragAndDropObjectReferences)); }
			#endif

			#if DEV_MODE && DEBUG_SET_MOUSE_DOWN_OVER_CONTROL
			if(mouseDownOverDrawer != null) { Debug.Log("mouseDownOverDrawer = " + StringUtils.Null + "(via Clear)"); }
			#endif

			mouseDownOverDrawer = null;
			draggingPrefixOfDrawer = null;
			inspector = null;
			IsClick = false;
			MouseButtonIsDown = false;
			MouseDownEventWasUsed = true;
			mouseoveredInspectorLeftDuringDrag = false;
			reordering.Clear();
			PrefixResizeUtility.NowResizing = null; // TO DO: use a delegate to reduce dependencies

			if(clearDragAndDropObjectReferences)
			{
				#if DEV_MODE && PI_ASSERTATIONS
				if(Event.current != null && Event.current.rawType == EventType.DragExited)
				{
					Debug.LogWarning("MouseDownInfo.Clear called with clearDragAndDropObjectReferences="+StringUtils.True+" and EventType="+StringUtils.Green("DragExited")+". This could break drag n drop between EditorWindows.");
				}
				#endif

				Platform.Active.GUI.DragAndDropObjectReferences = ArrayPool<Object>.ZeroSizeArray;
			}
			#if DEV_MODE && PI_ASSERTATIONS
			else if(Event.current == null || Event.current.rawType != EventType.DragExited && Platform.Active.GUI.DragAndDropObjectReferences.Length > 0)
			{
				Debug.LogWarning("MouseDownInfo.Clear called with clearDragAndDropObjectReferences="+StringUtils.False+" and Event="+StringUtils.ToString(Event.current)+". Should probably clear them?");
			}
			#endif
		}

		/// <summary>
		/// DragExited event gets called when cursor leaves EditorWindow bounds.
		/// This does not mean that the left mouse button was released or that the dragging has
		/// stopped, merely that dragging has stopped over the EditorWindow in question for the
		/// time being. This method helps differentiate between those two different DragExited
		/// events.
		/// </summary>
		/// <returns>
		/// True if event just means cursor left window bounds during drag, false if event means left mouse button was released / dragging has stopped.
		/// </returns>
		public bool IsDragExitedReallyMouseLeaveWindowEvent()
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(Event.current.type == EventType.DragExited);
			#endif

			if(mouseoveredInspectorLeftDuringDrag)
			{
				#if DEV_MODE
				//Debug.LogWarning(StringUtils.ToColorizedString("BeginInspector - "+currentEvent.type+ " ignoring because MouseoveredInspectorLeftDuringDrag="+StringUtils.True+", with ObjectReferences=", DrawGUI.Active.DragAndDropObjectReferences, ", MouseDownEventWasUsed=", activeManager.MouseDownInfo.MouseDownEventWasUsed, ", IgnoreAllMouseInputs=", activeManager.IgnoreAllMouseInputs, ", mouseoveredPart=", setMouseoveredPart, ", button=", Event.current.button));
				Debug.LogWarning("DragExited really MouseLeaveWindow because MouseoveredInspectorLeftDuringDrag="+StringUtils.True+" - cursor probably just left EditorWindow bounds.\nDragAndDropObjectReferences="+DrawGUI.Active.DragAndDropObjectReferences);
				#endif
				return true;
			}

			var activeManager = InspectorUtility.ActiveManager;
			var mouseoveredInspector = activeManager.MouseoveredInspector;
			if(mouseoveredInspector == null)
			{
				#if DEV_MODE
				Debug.LogWarning("DragExited really MouseLeaveWindow event because MouseoveredInspector was null - cursor probably just left EditorWindow bounds.\nDragAndDropObjectReferences="+DrawGUI.Active.DragAndDropObjectReferences);
				#endif
				return true;
			}

			var mouseoveredDrawer = mouseoveredInspector.InspectorDrawer;
			if(!mouseoveredDrawer.MouseIsOver)
			{
				#if DEV_MODE
				Debug.LogWarning("DragExited really MouseLeaveWindow event because MouseoveredDrawer.MouseIsOver was "+StringUtils.False+" - cursor probably just left EditorWindow bounds.\nDragAndDropObjectReferences="+DrawGUI.Active.DragAndDropObjectReferences);
				#endif
				return true;
			}

			var mouseoveredWindow = mouseoveredDrawer as UnityEditor.EditorWindow;
			if(mouseoveredWindow != null && mouseoveredWindow != UnityEditor.EditorWindow.mouseOverWindow)
			{
				#if DEV_MODE
				Debug.LogWarning("DragExited really MouseLeaveWindow event because MouseoveredDrawer != mouseOverWindow - cursor probably just left EditorWindow bounds.\nDragAndDropObjectReferences="+DrawGUI.Active.DragAndDropObjectReferences);
				#endif
				return true;
			}

			var inspectorDrawerScreenPosition = mouseoveredDrawer.position;
			if(!inspectorDrawerScreenPosition.Contains(Cursor.ScreenPosition))
			{
				#if DEV_MODE
				Debug.LogWarning("DragExited really MouseLeaveWindow event because inspectorDrawerScreenPosition ("+inspectorDrawerScreenPosition+") did not contain Cursor.ScreenPosition ("+Cursor.ScreenPosition+") - cursor probably just left EditorWindow bounds.\nDragAndDropObjectReferences="+DrawGUI.Active.DragAndDropObjectReferences);
				#endif
				return true;
			}

			return false;
		}

		/// <summary>
		/// Events like MouseUp, DragUpdated and DragPerformed are normally ignored when the cursor is outside EditorWindow bounds,
		/// but when a field is being dragged we still need those events (especially important for the MouseUp event!)
		/// </summary>
		/// <param name="e"> Event to check. </param>
		/// <returns></returns>
		public EventType GetEventTypeForMouseUpDetection([NotNull]Event e)
		{
			#if UNITY_2020_1_OR_NEWER // Ad-hoc fix for issue where Event with type Ignore and rawType MouseUp is not sent in Unity 2020.2
			if(e.type == EventType.MouseLeaveWindow && mouseoveredInspectorLeftDuringDrag && (inspector == null || !inspector.State.WindowRect.MouseIsOver()))
			{
				#if DEV_MODE
				Debug.LogWarning("MouseLeaveWindow event is probably actually MouseUp event so treating it as such.\nWindowRect.MouseIsOver=" + (inspector == null ? "n/a" : inspector.State.WindowRect.MouseIsOver().ToString()) + ", mouseoveredInspectorLeftDuringDrag =" + mouseoveredInspectorLeftDuringDrag+", inspector="+(inspector == null ? "null" : inspector.ToString())+ ", mouseDownOverDrawer="+(mouseDownOverDrawer == null ? "null" : mouseDownOverDrawer.ToString()));
				#endif

				return EventType.MouseUp;
			}
			#endif

			return e.type == EventType.Ignore && inspector != null ? e.rawType : e.type;
		}

		public override string ToString()
		{
			return "(isClick="+isClick+ ", reordering="+(reordering.Drawer == null ? "null" : reordering.Drawer.ToString())+")";
		}

		public void Dispose()
		{
			Cursor.OnPositionChanged -= OnCursorPositionChanged;
			DrawGUI.CancelOnEveryBeginOnGUI(OnBeginOnGUI);
			DrawGUI.OnEventUsed -= OnEventUsed;
		}
	}
}