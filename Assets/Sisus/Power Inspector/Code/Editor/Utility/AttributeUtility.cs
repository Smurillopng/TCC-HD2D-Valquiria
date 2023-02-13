//#define THREAD_SAFE

using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using Sisus.Compatibility;

namespace Sisus
{
	public static class AttributeUtility
	{
		private const bool InheritMemberAttributes = true;
		private const bool InheritClassAttributes = false;

		#if THREAD_SAFE
		private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, object[]> AttributesByClass = new System.Collections.Concurrent.ConcurrentDictionary<Type, object[]>(2, 1024);
		private static readonly System.Collections.Concurrent.ConcurrentDictionary<MemberInfo, object[]> AttributesByMember = new System.Collections.Concurrent.ConcurrentDictionary<MemberInfo, object[]>(2, 1024);
		#else
		private static readonly Dictionary<Type, object[]> AttributesByClass = new Dictionary<Type, object[]>(1024);
		private static readonly Dictionary<MemberInfo, object[]> AttributesByMember = new Dictionary<MemberInfo, object[]>(2048);
		#endif

		[NotNull]
		public static object[] GetAttributes([NotNull]Type classType)
		{
			object[] attributes;
			if(AttributesByClass.TryGetValue(classType, out attributes))
			{
				return attributes;
			}

			attributes = classType.GetCustomAttributes(InheritClassAttributes);
			PluginAttributeConverterProvider.ConvertAll(ref attributes);
			#if THREAD_SAFE
			AttributesByClass[classType] = attributes;
			#else
			AttributesByClass.Add(classType, attributes);
			#endif
			return attributes;
		}

		[NotNull]
		public static object[] GetAttributes([NotNull]MemberInfo member)
		{
			object[] attributes;
			if(AttributesByMember.TryGetValue(member, out attributes))
			{
				return attributes;
			}

			attributes = member.GetCustomAttributes(InheritMemberAttributes);
			PluginAttributeConverterProvider.ConvertAll(ref attributes);
			#if THREAD_SAFE
			AttributesByMember[member] = attributes;
			#else
			AttributesByMember.Add(member, attributes);
			#endif
			return attributes;
		}

		public static bool HasAttribute<TAttribute>([NotNull]Type classType) where TAttribute : Attribute
		{
			object[] attributes;
			if(AttributesByClass.TryGetValue(classType, out attributes))
			{
				for(int n = attributes.Length - 1; n >= 0; n--)
				{ 
					if(attributes[n] is TAttribute)
					{
						return true;
					}
				}
				return false;
			}

			#if CSHARP_7_3_OR_NEWER
			return classType.GetCustomAttribute<TAttribute>(InheritClassAttributes) != null;
			#else
			return classType.GetCustomAttributes(typeof(TAttribute), InheritClassAttributes).Length > 0;
			#endif
		}

		public static bool HasAttribute([NotNull]Type classType, Type attributeType)
		{
			object[] attributes;
			if(AttributesByClass.TryGetValue(classType, out attributes))
			{
				for(int n = attributes.Length - 1; n >= 0; n--)
				{ 
					if(attributes[n].GetType() == attributeType)
					{
						return true;
					}
				}
				return false;
			}

			#if CSHARP_7_3_OR_NEWER
			return classType.GetCustomAttribute(attributeType, InheritClassAttributes) != null;
			#else
			return classType.GetCustomAttributes(attributeType, InheritClassAttributes).Length > 0;
			#endif
		}

		[CanBeNull]
		public static TAttribute GetAttribute<TAttribute>([NotNull]MemberInfo member) where TAttribute : Attribute
		{
			object[] attributes;
			if(AttributesByMember.TryGetValue(member, out attributes))
			{
				for(int n = attributes.Length - 1; n >= 0; n--)
				{
					var cast = attributes[n] as TAttribute;
					if(cast != null)
					{
						return cast;
					}
				}
				return null;
			}

			#if CSHARP_7_3_OR_NEWER
			foreach(var attributeType in PluginAttributeConverterProvider.GetAttributeTypeAndEachAlias(typeof(TAttribute)))
			{
				var attribute = member.GetCustomAttribute(attributeType, InheritMemberAttributes);
				if(attribute != null)
				{
					TAttribute result;
					if(PluginAttributeConverterProvider.TryConvert(attribute, out result))
					{
						return result;
					}
				}
			}
			#else
			attributes = member.GetCustomAttributes(typeof(TAttribute), InheritMemberAttributes);
			if(attributes.Length > 0)
			{
				return attributes[0] as TAttribute;
			}
			#endif
			return null;
		}

