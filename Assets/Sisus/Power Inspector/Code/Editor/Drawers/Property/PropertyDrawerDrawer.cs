#define PI_ENABLE_UI_SUPPORT

#define DEBUG_ENABLED
//#define ENABLE_INDENT_FIX_HACK
#define ALWAYS_USE_WIDEMODE
//#define DRAW_LABEL_RESIZE_CONTROL
#define SAFE_MODE

using System;
using UnityEngine;
using System.Reflection;
using JetBrains.Annotations;
using UnityEditor;
using Object = UnityEngine.Object;

#if UNITY_2019_1_OR_NEWER // UI Toolkit doesn't exist in older versions
using UnityEngine.UIElements;
#endif

namespace Sisus
{
	/// <summary> Drawer for representing any field using a custom PropertyDrawer.</summary>
	[Serializable]
	public class PropertyDrawerDrawer : PrefixControlComboDrawer<object>, IPropertyDrawerDrawer, ITextFieldDrawer
	{
		private UnityEditor.PropertyDrawer drawerInstance;
		private bool editField;
		
		#if UNITY_2019_1_OR_NEWER
		private VisualElement element;
		#endif

		/// <inheritdoc/>
		public bool RequiresPropertyDrawerType
		{
			get
			{
				return true;
			}
		}

		private bool IsTextField
		{
			get
			{
				var type = Type;
				return type == Types.String || type.IsNumeric();
			}
		}

		/// <inheritdoc cref="IDrawer.PrefixResizingEnabledOverControl" />
		public override bool PrefixResizingEnabledOverControl
		{
			get
			{
				return false;
			}
		}

		private SerializedProperty SerializedProperty
		{
			get
			{
				return memberInfo == null ? null : memberInfo.SerializedProperty;
			}
		}

