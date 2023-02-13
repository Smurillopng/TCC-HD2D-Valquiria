using System;
using System.Reflection;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Sisus
{
	public sealed class ParameterData : MemberData
	{
		private ParameterInfo parameterInfo;

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

		public override string Name
		{
			get
			{
				return parameterInfo.Name;
			}
		}

		public override MemberTypes MemberType
		{
			get { return MemberTypes.Custom; }
		}

		public override LinkedMemberType LinkedMemberType
		{
			get
			{
				return LinkedMemberType.Parameter;
			}
		}

		public override MemberInfo MemberInfo
		{
			get { return null; }
		}

		public override MemberInfo SecondMemberInfo
		{
			get { return null; }
		}

		public override ICustomAttributeProvider AttributeProvider
		{
			get { return parameterInfo; }
		}

		public override bool IsStatic
		{
			get
			{
				//new test: true, because parent values are not actually needed for fetching value?
				return true;
			}
		}

		public override Type Type
		{
			get
			{
				return parameterInfo.ParameterType;
			}
		}

		public override bool CanRead
		{
			get
			{
				return true;
			}
		}

		public override bool CanReadWithoutSideEffects
		{
			get
			{
				return true;
			}
		}

		public override bool CanWrite
		{
			get
			{
				return true;
			}
		}

		public void Setup(ParameterInfo setParameterInfo)
		{
			parameterInfo = setParameterInfo;
		}

		public override bool Equals(MemberData other)
		{
			var b = other as ParameterData;
			return b != null && parameterInfo.EqualTo(b.parameterInfo);
		}

		public override void GetValue(object fieldOwner, out object result)
		{
			result = ParameterValues.GetValue(parameterInfo);
		}
			
		public override void SetValue(ref object fieldOwner, object value)
		{
			ParameterValues.CacheValue(parameterInfo, value);
		}
			
		public override object[] GetAttributes(bool inherit = true)
		{
			return parameterInfo.GetCustomAttributes(inherit);
		}

		public override object DefaultValue()
		{
			var result = parameterInfo.DefaultValue;

			//if parameter is optional (!DBNull)
			//and an optional parameter is supplied (!Missing)
			if(result != DBNull.Value && result != Type.Missing)
			{
				return result;
			}

			//otherwise use Default value derived from type
			return Type.DefaultValue();
		}

		/// <inheritdoc/>
		public override void Dispose()
		{
			LinkedMemberInfoPool.Dispose(this);
		}
	}
}