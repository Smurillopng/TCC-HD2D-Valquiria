#define SAFE_MODE

#define DEBUG_FOCUS_FIELD
//#define DEBUG_NULL_FIELD_INFO
#define DEBUG_ON_CLICK

using System;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Sisus
{
	/// <summary>
	/// Class for fields that have both a prefix and a linked control,
	/// which are essentially treated as one in terms of UX (can't select
	/// them individially etc.). Examples are ToggleField, TextField and NumericField.
	/// </summary>
	[Serializable]
	public abstract class PrefixControlComboDrawer<TValue> : FieldDrawer<TValue>
	{
		private const int FocusFieldRepeatTimes = 10;
		private Vector2 mouseDownCursorTopLeftCornerOffset;
		
		private static PrefixedControlPart mouseOverPart = PrefixedControlPart.None;
		private static PrefixedControlPart mouseDownOverPart = PrefixedControlPart.None;

		protected int focusField;
		protected Rect labelLastDrawPosition;
		protected Rect controlLastDrawPosition;

		/// <summary> Gets the position and dimensions of the drag bar between the prefix and the control. </summary>
		/// <value> The drag bar position. </value>
		private Rect DragBarPosition
		{
			get
			{
				var rect = labelLastDrawPosition;
				rect.x -= 10f;
				rect.width = 6f;
				rect.height = 8f;
				rect.y += 5f;
				return rect;
			}
		}

		/// <inheritdoc />
		public sealed override Vector2 MouseDownCursorTopLeftCornerOffset
		{
			get
			{
				return mouseDownCursorTopLeftCornerOffset;
			}
		}

		/// <inheritdoc />
		public override bool MouseDownOverReorderArea
		{
			get
			{
				return mouseDownOverPart == PrefixedControlPart.Prefix;
			}
		}

		/// <summary>
		/// Which part of this subject is the cursor currently residing over?
		/// </summary>
		/// <value>
		/// The mouseovered part.
		/// </value>
		protected PrefixedControlPart MouseOverPart
		{
			get
			{
				return Mouseovered ? mouseOverPart : PrefixedControlPart.None;
			}

			set
			{
				mouseOverPart = value;
			}
		}

		/// <summary>
		/// In context where mouse is currently pressed down, returns the part if this
		/// subject that was mouseovered when the left mouse button was pressed down?
		/// </summary>
		/// <value>
		/// The mouseovered part.
		/// </value>
		protected PrefixedControlPart MouseDownOverPart
		{
			get
			{
				return mouseDownOverPart;
			}
		}
		
		/// <inheritdoc />
		protected sealed override Rect PrefixLabelPosition
		{
			get
			{
				return labelLastDrawPosition;
			}
		}

		/// <inheritdoc />
		public sealed override Rect ControlPosition
		{
			get
			{
				return controlLastDrawPosition;
			}
		}

		/// <inheritdoc />
		public override float Height
		{
			get
			{
				return DrawGUI.SingleLineHeight;
			}
		}
		
		/// <summary>
		/// Starts a process that gives the control field keyboard focus
		/// </summary>
		protected void FocusControlField()
		{
			#if DEV_MODE && DEBUG_FOCUS_FIELD
			Debug.Log(Msg(ToString() + ".FocusField with ReadOnly=", ReadOnly, ", HasMultiSelectedControls=", InspectorUtility.ActiveManager.HasMultiSelectedControls));
			#endif

			#if DEV_MODE
			Debug.Assert(!InspectorUtility.ActiveManager.HasMultiSelectedControls, ToString()+ ".FocusControlField called with HasMultiSelectedControls="+StringUtils.True);
			Debug.Assert(Selected, ToString() + " FocusControlField called by Selected=" + StringUtils.False);
			Debug.Assert(!ObjectPicker.IsOpen);
			#endif

			if(!ReadOnly && !InspectorUtility.ActiveManager.HasMultiSelectedControls)
			{
				//During each repaint event, while value is more than zero,
				//we call GUI.SetNextControlName, and reduce the value by one.
				//Once the value reaches zero, we use DrawGUI.FocusControl to
				//set focus on the field.
				focusField = FocusFieldRepeatTimes;

				//trigger a repaint
				GUI.changed = true;
			}
			//new test
			else if(KeyboardControlUtility.JustClickedControl == 0)
			{
				KeyboardControlUtility.KeyboardControl = 0;
			}
		}

		/// <summary>
		/// Handles drawing label's tooltip as a separate hint icon before the control
		/// if preferences have been configured in such a way to show tooltips like so.
		/// </summary>
		/// <param name="position">
		/// The position. </param>
		protected void HandleDrawHintIcon(Rect position)
		{
			if(label.tooltip.Length > 0)
			{
				if(getValueCausedException)
				{
					var hintPos = position;
					hintPos.x -= DrawGUI.SingleLineHeight;
					hintPos.width = DrawGUI.SingleLineHeight;

					if(DrawGUI.Active.Button(hintPos, getOrSetValueExceptionLabel, InspectorPreferences.Styles.Label))
					{
						if(Event.current.button == 0)
						{
							Clipboard.Copy(getOrSetValueExceptionLabel.tooltip);
							if(getOrSetValueErrorOrWarningType == LogType.Warning)
							{
								Clipboard.SendCopyToClipboardMessage("Warning message");
							}
							else
							{
								Clipboard.SendCopyToClipboardMessage("Error message");
							}
						}
						else if(Event.current.button == 1)
						{
							var menu = Menu.Create();
							if(getOrSetValueErrorOrWarningType == LogType.Warning)
							{
								menu.Add("Copy Warning", () => Clipboard.Copy(getOrSetValueExceptionLabel.tooltip));
								Clipboard.SendCopyToClipboardMessage("Warning message");
							}
							else
							{
								menu.Add("Copy Error", ()=>Clipboard.Copy(getOrSetValueExceptionLabel.tooltip));
								Clipboard.SendCopyToClipboardMessage("Error message");
							}
							menu.OpenAt(hintPos);
						}
					}
				}
				else if(InspectorUtility.Preferences.enableTooltipIcons)
				{
					var hintPos = position;
					hintPos.x -= DrawGUI.SingleLineHeight;
					hintPos.width = DrawGUI.SingleLineHeight;

					DrawGUI.Active.HintIcon(hintPos, label.tooltip);
				}
			}
		}

		/// <inheritdoc />
		public override bool DrawBody(Rect position)
		{
			#if SAFE_MODE
			if(label == null)
			{
				Debug.LogError("DrawBody with null label called for "+ToString()+" with parent "+(parent == null ? "null" : parent.ToString()));
				return false;
			}
			#endif

			HandleDrawHintIcon(position);

			DrawDragBarIfReorderable();

			#if DEV_MODE && DEBUG_NULL_FIELD_INFO
			if(fieldInfo == null)
			{
				Debug.LogWarning(GetType().Name+".DrawBody() - fieldInfo was null!");
			}
			#endif

			var valueWas = Value;

			DrawDragBarIfReorderable();

			DrawerUtility.BeginFocusableField(this, controlId, ref focusField, MixedContent);
			var setValue = DrawControlVisuals(position, valueWas);
			DrawerUtility.EndFocusableField();

			if(setValue == null)
			{
				if(valueWas == null)
				{
					return false;
				}
				Value = default(TValue);
				return true;
			}

			if(!setValue.Equals(valueWas))
			{
				Value = setValue;
				return true;
			}

			return false;
		}

		protected void DrawDragBarIfReorderable()
		{
			if(IsReorderable)
			{
				GUI.Label(DragBarPosition, GUIContent.none, "RL DragHandle");
			}
		}
		
		/// <summary>
		/// Just draws the control portion with given input value and return changes made to the value via the control.
		/// NOTE: Things like data validation, focus handling etc. are outside the scope of this method.
		/// </summary>
		public virtual TValue DrawControlVisuals(Rect position, TValue inputValue)
		{
			return inputValue;
		}
		
		/// <summary>
		/// fill out lastDrawPosition, labelLastDrawPosition and controlLastDrawPosition
		/// So that DrawPrefix and DrawControl etc. can do their things
		/// </summary>
		protected override void GetDrawPositions(Rect position)
		{
			lastDrawPosition = position;
			lastDrawPosition.height = Height;
			
			lastDrawPosition.GetLabelAndControlRects(label, out labelLastDrawPosition, out controlLastDrawPosition);

			localDrawAreaOffset = DrawGUI.GetLocalDrawAreaOffset();
		}
		
		/// <inheritdoc />
		protected override void OnLayoutEvent(Rect position)
		{
			base.OnLayoutEvent(position);

			if(Mouseovered)
			{
				var mouseOverPartWas = MouseOverPart;

				var cursorPos = Cursor.LocalPosition;

				if(PrefixLabelPosition.Contains(cursorPos) || DragBarPosition.Contains(cursorPos))
				{
					if(mouseOverPartWas != PrefixedControlPart.Prefix)
					{
						MouseOverPart = PrefixedControlPart.Prefix;
						UpdatePrefixDrawer();
					}
				}
				else if(ControlPosition.Contains(cursorPos))
				{
					if(mouseOverPartWas != PrefixedControlPart.Control)
					{
						MouseOverPart = PrefixedControlPart.Control;
						UpdatePrefixDrawer();
					}
				}
				else
				{
					if(mouseOverPartWas != PrefixedControlPart.None)
					{
						MouseOverPart = PrefixedControlPart.None;
						UpdatePrefixDrawer();
					}
				}
			}
		}

		/// <inheritdoc />
		public override bool OnClick(Event inputEvent)
		{
			var clickedPart = mouseOverPart;

			#if DEV_MODE && DEBUG_ON_CLICK
			Debug.Log(ToString()+ ".OnPrefixedControlClick(clickedPart="+clickedPart+") and mouseOverPart="+ mouseOverPart+", Selected="+Selected+ ", KeyboardControl=" + KeyboardControlUtility.KeyboardControl);
			#endif

			#if DEV_MODE
			Debug.Assert(clickedPart == mouseOverPart);
			#endif

			mouseDownCursorTopLeftCornerOffset = new Vector2(labelLastDrawPosition.x - inputEvent.mousePosition.x, labelLastDrawPosition.y - inputEvent.mousePosition.y + DrawGUI.SingleLineHeight);

			mouseDownOverPart = clickedPart;
			
			if(clickedPart == PrefixedControlPart.Prefix)
			{
				OnPrefixClicked(inputEvent);
				return true;
			}

			if(clickedPart == PrefixedControlPart.Control)
			{
				OnControlClicked(inputEvent);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Called when the prefix label portion of the drawer are clicked. This happens
		/// when the OnClick gets called with the cursor residing over the prefix label portion of the drawer.
		/// </summary>
		/// <param name="inputEvent"> The event for the mouse input. </param>
		/// <returns> True if click event should be consumed. </returns>
		protected virtual void OnPrefixClicked(Event inputEvent)
		{
			HandleOnClickSelection(inputEvent, ReasonSelectionChanged.PrefixClicked);
			DrawGUI.Use(inputEvent);
			DrawGUI.EditingTextField = false;

			if(!ReadOnly && !InspectorUtility.ActiveManager.HasMultiSelectedControls)
			{
				FocusControlField();
			}
		}
		
		/// <summary>
		/// Called when the control portion of the drawer are clicked. This happens
		/// when the OnClick gets called with the cursor residing over the control portion of the drawer.
		/// </summary>
		/// <param name="inputEvent"> The event for the mouse input. </param>
		/// <returns> True if click event should be consumed. </returns>
		protected virtual void OnControlClicked(Event inputEvent)
		{
			#if DEV_MODE && DEBUG_ON_CLICK
			Debug.Log(StringUtils.ToColorizedString(ToString(), ".OnControlClicked(", inputEvent, ") with Selected=", Selected));
			#endif

			HandleOnClickSelection(inputEvent, ReasonSelectionChanged.ControlClicked);

			if(!ReadOnly && !InspectorUtility.ActiveManager.HasMultiSelectedControls)
			{
				FocusControlField();
			}
			else
			{
				DrawGUI.Use(inputEvent);
				DrawGUI.EditingTextField = false;
			}
		}
		
		/// <inheritdoc/>
		protected override void OnSelectedInternal(ReasonSelectionChanged reason, IDrawer previous, bool isMultiSelection)
		{
			InspectorUtility.ActiveInspector.ScrollToShow(this);
			if(!isMultiSelection)
			{
				FocusControlField();
			}
		}

		/// <inheritdoc/>
		protected override void OnDeselectedInternal(ReasonSelectionChanged reason, IDrawer losingFocusTo)
		{
			focusField = 0;
		}

		/// <inheritdoc />
		public override string ValueToStringForFiltering()
		{
			// NOTE: This is only overridden at PrefixControlComboDrawer level
			// instead of FieldDrawer level, because we only want to do this with
			// leaf Drawer, and not e.g. with ParentFieldDrawer.
			
			var value = Value;
			if(value == null)
			{
				return "null";
			}
			var obj = value as Object;
			if(obj != null)
			{
				return obj.name;
			}
			try
			{
				return StringUtils.ToString(value);
			}
			#if DEV_MODE
			catch(Exception e)
			{
				Debug.LogError(e);
				return "";
			}
			#else
			catch(Exception)
			{
				return "";
			}
			#endif
			
		}

		/// <inheritdoc />
		public override void OnMouseover()
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(Clickable);
			#endif
			
			if(MouseOverPart == PrefixedControlPart.Control)
			{
				if(!ReadOnly)
				{
					DrawGUI.DrawMouseoverEffect(ControlPosition, localDrawAreaOffset);
				}
			}
			else if(MouseOverPart == PrefixedControlPart.Prefix)
			{
				if(InspectorUtility.Preferences.mouseoverEffects.prefixLabel)
				{
					DrawGUI.DrawLeftClickAreaMouseoverEffect(PrefixLabelPosition, localDrawAreaOffset);
				}

				if(IsReorderable)
				{
					DrawGUI.Active.SetCursor(MouseCursor.MoveArrow);
				}
			}
		}

		/// <inheritdoc/>
		public override void Dispose()
		{
			base.Dispose();

			focusField = 0;

			labelLastDrawPosition.y = -100f;
			labelLastDrawPosition.width = 0f;
			labelLastDrawPosition.height = 0f;

			controlLastDrawPosition.y = -100f;
			controlLastDrawPosition.width = 0f;
			controlLastDrawPosition.height = 0f;
		}

		/// <inheritdoc/>
		public override bool OnDoubleClick(Event inputEvent)
		{
			if(mouseOverPart == PrefixedControlPart.Prefix && ResetOnDoubleClick())
			{
				#if DEV_MODE
				Debug.LogWarning("Resetting field because it was double clicked: " + ToString() + "!");
				if(!DrawGUI.Active.DisplayDialog("Rest field", "Resetting field because it was double clicked.", "Ok", "Abort"))
				{
					return false;
				}
				#endif

				Reset();
				
				DrawGUI.Use(inputEvent);
				return true;
			}
			return false;
		}
	}
}