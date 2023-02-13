//#define GET_VALUE_DURING_SETUP
#define DEBUG_INVOKE

using System;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;
using UnityEditor;

namespace Sisus
{
	/// <summary>
	/// Drawer that can handle representing any properties and indexers.
	/// Has buttons that allow getting and setting value.
	/// </summary>
	[Serializable]
	public class PropertyDrawer : ParentFieldDrawer<object>
	{
		private static readonly GUIContent GetButtonLabel = new GUIContent("Get");
		private static readonly GUIContent SetButtonLabel = new GUIContent("Set");

		private Rect getButtonRect;
		private Rect setButtonRect;
		private Rect backgroundRect;

		private bool hasParameters;
		private bool canRead;
		private bool canWrite;
		private bool hasResult;
		private bool mouseIsOverGetButton;
		private bool mouseIsOverSetButton;
		private bool showGetButton;
		private bool firstButtonSelected = true;
		private bool autoUpdate;
		private bool drawInSingleRow;

		/// <inheritdoc/>
		public override Type Type
		{
			get
			{
				return memberInfo.PropertyInfo.PropertyType;
			}
		}

		/// <inheritdoc />
		public override object Value
		{
			get
			{
				return GetValue(0);
			}
		}

		/// <inheritdoc/>
		public override bool DrawInSingleRow
		{
			get
			{
				return drawInSingleRow;
			}
		}

		/// <inheritdoc/>
		public override Rect ClickToSelectArea
		{
			get
			{
				return backgroundRect;
			}
		}

		/// <inheritdoc/>
		public override bool CanReadFromFieldWithoutSideEffects
		{
			get
			{
				return false;
			}
		}

		private bool ShowSetButton
		{
			get
			{
				// We could hide the set button when there's no result, but then it would
				// force the user to press the get button at least once before pressing the
				// set button, and this could sometimes be unsafe / impossible (result in an error).
				// Additionally it can be useful to see whether a field is readonly or not at a glance.
				return canWrite && !autoUpdate;
			}
		}

		private PropertyInfo PropertyInfo
		{
			get
			{
				return memberInfo.PropertyInfo;
			}
		}

		private ParameterDrawer IndexParameterDrawer
		{
			get
			{
				return members[0] as ParameterDrawer;
			}

			set
			{
				int index = 0;
				if(members.Length <= index)
				{
					Array.Resize(ref members, index + 1);
				}
				members[index] = value;
			}
		}

		private IDrawer ResultDrawer
		{
			get
			{
				int index = 0;
				if(hasParameters)
				{
					index++;
				}
				if(members.Length > index)
				{
					return members[index];
				}
				return null;
			}
			
			set
			{
				int index = 0;
				if(hasParameters)
				{
					index++;
				}

				bool resized;
				if(members.Length <= index)
				{
					resized = true;
					DrawerArrayPool.Resize(ref members, index+1);
				}
				else
				{
					resized = false;
					var existing = members[index];
					if(existing != null)
					{
						existing.Dispose();
					}
				}
				members[index] = value;

				//if draw in single row was true but no longer is
				//then DrawInSingleRow might have changed
				//in which case we need to rebuild the prefix drawer to
				//make the foldout arrow appear
				if(resized)
				{
					UpdatePrefixDrawer();
				}
			}
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="memberInfo"> LinkedMemberInfo for the property that the created drawers represent. Can be null. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="readOnly"> True if drawer should be read only. </param>
		/// <returns> The drawer instance, ready to be used. </returns>
		[NotNull]
		public static PropertyDrawer Create([NotNull]LinkedMemberInfo memberInfo, [CanBeNull]IParentDrawer parent, [CanBeNull]GUIContent label, bool readOnly, [CanBeNull]string getterErrorOrWarning, LogType getterErrorOrWarningType)
		{
			PropertyDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new PropertyDrawer();
			}

			if(!string.IsNullOrEmpty(getterErrorOrWarning))
			{
				result.OnErrorOrWarningWhenCallingGetter(getterErrorOrWarning, getterErrorOrWarningType);
			}

			object useValue = memberInfo.Type.IsValueType ? memberInfo.DefaultValue() : null;

			result.Setup(useValue, memberInfo.Type, memberInfo, parent, label, readOnly);
			result.LateSetup();

			return result;
		}

