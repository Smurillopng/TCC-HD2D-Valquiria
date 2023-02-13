using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using Sisus.Compatibility;

namespace Sisus
{
	public static class Attribute<TAttribute> where TAttribute : Attribute
	{
		#if !NET_2_0 && !NET_2_0_SUBSET && !NET_STANDARD_2_0
		private static readonly System.Collections.Concurrent.ConcurrentDictionary<MemberInfo, IEnumerable<TAttribute>> cacheSansInherited = new System.Collections.Concurrent.ConcurrentDictionary<MemberInfo, IEnumerable<TAttribute>>(4, 128);
		private static readonly System.Collections.Concurrent.ConcurrentDictionary<MemberInfo, IEnumerable<TAttribute>> cacheIncludingInherited = new System.Collections.Concurrent.ConcurrentDictionary<MemberInfo, IEnumerable<TAttribute>>(4, 128);
		private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, IEnumerable<TAttribute>> cacheForClassesSansInherited = new System.Collections.Concurrent.ConcurrentDictionary<Type, IEnumerable<TAttribute>>(4, 128);
		private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, IEnumerable<TAttribute>> cacheForClassesIncludingInherited = new System.Collections.Concurrent.ConcurrentDictionary<Type, IEnumerable<TAttribute>>(4, 128);
		#endif

		public static bool ExistsOn([NotNull]MemberInfo member, bool inherit = false)
		{
			IEnumerable<TAttribute> attributes;

			#if !NET_2_0 && !NET_2_0_SUBSET && !NET_STANDARD_2_0
			var cache = inherit ? cacheIncludingInherited : cacheSansInherited;
			if(!cache.TryGetValue(member, out attributes))
			#endif
			{
				#if CSHARP_7_3_OR_NEWER
				attributes = member.GetCustomAttributes<TAttribute>(inherit);
				#else
				attributes = member.GetCustomAttributes(typeof(TAttribute), inherit).Cast<TAttribute>();
				#endif

				AddAliases(member, ref attributes);

				#if !NET_2_0 && !NET_2_0_SUBSET && !NET_STANDARD_2_0
				cache[member] = attributes;
				#endif
			}

			return attributes.Any();
		}

		public static bool ExistsOn([NotNull]Type classType, bool inherit = false)
		{
			IEnumerable<TAttribute> attributes;

			#if !NET_2_0 && !NET_2_0_SUBSET && !NET_STANDARD_2_0
			var cache = inherit ? cacheForClassesIncludingInherited : cacheForClassesSansInherited;
			if(!cache.TryGetValue(classType, out attributes))
			#endif
			{
				#if CSHARP_7_3_OR_NEWER
				attributes = classType.GetCustomAttributes<TAttribute>(inherit);
				#else
				attributes = classType.GetCustomAttributes(typeof(TAttribute), inherit).Cast<TAttribute>();
				#endif

				AddAliases(classType, ref attributes);

				#if !NET_2_0 && !NET_2_0_SUBSET && !NET_STANDARD_2_0
				cache[classType] = attributes;
				#endif
			}

			if(attributes.Any())
			{
				return true;
			}

			return false;
		}

		[CanBeNull]
		public static TAttribute Get([NotNull]MemberInfo member, bool inherit = false)
		{
			IEnumerable<TAttribute> attributes;

			#if !NET_2_0 && !NET_2_0_SUBSET && !NET_STANDARD_2_0
			var cache = inherit ? cacheIncludingInherited : cacheSansInherited;
			if(!cache.TryGetValue(member, out attributes))
			#endif
			{
				#if CSHARP_7_3_OR_NEWER
				attributes = member.GetCustomAttributes<TAttribute>(inherit);
				#else
				attributes = member.GetCustomAttributes(typeof(TAttribute), inherit).Cast<TAttribute>();
				#endif

				AddAliases(member, ref attributes);

				#if !NET_2_0 && !NET_2_0_SUBSET && !NET_STANDARD_2_0
				cache[member] = attributes;
				#endif
			}

			return attributes.FirstOrDefault();
		}

		public static bool TryGet([NotNull]MemberInfo member, bool inherit, [CanBeNull]out TAttribute result)
		{
			IEnumerable<TAttribute> attributes;

			#if !NET_2_0 && !NET_2_0_SUBSET && !NET_STANDARD_2_0
			var cache = inherit ? cacheIncludingInherited : cacheSansInherited;
			if(!cache.TryGetValue(member, out attributes))
			#endif
			{
				#if CSHARP_7_3_OR_NEWER
				attributes = member.GetCustomAttributes<TAttribute>(inherit);
				#else
				attributes = member.GetCustomAttributes(typeof(TAttribute), inherit).Cast<TAttribute>();
				#endif

				AddAliases(member, ref attributes);

				#if !NET_2_0 && !NET_2_0_SUBSET && !NET_STANDARD_2_0
				cache[member] = attributes;
				#endif
			}

			result = attributes.FirstOrDefault();
			return result != null;
		}

