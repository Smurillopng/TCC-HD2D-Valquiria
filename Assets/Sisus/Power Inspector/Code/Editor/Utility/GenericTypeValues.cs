#define DEBUG_ENABLED

using System;

namespace Sisus
{
	/// <summary>
	/// Handles returning values for generic type controls and caching them so that they can persist through a single session.
	/// Currently cached values won't persist through assembly reloads.
	/// </summary>
	public static class GenericArgumentValues
	{
		private static readonly System.Collections.Generic.Dictionary<Type, Type[]> CachedValues = new System.Collections.Generic.Dictionary<Type, Type[]>();

		public static Type GetValue(Type genericTypeDefinition, int argumentIndex)
		{
			Type[] cachedArguments;
			if(CachedValues.TryGetValue(genericTypeDefinition, out cachedArguments))
			{
				#if DEV_MODE && DEBUG_ENABLED
				UnityEngine.Debug.Log("Returning cached value for generic type "+ genericTypeDefinition + " argument #"+argumentIndex+": "+StringUtils.ToString(cachedArguments[argumentIndex]));
				#endif

				return cachedArguments[argumentIndex];
			}
			return null;
		}

		public static void CacheValue(Type genericTypeDefinition, int argumentIndex, Type value)
		{
			#if DEV_MODE && DEBUG_ENABLED
			UnityEngine.Debug.Log("Caching value for generic type "+ genericTypeDefinition + " argument #"+argumentIndex+": "+StringUtils.ToString(value));
			#endif

			Type[] cachedArguments;
			if(!CachedValues.TryGetValue(genericTypeDefinition, out cachedArguments))
			{
				cachedArguments = new Type[genericTypeDefinition.GetGenericArguments().Length];
				CachedValues[genericTypeDefinition] = cachedArguments;
			}
			cachedArguments[argumentIndex] = value;
		}
	}
}