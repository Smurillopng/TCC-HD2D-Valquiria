//#define DEBUG_SHOW_PROPERTY
#define DEBUG_SHOW_FIELD

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.SceneManagement;
using Component = UnityEngine.Component;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

#if CSHARP_7_3_OR_NEWER
using Sisus.Vexe.FastReflection;
using System.Collections.Concurrent;
using System.Collections;
#endif

namespace Sisus
{
	public static class MemberInfoExtensions
	{
		private static readonly object[] params1 = new object[1];
		private static readonly object[] params2 = new object[2];
		private static readonly Stack<MemberInfo> ReusableMemberInfoStack = new Stack<MemberInfo>(DefaultFieldSearchDepth);

		public static bool IsInspectorViewable(this FieldInfo field, bool includeHidden, FieldVisibility showNonSerialized)
		{
			//don't show obsolete fields, even if hidden are included
			//since accessing them can cause errors to be thrown etc.
			if(Attribute<ObsoleteAttribute>.ExistsOn(field))
			{
				return false;
			}

			var fieldType = field.FieldType;

			//don't show cyclical fields to avoid infinite loop
			if(fieldType == field.DeclaringType && !Types.UnityObject.IsAssignableFrom(fieldType))
			{
				#if UNITY_2019_3_OR_NEWER
				//UPDATE: Unless using the SerializeReference attribute.
				if(!Attribute<SerializeReference>.ExistsOn(field))
				#endif
				{
				
					return false;
				}
			}

			// NEW: Skip showing pointers since getting their value can easily result in crashes.
			if(fieldType.IsPointer)
			{
				#if DEV_MODE && DEBUG_SHOW_FIELD
				Debug.LogWarning("Skipping pointer field: "+field);
				#endif
				return false;
			}

			bool attributeSetVisibility;
			if(TryGetAttributeDeterminedInspectorVisibility(field, out attributeSetVisibility, showNonSerialized != FieldVisibility.AllExceptHidden))
			{
				if(attributeSetVisibility)
				{
					return true;
				}

				if(!includeHidden)
				{
					return false;
				}

				// All fields are shown in Debug Mode+ even if they have the HideInInspector attribute except for static fields,
				// which are only shown if attributes are used to expose them (or when using the IsInspectorViewableInStaticMode method)
				// and property backing fields, as it makes more sense to show the properties themselves instead.
				// NOTE: Since backing fields are never public fields, this check only needs to be done when includeHidden is true.
				return !field.IsStatic && !field.IsPropertyBackingField();
			}

			// static fields are only shown if attributes are used to expose them
			if(field.IsStatic)
			{
				return false;
			}

			if(includeHidden)
			{
				// Property backing fields shouldn't be shown; it makes more sense to show the properties themselves instead.
				// NOTE: Since backing fields are never public fields, this check only needs to be done when includeHidden is true.
				if(field.IsPropertyBackingField())
				{
					return false;
				}

				// All fields no matter what access modifiers they have are shown when includeHidden is true.
				return true;
			}

			if(field.IsPublic)
			{
				if(showNonSerialized != FieldVisibility.SerializedOnly)
				{
					return true;
				}

				// public fields are shown by default only if the field type class has the Serializable attribute
				var type = field.FieldType;
				if(type.IsSerializable)
				{
					#if !UNITY_2020_1_OR_NEWER
					if(type.IsGenericType)
					{
						var genericType = type.GetGenericTypeDefinition();

						// Before version 2020.1 Unity couldn't handle serialization of generic types, with the exception of List<T>,
						// so don't display them by default, even if they're public, to avoid confusion. They can still be forced visible using the SerializeField tag.
						if(genericType == Types.List)
						{
							var listType = type.GetGenericArguments()[0];
							//TO DO: Should check the generic type recursively
							//return listType.IsInspectorViewable();
							return !listType.IsGenericType && (listType.IsSerializable || listType.Assembly == Types.UnityAssembly);
						}

						return false;
					}
					#endif

					if(type == Types.DateTime || type == Types.TimeSpan || typeof(Delegate).IsAssignableFrom(type))
					{
						return false;
					}
					
					return true;
				}
				
				//for some reason these are not marked as serializable
				//but as a special case Unity can still serialize them
				//(and displays them in the inspector)
				//if(type == typeof(AnimationCurve)
				//|| type == typeof(Color)
				//|| type == typeof(Color32)
				//|| type == typeof(AnimationClip))
				if(type.Assembly == Types.UnityAssembly)
				{
					return true;
				}
				var baseType = type.BaseType;
				while(baseType != null)
				{
					if(baseType.Assembly == Types.UnityAssembly)
					{
						return true;
					}
					baseType = baseType.BaseType;
				}

				return false;
			}

			return showNonSerialized == FieldVisibility.AllExceptHidden;
		}

		public static bool IsInspectorViewableInStaticMode(this FieldInfo field, bool includeHidden)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(field.IsStatic);
			#endif
			
			// don't show obsolete fields, even if hidden are included
			// since accessing them can cause errors to be thrown etc.
			if(field.IsDefined(Types.ObsoleteAttribute, true))
			{
				return false;
			}

			var fieldType = field.FieldType;

			// don't show cyclical fields to avoid infinite loop
			if(fieldType == field.DeclaringType && !Types.UnityObject.IsAssignableFrom(fieldType))
			{
				return false;
			}

			// NEW: Skip showing pointers since getting their value can easily result in crashes.
			if(fieldType.IsPointer)
			{
				#if DEV_MODE && DEBUG_SHOW_FIELD
				Debug.LogWarning("Skipping pointer field: "+field);
				#endif
				return false;
			}

