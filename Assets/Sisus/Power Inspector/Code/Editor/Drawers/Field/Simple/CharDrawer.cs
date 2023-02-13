#define DEBUG_START_EDITING_FIELD
#define DEBUG_VALUE_CHANGED

using System;
using Sisus.Attributes;
using UnityEngine;

namespace Sisus
{
	[Serializable, DrawerForField(typeof(char), false, true)]
	public class CharDrawer : PrefixControlComboDrawer<char>
	{
		private bool editField;

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="value"> The starting cached value of the drawer. </param>
		/// <param name="memberInfo"> LinkedMemberInfo for the field, property or parameter that the drawer represents. Can be null. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="readOnly"> True if control should be read only. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static CharDrawer Create(char value, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool readOnly)
		{
			CharDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new CharDrawer();
			}
			result.Setup(value, typeof(char), memberInfo, parent, label, readOnly);
			result.LateSetup();
			return result;
		}
		
		/// <inheritdoc />
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			Setup((char)setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc />
		protected override void Setup(char setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			base.Setup(setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
			OnValueChanged += OnValueChangedOverride;
		}
		
		private void OnValueChangedOverride(IDrawer changed, object value)
		{
			#if DEV_MODE
			Debug.Log(ToString() + ".OnValueChangedOverride("+StringUtils.ToString(value)+")");
			#endif

			OnNextLayout(TruncateAndSelectAllTextIfEditing);
		}

		/// <summary>
		/// Makes sure that all text is always selected and exactly one character long when editing.
		/// </summary>
		private void TruncateAndSelectAllTextIfEditing()
		{
			#if DEV_MODE
			Debug.Log(StringUtils.ToColorizedString(ToString(), ".SelectAllTextIfEditingText with EditingTextField=", DrawGUI.EditingTextField, ", Selected=", Selected, ", ReadOnly=", ReadOnly));
			#endif

			#if UNITY_EDITOR
			//not supported at runtime
			if(Selected && DrawGUI.EditingTextField && !ReadOnly)
			{
				TextEditorUtility.SetText(new string(new char[]{ Value }));
				TextEditorUtility.SelectAllText();
				
				GUI.changed = true;
			}
			#endif			
		}

		/// <inheritdoc />
		public override bool OnKeyboardInputGiven(Event inputEvent, KeyConfigs keys)
		{
			InspectorUtility.ActiveInspector.OnNextLayout(TruncateAndSelectAllTextIfEditing);

			switch(inputEvent.keyCode)
			{
				case KeyCode.Escape:
				{
					if(DrawGUI.EditingTextField)
					{
						GUI.changed = true;
						KeyboardControlUtility.KeyboardControl = 0;
						DrawGUI.EditingTextField = false;
						DrawGUI.Use(inputEvent);
						return true;
					}
					return false;
				}
				case KeyCode.Return:
				case KeyCode.KeypadEnter:
					if(inputEvent.modifiers == EventModifiers.None && !ReadOnly)
					{
						GUI.changed = true;
						DrawGUI.Use(inputEvent);

						if(!DrawGUI.EditingTextField)
						{
							StartEditingField();
						}
						else
						{
							KeyboardControlUtility.KeyboardControl = 0;
							DrawGUI.EditingTextField = false;
						}
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
						GUI.changed = true;
						DrawGUI.Use(inputEvent);
						Value = char.ToUpper(Value);
						return true;
					}
					return false;
				case KeyCode.L:
					if(Event.current.modifiers == EventModifiers.Control && !ReadOnly)
					{
						GUI.changed = true;
						DrawGUI.UseEvent();
						Value = char.ToLower(Value);
						ApplyValueToField();
						return true;
					}
					return false;
				case KeyCode.UpArrow:
					if(inputEvent.modifiers == EventModifiers.FunctionKey)
					{
						DrawGUI.Use(inputEvent);
						SelectNextFieldUp(GetSelectedRowIndex());
						return true;
					}
					return false;
				case KeyCode.DownArrow:
					if(inputEvent.modifiers == EventModifiers.FunctionKey)
					{
						DrawGUI.Use(inputEvent);
						SelectNextFieldDown(GetSelectedRowIndex());
						return true;
					}
					return false;
				case KeyCode.LeftArrow:
					if(inputEvent.modifiers == EventModifiers.FunctionKey)
					{
						DrawGUI.Use(inputEvent);
						SelectNextFieldLeft(false, false);
						return true;
					}
					return false;
				case KeyCode.RightArrow:
					#if DEV_MODE
					Debug.Log(StringUtils.ToColorizedString(ToString(), " RightArrow with modifiers=", StringUtils.ToString(inputEvent.modifiers)));
					#endif
					if(inputEvent.modifiers == EventModifiers.FunctionKey)
					{
						DrawGUI.Use(inputEvent);
						SelectNextFieldRight(false, false);
						return true;
					}
					return false;
				#endif
				default:
					if(inputEvent.character != 0 && inputEvent.modifiers == EventModifiers.None && !ReadOnly)
					{
						switch(inputEvent.character)
						{
							//TO DO: Don't insert tab, return etc.
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
								Value = inputEvent.character;
								break;
						}
						GUI.changed = true;
						DrawGUI.Use(inputEvent);
						StartEditingField();
						return true;
					}
					break;
			}

			if(DrawGUI.EditingTextField && keys.DetectTextFieldReservedInput(inputEvent, TextFieldType.TextRow))
			{
				#if DEV_MODE
				Debug.Log(GetType().Name + ".OnKeyboardInputGiven aborting because detected text field reserved input\nInput="+StringUtils.ToString(inputEvent));
				#endif

				return false;
			}

			if(base.OnKeyboardInputGiven(inputEvent, keys))
			{
				return true;
			}
			
			// Prevent keyboard events from bleeding elsewhere. E.g. when F was pressed, would cause Scene view to focus to selected Object.
			DrawGUI.Use(inputEvent);
			return true;
		}

		public void OnStartedFieldEditing() { }
		public void OnStoppedFieldEditing() { }

		/// <inheritdoc />
		public override object DefaultValue(bool _)
		{
			return default(char); //'\0'
		}

		private void StartEditingField()
		{
			#if DEV_MODE && DEBUG_START_EDITING_FIELD
			Debug.Log(GetType().Name + ".StartEditingField with ReadOnly="+ ReadOnly+", HasMultiSelectedControls="+ InspectorUtility.ActiveManager.HasMultiSelectedControls);
			#endif

			#if DEV_MODE
			Debug.Assert(!InspectorUtility.ActiveManager.HasMultiSelectedControls);
			Debug.Assert(Selected);
			#endif

			if(!ReadOnly && !InspectorUtility.ActiveManager.HasMultiSelectedControls)
			{
				//when field is given focus, also set text field editing mode true
				editField = true;
				DrawGUI.EditingTextField = true;
				FocusControlField();
			}
		}

		/// <inheritdoc />
		protected override void OnControlClicked(Event inputEvent)
		{
			#if DEV_MODE
			Debug.Log(ToString() + ".OnControlClicked(" + StringUtils.ToString(inputEvent) +") with Selected="+Selected+", EditingTextField="+DrawGUI.EditingTextField);
			#endif
			DrawGUI.Use(inputEvent);
			HandleOnClickSelection(inputEvent, ReasonSelectionChanged.ControlClicked);
			StartEditingField();
		}

		/// <inheritdoc />
		public override bool DrawBody(Rect position)
		{
			HandleDrawHintIcon(position);

			DrawDragBarIfReorderable();

			var guiChangedWas = GUI.changed;
			GUI.changed = false;
			bool guiChanged;
			bool mixedContent = MixedContent;
			const char valueDuringMixedContent = '\0';
			var valueWas = mixedContent ? valueDuringMixedContent : Value;
			char setValue;

			DrawerUtility.BeginInputField(this, controlId, ref editField, ref focusField, memberInfo == null ? false : memberInfo.MixedContent);
			{
				setValue = DrawControlVisuals(controlLastDrawPosition, valueWas);
				guiChanged = GUI.changed;
			}
			DrawerUtility.EndInputField();

			if(guiChanged)
			{
				#if DEV_MODE && DEBUG_VALUE_CHANGED
				var e = Event.current;
				Debug.Log(StringUtils.ToColorizedString(ToString(), " DrawControlVisuals GUI.changed detected! valueWas=", valueWas, ", setValue=", setValue, " EditingTextField =", DrawGUI.EditingTextField, ", Event type=", e.type, ", rawType=", e.type, ", isKey=", e.isKey, ", isMouse=", e.isMouse, ", keyCode=", e.keyCode, ", character=", StringUtils.ToString(e.character)));
				#endif

				if(ReadOnly)
				{
					return false;
				}

				if(!valueWas.Equals(setValue))
				{
					#if DEV_MODE && DEBUG_VALUE_CHANGED
					Debug.Log(ToString()+" DrawControlVisuals changed value from "+ valueWas + " to" + setValue);
					#endif

					Value = setValue;
					return true;
				}
			}

			return false;
		}
		
		/// <inheritdoc />
		public override char DrawControlVisuals(Rect position, char inputValue)
		{
			var setValueInString = new string(inputValue, 1);
			
			// There was an issue where TextField would cause EditingTextField to go false after certain keyboard inputs.
			// To avoid this, will manually restore EditingTextField if this happens.
			bool editingTextFieldWas = DrawGUI.EditingTextField;

			setValueInString = DrawGUI.Active.TextField(position, setValueInString);

			if(DrawGUI.EditingTextField != editingTextFieldWas)
			{
				#if DEV_MODE
				Debug.LogWarning("Restoring DrawGUI.EditingTextField to " + editingTextFieldWas + " after DrawGUI.Active.TextField changed it");
				#endif

				#if UNITY_EDITOR
				UnityEditor.EditorGUIUtility.editingTextField = editingTextFieldWas;
				#endif

				if(editingTextFieldWas)
				{
					StartEditingField();
				}
			}
			
			if(setValueInString.Length == 0)
			{
				return default(char);
			}
			return setValueInString[0];
		}

		/// <inheritdoc />
		protected override void OnSelectedInternal(ReasonSelectionChanged reason, IDrawer previous, bool isMultiSelection)
		{
			FocusControlField();
		}

		/// <inheritdoc />
		protected override void OnDeselectedInternal(ReasonSelectionChanged reason, IDrawer losingFocusTo)
		{
			editField = false;
			base.OnDeselectedInternal(reason, losingFocusTo);
		}

		/// <inheritdoc />
		protected override void BuildContextMenu(ref Menu menu, bool extendedMenu)
		{
			if(BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}

			if(!ReadOnly)
			{
				menu.AddSeparatorIfNotRedundant();
				
				var val = Value;
				var upper = char.ToUpper(val);
				menu.Add("To Uppercase", ()=> Value = upper, val == upper);
				var lower = char.ToLower(Value);
				menu.Add("To Lowercase", ()=> Value = lower, val == lower);
				
				menu.AddSeparator();

				menu.Add("Set To.../Null (\\0)", () => Value = '\0', val == '\0');
				menu.Add("Set To.../Tab (\\t)", () => Value = '\t', val == '\t');
				menu.Add("Set To.../Line Feed (\\n)", () => Value = '\n', val == '\n');
				menu.Add("Set To.../Carriage Return (\\r)", () => Value = '\r', val == '\r');
			}

			if(!BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}
		}

		/// <inheritdoc />
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
		protected override char GetRandomValue()
		{
			return RandomUtils.Char();
		}

		/// <inheritdoc />
		public override void Dispose()
		{
			editField = false;
			base.Dispose();
		}
	}
}