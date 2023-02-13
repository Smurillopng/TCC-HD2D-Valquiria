#define DEBUG_METHOD_MATCHES_DELEGATE

using System;
using System.Reflection;

namespace Sisus
{
	public static class DelegateUtility
	{
		/// <summary> Given a delegate type returns MethodInfo for it. </summary>
		/// <param name="delegateType"> Type of the delegate. </param>
		/// <param name="delegateReturnType"> Return type of the delegate. </param>
		/// <param name="delegateParameters"> Parameters of the delegate. </param>
		/// <returns> MethodInfo for delegate. </returns>
		public static void GetDelegateInfo(Type delegateType, out Type delegateReturnType, out ParameterInfo[] delegateParameters)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			UnityEngine.Debug.Assert(delegateType.IsSubclassOf(typeof(MulticastDelegate)), "GetMethodInfoForDelegateType - type was not a sub-class of MulticastDelegate." );
			#endif

			var invokeMethod = GetMethodInfoForDelegateType(delegateType);
			delegateReturnType = invokeMethod.ReturnType;
			delegateParameters = invokeMethod.GetParameters();
		}

		/// <summary> Given a delegate type returns MethodInfo for it. </summary>
		/// <param name="delegateType"> Type of the delegate. </param>
		/// <returns> MethodInfo for delegate. </returns>
		public static MethodInfo GetMethodInfoForDelegateType(Type delegateType)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			UnityEngine.Debug.Assert(delegateType.IsSubclassOf(typeof(MulticastDelegate)), "GetMethodInfoForDelegateType - type was not a sub-class of MulticastDelegate." );
			#endif

			return delegateType.GetMethod("Invoke");
		}

		public static bool MethodSignatureMatchesDelegate(this MethodInfo method, Type delegateReturnType, ParameterInfo[] delegateParameters)
		{
			if(method.IsGenericMethod)
			{
				return false;
			}

			var methodReturnType = method.ReturnType;

			if(delegateReturnType == Types.Void)
			{
				if(methodReturnType != Types.Void)
				{
					return false;
				}
			}
			else if(methodReturnType == Types.Void)
			{
				return false;
			}
			else if(!delegateReturnType.IsAssignableFrom(methodReturnType))
			{
				return false;
			}

			var methodParameters = method.GetParameters();
			int count = methodParameters.Length;
			if(count != delegateParameters.Length)
			{
				return false;
			}
			
			for(int n = count - 1; n >= 0; n--)
			{
				var delegateParameter = delegateParameters[n];
				var delegateParameterType = delegateParameter.ParameterType;
				var methodParameter = methodParameters[n];
				var methodParameterType = methodParameter.ParameterType;
				if(!delegateParameterType.IsAssignableFrom(methodParameterType))
				{
					return false;
				}

				// If is ref or out
				if(delegateParameterType.IsByRef)
				{
					if(!methodParameterType.IsByRef)
					{
						return false;
					}

					if(delegateParameter.IsOut != methodParameter.IsOut)
					{
						return false;
					}
				}
				else if(methodParameterType.IsByRef)
				{
					return false;
				}
			}

			#if DEV_MODE && DEBUG_METHOD_MATCHES_DELEGATE
			UnityEngine.Debug.Log("MethodSignatureMatchesDelegate("+method.Name+"): "+StringUtils.True+"\ndelegateParameters="+StringUtils.ToString(delegateParameters)+"\nmethodParameters="+StringUtils.ToString(methodParameters));
			#endif

			return true;
		}
	}
}