			if(includeHidden)
			{
				// property backing fields shouldn't be shown; it makes
				// more sense to show the properties themselves instead.
				// since backing fields are never public fields, this
				// check only needs to be done when includeHidden is true.
				if(field.IsPropertyBackingField())
				{
					return false;
				}

				// show all other fields when includeHidden is true
				// even ones marked with HideInInspector
				return true;
			}
			
			bool attributeSetVisibility;
			if(TryGetAttributeDeterminedInspectorVisibility(field, out attributeSetVisibility))
			{
				return attributeSetVisibility;
			}

			return field.IsPublic;
		}

		public static bool IsInspectorViewable(this PropertyInfo property, PropertyVisibility include, bool includeHidden)
		{
			if(PropertyBlacklist.IsBlacklisted(property))
			{
				#if DEV_MODE && DEBUG_SHOW_PROPERTY
				Debug.LogWarning("Skipping blacklisted property: "+property);
				#endif
				return false;
			}

			// don't show obsolete properties, even if hidden are included
			// since accessing them can cause errors to be thrown etc.
			if(property.IsDefined(Types.ObsoleteAttribute, true))
			{
				return false;
			}

			var propertyType = property.PropertyType;

			// NEW: Skip showing pointers since getting their value can easily result in crashes.
			if(propertyType.IsPointer)
			{
				#if DEV_MODE && DEBUG_SHOW_PROPERTY
				Debug.LogWarning("Skipping pointer property: "+property);
				#endif
				return false;
			}

			// Don't show cyclical properties to avoid infinite loop.
			if(propertyType == property.DeclaringType && !Types.UnityObject.IsAssignableFrom(propertyType))
			{
				#if UNITY_2019_3_OR_NEWER
				//UPDATE: Unless using the SerializeReference attribute.
				var backingField = property.GetPropertyBackingField();
				if(backingField == null || !Attribute<SerializeReference>.ExistsOn(backingField))
				#endif
				{
					#if DEV_MODE
					Debug.LogWarning("Skipping showing cyclical property: " + property + " in " + property.ReflectedType.Name);
					#endif
					return false;
				}
			}

			#if DEV_MODE
			if(property.Name.ToLower().Contains("get_") || property.Name.ToLower().Contains("get_"))
			{
				Debug.Log("INTERNAL PROPERTY: " + property.Name + " with includeHidden=" + includeHidden);
			}
			#endif
			
			if(includeHidden)
			{
				return true;
			}

			// check for attribute-based visibility for property itself
			bool attributeSetVisibility;
			if(TryGetAttributeDeterminedInspectorVisibility(property, out attributeSetVisibility))
			{
				#if DEV_MODE && DEBUG_SHOW_PROPERTY
				Debug.LogWarning("TryGetAttributeDeterminedInspectorVisibility for property " + property + ": "+StringUtils.ToColorizedString(attributeSetVisibility));
				#endif

				if(attributeSetVisibility)
				{
					return true;
				}

				if(!includeHidden)
				{
					return false;
				}

				// All properties are shown in Debug Mode+ even if they have the HideInInspector attribute
				// except for static properties, which are only shown if attributes are used to expose them
				// (or when using the IsInspectorViewableInStaticMode method)...
				return !property.IsStatic();
			}

			// figure out property getter visibility
			bool hideGet;
			MethodInfo getter;
			if(property.CanRead)
			{
				bool showGet;
				getter = property.GetGetMethod(true);
				if(TryGetAttributeDeterminedInspectorVisibility(getter, out showGet))
				{
					if(showGet)
					{
						#if DEV_MODE && DEBUG_SHOW_PROPERTY
						Debug.LogWarning("TryGetAttributeDeterminedInspectorVisibility for property getter " + property + ": "+StringUtils.True);
						#endif
						return true;
					}
					#if DEV_MODE && DEBUG_SHOW_PROPERTY
					Debug.LogWarning("TryGetAttributeDeterminedInspectorVisibility for property getter " + property + ": "+StringUtils.False);
					#endif
					hideGet = true;
				}
				// static properties are only shown if attributes are used to expose them
				else if(getter.IsStatic)
				{
					hideGet = true;
				}
				// non-public properties are never shown when not exposed with attributes and includeHidden is false
				else
				{
					hideGet = !getter.IsPublic;
				}
			}
			else
			{
				hideGet = true;
			}

			// figure out property setter visibility
			bool hideSet;
			MethodInfo setter;
			if(property.CanWrite)
			{
				setter = property.GetSetMethod(true);
				bool showSet;
				if(TryGetAttributeDeterminedInspectorVisibility(setter, out showSet))
				{
					if(showSet)
					{
						#if DEV_MODE && DEBUG_SHOW_PROPERTY
						Debug.LogWarning("TryGetAttributeDeterminedInspectorVisibility for property setter " + property + ": "+StringUtils.True);
						#endif
						return true;
					}
					#if DEV_MODE && DEBUG_SHOW_PROPERTY
					Debug.LogWarning("TryGetAttributeDeterminedInspectorVisibility for property setter " + property + ": "+StringUtils.False);
					#endif
					hideSet = true;
				}
				// static properties are only shown if attributes are used to expose them
				else if(setter.IsStatic)
				{
					hideSet = true;
				}
				// non-public properties are never shown when not exposed with attributes and includeHidden is false
				else
				{
					hideSet = !setter.IsPublic;
				}
			}
			else
			{
				hideSet = true;
			}
			
			// if neither get or set are shown, return false
			if(hideGet && hideSet)
			{
				return false;
			}

			// We now know that this is an instance property with a public getter or setter...
			switch(include)
			{
				case PropertyVisibility.AttributeExposedOnly:
					// ...but since it was not exposed with attributes, it will not be shown with this setting.
					return false;
				case PropertyVisibility.PublicAutoGenerated:
					// ...but it still also has to be an auto-property to be shown with this setting.
					return !property.IsAutoProperty();
				case PropertyVisibility.AllPublic:
					// ...and that is all that is required for it to be shown with this setting.
					return true;
				default:
					throw new NotImplementedException(include + " not supported by IsInspectorViewable");
			}
		}

