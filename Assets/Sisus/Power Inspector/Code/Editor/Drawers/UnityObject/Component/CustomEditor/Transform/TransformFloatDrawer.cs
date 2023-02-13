//#define DEBUG_NICIFY
//#define DEBUG_PREFIX_DRAGGED
#define DEBUG_ON_MOUSE_UP

using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Sisus
{
	/// <summary>
	/// Similar to FloatDrawer but specifically designed to be used with TransformMemberDrawer.
	/// </summary>
	[Serializable]
	public sealed class TransformFloatDrawer : NumericDrawer<float>
	{
		private const float PositionDragSensitivity = FloatDrawer.DragSensitivity;
		private const float RotationDragSensitivity = FloatDrawer.DragSensitivity * 5f;
		private const float ScaleDragSensitivity = FloatDrawer.DragSensitivity * 0.5f;

		private static readonly Color XAxisColorLight = new Color32(255, 26, 0, 255); // red
		private static readonly Color YAxisColorLight = new Color32(0, 75, 0, 255);  // green
		private static readonly Color ZAxisColorLight = new Color32(0, 95, 255, 255); // blue

		private static readonly Color XAxisColorDark = new Color32(253, 61, 28, 255); // red
		private static readonly Color YAxisColorDark = new Color32(182, 255, 88, 255); // green
		private static readonly Color ZAxisColorDark = new Color32(63, 141, 255, 255); // blue

		private int memberIndex;
		private float valueCachedRaw;
		private bool nowSnappingViaOnCachedValueChanged;
		private bool subscribedToOnSceneGUI;

		private Color axisColor = Color.white;

		private bool mixedContentCached;
		private float mixedContentLastCached;

		public Vector3 AxisDirection
		{
			get
			{
				switch(memberIndex)
				{
					default:
						return Vector3.right;
					case 1:
						return Vector3.up;
					case 2:
						return Vector3.forward;
				}
			}
		}

		/// <inheritdoc />
		public override float Width
		{
			get
			{
				return lastDrawPosition.width;
			}
		}

		/// <inheritdoc/>
		public override string DocumentationPageUrl
		{
			get
			{
				return PowerInspectorDocumentation.GetDrawerInfoUrl("transform-drawer");
			}
		}

		/// <inheritdoc />
		protected override float ValueDuringMixedContent
		{
			get
			{
				return 7921058762891625713f;
			}
		}

		/// <summary>
		/// Smallest value before Transform editor starts displaying warnings.
		/// Also used for randomization
		/// </summary>
		/// <value>
		/// The minimum value.
		/// </value>
		private float MinValue
		{
			get
			{
				if(parent is PositionDrawer)
				{
					return -100000f;
				}
				return -1000000000000000000f;
			}
		}
		
		/// <summary>
		/// Largest value before Transform editor starts displaying warnings.
		/// Also used for randomization
		/// </summary>
		/// <value>
		/// The minimum value.
		/// </value>
		private float MaxValue
		{
			get
			{
				if(parent is PositionDrawer)
				{
					return 100000f;
				}
				return 1000000000000000000f;
			}
		}

		/// <inheritdoc/>
		protected override bool MixedContent
		{
			get
			{
				// override for base.MixedContent that considers Nicify
				// to make sure that if two values look identical to the user,
				// they will also be shown to not have mixed values in multi-target mode.

				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(memberInfo != null);
				#endif

				if(memberInfo.TargetCount < 2 || !memberInfo.CanReadWithoutSideEffects)
				{
					return false;
				}

				float timeNow = Platform.Time;
				float timeSinceLastUpdatedMixedContentCached = timeNow - mixedContentLastCached;

				if(timeSinceLastUpdatedMixedContentCached > 0.1f)
				{
					mixedContentLastCached = timeNow;
					mixedContentCached = GetHasMixedContentAfterNicify();
					memberInfo.MixedContent = mixedContentCached;
				}
				return mixedContentCached;
			}
		}

		private float DragSensitivity
		{ 
			get
			{
				if(parent is RotationDrawer)
				{
					return RotationDragSensitivity;
				}
				if(parent is ScaleDrawer)
				{
					return ScaleDragSensitivity;
				}
				return PositionDragSensitivity;
			}
		}

		/// <summary> Creates a new instance of TransformFloatDrawer or returns a reusable instance from the pool. </summary>
		/// <param name="value"> The initial cached value of the drawer. </param>
		/// <param name="memberInfo"> LinkedMemberInfo for the field, property or parameter that the created drawers represents. Can be null. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="readOnly"> True if drawer should be read only. </param>
		/// <returns> Ready-to-use instance of TransformFloatDrawer. </returns>
		[NotNull]
		public static TransformFloatDrawer Create(float value, LinkedMemberInfo memberInfo, [NotNull]TransformMemberBaseDrawer parent, GUIContent label, bool readOnly)
		{
			TransformFloatDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new TransformFloatDrawer();
			}
			result.Setup(value, typeof(float), memberInfo, parent, label, readOnly);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc />
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			Setup((float)setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc/>
		public override void LateSetup()
		{
			valueCachedRaw = Value;
			NicifyCachedValue();
			base.LateSetup();
		}

		/// <inheritdoc/>
		public override void OnParentAssigned(IParentDrawer newParent)
		{
			memberIndex = Array.IndexOf(newParent.Members, this);

			switch(memberIndex)
			{
				default:
					axisColor = DrawGUI.IsProSkin ? XAxisColorDark : XAxisColorLight;
					break;
				case 1:
					axisColor = DrawGUI.IsProSkin ? YAxisColorDark : YAxisColorLight;
					break;
				case 2:
					axisColor = DrawGUI.IsProSkin ? ZAxisColorDark : ZAxisColorLight;
					break;
			}

			var component = parent.Parent as IUnityObjectDrawer;
			if(component != null)
			{
				component.OnWidthsChanged += OnWidthChanged;
			}

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(memberIndex != -1, ToString()+".OnParentAssigned: index in members "+StringUtils.ToString(newParent.Members)+" of parent "+newParent+" was -1");
			Debug.Assert(newParent is ISnappable, ToString() + ".OnParentAssigned: parent "+ newParent + " was not snappable");
			Debug.Assert(newParent is TransformMemberBaseDrawer, ToString() + ".OnParentAssigned: parent "+ newParent + " did not inherit from TransformMemberBaseDrawer");
			#endif
			
			SnapIfSnappingEnabled();
		}

		private void OnWidthChanged()
		{
			//re-nicify the displayed value so that we can optimize it's roundedness to best fit the available space
			SetCachedValueSilent(Nicify(Value));
		}

		/// <inheritdoc />
		public override void UpdateCachedValuesFromFieldsRecursively()
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(Mathf.RoundToInt(Value) == Mathf.RoundToInt(valueCachedRaw));
			#endif

			if(SelectedAndInspectorHasFocus && DrawGUI.EditingTextField)
			{
				return;
			}

			if(DraggingPrefix)
			{
				return;
			}
			
			float fieldValue = (float)memberInfo.GetValue(0);

			ISnappable snappable;
			bool snap = GetSnappableParentIfSnappingEnabled(out snappable) && !InspectorUtility.ActiveManager.MouseDownInfo.IsDrag(); //NEW TEST: disabling snapping while object is being dragged in scene view
			
			if(!fieldValue.Equals(valueCachedRaw))
			{
				#if DEV_MODE && DEBUG_NICIFY
				Debug.Log(parent.Name + "." + Name + " fieldValue changed from " + valueCachedRaw.ToString(StringUtils.DoubleFormat) + " to " + fieldValue.ToString(StringUtils.DoubleFormat));
				#endif

				float fieldValueWas = valueCachedRaw;
				valueCachedRaw = fieldValue;
				
				float setValue = fieldValue;
				
				if(snap)
				{
					snappable.SnapMemberValue(memberIndex, ref setValue, Nicify);
				}
				else
				{
					setValue = Nicify(setValue);
				}

				SetCachedValueSilent(setValue);
				if(!Mathf.Approximately(fieldValue, fieldValueWas))
				{
					bool applyToField = snap && !memberInfo.MixedContent;
					OnCachedValueChanged(applyToField, true);
					GUI.changed = true;
				}
			}

			if(snap && memberInfo.MixedContent)
			{
				Snap(snappable);
			}
		}
		
		/// <inheritdoc/>
		protected override void OnCachedValueChanged(bool applyToField, bool updateMembers)
		{
			valueCachedRaw = Value;

			// Handle snapping while avoiding possible infinite loops with SnapIfSnappingEnabled
			// causing OnCachedValueChanged to get called endlessly.
			if(!nowSnappingViaOnCachedValueChanged)
			{
				nowSnappingViaOnCachedValueChanged = true;
				try
				{
					SnapIfSnappingEnabled();
				}
				catch(Exception e)
				{
					Debug.LogError(e);
				}
				finally
				{
					nowSnappingViaOnCachedValueChanged = false;
				}
			}
			
			base.OnCachedValueChanged(applyToField, updateMembers);
			NicifyCachedValue();
		}

		/// <inheritdoc/>
		protected override void OnStoppedFieldEditing()
		{
			SnapIfSnappingEnabled();
			base.OnStoppedFieldEditing();
			NicifyCachedValue();
		}

		/// <inheritdoc/>
		protected override void ApplyValueToField()
		{
			if(Inspector.State.usingLocalSpace || !(parent is ScaleDrawer))
			{
				base.ApplyValueToField();
				return;
			}

			bool changed = false;

			float value = Value;

			// Can't write to field when using LinkedMemberInfo, because lossyScale property is get-only.
			// Convert to localScale and write directly to transform targets instead.
			var targets = UnityObjects;
			for(int n = targets.Length - 1; n >= 0; n--)
			{
				var transform = targets[n] as Transform;
				var setLossyScale = transform.lossyScale;
				if(transform.lossyScale[memberIndex] != value)
				{
					if(!changed)
					{
						changed = true;
						UndoHandler.RegisterUndoableAction(targets, UndoHandler.GetSetValueMenuText(parent.Name));
					}

					setLossyScale[memberIndex] = value;
					transform.SetWorldScale(setLossyScale);
				}
			}
		}

		/// <inheritdoc/>
		protected override void ApplyValuesToFields(object[] values)
		{
			if(Inspector.State.usingLocalSpace || !(parent is ScaleDrawer))
			{
				base.ApplyValuesToFields(values);
				return;
			}

			#if DEV_MODE
			Debug.Log(ToString()+".WriteToField("+StringUtils.ToString(values)+") with world space");
			#endif

			bool changed = false;

			// Can't write to field when using LinkedMemberInfo, because lossyScale property is get-only.
			// Convert to localScale and write directly to transform targets instead.
			var targets = UnityObjects;
			for(int n = targets.Length - 1; n >= 0; n--)
			{
				var transform = targets[n] as Transform;
				var setLossyScale = transform.lossyScale;
				float value = (float)values[n];
				if(transform.lossyScale[memberIndex] != value)
				{
					if(!changed)
					{
						changed = true;
						UndoHandler.RegisterUndoableAction(targets, UndoHandler.GetSetValueMenuText(parent.Name));
					}

					setLossyScale[memberIndex] = value;
					transform.SetWorldScale(setLossyScale);
				}
			}
		}

		/// <summary> Gets snappable parent if snapping enabled. </summary>
		/// <param name="snappable"> [out] The snappable. </param>
		/// <returns> True if it succeeds, false if it fails. </returns>
		private bool GetSnappableParentIfSnappingEnabled(out ISnappable snappable)
		{
			try
			{
				snappable = parent as ISnappable;
				return snappable.SnappingEnabled;
			}
			catch(NullReferenceException)
			{
				Debug.LogError(ToString()+ ".GetSnappableParentIfSnappingEnabled NullReferenceException with parent=" + StringUtils.ToString(parent)+": was TransformFloatDrawer added to a parent that wasn't snappable?");
				snappable = null;
				return false;
			}
		}

		/// <summary>
		/// Determines if snapping is enabled for this drawer, and if so, snaps current values.
		/// </summary>
		private void SnapIfSnappingEnabled()
		{
			if(inactive)
			{
				#if DEV_MODE
				Debug.Log(ToString()+ ".SnapIfSnappingEnabled() was called with inactive=true");
				#endif
				return;
			}

			ISnappable snappable;
			if(GetSnappableParentIfSnappingEnabled(out snappable))
			{
				Snap(snappable);
			}
		}

		/// <summary>
		/// Snaps current values using the snapping functionality provided ISnappable (likely PositionDrawer, RotationDrawer or ScaleDrawer).
		/// </summary>
		/// <param name="snappable"></param>
		private void Snap([NotNull]ISnappable snappable)
		{
			bool changed = false;
			var values = GetValues();
			for(int n = values.Length - 1; n >= 0; n--)
			{
				float val = (float)values[n];
				float setVal = val;
				snappable.SnapMemberValue(memberIndex, ref setVal, Nicify);
				if(!val.Equals(setVal))
				{
					changed = true;
					values[n] = val;
				}
			}

			if(changed)
			{
				SetValues(values);
			}
		}
		
		private bool SnapIfSnappingEnabled(ref float valueToSnap, bool snapEvenIfNotEnabled)
		{
			try
			{
				var snappable = (ISnappable)parent;
				if(snappable.SnappingEnabled || snapEvenIfNotEnabled)
				{
					snappable.SnapMemberValue(memberIndex, ref valueToSnap, Nicify);
					return true;
				}
			}
			catch(InvalidCastException)
			{
				Debug.LogError(ToString()+ ".SnapIfSnappingEnabled InvalidCastException with parent=" + StringUtils.ToString(parent)+": was TransformFloatDrawer added to a parent that wasn't snappable?");
			}
			catch(NullReferenceException)
			{
				Debug.LogError(ToString()+ ".SnapIfSnappingEnabled NullReferenceException with parent="+StringUtils.ToString(parent)+": was TransformFloatDrawer added to a parent that wasn't snappable?");
			}
			return false;
		}

		/// <summary>
		/// Determines whether or not target transforms have different values
		/// when it comes to the properties represented by this drawer.
		/// This will take display value nicifying into consideration, making sure 
		/// hat if two values look identical to the user, they will also be shown
		/// to not have mixed values in the UI.
		/// </summary>
		/// <returns></returns>
		private bool GetHasMixedContentAfterNicify()
		{
			int targetCount = MemberHierarchy.TargetCount;

			if(targetCount <= 1)
			{
				#if DEV_MODE && DEBUG_NOT_MIXED_CONTENT
				Debug.Log(StringUtils.ToColorizedString("GetHasMixedContentAfterNicify(", ToString(), "): ", false, " because TargetCount <= 1"));
				#endif

				return false;
			}
			
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(memberInfo.CanRead, "GetHasMixedContentUpdated called for float drawer with !CanRead: "+ToString());
			#endif

			#if SAFE_MODE
			if(!CanRead)
			{
				#if DEV_MODE && DEBUG_NOT_MIXED_CONTENT
				Debug.Log(StringUtils.ToColorizedString("GetHasMixedContentAfterNicify(", member.ToString(), "): ", false, " because CanRead=", false));
				#endif
				return false;
			}
			#endif

			
			float firstValue = Nicify((float)GetValue(0));
			for(int n = targetCount - 1; n >= 1; n--)
			{
				float otherValue = Nicify((float)GetValue(n));
				if(!ValuesAreEqual(firstValue, otherValue))
				{
					#if DEV_MODE && DEBUG_MIXED_CONTENT
					Debug.Log(StringUtils.ToColorizedString("GetHasMixedContentAfterNicify(", ToString(), "): ", true, " because values["+n+"] ("+ StringUtils.ToString(otherValue) + ") != values[0] (" + StringUtils.ToString(firstValue) + ")"));
					#endif

					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Rounds the value a bit, so that whole numbers don't so easily become displayed as 0.000001 etc. in the inspector
		/// </summary>
		private float Nicify(float value)
		{
			double width = controlLastDrawPosition.width;
			if(width.Equals(0d))
			{
				return value;
			}
			string valueString = value.ToString(StringUtils.DoubleFormat);
			double letterCount = valueString.Length;
			double numberOfDigitsThatFit = Math.Round(width / 7d);
			
			double valueRounded;
			if(numberOfDigitsThatFit < letterCount)
			{
				var accuracy = Math.Pow(10d, numberOfDigitsThatFit - 2d);
				if(accuracy <= 0d)
				{
					valueRounded = Mathf.Round(value);
				}
				else
				{
					valueRounded = Math.Round(value * accuracy) * (1f / accuracy);
				}
			}
			else
			{
				valueRounded = Math.Round(value * 1000000d) * 0.000001d;
			}
			
			if(parent is RotationDrawer)
			{
				if(valueRounded < 0f || valueRounded >= 360f)
				{
					valueRounded = valueRounded % 360f;
				}
			}

			if(!valueRounded.Equals(value))
			{
				#if DEV_MODE && DEBUG_NICIFY
				Debug.Log("Rounded value from "+value.ToString(StringUtils.DoubleFormat)+" to "+valueRounded);
				#endif
				return (float)valueRounded;
			}
			return value;
		}

		/// <summary>
		/// Rounds the value a bit, so that whole numbers don't so easily become displayed as 0.000001 etc. in the inspector
		/// </summary>
		private float Nicify(double value)
		{
			double width = controlLastDrawPosition.width;
			if(width.Equals(0d))
			{
				return 0f;
			}
			string valueString = value.ToString(StringUtils.DoubleFormat);
			double letterCount = valueString.Length;
			double numberOfDigitsThatFit = Math.Round(width / 7d);
			
			double valueRounded;
			if(numberOfDigitsThatFit < letterCount)
			{
				var accuracy = Math.Pow(10d, numberOfDigitsThatFit - 2d);
				if(accuracy <= 0d)
				{
					valueRounded = Math.Round(value);
				}
				else
				{
					valueRounded = Math.Round(value * accuracy) * (1f / accuracy);
				}
			}
			else
			{
				valueRounded = Math.Round(value * 1000000d) * 0.000001d;
			}
			
			if(parent is RotationDrawer)
			{
				if(valueRounded < 0f || valueRounded >= 360f)
				{
					valueRounded = valueRounded % 360f;
				}
			}

			if(!valueRounded.Equals(value))
			{
				#if DEV_MODE && DEBUG_NICIFY
				Debug.Log("Rounded value from "+value.ToString(StringUtils.DoubleFormat)+" to "+valueRounded);
				#endif
				return (float)valueRounded;
			}
			return (float)value;
		}
		
		/// <summary>
		/// Rounds the value a bit to avoid there being so many digits.
		/// </summary>
		private static float RoundToTwoDecimals(float value)
		{
			return Mathf.RoundToInt(value*100f)*0.01f;
		}

		/// <inheritdoc />
		protected override bool ValuesAreEqual(float a, float b)
		{
			return a.Approximately(b);
		}

		/// <summary>
		/// Rounds the value to something that is easily human readable
		/// and best fits the available control width.
		/// E.g. 0.99999999 will simply be displayed as 1.
		/// </summary>
		private void NicifyCachedValue()
		{
			float unnicified = Value;
			float nicified = Nicify(unnicified);
			SetCachedValueSilent(nicified);
		}

		/// <inheritdoc />
		protected override void GetDrawPositions(Rect position)
		{
			lastDrawPosition = position;
			lastDrawPosition.height = Height;

			labelLastDrawPosition = lastDrawPosition;

			//hide the prefix label if there's not enough space or if label is empty
			labelLastDrawPosition.width = labelLastDrawPosition.width >= DrawGUI.MinWidthWithSingleLetterPrefix && (label.text.Length > 0 || label.image != null) ? DrawGUI.SingleLetterPrefixWidth : 0f;

			controlLastDrawPosition = labelLastDrawPosition;
			controlLastDrawPosition.x += labelLastDrawPosition.width;
			controlLastDrawPosition.width = position.width - labelLastDrawPosition.width;

			localDrawAreaOffset = DrawGUI.GetLocalDrawAreaOffset();
		}

		/// <inheritdoc />
		public override bool DrawPrefix(Rect position)
		{
			//draw the prefix label if there's enough space provided
			//(it is dynamically hidden if there's not enough space)
			if(lastDrawPosition.width >= DrawGUI.MinWidthWithSingleLetterPrefix)
			{
				var colorizedXYZLabels = Inspector.Preferences.tintXYZLabels;
				var contentColorWas = GUI.contentColor;
				
				var textColorWas = DrawGUI.prefixLabel.normal.textColor;
				var mouseoveredTextColorWas = DrawGUI.prefixLabelMouseovered.normal.textColor;
				var selectedTextColorWas = DrawGUI.prefixLabelSelected;
				var selectedModifiedTextColorWas = DrawGUI.prefixLabelSelectedModified;
				var modifiedTextColorWas = DrawGUI.prefixLabelModified.normal.textColor;
				var mouseoveredModifiedTextColorWas = DrawGUI.prefixLabelMouseoveredModified.normal.textColor;

				if(colorizedXYZLabels)
				{
					GUI.contentColor = axisColor;
					DrawGUI.prefixLabel.normal.textColor = Color.white;
					DrawGUI.prefixLabelMouseovered.normal.textColor = Color.white;
					DrawGUI.prefixLabelSelected.normal.textColor = Color.white;
					DrawGUI.prefixLabelSelectedModified.normal.textColor = Color.white;
					DrawGUI.prefixLabelModified.normal.textColor = Color.white;
					DrawGUI.prefixLabelMouseoveredModified.normal.textColor = Color.white;
				}

				bool dirty = base.DrawPrefix(PrefixLabelPosition);

				if(colorizedXYZLabels)
				{
					GUI.contentColor = contentColorWas;
					DrawGUI.prefixLabel.normal.textColor = textColorWas;
					DrawGUI.prefixLabelMouseovered.normal.textColor = mouseoveredTextColorWas;
					DrawGUI.prefixLabelSelected = selectedTextColorWas;
					DrawGUI.prefixLabelSelectedModified = selectedModifiedTextColorWas;
					DrawGUI.prefixLabelModified.normal.textColor = modifiedTextColorWas;
					DrawGUI.prefixLabelMouseoveredModified.normal.textColor = mouseoveredModifiedTextColorWas;
				}

				return dirty;
			}

			return false;
		}

		/// <inheritdoc />
		public override void OnPrefixDragged(ref float inputValue, float inputMouseDownValue, float mouseDelta)
		{
			float setValue;
			
			var snappable = (ISnappable)parent;
			bool doSnapping = snappable.SnappingEnabled || DrawGUI.ActionKey;

			// if using high Snapping values, multiply delta value so that it's actually
			// possible to change the value (without moving the mouse several meters)
			if(doSnapping)
			{
				float snapAmount = snappable.GetSnapStep(snappable.GetMemberRowIndex(this));
				if(snapAmount > 1f)
				{
					mouseDelta *= snapAmount;
				}
			}

			if(Mathf.Approximately(mouseDelta, 0f))
			{
				setValue = inputMouseDownValue;
			}
			else
			{
				setValue = inputMouseDownValue + mouseDelta * DragSensitivity;
			}

			#if DEV_MODE && DEBUG_PREFIX_DRAGGED
			var setValueUnsnapped = setValue;
			#endif
			
			if(doSnapping)
			{
				snappable.SnapMemberValue(memberIndex, ref setValue, Nicify);
			}
			else
			{
				//Nicify the value displayed to the user, to make it more easily human readable
				setValue = RoundToTwoDecimals(setValue);
			}
			
			#if DEV_MODE && DEBUG_PREFIX_DRAGGED
			Debug.Log(ToString()+".OnPrefixDragged with mouseDelta="+mouseDelta+"), setUnsnapped="+setValueUnsnapped+", setSnapped="+setValue);
			#endif

			inputValue = setValue;
		}

		/// <inheritdoc />
		public override float DrawControlVisuals(Rect position, float value)
		{
			value = DrawGUI.Active.FloatField(position, value);
			SnapIfSnappingEnabled(ref value, false);
			return value;
		}

		/// <inheritdoc />
		protected override void BuildContextMenu(ref Menu menu, bool extendedMenu)
		{
			if(BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}

			bool canChangeValue = !ReadOnly;
			var val = Value;

			if(canChangeValue)
			{
				menu.AddSeparatorIfNotRedundant();

				var rounded = Mathf.Round(val);
				if(!val.Equals(rounded))
				{
					menu.Add("Round", () => Value = rounded, val.Equals(rounded));
				}
				if(!val.Equals(0f))
				{
					menu.Add("Invert", () => Value = 0f - val);
				}

				if(parent is RotationDrawer)
				{
					menu.Add("Rotate/-90°", () => Value = Value - 90f);
					menu.Add("Rotate/-45°", () => Value = Value - 45f);
					menu.Add("Rotate/45°", () => Value = Value + 45f);
					menu.Add("Rotate/90°", () => Value = Value = Value + 90f);
					menu.Add("Rotate/180°", () => Value = Value + 180f);
				}
				else
				{
					menu.Add("Set To.../0°", () => Value = 0f, val.Equals(0f));
					menu.Add("Set To.../90°", () => Value = 90f, val.Equals(90f));
					menu.Add("Set To.../180°", () => Value = 180f, val.Equals(180f));
					menu.Add("Set To.../270°", () => Value = 270f, val.Equals(270f));
				}
			}

			if(!BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}
		}

		/// <inheritdoc />
		protected override bool GetDataIsValidUpdated()
		{
			var value = Value;
			return value >= MinValue && value <= MaxValue && !float.IsNaN(value);
		}

		/// <inheritdoc />
		protected override float GetRandomValue()
		{
			float setValue = RandomUtils.Float(MinValue, MaxValue);
			SnapIfSnappingEnabled(ref setValue, false);
			return setValue;
		}

		/// <inheritdoc />
		public override object DefaultValue(bool _)
		{
			return parent is ScaleDrawer ? 1f : 0f;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			try
			{
				return StringUtils.Concat(StringUtils.ToString(GetType()), "(\"", parent.Name, ".", Name, "\")");
			}
			//this happens when ToString() is called for a pooled instance
			catch(NullReferenceException)
			{
				string result = StringUtils.Concat(StringUtils.ToString(GetType()), "(\"", Name, "\")");
				#if DEV_MODE
				Debug.LogWarning(result+" ToString() called with parent null, inactive="+StringUtils.ToColorizedString(inactive));
				#endif
				return result;
			}
		}

		/// <inheritdoc />
		public override void Dispose()
		{
			#if UNITY_EDITOR
			DisableOnSceneGUI();
			#endif

			memberIndex = -1;
			axisColor = Color.white;

			base.Dispose();
		}

		#if UNITY_EDITOR
		/// <inheritdoc />
		public override void OnMouseoverEnter(Event inputEvent, bool isDrag)
		{
			base.OnMouseoverEnter(inputEvent, isDrag);

			if(!isDrag)
			{
				EnableOnSceneGUI();
			}
		}
		#endif

		#if UNITY_EDITOR
		/// <inheritdoc />
		public override void OnPrefixDragStart(Event inputEvent)
		{
			base.OnPrefixDragStart(inputEvent);

			EnableOnSceneGUI();
		}
		#endif

		#if UNITY_EDITOR
		public override void OnMouseUpAfterDownOverControl(Event inputEvent, bool isClick)
		{
			#if DEV_MODE && DEBUG_ON_MOUSE_UP
			Debug.Log(ToString()+ ".OnMouseUpAfterDownOverControl with inputEvent="+StringUtils.ToString(inputEvent));
			#endif

			base.OnMouseUpAfterDownOverControl(inputEvent, isClick);

			if(!Mouseovered)
			{
				DisableOnSceneGUI();
			}
		}
		#endif

		#if UNITY_EDITOR
		/// <inheritdoc />
		public override void OnMouseoverExit(Event inputEvent)
		{
			base.OnMouseoverExit(inputEvent);

			if(!Inspector.Manager.MouseDownInfo.IsDrag())
			{
				DisableOnSceneGUI();
			}
		}
		#endif

		#if UNITY_EDITOR
		public void EnableOnSceneGUI()
		{
			if(!subscribedToOnSceneGUI)
			{
				subscribedToOnSceneGUI = true;
				#if UNITY_2019_1_OR_NEWER
				UnityEditor.SceneView.duringSceneGui += OnSceneGUI;
				#else
				UnityEditor.SceneView.onSceneGUIDelegate += OnSceneGUI;
				#endif

				UnityEditor.SceneView.RepaintAll();
			}
		}
		#endif

		#if UNITY_EDITOR
		public void DisableOnSceneGUI()
		{
			#if DEV_MODE && DEBUG_INFINITE_AXIS
			Debug.Log(ToString()+ ".DisableOnSceneGUI");
			#endif

			if(subscribedToOnSceneGUI)
			{
				subscribedToOnSceneGUI = false;
				#if UNITY_2019_1_OR_NEWER
				UnityEditor.SceneView.duringSceneGui -= OnSceneGUI;
				#else
				UnityEditor.SceneView.onSceneGUIDelegate += OnSceneGUI;
				#endif
				UnityEditor.SceneView.RepaintAll();
			}
		}
		#endif
		
		#if UNITY_EDITOR
		private void OnSceneGUI(UnityEditor.SceneView sceneView)
		{
			if(parent is PositionDrawer)
			{
				DrawInfiniteAxisLine();
			}
		}
		#endif
		
		#if UNITY_EDITOR
		private void DrawInfiniteAxisLine()
		{
			UnityEditor.Handles.color = axisColor;
			var dir = AxisDirection;

			var targets = UnityObjects;
			for(int n = targets.Length - 1; n >= 0; n--)
			{
				var transform = (Transform)targets[n];

				// This can happen when changing the loaded scene.
				if(transform == null)
				{
					return;
				}


				var position = transform.position;

				var from = OverflowSafeOffset(position, dir, 1000000f);
				var to = OverflowSafeOffset(position, dir, -1000000f);

				UnityEditor.Handles.DrawLine(from, to);
			}
		}
		#endif

		#if UNITY_EDITOR
		/// <summary>
		/// 
		/// </summary>
		/// <param name="position"> Starting point from which we calculate the offset. </param>
		/// <param name="direction"> Normalized direction vector. Tells which way to move from starting position. </param>
		/// <param name="distance"> Distance multiplier for direction vector. If negative then offset will be calculated in opposite direction. </param>
		/// <returns> Point that at given distance in given direction from starting point. </returns>
		private static Vector3 OverflowSafeOffset(Vector3 position, Vector3 direction, float distance)
		{
			var result = position;
			for(int axis = 0; axis <= 2; axis++)
			{
				try
				{
					float set = position[axis] + direction[axis] * distance;
					result[axis] = set;
				}
				catch(OverflowException)
				{
					if(distance > 0.1f || distance < -0.1f)
					{
						return OverflowSafeOffset(position, direction, distance * 0.1f);
					}
					return position;
				}
			}
			return result;
		}
		#endif
	}
}