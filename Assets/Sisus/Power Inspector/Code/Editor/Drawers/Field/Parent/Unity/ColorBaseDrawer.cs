#define DEBUG_APPLY_VALUE
#define DEBUG_DRAG_MOUSEOVER
#define DEBUG_UNAPPLIED_CHANGES
#define DEBUG_COLOR_PICKER
#define DEBUG_EYE_DROPPER
#define DEBUG_KEYBOARD_INPUT

using System;
using UnityEngine;

namespace Sisus
{
	/// <summary>
	/// Drawer representing Color32 and Color fields.
	/// </summary>
	public abstract class ColorBaseDrawer<TColor> : PrefixControlComboDrawer<TColor> where TColor : struct
	{
		private bool hasUnappliedChanges;
		private Color valueUnapplied;

		private bool eyeDropToolMouseovered;

		private bool listeningForColorPickerClosed;
		private bool listeningForStoppedUsingEyeDropper;

		/// <inheritdoc/>
		public override Part MouseoveredPart
		{
			get
			{
				return eyeDropToolMouseovered ? Part.Eyedropper : base.MouseoveredPart;
			}
		}

		/// <inheritdoc/>
		public override Type Type
		{
			get
			{
				return typeof(TColor);
			}
		}
		
		private Rect EyeDropToolRect
		{
			get
			{
				var rect = controlLastDrawPosition;
				rect.x += rect.width - 20f;
				rect.width = 20f;
				return rect;

			}
		}

		private Rect ColorRectPosition
		{
			get
			{
				var rect = controlLastDrawPosition;
				rect.width -= 20f;
				return rect;
			}
		}

