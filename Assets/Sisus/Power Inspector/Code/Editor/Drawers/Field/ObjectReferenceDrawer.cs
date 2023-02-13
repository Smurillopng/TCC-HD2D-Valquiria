//#define DEBUG_APPLY_VALUE
//#define DEBUG_DRAG_MOUSEOVER
//#define DEBUG_UNAPPLIED_CHANGES
//#define DEBUG_OBJECT_PICKER
//#define DEBUG_KEYBOARD_INPUT

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Sisus.Attributes;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Sisus
{
	/// <summary>
	/// Drawer representing UnityEngine.Object references, including Components, GameObject and various assets.
	/// </summary>
	[Serializable, DrawerForField(typeof(Object), true, true)]
	public class ObjectReferenceDrawer : PrefixControlComboDrawer<Object>
	{
		private const float ObjectPickerWidth = 18f;
		private static List<PopupMenuItem> generatedMenuItems = new List<PopupMenuItem>(30);
		private static Dictionary<string, PopupMenuItem> generatedGroupsByLabel = new Dictionary<string, PopupMenuItem>(10);
		private static Dictionary<string, PopupMenuItem> generatedItemsByLabel = new Dictionary<string, PopupMenuItem>(20);

		private static readonly List<Component> GetComponents = new List<Component>();
		private static readonly List<Object> FindObjects = new List<Object>();

		public static Func<Object> eyedropperCurrentTarget;
		public static Action<ObjectReferenceDrawer> onStartedUsingEyedropper;
		public static Action<ObjectReferenceDrawer> onStoppedUsingEyedropper;

		private Type type;
		private bool allowSceneObjects = true;
		private bool hasUnappliedChanges;
		private Object valueUnapplied;

		private bool objectFieldMouseovered;
		private bool objectPickerButtonMouseovered;
		private bool eyedropperMouseovered;

		private bool listeningForObjectPickerClosedWithUnappliedChanges;
		private bool listeningForObjectPickerClosed;

		private bool usingEyedropper;
		private bool viewWasLockedWhenStartedUsingEyedropper;
		private Object[] selectionWasWhenStartedUsingEyedropper;

		private bool drawEyedropper;

		private Rect eyedropperRect;
		private Rect objectFieldRectIncludingPicker;
		private Rect objectFieldRectExcludingPicker;

		/// <inheritdoc/>
		public override Part MouseoveredPart
		{
			get
			{
				return objectPickerButtonMouseovered ? Part.Picker : eyedropperMouseovered ? Part.Eyedropper : base.MouseoveredPart;
			}
		}

		/// <inheritdoc/>
		public override Type Type
		{
			get
			{
				return type;
			}
		}

		/// <inheritdoc/>
		protected override bool CanBeNull
		{
			get
			{
				return true;
			}
		}

		/// <summary>
		/// Gets the draw position and dimensions of the drag-n-drop region of the control.
		/// This is the control's bounds, without the object picker icon.
		/// </summary>
		/// <value> The object field position. </value>
		private Rect DragNDropAreaPosition
		{
			get
			{
				return objectFieldRectExcludingPicker;
			}
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="value"> The starting cached value of the drawer. </param>
		/// <param name="type"> The type constraint for the UnityEngine.Objects that can be dragged to the field. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="allowSceneObjects"> True if can assing scene objects to field, in addition to assets. </param>
		/// <param name="drawEyedropper"> Draw eyedropper icon next to object picker icon. </param>
		/// <param name="readOnly"> True if control should be read only. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static ObjectReferenceDrawer Create(Object value, Type type, IParentDrawer parent, GUIContent label, bool allowSceneObjects, bool drawEyedropper, bool readOnly)
		{
			ObjectReferenceDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new ObjectReferenceDrawer();
			}
			result.Setup(value, type, null, parent, label, allowSceneObjects, drawEyedropper, readOnly);
			result.LateSetup();
			return result;
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="value"> The starting cached value of the drawer. </param>
		/// <param name="memberInfo"> LinkedMemberInfo for the field, property or parameter that the drawer represents. </param>
		/// <param name="type"> The type constraint for the UnityEngine.Objects that can be dragged to the field. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="allowSceneObjects"> True if can assing scene objects to field, in addition to assets. </param>
		/// <param name="drawEyedropper"> Draw eyedropper icon next to object picker icon. </param>
		/// <param name="readOnly"> True if control should be read only. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static ObjectReferenceDrawer Create(Object value, [CanBeNull]LinkedMemberInfo memberInfo, Type type, IParentDrawer parent, GUIContent label, bool allowSceneObjects, bool drawEyedropper, bool readOnly)
		{
			ObjectReferenceDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new ObjectReferenceDrawer();
			}
			result.Setup(value, type, memberInfo, parent, label, allowSceneObjects, drawEyedropper, readOnly);
			result.LateSetup();
			return result;
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="value"> The starting cached value of the drawer. </param>
		/// <param name="memberInfo">
		/// LinkedMemberInfo for the field, property or parameter that the created drawer
		/// represent. If null, all UnityEngine.Object types can be assigned to the field.
		/// </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="allowSceneObjects"> True if can assing scene objects to field, in addition to assets. </param>
		/// <param name="drawEyedropper"> Draw eyedropper icon next to object picker icon. </param>
		/// <param name="readOnly"> True if control should be read only. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static ObjectReferenceDrawer Create(Object value, [CanBeNull]LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool allowSceneObjects, bool drawEyedropper, bool readOnly)
		{
			ObjectReferenceDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new ObjectReferenceDrawer();
			}
			result.Setup(value, null, memberInfo, parent, label, allowSceneObjects, drawEyedropper, readOnly);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc />
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			Setup((Object)setValue, setValueType, setMemberInfo, setParent, setLabel, true, Inspector.Preferences.drawObjectFieldEyedropper, setReadOnly);
		}

		/// <inheritdoc/>
		protected sealed override void Setup(Object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			throw new NotSupportedException("Please use the other Setup method");
		}

		private void Setup([CanBeNull]Object setValue, [CanBeNull]Type setValueType, [CanBeNull]LinkedMemberInfo setMemberInfo, [CanBeNull]IParentDrawer setParent, GUIContent setLabel, bool setAllowSceneObjects, bool setDrawEyedropper, bool setReadOnly)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(!hasUnappliedChanges, ToString(setLabel, setMemberInfo)+".Setup - hasUnappliedChanges was "+StringUtils.True);
			#endif

			if(setValueType == null)
			{
				if(setMemberInfo != null)
				{
					setValueType = setMemberInfo.Type;
					if(!setValueType.IsUnityObject())
					{
						setValueType = Types.UnityObject;
					}
				}
				else
				{
					setValueType = Types.UnityObject;
				}
			}
			else if(!setValueType.IsUnityObject())
			{
				setValueType = Types.UnityObject;
			}
			else if(!setAllowSceneObjects)
			{
				setAllowSceneObjects = setValueType == Types.UnityObject || setValueType.IsGameObject() || setValueType.IsComponent() || setValueType.IsInterface;
			}

			#if DEV_MODE
			Debug.Assert(setValueType != null);
			Debug.Assert(setValueType.IsUnityObject());
			if(setMemberInfo != null)
			{
				Debug.Assert(setMemberInfo.Type != null, ToString(setLabel, setMemberInfo)+" fieldInfo.Type was null "+ setMemberInfo);
				Debug.Assert(setMemberInfo.Type.IsInterface || setMemberInfo.Type.IsAssignableFrom(setValueType), ToString(setLabel, setMemberInfo) + " fieldInfo " + StringUtils.ToString(setMemberInfo.Type) + " not assignable from type parameter " + StringUtils.ToString(setValueType));
			}
			#endif
			
			allowSceneObjects = setAllowSceneObjects;

			type = setValueType;
			
			base.Setup(setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);

			valueUnapplied = Value;

			drawEyedropper = setDrawEyedropper && !ReadOnly;
		}

		/// <inheritdoc/>
		protected override bool DoSetValue(Object setValue, bool applyToField, bool updateMembers)
		{
			if(setValue != null)
			{
				if(!type.IsAssignableFrom(setValue.GetType()))
				{
					#if DEV_MODE
					Debug.LogWarning(ToString()+".Value.set type "+ StringUtils.ToString(type) + " not assignable from "+ StringUtils.TypeToString(setValue));
					#endif
					return false;
				}

				// Sometimes Type might be Object but actual field type is an interface
				if(memberInfo != null && !memberInfo.Type.IsAssignableFrom(setValue.GetType()))
				{
					#if DEV_MODE
					Debug.LogWarning(ToString()+".Value.set type "+ StringUtils.ToString(type) + " not assignable from "+ StringUtils.TypeToString(setValue));
					#endif
					return false;
				}
			}

			#if DEV_MODE
			Debug.Assert(setValue == null || memberInfo == null || memberInfo.Type.IsAssignableFrom(setValue.GetType()));
			#endif
			
			valueUnapplied = setValue;
			SetHasUnappliedChanges(false);

			return base.DoSetValue(setValue, applyToField, updateMembers);
		}

		/// <inheritdoc/>
		public override void UpdateCachedValuesFromFieldsRecursively()
		{
			//Don't update cached values while object picker is open
			if(ObjectPicker.IsOpen)
			{
				//Debug.Log("ObjectPickerIsOpen...");
				return;
			}

			//Don't update cached values while values picked using object picker
			//haven't been applied yet
			if(hasUnappliedChanges)
			{
				return;
			}

			base.UpdateCachedValuesFromFieldsRecursively();
			valueUnapplied = Value;
		}

		/// <summary>
		/// Just Draw the control with current value and return changes made to the value via the control,
		/// without fancy features like data validation color coding
		/// </summary>
		public override Object DrawControlVisuals(Rect position, Object inputValue)
		{
			if(ObjectPicker.IsOpen)
			{
				if(hasUnappliedChanges)
				{
					var color = InspectorUtility.Preferences.theme.ControlUnappliedChangesTint;
					color.a = 1f;
					DrawGUI.DrawMouseoverEffect(DragNDropAreaPosition, color);
				}
			}

			if(usingEyedropper)
			{
				DrawGUI.Active.AddCursorRect(new Rect(0f, 0f, Screen.width, Screen.height), MouseCursor.ArrowPlus);

				if(eyedropperCurrentTarget != null)
				{
					valueUnapplied = eyedropperCurrentTarget();

					if(valueUnapplied != null && EyedropperTargetIsAcceptableValue(valueUnapplied))
					{
						var color = InspectorUtility.Preferences.theme.ControlUnappliedChangesTint;
						color.a = 1f;
						DrawGUI.DrawMouseoverEffect(DragNDropAreaPosition, color);
					}
					else
					{
						valueUnapplied = inputValue;
					}
				}
			}

			if(PopupMenuManager.IsOpen && PopupMenuManager.LastmenuOpenedForDrawer == this)
			{
				var selected = PopupMenu.SelectedItem;
				if(selected != null && !selected.IsGroup && Value != selected.IdentifyingObject as Object)
				{
					var color = InspectorUtility.Preferences.theme.ControlUnappliedChangesTint;
					color.a = 1f;
					DrawGUI.DrawMouseoverEffect(DragNDropAreaPosition, color);
				}
			}

			Object setValueUnapplied;
			if(drawEyedropper)
			{
				if(Event.current.type == EventType.Repaint)
				{
					// Don't tint the eye dropper red if object field has invalid data
					if(DrawerUtility.GUIColorHistory.Count > 0 && !DataIsValid)
					{
						var invalidColor = GUI.color;
						GUI.color = DrawerUtility.GUIColorHistory.Peek();
						InspectorPreferences.Styles.EyeDropper.Draw(eyedropperRect, Mouseovered, usingEyedropper, false, SelectedAndInspectorHasFocus);
						EditorGUI.DrawRect(objectFieldRectIncludingPicker, DrawGUI.Active.InspectorBackgroundColor);
						GUI.color = invalidColor;
					}
					else
					{
						InspectorPreferences.Styles.EyeDropper.Draw(eyedropperRect, Mouseovered, usingEyedropper, false, SelectedAndInspectorHasFocus);
						EditorGUI.DrawRect(objectFieldRectIncludingPicker, DrawGUI.Active.InspectorBackgroundColor);
					}
				}
			}
			setValueUnapplied = DrawGUI.Active.ObjectField(objectFieldRectIncludingPicker, valueUnapplied, Type, allowSceneObjects);


			if(setValueUnapplied != valueUnapplied)
			{
				#if DEV_MODE && DEBUG_OBJECT_PICKER
				Debug.Log(Msg("valueUnapplied = ", setValueUnapplied," (was: ", valueUnapplied, ") with inputValue=", inputValue, ", Value=", Value));
				#endif
				valueUnapplied = setValueUnapplied;
				SetHasUnappliedChanges(inputValue != valueUnapplied);
			}
			
			if(ObjectPicker.IsOpen)
			{
				//don't apply changes while the object picker is open
				//so that we can e.g. revert back to previous value when escape is pressed
				return inputValue;
			}

			if(usingEyedropper)
			{
				return inputValue;
			}

			if(hasUnappliedChanges)
			{
				// try to figure out whether should apply the value last selected via the Object Picker
				// or if should discard it and revert to previous value (e.g. because escape was pressed)
				// this is tough, because there can be a number of Event calls (Layout, Repaint) before
				// the Escape, Return or KeypadEnter event gets through here. Two other ways to apply
				// the value is by double clicking an entry in the object picker, or by click off-screen
				// of the object picker to close it.
				var inspector = InspectorUtility.ActiveInspector;
				inspector.RefreshView();
				
				// re-focus the EditorWindow that was used to open the Object Picker - it always loses
				// focus to the Object Picker window when it's opened.
				if(!inspector.InspectorDrawer.HasFocus)
				{
					inspector.InspectorDrawer.FocusWindow();
				}

				switch(Event.current.keyCode)
				{
					//the object picker was closed using the esc key: discard the value
					case KeyCode.Escape:
						DiscardUnappliedChanges();
						StopUsingEyeDropper();
						return inputValue;
					//the object was picked using enter or return key: apply the value
					case KeyCode.KeypadEnter:
					case KeyCode.Return:
						ApplyUnappliedChanges();
						return inputValue;
				}

				//if no other applicable KeyCodes were detected until the next time the mouse was moved, then it's
				//safe to assume that the user closed the object picker either by double clicking an object in the view
				//or by clicking off-window and thus causing the window to close. In both instances the value should be applied.
				if(Event.current.isMouse)
				{
					ApplyUnappliedChanges();
					return inputValue;
				}

				//handle value being changed via drag n drop
				var lastInput = DrawGUI.LastInputEvent();
				if(lastInput != null && lastInput.type == EventType.DragPerform)
				{
					ApplyUnappliedChanges();
				}
			}
			
			return inputValue;
		}	

		private void StartUsingEyeDropper()
		{
			var inspector = Inspector;
			inspector.Manager.MouseDownInfo.onMouseUp += OnMouseUpWhileUsingEyedropper;
			inspector.InspectorDrawer.SelectionManager.OnNextSelectionChanged(OnSelectionChangedWhileUsingEyedropper);
			usingEyedropper = true;
			viewWasLockedWhenStartedUsingEyedropper = inspector.State.ViewIsLocked;
			selectionWasWhenStartedUsingEyedropper = inspector.InspectorDrawer.SelectionManager.Selected;
			inspector.State.ViewIsLocked = true;

			if(onStartedUsingEyedropper != null)
			{
				onStartedUsingEyedropper(this);
			}
		}

		private void OnMouseUpWhileUsingEyedropper(IDrawer mouseDownOverDrawer, bool isClick)
		{
			#if DEV_MODE
			Debug.Log(ToString()+ ".OnMouseUpWhileUsingEyedropper");
			#endif

			Inspector.Manager.MouseDownInfo.onMouseUp -= OnMouseUpWhileUsingEyedropper;
			StopUsingEyeDropper();
		}

		private void OnSelectionChangedWhileUsingEyedropper(Object[] selected)
		{
			#if DEV_MODE
			Debug.Log(ToString()+ ".OnSelectionChangedWhileUsingEyedropper");
			#endif

			if(!usingEyedropper)
			{
				return;
			}

			AssignFromUserProvidedRootObjects(selected, false);
		}

		/// <inheritdoc/>
		protected override void OnDeselectedInternal(ReasonSelectionChanged reason, IDrawer losingFocusTo)
		{
			#if DEV_MODE
			Debug.Log(ToString()+ ".OnDeselectedInternal reason="+ reason+ ", losingFocusTo="+StringUtils.ToString(losingFocusTo)+", usingEyedropper="+usingEyedropper+", inspected="+ StringUtils.ToString(Inspector.State.inspected)+", Selected="+ StringUtils.ToString(Inspector.InspectorDrawer.SelectionManager.Selected) +", SelectedWas="+ StringUtils.ToString(selectionWasWhenStartedUsingEyedropper));
			#endif

			if(usingEyedropper)
			{
				if(losingFocusTo == null)
				{
					OnNextLayout(()=>Select(ReasonSelectionChanged.OtherClicked));
				}
				else
				{
					AssignFromUserProvidedRootObjects(losingFocusTo.UnityObjects, false);
					StopUsingEyeDropper();
				}
			}

			base.OnDeselectedInternal(reason, losingFocusTo);
		}

		private void StopUsingEyeDropper()
		{
			if(!usingEyedropper)
			{
				return;
			}

			#if DEV_MODE
			if(Inspector == null) { Debug.Log(ToString() + ".StopUsingEyeDropper with Inspector=null"); }
			else{ Debug.Log(ToString()+ ".StopUsingEyeDropper with inspected="+ StringUtils.ToString(Inspector.State.inspected)+", Selected="+ StringUtils.ToString(Inspector.InspectorDrawer.SelectionManager.Selected) +", SelectedWas="+ StringUtils.ToString(selectionWasWhenStartedUsingEyedropper)); }
			#endif

			usingEyedropper = false;
			var inspector = Inspector;
			if(inspector != null)
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(inspector.State.ViewIsLocked);
				#endif

				inspector.Manager.MouseDownInfo.onMouseUp -= OnMouseUpWhileUsingEyedropper;
				inspector.InspectorDrawer.SelectionManager.CancelOnNextSelectionChanged(OnSelectionChangedWhileUsingEyedropper);

				inspector.Select(selectionWasWhenStartedUsingEyedropper);

				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(inspector.InspectorDrawer.SelectionManager.Selected.ContentsMatch(selectionWasWhenStartedUsingEyedropper));
				#endif

				inspector.State.ViewIsLocked = viewWasLockedWhenStartedUsingEyedropper;
			}

			if(onStoppedUsingEyedropper != null)
			{
				onStoppedUsingEyedropper(this);
			}
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
					if(usingEyedropper)
					{
						DiscardUnappliedChanges();
						StopUsingEyeDropper();
					}
					else if(hasUnappliedChanges)
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
					else if(Value != null)
					{
						DrawGUI.Ping(Values);
					}
					else
					{
						DisplayTargetSelectMenu();
					}
					return true;
			}
			return base.OnKeyboardInputGiven(inputEvent, keys);
		}

		/// <inheritdoc/>
		public override void OnMiddleClick(Event inputEvent)
		{
			if(Value != null)
			{
				DrawGUI.Use(inputEvent);
				Peek();
			}
		}

		/// <inheritdoc/>
		public override void CopyToClipboard()
		{
			if(MixedContent)
			{
				try
				{
					Clipboard.CopyObjectReferences(Values, type);
					SendCopyToClipboardMessage();
				}
				#if DEV_MODE
				catch(Exception e)
				{
					Debug.LogWarning(e);
				#else
				catch
				{
				#endif
					SendCopyToClipboardMessage();
				}
				return;
			}
			try
			{
				Clipboard.CopyObjectReference(Value, type);
				SendCopyToClipboardMessage();
			}
			#if DEV_MODE
			catch(Exception e)
			{
				Debug.LogWarning(e);
			#else
			catch
			{
			#endif
				SendCopyToClipboardMessage();
			}
		}

		/// <inheritdoc/>
		public override void CopyToClipboard(int index)
		{
			try
			{
				Clipboard.CopyObjectReference(GetValue(index) as Object, Type);
				SendCopyToClipboardMessage();
			}
			#if DEV_MODE
			catch(Exception e)
			{
				Debug.LogWarning(e);
			#else
			catch
			{
			#endif
				SendCopyToClipboardMessage();
			}
		}

		/// <inheritdoc/>
		public override bool CanPasteFromClipboard()
		{
			return Clipboard.HasObjectReference() && base.CanPasteFromClipboard();
		}

		/// <inheritdoc />
		protected override void DoPasteFromClipboard()
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(CanPasteFromClipboard());
			#endif

			Value = Clipboard.PasteObjectReference(GetAssignableType());
		}

		/// <summary>
		/// In cases where the Object reference field represents a class member with an interface type,
		/// we need to distinguish between this.Type which tells us what results the Object field accepts internally
		/// and memberInfo.Type which tells us the actual type of the interface.
		/// 
		/// This method will return the type of the interface instead of the internally used UnityEngine.Object
		/// in such scenarios.
		/// </summary>
		/// <returns></returns>
		protected override Type GetAssignableType()
		{
			if(memberInfo != null && memberInfo.Type.IsInterface)
			{
				return memberInfo.Type;
			}
			return Type;
		}

		/// <inheritdoc/>
		protected override string GetPasteFromClipboardMessage()
		{
			return "Pasted reference{0}.";
		}
		
		/// <inheritdoc />
		public override void OnMouseover()
		{
			if(objectFieldRectIncludingPicker.Contains(Cursor.LocalPosition))
			{
				if(objectFieldRectExcludingPicker.Contains(Cursor.LocalPosition))
				{
					DrawGUI.DrawMouseoverEffect(objectFieldRectExcludingPicker, localDrawAreaOffset);
				}
				else
				{
					var pickerRect = objectFieldRectExcludingPicker;
					pickerRect.x += objectFieldRectExcludingPicker.width;
					pickerRect.width = ObjectPickerWidth;
					DrawGUI.DrawMouseoverEffect(pickerRect, localDrawAreaOffset);
				}
			}
			else if(drawEyedropper && eyedropperRect.Contains(Cursor.LocalPosition))
			{
				DrawGUI.DrawMouseoverEffect(eyedropperRect, localDrawAreaOffset);
			}
			else if(InspectorUtility.Preferences.mouseoverEffects.prefixLabel && MouseOverPart == PrefixedControlPart.Prefix)
			{
				DrawGUI.DrawLeftClickAreaMouseoverEffect(PrefixLabelPosition, localDrawAreaOffset);
			}
		}

		/// <inheritdoc />
		public override void DrawFilterHighlight(SearchFilter filter, Color color)
		{
			if(lastPassedFilterTestType == FilterTestType.Value)
			{
				DrawGUI.DrawControlFilteringEffect(DragNDropAreaPosition, color, localDrawAreaOffset);
			}
		}

		/// <inheritdoc />
		public override void OnMouseoverDuringDrag(MouseDownInfo mouseDownInfo, Object[] dragAndDropObjectReferences)
		{
			if(!CanAcceptDrag(mouseDownInfo.MouseDownOverDrawer, dragAndDropObjectReferences))
			{
				DrawGUI.Active.DragAndDropVisualMode = DragAndDropVisualMode.Rejected;
				return;
			}

			DrawGUI.Active.DragAndDropVisualMode = DragAndDropVisualMode.Generic;

			if(Event.current.type == EventType.DragExited || Event.current.type == EventType.DragPerform)
			{
				if(AssignFromUserProvidedRootObjects(dragAndDropObjectReferences, true))
				{
					return;
				}
			}
			
			var objFieldRect = DragNDropAreaPosition;
			if(objFieldRect.Contains(Cursor.LocalPosition))
			{
				DrawGUI.DrawMouseoverEffect(objFieldRect, localDrawAreaOffset);
			}
			else if(InspectorUtility.Preferences.mouseoverEffects.prefixLabel && MouseOverPart == PrefixedControlPart.Prefix)
			{
				DrawGUI.DrawLeftClickAreaMouseoverEffect(PrefixLabelPosition, localDrawAreaOffset);
			}
		}

		private bool AssignFromUserProvidedRootObjects(Object[] userProvidedRootObjects, bool addNull)
		{
			var acceptableObjects = AcceptableDragNDropSubjects(userProvidedRootObjects, addNull);
			return AssignFromAcceptableObjects(acceptableObjects);
		}

		private bool AssignFromAcceptableObjects(Object[] acceptableObjects)
		{
			#if DEV_MODE
			Debug.Log("AssignFromAcceptableObjects(" + StringUtils.TypesToString(acceptableObjects) + ")");
			#endif

			DrawGUI.Use(Event.current);
			Inspector.Manager.MouseDownInfo.Clear();

			int count = acceptableObjects.Length;
			if(count == 1)
			{
				Value =  acceptableObjects[0];
				OnNextLayout(StopUsingEyeDropper);
				return true;
			}

			if(count > 1)
			{
				var menu = new List<PopupMenuItem>(count);
				for(int n = 0; n < count; n++)
				{
					var dropped = acceptableObjects[n];
					if(dropped == null)
					{
						var nullItem = PopupMenuItem.Item(null as Type, "None", "A null reference; one that does not refer to any object.", null);
						nullItem.Preview = null;
						menu.Add(nullItem);
					}
					else
					{
						var typeOfDropped = dropped.GetType();
						menu.Add(PopupMenuItem.Item(dropped, typeOfDropped, StringUtils.ToStringSansNamespace(typeOfDropped), typeOfDropped.Namespace, null, MenuItemValueType.UnityObject));
					}
				}
				string menuLabel = acceptableObjects.Length == 1 ? acceptableObjects[0].name : "Select "+StringUtils.ToStringSansNamespace(type);
				PopupMenuManager.Open(Inspector, menu, controlLastDrawPosition, OnSelectTargetSubTargetMenuItemClicked, OnSelectTargetSubTargetMenuClosed, menuLabel, this);
				// Don't stop using eye dropper yet, so that the inspector lock remains effective.
				// Stop using in OnSelectTargetSubTargetMenuClosed instead.
				return true;
			}

			OnNextLayout(StopUsingEyeDropper);
			return false;
		}

		private void OnSelectTargetSubTargetMenuItemClicked(PopupMenuItem item)
		{
			Value = item.IdentifyingObject as Object;
		}

		private void OnSelectTargetSubTargetMenuClosed()
		{
			StopUsingEyeDropper();
		}

		public override void OnInspectorLostFocusWhileSelected()
        {
            base.OnInspectorLostFocusWhileSelected();

			// When using the eye dropper the value of the drawer is assigned based on selection changes
			// when the user clicks on anything (like the scene window, hierarchy window).
			// If hower the user clicks on another window without triggering a selection change,
			// we still want to stop using the eye dropper.
			if(!usingEyedropper)
			{
				return;
			}
			StaticCoroutine.LaunchDelayed(0.3f, ()=>
			{
				OnNextLayout(DiscardUnappliedChangesAndStopUsingEyeDropper);
			}, true);
        }

		private void DiscardUnappliedChangesAndStopUsingEyeDropper()
        {
			DiscardUnappliedChanges();
			StopUsingEyeDropper();
		}

        private bool CanAcceptDrag(IDrawer mouseDownOverControl, Object[] dragAndDropObjectReferences)
		{
			if(ReadOnly)
			{
				return false;
			}

			if(mouseDownOverControl == this)
			{
				return false;
			}

			if(dragAndDropObjectReferences.Length < 1)
			{
				return false;
			}
			
			var mousePos = Cursor.LocalPosition;
			if(!DragNDropAreaPosition.Contains(mousePos) && !labelLastDrawPosition.Contains(mousePos))
			{
				return false;
			}

			var assignableType = GetAssignableType();

			var dragged = dragAndDropObjectReferences[0];

			var draggedGo = dragged as GameObject;
			if(draggedGo != null)
			{
				if(assignableType == Types.GameObject)
				{
					return true;
				}

				if(assignableType == Types.UnityObject)
				{
					return true;
				}

				if(Types.Component.IsAssignableFrom(assignableType) || assignableType.IsInterface)
				{
					#if UNITY_2019_2_OR_NEWER
					return draggedGo.TryGetComponent(assignableType, out var _);
					#else
					return draggedGo.GetComponent(assignableType) != null;
					#endif
				}

				return GameObjectHasReferenceToAssetOfType(draggedGo, assignableType);
			}

			return assignableType.IsInstanceOfType(dragged);
		}

		[NotNull]
		private Object[] AcceptableDragNDropSubjects([NotNull]Object[] dragAndDropObjectReferences, bool addNull)
		{
			#if DEV_MODE
			Debug.Log("AcceptableDragNDropSubjects(" + StringUtils.ToString(dragAndDropObjectReferences) + ")");
			#endif

			int draggedCount = dragAndDropObjectReferences.Length;
			if(draggedCount == 0)
			{
				return dragAndDropObjectReferences;
			}
			
			var firstDragged = dragAndDropObjectReferences[0];
			var draggedGo = firstDragged as GameObject;
			if(draggedGo != null)
			{
				if(type == Types.GameObject)
				{
					return dragAndDropObjectReferences;
				}

				if(memberInfo != null && memberInfo.Type.IsInterface)
				{
					return draggedGo.GetComponents(memberInfo.Type);
				}

				if(type == Types.UnityObject)
				{
					draggedGo.GetComponents(Types.Component, GetComponents);
					if(addNull)
					{
						GetComponents.Insert(0, null);
					}
					int count = GetComponents.Count;
					var result = ArrayPool<Object>.Create(count);
					result[0] = draggedGo;
					for(int n = addNull ? 1 : 0; n < count; n++)
					{
						result[n] = GetComponents[n];
					}
					GetComponents.Clear();
					return result;
				}

				if(Types.Component.IsAssignableFrom(type))
				{
					return draggedGo.GetComponents(type);
				}

				var asset = draggedGo.FindAssetInChildren(type);
				if(asset != null)
				{
					return ArrayPool<Object>.CreateWithContent(asset);
				}
				return ArrayPool<Object>.ZeroSizeArray;
			}

			if(firstDragged == null)
			{
				return ArrayPool<Object>.ZeroSizeArray;
			}

			if(memberInfo != null && memberInfo.Type.IsInterface)
			{
				if(memberInfo.Type.IsAssignableFrom(firstDragged.GetType()))
				{
					return ArrayExtensions.TempUnityObjectArray(firstDragged);
				}
				return ArrayPool<Object>.ZeroSizeArray;
			}

			if(type.IsInstanceOfType(firstDragged))
			{
				return ArrayExtensions.TempUnityObjectArray(firstDragged);
			}
			return ArrayPool<Object>.ZeroSizeArray;
		}

		[NotNull]
		private bool EyedropperTargetIsAcceptableValue([NotNull]Object test)
		{
			var gameObject = test as GameObject;
			if(gameObject != null)
			{
				if(type == Types.GameObject || type == Types.UnityObject || type == Types.Component)
				{
					return true;
				}
				if(Types.Component.IsAssignableFrom(type))
				{
					#if UNITY_2019_2_OR_NEWER
					return gameObject.TryGetComponent(type, out var _);
					#else
					return gameObject.GetComponent(type) != null;
					#endif
				}
				return GameObjectHasReferenceToAssetOfType(gameObject, type);
			}
			return type.IsInstanceOfType(test);
		}

		private bool GameObjectHasReferenceToAssetOfType(GameObject gameObject, Type type)
        {
			return gameObject.FindAsset(type) != null;
        }

		/// <inheritdoc/>
		public override bool OnRightClick(Event inputEvent)
		{
			#if DEV_MODE
			Debug.Log(GetType().Name+ ".OnRightClick with objectFieldMouseovered="+ objectFieldMouseovered);
			#endif

			if(!ReadOnly)
			{
				if(objectFieldMouseovered)
				{
					HandleOnClickSelection(inputEvent, ReasonSelectionChanged.ThisClicked);

					DrawGUI.Use(inputEvent);
					DisplayTargetSelectMenu();
					return true;
				}
				else if(eyedropperMouseovered || (objectPickerButtonMouseovered && !drawEyedropper))
				{
					HandleOnClickSelection(inputEvent, ReasonSelectionChanged.ThisClicked);

					DrawGUI.Use(inputEvent);
					StartUsingEyeDropper();
					return true;
				}
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

			if(Value != null)
			{
				menu.Add("Peek", PeekNextLayout);
				menu.Add("Ping", Ping);

				if(Value.IsSceneObject())
				{
					menu.AddSeparator();
					menu.Add("Show In Scene View", ShowInSceneView);
				}

				menu.AddSeparator();
			}
			
			menu.Add("Eyedropper Tool", StartUsingEyeDropper);

			if(type != Types.UnityObject && type != Types.Component && type != Types.Texture && type != Types.Texture2D && type != Types.Material && !ReadOnly && (Value == null || extendedMenu))
			{
				menu.AddSeparator();
				menu.Add("Auto-Assign", AutoAssign);

				if(type.IsScriptableObject() && type != Types.ScriptableObject && !type.IsAbstract)
				{
					menu.Add("Create Asset", CreateAsset);
				}
			}

			if(!BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}
		}

		private void AutoAssign()
		{
			#if DEV_MODE
			Debug.Assert(type != Types.UnityObject);
			#endif

			var gameObjects = UnityObjects.GameObjects();
			int count = gameObjects.Length;

			var setValues = ArrayPool<Object>.Create(count);
			if(type == Types.GameObject)
			{
				setValues = gameObjects;
			}
			else if(type.IsComponent() && allowSceneObjects)
			{ 
				for(int n = count - 1; n >= 0; n--)
				{
					var gameObject = gameObjects[n];
					Object setValue = gameObject.GetComponentInChildren(type);
					if(setValue != null)
					{
						setValues[n] = setValue;
						continue;
					}
					setValue = gameObject.GetComponentInParent(type);
					if(setValue != null)
					{
						setValues[n] = setValue;
						continue;
					}

					if(type == Types.Camera)
					{
						Value = Camera.main;
						return;
					}

					if(count == 1)
					{
						AssignFromAcceptableObjects
						(
							#if UNITY_2023_1_OR_NEWER
							Object.FindObjectsByType(type, FindObjectsInactive.Include, FindObjectsSortMode.None)
							#else
							Object.FindObjectsOfType(type)
							#endif
						);
						return;
					}

					setValue =
						#if UNITY_2023_1_OR_NEWER
						Object.FindAnyObjectByType(type, FindObjectsInactive.Include);
						#else
						Object.FindObjectOfType(type);
						#endif

					if(setValue != null)
					{
						setValues[n] = setValue;
						continue;
					}
				}
			}
			else
			{
				var assetGuids = AssetDatabase.FindAssets("t:" + type.Name);
				count = assetGuids.Length;
				#if DEV_MODE
				Debug.Log("Found "+ count+" of "+ type);
				#endif
				var results = new List<Object>(count);
				for(int n = 0; n < count; n++)
				{
					var asset = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(assetGuids[n]), type);
					if(asset != null)
					{
						#if DEV_MODE
						Debug.Log("Found: "+ AssetDatabase.GUIDToAssetPath(assetGuids[n]));
						#endif
						results.Add(asset);
					}
				}
				AssignFromAcceptableObjects(results.ToArray());
				return;
			}

			SetValues(setValues);
		}

		private void CreateAsset()
		{
			string path = EditorUtility.SaveFilePanelInProject("Create "+type.Name+" Asset", StringUtils.ToStringSansNamespace(type) + ".asset", "asset", "Please specify the save path.");
			if(!string.IsNullOrEmpty(path))
			{
				var instance = ScriptableObject.CreateInstance(type);
				AssetDatabase.CreateAsset(instance, path);
				var asset = AssetDatabase.LoadAssetAtPath(path, type);
				Value = asset;
			}
		}

		private void ShowInSceneView()
		{
			var sceneViews = Resources.FindObjectsOfTypeAll<SceneView>();
			if(sceneViews.Length > 0)
			{
				var sceneView = sceneViews[0];
				sceneView.AlignViewToObject(Value.Transform());
			}
		}

		/// <inheritdoc/>
		public override void OnDrag(Event inputEvent)
		{
			base.OnDrag(inputEvent);

			if(Inspector.Manager.MouseDownInfo.CursorMovedAfterMouseDown && !DrawGUI.IsUnityObjectDrag && Event.current.type == EventType.MouseDrag)
			{
				DrawGUI.Active.DragAndDropObjectReferences = Values;
			}
		}

		/// <inheritdoc/>
		protected override void OnControlClicked(Event inputEvent)
		{
			if(objectPickerButtonMouseovered)
			{
				listeningForObjectPickerClosed = true;
				ObjectPicker.OnClosed += OnObjectPickerClosed;
				HandleOnClickSelection(inputEvent, ReasonSelectionChanged.ControlClicked);
				FocusControlField();
				return;
			}

			if(eyedropperMouseovered)
			{
				HandleOnClickSelection(inputEvent, ReasonSelectionChanged.ThisClicked);
				return;
			}

			HandleOnClickSelection(inputEvent, ReasonSelectionChanged.ControlClicked);
			FocusControlField();
			
			var value = Value;
			if(value != null)
			{
				// While this is not necessary for pinging to take place (Unity already does this internally),
				// what it does is handle bringing the Project / Hierarchy window to front if it's a background tab.
				DrawGUI.Active.PingObject(value);
				InspectorUtility.ActiveInspector.ScrollToShow(value);
			}
		}

		/// <inheritdoc/>
		public override void OnMouseUpAfterDownOverControl(Event inputEvent, bool isClick)
		{
			if(eyedropperMouseovered && isClick)
			{
				HandleOnClickSelection(inputEvent, ReasonSelectionChanged.ThisClicked);
				DrawGUI.Use(inputEvent);
				StartUsingEyeDropper();
				return;
			}

			base.OnMouseUpAfterDownOverControl(inputEvent, isClick);
		}

		/// <inheritdoc/>
		protected override Object GetRandomValue()
		{
			var type = Type;
			if(!allowSceneObjects || Random.Range(0, 2) == 0)
			{
				var allAssetGuids = AssetDatabase.FindAssets(StringUtils.Concat("t:", Type.Name));
				int assetCount = allAssetGuids.Length;
				if(assetCount > 0)
				{
					
					for(int n = 0; n < 10; n++)
					{
						var guid = allAssetGuids[Random.Range(0, assetCount)];
						var assetPath = AssetDatabase.GUIDToAssetPath(guid);
						var asset = AssetDatabase.LoadAssetAtPath(assetPath, type);
						if(type.IsAssignableFrom(asset.GetType()))
						{
							return asset;
						}
					}
				}
				return null;
			}
			
			var allSceneObjects =
				#if UNITY_2023_1_OR_NEWER
				Object.FindObjectsByType(Type, FindObjectsInactive.Include, FindObjectsSortMode.None);
				#else
				Object.FindObjectsOfType(Type);
				#endif

			int objCount = allSceneObjects.Length;
			if(objCount > 0)
			{
				for(int n = 0; n < 10; n++)
				{
					var obj = allSceneObjects[Random.Range(0, objCount)];
					if(type.IsAssignableFrom(obj.GetType()))
					{
						return obj;
					}
				}
			}
			return null;
		}

		/// <inheritdoc/>
		public override void Dispose()
		{
			StopUsingEyeDropper();
			ApplyUnappliedChanges();
			hasUnappliedChanges = false;
			valueUnapplied = null;

			eyedropperMouseovered = false;
			objectFieldMouseovered = false;
			objectPickerButtonMouseovered = false;

			#if DEV_MODE
			Debug.Assert(!hasUnappliedChanges, ToString()+".Dispose - hasUnappliedChanges was true!");
			#endif

			if(listeningForObjectPickerClosedWithUnappliedChanges)
			{
				#if DEV_MODE && DEBUG_OBJECT_PICKER
				Debug.Log("listeningForObjectPickerClosed = "+StringUtils.False);
				#endif
				listeningForObjectPickerClosedWithUnappliedChanges = false;
				ObjectPicker.OnClosed -= OnObjectPickerClosedWithUnappliedChanges;
			}

			if(listeningForObjectPickerClosed)
			{
				listeningForObjectPickerClosed = false;
				ObjectPicker.OnClosed -= OnObjectPickerClosed;
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
		protected override Object GetCopyOfValue(Object source)
		{
			return source;
		}

		/// <inheritdoc/>
		protected override void OnLayoutEvent(Rect position)
		{
			base.OnLayoutEvent(position);
			if(MouseOverPart == PrefixedControlPart.Control)
			{
				if(DragNDropAreaPosition.MouseIsOver())
				{
					objectFieldMouseovered = true;
					objectPickerButtonMouseovered = false;
					eyedropperMouseovered = false;
				}
				else if(drawEyedropper && eyedropperRect.MouseIsOver())
				{
					objectFieldMouseovered = false;
					objectPickerButtonMouseovered = false;
					eyedropperMouseovered = true;
				}
				else
				{
					objectFieldMouseovered = false;
					objectPickerButtonMouseovered = true;
					eyedropperMouseovered = false;
				}
			}
			else
			{
				objectFieldMouseovered = false;
				objectPickerButtonMouseovered = false;
				eyedropperMouseovered = false;
			}
		}

		private void SetHasUnappliedChanges(bool setHasUnappliedChanges)
		{
			if(hasUnappliedChanges != setHasUnappliedChanges)
			{
				#if DEV_MODE && DEBUG_UNAPPLIED_CHANGES
				Debug.Log("SetHasUnappliedChanges("+StringUtils.ToColorizedString(setHasUnappliedChanges)+ ") with Value="+ StringUtils.ToColorizedString(Value)+ ", valueUnapplied=" + StringUtils.ToColorizedString(valueUnapplied));
				#endif

				hasUnappliedChanges = setHasUnappliedChanges;
				
				if(hasUnappliedChanges)
				{
					if(ObjectPicker.IsOpen && !listeningForObjectPickerClosedWithUnappliedChanges)
					{
						#if DEV_MODE && DEBUG_OBJECT_PICKER
						Debug.Log("listeningForObjectPickerClosed = "+StringUtils.True);
						#endif
						listeningForObjectPickerClosedWithUnappliedChanges = true;
						ObjectPicker.OnClosed += OnObjectPickerClosedWithUnappliedChanges;
					}
				}
				else
				{
					if(listeningForObjectPickerClosedWithUnappliedChanges)
					{
						#if DEV_MODE && DEBUG_OBJECT_PICKER
						Debug.Log("listeningForObjectPickerClosed = "+StringUtils.False);
						#endif
						listeningForObjectPickerClosedWithUnappliedChanges = false;
						ObjectPicker.OnClosed -= OnObjectPickerClosedWithUnappliedChanges;
					}
				}
			}
		}

		private void OnObjectPickerClosed(Object initialObject, Object selectedObject, bool wasCancelled)
		{
			Select(ReasonSelectionChanged.GainedFocus);

			listeningForObjectPickerClosed = false;
			ObjectPicker.OnClosed -= OnObjectPickerClosed;
		}
		
		private void OnObjectPickerClosedWithUnappliedChanges(Object initialObject, Object selectedObject, bool wasCancelled)
		{
			#if DEV_MODE && DEBUG_OBJECT_PICKER
			Debug.Log(Msg("OnObjectPickerClosedWithUnappliedChanges(initial=", initialObject, ", selected=", selectedObject, ", wasCancelled=", wasCancelled,") - hasUnappliedChanges=" + StringUtils.ToColorizedString(hasUnappliedChanges)+ ", Value="+ StringUtils.ToColorizedString(Value)+ ", valueUnapplied=" + StringUtils.ToColorizedString(valueUnapplied)));
			#endif

			Select(ReasonSelectionChanged.GainedFocus);

			if(listeningForObjectPickerClosedWithUnappliedChanges)
			{
				#if DEV_MODE && DEBUG_OBJECT_PICKER
				Debug.Log("listeningForObjectPickerClosed = "+StringUtils.False);
				#endif
				listeningForObjectPickerClosedWithUnappliedChanges = false;
				ObjectPicker.OnClosed -= OnObjectPickerClosedWithUnappliedChanges;
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

		private void Peek()
		{
			var inspector = InspectorUtility.ActiveInspector;
			if(inspector != null && inspector.InspectorDrawer.CanSplitView)
			{
				var splittableDrawer = (ISplittableInspectorDrawer)inspector.InspectorDrawer;
				splittableDrawer.ShowInSplitView(Value, true);
			}
		}

		private void PeekNextLayout()
		{
			var inspector = InspectorUtility.ActiveInspector;
			if(inspector != null && inspector.InspectorDrawer.CanSplitView)
			{
				var splittableDrawer = (ISplittableInspectorDrawer)inspector.InspectorDrawer;
				inspector.OnNextLayout(() => splittableDrawer.ShowInSplitView(Value, true));
			}
		}

		private void Ping()
		{
			DrawGUI.Ping(Value);
		}
		
		private void DisplayTargetSelectMenu()
		{
			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.BeginSample("ObjectReferenceDrawer.DisplayTargetSelectMenu");
			#endif

			generatedMenuItems.Clear();
			generatedGroupsByLabel.Clear();
			generatedItemsByLabel.Clear();

			var currentValue = Value;
			Transform currentValueTransform;

			GameObject prefabRoot;
			bool isAsset;

			// if field has a current value, generate menu
			// in relation to said target value
			if(currentValue != null)
			{
				currentValueTransform = currentValue.Transform();
				if(currentValueTransform == null)
				{
					prefabRoot = null;
					isAsset = true;
				}
				else if(currentValueTransform.IsPrefab())
				{
					#if !UNITY_EDITOR
					prefabRoot = currentValueTransform.gameObject;
					#elif UNITY_2018_2_OR_NEWER
					prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GetAssetPath(currentValueTransform.gameObject));
					#else
					prefabRoot = PrefabUtility.FindPrefabRoot(currentValueTransform.gameObject);
					#endif
					isAsset = true;
				}
				else
				{
					prefabRoot = null;
					isAsset = false;
				}
			}
			// if field has no current value, generate menu
			// in relation to gameObject which holds the field
			else
			{
				currentValueTransform = null;

				var target = UnityObject;
				var gameObject = target == null ? null : target.GameObject();

				// if field resides in a prefab populate the menu with all objects in prefab
				if(gameObject != null && gameObject.IsPrefab())
				{
					#if UNITY_2018_2_OR_NEWER
					prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GetAssetPath(gameObject));
					#else
					prefabRoot = PrefabUtility.FindPrefabRoot(gameObject);
					#endif
					isAsset = true;
				}
				else
				{
					// if field has no value and the field does not reside inside a prefab,
					// set root to null, as we'll populate the menu with all targets in assets / hierarchy
					prefabRoot = null;
					isAsset = !allowSceneObjects;
				}
			}
			
			#if DEV_MODE
			Debug.Log(StringUtils.ToColorizedString("DisplayTargetSelectMenu with isAsset=", isAsset, ", rootGameObject=", prefabRoot, ", type=", type));
			#endif

			string selectItemAtPath = null;

			var menuLabel = GUIContentPool.Empty();

			// if this member resides in a prefab target, then we get all
			// GameObjects / Components in children of the prefab.
			if(prefabRoot != null)
			{
				// add all GameObjects and Components in children of the prefab
				selectItemAtPath = PopupMenuUtility.BuildPopupMenuItemForObjectsInChildren(generatedMenuItems, generatedGroupsByLabel, generatedItemsByLabel, prefabRoot, type, currentValue);
				menuLabel.text = prefabRoot.name;
			}
			else
			{
				if(!isAsset)
				{
					menuLabel.text = "Hierarchy";

					if(type == Types.GameObject)
					{
						for(int s = 0, scount = SceneManager.sceneCount;  s < scount; s++)
						{
							var scene = SceneManager.GetSceneAt(s);
							var gos = scene.GetAllGameObjects();
							for(int g = 0, count = gos.Length; g < count; g++)
							{
								PopupMenuUtility.BuildPopupMenuItemForGameObject(generatedMenuItems, generatedGroupsByLabel, generatedItemsByLabel, gos[g]);
							}
						}
					}
					else if(type == Types.Transform || type == Types.RectTransform)
					{
						for(int s = 0, scount = SceneManager.sceneCount;  s < scount; s++)
						{
							var scene = SceneManager.GetSceneAt(s);
							var gos = scene.GetAllGameObjects();
							for(int g = 0, count = gos.Length; g < count; g++)
							{
								PopupMenuUtility.BuildPopupMenuItemForTransform(generatedMenuItems, generatedGroupsByLabel, generatedItemsByLabel, gos[g].transform);
							}
						}
					}
					else if(type == Types.UnityObject)
					{
						for(int s = 0, scount = SceneManager.sceneCount;  s < scount; s++)
						{
							var scene = SceneManager.GetSceneAt(s);
							var gos = scene.GetAllGameObjects();
							for(int g = 0, count = gos.Length; g < count; g++)
							{
								PopupMenuUtility.BuildPopupMenuItemForGameObjectAndItsComponents(generatedMenuItems, generatedGroupsByLabel, generatedItemsByLabel, gos[g]);
							}
						}
					}
					else if(type.IsInterface)
					{
						type.FindObjectsImplementingInterface(GetComponents, FindObjects);
						GetComponents.Sort(SortComponentsByHierarchyOrder.Instance);
						for(int n = 0, count = GetComponents.Count; n < count; n++)
						{
							var comp = GetComponents[n];
							string hierarchyPath = string.Concat(comp.transform.GetHierarchyPath(), "/", StringUtils.ToStringSansNamespace(comp.GetType()));
							PopupMenuUtility.BuildPopupMenuItemWithLabel(generatedMenuItems, generatedGroupsByLabel, generatedItemsByLabel, comp, hierarchyPath, MenuItemValueType.UnityObject);
						}
						for(int n = 0, count = FindObjects.Count; n < count; n++)
						{
							var obj = FindObjects[n];
							string assetPath = string.Concat(obj.HierarchyOrAssetPath(), "/", StringUtils.ToStringSansNamespace(obj.GetType()));
							PopupMenuUtility.BuildPopupMenuItemWithLabel(generatedMenuItems, generatedGroupsByLabel, generatedItemsByLabel, obj, assetPath, MenuItemValueType.UnityObject);
						}
					}
					else if(type.IsComponent())
					{
						var comps =
							#if UNITY_2023_1_OR_NEWER
							Object.FindObjectsByType(type, FindObjectsInactive.Include, FindObjectsSortMode.None) as Component[];
							#else
							Object.FindObjectsOfType(type) as Component[];
							#endif

						Array.Sort(comps, SortComponentsByHierarchyOrder.Instance);
						for(int n = 0, count = comps.Length; n < count; n++)
						{
							var comp = comps[n];
							string hierarchyPath = string.Concat(comp.transform.GetHierarchyPath(), "/", StringUtils.ToStringSansNamespace(comp.GetType()));
							PopupMenuUtility.BuildPopupMenuItemWithLabel(generatedMenuItems, generatedGroupsByLabel, generatedItemsByLabel, comp, hierarchyPath, MenuItemValueType.UnityObject);
						}
					}
					else //ScriptableObject, Asset etc.
					{
						var objs =
							#if UNITY_2023_1_OR_NEWER
							Object.FindObjectsByType(type, FindObjectsInactive.Include, FindObjectsSortMode.None);
							#else
							Object.FindObjectsOfType(type);
							#endif

						for(int n = 0, count = objs.Length; n < count; n++)
						{
							var obj = objs[n];
							PopupMenuUtility.BuildPopupMenuItemWithLabel(generatedMenuItems, generatedGroupsByLabel, generatedItemsByLabel, obj, obj.name, MenuItemValueType.UnityObject);
						}
					}

					if(currentValueTransform != null)
					{
						selectItemAtPath = currentValueTransform.GetHierarchyPath();
						if(!type.IsGameObject())
						{
							selectItemAtPath = string.Concat(selectItemAtPath, "/", currentValue.GetType().Name);
						}
					}
				}
				//an asset
				else
				{
					var currentOrBaseType = currentValue != null ? currentValue.GetType() : type;

					// If type is GameObject or Component, just list all prefabs without Components.
					// The user can then use the right click menu again to further select which Component to use.
					// Alternatively could also immediately pop a new menu open to let the user define the
					// Object inside said Prefab in OnPopupMenuClosed.
					if(currentOrBaseType == Types.GameObject || currentOrBaseType.IsComponent())
					{
						menuLabel.text = "Prefabs";
						var gameObjects = AssetDatabase.FindAssets("t:GameObject");
						int count = gameObjects.Length;
						if(count > 0)
						{
							for(int n = 0; n < count; n++)
							{
								var assetGuid = gameObjects[n];
								string assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);

								// remove "Assets/" from the beginning, and ".prefab" from the end
								string assetPathShortened = assetPath.Substring(7, assetPath.Length - 14);
								PopupMenuUtility.BuildPopupMenuItemWithLabel(generatedMenuItems, generatedGroupsByLabel, generatedItemsByLabel, assetPath, type, assetPathShortened, "", MenuItemValueType.AssetPath);
							}
						}
					}
					else
					{
						//menuLabel.text = "Assets";
						menuLabel.text = type.Name + " Assets";

						var assets = AssetDatabase.FindAssets(string.Concat("t:", type.Name));
						
						var alreadyAddedToMenu = new HashSet<string>();

						int count = assets.Length;
						if(type.IsSealed)
						{
							for(int n = 0; n < count; n++)
							{
								var assetGuid = assets[n];
								string assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);

								#if DEV_MODE && PI_ASSERTATIONS
								Debug.Assert(assetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase), assetPath);
								#endif

								// remove "Assets/" from the beginning
								string assetPathShortened = assetPath.Substring(7);

								if(alreadyAddedToMenu.Add(assetPathShortened))
								{
									#if DEV_MODE
									Debug.LogWarning("Skipping "+assetPath+" because was already added to menu...");
									#endif
									continue;
								}

								PopupMenuUtility.BuildPopupMenuItemWithLabel(generatedMenuItems, generatedGroupsByLabel, generatedItemsByLabel, assetPath, type, assetPathShortened, "", MenuItemValueType.AssetPath);
							}
						}
						else
						{
							for(int n = 0; n < count; n++)
							{
								var assetGuid = assets[n];
								string assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);

								#if DEV_MODE && PI_ASSERTATIONS
								Debug.Assert(assetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase), assetPath);
								#endif

								// remove "Assets/" from the beginning
								string assetPathShortened = assetPath.Substring(7);

								if(alreadyAddedToMenu.Add(assetPathShortened))
								{
									#if DEV_MODE
									Debug.LogWarning("Skipping "+assetPath+" because was already added to menu...");
									#endif
									continue;
								}

								var loaded = AssetDatabase.LoadAssetAtPath(assetPath, type);
								var assetType = loaded.GetType();
								EditorUtility.UnloadUnusedAssetsImmediate();
								PopupMenuUtility.BuildPopupMenuItemWithLabel(generatedMenuItems, generatedGroupsByLabel, generatedItemsByLabel, assetPath, assetType, assetPathShortened, "", MenuItemValueType.AssetPath);
							}
						}

						if(currentValue != null)
						{
							var assetPath = AssetDatabase.GetAssetPath(currentValue);
							selectItemAtPath = assetPath.Substring(7);
						}
					}
				}
			}
			
			if(generatedMenuItems.Count > 0)
			{
				var unrollPosition = DragNDropAreaPosition;
				unrollPosition.height = PopupMenu.TotalMaxHeightWithNavigationBar;
				PopupMenuManager.Open(Inspector, generatedMenuItems, generatedGroupsByLabel, generatedItemsByLabel, DragNDropAreaPosition, OnPopupMenuItemClicked, OnPopupMenuClosed, menuLabel, this);

				if(selectItemAtPath != null)
				{
					#if DEV_MODE
					Debug.Log("selectItemAtPath: "+ selectItemAtPath);
					#endif
					PopupMenuManager.SelectItem(selectItemAtPath);
				}
			}

			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.EndSample();
			#endif
		}

		private void OnPopupMenuItemClicked(PopupMenuItem item)
		{
			Value = item.IdentifyingObject as Object;
		}

		private void OnPopupMenuClosed()
		{
			Select(ReasonSelectionChanged.Initialization);
		}
		
		/// <summary>
		/// If value selected via object picker is still unapplied, apply it now
		/// </summary>
		private void ApplyUnappliedChanges()
		{
			if(hasUnappliedChanges)
			{
				#if DEV_MODE && DEBUG_UNAPPLIED_CHANGES
				Debug.Log(StringUtils.ToColorizedString(ToString(), ".ApplyUnappliedChanges with valueUnapplied=", valueUnapplied, ", Value=", Value, "  - Event=", StringUtils.ToString(Event.current) + ", KeyCode=" + (Event.current == null ? KeyCode.None : Event.current.keyCode) + ", button=" + (Event.current == null ? -1 : Event.current.button)));
				#endif

				Value = valueUnapplied;
			}
			#if DEV_MODE && DEBUG_UNAPPLIED_CHANGES
			else { Debug.Log(StringUtils.ToColorizedString(ToString(), ".ApplyUnappliedChanges with valueUnapplied=", valueUnapplied, ", Value=", Value, "  - Event=", StringUtils.ToString(Event.current) + ", KeyCode=" + (Event.current == null ? KeyCode.None : Event.current.keyCode) + ", button=" + (Event.current == null ? -1 : Event.current.button))); }
			#endif
		}

		protected override void GetDrawPositions(Rect position)
		{
			base.GetDrawPositions(position);

			if(drawEyedropper)
			{
				objectFieldRectIncludingPicker = controlLastDrawPosition;
				
				const float eyedropperWidth = 24f;
				eyedropperRect = objectFieldRectIncludingPicker;
				eyedropperRect.x += objectFieldRectIncludingPicker.width - eyedropperWidth;
				eyedropperRect.width = eyedropperWidth;

				objectFieldRectIncludingPicker.width -= 18f;
			}
			else
			{
				objectFieldRectIncludingPicker = controlLastDrawPosition;
				eyedropperRect.width = 0f;
			}

			objectFieldRectExcludingPicker = objectFieldRectIncludingPicker;
			objectFieldRectExcludingPicker.width -= ObjectPickerWidth;
		}

		/// <summary>
		/// If value selected via object picker is still unapplied, discard it now
		/// and keep the previously selected value
		/// </summary>
		private void DiscardUnappliedChanges()
		{
			if(hasUnappliedChanges)
			{
				#if DEV_MODE && DEBUG_UNAPPLIED_CHANGES
				Debug.Log("DiscardUnappliedChanges - Event=" + StringUtils.ToString(Event.current) + ", KeyCode=" + Event.current.keyCode + ", button=" + Event.current.button);
				#endif

				valueUnapplied = Value;
				SetHasUnappliedChanges(false);
			}
		}
	}
}