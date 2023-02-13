//#define DEBUG_SET_MOUSE_DOWN_OVER_CONTROL
//#define DEBUG_SET_MOUSE_BUTTON_IS_DOWN
//#define DEBUG_MOUSE_DOWN_USED

using System;
using UnityEngine;

namespace Sisus
{
	/// <summary>
	/// Contains information related to right mouse button being pressed down.
	/// Helps determine if it's a click event or a drag event, and whether the MouseDown event was used or not.
	/// </summary>
	public class RightClickInfo : IDisposable
	{
		/// <summary>
		/// The control over which the cursor was when left mouse button was pressed down
		/// </summary>
		private IDrawer mouseDownOverControl;

		/// <summary>
		/// The inspector over which the mouse button was pressed down
		/// </summary>
		private IInspector inspector;

		private bool isClick;
		private bool cursorMovedAfterMouseDown;
		private bool mouseDownEventWasUsed;
		private bool mouseButtonIsDown;

		private Vector2 mouseDownPos;

		private bool mouseoveredInspectorLeftDuringDrag;

		private bool mouseUpEventDone;
		private bool contextClickEventDone;

		/// <summary>
		/// Returns true during the frames that the right mouse button is pressed down,
		/// and during the frame that it is released, as long as the cursor position has
		/// remained unchanged throughout this interaction.
		/// </summary>
		/// <value>
		/// True if right mouse button is now being clicked, false if not.
		/// </value>
		public bool IsClick
		{
			get
			{
				return isClick;
			}

			private set
			{
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
				if(!mouseButtonIsDown && IsDrag() && DrawGUI.LastInputEventType != EventType.DragPerform && DrawGUI.LastInputEventType != EventType.DragExited)
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
		/// the right mouse button was last held down.
		/// 
		/// If right mouse button is currently held down, value indicates whether or not
		/// this is a drag event.
		/// 
		/// During mouse up event this can be used to determine whether or not this is a click event.
		/// </summary>
		/// <value> True if cursor moved while right mouse button was last held down. </value>
		public bool CursorMovedAfterMouseDown
		{
			get
			{
				return cursorMovedAfterMouseDown;
			}
		}

		/// <summary>
		/// This is set to false whenever the right mouse button is pressed down (via InspectorUtility.BeginInspector),
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
				if(mouseDownEventWasUsed != value) { Debug.Log("RightClickInfo.MouseDownEventWasUsed = "+StringUtils.ToColorizedString(value) +" with Event="+StringUtils.ToString(Event.current)); }
				#endif

				mouseDownEventWasUsed = value;
			}
		}

