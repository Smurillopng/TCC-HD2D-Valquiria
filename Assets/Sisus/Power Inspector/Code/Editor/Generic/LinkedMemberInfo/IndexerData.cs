// Warning CS1058: A previous catch clause already catches all exceptions. All non-exceptions thrown will be wrapped in a `System.Runtime.CompilerServices.RuntimeWrappedException'
// In practice not all exceptions are caught.
#pragma warning disable 1058

using System;
using System.Reflection;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Sisus
{
	public sealed class IndexerData : MemberData
	{
		private PropertyInfo propertyInfo;

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

		public override object[] IndexParameters
		{
			get
			{
				var parameters = propertyInfo.GetIndexParameters();
				int count = parameters.Length;
				var indexParameters = ArrayPool<object>.Create(count);
				for(int n = 0; n < count; n++)
				{
					indexParameters[n] = ParameterValues.GetValue(parameters[n]);
				}

				return indexParameters;
			}
				
			set
			{
				var parameters = propertyInfo.GetIndexParameters();
					
				#if DEV_MODE
				Debug.Assert(value.Length == parameters.Length);
				#endif

				int count = Mathf.Min(value.Length, parameters.Length);
				for(int n = 0; n < count; n++)
				{
					ParameterValues.CacheValue(parameters[n], value[n]);
				}
			}
		}

		public override MemberTypes MemberType
		{
			get { return MemberTypes.Property; }
		}

		public override LinkedMemberType LinkedMemberType
		{
			get
			{
				return LinkedMemberType.Indexer;
			}
		}
			
		public override string Name
		{
			get
			{
				if(propertyInfo == null)
				{
					#if DEV_MODE
					Debug.LogWarning("IndexerData.Name was called with fieldInfo null. This can happen when ToString is called during Setup phase.");
					#endif
					return "";
				}

				return propertyInfo.Name;
			}
		}

		public override bool IsStatic
		{
			get
			{
				if(propertyInfo.CanRead)
				{
					if(propertyInfo.GetGetMethod() is MethodInfo getMethod)
					{
						return getMethod.IsStatic;
					}
				}

				if(propertyInfo.GetSetMethod() is MethodInfo setMethod)
				{
					return setMethod.IsStatic;
				}

				return false;
			}
		}

		public override MemberInfo MemberInfo
		{
			get { return propertyInfo; }
		}

		public override MemberInfo SecondMemberInfo
		{
			get { return null; }
		}

		public override Type Type
		{
			get
			{
				return propertyInfo.PropertyType;
			}
		}

		public override bool CanRead
		{
			get
			{
				return propertyInfo.CanRead;
			}
		}

		public override bool CanReadWithoutSideEffects
		{
			get
			{
				return false; //should we trust that an indexer doesn't have side effects?
			}
		}

		public override bool CanWrite
		{
			get
			{
				return propertyInfo.CanWrite;
			}
		}
			
		public void Setup(PropertyInfo inPropertyInfo)
		{
			propertyInfo = inPropertyInfo;
		}

		public override bool Equals(MemberData other)
		{
			var b = other as IndexerData;
			if(b == null)
			{
				return false;
			}

			return b.propertyInfo != null && propertyInfo.EqualTo(b.propertyInfo);
		}
			
		public override void GetValue(object fieldOwner, out object result)
		{
			try
			{
				result = propertyInfo.GetGetMethod(true).Invoke(fieldOwner, IndexParameters);
			}
			catch(Exception e)
			{
				Debug.LogError(ToString() + ".GetValue(" + StringUtils.ToStringCompact(fieldOwner) + ") with indexParameters=" + StringUtils.ToString(IndexParameters) + " " + e);
				result = Type.DefaultValue();
			}
		}
			
		public override void SetValue(ref object fieldOwner, object value)
		{
			try
			{
				propertyInfo.SetValue(fieldOwner, value, IndexParameters);
			}
			catch(Exception e)
			{
				Debug.LogError(ToString() + ".SetValue("+ StringUtils.ToStringCompact(fieldOwner) + ", "+StringUtils.ToStringCompact(value)+ ") with indexParameters="+StringUtils.ToString(IndexParameters) + " " + e);
			}
		}
			
		public override object[] GetAttributes(bool inherit = true)
		{
			var result = propertyInfo.GetCustomAttributes(inherit);
			Compatibility.PluginAttributeConverterProvider.ConvertAll(ref result);
			return result;
		}

		/// <inheritdoc/>
		public override void Dispose()
		{
			LinkedMemberInfoPool.Dispose(this);
		}
	}
}