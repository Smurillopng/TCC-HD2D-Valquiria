#define CACHE_PARAMETER_VALUES

#define DEBUG_ENABLED

using JetBrains.Annotations;
using System.Reflection;

namespace Sisus
{
	/// <summary>
	/// Handles returning values for ParameterInfos and  caching their values so that they can persist through a single session.
	/// Currently cached values won't persist through assembly reloads.
	/// </summary>
	public static class ParameterValues
	{
		private static readonly System.Collections.Generic.Dictionary<int, object> CachedParameterValues = new System.Collections.Generic.Dictionary<int, object>();

		public static object GetValue([NotNull]ParameterInfo parameterInfo)
		{
			object cachedValue;
			if(CachedParameterValues.TryGetValue(parameterInfo.MetadataToken, out cachedValue))
			{
				#if DEV_MODE && DEBUG_ENABLED
				UnityEngine.Debug.Log("Returning cached value for "+parameterInfo+" ("+ parameterInfo.MetadataToken + "): "+StringUtils.ToString(cachedValue));
				#endif

				#if DEV_MODE
				UnityEngine.Debug.Assert(cachedValue == null || parameterInfo.ParameterType == cachedValue.GetType());
				#endif

				return cachedValue;
			}

			return parameterInfo.DefaultValue();
		}

		public static void CacheValue([NotNull]ParameterInfo parameterInfo, object value)
		{
			#if DEV_MODE && DEBUG_ENABLED
			UnityEngine.Debug.Log("Caching value for "+parameterInfo+" ("+ parameterInfo.MetadataToken + "): "+StringUtils.ToString(value));
			#endif

			CachedParameterValues[parameterInfo.MetadataToken] = value;
		}
	}
}