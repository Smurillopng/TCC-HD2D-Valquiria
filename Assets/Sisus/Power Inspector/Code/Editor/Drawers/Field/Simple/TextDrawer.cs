//#define DEBUG_SET_HEIGHT
#define DEBUG_ON_CLICK
//#define DEBUG_KEYBOARD_INPUT
//#define DEBUG_SET_AREA_MODE
#define DEBUG_START_EDITING_FIELD
#define DEBUG_STOP_EDITING_FIELD

using System;
using JetBrains.Annotations;
using Sisus.Attributes;
using UnityEngine;

namespace Sisus
{
	[Serializable, DrawerForField(typeof(string), false, true), DrawerForAttribute(true, typeof(TextAreaAttribute), typeof(string)), DrawerForAttribute(true, typeof(DelayedAttribute), typeof(string)), DrawerForAttribute(true, typeof(MultilineAttribute), typeof(string)), DrawerForAttribute(true, typeof(PMultilineAttribute), typeof(string)), DrawerForAttribute(true, typeof(PTextAreaAttribute), typeof(string))]
	public class TextDrawer : PrefixControlComboDrawer<string>, ITextFieldDrawer, IPropertyDrawerDrawer
	{
		private const float TextAreaMinRows = 4f;
		private const float MaxRows = 10f;

		private bool editField;
		protected bool textArea; // if true text field is drawn below the prefix, utilizing the whole width of the inspector view
		private bool forceAlwaysTextArea; // if true, text area is always true. otherwise, it depends on things like field input and available space for the text field
		private float height = DrawGUI.SingleLineHeight;
		private bool alwaysSingleLineHeight;
		private Rect textFieldDrawPosition; // draw position used for control when not drawing as a TextArea
		private Rect textAreaDrawPosition; // draw position used for control when drawing as a TextArea
		private TextFieldHeightDeterminant textFieldHeight;
		private bool parentDrawnInSingleRow;
		private bool delayed = false;

		/// <inheritdoc/>
		public bool RequiresPropertyDrawerType
		{
			get
			{
				return false;
			}
		}

		private static float MaxHeight
		{
			get
			{
				return MaxRows * DrawGUI.SingleLineHeight;
			}
		}

		private static float SwitchToAreaModeThreshold
		{
			get
			{
				return 4f * DrawGUI.SingleLineHeight;
			}
		}

		/// <inheritdoc cref="IDrawer.Height" />
		public override float Height
		{
			get
			{
				return height;
			}
		}

		private float MinHeight
		{
			get
			{
				return textArea ? TextAreaMinRows * DrawGUI.SingleLineHeight : DrawGUI.SingleLineHeight;
			}
		}

		private TextFieldHeightDeterminant HeightDeterminant
		{
			get
			{
				return textArea ? TextFieldHeightDeterminant.WordWrapping : textFieldHeight;
			}

			set
			{
				textFieldHeight = value;
			}
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="value"> The starting cached value of the drawer. </param>
		/// <param name="memberInfo"> LinkedMemberInfo for the field, property or parameter that the drawer represents. Can be null. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="readOnly"> True if control should be read only. </param>
		/// <param name="textArea"> True if text field has the TextArea attribute and should be shown in expanded mode. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static TextDrawer Create(string value, [CanBeNull]LinkedMemberInfo memberInfo, [CanBeNull]IParentDrawer parent, [CanBeNull]GUIContent label, bool readOnly = false, bool textArea = false, bool setDelayed = false)
		{
			TextDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new TextDrawer();
			}
			result.Setup(value, memberInfo, parent, label, readOnly, textArea, setDelayed);
			result.LateSetup();
			return result;
		}
		
		/// <inheritdoc cref="IFieldDrawer.SetupInterface" />
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			Setup(setValue as string, setMemberInfo, setParent, setLabel, setReadOnly, setMemberInfo != null && setMemberInfo.GetAttribute<TextAreaAttribute>() != null, setMemberInfo != null && setMemberInfo.GetAttribute<DelayedAttribute>() != null);
		}