		public static bool IsInspectorViewableInStaticMode(this PropertyInfo property, PropertyVisibility include, bool includeHidden)
		{
			// don't show obsolete fields, even if hidden are included
			// since accessing them can cause errors to be thrown etc.
			if(property.IsDefined(Types.ObsoleteAttribute, true))
			{
				return false;
			}

			// don't show cyclical fields to avoid infinite loop
			if(property.PropertyType == property.DeclaringType && !Types.UnityObject.IsAssignableFrom(property.PropertyType))
			{
				return false;
			}

			#if DEV_MODE
			if(property.Name.ToLower().Contains("get_") || property.Name.ToLower().Contains("get_"))
			{
				Debug.Log("INTERNAL PROPERTY: " + property.Name + " with includeHidden=" + includeHidden);
			}
			#endif
			
			if(includeHidden)
			{
				return true;
			}

			bool attributeSetVisibility;
			if(TryGetAttributeDeterminedInspectorVisibility(property, out attributeSetVisibility))
			{
				return attributeSetVisibility;
			}

			bool hideGet;
			MethodInfo getter;
			if(property.CanRead)
			{
				bool showGet;
				getter = property.GetGetMethod(true);

				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(getter.IsStatic);
				#endif

				if(TryGetAttributeDeterminedInspectorVisibility(getter, out showGet))
				{
					if(showGet)
					{
						return true;
					}
					hideGet = true;
				}
				// non-public properties are never shown when not exposed with attributes and includeHidden is false
				else
				{
					hideGet = !getter.IsPublic;
				}
			}
			else
			{
				hideGet = true;
			}
			
			bool hideSet;
			MethodInfo setter;
			if(property.CanWrite)
			{
				setter = property.GetSetMethod(true);

				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(setter.IsStatic);
				#endif

				bool showSet;
				if(TryGetAttributeDeterminedInspectorVisibility(setter, out showSet))
				{
					if(showSet)
					{
						return true;
					}
					hideSet = true;
				}
				// non-public properties are never shown when not exposed with attributes and includeHidden is false
				else
				{
					hideSet = !setter.IsPublic;
				}
			}
			else
			{
				hideSet = true;
			}
			
			// if neither get or set are shown, return false
			if(hideGet && hideSet)
			{
				return false;
			}

			// We now know that this is a static property with a public getter or setter...
			switch(include)
			{
				case PropertyVisibility.AttributeExposedOnly:
					// ...but since it was not exposed with attributes, it will not be shown with this setting.
					return false;
				case PropertyVisibility.PublicAutoGenerated:
					// ...but it still also has to be an auto-property to be shown with this setting.
					return !property.IsAutoProperty();
				case PropertyVisibility.AllPublic:
					// ...and that is all that is required for it to be shown with this setting.
					return true;
				default:
					throw new NotImplementedException(include + " not supported by IsInspectorViewable");
			}
		}
		
		/// <summary>
		/// Should the PropertyInfo (which we already know should be shown in the inspector) be
		/// displayed like normal field, instead of using PropertyDrawer with separate
		/// buttons for getting and setting value.
		/// This requires the following conditions to be met:
		/// 1. the property has a getter
		/// 2. the property is not an indexer
		/// 3. the property is auto-generated OR the property has been tagged to be shown with attributes
		/// </summary>
		/// <param name="property"> The property to act on. </param>
		/// <returns> True if it succeeds, false if it fails. </returns>
		public static bool ShowInspectorViewableAsNormalField(this PropertyInfo property)
		{
			//set-only properties can't be shown as normal fields
			if(!property.CanRead)
			{
				return false;
			}

			//indexer properties can't be shown as normal fields
			if(property.GetIndexParameters().Length > 0)
			{
				return false;
			}

			// Due to their unsafety never show pointers as normal fields.
			if(property.PropertyType.IsPointer)
			{
				return false;
			}

			//auto-properties with a getter are always safe to be shown as normal fields
			if(property.IsAutoProperty())
			{
				return true;
			}
			
			//If getter is marked to be shown in the inspector then we can
			//show the property like a normal field (even if setter is
			// marked to be hidden, it can still be shown as a read-only field).
			//If getter is hidden, then property can't be shown as a normal field.
			bool show;
			var getter = property.GetGetMethod(true);
			if(TryGetAttributeDeterminedInspectorVisibility(getter, out show))
			{
				return show;
			}

			//If property is marked to be shown in the inspector and getter
			//was not hidden, then it should be shown like a normal field.
			if(TryGetAttributeDeterminedInspectorVisibility(property, out show))
			{
				return show;
			}

			// If neither property or its getter were marked to be shown in the inspector
			// then their getters could have undesired side effects, and are not safe to be shown
			// as normal fields.
			// UPDATE: If getter is public by default we'll assume that it's safe to display as a normal field.
			// UPDATE 2: Allow users to customize this behaviour.
			return getter.IsPublic && InspectorUtility.Preferences.simplePropertiesInDebugMode;
		}