		/// <inheritdoc cref="IDrawer.Height" />
		public override float Height
		{
			get
			{
				#if UNITY_2019_1_OR_NEWER
				// needed?
				if(element != null && !float.IsNaN(element.resolvedStyle.height))
				{
					return element.resolvedStyle.height + 2f;
				}
				#endif

				#if ALWAYS_USE_WIDEMODE
				bool wideModeWas = EditorGUIUtility.wideMode;
				EditorGUIUtility.wideMode = true;
				#endif

				float height = SerializedProperty == null ? 0f : drawerInstance.GetPropertyHeight(SerializedProperty, label);
				if(height <= 0f)
				{
					height = DrawGUI.SingleLineHeight;
				}

				#if ALWAYS_USE_WIDEMODE
				EditorGUIUtility.wideMode = wideModeWas;
				#endif

				#if UNITY_2019_3_OR_NEWER
				return height + 5f;
				#else
				return height + 2f;
				#endif
			}
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="value"> The starting cached value of the drawer. </param>
		/// <param name="drawerType"> LinkedMemberInfo of the method that the drawer represent. </param>
		/// <param name="memberInfo"> LinkedMemberInfo for the field, property or parameter that the drawer represents. Can be null. </param>
		/// <param name="parent"> The parent drawer of this member. Can be null. </param>
		/// <param name="label"> The label. </param>
		/// <param name="readOnly"> True if control should be read only. </param>
		/// <returns> The newly-created instance. </returns>
		[CanBeNull]
		public static PropertyDrawerDrawer Create(object value, object attribute, Type drawerType, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool readOnly)
		{
			if(memberInfo == null)
			{
				#if DEV_MODE
				Debug.LogError("PropertyDrawerDrawer.Create(drawerType="+drawerType.Name+", parent="+StringUtils.ToString(parent)+", label="+(label == null ? "null" : label.text)+"), returning null because fieldInfo was null");
				#endif
				return null;
			}

			if(memberInfo.SerializedProperty == null)
			{
				#if DEV_MODE
				Debug.LogError("PropertyDrawerDrawer.Create(drawerType=" + drawerType.Name + ", parent=" + StringUtils.ToString(parent) + ", label=" + (label == null ? "null" : label.text) + "), returning null because fieldInfo.SerializedProperty was null");
				#endif
				return null;
			}

			UnityEditor.PropertyDrawer drawerInstance;
			try
			{
				drawerInstance = drawerType.CreateInstance() as UnityEditor.PropertyDrawer;
			}
			catch(Exception e)
			{
				Debug.LogError("Failed to create instance of PropertyDrawer "+StringUtils.ToString(drawerType)+" "+e);
				return null;
			}
			
			PropertyDrawerDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new PropertyDrawerDrawer();
			}
			result.Setup(value, attribute, drawerInstance, drawerType, memberInfo, parent, label, readOnly);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc cref="IFieldDrawer.SetupInterface" />
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			SetupInterface(setMemberInfo.GetAttribute<PropertyAttribute>(), setValue, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc/>
		public void SetupInterface(object attribute, object setValue, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(setValue != null || setMemberInfo != null);
			#endif

			Type propertyDrawerType;
			if(attribute != null)
			{
				if(!CustomEditorUtility.TryGetPropertyDrawerType(attribute.GetType(), out propertyDrawerType))
				{
					#if DEV_MODE
					Debug.LogError("CustomEditorUtility.TryGetPropertyDrawerType returned null for attribute "+StringUtils.TypeToString(attribute));
					#endif
				}
				#if DEV_MODE && DEBUG_ENABLED
				else { Debug.Log("Drawer type for attribute "+StringUtils.TypeToString(attribute)+": "+StringUtils.ToString(propertyDrawerType)); }
				#endif
			}
			else
			{
				if(setMemberInfo == null || !CustomEditorUtility.TryGetPropertyDrawerType(setMemberInfo.Type, out propertyDrawerType))
				{
					if(setValue == null || !CustomEditorUtility.TryGetPropertyDrawerType(setValue.GetType(), out propertyDrawerType))
					{
						propertyDrawerType = null;
						#if DEV_MODE
						if(setValue == null)
						{
							Debug.LogError("PropertyDrawerGUI.SetupInterface called with attribute="+StringUtils.Null+", setValue="+StringUtils.Null+" and setMemberInfo="+StringUtils.Null);
						}
						else
						{
							Debug.LogError("PropertyDrawerGUI.SetupInterface - TryGetPropertyDrawerType returned "+StringUtils.Null+" for type "+StringUtils.ToString(DrawerUtility.GetType(setMemberInfo, setValue))+ " with attribute "+StringUtils.Null);
						}
						#endif
					}
				}
			}
			
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(propertyDrawerType != null);
			#endif

			Setup(setValue, attribute, null, propertyDrawerType, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc />
		protected override void Setup(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			throw new NotSupportedException("Please use the other Setup method.");
		}

		private void Setup(object setValue, [CanBeNull]object attribute, [CanBeNull]UnityEditor.PropertyDrawer setDrawerInstance, [NotNull]Type drawerType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(drawerType != null);
			#endif

			if(setDrawerInstance == null)
			{
				setDrawerInstance = (UnityEditor.PropertyDrawer)drawerType.CreateInstance();
			}
			drawerInstance = setDrawerInstance;

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(drawerInstance != null, "Failed to create PropertyDrawer instance of type "+StringUtils.ToString(drawerType)+" for field of type "+StringUtils.ToString(DrawerUtility.GetType(setMemberInfo, setValue))+" and attribute "+StringUtils.TypeToString(setMemberInfo == null ? null : setMemberInfo.GetAttribute<PropertyAttribute>()));
			#endif

			memberInfo = setMemberInfo;

			var attField = drawerType.GetField("m_Attribute", BindingFlags.Instance | BindingFlags.NonPublic);

			if(setMemberInfo != null)
			{
				#if UNITY_2019_1_OR_NEWER && (DEV_MODE || PI_ENABLE_UI_SUPPORT)
				element = drawerInstance.CreatePropertyGUI(setMemberInfo.SerializedProperty);
				if(element != null)
				{
					InspectorUtility.ActiveInspectorDrawer.AddElement(element, this);
				}
				#endif

				if(attribute != null)
				{
					#if DEV_MODE && DEBUG_ENABLED
					Debug.Log("Setting field "+drawerType.Name+".m_Attribute value to "+ StringUtils.TypeToString(attribute));
					#endif

					attField.SetValue(drawerInstance, attribute);
				}
			
				attField = drawerType.GetField("m_FieldInfo", BindingFlags.Instance | BindingFlags.NonPublic);
				attField.SetValue(drawerInstance, setMemberInfo.FieldInfo);
			}
			else
			{
				Debug.LogError("PropertyDrawerDrawer(\""+(setLabel != null ? setLabel.text : "")+"\").Setup("+drawerType.Name+") - fieldInfo was null (parent="+StringUtils.ToString(setParent)+")");
			}
			
			base.Setup(setValue, DrawerUtility.GetType(setMemberInfo, setValue), setMemberInfo, setParent, setLabel, setReadOnly);

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(SerializedProperty != null, StringUtils.ToColorizedString(GetDevInfo()));
			#endif
		}

		/// <inheritdoc cref="IDrawer.UpdateCachedValuesFromFieldsRecursively" />
		public override void UpdateCachedValuesFromFieldsRecursively()
		{
			base.UpdateCachedValuesFromFieldsRecursively();
			var serializedProperty = SerializedProperty;
			if(serializedProperty != null)
			{
				if(serializedProperty.serializedObject != null)
				{
					serializedProperty.serializedObject.Update();
				}
				#if DEV_MODE && DEBUG_NULL_PROPERTY
				else { Debug.LogError(ToString()+".UpdateCachedValuesFromFieldsRecursively() - serializedObject was null (parent=" + StringUtils.ToString(parent) + ")"); }
				#endif
			}
			#if DEV_MODE && DEBUG_NULL_PROPERTY
			else { Debug.LogError(ToString() + ".UpdateCachedValuesFromFieldsRecursively() - Property was null (fieldInfo="+(memberInfo == null ? "null" : "NotNull")+", parent=" + StringUtils.ToString(parent) + ")"); }
			#endif
		}

		/// <inheritdoc cref="IDrawer.Draw" />
		public override bool Draw(Rect position)
		{
			#if SAFE_MODE || DEV_MODE
			var targets = memberInfo.UnityObjects;
			int targetCount = targets.Length;
			if(targetCount == 0)
			{
				#if DEV_MODE
				Debug.LogWarning(ToString() + ".Draw() - target count was zero, rebuilding drawer");
				#endif

				InspectorUtility.ActiveInspector.RebuildDrawersIfTargetsChanged();
				return false;
			}
			if(targets.ContainsNullObjects())
			{
				#if DEV_MODE
				Debug.LogWarning(ToString() + ".Draw() - target was null, rebuilding drawer");
				#endif

				InspectorUtility.ActiveInspector.RebuildDrawersIfTargetsChanged();
				return false;
			}
			#endif

			var positionWithoutMargins = position;

			bool dirty = false;

			if(Event.current.type == EventType.Layout)
			{
				OnLayoutEvent(position);
			}
			
			#if UNITY_2019_1_OR_NEWER
			if(element != null)
			{
				return false;
			}
			#endif

			#if !DRAW_LABEL_RESIZE_CONTROL
			DrawGUI.Active.ColorRect(position, DrawGUI.Active.InspectorBackgroundColor);
			#endif

			position.height -= 2f;
			position.y += 1f;

			position.width -= DrawGUI.RightPadding;
			
			float labelWidthWas = EditorGUIUtility.labelWidth;
			float fieldWidthWas = EditorGUIUtility.fieldWidth;

			float leftPadding = DrawGUI.LeftPadding;
			int labelRightPadding = (int)(DrawGUI.MiddlePadding + DrawGUI.MiddlePadding);

			position.x += leftPadding;
			position.width -= leftPadding;
			
			//always use wide mode for properties because it works better with the prefix width control
			#if ALWAYS_USE_WIDEMODE
			bool wideModeWas = EditorGUIUtility.wideMode;
			EditorGUIUtility.wideMode = true;
			#endif
			
			EditorStyles.label.padding.right = labelRightPadding;
			
			GUILayout.BeginArea(positionWithoutMargins);
			{
				position.y -= positionWithoutMargins.y;
				position.x -= positionWithoutMargins.x;

				EditorGUI.BeginChangeCheck();
				{
					DrawerUtility.BeginInputField(this, controlId, ref editField, ref focusField, memberInfo == null ? false : memberInfo.MixedContent);
					{
						var serializedProperty = SerializedProperty;
						if(serializedProperty == null)
						{
							// NOTE: This can happen during Remove Component for some reason
							#if DEV_MODE
							Debug.LogError(ToString() + ".Draw - SerializedProperty was null (parent=" + StringUtils.ToString(parent) + ") so can't use EditorGUI.PropertyField");
							#endif
							
							EditorGUI.PrefixLabel(position, label);
						}
						else
						{
							bool editingTextFieldWas;
							EventType eventType;
							KeyCode keyCode;
							CustomEditorUtility.BeginPropertyDrawer(out editingTextFieldWas, out eventType, out keyCode);
							{
								var leftMarginWas = EditorStyles.foldout.margin.left;
								if(!EditorGUIDrawer.EnableFoldoutFix)
								{
									// fix needed or foldouts will be drawn at incorrect positions
									EditorStyles.foldout.margin.left = -12;
								}

								try
								{
									EditorGUI.PropertyField(position, serializedProperty, label, serializedProperty.isExpanded);
								}
								catch(Exception e)
								{
									if(ExitGUIUtility.ShouldRethrowException(e))
									{
										EditorStyles.foldout.margin.left = leftMarginWas;

										throw;
									}
									#if DEV_MODE
									Debug.LogWarning(ToString()+" "+e);
									#endif
								}

								EditorStyles.foldout.margin.left = leftMarginWas;
							}
							CustomEditorUtility.EndPropertyDrawer(editingTextFieldWas, eventType, keyCode);

							bool editingTextFieldIs = EditorGUIUtility.editingTextField;
							if(editingTextFieldIs != editingTextFieldWas)
							{
								DrawGUI.EditingTextField = editingTextFieldIs;
							}
						}
					}
					DrawerUtility.EndInputField();
				}
				if(EditorGUI.EndChangeCheck())
				{
					GUI.changed = true;
					SerializedProperty.serializedObject.ApplyModifiedProperties();
					dirty = true;
				}
			}
			GUILayout.EndArea();

			#if ALWAYS_USE_WIDEMODE
			EditorGUIUtility.wideMode = wideModeWas;
			#endif

			EditorStyles.label.padding.right = 2;
			EditorGUIUtility.labelWidth = labelWidthWas;
			EditorGUIUtility.fieldWidth = fieldWidthWas;

			return dirty;
		}
		
		/// <summary>
		/// fill out lastDrawPosition for Draw
		/// </summary>
		protected override void GetDrawPositions(Rect position)
		{
			lastDrawPosition = position;
			lastDrawPosition.height = Height;

			localDrawAreaOffset = DrawGUI.GetLocalDrawAreaOffset();
		}

		/// <inheritdoc/>
		protected override void ApplyValueToField()
		{
			if(!Type.IsArray)
			{
				object value = Value != null ? Value : SerializedProperty.GetType().DefaultValue();
				memberInfo.SetValue(value);
				if(OnValueChanged != null)
				{
					OnValueChanged(this, value);
				}
				OnValidate();
			}
		}

		/// <inheritdoc cref="IDrawer.OnMouseoverDuringDrag" />
		public override void OnMouseoverDuringDrag(MouseDownInfo mouseDownInfo, Object[] dragAndDropObjectReferences)
		{
			// Don't set DragAndDropVisualMode to Rejected; we don't know if the drawer accepts them or not
		}

		/// <inheritdoc/>
		protected override void OnSelectedInternal(ReasonSelectionChanged reason, IDrawer previous, bool isMultiSelection)
		{
			//TO DO: do something similar to CustomEditorBaseDrawer to handle selecting of first or last control

			switch(reason)
			{
				case ReasonSelectionChanged.PrefixClicked:
					return;
				case ReasonSelectionChanged.SelectControlUp:
				case ReasonSelectionChanged.SelectPrevControl:
					if(IsTextField)
					{
						StartEditingField();
					}
					else
					{
						FocusControlField();
					}
					return;
				case ReasonSelectionChanged.SelectControlLeft:
				case ReasonSelectionChanged.SelectControlDown:
				case ReasonSelectionChanged.SelectControlRight:
				case ReasonSelectionChanged.SelectNextControl:
				case ReasonSelectionChanged.KeyPressOther:
					if(IsTextField)
					{
						StartEditingField();
					}
					else
					{
						FocusControlField();
					}
					return;
			}
		}

		/// <inheritdoc/>
		public void StartEditingField()
		{
			if(!editField)
			{
				#if DEV_MODE
				Debug.Log(GetType().Name + ".StartEditingField()");
				#endif

				//when field is given focus, also set text field editing mode true
				editField = true;
				DrawGUI.EditingTextField = true;
				FocusControlField();
			}
		}

		/// <inheritdoc/>
		public void StopEditingField()
		{
			editField = false;
			focusField = 0;
			KeyboardControlUtility.KeyboardControl = 0;
			DrawGUI.EditingTextField = false;

			//this needs to be delayed or Unity can internally start editing the text again immediately e.g. when return is pressed
			InspectorUtility.ActiveInspector.OnNextLayout(StopEditingFieldStep);
		}

		private void StopEditingFieldStep()
		{
			DrawGUI.EditingTextField = false;
		}

		/// <inheritdoc cref="IDrawer.OnKeyboardInputGiven" />
		public override bool OnKeyboardInputGiven(Event inputEvent, KeyConfigs keys)
		{
			switch(inputEvent.keyCode)
			{
				case KeyCode.F2:
				{
					GUI.changed = true;
					DrawGUI.Use(inputEvent);
					StartEditingField();
					return true;
				}
				case KeyCode.Escape:
				{
					GUI.changed = true;
					KeyboardControlUtility.KeyboardControl = 0;
					DrawGUI.Use(inputEvent);
					StopEditingField();
					return true;
				}
			}
			return base.OnKeyboardInputGiven(inputEvent, keys);
		}

		/// <inheritdoc cref="IDrawer.GetRandomValue" />
		protected override object GetRandomValue()
		{
			var type = Type;
			if(type.IsPrimitive)
			{
				if(type == Types.Int)
				{
					return RandomUtils.Int();
				}
				else if(type == Types.Float)
				{
					return RandomUtils.Float(float.MinValue, float.MaxValue);
				}
				else if(type == Types.Char)
				{
					return RandomUtils.Char();
				}
				else if(type == Types.Bool)
				{
					return RandomUtils.Bool();
				}
				else if(type == Types.Double)
				{
					return RandomUtils.Double(double.MinValue, double.MaxValue);
				}
			}
			else if(type == Types.String)
			{
				return RandomUtils.String(0, 100);
			}

			#if DEV_MODE
			Debug.LogError(GetType().Name+" Randomize was not supported for type "+StringUtils.ToString(type));
			#endif

			return type.DefaultValue();
		}

		/// <inheritdoc/>
		protected override void OnCachedValueChanged(bool applyToField, bool updateMembers)
		{
			base.OnCachedValueChanged(applyToField, updateMembers);

			var serializedProperty = SerializedProperty;
			if(serializedProperty != null && serializedProperty.serializedObject != null)
			{
				serializedProperty.serializedObject.Update();
			}
		}

		#if UNITY_2019_1_OR_NEWER
		/// <inheritdoc/>
		public override void Dispose()
		{
			if(element != null)
			{
				InspectorUtility.ActiveInspectorDrawer.RemoveElement(element, this);
				element = null;
			}
			base.Dispose();
		}
		#endif
	}
}