		/// <inheritdoc/>
		public virtual void SetupInterface(object attribute, object setValue, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			bool setTextArea = attribute is TextAreaAttribute || attribute is MultilineAttribute || attribute is PTextAreaAttribute || attribute is PMultilineAttribute || (setMemberInfo != null && setMemberInfo.HasAnyAttribute<TextAreaAttribute, MultilineAttribute, PTextAreaAttribute, PMultilineAttribute>());
			bool setDelayed = attribute is DelayedAttribute || (setMemberInfo != null && setMemberInfo.GetAttribute<DelayedAttribute>() != null);
			Setup(setValue as string, setMemberInfo, setParent, setLabel, setReadOnly, setTextArea, setDelayed);
		}

		/// <inheritdoc/>
		protected sealed override void Setup(string setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			throw new NotSupportedException("Please use the other Setup method.");
		}

		protected virtual void Setup(string setValue, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly, bool setTextArea, bool setDelayed)
		{
			#if DEV_MODE
			if(setValue == null) { Debug.LogWarning(ToString(setLabel, setMemberInfo) + ".Setup called with null value"); }
			#endif

			base.Setup(setValue, typeof(string), setMemberInfo, setParent, setLabel, setReadOnly);

			delayed = setDelayed;

			PrefixResizeUtility.OnPrefixResizingFinished += OnPrefixResizingFinished;

			var inspector = InspectorUtility.ActiveInspector;
			inspector.State.OnWidthChanged += UpdateDynamicHeight;

			parentDrawnInSingleRow = setParent != null && setParent.DrawInSingleRow;

			#if DEV_MODE && PI_ASSERTATIONS
			inspector.OnNextLayout(()=>Debug.Assert(parentDrawnInSingleRow == (setParent != null && setParent.DrawInSingleRow), ToString()+".parentDrawnInSingleRow was "+parentDrawnInSingleRow+" but now is "+(setParent == null ? "null" : setParent.DrawInSingleRow.ToString())));
			#endif

			if(parentDrawnInSingleRow)
			{
				textArea = false;
				forceAlwaysTextArea = false;

				textFieldHeight = TextFieldHeightDeterminant.Constant;
			}
			else
			{
				// if parent is a collection and has the TextArea attribute, then set this as text area
				var parentCollection = setParent as ICollectionDrawer;
				if(parentCollection != null)
				{
					var parentMemberInfo = parent.MemberInfo;
					if(parentMemberInfo != null)
					{
						var parentTextAreaAttribute = parentMemberInfo.GetAttribute<TextAreaAttribute>();
						if(parentTextAreaAttribute != null)
						{
							setTextArea = true;
						}
					}
				}
				forceAlwaysTextArea = setTextArea;
				textArea = setTextArea;
			
				textFieldHeight = inspector.Preferences.textFieldHeight;
			}

			if(textFieldHeight == TextFieldHeightDeterminant.Constant)
			{
				alwaysSingleLineHeight = !textArea;
				height = MinHeight;
			}
			else
			{
				alwaysSingleLineHeight = false;
				if(textFieldHeight == TextFieldHeightDeterminant.WordWrapping)
				{
					height = MinHeight;
				}
				UpdateDynamicHeightWhenReady();
			}
		}

		/// <inheritdoc cref="IDrawer.UpdateCachedValuesFromFieldsRecursively" />
		public override void UpdateCachedValuesFromFieldsRecursively()
		{
			if(DrawGUI.EditingTextField && SelectedAndInspectorHasFocus)
			{
				return;
			}

			base.UpdateCachedValuesFromFieldsRecursively();
		}

		private void UpdateDynamicHeightWhenReady()
		{
			// If using dynamic height and draw positions haven't been determined yet, wait until next layout,
			// because they are needed in calculating the height when using word wrapping.
			if(!alwaysSingleLineHeight && lastDrawPosition.width <= 0f)
			{
				OnNextLayout(UpdateDynamicHeightWhenReady);
				return;
			}

			UpdateDynamicHeight();
		}

		private void OnPrefixResizingFinished(IUnityObjectDrawer subject, float newPrefixWidth)
		{
			InspectorUtility.ActiveInspector.OnNextLayout(UpdateDynamicHeight);
		}
		