		#if CSHARP_7_3_OR_NEWER
		private static readonly ConcurrentDictionary<PropertyInfo, bool> IsAutoPropertyCache = new ConcurrentDictionary<PropertyInfo, bool>(1, 128);
		private static readonly ConcurrentDictionary<MemberInfo, bool> IsInspectorViewableCache = new ConcurrentDictionary<MemberInfo, bool>();
		private static readonly ConcurrentDictionary<MemberInfo, bool> IsInspectorViewableInDebugModeCache = new ConcurrentDictionary<MemberInfo, bool>();
		#endif

		public static bool IsInspectorViewable(this MethodInfo method, MethodVisibility methodVisibility, bool includeHidden)
		{
			#if CSHARP_7_3_OR_NEWER
			var cache = includeHidden ? IsInspectorViewableInDebugModeCache : IsInspectorViewableCache;
			bool cachedResult;
			if(cache.TryGetValue(method, out cachedResult))
			{
				return cachedResult;
			}
			#endif

			//don't show obsolete fields, even if hidden are included
			//since accessing them can cause errors to be thrown etc.
			if(method.IsDefined(Types.ObsoleteAttribute, true))
			{
				#if CSHARP_7_3_OR_NEWER
				cache[method] = false;
				#endif

				return false;
			}

			bool attributeSetVisibility;
			if(TryGetAttributeDeterminedInspectorVisibility(method, out attributeSetVisibility))
			{
				#if CSHARP_7_3_OR_NEWER
				cache[method] = attributeSetVisibility;
				#endif

				return attributeSetVisibility;
			}

			// static methods are only shown if attributes are used to expose them
			if(method.IsStatic)
			{
				#if CSHARP_7_3_OR_NEWER
				cache[method] = false;
				#endif

				return false;
			}

			if(methodVisibility != MethodVisibility.AttributeExposedOnly)
			{
				var contextMenu = AttributeUtility.GetAttribute<ContextMenu>(method);
				if(contextMenu != null)
				{
					//skip showing validate methods
					if(contextMenu.validate)
					{
						#if CSHARP_7_3_OR_NEWER
						cache[method] = false;
						#endif

						return false;
					}

					#if CSHARP_7_3_OR_NEWER
					cache[method] = true;
					#endif

					return true;
				}
			}

			if(includeHidden)
			{
				//as a special case skip methods with names starting with get_, set_ or INTERNAL_
				//as these are commonly used by Unity components for internal things that should not really get exposed
				string name = method.Name;
				int length = name.Length;
				if(length > 4)
				{
					//skip methods with names starting with "get_" or "set_" as these are commonly
					//found in Property helper methods of built-in components (e.g. Transform)
					//and as such are redundant.
					if(name[3] == '_')
					{
						if(name.StartsWith("get", StringComparison.Ordinal) || name.StartsWith("set", StringComparison.Ordinal))
						{
							#if CSHARP_7_3_OR_NEWER
							cache[method] = false;
							#endif

							return false;
						}
					}
					//Skip methods with names starting with "INTERNAL_" as these are commonly
					//found in internal helper methods of built-in components (e.g. Transform).
					//Being named so probably means that they should not be exposed for external tinkering
					//and are often redundant anyways due to there being public method variants for doing the same thing.
					else if(name.StartsWith("INTERNAL", StringComparison.Ordinal))
					{
						#if CSHARP_7_3_OR_NEWER
						cache[method] = false;
						#endif

						return false;
					}
				}

				return true;
			}
			
			if(methodVisibility == MethodVisibility.AllPublic)
			{
				#if CSHARP_7_3_OR_NEWER
				cache[method] = method.IsPublic;
				#endif

				return method.IsPublic;
			}

			#if CSHARP_7_3_OR_NEWER
			cache[method] = false;
			#endif

			return false;
		}

		public static bool IsInspectorViewableInStaticMode(this MethodInfo method, MethodVisibility methodVisibility, bool includeHidden)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(method.IsStatic);
			#endif

			//don't show obsolete fields, even if hidden are included
			//since accessing them can cause errors to be thrown etc.
			if(method.IsDefined(Types.ObsoleteAttribute, true))
			{
				return false;
			}

			bool attributeSetVisibility;
			if(TryGetAttributeDeterminedInspectorVisibility(method, out attributeSetVisibility))
			{
				return attributeSetVisibility;
			}

			if(methodVisibility != MethodVisibility.AttributeExposedOnly)
			{
				var contextMenu = AttributeUtility.GetAttribute<ContextMenu>(method);
				if(contextMenu != null)
				{
					//skip showing validate methods
					if(contextMenu.validate)
					{
						return false;
					}
					return true;
				}
			}

			if(includeHidden)
			{
				//as a special case skip methods with names starting with get_, set_ or INTERNAL_
				//as these are commonly used by Unity components for internal things that should not really get exposed
				string name = method.Name;
				int length = name.Length;
				if(length > 4)
				{
					//skip methods with names starting with "get_" or "set_" as these are commonly
					//found in Property helper methods of built-in components (e.g. Transform)
					//and as such are redundant.
					if(name[3] == '_')
					{
						if(name.StartsWith("get", StringComparison.Ordinal) || name.StartsWith("set", StringComparison.Ordinal))
						{
							return false;
						}
					}
					//Skip methods with names starting with "INTERNAL_" as these are commonly
					//found in internal helper methods of built-in components (e.g. Transform).
					//Being named so probably means that they should not be exposed for external tinkering
					//and are often redundant anyways due to there being public method variants for doing the same thing.
					else if(name.StartsWith("INTERNAL", StringComparison.Ordinal))
					{
						return false;
					}
				}

				return true;
			}
			
