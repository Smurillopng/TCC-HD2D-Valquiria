//#define DEBUG_FOCUS_FIELD
//#define DEBUG_EDIT_FIELD
//#define DEBUG_CAN_DRAW_IN_SINGLE_ROW
//#define DEBUG_CAN_DRAW_IN_SINGLE_ROW_STEPS
//#define DEBUG_CAN_DRAW_MULTIPLE_CONTROLS_OF_TYPE
//#define DEBUG_NULL_FIELD_INFO

using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Sisus
{
	public static class DrawerUtility
	{
		private static bool beginDataValidatedControlsWasReadonly;
		private static bool guiWasEnabledBeforeBeginDataValidatedControls;
		private static bool shouldEndProperty;
		public static IDrawer currentField;
		internal static readonly Stack<Color> GUIColorHistory = new Stack<Color>();

		public static void TryGetDecoratorDrawer([NotNull]IDrawerProvider drawerProvider, [NotNull]LinkedMemberInfo memberInfo, ref List<IDrawer> addResultToEndOfArray, IParentDrawer parent)
		{
			var propertyAttributes = memberInfo.GetAttributes<PropertyAttribute>();
			for(int n = 0, count = propertyAttributes.Length; n < count; n++)
			{
				var propertyAttribute = propertyAttributes[n];
				IDecoratorDrawerDrawer add;
				if(drawerProvider.TryGetForDecoratorDrawer(propertyAttribute, propertyAttribute.GetType(), parent, memberInfo, out add))
				{
					addResultToEndOfArray.Add(add);
				}
			}
		}
		
		public static float GetOptimalPrefixLabelWidth(int indentLevel, GUIContent label, bool boldStyle = false)
		{
			float textSize = boldStyle ? DrawGUI.prefixLabelModified.CalcSize(label).x : DrawGUI.prefixLabel.CalcSize(label).x;
			float result = DrawGUI.LeftPadding + DrawGUI.IndentWidth * indentLevel + textSize + DrawGUI.MiddlePadding;

			#if DEV_MODE && DEBUG_GET_OPTIMAL_PREFIX_WIDTH
			Debug.Log("GetOptimalPrefixLabelWidth("+label.text+"): "+result);
			#endif

			return result;
		}

		private static void BeginDataValidatedControls(IFieldDrawer subject)
		{
			if(subject.ReadOnly)
			{
				beginDataValidatedControlsWasReadonly = true;
				guiWasEnabledBeforeBeginDataValidatedControls = GUI.enabled;
				GUI.enabled = false;
			}
			else
			{
				beginDataValidatedControlsWasReadonly = false;
			}

			var guiColorWas = GUI.color;
			GUIColorHistory.Push(guiColorWas);

			currentField = subject;

			#if UNITY_EDITOR
			var linkedFieldInfo = subject.MemberInfo;
			shouldEndProperty = false;
			if(linkedFieldInfo != null)
			{
				var serializedProperty = linkedFieldInfo.SerializedProperty;
				if(serializedProperty != null)
				{
					#if DEV_MODE && PI_ASSERTATIONS
					string s = "BeginProperty called for "+linkedFieldInfo;
					Debug.Assert(!linkedFieldInfo.IsStatic, s+" but it is static.");
					Debug.Assert(linkedFieldInfo.SerializedProperty.serializedObject != null, s+" but serializedObject was null.");
					Debug.Assert(linkedFieldInfo.SerializedProperty.serializedObject.targetObject != null, s+" but serializedObject.targetObject was null.");
					#endif

					EditorGUI.BeginProperty(subject.ControlPosition, GUIContent.none, serializedProperty);
					shouldEndProperty = true;
				}
			}
			#endif
			
			if(subject.IsAnimated)
			{
				//make animated fields update their values faster when animation is being played
				if(AnimationWindowUtility.Playing())
				{
					subject.UpdateCachedValuesFromFieldsRecursively();
				}
			}
			else if(!subject.DataIsValid) //UPDATE: using same red tint for controls animated in the Animation window too, at least for now
			{
				if(guiColorWas.r == 1f && guiColorWas.g == 1f && guiColorWas.b == 1f && guiColorWas.a == 1f)
				{
					GUI.color = Color.red;
				}
				else
				{
					var setGUIColor = Color.red;
					setGUIColor.r = (setGUIColor.r + guiColorWas.r) * 0.5f;
					setGUIColor.g = (setGUIColor.g + guiColorWas.g) * 0.5f;
					setGUIColor.b = (setGUIColor.b + guiColorWas.b) * 0.5f;
					setGUIColor.a = guiColorWas.a;
					GUI.color = setGUIColor;
				}
			}
		}

		private static void EndDataValidatedControls()
		{
			GUI.color = GUIColorHistory.Pop();

			currentField = null;

			#if UNITY_EDITOR
			if(shouldEndProperty)
			{
				EditorGUI.EndProperty();
			}
			#endif

			if(beginDataValidatedControlsWasReadonly)
			{
				GUI.enabled = guiWasEnabledBeforeBeginDataValidatedControls;
			}
		}
		
		public static void BeginInputField([NotNull]IFieldDrawer subject, int controlID, ref bool editField, ref int focusField, bool mixedContent)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(subject != null);
			#endif

			BeginDataValidatedControls(subject);
			HandleFieldFocusAndStartEditing(controlID, ref editField, ref focusField);
			DrawGUI.ShowMixedValue = mixedContent;
		}

		public static void EndInputField()
		{
			EndDataValidatedControls();
			DrawGUI.ShowMixedValue = false;
		}
		
		public static void HandleFieldFocusAndStartEditing(int controlID, ref bool editField, ref int focusField)
		{
			if(focusField > 0)
			{
				#if DEV_MODE
				Debug.Assert(!InspectorUtility.ActiveManager.HasMultiSelectedControls);
				#endif
				
				// Trigger a Layout event.
				GUI.changed = true;

				if(editField)
				{
					string id = StringUtils.ToString(controlID);
					GUI.SetNextControlName(id);

					// Only call FocusControl and EditingTextField at the lastest possible moment
					// to prevent issue where keyboard focus would visibly jump between multiple controls.
					if(Event.current.type == EventType.Layout && focusField == 1)
					{
						DrawGUI.FocusControl(id);
						DrawGUI.EditingTextField = true;
					}
				}
				else
				{
					KeyboardControlUtility.KeyboardControl = 0;
				}
				
				if(Event.current.type == EventType.Layout)
				{
					focusField--;

					if(focusField == 0)
					{
						editField = false;
					}

					#if DEV_MODE && (DEBUG_EDIT_FIELD || DEBUG_FOCUS_FIELD)
					Debug.Log(StringUtils.ToColorizedString("focusing field " + controlID + " with editField=", editField, ", repeats=", focusField, ", Event=", StringUtils.ToString(Event.current)));
					#endif
				}
			}
		}
		
		public static void BeginFocusableField(IFieldDrawer subject, int controlID, ref int focusField, bool mixedContent)
		{
			BeginDataValidatedControls(subject);
			HandleFieldFocus(controlID, ref focusField);
			DrawGUI.ShowMixedValue = mixedContent;
		}

		public static void EndFocusableField()
		{
			EndDataValidatedControls();
			DrawGUI.ShowMixedValue = false;
		}

		public static void HandleFieldFocus(int controlID, ref int focusField)
		{
			if(focusField > 0)
			{
				HandleFieldFocus(StringUtils.ToString(controlID), ref focusField);
			}
		}
		public static void HandleFieldFocus(string id, ref int focusField)
		{
			if(focusField <= 0)
			{
				return;
			}

			if(ObjectPicker.IsOpen)
            {
				return;
            }

			// Trigger a Layout event.
			GUI.changed = true;

			GUI.SetNextControlName(id);

			if(Event.current.type == EventType.Layout)
			{
				// Only call FocusControl at the lastest possible moment to prevent issue where keyboard focus would visibly jump between multiple controls.
				// UPDATE: This caused field focusing to get applied after a clear delay, so now calling it immediately instead.
				//if(focusField == 1)
				{
					DrawGUI.FocusControl(id);
				}

				focusField--;

				#if DEV_MODE && DEBUG_FOCUS_FIELD
				Debug.Log(StringUtils.ToColorizedString("focusing field " + id + ", repeats=", focusField, ", Event=", StringUtils.ToString(Event.current)));
				#endif
			}
		}
		
		public static IDrawer FindFieldDrawerInMembers(LinkedMemberInfo memberInfo, IDrawer target, bool onlyVisibleMembers = true, int depth = 2)
		{
			var parent = target as IParentDrawer;
			if(parent != null)
			{
				var members = onlyVisibleMembers ? parent.VisibleMembers : parent.Members;
				int lastIndex = members.Length - 1;

				//check members
				for(int n = lastIndex; n >= 0; n--)
				{
					var member = members[n];
					if(member.MemberInfo == memberInfo)
					{
						return member;
					}
				}

				if(depth > 0)
				{
					depth--;
					//go check members of members and so forth
					for(int n = lastIndex; n >= 0; n--)
					{
						var found = FindFieldDrawerInMembers(memberInfo, members[n], onlyVisibleMembers, depth);
						if(found != null)
						{
							return found;
						}
					}
				}
			}

			return null;
		}
		
		public static Type GetType<TFallback>([CanBeNull]LinkedMemberInfo memberInfo, [CanBeNull]TFallback value)
		{
			if(memberInfo != null)
			{
				var type = memberInfo.Type;

				if((!type.IsAbstract && type != Types.SystemObject) || typeof(TFallback).IsAssignableFrom(type))
				{
					return type;
				}

				#if DEV_MODE
				Debug.LogWarning("GetType - Won't use LinkedMemberInfo.Type " + StringUtils.ToString(type) + " because it is general. value="+StringUtils.ToString(value)+", TFallback="+StringUtils.ToString(typeof(TFallback))+ ".");
				#endif
			}

			if(value != null)
			{
				#if DEV_MODE && DEBUG_NULL_FIELD_INFO
				Debug.LogWarning("DrawerUtility.GetType memberInfo was null, value of type "+StringUtils.TypeToString(value)+": "+StringUtils.ToString(value));
				#endif

				var type = value.GetType();

				if(typeof(TFallback).IsAssignableFrom(type))
				{
					return type;
				}

				#if DEV_MODE
				Debug.LogWarning("GetType - value.GetType() " + StringUtils.ToString(type)+ " was not valid. TFallback=" + StringUtils.ToString(typeof(TFallback))+ ".");
				#endif
			}
			
			#if DEV_MODE
			Debug.LogWarning("GetType return TFallback ("+typeof(TFallback).Name+") because could not get valid type from value nor fieldInfo.");
			#endif
			
			return typeof(TFallback);
		}
		
		public static Type GetType<TFallback>([CanBeNull]LinkedMemberInfo memberInfo, [CanBeNull]TFallback value, Func<Type, bool> validate)
		{
			if(memberInfo != null)
			{
				var type = memberInfo.Type;
				if(validate(type))
				{
					return type;
				}
			}

			if(value != null)
			{
				#if DEV_MODE && DEBUG_NULL_FIELD_INFO
				Debug.LogWarning("DrawerUtility.GetType memberInfo was null, value of type "+StringUtils.TypeToString(value)+": "+StringUtils.ToString(value));
				#endif

				var type = value.GetType();
				if(validate(type))
				{
					return type;
				}
			}

			#if DEV_MODE
			//if(typeof(TFallback) == typeof(Object) || typeof(TFallback).IsAbstract || typeof(TFallback).IsInterface || typeof(TFallback) == typeof(Enum) || typeof(TFallback).IsGenericType || typeof(TFallback).IsGenericTypeDefinition|| typeof(TFallback) == typeof(Component) || typeof(TFallback) == typeof(Behaviour) || typeof(TFallback) == typeof(MonoBehaviour))
			{
				Debug.LogWarning("GetType return TFallback ("+typeof(TFallback).Name+ ") because both value and fieldInfo were null or neither passed validation");
			}
			#endif
			
			return typeof(TFallback);
		}

		public static MemberInfo GetInspectorViewable(this Type type, bool includeHidden, int nth)
		{
			int memberCount = 1;
			
			var settings = InspectorUtility.Preferences;
			var showNonSerializedFields = settings.showFields;
			var propertyVisibility = settings.showProperties;
			var methodVisibility = settings.showMethods;

			var order = InspectorUtility.Preferences.MemberDisplayOrder;
			for(int t = 0; t < 3; t++)
			{
				switch(order[t])
				{
					case Member.Field:
						var fields = type.GetFields(ParentDrawerUtility.BindingFlagsInstance);
						for(int n = 0, count = fields.Length; n < count; n++)
						{
							var field = fields[n];
							if(field.IsInspectorViewable(includeHidden, showNonSerializedFields))
							{
								if(memberCount == nth)
								{
									return field;
								}
								memberCount++;
							}
						}
						break;
					case Member.Property:
						var properties = type.GetProperties(ParentDrawerUtility.BindingFlagsInstance);
						for(int n = 0, count = properties.Length; n < count; n++)
						{
							var property = properties[n];
							if(property.IsInspectorViewable(propertyVisibility, includeHidden))
							{
								if(memberCount == nth)
								{
									return property;
								}
								memberCount++;
							}
						}
						break;
					case Member.Method:
						var methods = type.GetMethods(ParentDrawerUtility.BindingFlagsInstance);
						for(int n = 0, count = methods.Length; n < count; n++)
						{
							var method = methods[n];
							if(method.IsInspectorViewable(methodVisibility, includeHidden))
							{
								if(memberCount == nth)
								{
									return method;
								}
								memberCount++;
							}
						}
						break;
				}
			}
			return null;
		}

		public static bool CanDrawInSingleRow(Type classMemberType, bool includeHidden)
		{
			if(CanDrawMultipleControlsOfTypeInSingleRow(classMemberType))
			{
				#if DEV_MODE && DEBUG_CAN_DRAW_IN_SINGLE_ROW
				Debug.Log(StringUtils.ToColorizedString("CanDrawInSingleRow(", classMemberType.Name, ", ", includeHidden, "): ", true, "(because CanDrawMultipleControlsOfTypeInSingleRow)"));
				#endif
				return true;
			}
			
			var settings = InspectorUtility.Preferences;
			var methodVisibility = settings.showMethods;
			var showNonSerializedFields = settings.showFields;
			var propertyVisibility = settings.showProperties;
			int oneBeforeMax = ParentDrawerUtility.MaxMembersDrawnInSingleDraw - 1;
			var bindingFlags = ParentDrawerUtility.BindingFlagsInstanceAndStatic;

			int memberCount = 0;
			for(var type = classMemberType; type != null; type = type.BaseType)
			{
				//We stop once we hit specific base types (even in debug mode this is desired, to avoid the number of results getting out of hand)
				if(type == Types.MonoBehaviour || type == Types.ScriptableObject || type == Types.Component || type == Types.UnityObject || type == Types.SystemObject)
				{
					if(type != classMemberType)
					{
						break;
					}
				}

				var fields = type.GetFields(bindingFlags);
				for(int n = 0, count = fields.Length; n < count; n++)
				{
					var field = fields[n];
					if(field.IsInspectorViewable(includeHidden, showNonSerializedFields))
					{
						if(field.Name.Length > 1)
						{
							return false;
						}

						if(!CanDrawMultipleControlsOfTypeInSingleRow(field.FieldType) || memberCount >= oneBeforeMax)
						{
							return false;
						}
						
						memberCount++;

						#if DEV_MODE && DEBUG_CAN_DRAW_IN_SINGLE_ROW_STEPS
						Debug.Log("Counted field \""+field.Name+"\" of type "+field.FieldType.Name+". Count now "+memberCount);
						#endif
					}
					#if DEV_MODE && DEBUG_CAN_DRAW_IN_SINGLE_ROW_STEPS
					else { Debug.Log("Ignoring "+(field.IsPublic ? "public" : "non-public")+" field \""+field.Name+"\" of type "+field.FieldType.Name+" because IsInspectorViewable was "+StringUtils.False+"...");}
					#endif
				}
				
				var properties = type.GetProperties(bindingFlags);
				for(int n = 0, count = properties.Length; n < count; n++)
				{
					var property = properties[n];
					if(property.IsInspectorViewable(propertyVisibility, includeHidden))
					{
						if(property.Name.Length > 1)
						{
							return false;
						}

						if(!property.ShowInspectorViewableAsNormalField())
						{
							return false;
						}

						if(!CanDrawMultipleControlsOfTypeInSingleRow(property.PropertyType) || memberCount >= oneBeforeMax)
						{
							return false;
						}
						memberCount++;
					}
				}

				var methods = type.GetMethods(bindingFlags);
				for(int n = 0, count = methods.Length; n < count; n++)
				{
					if(methods[n].IsInspectorViewable(methodVisibility, includeHidden))
					{
						return false;
					}
				}

				#if DEV_MODE && DEBUG_CAN_DRAW_IN_SINGLE_ROW
				if(memberCount <= ParentDrawerUtility.MaxMembersDrawnInSingleDraw) { Debug.Log(StringUtils.ToColorizedString("CanDrawInSingleRow(", type.Name, ", ", includeHidden, "): ", true, " (because memberCount ", memberCount, " <= ", ParentDrawerUtility.MaxMembersDrawnInSingleDraw, ")\nfields="+fields.Length+", properties="+properties.Length)); }
				#endif
			}

			return memberCount <= ParentDrawerUtility.MaxMembersDrawnInSingleDraw;
		}

		public static bool CanDrawMultipleControlsOfTypeInSingleRow(Type type)
		{
			#if DEV_MODE && DEBUG_CAN_DRAW_MULTIPLE_CONTROLS_OF_TYPE
			Debug.Log(StringUtils.ToColorizedString("CanDrawMultipleControlsOfTypeInSingleRow(", type, "): ", type.IsPrimitive || type == Types.String || type.IsEnum || type.IsUnityObject() || type == Types.Color));
			#endif
			return type.IsPrimitive || type == Types.String || type.IsEnum || type.IsUnityObject() || type == Types.Color;
		}

		public static bool CanDrawInSingleRow(IDrawer subject)
		{
			var parent = subject as IParentDrawer;
			return parent == null || parent.DrawInSingleRow;
		}

		public static void SendResetMessage(string name)
		{
			InspectorUtility.ActiveInspector.Message(name.Length > 0 ? StringUtils.Concat("\"", name, "\" value was reset") : "Value was reset");
		}
	}
}