		/// <inheritdoc />
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			Setup((TColor)setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc/>
		protected override void Setup(TColor setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			valueUnapplied = ToColor(setValue);
			base.Setup(setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		private Color ToColor(TColor colorOrColor32)
		{
			return ToColor((object)colorOrColor32);
		}

		private Color ToColor(object colorOrColor32)
		{
			return colorOrColor32 is Color ? (Color)colorOrColor32 : (Color)(Color32)colorOrColor32;
		}

		private Color32 ToColor32(object colorOrColor32)
		{
			return colorOrColor32 is Color32 ? (Color32)colorOrColor32 : (Color32)(Color)colorOrColor32;
		}

		private TColor FromColor(Color color)
		{
			return Type == Types.Color ? (TColor)(object)color : (TColor)(object)(Color32)color;
		}

		private TColor FromColor(object color)
		{
			return Type == Types.Color ? (TColor)color : (TColor)(object)(Color32)(Color)color;
		}

		/// <inheritdoc/>
		protected override bool DoSetValue(TColor setValue, bool applyToField, bool updateMembers)
		{
#if DEV_MODE //TEMP
if(valueUnapplied != ToColor(setValue)) { Debug.Log(ToString()+".DoSetValue("+setValue+", applyToField="+applyToField+")"); }
else Debug.Log(ToString()+".DoSetValue("+setValue+", applyToField="+applyToField+")");
#endif

			valueUnapplied = ToColor(setValue);
			SetHasUnappliedChanges(false);

			return base.DoSetValue(setValue, applyToField, updateMembers);
		}
		
		public override void UpdateCachedValuesFromFieldsRecursively()
		{
			#if UNITY_EDITOR
			// Don't update cached values while color picker is open.
			if(ColorPicker.IsOpen)
			{
				return;
			}

			// Don't update cached values while using eye dropper.
			if(ColorPicker.UsingEyeDropper)
			{
				return;
			}
			#endif

			//Don't update cached values while values picked using color picker
			//haven't been applied yet
			if(hasUnappliedChanges)
			{
				return;
			}

			base.UpdateCachedValuesFromFieldsRecursively();
			
			valueUnapplied = ToColor(GetValue());
		}

		/// <summary>
		/// Just Draw the control with current value and return changes made to the value via the control,
		/// without fancy features like data validation color coding
		/// </summary>
		public override TColor DrawControlVisuals(Rect position, TColor inputValue)
		{
			var setValueUnapplied = DrawGUI.Active.ColorField(position, valueUnapplied);
			
			if(setValueUnapplied != valueUnapplied)
			{
				#if DEV_MODE && (DEBUG_COLOR_PICKER || DEBUG_EYE_DROPPER)
				Debug.Log(Msg("valueUnapplied = ", setValueUnapplied," (was: ", valueUnapplied, ") with inputValue=", inputValue, ", Value=", Value, ", Event=", Event.current));
				#endif
				valueUnapplied = setValueUnapplied;
				SetHasUnappliedChanges(ToColor(inputValue) != valueUnapplied);
			}
			
			#if UNITY_EDITOR
			if(ColorPicker.IsOpen)
			{
				//don't apply changes while the color picker is open
				//so that we can e.g. revert back to previous value when escape is pressed
				return inputValue;
			}
			#endif
			
			if(hasUnappliedChanges)
			{
				if(Event.current.type == EventType.ExecuteCommand && string.Equals(Event.current.commandName, "EyeDropperClicked", StringComparison.OrdinalIgnoreCase))
				{
					switch(Event.current.commandName)
					{
						case "EyeDropperClicked":
							#if DEV_MODE
							Debug.Log("EyeDropperClicked with hasUnappliedChanges="+ hasUnappliedChanges);
							#endif
							ApplyUnappliedChanges();
							return inputValue;
						case "EyeDropperCancelled":
							#if DEV_MODE
							Debug.Log("EyeDropperCancelled with hasUnappliedChanges=" + hasUnappliedChanges);
							#endif
							DiscardUnappliedChanges();
							return inputValue;
					}
				}

				switch(Event.current.keyCode)
				{
					//the color picker was closed using the esc key: discard the value
					case KeyCode.Escape:
						DiscardUnappliedChanges();
						return inputValue;
					//the color was picked using enter or return key: apply the value
					case KeyCode.KeypadEnter:
					case KeyCode.Return:
						ApplyUnappliedChanges();
						return inputValue;
				}

				#if UNITY_EDITOR
				if(ColorPicker.UsingEyeDropper)
				{
					return inputValue;
				}
				#endif

				//if no other applicable KeyCodes were detected until the next time the mouse was moved, then it's
				//safe to assume that the user closed the color picker either by double clicking an color in the view
				//or by clicking off-window and thus causing the window to close. In both instances the value should be applied.
				if(Event.current.isMouse)
				{
					ApplyUnappliedChanges();
					return inputValue;
				}
			}
			#if DEV_MODE && PI_ASSERTATIONS
			else if(Event.current.type == EventType.ExecuteCommand && string.Equals(Event.current.commandName, "EyeDropperClicked", StringComparison.OrdinalIgnoreCase))
			{ Debug.LogWarning(Msg("EyeDropperClicked with hasUnappliedChanges=", hasUnappliedChanges,", inputValue=", inputValue, ", valueUnapplied = ", valueUnapplied, ", Value=", Value, ", UsingEyeDropper=", ColorPicker.UsingEyeDropper)); }
			#endif

			return inputValue;
		}

		/// <inheritdoc/>
		public override bool OnKeyboardInputGiven(Event inputEvent, KeyConfigs keys)
		{
			#if DEV_MODE && DEBUG_KEYBOARD_INPUT
			Debug.Log(GetType().Name + ".OnKeyboardInputGiven(" + inputEvent.keyCode + ") with DrawGUI.EditingTextField=" + DrawGUI.EditingTextField);
			#endif

			switch(inputEvent.keyCode)
			{
				case KeyCode.Escape:
					if(hasUnappliedChanges)
					{
						#if DEV_MODE && DEBUG_APPLY_VALUE
						Debug.Log(GetType().Name+" - Discarding unapplied value "+ StringUtils.TypeToString(valueUnapplied) + " because esc was pressed");
						#endif

						DiscardUnappliedChanges();
						GUI.changed = true;
					}
					return true;
				case KeyCode.Return:
				case KeyCode.KeypadEnter:
					if(hasUnappliedChanges)
					{
						#if DEV_MODE && DEBUG_APPLY_VALUE
						Debug.Log(GetType().Name+" - Applying value "+ StringUtils.TypeToString(valueUnapplied) + " because return or enter was pressed");
						#endif

						ApplyUnappliedChanges();
						GUI.changed = true;
					}
					return true;
			}
			return base.OnKeyboardInputGiven(inputEvent, keys);
		}
		
		/// <inheritdoc />
		public override void OnMouseover()
		{
			var objFieldRect = ColorRectPosition;
			if(objFieldRect.Contains(Cursor.LocalPosition))
			{
				DrawGUI.DrawMouseoverEffect(objFieldRect, localDrawAreaOffset);
			}
			else if(InspectorUtility.Preferences.mouseoverEffects.prefixLabel && MouseOverPart == PrefixedControlPart.Prefix)
			{
				DrawGUI.DrawLeftClickAreaMouseoverEffect(PrefixLabelPosition, localDrawAreaOffset);
			}
		}
		
		/// <inheritdoc/>
		public override bool OnRightClick(Event inputEvent)
		{
			if(MouseOverPart == PrefixedControlPart.Control)
			{
				DrawGUI.Use(inputEvent);
				if(!ReadOnly)
				{
					DisplayTargetSelectMenu();
				}
				return true;
			}

			return base.OnRightClick(inputEvent);
		}
		
		/// <inheritdoc/>
		protected override void BuildContextMenu(ref Menu menu, bool extendedMenu)
		{
			if(BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}

			var value = ToColor(Value);

			menu.Add("Set Value/Clear", SetValueFromMenu, Color.clear, value == Color.clear);
			menu.Add("Set Value/White", SetValueFromMenu, Color.white, value == Color.white);
			menu.Add("Set Value/Black", SetValueFromMenu, Color.black, value == Color.black);
			menu.Add("Set Value/Gray", SetValueFromMenu, Color.gray, value == Color.gray);
			menu.Add("Set Value/Red", SetValueFromMenu, Color.red, value == Color.red);
			menu.Add("Set Value/Yellow", SetValueFromMenu, Color.yellow, value == Color.yellow);
			menu.Add("Set Value/Blue", SetValueFromMenu, Color.blue, value == Color.blue);
			menu.Add("Set Value/Green", SetValueFromMenu, Color.green, value == Color.green);
			menu.Add("Set Value/Cyan", SetValueFromMenu, Color.cyan, value == Color.cyan);
			menu.Add("Set Value/Magenta", SetValueFromMenu, Color.magenta, value == Color.magenta);
			
			
			var alpha = value.a;

			menu.Add(Type == Types.Color ? "Set Alpha/Opaque\t1" : "Set Alpha/Opaque\t255", SetAlphaFromMenu, 1f, alpha >= 1f);
			menu.Add("Set Alpha/Transparent\t0", SetAlphaFromMenu, 0f, alpha <= 0f);

			var hexRGB = ValueToHexCodeRGB();
			menu.Add("Copy As/Hex RGB\t#"+hexRGB, CopyValueAsHexCodeRGB);

			var hexRGBA = ValueToHexCodeRGBA();
			menu.Add("Copy As/Hex RGBA\t#"+hexRGBA, CopyValueAsHexCodeRGBA);

			var colorRGB = ValueToColorRGB();
			menu.Add("Copy As/Color RGB\t#"+colorRGB, CopyValueAsColorRGB);

			var colorRGBA = ValueToColorRGBA();
			menu.Add("Copy As/Color RGBA\t#"+colorRGBA, CopyValueAsColorRGBA);

			var color32RGB = ValueToColor32RGB();
			menu.Add("Copy As/Color32 RGB\t#"+color32RGB, CopyValueAsColor32RGB);

			var color32RGBA = ValueToColor32RGBA();
			menu.Add("Copy As/Color32 RGBA\t#"+color32RGBA, CopyValueAsColor32RGBA);
			
			if(!BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}
		}

		private void CopyValueAsHexCodeRGBA()
		{
			Clipboard.Copy(ValueToHexCodeRGBA());
			Clipboard.SendCopyToClipboardMessage(GetFieldNameForMessages());
		}

		private void CopyValueAsHexCodeRGB()
		{
			Clipboard.Copy(ValueToHexCodeRGB());
			Clipboard.SendCopyToClipboardMessage(GetFieldNameForMessages());
		}

		private void CopyValueAsColorRGBA()
		{
			Clipboard.Copy(ValueToColorRGBA());
			Clipboard.SendCopyToClipboardMessage(GetFieldNameForMessages());
		}

		private void CopyValueAsColorRGB()
		{
			Clipboard.Copy(ValueToColorRGB());
			Clipboard.SendCopyToClipboardMessage(GetFieldNameForMessages());
		}

		private void CopyValueAsColor32RGBA()
		{
			Clipboard.Copy(ValueToColor32RGBA());
			Clipboard.SendCopyToClipboardMessage(GetFieldNameForMessages());
		}

		private void CopyValueAsColor32RGB()
		{
			Clipboard.Copy(ValueToColor32RGB());
			Clipboard.SendCopyToClipboardMessage(GetFieldNameForMessages());
		}

		private string ValueToHexCodeRGBA()
		{
			return ColorUtility.ToHtmlStringRGBA(ToColor(Value));
		}

		private string ValueToHexCodeRGB()
		{
			return ColorUtility.ToHtmlStringRGB(ToColor(Value));
		}

		private string ValueToColorRGB()
		{
			var color = ToColor(Value);
			return StringUtils.Concat(color.r, ", ", color.g, ", ", color.b);
		}

		private string ValueToColorRGBA()
		{
			var color = ToColor(Value);
			return StringUtils.Concat(color.r, ", ", color.g, ", ", color.b, ", ", color.a);
		}

		private string ValueToColor32RGB()
		{
			var color = ToColor32(Value);
			return StringUtils.Concat(color.r, ", ", color.g, ", ", color.b);
		}

		private string ValueToColor32RGBA()
		{
			var color = ToColor32(Value);
			return StringUtils.Concat(color.r, ", ", color.g, ", ", color.b, ", ", color.a);
		}
		
		/// <inheritdoc/>
		public override bool CanPasteFromClipboard()
		{
			return Clipboard.CanPasteAs(Types.Color) || Clipboard.CanPasteAs(Types.Color32);
		}

		/// <inheritdoc/>
		protected override void DoPasteFromClipboard()
		{
			//var type = Type;
			if(memberInfo != null && memberInfo.MixedContent)
			{
				object multipleValues = null;
				if(Clipboard.TryPaste(typeof(Color[]), ref multipleValues))
				{
					var array = multipleValues as object[];
					if(array != null)
					{
						if(Type == Types.Color)
						{
							SetValues(array);
						}
						else
						{
							var colors32 = ArrayPool<object>.CastToValueTypeArray<Color32>(array);
							var colors = ArrayPool<Color32>.CastToValueTypeArray<Color>(colors32);
							var objs = ArrayPool<Color>.Cast<object>(colors);
							SetValues(objs);
						}
						return;
					}
				}
				else
				{
					if(Clipboard.TryPaste(typeof(Color32[]), ref multipleValues))
					{
						var array = multipleValues as object[];
						if(array != null)
						{
							if(Type == Types.Color32)
							{
								SetValues(array);
							}
							else
							{
								var colors = ArrayPool<object>.CastToValueTypeArray<Color>(array);
								var colors32 = ArrayPool<Color>.CastToValueTypeArray<Color32>(colors);
								var objs = ArrayPool<Color32>.Cast<object>(colors32);
								SetValues(objs);
							}
							return;
						}
					}
				}
			}

			object setValue = default(Color);
			if(Clipboard.TryPaste(Types.Color, ref setValue))
			{
				SetValue(FromColor(setValue));
			}
			else
			{
				setValue = Clipboard.Paste(Types.Color32);
				if(Type == Types.Color32)
				{
					SetValue(setValue);
				}
				else
				{
					SetValue(ToColor(setValue));
				}
			}
		}

		private void SetValueFromMenu(object value)
		{
			Value = FromColor(value);
		}

		private void SetAlphaFromMenu(object alpha)
		{
			var setValue = ToColor(GetValue());
			setValue.a = (float)alpha;
			Value = (TColor)(object)setValue;
		}

		/// <inheritdoc/>
		protected override void OnControlClicked(Event inputEvent)
		{
			if(MouseOverPart == PrefixedControlPart.Control)
			{
				if(eyeDropToolMouseovered)
				{
					HandleOnClickSelection(inputEvent, ReasonSelectionChanged.ControlClicked);
					FocusControlField();
					return;
				}
				//what to do here?
				//save current selection for later restoring?
				//deselect inspector?
				//at least don't focus control field!
				return;
			}

			HandleOnClickSelection(inputEvent, ReasonSelectionChanged.ControlClicked);
			FocusControlField();
		}
		
		/// <inheritdoc />
		protected override TColor GetRandomValue()
		{
			var color = ToColor(GetValue());
			color.r = RandomUtils.Float(0f, 1f);
			color.g = RandomUtils.Float(0f, 1f);
			color.b = RandomUtils.Float(0f, 1f);
			color.a = RandomUtils.Float(0f, 1f);
			return FromColor(color);
		}

		/// <inheritdoc/>
		public override void Dispose()
		{
			eyeDropToolMouseovered = false;

			ApplyUnappliedChanges();
			
			#if DEV_MODE
			Debug.Assert(!hasUnappliedChanges, ToString()+".Dispose - hasUnappliedChanges was true!");
			#endif

			if(listeningForColorPickerClosed)
			{
				#if DEV_MODE && DEBUG_COLOR_PICKER
				Debug.Log("listeningForColorPickerClosed = "+StringUtils.False);
				#endif
				listeningForColorPickerClosed = false;
				ColorPicker.OnClosed -= OnColorPickerClosedWithUnappliedChanges;
			}

			if(listeningForStoppedUsingEyeDropper)
			{
				#if DEV_MODE && DEBUG_COLOR_PICKER
				Debug.Log("listeningForColorPickerClosed = "+StringUtils.False);
				#endif
				listeningForColorPickerClosed = false;
				ColorPicker.OnStoppedUsingEyeDropper -= OnColorPickerClosedWithUnappliedChanges;

			}
			
			base.Dispose();
		}

		/// <inheritdoc />
		protected override bool TryGetSingleValueVisualizedInInspector(out object visualizedValue)
		{
			if(hasUnappliedChanges)
			{
				visualizedValue = valueUnapplied;
				return true;
			}

			return base.TryGetSingleValueVisualizedInInspector(out visualizedValue);
		}

		/// <inheritdoc/>
		protected override TColor GetCopyOfValue(TColor source)
		{
			return source;
		}

		/// <inheritdoc/>
		protected override void OnLayoutEvent(Rect position)
		{
			base.OnLayoutEvent(position);

			if(MouseOverPart == PrefixedControlPart.Control)
			{
				eyeDropToolMouseovered = EyeDropToolRect.Contains(Event.current.mousePosition);
			}
			else
			{
				eyeDropToolMouseovered = false;
			}
		}

		/// <inheritdoc />
		protected override void GetDrawPositions(Rect position)
		{
			base.GetDrawPositions(position);
			controlLastDrawPosition.y += 1f;
			controlLastDrawPosition.height -= 2f;
		}

		private void SetHasUnappliedChanges(bool setHasUnappliedChanges)
		{
			if(hasUnappliedChanges != setHasUnappliedChanges)
			{
				#if DEV_MODE && DEBUG_UNAPPLIED_CHANGES
				Debug.Log("SetHasUnappliedChanges("+StringUtils.ToColorizedString(setHasUnappliedChanges)+ ") with Value="+ StringUtils.ToColorizedString(ToColor(GetValue()))+ ", valueUnapplied=" + StringUtils.ToColorizedString(valueUnapplied));
				#endif

				hasUnappliedChanges = setHasUnappliedChanges;
				
				#if UNITY_EDITOR
				if(hasUnappliedChanges)
				{
					if(ColorPicker.IsOpen && !listeningForColorPickerClosed)
					{
						#if DEV_MODE && DEBUG_COLOR_PICKER
						Debug.Log("listeningForColorPickerClosed = "+StringUtils.True);
						#endif
						listeningForColorPickerClosed = true;
						ColorPicker.OnClosed += OnColorPickerClosedWithUnappliedChanges;
					}
					else if(ColorPicker.UsingEyeDropper && !listeningForStoppedUsingEyeDropper)
					{
						#if DEV_MODE && DEBUG_COLOR_PICKER
						Debug.Log("listeningForStoppedUsingEyeDropper = " + StringUtils.True);
						#endif
						listeningForStoppedUsingEyeDropper = true;
						ColorPicker.OnStoppedUsingEyeDropper += OnStoppedUsingEyeDropperWithUnappliedChanges;
					}
				}
				else
				{
					if(listeningForColorPickerClosed)
					{
						#if DEV_MODE && DEBUG_COLOR_PICKER
						Debug.Log("listeningForColorPickerClosed = "+StringUtils.False);
						#endif
						listeningForColorPickerClosed = false;
						ColorPicker.OnClosed -= OnColorPickerClosedWithUnappliedChanges;
					}
					else if(listeningForStoppedUsingEyeDropper)
					{
						#if DEV_MODE && DEBUG_COLOR_PICKER
						Debug.Log("listeningForStoppedUsingEyeDropper = " + StringUtils.False);
						#endif
						listeningForStoppedUsingEyeDropper = false;
						ColorPicker.OnStoppedUsingEyeDropper -= OnStoppedUsingEyeDropperWithUnappliedChanges;
					}
				}
				#endif
			}
		}
		
		private void OnColorPickerClosedWithUnappliedChanges(Color initialColor, Color selectedColor, bool wasCancelled)
		{
			#if DEV_MODE && DEBUG_COLOR_PICKER
			Debug.Log(Msg("OnColorPickerClosedWithUnappliedChanges(initial=", initialColor, ", selected=", selectedColor, ") - hasUnappliedChanges="+StringUtils.ToColorizedString(hasUnappliedChanges)+ ", Value="+ StringUtils.ToColorizedString(ToColor(GetValue()))+ ", valueUnapplied=" + StringUtils.ToColorizedString(valueUnapplied)));
			#endif

			Select(ReasonSelectionChanged.GainedFocus);

			if(listeningForColorPickerClosed)
			{
				#if DEV_MODE && DEBUG_COLOR_PICKER
				Debug.Log("listeningForColorPickerClosed = "+StringUtils.False);
				#endif
				listeningForColorPickerClosed = false;
				ColorPicker.OnClosed -= OnColorPickerClosedWithUnappliedChanges;
			}

			if(wasCancelled)
			{
				DiscardUnappliedChanges();
			}
			else
			{
				ApplyUnappliedChanges();
			}
		}

		private void OnStoppedUsingEyeDropperWithUnappliedChanges(Color initialColor, Color selectedColor, bool wasCancelled)
		{
			#if DEV_MODE && DEBUG_COLOR_PICKER
			Debug.Log(Msg("OnStoppedUsingEyeDropperWithUnappliedChanges(initial=", initialColor, ", selected=", selectedColor, ") - hasUnappliedChanges="+StringUtils.ToColorizedString(hasUnappliedChanges)+ ", Value="+ StringUtils.ToColorizedString(ToColor(GetValue()))+ ", valueUnapplied=" + StringUtils.ToColorizedString(valueUnapplied)));
			#endif

			Select(ReasonSelectionChanged.GainedFocus);

			if(listeningForColorPickerClosed)
			{
				#if DEV_MODE && DEBUG_COLOR_PICKER
				Debug.Log("listeningForStoppedUsingEyeDropper = " + StringUtils.False);
				#endif
				listeningForStoppedUsingEyeDropper = false;
				ColorPicker.OnStoppedUsingEyeDropper -= OnStoppedUsingEyeDropperWithUnappliedChanges;
			}

			if(wasCancelled)
			{
				DiscardUnappliedChanges();
			}
			else
			{
				ApplyUnappliedChanges();
			}
		}


		private void DisplayTargetSelectMenu()
		{
			var menu = Menu.Create();

			menu.Add("Clear", SetValueFromMenu, Color.clear);
			menu.Add("White", SetValueFromMenu, Color.white);
			menu.Add("Black", SetValueFromMenu, Color.black);
			menu.Add("Gray", SetValueFromMenu, Color.gray);
			menu.Add("Red", SetValueFromMenu, Color.red);
			menu.Add("Yellow", SetValueFromMenu, Color.yellow);
			menu.Add("Blue", SetValueFromMenu, Color.blue);
			menu.Add("Green", SetValueFromMenu, Color.green);
			menu.Add("Cyan", SetValueFromMenu, Color.cyan);
			menu.Add("Magenta", SetValueFromMenu, Color.magenta);
			
			ContextMenuUtility.Open(menu, this);
		}
		
		/// <summary>
		/// If value selected via color picker is still unapplied, apply it now
		/// </summary>
		private void ApplyUnappliedChanges()
		{
			if(hasUnappliedChanges)
			{
				#if DEV_MODE && DEBUG_UNAPPLIED_CHANGES
				Debug.Log(StringUtils.ToColorizedString(ToString(), ".ApplyUnappliedChanges with valueUnapplied=", valueUnapplied, ", Value=", Value, "  - Event=", StringUtils.ToString(Event.current) + ", KeyCode=" + (Event.current == null ? KeyCode.None : Event.current.keyCode) + ", button=" + (Event.current == null ? -1 : Event.current.button)));
				#endif

				SetValue(valueUnapplied);
			}
			#if DEV_MODE && DEBUG_UNAPPLIED_CHANGES
			else { Debug.Log(StringUtils.ToColorizedString(ToString(), ".ApplyUnappliedChanges with valueUnapplied=", valueUnapplied, ", Value=", Value, "  - Event=", StringUtils.ToString(Event.current) + ", KeyCode=" + (Event.current == null ? KeyCode.None : Event.current.keyCode) + ", button=" + (Event.current == null ? -1 : Event.current.button))); }
			#endif
		}

		/// <summary>
		/// If value selected via color picker is still unapplied, discard it now
		/// and keep the previously selected value
		/// </summary>
		private void DiscardUnappliedChanges()
		{
			if(hasUnappliedChanges)
			{
				#if DEV_MODE && DEBUG_UNAPPLIED_CHANGES
				Debug.Log("DiscardUnappliedChanges - Event=" + StringUtils.ToString(Event.current) + ", KeyCode=" + Event.current.keyCode + ", button=" + Event.current.button);
				#endif

				valueUnapplied = ToColor(GetValue());
				SetHasUnappliedChanges(false);
			}
		}
	}
}