			return methodVisibility == MethodVisibility.AllPublic && method.IsPublic;
		}

		internal static bool TryGetAttributeDeterminedInspectorVisibility(MemberInfo member, out bool showInInspector, bool skipNonSerialized = true)
		{
			showInInspector = true;
			bool result = false;
			int priority = 0;

			var attributes = AttributeUtility.GetAttributes(member);

			#if DEV_MODE && DEBUG_SHOW_PROPERTY
			Debug.LogWarning("attributes for " + member + ": "+StringUtils.TypesToString(attributes));
			#endif

			for(int n = attributes.Length - 1; n >= 0; n--)
			{
				var att = attributes[n];

				// If a field has been specifically marked with the
				// HideInInspector don't display it even if it's public.
				var hideInInspector = att as HideInInspector;
				if(hideInInspector != null)
				{
					showInInspector = false;
					return true;
				}

				// Show all fields that have the SerializeField attribute. Even if Unity can't really serialize e.g. a Dictionary field,
				// it can be useful to allow users force it visible with the SerializeField attribute, and they can then handle the serialization manually,
				// only populate it at runtime etc. The SerializeField attribute is kind of like Unity's ShowInInspector attribute (since there is no built-in
				// ShowInInspector attribute - although I'm also adding manual support for the EditorBrowsableAttribute here)
				var serializeField = att as SerializeField;
				if(serializeField != null)
				{
					if(priority < 1)
					{
						priority = 1;
						// Show in inspector unless another attribute of higher priority (like HideInInspector) is found.
						showInInspector = true;
						result = true;
					}
					continue;
				}

				#if UNITY_2019_3_OR_NEWER
				var serializeReference = att as SerializeReference;
				if(serializeReference != null)
				{
					if(priority < 1)
					{
						priority = 1;
						// Show in inspector unless another attribute of higher priority (like HideInInspector) is found.
						showInInspector = true;
						result = true;
					}
					continue;
				}
				#endif

				var nonSerialized = att as NonSerializedAttribute;
				if(nonSerialized != null && skipNonSerialized)
				{
					if(priority < 1)
					{
						priority = 1;
						//hide in inspector unless another attribute of higher priority (like ShowInInspector) is found
						showInInspector = false;
						result = true;
					}
					continue;
				}

				// If a class member has been marked with the EditorBrowsableAttribute
				// using the Always parameter, display the field even if it's not public
				var editorBrowsable = att as EditorBrowsableAttribute;
				if(editorBrowsable != null)
				{
					if(editorBrowsable.State == EditorBrowsableState.Always)
					{
						if(priority < 2)
						{
							priority = 2;
							showInInspector = true;
							result = true;
						}
					}
					else if(editorBrowsable.State == EditorBrowsableState.Never)
					{
						if(priority < 2)
						{
							priority = 2;
							showInInspector = false;
							result = true;
						}
					}
					continue;
				}

				var browsable = att as BrowsableAttribute;
				if(browsable != null)
				{
					if(priority < 2)
					{
						priority = 2;
						if(browsable.Browsable)
						{
							//show in inspector unless another attribute of higher priority (like HideInInspector) is found
							showInInspector = true;
							result = true;
							continue;
						}
						//hide in inspector unless another attribute of higher priority (like ShowInInspector) is found
						showInInspector = false;
						result = true;
					}
					continue;
				}

				var showInInspectorAttribute = att as Attributes.ShowInInspectorAttribute;
				if(showInInspectorAttribute != null)
				{
					showInInspector = true;
					return true;
				}

				// NEW: Also display all fields that contain the ReadOnly attribute
				var readOnlyAttribute = att as Attributes.ReadOnlyAttribute;
				if(readOnlyAttribute != null)
				{
					showInInspector = true;
					return true;
				}

				var showInInspectorIf = att as Attributes.IShowInInspectorIf;
				if(showInInspectorIf != null)
				{
					// at this point always return true. handle dynamic hiding at drawer level
					showInInspector = true;
					return true;
				}

				// support any attributes named "ShowInInspector" or "ShowInInspectorAttribute"
				var attName = att.GetType().Name;
				if((attName.Length == 15 || attName.Length == 24) && attName.StartsWith("ShowInInspector", StringComparison.Ordinal))
				{
					if(attName.Length == 15 || attName.EndsWith("Attribute", StringComparison.Ordinal))
					{
						showInInspector = true;
						return true;
					}
				}
			}
			return result;
		}

		public static bool IsPropertyBackingField(this FieldInfo fieldInfo)
		{
			return fieldInfo.Name.EndsWith(">k__BackingField", StringComparison.Ordinal);
		}

		#if UNITY_EDITOR
		public static Type GetTargetType(this SerializedProperty property)
		{
			switch(property.type)
			{
				case "bool":
					if(property.isArray)
					{
						#if DEV_MODE
						Debug.Log("property type is bool[]?: " + property.type);
						#endif
						return typeof(bool[]); //could this also be a List<bool>?)
					}
					return Types.Bool;
				case "int":
					if(property.isArray)
					{
						#if DEV_MODE
						Debug.Log("property type is int[]?: " + property.type);
						#endif
						return typeof(int[]);
					}
					return Types.Int;
				case "string":
					if(property.isArray)
					{
						return typeof(string[]);
					}
					return Types.String;
				case "Vector3":
					if(property.isArray)
					{
						return typeof(Vector3[]);
					}
					return Types.Vector3;
				case "Vector2":
					if(property.isArray)
					{
						return typeof(Vector2[]);
					}
					return Types.Vector2;
				case "Rect":
					if(property.isArray)
					{
						return typeof(Rect[]);
					}
					return Types.Rect;
				case "float":
					if(property.isArray)
					{
						return typeof(float[]);
					}
					return Types.Float;
				default:
					MemberInfo memberInfo;
					object owner;
					SerializedPropertyExtensions.GetMemberInfoAndOwner(property, out memberInfo, out owner);
					if(memberInfo != null)
					{
						var fieldInfo = memberInfo as FieldInfo;
						if(fieldInfo != null)
						{
							return fieldInfo.FieldType;
						}
						var propertyInfo = memberInfo as PropertyInfo;
						if(propertyInfo != null)
						{
							return propertyInfo.PropertyType;
						}
						var methodInfo = memberInfo as MethodInfo;
						if(methodInfo != null)
						{
							return methodInfo.ReturnType;
						}
					}
					return Types.SystemObject;
			}
		}
		#endif

		/// <summary>
		/// https://docs.unity3d.com/Manual/script-Serialization.html
		/// </summary>
		public static bool IsUnitySerializable(this FieldInfo field)
		{
			//if a field has been marked with the NonSerialized
			//attribute, don't display it even if it's public
			if(field.IsNotSerialized)
			{
				return false;
			}

			var type = field.FieldType;

			if(!type.IsSerializable)
			{
				// It seems that various Unity classes don't have the Serializable attribute
				// but Unity can still serialize them internally.
				//if(type == typeof(AnimationCurve)
				//|| type == typeof(Color)
				//|| type == typeof(Color32)
				//|| type == typeof(AnimationClip))
				if(type.Assembly != Types.UnityAssembly)
				{
					return false;
				}
			}

			//Unity won't serialize readonly fields
			if(field.IsInitOnly || !field.FieldType.IsAbstract)
			{
				return false;
			}

			#if UNITY_2019_3_OR_NEWER
			if(field.IsPublic || field.IsDefined(Types.SerializeField, true) || field.IsDefined(typeof(SerializeReference), true))
			#else
			if(field.IsPublic || field.IsDefined(Types.SerializeField, true))
			#endif
			{
				//can't do nullables
				if(Nullable.GetUnderlyingType(type) != null)
				{
					return false;
				}

				#if !UNITY_2020_1_OR_NEWER
				if(type.IsGenericType)
				{
					var genericType = type.GetGenericTypeDefinition();

					// Unity could not handle serialization of generic types, with the exception of List<T>, before version 2020.1
					if(genericType == Types.List)
					{
						var listType = type.GetGenericArguments()[0];
						return !listType.IsGenericType && (listType.IsSerializable || listType.Assembly == Types.UnityAssembly);
					}

					return false;
				}
				#endif

				if(type.IsEnum)
				{
					type = Enum.GetUnderlyingType(type);
				}

				if(type.IsPrimitive)
				{
					if(Types.ULong.IsAssignableFrom(type) || Types.UShort.IsAssignableFrom(type) || Types.UInt.IsAssignableFrom(type) || Types.Short.IsAssignableFrom(type) || Types.SByte.IsAssignableFrom(type))
					{
						return false;
					}
				}

				if(type == Types.DateTime || type == Types.TimeSpan)
				{
					return false;
				}

				return true;
			}

			return false;
		}

		public static bool IsAutoProperty(this PropertyInfo property)
		{
			try
			{
				#if CSHARP_7_3_OR_NEWER
				bool result;
				if(IsAutoPropertyCache.TryGetValue(property, out result))
				{
					return result;
				}
				#endif

				if(!property.CanWrite || !property.CanRead)
				{
					#if CSHARP_7_3_OR_NEWER
					IsAutoPropertyCache[property] = false;
					#endif

					return false;
				}

				var fields = property.DeclaringType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
				for(int n = fields.Length - 1; n >= 0; n--)
				{
					string name = fields[n].Name;
					if(name[0] == '<')
					{
						if(name.StartsWith(string.Concat("<", property.Name, ">")))
						{
							#if CSHARP_7_3_OR_NEWER
							IsAutoPropertyCache[property] = true;
							#endif

							return true;
						}
					}
				}
			}
			catch { }

			#if CSHARP_7_3_OR_NEWER
			IsAutoPropertyCache[property] = false;
			#endif

			return false;
		}

		public static bool EqualTo(this MemberInfo a, MemberInfo b)
		{
			switch(a.MemberType)
			{
				case MemberTypes.Constructor:
				case MemberTypes.Method:
					return Equals(a as MethodBase, b as MethodBase);
				case MemberTypes.Property:
				case MemberTypes.Event:
				case MemberTypes.Field:
					return b != null && a.MetadataToken == b.MetadataToken && a.Module.Equals(b.Module) && a.DeclaringType == b.DeclaringType;
				case MemberTypes.TypeInfo:
				case MemberTypes.NestedType:
					return a == b;
				default:
					if(a == b)
					{
						return true;
					}
					throw new NotImplementedException(a.MemberType.ToString());
			}
		}

		public static bool EqualTo(this FieldInfo a, FieldInfo b)
		{
			return b != null && a.MetadataToken == b.MetadataToken && a.Module.Equals(b.Module) && a.DeclaringType == b.DeclaringType;
		}

		public static bool EqualTo(this PropertyInfo a, PropertyInfo b)
		{
			return b != null && a.MetadataToken == b.MetadataToken && a.Module.Equals(b.Module) && a.DeclaringType == b.DeclaringType;
		}

		public static bool EqualTo(this MethodBase a, MethodBase b)
		{
			if(a == b)
			{
				return true;
			}

			if(b == null)
			{
				return false;
			}

			if(a.MetadataToken != b.MetadataToken || !a.Module.Equals(b.Module) || a.DeclaringType != b.DeclaringType)
			{
				return false;
			}

			return a.GetGenericArguments().ContentsMatch(b.GetGenericArguments());
		}

		public static bool EqualTo(this ParameterInfo a, ParameterInfo b)
		{
			if(a == b)
			{
				return true;
			}

			if(b == null)
			{
				return false;
			}

			return a.Position == b.Position && a.Member.Equals(b.Member);
		}

		public static bool IsStatic([NotNull]this MemberInfo member)
		{
			var fieldInfo = member as FieldInfo;
			if(fieldInfo != null)
			{
				return fieldInfo.IsStatic;
			}

			var propertyInfo = member as PropertyInfo;
			if(propertyInfo != null)
			{
				return propertyInfo.IsStatic();
			}

			var methodBase = member as MethodBase;
			if(methodBase != null)
			{
				return methodBase.IsStatic;
			}

			var eventInfo = member as EventInfo;
			if(eventInfo != null)
			{
				return eventInfo.GetRaiseMethod(true).IsStatic;
			}

			var type = member as Type;
			if(type != null)
			{
				return IsStatic(type);
			}

			if(member == null)
			{
				throw new NullReferenceException();
			}

			Debug.LogError("Failed to determine if MemberInfo of unknown type is static: "+StringUtils.TypeToString(member));

			return false;
		}
		
		public static bool IsStatic([NotNull]this Type type)
		{
			return type.IsSealed && type.IsAbstract;
		}

		public static bool IsStatic(this PropertyInfo propertyInfo)
		{
			return (propertyInfo.CanRead ? propertyInfo.GetGetMethod(true) : propertyInfo.GetSetMethod(true)).IsStatic;
		}

		/// <summary>
		/// Tries to invoke MethodInfo no matter if it static or not or if it has parameters.
		/// 
		/// If method is not static, then invokes method in all targets, otherwise only invokes
		/// it once.
		/// 
		/// If method has parameters, then uses default values for them all.
		/// NOTE: Currently will not respect NotNull attributes and such and can result in exceptions being thrown.
		/// </summary>
		/// <param name="method">
		/// The method to act on. </param>
		/// <param name="targets">
		/// The targets. </param>
		public static object AutoInvoke([NotNull]this MethodInfo method, [CanBeNull]object[] targets)
		{
			object[] parameters = null;
			var parameterInfos = method.GetParameters();
			int paramCount = parameterInfos.Length;
			if(paramCount > 0)
			{
				parameters = ArrayPool<object>.Create(paramCount);
				for(int n = paramCount - 1; n >= 0; n--)
				{
					parameters[n] = parameterInfos[n].DefaultValue();
				}
			}

			if(method.IsStatic() || targets == null)
			{
				return method.Invoke(null, parameters);
			}
			
			object result = null;
			int count  = targets.Length;
			for(int n = count - 1; n >= 0; n--)
			{
				result = method.Invoke(targets[n], parameters);
			}

			return result;
		}

		public static void FindInstancesOfTypeInOpenScenes(this Type typeToFind, BindingFlags bindingFlags, [NotNull]ref List<LinkedMemberInfo> results)
		{
			for(int n = SceneManager.sceneCount - 1; n >= 0; n--)
			{
				SceneManager.GetSceneAt(n).FindInstancesOfType(typeToFind, bindingFlags, ref results);
			}
		}

		public static void FindInstancesOfType(this Scene scene, Type typeToFind, BindingFlags bindingFlags, [NotNull]ref List<LinkedMemberInfo> results)
		{
			var gameObjects = scene.GetAllGameObjects();

			for(int g = gameObjects.Length - 1; g >= 0; g--)
			{
				var gameObject = gameObjects[g];
				gameObject.FindInstancesOfType(typeToFind, bindingFlags, ref results);
			}
		}

		#if UNITY_EDITOR
		public static void FindInstancesOfTypeInResources(this Type typeToFind, BindingFlags bindingFlags, [NotNull]ref List<LinkedMemberInfo> results)
		{
			var allAssetGuids = AssetDatabase.FindAssets("t:Object");
			var allAssetPaths = allAssetGuids.Select(AssetDatabase.GUIDToAssetPath).Distinct().ToArray();
			for(int n = allAssetPaths.Length - 1; n >= 0; n--)
			{
				var assetPath = allAssetPaths[n];
				var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
				for(int a = assets.Length - 1; n >= 0; n--)
				{
					assets[a].FindInstancesOfType(typeToFind, bindingFlags, ref results);
				}
			}
		}
		#endif

		public static void FindInstancesOfType(this GameObject gameObject, Type typeToFind, BindingFlags bindingFlags, [NotNull]ref List<LinkedMemberInfo> results)
		{
			var components = gameObject.GetComponents<Component>();
			for(int c = components.Length - 1; c >= 0; c--)
			{
				var component = components[c];
				if(component != null)
				{
					component.FindInstancesOfType(typeToFind, bindingFlags, ref results);
				}
			}
		}
		
		public static void FindInstancesOfType(this Object unityObject, Type typeToFind, BindingFlags bindingFlags, [NotNull]ref List<LinkedMemberInfo> results)
		{
			MemberInfoExtensions.ReusableMemberInfoStack.Clear();
			var unityObjectType = unityObject.GetType();
			unityObjectType.FindInstancesOfType(MemberInfoExtensions.ReusableMemberInfoStack, typeToFind, unityObject, bindingFlags, ref results);
		}
		
		public const int DefaultFieldSearchDepth = 3;

		public static bool FindInstancesOfType(this Type searchInType, [NotNull]Stack<MemberInfo> parents, [NotNull]Type typeToFind, [CanBeNull]Object targetObject, BindingFlags bindingFlags, [NotNull]ref List<LinkedMemberInfo> results, int searchDepth = DefaultFieldSearchDepth)
		{
			int countWas = results.Count;
			var order = InspectorUtility.Preferences.MemberDisplayOrder;
			
			for(int t = 0; t < 3; t++)
			{
				switch(order[t])
				{
					case Member.Field:
						var fields = searchInType.GetFields(bindingFlags);
						for(int n = 0, count = fields.Length; n < count; n++)
						{
							var field = fields[n];
							var testType = field.FieldType;
							if(typeToFind.IsAssignableFrom(testType))
							{
								parents.Push(field);
								results.Add(LinkedMemberInfoPool.Create(targetObject, parents.ToArray()));
								parents.Pop();
							}
							else if(searchDepth > 0)
							{
								parents.Push(field);
								testType.FindInstancesOfType(parents, typeToFind, targetObject, bindingFlags, ref results, searchDepth - 1);
								parents.Pop();
							}
						}
						break;
					case Member.Property:
						var properties = searchInType.GetProperties(bindingFlags);
						for(int n = 0, count = properties.Length; n < count; n++)
						{
							var property = properties[n];
							var testType = property.PropertyType;
							if(typeToFind.IsAssignableFrom(testType))
							{
								parents.Push(property);
								results.Add(LinkedMemberInfoPool.Create(targetObject, parents.ToArray()));
								parents.Pop();
							}
							else if(searchDepth > 0)
							{
								parents.Push(property);
								testType.FindInstancesOfType(parents, typeToFind, targetObject, bindingFlags, ref results, searchDepth - 1);
								parents.Pop();
							}
						}
						break;
					case Member.Method:
						var methods = searchInType.GetMethods(bindingFlags);
						for(int n = 0, count = methods.Length; n < count; n++)
						{
							var method = methods[n];
							var testType = method.ReturnType;
							if(typeToFind.IsAssignableFrom(testType))
							{
								parents.Push(method);
								results.Add(LinkedMemberInfoPool.Create(targetObject, parents.ToArray()));
								parents.Pop();
							}
							//else if(searchDepth > 0)
							//{
							//	parents.Push(method);
							//	testType.FindInstancesOfType(parents, typeToFind, targetObject, bindingFlags, ref results, searchDepth - 1);
							//	parents.Pop();
							//}
						}
						break;
				}
			}

			return countWas != results.Count;
		}

		public static object DefaultValue(this ParameterInfo parameterInfo)
		{
			TypeExtensions.createInstanceRecursiveCallCount = 0;
			return parameterInfo.DefaultValueInternal();
		}

		public static object DefaultValueInternal(this ParameterInfo parameterInfo)
		{
			var defaultValue = parameterInfo.DefaultValue;
			
			// If parameter is not optional (DBNull),
			// optional parameter is not supplied (Missing)
			// or returned default value is null (this often happens even with value types!)
			// then use Type.DefaultValue() instead.
			if(defaultValue == DBNull.Value || defaultValue == Type.Missing || defaultValue == null)
			{
				defaultValue = parameterInfo.ParameterType.DefaultValueInternal();
			}

			return defaultValue;
		}

		public static void Set(this MemberInfo memberInfo, ref object owner, object value)
		{
			var field = memberInfo as FieldInfo;
			if(field != null)
			{
				field.SetValue(owner, value);
				return;
			}
			var property = memberInfo as PropertyInfo;
			if(property != null)
			{
				property.SetValue(owner, value, null);
			}
			throw new InvalidOperationException();
		}
		
		public static object InvokeWithParameters([NotNull]this MethodInfo method, [CanBeNull]object target, [CanBeNull]object parameter1, [CanBeNull]object parameter2)
		{
			params2[0] = parameter1;
			params2[1] = parameter2;
			return method.Invoke(target, params2);
		}

		public static object InvokeWithParameter([NotNull]this MethodInfo method, [CanBeNull]object target, [CanBeNull]object parameter)
		{
			params1[0] = parameter;
			return method.Invoke(target, params1);
		}

		public static object Invoke([NotNull]this MethodInfo method, [CanBeNull]object target)
		{
			return method.Invoke(target, null);
		}

		public static object Invoke([NotNull]this MethodInfo method)
		{
			return method.Invoke(null, null);
		}

		#if CSHARP_7_3_OR_NEWER
		public static TReturn InvokeWithParameter<TTarget, TReturn>([NotNull]this MethodCaller<TTarget, TReturn> methodCaller, TTarget target, [CanBeNull]object parameter)
		{
			params1[0] = parameter;
			return methodCaller(target, params1);
		}
		#endif

		public static FieldInfo GetPropertyBackingField(this PropertyInfo property)
		{
			return GetPropertyBackingField(property.DeclaringType, property.Name);
		}

		public static FieldInfo GetPropertyBackingField(this Type owningClassType, string propertyName)
		{
			return owningClassType.GetField(GetBackingFieldName(propertyName), BindingFlags.Instance | BindingFlags.NonPublic);
		}

		private static string GetBackingFieldName(string propertyName)
		{
			return string.Concat("<", propertyName, ">k__BackingField");
		}
	}
}