		[NotNull]
		public static TAttribute[] GetAttributes<TAttribute>([NotNull]Type classType) where TAttribute : Attribute
		{
			List<TAttribute> list = null;
			object[] attributes;
			if(AttributesByClass.TryGetValue(classType, out attributes))
			{
				for(int n = attributes.Length - 1; n >= 0; n--)
				{
					var cast = attributes[n] as TAttribute;
					if(cast != null)
					{
						if(list == null)
						{
							list = new List<TAttribute>(0);
						}
						list.Add(cast);
					}
				}
			}
			else
			{
				#if CSHARP_7_3_OR_NEWER
				foreach(var attributeType in PluginAttributeConverterProvider.GetAttributeTypeAndEachAlias(typeof(TAttribute)))
				{
					var attribute = classType.GetCustomAttribute(attributeType, InheritMemberAttributes);
					if(attribute != null)
					{
						TAttribute cast;
						if(PluginAttributeConverterProvider.TryConvert(attribute, out cast))
						{
							if(list == null)
							{
								list = new List<TAttribute>(0);
							}
							list.Add(cast);
						}
					}
				}
				#else
				attributes = classType.GetCustomAttributes(typeof(TAttribute), InheritClassAttributes);
				PluginAttributeConverterProvider.ConvertAll(ref attributes);
				for(int n = attributes.Length - 1; n >= 0; n--)
				{
					var cast = attributes[n] as TAttribute;
					if(cast != null)
					{
						if(list == null)
						{
							list = new List<TAttribute>(0);
						}
						list.Add(cast);
					}
				}
				#endif
			}
			return list == null ? ArrayPool<TAttribute>.ZeroSizeArray : list.ToArray();
		}

		[NotNull]
		public static TAttribute[] GetAttributes<TAttribute>([NotNull]MemberInfo member) where TAttribute : Attribute
		{
			object[] attributes;
			List<TAttribute> list = null;
			if(AttributesByMember.TryGetValue(member, out attributes))
			{
				for(int n = attributes.Length - 1; n >= 0; n--)
				{
					var cast = attributes[n] as TAttribute;
					if(cast != null)
					{
						if(list == null)
						{
							list = new List<TAttribute>(0);
						}
						list.Add(cast);
					}
				}
				return list == null ? ArrayPool<TAttribute>.ZeroSizeArray : list.ToArray();
			}
			else
			{
				attributes = member.GetCustomAttributes(typeof(TAttribute), InheritMemberAttributes);
				PluginAttributeConverterProvider.ConvertAll(ref attributes);
				for(int n = attributes.Length - 1; n >= 0; n--)
				{
					if(list == null)
					{
						list = new List<TAttribute>(0);
					}
					list.Add(attributes[n] as TAttribute);
				}
			}
			return list == null ? ArrayPool<TAttribute>.ZeroSizeArray : list.ToArray();
		}

		public static void GetAttributes<TAttribute>([NotNull]MemberInfo member, [NotNull]List<TAttribute> addToList) where TAttribute : Attribute
		{
			object[] attributes;
			if(AttributesByMember.TryGetValue(member, out attributes))
			{
				for(int n = attributes.Length - 1; n >= 0; n--)
				{
					var cast = attributes[n] as TAttribute;
					if(cast != null)
					{
						addToList.Add(cast);
					}
				}
			}
			else
			{
				attributes = member.GetCustomAttributes(typeof(TAttribute), InheritMemberAttributes);
				PluginAttributeConverterProvider.ConvertAll(ref attributes);
				for(int n = attributes.Length - 1; n >= 0; n--)
				{
					addToList.Add(attributes[n] as TAttribute);
				}
			}
		}

		public static void GetAttributes<TAttribute>([NotNull]Type classType, [NotNull]List<TAttribute> addToList) where TAttribute : Attribute
		{
			object[] attributes;
			if(AttributesByClass.TryGetValue(classType, out attributes))
			{
				for(int n = attributes.Length - 1; n >= 0; n--)
				{
					var cast = attributes[n] as TAttribute;
					if(cast != null)
					{
						addToList.Add(cast);
					}
				}
			}
			else
			{
				attributes = classType.GetCustomAttributes(typeof(TAttribute), InheritClassAttributes);
				PluginAttributeConverterProvider.ConvertAll(ref attributes);
				for(int n = attributes.Length - 1; n >= 0; n--)
				{
					addToList.Add(attributes[n] as TAttribute);
				}
			}
		}

		public static void GetAttributes([NotNull]MemberInfo member, [NotNull]Type attributeType, [NotNull]List<object> addToList)
		{
			object[] attributes;
			if(AttributesByMember.TryGetValue(member, out attributes))
			{
				for(int n = attributes.Length - 1; n >= 0; n--)
				{
					if(attributeType.IsAssignableFrom(attributes[n].GetType()))
					{
						addToList.Add(attributes[n]);
					}
				}
			}
			else
			{
				foreach(var type in PluginAttributeConverterProvider.GetAttributeTypeAndEachAlias(attributeType))
				{
					attributes = member.GetCustomAttributes(type, InheritMemberAttributes);
					PluginAttributeConverterProvider.ConvertAll(ref attributes);
					addToList.AddRange(attributes);
				}
			}
		}

