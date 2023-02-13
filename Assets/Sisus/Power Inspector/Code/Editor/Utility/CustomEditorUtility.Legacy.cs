#define SUPPORT_EDITORS_FOR_INTERFACES // the default inspector doesn't support this but we can
//#define USE_IL_FOR_GET_AND_SET

//#define DEBUG_PROPERTY_DRAWERS
//#define DEBUG_SET_EDITING_TEXT_FIELD

#if !UNITY_2023_1_OR_NEWER
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using Sisus.Compatibility;
using System.Collections;
using UnityEngine.Rendering;

#if CSHARP_7_3_OR_NEWER && USE_IL_FOR_GET_AND_SET
using Sisus.Vexe.FastReflection;
#endif

namespace Sisus
{
	[InitializeOnLoad]
	public static class CustomEditorUtility
	{
		public static event Action EditorsUpdated;

		internal static Dictionary<Type, Type> CustomEditorsByType;
		internal static Dictionary<Type, Type> CustomMultiEditorsByType;
		internal static Dictionary<Type, Type> PropertyDrawersByType;
		internal static Dictionary<Type, Type> DecoratorDrawersByType;

		public static bool IsReady;

		static CustomEditorUtility()
        {
			Setup();

			if(!PluginCompatibilityUtility.OtherToolsHaveInjectedTheirEditors())
			{
				EditorApplication.delayCall += SetupWhenReady;
			}
		}

		private static void SetupWhenReady()
		{
			if(!PluginCompatibilityUtility.OtherToolsHaveInjectedTheirEditors())
			{
				EditorApplication.delayCall += SetupWhenReady;
				return;
			}

			Setup();
		}

		private static void Setup()
		{
			#if DEV_MODE
			var timer = new ExecutionTimeLogger();
			timer.Start("CustomEditorUtility.Setup()");
			#endif

			IsReady = false;

			var internalEditorsField = GetEditorStaticInternalField("UnityEditor.CustomEditorAttributes", "kSCustomEditors");
			if(internalEditorsField is null)
			{
				#if DEV_MODE
				Debug.LogError("Field UnityEditor.CustomEditorAttributes.kSCustomEditors not found.");
				#endif

				CustomEditorsByType = new Dictionary<Type, Type>();
			}
			else
			{
				BuildCustomEditorsByTypeDictionary(out CustomEditorsByType, internalEditorsField);
			}

			var internalMultiEditorsField = GetEditorStaticInternalField("UnityEditor.CustomEditorAttributes", "kSCustomMultiEditors");
			if(internalMultiEditorsField is null)
			{
				#if DEV_MODE
				Debug.LogError("Field UnityEditor.CustomEditorAttributes.kSCustomMultiEditors not found.");
				#endif

				CustomMultiEditorsByType = new Dictionary<Type, Type>();
			}
			else
			{
				BuildCustomEditorsByTypeDictionary(out CustomMultiEditorsByType, internalMultiEditorsField);
			}

			BuildPropertyDrawersByTypeDictionary(out PropertyDrawersByType);
			BuildDecoratorDrawersByTypeDictionary(out DecoratorDrawersByType);

			IsReady = true;

			#if DEV_MODE
			timer.FinishAndLogResults();
			#endif

			EditorsUpdated?.Invoke();
		}

