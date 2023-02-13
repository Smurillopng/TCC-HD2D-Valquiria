//#define DEBUG_VALUE_CHANGED
//#define DEBUG_START_EDITING_FIELD
//#define DEBUG_STOP_EDITING_FIELD
//#define DEBUG_KEYBOARD_INPUT

using System;
using UnityEngine;
using UnityEditor;

namespace Sisus
{
	[Serializable]
	public abstract class NumericDrawer<TValue> : PrefixControlComboDrawer<TValue>, IDraggablePrefix<TValue>, ITextFieldDrawer where TValue : IConvertible
	{
		protected bool editField;

		/// <inheritdoc cref="IDraggablePrefix.DraggingPrefix"/>
		public virtual bool DraggingPrefix
		{
			get
			{
				if(MouseDownOverPart == PrefixedControlPart.Prefix && !ReadOnly)
				{
					var mouseDownInfo = Inspector.Manager.MouseDownInfo;
					if(mouseDownInfo.MouseDownOverDrawer == this)
					{
						// Reordering functionality takes precedence over prefix dragging to change value functionality.
						if(mouseDownInfo.Reordering.Drawer != null)
						{
							return false;
						}
						return true;
					}
				}
				return false;
			}
		}

		/// <summary>
		/// Tells whether or not changes made to the field are applied immediately after each change, or only after the user stops editing the text field.
		/// </summary>
		protected virtual bool IsDelayedField
		{
			get
			{
				return false;
			}
		}

		/// <summary>
		/// This is used when determining if user has changed the current value when MixedContent is true.
		/// The detection is more accurate the rarer this value is. So e.g. 105230812 is probably better than 0.
		/// 
		/// Invalid values like NaN should still be avoided, otherwise the field might get tinted red while
		/// text field is being edited due to failing to pass data validation.
		/// </summary>
		protected virtual TValue ValueDuringMixedContent
		{
			get
			{
				return default(TValue);
			}
		}

		/// <inheritdoc/>
		public void StartEditingField()
		{
			#if DEV_MODE && DEBUG_START_EDITING_FIELD
			Debug.Log(StringUtils.ToColorizedString(ToString(), ".StartEditingField with CanEditField=", this.CanEditField(), ", Selected=", Selected, ", HasMultiSelectedControls=", InspectorUtility.ActiveManager.HasMultiSelectedControls));
			#endif

			if(!this.CanEditField())
			{
				#if DEV_MODE
				Debug.LogWarning(Msg(ToString(), ".StopEditingField called but CanStartEditing() returned ", false, ", with ReadOnly=", ReadOnly, "HasMultiSelectedControls=", InspectorUtility.ActiveManager.HasMultiSelectedControls));
				#endif
				return;
			}

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(Selected, ToString()+ " StartEditingField called by Selected="+StringUtils.False);
			#endif

			if(!InspectorUtility.ActiveManager.HasMultiSelectedControls)
			{
				editField = true;
				FocusControlField();
			}
		}

		public static bool CanStartEditingField(ITextFieldDrawer target)
		{
			return !target.ReadOnly && !InspectorUtility.ActiveManager.HasMultiSelectedControls;
		}

		/// <inheritdoc/>
		public void StopEditingField()
		{
			#if DEV_MODE && DEBUG_STOP_EDITING_FIELD
			Debug.Log(GetType().Name + ".StopEditingField()");
			#endif

			if(!this.CanEditField())
			{
				#if DEV_MODE
				Debug.LogWarning(Msg(ToString(), ".StopEditingField called but CanStartEditing() returned ", false, ", with ReadOnly=", ReadOnly, "HasMultiSelectedControls=", InspectorUtility.ActiveManager.HasMultiSelectedControls));
				#endif
				return;
			}

			if(DrawGUI.EditingTextField)
			{
				OnStoppedFieldEditing();
			}
			
			editField = false;
			focusField = 0;
			KeyboardControlUtility.KeyboardControl = 0;
			DrawGUI.EditingTextField = false;

			//this needs to be delayed or Unity can internally start editing the text again immediately e.g. when return is pressed
			InspectorUtility.ActiveInspector.OnNextLayout(StopEditingFieldStep);
		}

		/// <summary> Helper method of StopEditingField. </summary>
		private void StopEditingFieldStep()
		{
			DrawGUI.EditingTextField = false;
		}