		public static void GetAttributes(Type classType, [NotNull]Type attributeType, [NotNull]List<object> addToList)
		{
			object[] attributes;
			if(AttributesByClass.TryGetValue(classType, out attributes))
			{
				for(int n = attributes.Length - 1; n >= 0; n--)
				{
					if(attributeType.IsAssignableFrom(attributes[n].GetType()))
					{
						addToList.Add(attributes[n]);
					}
				}
			}
			else
			{
				attributes = classType.GetCustomAttributes(attributeType, InheritClassAttributes);
				PluginAttributeConverterProvider.ConvertAll(ref attributes);
				addToList.AddRange(attributes);
			}
		}

		[NotNull]
		public static object[] GetAttributes([NotNull]MemberInfo member, [NotNull]Type attributeType)
		{
			object[] attributes;
			if(AttributesByMember.TryGetValue(member, out attributes))
			{
				return attributes;
			}

			attributes = member.GetCustomAttributes(attributeType, InheritMemberAttributes);
			PluginAttributeConverterProvider.ConvertAll(ref attributes);
			return attributes;
		}

		public static bool TryGetAttribute<TAttribute>([NotNull]MemberInfo member, out TAttribute attribute) where TAttribute : Attribute
		{
			object[] attributes;
			if(AttributesByMember.TryGetValue(member, out attributes))
			{
				for(int n = attributes.Length - 1; n >= 0; n--)
				{
					attribute = attributes[n] as TAttribute;
					if(attribute != null)
					{
						return true;
					}
				}
			}
			else
			{
				#if CSHARP_7_3_OR_NEWER
				attribute = member.GetCustomAttribute<TAttribute>(InheritMemberAttributes);
				return attribute != null;
				#else
				attributes = member.GetCustomAttributes(typeof(TAttribute), InheritMemberAttributes);
				if(attributes.Length > 0)
				{
					attribute = attributes[0] as TAttribute;
					return true;
				}
				#endif
			}
			attribute = null;
			return false;
		}

		public static bool TryGetImplementingAttribute<TInterface>([NotNull]MemberInfo member, out TInterface attribute) where TInterface : class
		{
			object[] attributes;
			if(AttributesByMember.TryGetValue(member, out attributes))
			{
				for(int n = attributes.Length - 1; n >= 0; n--)
				{
					attribute = attributes[n] as TInterface;
					if(attribute != null)
					{
						return true;
					}
				}
			}
			else
			{
				attributes = member.GetCustomAttributes(InheritMemberAttributes);

				#if THREAD_SAFE
				AttributesByMember[member] = attributes;
				#else
				AttributesByMember.Add(member, attributes);
				#endif

				for(int n = attributes.Length - 1; n >= 0; n--)
				{
					attribute = attributes[n] as TInterface;
					if(attribute != null)
					{
						return true;
					}
				}
			}
			attribute = null;
			return false;
		}

		public static bool TryGetAttribute<TAttribute>([NotNull]Type classType, out TAttribute attribute) where TAttribute : Attribute
		{
			object[] attributes;
			if(AttributesByClass.TryGetValue(classType, out attributes))
			{
				for(int n = attributes.Length - 1; n >= 0; n--)
				{
					attribute = attributes[n] as TAttribute;
					if(attribute != null)
					{
						return true;
					}
				}
			}
			else
			{
				#if CSHARP_7_3_OR_NEWER
				attribute =  classType.GetCustomAttribute<TAttribute>(InheritClassAttributes);
				return attribute != null;
				#else
				attributes = classType.GetCustomAttributes(typeof(TAttribute), InheritClassAttributes);
				if(attributes.Length > 0)
				{
					attribute = attributes[0] as TAttribute;
					return true;
				}
				#endif
			}
			attribute = null;
			return false;
		}

		public static bool TryGetImplementingAttribute<TInterface>([NotNull]Type classType, out TInterface attribute) where TInterface : class
		{
			object[] attributes;
			if(AttributesByClass.TryGetValue(classType, out attributes))
			{
				for(int n = attributes.Length - 1; n >= 0; n--)
				{
					attribute = attributes[n] as TInterface;
					if(attribute != null)
					{
						return true;
					}
				}
			}
			else
			{
				attributes = classType.GetCustomAttributes(InheritClassAttributes);

				#if THREAD_SAFE
				AttributesByClass[classType] = attributes;
				#else
				AttributesByClass.Add(classType, attributes);
				#endif

				for(int n = attributes.Length - 1; n >= 0; n--)
				{
					attribute = attributes[n] as TInterface;
					if(attribute != null)
					{
						return true;
					}
				}
			}
			attribute = null;
			return false;
		}
	}
}