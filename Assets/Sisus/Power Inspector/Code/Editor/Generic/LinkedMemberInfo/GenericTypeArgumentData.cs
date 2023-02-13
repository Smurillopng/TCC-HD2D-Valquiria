using System;
using System.Reflection;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Sisus
{
	public sealed class GenericTypeArgumentData : MemberData
	{
		private Type genericTypeArgument;
		private int argumentIndex;

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
				return genericTypeArgument.Name;
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

		public override int CollectionIndex
		{
			get
			{
				return argumentIndex;
			}
		}

		public override ICustomAttributeProvider AttributeProvider
		{
			get { return genericTypeArgument; }
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
				return Types.Type;
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

		public void Setup(Type setGenericTypeArgument, int setArgumentIndex)
		{
			genericTypeArgument = setGenericTypeArgument;
			argumentIndex = setArgumentIndex;
		}

		public override bool Equals(MemberData other)
		{
			var b = other as GenericTypeArgumentData;
			return b != null && genericTypeArgument.EqualTo(b.genericTypeArgument);
		}

		public override void GetValue(object fieldOwner, out object result)
		{
			result = GenericArgumentValues.GetValue(genericTypeArgument, argumentIndex);
		}
		
		public override void SetValue(ref object fieldOwner, object value)
		{
			GenericArgumentValues.CacheValue(genericTypeArgument, argumentIndex, (Type)value);
		}

		public override object[] GetAttributes(bool inherit = true)
		{
			return genericTypeArgument.GetCustomAttributes(inherit);
		}

		public override object DefaultValue()
		{
			return null;
		}

		/// <inheritdoc/>
		public override void Dispose()
		{
			LinkedMemberInfoPool.Dispose(this);
		}
	}
}