		private void UpdateDynamicHeight()
		{
			if(alwaysSingleLineHeight)
			{
				if(!height.Equals(DrawGUI.SingleLineHeight))
				{
					height = DrawGUI.SingleLineHeight;
					GUI.changed = true;
				}
				return;
			}

			float setHeight;
			string text = Value;
			if(forceAlwaysTextArea)
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(controlLastDrawPosition.width > 0f, "TextDrawer controlLastDrawPosition.width zero: " + controlLastDrawPosition + " with Event="+StringUtils.ToString(Event.current));
				#endif

				setHeight = CalculateDynamicHeight(text, true, controlLastDrawPosition.width > 0f ? controlLastDrawPosition.width : Mathf.Infinity);
			}
			else if(HeightDeterminant == TextFieldHeightDeterminant.LineBreaks)
			{
				setHeight = CalculateDynamicHeight(text, false, Mathf.Infinity);
			}
			else //word wrapped
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(textFieldDrawPosition.width > 0f, "TextDrawer textFieldDrawPosition.width zero: " + textFieldDrawPosition + " with Event=" + StringUtils.ToString(Event.current));
				#endif

				//calculate height for non-text area
				setHeight = CalculateDynamicHeight(text, false, textFieldDrawPosition.width > 0f ? textFieldDrawPosition.width : Mathf.Infinity);
				