		/// <inheritdoc/>
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			Setup(setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc />
		protected override void Setup(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			if(setMemberInfo == null)
			{
				Debug.LogError("Null fieldInfo detected for \"" + (setLabel != null ? setLabel.text : "") + "\"");
				return;
			}

			var propertyInfo = setMemberInfo.PropertyInfo;

			if(propertyInfo == null)
			{
				Debug.LogError("Null PropertyInfo detected for \"" + (setLabel != null ? setLabel.text : "") + "\" / " +setMemberInfo.Name);
				return;
			}
			
			memberInfo = setMemberInfo;
			hasParameters = propertyInfo.GetIndexParameters().Length > 0;
			canRead = propertyInfo.CanRead;
			canWrite = !setReadOnly && propertyInfo.CanWrite;
			showGetButton = canRead;
			
			if(!canRead)
			{
				if(setValue == null)
				{
					setValue = DefaultValue();
				}
				hasResult = true;
			}

			drawInSingleRow = GetDrawInSingleRowUpdated();

			base.Setup(setValue, setValueType, setMemberInfo, setParent, setLabel, false);
		}

		/// <inheritdoc/>
		protected override void DoGenerateMemberBuildList()
		{
			memberBuildList.Add(memberInfo);
		}

		/// <inheritdoc/>
		protected override void DoBuildMembers()
		{
			if(!hasResult && !hasParameters)
			{
				DrawerArrayPool.Resize(ref members, 0);
			}
			else
			{
				if(hasResult)
				{
					ResultDrawer = DrawerProvider.GetForField(base.Value, Type, memberInfo, this, GUIContentPool.Create("Value"), ReadOnly);
				}

				if(hasParameters)
				{
					IndexParameterDrawer = ParameterDrawer.Create(PropertyInfo.GetIndexParameters(), memberInfo, this, GUIContentPool.Create("Parameters"), false);
				}
			}
		}

		/// <inheritdoc />
		protected override bool DoSetValue(object setValue, bool applyToField, bool updateMembers)
		{
			if(SetResult(setValue, applyToField, updateMembers))
			{
				return true;
			}
			return false;
		}

		/// <inheritdoc />
		public override bool SetValue(object setValue)
		{
			if(SetResult(setValue, true, true))
			{
				return true;
			}
			return false;
		}
		
		/// <inheritdoc />
		public override void OnMemberValueChanged(int memberIndex, object memberValue, LinkedMemberInfo memberLinkedMemberInfo)
		{
			if(!autoUpdate)
			{
				HasUnappliedChanges = true;
				UpdateDataValidity(true);

				// don't send OnMemberValueChanged up the parent chain since
				// value changes in Parameters "don't count"
				// (they are not field backed and we know that changing them will
				//  have zero consequences up from this point)
				return;
			}
			
			InvokeSet();
			UpdateDataValidity(true);

			for(int n = members.Length - 1; n >= 0; n--)
			{
				if(n != memberIndex)
				{
					members[n].OnSiblingValueChanged(memberIndex, memberValue, memberLinkedMemberInfo);
				}
			}

			if(parent != null)
			{
				parent.OnMemberValueChanged(Array.IndexOf(parent.Members, this), GetValue(0), memberLinkedMemberInfo);
			}
		}

		/// <inheritdoc />
		public override object GetValue(int index)
		{
			#if DEV_MODE
			if(hasParameters)
			{
				var vDrawer = IndexParameterDrawer.Value;
				var vMemberInfo = memberInfo.IndexParameters;
				Debug.Assert(vDrawer.ContentsMatch(vMemberInfo), "IndexParameterDrawer.Value "+StringUtils.ToString(vDrawer) + " != memberInfo.IndexParameters " + StringUtils.ToString(vMemberInfo));
			}
			#endif

			#if DEV_MODE && DEBUG_INVOKE
			Debug.Log("Invoking "+ memberInfo + " with "+(hasParameters ? "params "+StringUtils.ToString((object)IndexParameterDrawer.Value) : "0 parameters"));
			#endif

			if(memberInfo != null && memberInfo.CanRead && !getValueCausedException)
			{
				using(var logCatcher = new LogCatcher())
				{
					try
					{
						var result = memberInfo.GetValue(index);

						if(logCatcher.HasMessage && logCatcher.LogType != LogType.Log)
						{
							OnErrorOrWarningWhenCallingGetter(logCatcher.Message, logCatcher.LogType);
						}

						return result;
					}
					catch(Exception e)
					{
						if(ExitGUIUtility.ShouldRethrowException(e))
						{
							throw;
						}

						OnExceptionWhenCallingGetter(e);

						Debug.LogError(ToString() + ".GetValue " + e);

						return base.Value;
					}
				}
			}

			#if DEV_MODE
			Debug.LogWarning(ToString()+".GetValue("+index+") called but memberInfo.CanRead="+StringUtils.False);
			#endif

			return base.Value;
		}

		protected override void OnErrorOrWarningWhenCallingGetter(string message, LogType logType)
		{
			base.OnErrorOrWarningWhenCallingGetter(message, logType);
			autoUpdate = false;
		}

		private bool SetResult(object setResult, bool applyToField, bool updateMembers)
		{
			if(hasResult)
			{
				if(ValueEquals(setResult))
				{
					return false;
				}
				#if DEV_MODE
				Debug.Log(StringUtils.ToColorizedString("PropertyDrawer.SetResult(", setResult, ") called with hasResult=", true, " and ValueEquals returned ", false));
				#endif
			}
			else
			{
				hasResult = true;
			}

			SetCachedValueSilent(setResult);
			if(applyToField)
			{
				InvokeSet();
			}
			OnCachedValueChanged(false, updateMembers);
			
			GUI.changed = true;
			InspectorUtility.ActiveInspector.OnNextLayout(RebuildResultDrawer);
			return true;
		}
		
		private void RebuildResultDrawer()
		{
			if(inactive)
			{
				#if DEV_MODE
				Debug.LogWarning("Ignoring RebuildResuiltDrawer because inactive");
				#endif
				return;
			}

			if(!hasResult)
			{
				#if DEV_MODE
				Debug.LogWarning("Ignoring RebuildResuiltDrawer because hasResult was false");
				#endif
				return;
			}

			#if DEV_MODE
			Debug.Log("RebuildResultDrawer from result: "+StringUtils.ToString(base.Value));
			#endif

			ResultDrawer = DrawerProvider.GetForField(base.Value, Type, null, this, GUIContentPool.Create("Result"), ReadOnly);
			UpdateVisibleMembers();
			drawInSingleRow = GetDrawInSingleRowUpdated();
		}

		/// <inheritdoc />
		public override void UpdateCachedValuesFromFieldsRecursively()
		{
			if(autoUpdate)
			{
				InvokeGet();
			}
		}

		/// <inheritdoc />
		public override float GetOptimalPrefixLabelWidth(int indentLevel)
		{
			return ParentDrawerUtility.GetOptimalPrefixLabelWidth(this, indentLevel, false);
		}

		/// <inheritdoc />
		protected override void DoPasteFromClipboard() { CantSetValueError(); }

		/// <inheritdoc />
		protected override void DoReset() { CantSetValueError(); }

		private static void CantSetValueError(){ Debug.LogError("Button value can't be changed"); }

		/// <inheritdoc />
		protected override void GetDrawPositions(Rect position)
		{
			lastDrawPosition = position;
			lastDrawPosition.height = HeaderHeight;

			bodyLastDrawPosition = lastDrawPosition;
			bodyLastDrawPosition.y += lastDrawPosition.height;

			labelLastDrawPosition = lastDrawPosition;
			DrawGUI.AddMarginsAndIndentation(ref labelLastDrawPosition);
			if(!DrawInSingleRow)
			{
				float foldoutArrowSize = 12f;
				labelLastDrawPosition.x -= foldoutArrowSize;
				labelLastDrawPosition.width += foldoutArrowSize;
			}

			backgroundRect = lastDrawPosition;
			backgroundRect.x += DrawGUI.RightPadding;
			backgroundRect.width -= DrawGUI.RightPadding + DrawGUI.RightPadding;

			if(showGetButton)
			{
				if(ShowSetButton)
				{
					getButtonRect = labelLastDrawPosition;
					float totalWidth = DrawGUI.MinControlFieldWidth - DrawGUI.MiddlePadding - DrawGUI.RightPadding - 1f;
					getButtonRect.x = lastDrawPosition.xMax - totalWidth - DrawGUI.RightPadding - 2f;
					float buttonWidth = (totalWidth - 3f) * 0.5f;
					getButtonRect.width = buttonWidth;
					getButtonRect.y += 1f;
					getButtonRect.height -= 2f;

					setButtonRect = getButtonRect;
					setButtonRect.x += getButtonRect.width + 3f;

					if(DrawInSingleRow)
					{
						bodyLastDrawPosition = getButtonRect;
						bodyLastDrawPosition.width = totalWidth;
					}
				}
				else
				{
					getButtonRect = labelLastDrawPosition;
					getButtonRect.width = DrawGUI.MinControlFieldWidth - DrawGUI.RightPadding - DrawGUI.MiddlePadding - 1f;
					getButtonRect.x = lastDrawPosition.xMax - getButtonRect.width - DrawGUI.RightPadding - 2f;
					getButtonRect.y += 1f;
					getButtonRect.height -= 2f;

					if(DrawInSingleRow)
					{
						bodyLastDrawPosition = getButtonRect;
					}
				}

				labelLastDrawPosition.width = getButtonRect.x - labelLastDrawPosition.x;
			}
			else if(ShowSetButton)
			{
				setButtonRect = labelLastDrawPosition;
				setButtonRect.width = DrawGUI.MinControlFieldWidth - DrawGUI.RightPadding - DrawGUI.MiddlePadding - 1f;
				setButtonRect.x = lastDrawPosition.xMax - setButtonRect.width - DrawGUI.RightPadding - 2f;
				setButtonRect.y += 1f;
				setButtonRect.height -= 2f;

				labelLastDrawPosition.width = setButtonRect.x - labelLastDrawPosition.x;
				
				if(DrawInSingleRow)
				{
					bodyLastDrawPosition = setButtonRect;
				}
			}

			localDrawAreaOffset = DrawGUI.GetLocalDrawAreaOffset();
		}

		/// <inheritdoc />
		public override bool DrawPrefix(Rect position)
		{
			//hide prefix resizer line behind the field
			DrawGUI.Active.ColorRect(lastDrawPosition, DrawGUI.Active.InspectorBackgroundColor);

			GUI.Label(backgroundRect, "", InspectorPreferences.Styles.MethodBackground);

			bool drawBackgroundBehindFoldoutsWas = DrawGUI.drawBackgroundBehindFoldouts;
			DrawGUI.drawBackgroundBehindFoldouts = false;
			
			var labelPos = PrefixLabelPosition;
			//use BeginArea to prevent foldout text clipping past the Button
			GUILayout.BeginArea(labelPos);
			labelPos.x = 0f;
			labelPos.y = 0f;
			bool dirty = base.DrawPrefix(labelPos);
			GUILayout.EndArea();
			
			DrawGUI.drawBackgroundBehindFoldouts = drawBackgroundBehindFoldoutsWas;
			
			bool highlightButton = Selected && (showGetButton || ShowSetButton);
			var guiColorWas = GUI.color;
			
			if(label.tooltip.Length > 0)
			{
				ParentDrawerUtility.HandleTooltipBeforeControl(label, getButtonRect);
			}

			if(showGetButton)
			{
				if(highlightButton && firstButtonSelected)
				{
					var col = InspectorUtility.Preferences.theme.ButtonSelected;
					GUI.color = col;
				}

				if(getValueCausedException)
				{
					GetButtonLabel.tooltip = getOrSetValueExceptionLabel.tooltip;
				}

				if(DrawGUI.Active.Button(getButtonRect, GetButtonLabel, InspectorPreferences.Styles.MiniButton))
				{
					dirty = true;
					InvokeGet();
					Select(ReasonSelectionChanged.ControlClicked);
					DrawGUI.UseEvent();
				}

				if(getValueCausedException)
				{
					GetButtonLabel.tooltip = "";
					GUI.Label(getButtonRect, getOrSetValueExceptionLabel);
				}
			}

			if(ShowSetButton)
			{
				if(highlightButton)
				{
					if(!firstButtonSelected || !showGetButton)
					{
						var col = InspectorUtility.Preferences.theme.ButtonSelected;
						GUI.color = col;
					}
					else
					{
						GUI.color = guiColorWas;
					}
				}

				if(setValueCausedException)
				{
					SetButtonLabel.tooltip = getOrSetValueExceptionLabel.tooltip;
				}

				if(DrawGUI.Active.Button(setButtonRect, SetButtonLabel, InspectorPreferences.Styles.MiniButton))
				{
					dirty = true;
					InvokeSet();
					Select(ReasonSelectionChanged.ControlClicked);
					DrawGUI.UseEvent();
				}

				if(setValueCausedException)
				{
					SetButtonLabel.tooltip = "";
					GUI.Label(setButtonRect, getOrSetValueExceptionLabel);
				}
			}

			if(highlightButton)
			{
				GUI.color = guiColorWas;
			}

			return dirty;
		}

		/// <inheritdoc />
		protected override void OnLayoutEvent(Rect position)
		{
			if(Cursor.CanRequestLocalPosition)
			{
				if(showGetButton && getButtonRect.MouseIsOver())
				{
					mouseIsOverGetButton = true;
					mouseIsOverSetButton = false;
				}
				else if(ShowSetButton && setButtonRect.MouseIsOver())
				{
					mouseIsOverSetButton = true;
					mouseIsOverGetButton = false;
				}
				else
				{
					mouseIsOverGetButton = false;
					mouseIsOverSetButton = false;
				}
			}
			base.OnLayoutEvent(position);
		}

		/// <inheritdoc />
		protected override void BuildContextMenu(ref Menu menu, bool extendedMenu)
		{
			if(BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}

			if(canRead || canWrite)
			{
				menu.AddSeparatorIfNotRedundant();
				if(canRead)
				{
					menu.Add("Get", InvokeGet);
				}
				if(canWrite)
				{
					menu.Add("Set", InvokeSet);
				}
			}

			menu.AddSeparatorIfNotRedundant();
			if(autoUpdate)
			{
				menu.Add("Auto-Update", DisableAutoUpdate, true);
			}
			else
			{
				menu.Add("Auto-Update", EnableAutoUpdate, false);
			}

			if(!BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}
		}
		
		/// <inheritdoc />
		public override void CopyToClipboard(int index)
		{
			if(!canRead)
			{
				InspectorUtility.ActiveInspector.Message(Clipboard.MakeInvalidOperationMessage(Name + " has no getter."), null, MessageType.Info, false);
				return;
			}

			if(!hasResult)
			{
				InvokeGet();
			}
			base.CopyToClipboard(index);
		}

		/// <inheritdoc />
		public override void OnMouseover()
		{
			if(mouseIsOverGetButton)
			{
				DrawGUI.DrawMouseoverEffect(getButtonRect, localDrawAreaOffset);
				DrawGUI.Active.AddCursorRect(getButtonRect, MouseCursor.Link);
			}
			else if(mouseIsOverSetButton)
			{
				DrawGUI.DrawMouseoverEffect(setButtonRect, localDrawAreaOffset);
				DrawGUI.Active.AddCursorRect(setButtonRect, MouseCursor.Link);
			}
			else if(InspectorUtility.Preferences.mouseoverEffects.prefixLabel)
			{
				var rect = labelLastDrawPosition;
				rect.y += 1f;
				rect.height -= 2f;
				DrawGUI.DrawLeftClickAreaMouseoverEffect(rect, localDrawAreaOffset);
			}
		}

		/// <inheritdoc />
		public override bool OnClick(Event inputEvent)
		{
			#if DEV_MODE && DEBUG_CLICK
			Debug.Log(ToString()+".OnClick with mouseIsOverGetButton="+ mouseIsOverGetButton+ ", mouseIsOverSetButton="+ mouseIsOverSetButton);
			#endif

			if(mouseIsOverGetButton)
			{
				if(inputEvent.control)
				{
					if(autoUpdate)
					{
						DisableAutoUpdate();
					}
					else
					{
						EnableAutoUpdate();
					}
				}

				DrawGUI.Use(inputEvent);
				InvokeGet();
				GUI.changed = true;
				firstButtonSelected = true;
				return true;
			}
			
			if(mouseIsOverSetButton)
			{
				if(inputEvent.control)
				{
					if(autoUpdate)
					{
						DisableAutoUpdate();
					}
					else
					{
						EnableAutoUpdate();
					}
				}

				DrawGUI.Use(inputEvent);
				InvokeSet();
				GUI.changed = true;
				firstButtonSelected = !showGetButton;
				return true;
			}

			return base.OnClick(inputEvent);
		}

		/// <inheritdoc />
		protected override void OnDeselectedInternal(ReasonSelectionChanged reason, IDrawer losingFocusTo)
		{
			base.OnDeselectedInternal(reason, losingFocusTo);
			firstButtonSelected = true;
		}

		/// <inheritdoc />
		protected override void OnSelectedInternal(ReasonSelectionChanged reason, IDrawer previous, bool isMultiSelection)
		{
			base.OnSelectedInternal(reason, previous, isMultiSelection);

			if(previous != null)
			{
				switch(reason)
				{
					case ReasonSelectionChanged.SelectControlDown:
					case ReasonSelectionChanged.SelectControlUp:
						var previousParent = previous.Parent;
						if(previousParent != null)
						{
							int index = previousParent.GetMemberRowIndex(previous);
							firstButtonSelected = index < 2;
						}
						break;
				}
			}
		}

		/// <inheritdoc />
		public override bool OnKeyboardInputGiven(Event inputEvent, KeyConfigs keys)
		{
			if(DrawGUI.EditingTextField)
			{
				return false;
			}

			switch(inputEvent.keyCode)
			{
				case KeyCode.RightArrow:
					if(showGetButton && ShowSetButton && firstButtonSelected)
					{
						firstButtonSelected = false;
						GUI.changed = true;
						return true;
					}
					break;
				case KeyCode.LeftArrow:
					if(showGetButton && ShowSetButton && !firstButtonSelected)
					{
						firstButtonSelected = true;
						GUI.changed = true;
						return true;
					}
					break;
				case KeyCode.Return:
				case KeyCode.KeypadEnter:
					GUI.changed = true;
					DrawGUI.Use(inputEvent);
					if(showGetButton && firstButtonSelected)
					{
						InvokeGet();
					}
					else if(ShowSetButton && (!showGetButton || !firstButtonSelected))
					{
						InvokeSet();
					}
					return true;
			}

			return base.OnKeyboardInputGiven(inputEvent, keys);
		}

		/// <inheritdoc />
		public override int GetSelectedRowIndex()
		{
			if(parent != null && parent.DrawInSingleRow)
			{
				return parent.GetMemberRowIndex(this);
			}
			return firstButtonSelected ? 0 : 1;
		}

		/// <inheritdoc />
		public override int GetRowSelectableCount()
		{
			if(parent != null && parent.DrawInSingleRow)
			{
				return parent.VisibleMembers.Length + 1;
			}
			return 2; //two buttons
		}
		
		private void InvokeGet()
		{
			getValueCausedException = false;
			object newResult = GetValue(0);
			if(getValueCausedException)
			{
				hasResult = false;
				return;
			}
			SetResult(newResult, false, false);
		}

		private void InvokeSet()
		{
			#if DEV_MODE
			Debug.Log(ToString() + ".InvokeSet with hasResult="+ hasResult+ ", RebuildResultDrawer="+ StringUtils.ToString(ResultDrawer));
			#endif

			if(!hasResult)
			{
				if(!ReadOnly && !getValueCausedException)
				{
					InvokeGet();
					return;
				}
				
				#if DEV_MODE && PI_ASSERTATIONS
				if(ReadOnly) {Debug.LogError("Set-Only property had no result!"); }
				#endif

				SetResult(DefaultValue(), false, true);
				return;
			}
			
			HasUnappliedChanges = false;
			var setValue = ResultDrawer.GetValue(0);

			//new test, because the rest should now be handled by the LinkedMemberInfo and IndexerData class
			memberInfo.SetValue(setValue);
		}
		
		private void EnableAutoUpdate()
		{
			autoUpdate = true;

			showGetButton = false;

			if(!showGetButton || !ShowSetButton)
			{
				firstButtonSelected = true;
			}

			if(!hasResult)
			{
				InvokeGet();
			}

			drawInSingleRow = GetDrawInSingleRowUpdated();
		}

		private void DisableAutoUpdate()
		{
			autoUpdate = false;

			showGetButton = canRead;

			if(!showGetButton || !ShowSetButton)
			{
				firstButtonSelected = true;
			}

			drawInSingleRow = GetDrawInSingleRowUpdated();
		}

		private bool GetDrawInSingleRowUpdated()
		{
			return hasParameters ? false : !hasResult;
		}

		/// <inheritdoc />
		public override void Dispose()
		{
			hasResult = false;
			firstButtonSelected = true;
			autoUpdate = false;
			base.Dispose();
		}

		/// <inheritdoc />
		protected override bool GetHasUnappliedChangesUpdated()
		{
			return HasUnappliedChanges;
		}
	}
}