		public IDrawer MouseDownOverControl
		{
			get
			{
				return mouseDownOverControl;
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

		public RightClickInfo()
		{
			Cursor.OnPositionChanged += OnCursorPositionChanged;
			DrawGUI.OnEveryBeginOnGUI(OnBeginOnGUI, false);
		}

		public bool IsUnusedRightClickEvent()
		{
			var e = Event.current;
			if(e == null)
			{
				return false;
			}
			var type = e.type;

			if(type == EventType.MouseDown && e.button == 1)
			{
				return true;
			}

			if(type == EventType.ContextClick)
			{
				return !mouseDownEventWasUsed && isClick;
			}

			if(type == EventType.MouseUp && e.button == 1)
			{
				return !mouseDownEventWasUsed && isClick;
			}

			return false;
		}

		private void OnBeginOnGUI()
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(InspectorUtility.ActiveManager != null);
			#endif

			var e = Event.current;
			
			switch(e.type)
			{
				case EventType.ContextClick:

					contextClickEventDone = true;

					if(mouseDownEventWasUsed)
					{
						Clear();
						DrawGUI.Use(e);
						return;
					}

					IsClick = true;
					MouseButtonIsDown = true;
					MouseDownEventWasUsed = false;
					DrawGUI.OnEventUsed += OnEventUsed;

					if(mouseUpEventDone)
					{
						InspectorUtility.ActiveManager.OnNextLayout(Clear);
					}
					return;
				case EventType.MouseDown:
					if(e.button == 1)
					{
						IsClick = true;
						MouseButtonIsDown = true;
						cursorMovedAfterMouseDown = false;
						MouseDownEventWasUsed = false;
						DrawGUI.OnEventUsed += OnEventUsed;
						mouseUpEventDone = false;
						contextClickEventDone = false;
					}
					return;
				case EventType.MouseDrag:
				case EventType.DragUpdated:
					if(e.button == 1 && !mouseButtonIsDown)
					{
						MouseButtonIsDown = true;
						cursorMovedAfterMouseDown = true;
						MouseDownEventWasUsed = false;
					}
					break;
				case EventType.MouseUp:
					if(e.button == 1)
					{
						if(mouseDownEventWasUsed)
						{
							Clear();
							DrawGUI.Use(e);
							return;
						}

						IsClick = true;
						MouseButtonIsDown = true;
						cursorMovedAfterMouseDown = false;
						MouseDownEventWasUsed = false;

						DrawGUI.OnEventUsed += OnEventUsed;

						if(contextClickEventDone)
						{
							InspectorUtility.ActiveManager.OnNextLayout(Clear);
						}
					}					
					return;
			}
		}

		private void OnCursorPositionChanged(Vector2 newPosition)
		{
			IsClick = false;

			if(mouseButtonIsDown)
			{
				cursorMovedAfterMouseDown = true;
			}			
		}

		public void OnPressingMouseDown(IInspector setInspector, IDrawer overControl)
		{
			MouseDownEventWasUsed = false;

			#if DEV_MODE && DEBUG_SET_MOUSE_DOWN_OVER_CONTROL
			Debug.Log("RightClickInfo.mouseDownOverControl = "+StringUtils.ToString(overControl));
			#endif

			mouseDownOverControl = overControl;
			Inspector = setInspector;
		}

		public void OnPressedMouseDown(IInspector setInspector, IDrawer overControl)
		{
			if(overControl != null)
			{
				var e = Event.current;
				MouseDownPos = e.mousePosition;
			}
		}
		
		private void OnEventUsed(EventType type, Event clickEvent)
		{
			switch(type)
			{
				case EventType.MouseDown:
					if(clickEvent.button == 1)
					{
						MouseDownEventWasUsed = true;
						InspectorUtility.ActiveManager.OnNextLayout(SetIsNotClick);
					}
					break;
				case EventType.MouseUp:
					if(clickEvent.button == 1)
					{
						MouseDownEventWasUsed = true;
						InspectorUtility.ActiveManager.OnNextLayout(SetIsNotClick);
						MouseButtonIsDown = false;
					}
					break;
				case EventType.ContextClick:
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

		private void SetIsNotClick()
		{
			IsClick = false;
		}
		
		public void OnMouseoveredInspectorChanged(IInspector from, IInspector to)
		{
			// DragExited, DrawPerformed, MouseUp etc. are not called when it happens outside EditorWindow bounds.
			// Because of this we clear MouseDown info when cursor leaves EditorWindow bounds.
			
			#if DEV_MODE && DEBUG_ON_MOUSEOVERED_INSPECTOR_CHANGED
			Debug.Log(StringUtils.ToColorizedString("RightClickInfo.OnMouseoveredInspectorChanged(", from," => ", to, " with MouseButtonIsDown=", MouseButtonIsDown, ", mouseoveredInspectorLeftDuringDrag=", mouseoveredInspectorLeftDuringDrag, ", IsClick=", IsClick));
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
		}

		/// <summary>
		/// Determines whether current event is a mouse drag event.
		/// 
		/// True if has DragAndDropObjectReferences.
		/// 
		/// True if mouse has moved since mouse button was pressed down and last input event type is MouseDrag, DragUpdated, or MouseDown for right mouse button.
		/// 
		/// True if mouse has moved since mouse button was pressed down and current input event type is DragPerform, DragExited.
		/// 
		/// NOTE: This can return true for the remaining duration of the the frame, even after Clear has been called.
		/// 
		/// NOTE: This can return true, even if mouse down event was used.
		/// </summary>
		/// <returns> True if event is a mouse drag event, otherwise returns false. </returns>
		public bool IsDrag()
		{
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
				case EventType.ContextClick:
				case EventType.MouseDown:
					var lastInputEvent = DrawGUI.LastInputEvent();
					return lastInputEvent != null && lastInputEvent.button == 1;
				default:
					return false;
			}
		}

		/// <summary>
		/// Clears this object to its blank/initial state.
		/// This should be called each time the right mouse button is released.
		/// </summary>
		public void Clear()
		{
			#if DEV_MODE && DEBUG_SET_MOUSE_DOWN_OVER_CONTROL
			Debug.Log("RightClickInfo.mouseDownOverControl = "+StringUtils.Null);
			#endif

			mouseDownOverControl = null;
			inspector = null;
			IsClick = false;
			MouseButtonIsDown = false;
			MouseDownEventWasUsed = true;
			cursorMovedAfterMouseDown = false;
			mouseUpEventDone = false;
			contextClickEventDone = false;
		}

		/// <summary>
		/// DragExited event gets called when cursor leaves EditorWindow bounds.
		/// This does not mean that the right mouse button was released or that the dragging has
		/// stopped, merely that dragging has stopped over the EditorWindow in question for the
		/// time being. This method helps differentiate between those two different DragExited
		/// events.
		/// </summary>
		/// <returns>
		/// True if event just means cursor right window bounds during drag, false if event means right mouse button was released / dragging has stopped.
		/// </returns>
		public bool IsDragExitedReallyMouseLeaveWindowEvent()
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(Event.current.type == EventType.DragExited);
			#endif

			#if UNITY_EDITOR
			if(Platform.EditorMode)
			{
				if(mouseoveredInspectorLeftDuringDrag)
				{
					#if DEV_MODE
					//Debug.LogWarning(StringUtils.ToColorizedString("BeginInspector - "+currentEvent.type+ " ignoring because MouseoveredInspectorLeftDuringDrag="+StringUtils.True+", with ObjectReferences=", DrawGUI.Active.DragAndDropObjectReferences, ", MouseDownEventWasUsed=", activeManager.RightClickInfo.MouseDownEventWasUsed, ", IgnoreAllMouseInputs=", activeManager.IgnoreAllMouseInputs, ", mouseoveredPart=", setMouseoveredPart, ", button=", Event.current.button));
					Debug.LogWarning("RightClickInfo.DragExited really MouseLeaveWindow because MouseoveredInspectorLeftDuringDrag=" + StringUtils.True+" - cursor probably just right EditorWindow bounds.\nDragAndDropObjectReferences="+DrawGUI.Active.DragAndDropObjectReferences);
					#endif
					return true;
				}

				var activeManager = InspectorUtility.ActiveManager;
				var mouseoveredInspector = activeManager.MouseoveredInspector;
				if(mouseoveredInspector == null)
				{
					#if DEV_MODE
					Debug.LogWarning("RightClickInfo.DragExited really MouseLeaveWindow event because MouseoveredInspector was null - cursor probably just right EditorWindow bounds.\nDragAndDropObjectReferences=" + DrawGUI.Active.DragAndDropObjectReferences);
					#endif
					return true;
				}

				var mouseoveredDrawer = mouseoveredInspector.InspectorDrawer;
				if(!mouseoveredDrawer.MouseIsOver)
				{
					#if DEV_MODE
					Debug.LogWarning("RightClickInfo.DragExited really MouseLeaveWindow event because MouseoveredDrawer.MouseIsOver was " + StringUtils.False+" - cursor probably just right EditorWindow bounds.\nDragAndDropObjectReferences="+DrawGUI.Active.DragAndDropObjectReferences);
					#endif
					return true;
				}

				#if UNITY_EDITOR
				var mouseoveredWindow = mouseoveredDrawer as UnityEditor.EditorWindow;
				if(mouseoveredWindow != null && mouseoveredWindow != UnityEditor.EditorWindow.mouseOverWindow)
				{
					#if DEV_MODE
					Debug.LogWarning("RightClickInfo.DragExited really MouseLeaveWindow event because MouseoveredDrawer != mouseOverWindow - cursor probably just right EditorWindow bounds.\nDragAndDropObjectReferences=" + DrawGUI.Active.DragAndDropObjectReferences);
					#endif
					return true;
				}
				#endif

				var inspectorDrawerScreenPosition = mouseoveredDrawer.position;
				if(!inspectorDrawerScreenPosition.Contains(Cursor.ScreenPosition))
				{
					#if DEV_MODE
					Debug.LogWarning("RightClickInfo.DragExited really MouseLeaveWindow event because inspectorDrawerScreenPosition (" + inspectorDrawerScreenPosition+") did not contain Cursor.ScreenPosition ("+Cursor.ScreenPosition+") - cursor probably just right EditorWindow bounds.\nDragAndDropObjectReferences="+DrawGUI.Active.DragAndDropObjectReferences);
					#endif
					return true;
				}
			}
			#endif

			return false;
		}

		public override string ToString()
		{
			return "RightClickInfo(MouseButtonIsDown="+ MouseButtonIsDown+", IsClick = " + IsClick+ ")";
		}

		public void Dispose()
		{
			Cursor.OnPositionChanged -= OnCursorPositionChanged;
			DrawGUI.CancelOnEveryBeginOnGUI(OnBeginOnGUI);
			DrawGUI.OnEventUsed -= OnEventUsed;
		}
	}
}