		private static FieldInfo GetEditorStaticInternalField(string fullTypeName, string fieldName)
		{
			return GetInternalEditorType(fullTypeName)?.GetField(fieldName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		}

		internal static Type GetInternalEditorType(string fullTypeName)
		{
			#if DEV_MODE
			Debug.Assert(fullTypeName.IndexOf(".") != -1, fullTypeName);
			#endif

			var type = typeof(Editor).Assembly.GetType(fullTypeName);

			#if DEV_MODE
			Debug.Assert(type != null, $"Type {fullTypeName} was not found in assembly {typeof(Editor).Assembly.GetName().Name}.");
			#endif

			return type;
		}

		private static void BuildCustomEditorsByTypeDictionary(out Dictionary<Type, Type> results, FieldInfo internalEditorsField)
		{
			var internalDictionary = internalEditorsField.GetValue(null) as IDictionary;
			results = new Dictionary<Type, Type>(internalDictionary.Count);
			var customEditorsByType = new Dictionary<Type, List<MonoEditorType>>(internalDictionary.Count);

			foreach(DictionaryEntry typeAndList in internalDictionary)
			{
				if(!(typeAndList.Key is Type type))
				{
					continue;
				}

				var internalList = typeAndList.Value as IList;
				var editorTypeList = new List<MonoEditorType>(internalList.Count);

				foreach(var item in internalList)
				{
					editorTypeList.Add(new MonoEditorType(item));
				}

				customEditorsByType.Add(type, editorTypeList);
			}

			List<MonoEditorType> list = new List<MonoEditorType>();
			foreach(Type type in customEditorsByType.Keys)
			{
				if(TryGetCustomEditor(type, out Type editorType, list))
				{
					results[type] = editorType;
				}
			}

			bool TryGetCustomEditor(Type type, out Type result, List<MonoEditorType> possibleResults)
			{
				if(type is null)
				{
					result = null;
					return false;
				}

				for(int pass = 0; pass < 2; ++pass)
				{
					for(Type inspectedType = type; inspectedType != null; inspectedType = inspectedType.BaseType)
					{
						if(!customEditorsByType.TryGetValue(inspectedType, out List<MonoEditorType> editorOptions))
						{
							if(!inspectedType.IsGenericType)
							{
								continue;
							}

							inspectedType = inspectedType.GetGenericTypeDefinition();
							if(!customEditorsByType.TryGetValue(inspectedType, out editorOptions))
							{
								continue;
							}
						}

						possibleResults.Clear();
						foreach (var editor in editorOptions)
						{
							if(IsPossibleResult(editor, inspectedType, type != inspectedType, pass == 1))
							{
								possibleResults.Add(editor);
							}
						}

						if(GraphicsSettings.currentRenderPipeline != null)
						{
							Type currentRenderPipelineType = GraphicsSettings.currentRenderPipeline.GetType();
							result = null;

							foreach(var editor in possibleResults)
							{
								// First priority: Render pipeline-specific custom editor targeting type exactly
								if(editor.renderPipelineType == currentRenderPipelineType)
								{
									result = editor.inspectorType;
									break;
								}

								// else use an inherited RP editor as fallback, but continue searching for a direct option.
								if(editor.renderPipelineType != null && editor.renderPipelineType.IsAssignableFrom(currentRenderPipelineType))
								{
									result = editor.inspectorType;
								}
							}

							if(result != null)
							{
								possibleResults.Clear();
								return true;
							}
						}

						foreach(var editor in possibleResults)
						{
							if(editor.renderPipelineType == null)
							{
								possibleResults.Clear();
								result = editor.inspectorType;
								return result != null;
							}
						}

						possibleResults.Clear();
					}
				}

				result = null;
				return false;

				bool IsPossibleResult(MonoEditorType editor, Type parentClass, bool isChildClass, bool isFallback)
				{
					if(isChildClass && !editor.editorForChildClasses)
					{
						return false;
					}

					if(isFallback != editor.isFallback)
					{
						return false;
					}

					if(parentClass == editor.inspectedType)
					{
						return true;
					}

					return parentClass.IsGenericType && parentClass.GetGenericTypeDefinition() == editor.inspectedType;
				}
			}
		}

		public static void BuildPropertyDrawersByTypeDictionary(out Dictionary<Type, Type> propertyDrawersByType)
		{
			propertyDrawersByType = new Dictionary<Type, Type>();

			var typeField = typeof(CustomPropertyDrawer).GetField("m_Type", BindingFlags.NonPublic | BindingFlags.Instance);
			var useForChildrenField = typeof(CustomPropertyDrawer).GetField("m_UseForChildren", BindingFlags.NonPublic | BindingFlags.Instance);
			var propertyDrawerTypes = TypeExtensions.GetTypesWithAttributeNotThreadSafe<CustomPropertyDrawer>().Where((t) => t.IsSubclassOf(typeof(UnityEditor.PropertyDrawer)) && !t.IsAbstract);

			#if DEV_MODE && PI_ASSERTATIONS
			if(!propertyDrawerTypes.Contains(typeof(UnityEditorInternal.UnityEventDrawer))) { Debug.LogError("UnityEventDrawer not found among "+ propertyDrawerTypes.Count()+" PropertyDrawer types:\n"+StringUtils.ToString(propertyDrawerTypes, "\n")); };
			#endif

			#if CSHARP_7_3_OR_NEWER && USE_IL_FOR_GET_AND_SET
			var getTargetType = typeField.DelegateForGet<object, Type>();
			#endif

			try
			{
				#if CSHARP_7_3_OR_NEWER && USE_IL_FOR_GET_AND_SET
				GetDrawersByInspectedTypeFromAttributes<CustomPropertyDrawer>(propertyDrawerTypes, getTargetType, ref propertyDrawersByType, true);
				GetDrawersByInheritedInspectedTypesFromAttributes<CustomPropertyDrawer>(propertyDrawerTypes, getTargetType, useForChildrenField, ref propertyDrawersByType, false, true);
				#else
				GetDrawersByInspectedTypeFromAttributes<CustomPropertyDrawer>(propertyDrawerTypes, typeField, ref propertyDrawersByType, true);
				GetDrawersByInheritedInspectedTypesFromAttributes<CustomPropertyDrawer>(propertyDrawerTypes, typeField, useForChildrenField, ref propertyDrawersByType, false, true);
				#endif
			}
			#if DEV_MODE
			catch(Exception e)
			{
				Debug.LogWarning(e);
			#else
			catch(Exception)
			{
			#endif
				// Do slow but safe loading with try-catch used for every type separately to figure out which type caused the exception.
				// This should also inform the user about the Type causing issues as well as instructions on how to deal with it.
				propertyDrawerTypes = TypeExtensions.GetExtendingTypesThreadSafeExceptionSafeSlow(typeof(UnityEditor.PropertyDrawer), false, true, true);

				propertyDrawersByType.Clear();

				#if CSHARP_7_3_OR_NEWER && USE_IL_FOR_GET_AND_SET
				GetDrawersByInspectedTypeFromAttributes<CustomPropertyDrawer>(propertyDrawerTypes, getTargetType, ref propertyDrawersByType, true);
				GetDrawersByInheritedInspectedTypesFromAttributes<CustomPropertyDrawer>(propertyDrawerTypes, getTargetType, useForChildrenField, ref propertyDrawersByType, false, true);
				#else
				GetDrawersByInspectedTypeFromAttributes<CustomPropertyDrawer>(propertyDrawerTypes, typeField, ref propertyDrawersByType, true);
				GetDrawersByInheritedInspectedTypesFromAttributes<CustomPropertyDrawer>(propertyDrawerTypes, typeField, useForChildrenField, ref propertyDrawersByType, false, true);
				#endif
			}

			#if DEV_MODE && PI_ASSERTATIONS
			if(!propertyDrawersByType.Values.Contains(typeof(UnityEditorInternal.UnityEventDrawer))) { Debug.LogError("UnityEventDrawer not found among "+ propertyDrawersByType.Count+" PropertyDrawers. "); };
			#endif

			#if DEV_MODE && DEBUG_PROPERTY_DRAWERS
			Debug.Log("propertyDrawersByType:\r\n"+StringUtils.ToString(propertyDrawersByType, "\r\n"));
			#endif
		}

		public static void BuildDecoratorDrawersByTypeDictionary(out Dictionary<Type, Type> decoratorDrawersByType)
		{
			decoratorDrawersByType = new Dictionary<Type, Type>();

			var typeField = typeof(CustomPropertyDrawer).GetField("m_Type", BindingFlags.NonPublic | BindingFlags.Instance);

			// UPDATE: Apparently this field is not really respected for decorator drawers
			//var useForChildrenField = propertyDrawerType.GetField("m_UseForChildren", BindingFlags.NonPublic | BindingFlags.Instance);

			var decoratorDrawerTypes = TypeExtensions.GetTypesWithAttributeNotThreadSafe<CustomPropertyDrawer>().Where((t) => t.IsSubclassOf(typeof(DecoratorDrawer)) && !t.IsAbstract);

			#if CSHARP_7_3_OR_NEWER && USE_IL_FOR_GET_AND_SET
			var getTargetType = typeField.DelegateForGet<object, Type>();
			#endif

			try
			{
				#if CSHARP_7_3_OR_NEWER && USE_IL_FOR_GET_AND_SET
				GetDrawersByInspectedTypeFromAttributes<CustomPropertyDrawer>(decoratorDrawerTypes, getTargetType, ref decoratorDrawersByType, false);
				GetDrawersByInheritedInspectedTypesFromAttributes<CustomPropertyDrawer>(decoratorDrawerTypes, getTargetType, null, ref decoratorDrawersByType, false, false);
				#else
				GetDrawersByInspectedTypeFromAttributes<CustomPropertyDrawer>(decoratorDrawerTypes, typeField, ref decoratorDrawersByType, false);
				GetDrawersByInheritedInspectedTypesFromAttributes<CustomPropertyDrawer>(decoratorDrawerTypes, typeField, null, ref decoratorDrawersByType, false, false);
				#endif
			}
			#if DEV_MODE
			catch(Exception e)
			{
				Debug.LogWarning(e);
			#else
			catch(Exception)
			{
			#endif
				// Do slow but safe loading with try-catch used for every type separately to figure out which type caused the exception.
				// This should also inform the user about the Type causing issues as well as instructions on how to deal with it.
				decoratorDrawerTypes = TypeExtensions.GetExtendingTypesThreadSafeExceptionSafeSlow(typeof(DecoratorDrawer), false, true, true);

				decoratorDrawersByType.Clear();

				#if CSHARP_7_3_OR_NEWER && USE_IL_FOR_GET_AND_SET
				GetDrawersByInspectedTypeFromAttributes<CustomPropertyDrawer>(decoratorDrawerTypes, getTargetType, ref decoratorDrawersByType, false);
				GetDrawersByInheritedInspectedTypesFromAttributes<CustomPropertyDrawer>(decoratorDrawerTypes, getTargetType, null, ref decoratorDrawersByType, false, false);
				#else
				GetDrawersByInspectedTypeFromAttributes<CustomPropertyDrawer>(decoratorDrawerTypes, typeField, ref decoratorDrawersByType, false);
				GetDrawersByInheritedInspectedTypesFromAttributes<CustomPropertyDrawer>(decoratorDrawerTypes, typeField, null, ref decoratorDrawersByType, false, false);
				#endif
			}
		}
		
		/// <summary>
		/// Attempts to get PropertyDrawer Type for given class or from attributes on the field
		/// </summary>
		/// <param name="classMemberType"> Type of the class for which we are trying to find the PropertyDrawer. </param>
		/// <param name="memberInfo"> LinkedMemberInfo of the property for which we are trying to find the PropertyDrawer. </param>
		/// <param name="propertyAttribute"> [out] PropertyAttribute found on the property. </param>
		/// <param name="drawerType"> [out] Type of the PropertyDrawer for the PropertyAttribute. </param>
		/// <returns>
		/// True if target has a PropertyDrawer, false if not.
		/// </returns>
		public static bool TryGetPropertyDrawerType([NotNull]Type classMemberType, [NotNull]LinkedMemberInfo memberInfo, out PropertyAttribute propertyAttribute, out Type drawerType)
		{
			var attributes = memberInfo.GetAttributes(Types.PropertyAttribute);
			for(int n = attributes.Length - 1; n >= 0; n--)
			{
				var attribute = attributes[n];
				if(TryGetPropertyDrawerType(attribute.GetType(), out drawerType))
				{
					propertyAttribute = attribute as PropertyAttribute;
					return true;
				}
			}
			propertyAttribute = null;
			return TryGetPropertyDrawerType(classMemberType, out drawerType);
		}

		/// <summary>
		/// Attempts to get PropertyDrawer Type for given class.
		/// </summary>
		/// <param name="classMemberOrAttributeType"> Type of the class for which we are trying to find the PropertyDrawer. </param>
		/// <param name="propertyDrawerType"> [out] Type of the PropertyDrawer. </param>
		/// <returns> True if target has a PropertyDrawer, false if not. </returns>
		public static bool TryGetPropertyDrawerType([NotNull]Type classMemberOrAttributeType, out Type propertyDrawerType)
		{
			if(PropertyDrawersByType.TryGetValue(classMemberOrAttributeType, out propertyDrawerType))
			{
				return true;
			}

			if(classMemberOrAttributeType.IsGenericType && !classMemberOrAttributeType.IsGenericTypeDefinition)
			{
				return PropertyDrawersByType.TryGetValue(classMemberOrAttributeType.GetGenericTypeDefinition(), out propertyDrawerType);
			}

			return false;
		}

		/// <summary>
		/// Does class member attribute of given type have a PropertyDrawer?
		/// </summary>
		/// <param name="classMemberOrAttributeType"> Type of class member or attribute. </param>
		/// <returns> True if class has a PropertyDrawer, false if not. </returns>
		public static bool HasPropertyDrawer([NotNull]Type classMemberOrAttributeType)
		{
			if(PropertyDrawersByType.ContainsKey(classMemberOrAttributeType))
			{
				return true;
			}

			if(classMemberOrAttributeType.IsGenericType && !classMemberOrAttributeType.IsGenericTypeDefinition)
			{
				return PropertyDrawersByType.ContainsKey(classMemberOrAttributeType.GetGenericTypeDefinition());
			}

			return false;
		}

		public static bool TryGetDecoratorDrawerTypes([NotNull]LinkedMemberInfo memberInfo, out object[] decoratorAttributes, out Type[] drawerTypes)
		{
			//TO DO: Add support for PropertyAttribute.order
			
			drawerTypes = null;
			decoratorAttributes = null;
			
			var attributes = memberInfo.GetAttributes(Types.PropertyAttribute);
			for(int n = attributes.Length - 1; n >= 0; n--)
			{
				var attribute = attributes[n];
				Type drawerType;
				if(TryGetDecoratorDrawerType(attribute.GetType(), out drawerType))
				{
					if(drawerTypes == null)
					{
						decoratorAttributes = new[] { attribute };
						drawerTypes = new[]{drawerType};
					}
					else
					{
						decoratorAttributes = decoratorAttributes.Add(attribute);
						drawerTypes = drawerTypes.Add(drawerType);
					}
				}
			}
			
			return drawerTypes != null;
		}

		public static bool AttributeHasDecoratorDrawer(Type propertyAttributeType)
		{
			if(DecoratorDrawersByType.ContainsKey(propertyAttributeType))
			{
				return true;
			}

			if(propertyAttributeType.IsGenericType && !propertyAttributeType.IsGenericTypeDefinition)
			{
				return DecoratorDrawersByType.ContainsKey(propertyAttributeType.GetGenericTypeDefinition());
			}

			return false;
		}

		public static bool TryGetDecoratorDrawerType(Type propertyAttributeType, out Type decoratorDrawerType)
		{
			if(DecoratorDrawersByType.TryGetValue(propertyAttributeType, out decoratorDrawerType))
			{
				return true;
			}

			if(propertyAttributeType.IsGenericType && !propertyAttributeType.IsGenericTypeDefinition)
			{
				return DecoratorDrawersByType.TryGetValue(propertyAttributeType.GetGenericTypeDefinition(), out decoratorDrawerType);
			}

			return false;
		}
		
		public static bool TryGetCustomEditorType([NotNull]Type targetType, out Type editorType)
		{
			if(CustomEditorsByType.TryGetValue(targetType, out editorType))
			{
				return true;
			}

			if(targetType.IsGenericType && !targetType.IsGenericTypeDefinition)
			{
				return CustomEditorsByType.TryGetValue(targetType.GetGenericTypeDefinition(), out editorType);
			}

			return false;
		}

		/// <summary>
		/// Given an array of PropertyDrawer, DecoratorDrawers or Editors, gets their inspected types and adds them to drawersByInspectedType.
		/// </summary>
		/// <typeparam name="TAttribute"> Type of the attribute. </typeparam>
		/// <param name="drawerOrEditorTypes"> List of PropertyDrawer, DecoratorDrawer or Editor types. </param>
		/// <param name="targetTypeField"> FieldInfo for getting the inspected type. </param>
		/// <param name="drawersByInspectedType">
		/// [in,out] dictionary where drawer types will be added with their inspected types as the keys. </param>
		#if CSHARP_7_3_OR_NEWER && USE_IL_FOR_GET_AND_SET
		private static void GetDrawersByInspectedTypeFromAttributes<TAttribute>([NotNull]IEnumerable<Type> drawerOrEditorTypes, [NotNull]MemberGetter<object, Type> getTargetType, [NotNull]ref Dictionary<Type,Type> drawersByInspectedType, bool canBeAbstract) where TAttribute : Attribute
		#else
		private static void GetDrawersByInspectedTypeFromAttributes<TAttribute>([NotNull]IEnumerable<Type> drawerOrEditorTypes, [NotNull]FieldInfo targetTypeField, [NotNull]ref Dictionary<Type,Type> drawersByInspectedType, bool canBeAbstract) where TAttribute : Attribute
		#endif
		{
			var attType = typeof(TAttribute);
			
			foreach(var drawerOrEditorType in drawerOrEditorTypes)
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(!drawerOrEditorType.IsAbstract);
				#endif

				var attributes = drawerOrEditorType.GetCustomAttributes(attType, false);
				for(int a = attributes.Length - 1; a >= 0; a--)
				{
					var attribute = attributes[a];

					#if CSHARP_7_3_OR_NEWER && USE_IL_FOR_GET_AND_SET
					var inspectedType = getTargetType(attribute);
					#else
					var inspectedType = targetTypeField.GetValue(attribute) as Type;
					#endif

					if(!inspectedType.IsAbstract || canBeAbstract)
					{
						if(!drawersByInspectedType.ContainsKey(inspectedType))
						{
							drawersByInspectedType.Add(inspectedType, drawerOrEditorType);
						}
						else if(IsHigherPriority(drawerOrEditorType, drawersByInspectedType[inspectedType]))
						{
							#if DEV_MODE && DEBUG_OVERRIDE_EDITOR
							Debug.LogWarning($"Replacing {inspectedType.Name} old Editor {drawersByInspectedType[inspectedType].FullName} with new editor {drawerOrEditorType.FullName}.");
							#endif

							drawersByInspectedType[inspectedType] = drawerOrEditorType;
						}
						#if DEV_MODE && DEBUG_IGNORE_EDITOR
						else if(drawerOrEditorType != drawersByInspectedType[inspectedType])
						{
							Debug.LogWarning($"Won't use Editor {drawerOrEditorType.FullName} from {drawerOrEditorType.Assembly.GetType().Name} for {inspectedType.Name} because already using {drawersByInspectedType[inspectedType].FullName} from {drawersByInspectedType[inspectedType].Assembly.GetType().Name}.");
						}
						#endif
					}
				}
			}
		}

		private static bool IsHigherPriority(Type compare, Type compareTo)
		{
			return GetPriority(compare) < GetPriority(compareTo);
		}

		/// <summary>
		/// Gets priority order for custom editor, property drawer or decorator drawer.
		/// Lower is better.
		/// </summary>
		/// <param name="type"> Custom editor, property drawer or decorator drawer type. </param>
		/// <returns> Priority order; lower is better. </returns>
		private static int GetPriority(Type type)
		{
			if(TypeExtensions.IsInUnityNamespaceThreadSafe(type))
			{
				// Unity's built-in types have lowest priority
				if(string.Equals(type.Namespace, "UnityEditor"))
				{
					return 4;
				}
				// Unity's types from imported packages have second-to-lowest priority.
				return 3;
			}
			// Sisus assembly has medium priority.
			if(type.Assembly == typeof(CustomEditorUtility).Assembly)
			{
				return 2;
			}
			// Other assemblies have highest priority.
			return 1;
		}

		#if CSHARP_7_3_OR_NEWER && USE_IL_FOR_GET_AND_SET
		private static void GetDrawersByInheritedInspectedTypesFromAttributes<TAttribute>([NotNull]IEnumerable<Type> drawerOrEditorTypes, [NotNull]MemberGetter<object, Type> getTargetType, [CanBeNull]FieldInfo useForChildrenField, [NotNull]ref Dictionary<Type,Type> drawersByInspectedType, bool targetMustBeUnityObject, bool canBeAbstract) where TAttribute : Attribute
		#else
		private static void GetDrawersByInheritedInspectedTypesFromAttributes<TAttribute>([NotNull]IEnumerable<Type> drawerOrEditorTypes, [NotNull]FieldInfo targetTypeField, [CanBeNull]FieldInfo useForChildrenField, [NotNull]ref Dictionary<Type,Type> drawersByInspectedType, bool targetMustBeUnityObject, bool canBeAbstract) where TAttribute : Attribute
		#endif
		{
			var attType = typeof(TAttribute);
			foreach(var drawerType in drawerOrEditorTypes)
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(!drawerType.IsAbstract);
				#endif

				var attributes = drawerType.GetCustomAttributes(attType, false);
				for(int a = attributes.Length - 1; a >= 0; a--)
				{
					var attribute = attributes[a];

					bool useForChildren = useForChildrenField == null ? true : (bool)useForChildrenField.GetValue(attribute);
					if(!useForChildren)
					{
						#if DEV_MODE
						if(typeof(DecoratorDrawer).IsAssignableFrom(drawerType)) { Debug.LogWarning(drawerType.Name+ ".useForChildren was "+StringUtils.False); }
						#endif
						continue;
					}

					#if CSHARP_7_3_OR_NEWER && USE_IL_FOR_GET_AND_SET
					var targetType = getTargetType(attribute);
					#else
					var targetType = targetTypeField.GetValue(attribute) as Type;
					#endif

					if(targetType == null)
                    {
						continue;
                    }

					if(targetType.IsClass)
					{
						IEnumerable<Type> allTypes;
						if(targetType.IsGenericTypeDefinition)
						{
							allTypes = TypeExtensions.GetAllTypesThreadSafe(targetType.Assembly, canBeAbstract, true, true).Where((t) => t.IsSubclassOfUndeclaredGeneric(targetType));
						}
						else
						{
							allTypes = TypeExtensions.GetExtendingTypesNotThreadSafe(targetType, true, canBeAbstract);
						}

						try
						{
							foreach(var extendingType in allTypes)
							{
								if(!drawersByInspectedType.ContainsKey(extendingType))
								{
									drawersByInspectedType.Add(extendingType, drawerType);
								}
							}
						}
						#if DEV_MODE
						catch(Exception e)
						{
							Debug.LogWarning(e);
						#else
						catch(Exception)
						{
						#endif
							// Do slow but safe loading with try-catch used for every type separately to figure out which type caused the exception.
							// This should also inform the user about the Type causing issues as well as instructions on how to deal with it.
							foreach(var extendingType in TypeExtensions.GetExtendingTypesThreadSafeExceptionSafeSlow(targetType, canBeAbstract, true, true))
							{
								if(!drawersByInspectedType.ContainsKey(extendingType))
								{
									drawersByInspectedType.Add(extendingType, drawerType);
								}
							}
						}
						continue;
					}

					// Value types don't support inheritance
					if(!targetType.IsInterface)
					{
						continue;
					}

					var implementingTypes = targetMustBeUnityObject ? targetType.GetImplementingUnityObjectTypesNotThreadSafe(true, canBeAbstract) : targetType.GetImplementingTypesNotThreadSafe(true, canBeAbstract);

					#if DEV_MODE && DEBUG_INTERFACE_SUPPORT
					Debug.Log("interface "+targetType.Name+" implementing types: "+StringUtils.ToString(implementingTypes));
					#endif

					foreach(var implementingType in implementingTypes)
					{
						#if DEV_MODE && PI_ASSERTATIONS
						Debug.Assert(!implementingType.IsAbstract || canBeAbstract);
						#endif

						if(!drawersByInspectedType.ContainsKey(implementingType))
						{
							#if DEV_MODE && DEBUG_INTERFACE_SUPPORT
							Debug.Log("Adding interface "+targetType.Name+" implementing type "+StringUtils.ToString(implementingType) +"...");
							#endif

							drawersByInspectedType.Add(implementingType, drawerType);
						}
					}

					#if SUPPORT_EDITORS_FOR_INTERFACES
					if(targetMustBeUnityObject)
					{
						drawersByInspectedType.Add(targetType, drawerType);
					}
					#endif
				}
			}
		}

		public static void BeginEditor(out bool editingTextFieldWas, out EventType eventType, out KeyCode keyCode)
		{
			BeginEditorOrPropertyDrawer(out editingTextFieldWas, out eventType, out keyCode);
		}

		public static void EndEditor(bool editingTextFieldWas, EventType eventType, KeyCode keyCode)
		{
			EndEditorOrPropertyDrawer(editingTextFieldWas, eventType, keyCode);
		}

		public static void BeginPropertyDrawer(out bool editingTextFieldWas, out EventType eventType, out KeyCode keyCode)
		{
			BeginEditorOrPropertyDrawer(out editingTextFieldWas, out eventType, out keyCode);
		}		

		public static void EndPropertyDrawer(bool editingTextFieldWas, EventType eventType, KeyCode keyCode)
		{
			EndEditorOrPropertyDrawer(editingTextFieldWas, eventType, keyCode);
		}

		private static void BeginEditorOrPropertyDrawer(out bool editingTextFieldWas, out EventType eventType, out KeyCode keyCode)
		{
			editingTextFieldWas = EditorGUIUtility.editingTextField;
			EditorGUIUtility.hierarchyMode = true;
			eventType = DrawGUI.LastInputEventType;
			var lastInputEvent = DrawGUI.LastInputEvent();
			keyCode = lastInputEvent == null ? KeyCode.None : lastInputEvent.keyCode;
		}

		private static void EndEditorOrPropertyDrawer(bool editingTextFieldWas, EventType eventType, KeyCode keyCode)
		{
			if(EditorGUIUtility.editingTextField != editingTextFieldWas)
			{
				if(eventType != EventType.KeyDown && eventType != EventType.KeyUp)
				{
					#if DEV_MODE && DEBUG_SET_EDITING_TEXT_FIELD
					Debug.Log("DrawGUI.EditingTextField = "+StringUtils.ToColorizedString(EditorGUIUtility.editingTextField)+" with eventType="+StringUtils.ToString(eventType)+", keyCode="+keyCode+", lastInputEvent="+StringUtils.ToString(DrawGUI.LastInputEvent()));
					#endif
					DrawGUI.EditingTextField = EditorGUIUtility.editingTextField;
				}
				else
				{
					switch(keyCode)
					{
						case KeyCode.UpArrow:
						case KeyCode.DownArrow:
						case KeyCode.LeftArrow:
						case KeyCode.RightArrow:
							if(!EditorGUIUtility.editingTextField)
							{
								#if DEV_MODE
								Debug.Log("DrawGUI.EditingTextField = "+StringUtils.ToColorizedString(false)+" with eventType="+StringUtils.ToString(eventType)+", keyCode="+keyCode+", lastInputEvent="+StringUtils.ToString(DrawGUI.LastInputEvent()));
								#endif
								DrawGUI.EditingTextField = false;
							}
							else // prevent Unity automatically starting field editing when field focus is changed to a text field, as that is not how Power Inspector functions
							{
								#if DEV_MODE
								Debug.LogWarning("EditorGUIUtility.editingTextField = "+StringUtils.ToColorizedString(false)+" with eventType="+StringUtils.ToString(eventType)+", keyCode="+keyCode+", lastInputEvent="+StringUtils.ToString(DrawGUI.LastInputEvent()));
								#endif
								EditorGUIUtility.editingTextField = false;
							}
							return;
						default:
							#if DEV_MODE
							Debug.Log("DrawGUI.EditingTextField = "+StringUtils.ToColorizedString(false)+" with eventType="+StringUtils.ToString(eventType)+", keyCode="+keyCode+", lastInputEvent="+StringUtils.ToString(DrawGUI.LastInputEvent()));
							#endif
							DrawGUI.EditingTextField = EditorGUIUtility.editingTextField;
							return;
					}
				}
			}
		}
	}
}
#endif