				//if height for non-text area is past threshold, calculate height using textArea
				if(setHeight >= SwitchToAreaModeThreshold)
				{
					if(!textArea)
					{
						SetTextAreaMode(true, false);

						//SetTextAreaMode calls UpdateDynamicHeight when ready, so stop here
						return;
					}

					#if DEV_MODE && PI_ASSERTATIONS
					Debug.Assert(textAreaDrawPosition.width > 0f);
					#endif

					setHeight = CalculateDynamicHeight(text, true, textAreaDrawPosition.width);
				}
				else if(textArea)
				{
					SetTextAreaMode(false, false);

					//SetTextAreaMode calls UpdateDynamicHeight when ready, so stop here
					return;
				}
			}
			
			if(!height.Equals(setHeight))
			{
				setHeight = Mathf.Clamp(setHeight, MinHeight, MaxHeight);
				if(!height.Equals(setHeight))
				{
					#if DEV_MODE && DEBUG_SET_HEIGHT
					Debug.Log(ToString()+".height = "+setHeight);
					#endif
					height = setHeight;
					GUI.changed = true;
				}
			}
		}

		private static float CalculateDynamicHeight(string text, bool forTextArea, float textAreaWidth)
		{
			if(textAreaWidth <= 0f)
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.LogError("CalculateDynamicHeight called with textAreaWidth "+textAreaWidth);
				#endif
				textAreaWidth = Mathf.Infinity;
			}

			try
			{
				float setHeight = InspectorUtility.Preferences.GUISkin.textArea.CalcHeight(GUIContentPool.Temp(text), textAreaWidth);
				return setHeight + (forTextArea ? DrawGUI.SingleLineHeight + 3f : 3f);
			}
			#if DEV_MODE
			catch(Exception e)
			{
				Debug.LogError(e);
				if(InspectorUtility.Preferences == null)
				{
					Debug.LogError("CalculateDynamicHeight: InspectorUtility.Preferences was null - "+ e);
				}
				else if(InspectorUtility.Preferences.GUISkin == null)
				{
					Debug.LogError("CalculateDynamicHeight: InspectorUtility.Preferences.GUISkin was null - "+ e);
				}
				else if(InspectorUtility.Preferences.GUISkin.textArea == null)
				{
					Debug.LogError("CalculateDynamicHeight: InspectorUtility.Preferences.GUISkin.textArea was null - "+ e);
				}
				else if(text == null)
				{
					Debug.LogError("CalculateDynamicHeight: text was null - "+ e);
				}
				else
				{
					Debug.LogError("CalculateDynamicHeight - "+ e);
				}
			#else
			catch(Exception)
			{
			#endif

				return DrawGUI.SingleLineHeight + (forTextArea ? DrawGUI.SingleLineHeight + 3f : 3f);
			}
		}

		private void SetTextAreaMode(bool setIsTextArea, bool forceStickOn)
		{
			if(textArea != setIsTextArea)
			{
				#if DEV_MODE && DEBUG_SET_AREA_MODE
				Debug.Log(ToString()+".textArea = "+StringUtils.ToColorizedString(setIsTextArea));
				#endif

				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(!setIsTextArea || !parentDrawnInSingleRow);
				#endif

				textArea = setIsTextArea;
				if(forceStickOn)
				{
					forceAlwaysTextArea = textArea;
				}
				alwaysSingleLineHeight = !setIsTextArea && HeightDeterminant == TextFieldHeightDeterminant.Constant;
				InspectorUtility.ActiveInspector.OnNextLayout(UpdateDynamicHeight);
			}
		}

		/// <inheritdoc/>
		protected override void OnCachedValueChanged(bool applyToField, bool updateMembers)
		{
			UpdateDynamicHeight();
			base.OnCachedValueChanged(applyToField, updateMembers);
		}

		/// <inheritdoc cref="IDrawer.OnKeyboardInputGiven" />
		public override bool OnKeyboardInputGiven(Event inputEvent, KeyConfigs keys)
		{
			#if DEV_MODE && DEBUG_KEYBOARD_INPUT
			Debug.Log(StringUtils.ToColorizedString(ToString(), ".OnKeyboardInputGiven(", inputEvent, ") with DrawGUI.EditingTextField=", DrawGUI.EditingTextField, ")"));
			#endif

			if(DrawGUI.EditingTextField)
			{
				switch(inputEvent.keyCode)
				{
					case KeyCode.Escape:
					if(!ReadOnly)
					{
						DrawGUI.Use(inputEvent);
						StopEditingField();
						return true;
					}
					return false;
					case KeyCode.Return:
					case KeyCode.KeypadEnter:
					{
						if(inputEvent.modifiers == EventModifiers.None)
						{
							if(!textArea)
							{
								DrawGUI.Use(inputEvent);
								StopEditingField();
								return true;
							}
							return false;
						}

						var textEditor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
						int cursorIndex = textEditor.cursorIndex;
						textEditor.text = string.Concat(textEditor.text.Substring(0, cursorIndex), Environment.NewLine, textEditor.text.Substring(cursorIndex));
						textEditor.cursorIndex += Environment.NewLine.Length;
						return true;
					}
				}

				if(keys.DetectTextFieldReservedInput(inputEvent, lastDrawPosition.height > DrawGUI.SingleLineHeight ? TextFieldType.TextArea : TextFieldType.TextRow))
				{
					#if DEV_MODE
					Debug.Log(GetType().Name + ".OnKeyboardInputGiven aborting because detected text field reserved input\ntextArea="+textArea+"\nInput="+StringUtils.ToString(inputEvent));
					#endif

					return false;
				}
				return base.OnKeyboardInputGiven(inputEvent, keys);
			}

			switch(inputEvent.keyCode)
			{
				case KeyCode.Return:
				case KeyCode.KeypadEnter:
					if(inputEvent.modifiers == EventModifiers.None && !ReadOnly)
					{
						DrawGUI.Use(inputEvent);
						//this needs to be delayed or the text field's content can get replaced by a new line character
						InspectorUtility.ActiveInspector.OnNextLayout(()=>InspectorUtility.ActiveInspector.OnNextLayout(StartEditingField));
						DrawGUI.Use(inputEvent);
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
				#if DEV_MODE
				case KeyCode.U:
					if(Event.current.modifiers == EventModifiers.Control && !ReadOnly)
					{
						if(MixedContent)
						{
							Debug.LogWarning("Converting mixed content to upper case not yet supported...");
							return true;
						}

						if(string.IsNullOrEmpty(Value))
						{
							return true;
						}

						GUI.changed = true;
						DrawGUI.Use(inputEvent);
						Value = Value.ToUpper();
						return true;
					}
					return false;
				case KeyCode.L:
					if(Event.current.modifiers == EventModifiers.Control && !ReadOnly)
					{
						if(MixedContent)
						{
							Debug.LogWarning("Converting mixed content to lower case not yet supported...");
							return true;
						}

						if(string.IsNullOrEmpty(Value))
						{
							return true;
						}

						GUI.changed = true;
						DrawGUI.Use(inputEvent);
						Value = Value.ToLower();
						ApplyValueToField();
						return true;
					}
					return false;
				#endif
				default:
					if(inputEvent.character != 0 && inputEvent.modifiers == EventModifiers.None && !ReadOnly)
					{
						switch(inputEvent.character)
						{
							//TO DO: Don't append tab, return etc. at the end of a field
							case '\t':
							case '\n':
							case '\r':
								//break;
								//UPDATE: with only a break here,
								//it was causing the field to get selected again
								//immediately after being deselected
								//via the return or enter keys
								return false;
							default:
								Value = string.Concat(Value, inputEvent.character.ToString());
								break;
						}
						GUI.changed = true;
						DrawGUI.Use(inputEvent);
						StartEditingField();

						#if UNITY_EDITOR
						TextEditorUtility.MoveCursorToTextEditorEnd();
						#endif

						return true;
					}
					break;
			}
			
			return base.OnKeyboardInputGiven(inputEvent, keys);
		}

		/// <inheritdoc cref="IDrawer.DefaultValue" />
		public override object DefaultValue(bool _)
		{
			return "";
		}

		public void StartEditingField()
		{
			if(!editField)
			{
				#if DEV_MODE && DEBUG_START_EDITING_FIELD
				Debug.Log(ToString()+".StartEditingField()");
				#endif

				if(!this.CanEditField())
				{
					#if DEV_MODE
					Debug.LogWarning(Msg(ToString(), ".StartEditingField called but CanStartEditing() returned ", false, ", with ReadOnly=", ReadOnly, "HasMultiSelectedControls=", InspectorUtility.ActiveManager.HasMultiSelectedControls));
					#endif
					return;
				}

				//when field is given focus, also set text field editing mode true
				editField = true;
				DrawGUI.EditingTextField = true;
				FocusControlField();
			}
		}

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
			
			editField = false;
			focusField = 0;
			KeyboardControlUtility.KeyboardControl = 0;
			DrawGUI.EditingTextField = false;

			//this needs to be delayed or Unity can internally start editing the text again immediately e.g. when return is pressed
			InspectorUtility.ActiveInspector.OnNextLayout(StopEditingFieldStep);
		}

		private void StopEditingFieldStep()
		{
			if(Selected)
			{
				DrawGUI.EditingTextField = false;
			}
		}

		/// <inheritdoc />
		protected override void OnControlClicked(Event inputEvent)
		{
			#if DEV_MODE && UNITY_EDITOR && DEBUG_ON_CLICK
			Debug.Log(StringUtils.ToColorizedString(ToString(), ".OnControlClicked(", StringUtils.ToString(inputEvent), ") with Selected=", Selected, ", KeyboardControl=", KeyboardControlUtility.KeyboardControl, ", controlId=", controlId, ", ReadOnly=", ReadOnly, ", EditingTextField=", DrawGUI.EditingTextField, ", MultiSelectedControls = ", InspectorUtility.ActiveManager.HasMultiSelectedControls));
			InspectorUtility.ActiveInspector.OnNextLayout(()=>Debug.Log(StringUtils.ToColorizedString("now: EditorGUIUtility.editingTextField=", UnityEditor.EditorGUIUtility.editingTextField, ", DrawGUI.EditingTextField=", DrawGUI.EditingTextField)));
			#endif

			TextFieldUtility.OnControlClicked(this, inputEvent);
		}

		/// <inheritdoc />
		protected override void GetDrawPositions(Rect position)
		{
			lastDrawPosition = position;
			lastDrawPosition.height = Height;

			lastDrawPosition.GetLabelAndControlRects(label, out labelLastDrawPosition, out textFieldDrawPosition);

			textAreaDrawPosition = lastDrawPosition;
			DrawGUI.AddMarginsAndIndentation(ref textAreaDrawPosition);
			DrawGUI.RemoveFirstLine(ref textAreaDrawPosition);
			
			if(textArea)
			{
				labelLastDrawPosition = lastDrawPosition;
				DrawGUI.AddMargins(ref labelLastDrawPosition);
				labelLastDrawPosition.height = DrawGUI.SingleLineHeight;
				controlLastDrawPosition = textAreaDrawPosition;
			}
			else
			{
				controlLastDrawPosition = textFieldDrawPosition;
			}

			localDrawAreaOffset = DrawGUI.GetLocalDrawAreaOffset();
		}

		/// <inheritdoc cref="IDrawer.Draw" />
		public override bool Draw(Rect position)
		{
			if(textArea)
			{
				// text areas are drawn on top of the prefix resizer control,
				// so we need to draw a rect the color of the background behind
				// the control to avoid ugly clipping
				DrawGUI.Active.ColorRect(position, DrawGUI.Active.InspectorBackgroundColor);
			}
			return base.Draw(position);
		}

		/// <inheritdoc />
		public override bool DrawBody(Rect position)
		{
			HandleDrawHintIcon(position);

			DrawDragBarIfReorderable();

			var guiChangedWas = GUI.changed;
			GUI.changed = false;
			bool changed;
			string valueWas = Value;
			string setValue;

			DrawerUtility.BeginInputField(this, controlId, ref editField, ref focusField, MixedContent);
			{
				setValue = DrawControlVisuals(controlLastDrawPosition, valueWas);
				changed = GUI.changed;
			}
			DrawerUtility.EndInputField();

			if(changed)
			{
				#if DEV_MODE && DEBUG_VALUE_CHANGED
				Debug.Log(ToString()+" DrawControlVisuals GUI.changed detected! EditingTextField="+DrawGUI.EditingTextField);
				#endif

				// When MixedContent is true and GUI.changed becomes true during DrawControlVisuals, we assume
				// that the value has been changed by the user, even if valueWas equals the new value. This is because it is possible that
				// the user happens to enters a value that happens to match that of the passed cached value, in which case
				// if we only used an ValuesAreEqual, it would return false, and the values of all targets would not get unified.
				if(!ReadOnly && (MixedContent || !string.Equals(valueWas, setValue)))
				{
					#if DEV_MODE && DEBUG_VALUE_CHANGED
					Debug.Log(ToString()+" DrawControlVisuals changed value from "+StringUtils.ToString(setValue)+" to "+StringUtils.ToString(valueWas));
					#endif

					Value = setValue;
					return true;
				}
			}
			GUI.changed = guiChangedWas;
			return false;
		}

		/// <inheritdoc />
		public override string DrawControlVisuals(Rect position, string inputValue)
		{
			if(inputValue == null)
			{
				var setValue = DrawGUI.Active.TextField(position, "", delayed);
				if(!string.IsNullOrEmpty(setValue))
				{
					return setValue;
				}

				if(!Selected || !DrawGUI.EditingTextField)
				{
					bool guiWasEnabled = GUI.enabled;
					GUI.enabled = false;

					var nullPosition = position;
					nullPosition.x += 1f;
					nullPosition.y += 1f;
					GUI.Label(nullPosition, "null");

					GUI.enabled = guiWasEnabled;
				}

				return null;
			}

			const float TwoLinesHeight = DrawGUI.SingleLineHeightWithoutPadding * 2f;
			bool isMultiLineField = position.height >= TwoLinesHeight;
			if(isMultiLineField)
			{
				inputValue = DrawGUI.Active.TextArea(position, inputValue, HeightDeterminant == TextFieldHeightDeterminant.WordWrapping);
				return inputValue;
			}

			return DrawGUI.Active.TextField(position, inputValue, delayed);
		}

		/// <inheritdoc />
		protected override void OnSelectedInternal(ReasonSelectionChanged reason, IDrawer previous, bool isMultiSelection)
		{
			if(!isMultiSelection)
			{
				FocusControlField();
			}
		}

		/// <inheritdoc />
		protected override void OnDeselectedInternal(ReasonSelectionChanged reason, IDrawer losingFocusTo)
		{
			editField = false;
			base.OnDeselectedInternal(reason, losingFocusTo);
		}

		/// <inheritdoc cref="IDrawer.OnMouseover" />
		public override void OnMouseover()
		{
			// UPDATE: Don't tint the text field on mouseover when it's being edited
			if(MouseOverPart == PrefixedControlPart.Control && Selected && DrawGUI.EditingTextField)
			{
				return;
			}
			base.OnMouseover();
		}

		/// <inheritdoc />
		public override void DrawFilterHighlight(SearchFilter filter, Color color)
		{
			if(lastPassedFilterTestType == FilterTestType.Value)
			{
				DrawGUI.DrawControlFilteringEffect(ControlPosition, color, localDrawAreaOffset);
			}
		}

		/// <inheritdoc/>
		protected override void BuildContextMenu(ref Menu menu, bool extendedMenu)
		{
			if(BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}

			if(!parentDrawnInSingleRow)
			{
				menu.AddSeparatorIfNotRedundant();

				if(!forceAlwaysTextArea)
				{
					menu.Add("Height Expanding/Disabled", SetConstantHeight, textFieldHeight == TextFieldHeightDeterminant.Constant);
					menu.Add("Height Expanding/Line Breaks Only", SetLineBreakBasedHeight, textFieldHeight == TextFieldHeightDeterminant.LineBreaks);
					menu.Add("Height Expanding/Word Wrap", SetWordWrappedHeight, textFieldHeight == TextFieldHeightDeterminant.WordWrapping);
				}

				menu.Add("Text Area", ToggleTextAreaMode, forceAlwaysTextArea);
			}
			
			if(!ReadOnly)
			{
				menu.AddSeparatorIfNotRedundant();
				var value = Value;
				if(string.IsNullOrEmpty(value))
				{
					menu.AddDisabled("Uppercase");
					menu.AddDisabled("Lowercase");
				}
				else
				{
					var upper = value.ToUpper();
					if(string.Equals(value, upper))
					{
						menu.AddDisabled("Uppercase");
					}
					else
					{
						menu.Add("Uppercase", () => Value = upper);
					}

					var lower = value.ToLower();
					if(string.Equals(value, lower))
					{
						menu.AddDisabled("Lowercase");
					}
					else
					{
						menu.Add("Lowercase", () => Value = lower);
					}
				}
			}

			if(!BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}
		}

		private void ToggleTextAreaMode()
		{
			SetTextAreaMode(!textArea, true);
		}

		private void SetConstantHeight()
		{
			SetHeightDeterminant(TextFieldHeightDeterminant.Constant);
		}

		private void SetLineBreakBasedHeight()
		{
			SetHeightDeterminant(TextFieldHeightDeterminant.LineBreaks);
		}

		private void SetWordWrappedHeight()
		{
			SetHeightDeterminant(TextFieldHeightDeterminant.WordWrapping);
		}

		private void SetHeightDeterminant(TextFieldHeightDeterminant setValue)
		{
			HeightDeterminant = setValue;

			forceAlwaysTextArea = false;
			alwaysSingleLineHeight = /*!forceAlwaysTextArea && */setValue == TextFieldHeightDeterminant.Constant;

			InspectorUtility.ActiveManager.OnNextLayout(UpdateDynamicHeight);
		}

		/// <inheritdoc cref="IDrawer.Dispose" />
		public override void Dispose()
		{
			PrefixResizeUtility.OnPrefixResizingFinished -= OnPrefixResizingFinished;
			InspectorUtility.ActiveInspector.State.OnWidthChanged -= UpdateDynamicHeight;
			
			editField = false;
			textArea = false;
			forceAlwaysTextArea = false;
			height = DrawGUI.SingleLineHeight;
			alwaysSingleLineHeight = false;
			textFieldDrawPosition.width = 0f;
			textAreaDrawPosition.width = 0f;
			textFieldHeight = default(TextFieldHeightDeterminant);
			parentDrawnInSingleRow = false;
			delayed = false;

			base.Dispose();
		}

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

		/// <inheritdoc />
		protected override string GetRandomValue()
		{
			return RandomUtils.String(0, 100);
		}
	}
}