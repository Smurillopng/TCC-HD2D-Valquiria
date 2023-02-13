#define USE_IL_FOR_GET_AND_SET

using System;
using System.Reflection;
using UnityEngine;

#if CSHARP_7_3_OR_NEWER && USE_IL_FOR_GET_AND_SET
using Sisus.Vexe.FastReflection;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Sisus
{
	public sealed class MethodData : MemberData
	{
		private MethodInfo getMethodInfo;
		private MethodInfo setMethodInfo;

		#if CSHARP_7_3_OR_NEWER && USE_IL_FOR_GET_AND_SET
		private MethodCaller<object,object> getMethod;
		private MethodCaller<object,object> setMethod;
		#endif

		private bool getHasReturnValue;
		private bool getHasNoParameters;
		private bool isGeneric;
		private bool setHasSingleParameter;

		#if UNITY_EDITOR
		public override SerializedProperty SerializedProperty
		{
			get
			{
				return null;
			}

			set
			{
				throw new NotSupportedException();
			}
		}
		#endif

		public override MemberTypes MemberType
		{
			get { return MemberTypes.Method; }
		}

		public override LinkedMemberType LinkedMemberType
		{
			get
			{
				return LinkedMemberType.Method;
			}
		}

		public override string Name
		{
			get
			{
				return MemberInfo.Name;
			}
		}

		public override bool IsStatic
		{
			get
			{
				return getMethodInfo != null ? getMethodInfo.IsStatic : setMethodInfo.IsStatic;
			}
		}

		public override bool Equals(MemberData other)
		{
			var b = other as MethodData;
			if(b == null)
			{
				return false;
			}

			if(getMethodInfo == null)
			{
				if(b.getMethodInfo != null)
				{
					return false;
				}
			}
			else
			{
				return b.getMethodInfo != null && getMethodInfo.EqualTo(b.getMethodInfo);
			}
				
			return b.setMethodInfo != null && setMethodInfo.EqualTo(b.setMethodInfo);
		}

		public override MemberInfo MemberInfo
		{
			get { return getMethodInfo; }
		}

		public override MemberInfo SecondMemberInfo
		{
			get { return setMethodInfo; }
		}

		public override Type Type
		{
			get
			{
				if(getMethodInfo != null)
				{
					return getMethodInfo.ReturnType;
				}
				return setMethodInfo.ReturnType;
					
			}
		}

		public override bool CanRead
		{
			get
			{
				return getHasReturnValue && !isGeneric && getHasNoParameters;
			}
		}

		public override bool CanReadWithoutSideEffects
		{
			get
			{
				return false;
			}
		}

		public override bool CanWrite
		{
			get
			{
				return false;
			}
		}

		public void Setup(MethodInfo inGetMethodInfo, MethodInfo inSetMethodInfo)
		{
			getMethodInfo = inGetMethodInfo;
			setMethodInfo = inSetMethodInfo;

			getHasReturnValue = getMethodInfo.ReturnType != Types.Void;
			getHasNoParameters = getMethodInfo.GetParameters().Length == 0;

			isGeneric = getMethodInfo.IsGenericMethod;
			#if CSHARP_7_3_OR_NEWER && USE_IL_FOR_GET_AND_SET
			if(isGeneric)
			{
				#if DEV_MODE
				Debug.LogWarning("MethodData("+getMethodInfo.Name+") - IL not currently supported for generic methods!");
				#endif
			}
			else
			{
				getMethod = getMethodInfo.DelegateForCall();
			}
			#endif
			
			if(setMethodInfo != null)
			{
				setHasSingleParameter = setMethodInfo.GetParameters().Length == 1;

				if(!setHasSingleParameter)
				{
					#if DEV_MODE
					Debug.LogWarning("setMethodInfo.GetParameters().Length != 1 - setMethodInfo should probably be null");
					#endif
				}

				#if CSHARP_7_3_OR_NEWER && USE_IL_FOR_GET_AND_SET
				if(setMethodInfo.IsGenericMethod)
				{
					#if DEV_MODE
					Debug.LogWarning("MethodData("+setMethodInfo.Name+") - IL not currently supported for generic methods!");
					#endif
				}
				else
				{
					setMethod = setMethodInfo.DelegateForCall();
				}
				#endif
			}
			else
			{
				setHasSingleParameter = false;
			}

		}

		public override void GetValue(object fieldOwner, out object result)
		{
			#if CSHARP_7_3_OR_NEWER && USE_IL_FOR_GET_AND_SET
			if(getMethod != null)
			{
				try
				{
					result = getMethod(fieldOwner, null);
				}
				#if DEV_MODE
				catch(Exception e)
				{
					Debug.LogError(GetType().Name + ".GetValue("+StringUtils.ToString(fieldOwner)+") - " + e + ", MemberName="+(MemberInfo != null ? MemberInfo.Name : "null")+", getMethodInfo.ParamCount="+(getMethodInfo != null ? getMethodInfo.GetParameters().Length.ToString() : "null")+", isGeneric="+isGeneric+", Type="+Type.Name+", getMethodInfo.ReturnType="+(getMethodInfo != null ? getMethodInfo.ReturnType.Name : "null"));
				#else
				catch
				{
				#endif
					result = Type.DefaultValue();
				}
				return;
			}
			#endif

			try
			{
				result = getMethodInfo.Invoke(fieldOwner, null);
			}
			catch(Exception e)
			{
				Debug.LogError(ToString() + ".GetValue(owner=" + StringUtils.ToString(fieldOwner) + "), ownerType=" + StringUtils.TypeToString(fieldOwner)+" "+e);
				result = DefaultValue();
			}
		}
			
		public override void SetValue(ref object fieldOwner, object value)
		{
			try
			{
				#if CSHARP_7_3_OR_NEWER && USE_IL_FOR_GET_AND_SET
				setMethod.InvokeWithParameter(fieldOwner, value);
				#else
				setMethodInfo.InvokeWithParameter(fieldOwner, value);
				#endif
			}
			catch(Exception e)
			{
				Debug.LogError(GetType().Name + ".SetValue("+ StringUtils.ToString(fieldOwner) + ", "+StringUtils.ToString(value)+") - " + e + ", MemberName="+MemberInfo.Name+", ParamCount="+setMethodInfo.GetParameters().Length+", isGeneric="+isGeneric+", Type="+Type.Name+", setMethodInfo.ReturnType="+setMethodInfo.ReturnType);
			}
		}
			
		public override object[] GetAttributes(bool inherit = true)
		{
			var result = MemberInfo.GetCustomAttributes(inherit);
			Compatibility.PluginAttributeConverterProvider.ConvertAll(ref result);
			return result;
		}

		/// <inheritdoc/>
		public override void Dispose()
		{
			#if CSHARP_7_3_OR_NEWER && USE_IL_FOR_GET_AND_SET
			getMethod = null;
			setMethod = null;
			#endif

			LinkedMemberInfoPool.Dispose(this);
		}
	}
}