		/// <summary>
		/// Called when was editing this numeric field's content (DrawGUI.EditingTextField was true)
		/// and then stopped (either DrawGUI.EditingTextField was set to false or selection changed
		/// to another control)
		/// </summary>
		protected virtual void OnStoppedFieldEditing() { }

		/// <inheritdoc cref="IDrawer.DrawSelectionRect" />
		public override void DrawSelectionRect()
		{
			//when editing text field we use the internally created selection rect
			if(!DrawGUI.EditingTextField || !Inspector.InspectorDrawer.HasFocus)
			{
				var rect = controlLastDrawPosition;
				rect.yMin += 1f;
				rect.height -= 1f;
				var theme = InspectorUtility.Preferences.theme;
				var color = Inspector.InspectorDrawer.HasFocus ? theme.ControlSelectedRect : theme.ControlSelectedUnfocusedRect;
				DrawGUI.DrawRect(rect, color, localDrawAreaOffset);
			}

			base.DrawSelectionRect();
		}

		public virtual void OnPrefixDragged(Event inputEvent)
		{
			var values = GetValues();
			var mouseDownValues = MouseDownValues;
			float mouseDelta = this.GetMouseDelta(inputEvent, MouseDownPosition);

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(values.Length == mouseDownValues.Length);
			Debug.Assert(mouseDownValues.Length > 0);
			#endif

			bool changed = false;
			for(int n = values.Length - 1; n >= 0; n--)
			{
				var valueWas = (TValue)values[n];
				var mouseDownValue = (TValue)MouseDownValues[n];
				var setValue = valueWas;
				OnPrefixDragged(ref setValue, mouseDownValue, mouseDelta);

				if(!setValue.Equals(valueWas))
				{
					values[n] = setValue;
					changed = true;
				}
			}

			if(changed)
			{
				SetValues(values);
			}

			// highlight the control when prefix is being dragged
			// to make it clear that it is being dragged
			DrawGUI.DrawMouseoverEffect(ControlPosition, Inspector.Preferences.theme.CanDragPrefixToAdjustValueTint, localDrawAreaOffset);
		}

		/// <inheritdoc cref="IDrawer.OnMouseover" />
		public override void OnMouseover()
		{
			#if DEV_MODE && PI_ASSERTATIONS
			if(!Clickable || !ShouldShowInInspector) { Debug.LogError(StringUtils.ToColorizedString(
			ToString(),".OnMouseover called with Clickable=", Clickable, ", ShowInInspector=", ShouldShowInInspector));}
			#endif

			if(MouseOverPart == PrefixedControlPart.Prefix)
			{
				if(InspectorUtility.Preferences.mouseoverEffects.prefixLabel)
				{
					DrawGUI.DrawLeftClickAreaMouseoverEffect(PrefixLabelPosition, localDrawAreaOffset);
				}

				if(IsReorderable)
				{
					DrawGUI.Active.SetCursor(MouseCursor.MoveArrow);
					return;
				}

				if(!ReadOnly)
				{
					DrawGUI.Active.SetCursor(MouseCursor.SlideArrow);

					// highlight the control when mouseovering the prefix
					// to make it clear that dragging will change the value of that field
					DrawGUI.DrawMouseoverEffect(ControlPosition, Inspector.Preferences.theme.CanDragPrefixToAdjustValueTint, localDrawAreaOffset);
				}
				return;
			}

			if(MouseOverPart == PrefixedControlPart.Control && !ReadOnly)
			{
				//UPDATE: Don't tint the text field on mouseover when it's being edited
				if(SelectedAndInspectorHasFocus && DrawGUI.EditingTextField)
				{
					return;
				}
				DrawGUI.DrawMouseoverEffect(ControlPosition, localDrawAreaOffset);
			}
		}

		/// <inheritdoc />
		public override void DrawFilterHighlight(SearchFilter filter, Color color)
		{
			if(lastPassedFilterTestType == FilterTestType.Value)
			{
				DrawGUI.DrawControlFilteringEffect(ControlPosition, color, localDrawAreaOffset);
			}
		}

		/// <inheritdoc />
		protected override void OnPrefixClicked(Event inputEvent)
		{
			HandleOnClickSelection(inputEvent, ReasonSelectionChanged.PrefixClicked);
			DrawGUI.Use(inputEvent);
			StopEditingField();
		}

		/// <inheritdoc />
		public virtual void OnPrefixDragStart(Event inputEvent) { }

		/// <inheritdoc />
		protected override void OnControlClicked(Event inputEvent)
		{
			TextFieldUtility.OnControlClicked(this, inputEvent);
		}

