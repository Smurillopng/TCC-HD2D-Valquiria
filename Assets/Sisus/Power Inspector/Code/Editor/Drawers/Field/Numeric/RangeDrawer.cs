//#define DEBUG_FOCUS_FIELD
//#define DEBUG_STEP

using System;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Sisus
{
	/// <summary>
	/// Drawer representing a numeric field with the Range attribute.
	/// </summary>
	/// <typeparam name="TValue"> Type of the value. </typeparam>
	[Serializable]
	public abstract class RangeDrawer<TValue> : NumericDrawer<TValue> where TValue : IConvertible, IComparable
	{
		private const float NumberFieldNormalWidth = 50f;

		protected float min;
		protected float max;

		private double step;
		private int stepLeadingZeroCount;

		private int sliderID;
		private int focusSlider;
		private bool sliderFocused;

		private SliderSubPart mouseoveredSubPart;
		private SliderSubPart mouseDownOverSubPart;

		private Rect sliderPosition;
		private Rect numberFieldPosition;
		private Rect sliderClickableAreaRect;

		/// <inheritdoc />
		public override bool RequiresConstantRepaint
		{
			get
			{
				return mouseoveredSubPart == SliderSubPart.Slider;
			}
		}

		/// <inheritdoc />
		public override string DocumentationPageUrl
		{
			get
			{
				return PowerInspectorDocumentation.GetAttributeUrl("prange");
			}
		}

		private bool HasNumberField
		{
			get
			{
				return numberFieldPosition.width > 0f;
			}
		}

		private Rect SliderPosition
		{
			get
			{
				return sliderPosition;
			}
		}

		private Rect NumberFieldPosition
		{
			get
			{
				return numberFieldPosition;
			}
		}

		private Rect SliderClickableAreaRect
		{
			get
			{
				return sliderClickableAreaRect;
			}
		}

		/// <inheritdoc />
		public sealed override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			throw new NotSupportedException("Please use the other Setup method");
		}

		/// <inheritdoc/>
		protected sealed override void Setup(TValue setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			throw new NotSupportedException("Please use the other Setup method");
		}

		protected void Setup(TValue setValue, float setMin, float setMax, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			sliderID = GetUniqueControlId();

			min = Mathf.Min(setMin, setMax);
			max = Mathf.Max(setMin, setMax);

			step = GetStep(min, max);
			stepLeadingZeroCount = Convert.ToDecimal(step).ToString(System.Globalization.CultureInfo.InvariantCulture).Length - 2;

			base.Setup(Clamped(setValue), typeof(TValue), setMemberInfo, setParent, setLabel, setReadOnly);

			if(label.tooltip.Length == 0)
			{
				label.tooltip = string.Concat("Value between ", StringUtils.ToString(min), " and ", StringUtils.ToString(max));
			}

			#if DEV_MODE && DEBUG_STEP
			Debug.Log(ToString() + " - " + StringUtils.ToString(min) + "..." + StringUtils.ToString(max) + " step:"+step);
			#endif
		}

		/// <inheritdoc/>
		protected override bool DoSetValue(TValue setValue, bool applyToField, bool updateMembers)
		{
			return base.DoSetValue(Clamped(setValue), applyToField, updateMembers);
		}

		private static double GetStep(double min, double max)
		{
			var diff = Math.Abs(max - min);

			if(diff >= 50d)
			{
				if(diff < 500d)
				{
					return 1d;
				}
				return GetStepMoreOrEqualToOne(diff);
			}
			if(diff >= 5d && diff < 50d)
			{
				return 0.1d;
			}
			if(diff >= 0.5d && diff < 5d)
			{
				return 0.01d;
			}
			return GetStepLessThanOne(diff);
		}

		private static double GetStepLessThanOne(double diff)
		{
			//NOTE: Won't work with really large values, like 0 to TValue.MaxValue
			//string digits = Convert.ToDecimal(diff).ToString(System.Globalization.CultureInfo.InvariantCulture);
			string digits = diff.ToString(StringUtils.DoubleFormat);

			int count = digits.Length;
			bool dotFound = false;
			var result = 0.1d;

			for(int n = 0; n < count; n++)
			{
				var digit = digits[n];
				if(digit == '0')
				{
					if(dotFound)
					{
						result *= 0.1d;
					}
					continue;
				}
				if(digit == '.')
				{
					dotFound = true;
					continue;
				}

				switch(digit)
				{
					case '1':
					case '2':
					case '3':
					case '4':
						return result * 0.1d;
					default:
						return result;
				}
			}
			return result;
		}

		private static double GetStepMoreOrEqualToOne(double diff)
		{
			var wholeNumbers = Math.Round(diff);
			string digits = wholeNumbers.ToString(System.Globalization.CultureInfo.InvariantCulture);
			int count = digits.Length;

			var result = 0.01d;

			for(int n = count - 1; n >= 1; n--)
			{
				result *= 10d;
			}

			switch(digits[0])
			{
				case '1':
				case '2':
				case '3':
				case '4':
					break;
				default:
					result *= 10d;
					break;
			}

			return result;
		}

		/// <inheritdoc/>
		public sealed override void OnPrefixDragged(ref TValue inputValue, TValue inputMouseDownValue, float mouseDelta)
		{
			if(mouseDelta == 0f)
			{
				inputValue = inputMouseDownValue;
			}
			else
			{
				inputValue = RoundedAndClamped(Convert.ToDouble(inputMouseDownValue) + mouseDelta * step * 0.1d);
			}
		}

		/// <inheritdoc/>
		public sealed override bool DrawBody(Rect position)
		{
			HandleDrawHintIcon(position);
			DrawDragBarIfReorderable();

			var valueWas = Value;
			var setValue = valueWas;

			if(HasNumberField)
			{
				DrawerUtility.BeginInputField(this, controlId, ref editField, ref focusField, MixedContent);
				setValue = DrawNumberFieldVisuals(NumberFieldPosition, setValue);
				DrawerUtility.EndInputField();
			}

			DrawerUtility.BeginFocusableField(this, sliderID, ref focusSlider, MixedContent);
			{
				if(DrawGUI.EditingTextField)
				{
					DrawControlVisuals(SliderPosition, setValue);
				}
				else
				{
					setValue = DrawControlVisuals(SliderPosition, setValue);
				}
			}
			DrawerUtility.EndFocusableField();

			if(!setValue.Equals(valueWas))
			{
				Value = setValue;
				return true;
			}

			return false;
		}

		/// <inheritdoc/>
		public sealed override TValue DrawControlVisuals(Rect position, TValue value)
		{
			var setValue = DrawGUI.Active.Slider(position, value, min, max);
			if(!Equals(value, setValue))
			{
				return RoundedAndClamped(setValue);
			}
			return value;
		}

		/// <summary> Draw the numeric text field. </summary>
		/// <param name="position"> The position and dimensions where to draw. </param>
		/// <param name="value"> The current value shown in the field. </param>
		/// <returns> The value after drawing has finished. </returns>
		protected abstract TValue DrawNumberFieldVisuals(Rect position, TValue value);
		
		/// <inheritdoc />
		protected sealed override void GetDrawPositions(Rect position)
		{
			base.GetDrawPositions(position);

			if(controlLastDrawPosition.width < 90f)
			{
				sliderPosition = controlLastDrawPosition;
				numberFieldPosition.width = 0f;
			}
			else
			{
				sliderPosition = controlLastDrawPosition;
				sliderPosition.width -= NumberFieldNormalWidth + 5f;

				numberFieldPosition = sliderPosition;
				numberFieldPosition.x += numberFieldPosition.width + 5f;
				numberFieldPosition.width = NumberFieldNormalWidth;
			}

			sliderClickableAreaRect = sliderPosition;
			#if !UNITY_2019_3_OR_NEWER
			sliderClickableAreaRect.x += 5f;
			sliderClickableAreaRect.width -= 7f;
			#endif
		}

		/// <inheritdoc/>
		protected sealed override void OnLayoutEvent(Rect position)
		{
			base.OnLayoutEvent(position);

			if(sliderClickableAreaRect.MouseIsOver())
			{
				mouseoveredSubPart = SliderSubPart.Slider;
			}
			else if(HasNumberField && numberFieldPosition.MouseIsOver())
			{
				mouseoveredSubPart = SliderSubPart.NumberField;
			}
			else
			{
				mouseoveredSubPart = SliderSubPart.None;
			}
		}

		/// <inheritdoc />
		protected override void OnPrefixClicked(Event inputEvent)
		{
			mouseDownOverSubPart = mouseoveredSubPart;
			HandleOnClickSelection(inputEvent, ReasonSelectionChanged.PrefixClicked);
			DrawGUI.EditingTextField = false;
			DrawGUI.Use(inputEvent);
			FocusSlider();
		}

		/// <inheritdoc />
		protected override void OnControlClicked(Event inputEvent)
		{
			mouseDownOverSubPart = mouseoveredSubPart;
			HandleOnClickSelection(inputEvent, ReasonSelectionChanged.ControlClicked);

			if(mouseoveredSubPart == SliderSubPart.NumberField)
			{
				focusSlider = 0;
				sliderFocused = false;

				//if field was already selected when it was clicked, don't use the event
				//this way Unity can handle positioning the cursor in a specific point on the text field etc.
				if(Selected)
				{
					return;
				}

				DrawGUI.Use(inputEvent);
				StartEditingField();

				return;
			}

			DrawGUI.EditingTextField = false;

			if(mouseoveredSubPart == SliderSubPart.Slider)
			{
				sliderFocused = true;
			}
		}

		/// <inheritdoc />
		public sealed override void OnMouseUpAfterDownOverControl(Event inputEvent, bool isClick)
		{
			if(mouseDownOverSubPart == SliderSubPart.Slider)
			{
				UpdateCachedValuesFromFieldsRecursively();
			}
			else
			{
				base.OnMouseUpAfterDownOverControl(inputEvent, isClick);
			}
		}

		/// <summary>
		/// Moves keyboard focus to the slider sub-control.
		/// </summary>
		private void FocusSlider()
		{
			if(sliderFocused)
			{
				return;
			}
			sliderFocused = true;

			#if DEV_MODE && DEBUG_FOCUS_FIELD
			Debug.Log(ToString() + ".FocusSlider()");
			#endif

			//During each repaint event, while value is more than zero,
			//we call GUI.SetNextControlName, and reduce the value by one.
			//Once the value reaches zero, we use DrawGUI.FocusControl to
			//set focus on the field.
			focusSlider = 3;

			focusField = 0;
			editField = false;

			DrawGUI.EditingTextField = false;

			//trigger a repaint
			GUI.changed = true;
		}

		/// <inheritdoc />
		protected sealed override void OnSelectedInternal(ReasonSelectionChanged reason, IDrawer previous, bool isMultiSelection)
		{
			switch(reason)
			{
				case ReasonSelectionChanged.PrefixClicked:
					return;
				case ReasonSelectionChanged.SelectPrevControl:
					FocusControlField();
					return;
				case ReasonSelectionChanged.SelectControlUp:
					FocusSlider();
					return;
				case ReasonSelectionChanged.SelectControlLeft:
				case ReasonSelectionChanged.SelectControlDown:
				case ReasonSelectionChanged.SelectControlRight:
				case ReasonSelectionChanged.SelectNextControl:
				case ReasonSelectionChanged.KeyPressOther:
					FocusSlider();
					return;
			}
		}

		/// <inheritdoc />
		protected sealed override void OnDeselectedInternal(ReasonSelectionChanged reason, IDrawer losingFocusTo)
		{
			sliderFocused = false;
			base.OnDeselectedInternal(reason, losingFocusTo);
		}
		
		private TValue GetSliderValueAt(float position)
		{
			var clickableRect = SliderClickableAreaRect;
			#if UNITY_2019_3_OR_NEWER
			clickableRect.x += 5f;
			clickableRect.width -= 10f;
			#else
			clickableRect.width -= 1f;
			#endif
			var p = (position - clickableRect.x) / (clickableRect.width);
			return RoundedAndClamped(MathUtils.Lerp(min, max, p));
		}

		/// <inheritdoc />
		public sealed override void OnMouseoverDuringDrag(MouseDownInfo mouseDownInfo, Object[] dragAndDropObjectReferences)
		{
			if(mouseDownOverSubPart != SliderSubPart.Slider)
			{
				DrawGUI.Active.DragAndDropVisualMode = DragAndDropVisualMode.Rejected;
				return;
			}
			
			OnMouseoverSlider();
		}

		/// <inheritdoc />
		public sealed override void OnMouseover()
		{
			if(mouseoveredSubPart == SliderSubPart.Slider)
			{
				OnMouseoverSlider();
			}
			else if(mouseoveredSubPart == SliderSubPart.NumberField)
			{
				if(HasNumberField)
				{
					DrawGUI.DrawMouseoverEffect(NumberFieldPosition, localDrawAreaOffset);
				}

				var valueString = StringUtils.ToString(Value);
				if(valueString.Length > 6)
				{
					var tooltip = GUIContentPool.Create(valueString);
					var tooltipRect = labelLastDrawPosition;
					tooltipRect.y += 1f;
					tooltipRect.height -= 2f;

					var tooltipWidth = DrawGUI.prefixLabel.CalcSize(tooltip).x + 3f;
					tooltipRect.x = controlLastDrawPosition.x - tooltipWidth - DrawGUI.MiddlePadding - DrawGUI.MiddlePadding;
					tooltipRect.width = tooltipWidth;

					DrawGUI.Active.TooltipBox(tooltipRect, tooltip);
					GUIContentPool.Dispose(ref tooltip);
				}
			}
			else if(MouseOverPart == PrefixedControlPart.Prefix)
			{
				if(InspectorUtility.Preferences.mouseoverEffects.prefixLabel)
				{
					DrawGUI.DrawLeftClickAreaMouseoverEffect(PrefixLabelPosition, localDrawAreaOffset);
				}

				if(!ReadOnly)
				{
					DrawGUI.Active.SetCursor(MouseCursor.SlideArrow);

					if(HasNumberField)
					{
						//UPDATE: highlight the control even when mouseovering the prefix
						//to make it clear than dragging will change the value of that field
						DrawGUI.DrawMouseoverEffect(NumberFieldPosition, localDrawAreaOffset);
					}
				}
			}
		}

		private void OnMouseoverSlider()
		{
			//trigger a repaint every frame for better responsiveness
			GUI.changed = true;

			if(Event.current.type != EventType.Repaint)
			{
				return;
			}

			var currentDrawAreaOffset = DrawGUI.GetLocalDrawAreaOffset();
			var drawAreaOffsetDifference = currentDrawAreaOffset - localDrawAreaOffset;

			var visualSliderRect = SliderPosition;

			var effectiveSliderRect = SliderClickableAreaRect;

			var mousePos = Event.current.mousePosition;

			var effectiveMouseX = Mathf.Clamp(mousePos.x + drawAreaOffsetDifference.x, effectiveSliderRect.x + drawAreaOffsetDifference.x, effectiveSliderRect.x + drawAreaOffsetDifference.x + effectiveSliderRect.width);
			var valueAtCursor = GetSliderValueAt(effectiveMouseX);

			double pCurrent = MathUtils.Subtract(Value, min) / MathUtils.Subtract(max, min);
			int currentValueX = (int)(visualSliderRect.x + MathUtils.Lerp(0d, visualSliderRect.width, pCurrent));
			
			//if cursor is not over the slider then draw the click cursor
			//and display value at current cursor position as a tooltip
			int diff = MathUtils.Abs(Mathf.RoundToInt(effectiveMouseX - currentValueX));
			
			if(diff > 6)
			{
				var tooltip = GUIContentPool.Create(StringUtils.ToString(valueAtCursor));
				
				var tooltipRect = labelLastDrawPosition;
				tooltipRect.x -= drawAreaOffsetDifference.x;
				tooltipRect.y -= drawAreaOffsetDifference.y;
				tooltipRect.height -= 2f;
				var tooltipWidth = DrawGUI.tooltipStyle.CalcSize(tooltip).x;
				tooltipRect.x = mousePos.x - tooltipWidth * 0.5f + 1f;
				tooltipRect.y -= DrawGUI.SingleLineHeight;
				tooltipRect.width = tooltipWidth;

				DrawGUI.Active.TooltipBox(tooltipRect, tooltip);
				GUIContentPool.Dispose(ref tooltip);

				effectiveSliderRect.x = mousePos.x - 3f;
				effectiveSliderRect.y -= drawAreaOffsetDifference.y;
				effectiveSliderRect.width = 8f;

				#if UNITY_2019_3_OR_NEWER
				effectiveSliderRect.y -= 4f;
				#endif

				var guiColorWas = GUI.color;
				var setGuiColor = guiColorWas;
				setGuiColor.a = 0.5f;
				GUI.color = setGuiColor;
				GUI.Label(effectiveSliderRect, " ", InspectorPreferences.Styles.RangeIndicator);
				GUI.color = guiColorWas;
			}
		}

		/// <inheritdoc />
		protected sealed override void OnStoppedFieldEditing()
		{
			if(Selected)
			{
				FocusSlider();
			}
			base.OnStoppedFieldEditing();
		}

		/// <inheritdoc />
		public sealed override bool OnKeyboardInputGiven(Event e, KeyConfigs keys)
		{
			if(DrawGUI.EditingTextField)
			{
				return false;
			}

			switch(e.keyCode)
			{
				case KeyCode.LeftArrow:
				case KeyCode.KeypadPlus:
					GUI.changed = true;
					DrawGUI.Use(e);
					TValue value = Value;
					TValue decreased = RoundedAndClamped(Convert.ToDouble(value) - (e.shift ? step * 10d : step));
					if(decreased.CompareTo(value) != 0)
					{
						Value = decreased;
					}
					return true;
				case KeyCode.RightArrow:
				case KeyCode.KeypadMinus:
					GUI.changed = true;
					DrawGUI.Use(e);
					value = Value;
					TValue increased = RoundedAndClamped(Convert.ToDouble(value) + (e.shift ? step * 10d : step));
					if(increased.CompareTo(value) != 0)
					{
						Value = increased;
					}
					return true;
			}
			return base.OnKeyboardInputGiven(e, keys);
		}

		/// <inheritdoc />
		public sealed override void DrawSelectionRect()
		{
			//when editing text field we use the internally created selection rect
			if(!DrawGUI.EditingTextField && HasNumberField)
			{
				var rect = NumberFieldPosition;
				rect.yMin += 1f;
				rect.height -= 1f;
				DrawGUI.DrawRect(rect, InspectorUtility.Preferences.theme.ControlSelectedRect, localDrawAreaOffset);
			}

			if(IsFullInspectorWidth)
			{
				DrawGUI.DrawSelectionRect(SelectionRect, localDrawAreaOffset);
			}
		}

		protected abstract TValue Clamped(TValue input);

		private TValue Clamped(double input)
		{
			return (TValue)Convert.ChangeType(MathUtils.Clamp(input, min, max), typeof(TValue));
		}

		protected abstract bool Equals(TValue a, float b);

		private TValue RoundedAndClamped(double input)
		{
			return Clamped(Rounded(input));
		}

		private double Rounded(double input)
		{
			if(step == 0d)
			{
				return input;
			}

			double diff = input % step;
			if(diff == 0d)
			{
				return input;
			}

			var stepMultiple = input / step;

			var result = step * stepMultiple;
			if(step >= 1d)
			{
				result = Math.Round(result);
			}
			else
			{
				result = Math.Round(result, stepLeadingZeroCount, MidpointRounding.AwayFromZero);
			}

			return result;
		}

		/// <inheritdoc/>
		public sealed override void UpdateCachedValuesFromFieldsRecursively()
		{
			if(mouseDownOverSubPart == SliderSubPart.Slider && this == InspectorUtility.ActiveManager.MouseDownInfo.MouseDownOverDrawer)
			{
				return;
			}

			base.UpdateCachedValuesFromFieldsRecursively();
		}

		/// <inheritdoc/>
		public override void Dispose()
		{
			sliderID = 0;
			focusSlider = 0;
			sliderFocused = false;
			mouseoveredSubPart = SliderSubPart.None;
			mouseDownOverSubPart = SliderSubPart.None;
			base.Dispose();
		}
	}
}