		public static bool TryGetAll([NotNull]MemberInfo member, bool inherit, [NotNull]out IEnumerable<TAttribute> result)
		{
			IEnumerable<TAttribute> attributes;

			#if !NET_2_0 && !NET_2_0_SUBSET && !NET_STANDARD_2_0
			var cache = inherit ? cacheIncludingInherited : cacheSansInherited;
			if(!cache.TryGetValue(member, out attributes))
			#endif
			{
				#if CSHARP_7_3_OR_NEWER
				attributes = member.GetCustomAttributes<TAttribute>(inherit);
				#else
				attributes = member.GetCustomAttributes(typeof(TAttribute), inherit).Cast<TAttribute>();
				#endif

				AddAliases(member, ref attributes);

				#if !NET_2_0 && !NET_2_0_SUBSET && !NET_STANDARD_2_0
				cache[member] = attributes;
				#endif
			}

			result = attributes;
			return result.Any();
		}

		public static bool TryGetAll([NotNull]Type classType, bool inherit, [NotNull]out IEnumerable<TAttribute> result)
		{
			IEnumerable<TAttribute> attributes;

			#if !NET_2_0 && !NET_2_0_SUBSET && !NET_STANDARD_2_0
			var cache = inherit ? cacheForClassesIncludingInherited : cacheForClassesSansInherited;
			if(!cache.TryGetValue(classType, out attributes))
			#endif
			{
				//#if CSHARP_7_3_OR_NEWER
				//attributes = classType.GetCustomAttributes<TAttribute>(inherit); // UPDATE: This doesn't seem to be thread safe!
				//#else 
				attributes = classType.GetCustomAttributes(typeof(TAttribute), inherit).Cast<TAttribute>();
				//#endif

				AddAliases(classType, ref attributes);

				#if !NET_2_0 && !NET_2_0_SUBSET && !NET_STANDARD_2_0
				cache[classType] = attributes;
				#endif
			}

			result = attributes;
			return result.Any();
		}

		[NotNull]
		public static IEnumerable<TAttribute> GetAll([NotNull]MemberInfo member, bool inherit = false)
		{
			IEnumerable<TAttribute> attributes;

			#if !NET_2_0 && !NET_2_0_SUBSET && !NET_STANDARD_2_0
			var cache = inherit ? cacheIncludingInherited : cacheSansInherited;
			if(!cache.TryGetValue(member, out attributes))
			#endif
			{
				#if CSHARP_7_3_OR_NEWER
				attributes = member.GetCustomAttributes<TAttribute>(inherit);
				#else
				attributes = member.GetCustomAttributes(typeof(TAttribute), inherit).Cast<TAttribute>();
				#endif

				AddAliases(member, ref attributes);

				#if !NET_2_0 && !NET_2_0_SUBSET && !NET_STANDARD_2_0
				cache[member] = attributes;
				#endif
			}

			return attributes;
		}

		private static void AddAliases([NotNull]MemberInfo member, [NotNull]ref IEnumerable<TAttribute> attributes)
		{
			var aliasAttributeTypes = PluginAttributeConverterProvider.GetAliases(typeof(TAttribute));
			for(int n = aliasAttributeTypes.Length - 1; n >= 0; n--)
			{
				var aliasAttributeInstances = member.GetCustomAttributes(aliasAttributeTypes[n], false);
				for(int a = aliasAttributeInstances.Length - 1; a >= 0; a--)
				{
					TAttribute converted;
					if(PluginAttributeConverterProvider.TryConvert(aliasAttributeInstances[a], out converted))
					{
						#if !NET_2_0 && !NET_2_0_SUBSET && !NET_STANDARD_2_0
						attributes = attributes.Append(converted);
						#else
						attributes = Append(attributes, converted);
						#endif
					}
				}
			}
		}

		private static void AddAliases([NotNull]Type classType, [NotNull]ref IEnumerable<TAttribute> attributes)
		{
			var aliasAttributeTypes = PluginAttributeConverterProvider.GetAliases(typeof(TAttribute));
			for(int n = aliasAttributeTypes.Length - 1; n >= 0; n--)
			{
				var aliasAttributeInstances = classType.GetCustomAttributes(aliasAttributeTypes[n], false);
				for(int a = aliasAttributeInstances.Length - 1; a >= 0; a--)
				{
					TAttribute converted;
					if(PluginAttributeConverterProvider.TryConvert(aliasAttributeInstances[a], out converted))
					{
						#if !NET_2_0 && !NET_2_0_SUBSET && !NET_STANDARD_2_0
						attributes = attributes.Append(converted);
						#else
						attributes = Append(attributes, converted);
						#endif
					}
				}
			}
		}

		#if NET_2_0 || NET_2_0_SUBSET || NET_STANDARD_2_0
		private static IEnumerable<TAttribute> Append(IEnumerable<TAttribute> ienumerable, TAttribute attribute)
		{
			foreach(var existing in ienumerable)
			{
				yield return existing;
			}
			yield return attribute;
		}
		#endif
	}
}