		/// <inheritdoc cref="IDrawer.OnKeyboardInputGiven" />
		public override bool OnKeyboardInputGiven(Event inputEvent, KeyConfigs keys)
		{
			#if DEV_MODE && DEBUG_KEYBOARD_INPUT
			Debug.Log(ToString()+".OnKeyboardInputGiven("+StringUtils.ToString(inputEvent)+")");
			#endif

			if(DrawGUI.EditingTextField)
			{
				switch(inputEvent.keyCode)
				{
					case KeyCode.Escape:
					{
						GUI.changed = true;
						StopEditingField();
						DrawGUI.Use(inputEvent);
						return true;
					}
					case KeyCode.Return:
					case KeyCode.KeypadEnter:
					{
						if(inputEvent.modifiers == EventModifiers.None)
						{
							GUI.changed = true;
							StopEditingField();
							DrawGUI.Use(inputEvent);
							return true;
						}
						return false;
					}
				}

				if(keys.DetectTextFieldReservedInput(inputEvent, TextFieldType.Numeric))
				{
					return false;
				}
				return base.OnKeyboardInputGiven(inputEvent, keys);
			}

			switch(inputEvent.keyCode)
			{
				case KeyCode.Alpha0:
				case KeyCode.Alpha1:
				case KeyCode.Alpha2:
				case KeyCode.Alpha3:
				case KeyCode.Alpha4:
				case KeyCode.Alpha5:
				case KeyCode.Alpha6:
				case KeyCode.Alpha7:
				case KeyCode.Alpha8:
				case KeyCode.Alpha9:
				case KeyCode.Keypad0:
				case KeyCode.Keypad1:
				case KeyCode.Keypad2:
				case KeyCode.Keypad3:
				case KeyCode.Keypad4:
				case KeyCode.Keypad5:
				case KeyCode.Keypad6:
				case KeyCode.Keypad7:
				case KeyCode.Keypad8:
				case KeyCode.Keypad9:
					if(inputEvent.modifiers == EventModifiers.None)
					{
						GUI.changed = true;
						string valueString = inputEvent.ToString();

						char valueChar = valueString[valueString.Length - 1];
						var valueNumeric = char.GetNumericValue(valueChar);
						var setValue = (TValue)Convert.ChangeType(valueNumeric, typeof(TValue));

						#if DEV_MODE
						Debug.Log("valueString="+valueString+", valueChar="+valueChar+ ", valueNumeric=" + valueNumeric + ", setValue="+setValue);
						#endif

						Value = setValue;
						DrawGUI.Use(inputEvent);
						StartEditingField();

						TextEditorUtility.MoveCursorToTextEditorEnd();
						return true;
					}
					break;
				case KeyCode.Return:
				case KeyCode.KeypadEnter:
				{
					if(inputEvent.modifiers == EventModifiers.None && !ReadOnly)
					{
						GUI.changed = true;
						DrawGUI.Use(inputEvent);
						StartEditingField();
						return true;
					}
					return false;
				}
				case KeyCode.Comma:
				case KeyCode.Period:
				case KeyCode.KeypadPeriod:
					if(!ReadOnly)
					{
						InspectorUtility.ActiveInspector.OnNextLayout(()=>TextEditorUtility.Insert('.'));
						return true;
					}
					return false;
				case KeyCode.Plus:
				case KeyCode.KeypadPlus:
					if(!ReadOnly)
					{
						GUI.changed = true;
						DrawGUI.Use(inputEvent);
						double valueNumeric = (double)Convert.ChangeType(Value, Types.Double) + 1d;
						Value = (TValue)Convert.ChangeType(valueNumeric, typeof(TValue));
						return true;
					}
					return false;
				case KeyCode.Minus:
				case KeyCode.KeypadMinus:
					if(!ReadOnly)
					{
						GUI.changed = true;
						DrawGUI.Use(inputEvent);
						double valueNumeric = (double)Convert.ChangeType(Value, Types.Double) - 1d;
						Value = (TValue)Convert.ChangeType(valueNumeric, typeof(TValue));
						return true;
					}
					return false;
				case KeyCode.F2:
					if(!ReadOnly)
					{
						GUI.changed = true;
						DrawGUI.Use(inputEvent);
						StartEditingField();
						return true;
					}
					return false;
			}
			
			return base.OnKeyboardInputGiven(inputEvent, keys);
		}

		/// <inheritdoc/>
		public void OnPrefixDraggedInterface(ref object inputValue, object inputMouseDownValue, float mouseDelta)
		{
			var valueCast = (TValue)inputValue;
			var mouseDownValueCast = (TValue)inputMouseDownValue;
			OnPrefixDragged(ref valueCast, mouseDownValueCast, mouseDelta);
			inputValue = valueCast;
		}

		/// <inheritdoc/>
		public abstract void OnPrefixDragged(ref TValue inputValue, TValue inputMouseDownValue, float mouseDelta);

		/// <inheritdoc/>
		public override bool DrawBody(Rect position)
		{
			HandleDrawHintIcon(position);

			DrawDragBarIfReorderable();

			var guiChangedWas = GUI.changed;
			GUI.changed = false;
			bool guiChanged;
			bool mixedContent = MixedContent;
			var valueWas = mixedContent ? ValueDuringMixedContent : Value;
			TValue setValue;
			
			DrawerUtility.BeginInputField(this, controlId, ref editField, ref focusField, mixedContent);
			{
				setValue = DrawControlVisuals(position, valueWas);
				guiChanged = GUI.changed;
			}
			DrawerUtility.EndInputField();

			if(guiChanged && !DraggingPrefix) // new test: disabled when dragging prefix
			{
				#if DEV_MODE && DEBUG_VALUE_CHANGED
				Debug.Log(StringUtils.ToColorizedString(ToString(), " DrawControlVisuals GUI.changed detected! EditingTextField=", DrawGUI.EditingTextField, ", Event type=", Event.current.type, ", rawType=", Event.current.type, ", isKey=", Event.current.isKey, ", isMouse=", Event.current.isMouse, ", keyCode=", Event.current.keyCode, ", character=", StringUtils.ToString(Event.current.character)));
				#endif

				if(ReadOnly)
				{
					return false;
				}

				if(!ValuesAreEqual(setValue, valueWas))
				{
					#if DEV_MODE && DEBUG_VALUE_CHANGED
					Debug.Log(ToString()+" DrawControlVisuals changed value from "+ valueWas + " to " + setValue);
					#endif

					Value = setValue;

					#if DEV_MODE && PI_ASSERTATIONS
					Debug.Assert(!MixedContent);
					#endif
				}
				else if(mixedContent)
				{
					var e = Event.current;

					// Try to detect if GUI.changed was caused by value being changed. Currently this does not work with delayed fields, would need to add an unappliedValue.
					if(Event.current.type == EventType.Used && !'\0'.Equals(e.character) && !IsDelayedField)
					{
						#if DEV_MODE && DEBUG_VALUE_CHANGED
						Debug.Log(StringUtils.ToColorizedString(ToString(), " DrawControlVisuals changed value from Mixed to "+ setValue + "? EditingTextField=", DrawGUI.EditingTextField, ", Event type=", e.type, ", rawType=", e.type, ", isKey=", e.isKey, ", isMouse=", e.isMouse, ", keyCode=", e.keyCode, ", character=", StringUtils.ToString(e.character)));
						#endif

						Value = setValue;
					}
				}
				
				return true;
			}
			GUI.changed = guiChangedWas;
			return false;
		}

		/// <inheritdoc />
		protected override void OnSelectedInternal(ReasonSelectionChanged reason, IDrawer previous, bool isMultiSelection)
		{
			if(!ReadOnly && DrawGUI.EditingTextField && !isMultiSelection)
			{
				editField = true;
			}
			base.OnSelectedInternal(reason, previous, isMultiSelection);
		}

		/// <inheritdoc />
		protected override void OnDeselectedInternal(ReasonSelectionChanged reason, IDrawer losingFocusTo)
		{
			if(DrawGUI.EditingTextField)
			{
				OnStoppedFieldEditing();
			}
			base.OnDeselectedInternal(reason, losingFocusTo);
			editField = false;
		}

		/// <inheritdoc cref="IDrawer.UpdateCachedValuesFromFieldsRecursively" />
		public override void UpdateCachedValuesFromFieldsRecursively()
		{
			if(DrawGUI.EditingTextField && SelectedAndInspectorHasFocus)
			{
				return;
			}

			if(DraggingPrefix)
			{
				return;
			}

			base.UpdateCachedValuesFromFieldsRecursively();
		}

		/// <inheritdoc cref="IDrawer.Dispose" />
		public override void Dispose()
		{
			base.Dispose();
			editField = false;
		}
	}
}