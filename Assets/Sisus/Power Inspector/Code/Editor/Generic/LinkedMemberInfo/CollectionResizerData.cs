using System;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Sisus
{
	public sealed class CollectionResizerData : MemberData
	{
		private const int CollectionIndexValue = -2;

		private GetSize getSize;
		private SetSize setSize;
		private Type type;
		
		#if UNITY_EDITOR
		private SerializedProperty serializedProperty;
		#endif

		#if UNITY_EDITOR
		public override SerializedProperty SerializedProperty
		{
			get
			{
				return serializedProperty;
			}
				
			set
			{
				serializedProperty = value;
			}
		}
		#endif

		public override MulticastDelegate GetDelegate
		{
			get
			{
				return getSize;
			}
		}

		public override MulticastDelegate SetDelegate
		{
			get
			{
				return setSize;
			}
		}

		public override int CollectionIndex
		{
			get
			{
				return CollectionIndexValue;
			}
		}

		public override MemberTypes MemberType
		{
			get { return MemberTypes.Method; }
		}

		public override LinkedMemberType LinkedMemberType
		{
			get
			{
				return LinkedMemberType.CollectionResizer;
			}
		}

		public override string Name
		{
			get
			{
				return getSize != null ? StringUtils.ToString(getSize) : setSize != null ? StringUtils.ToString(setSize) : "";
			}
		}

		/// <inheritdoc />
		public override bool IsStatic
		{
			get
			{
				// Even if getSize / setSize actually refer to static methods,
				// we want want to treat the CollectionResizerData as non-static,
				// so that GetValue is called with fieldOwner value containing
				// the value of the collection.
				return false;
			}
		}

		/// <inheritdoc />
		public override bool HasOwner
		{
			get
			{
				// Even if getSize / setSize actually refer to static methods,
				// GetValue is called with fieldOwner value containing
				// the value of the collection.
				return true;
			}
		}

		/// <inheritdoc />
		public override bool OwnerCanBeNull
		{
			get
			{
				// Unlike with instance methods where owner represents instance containing the method,
				// here the instance represents the class, and null values are supported (will just return -1).
				return true;
			}
		}

		public override bool Equals(MemberData other)
		{
			return false;
		}

		public override MemberInfo MemberInfo
		{
			get
			{
				return getSize != null ? getSize.Method : null;
			}
		}

		public override MemberInfo SecondMemberInfo
		{
			get
			{
				return setSize != null ? setSize.Method : null;
			}
		}

		public override Type Type
		{
			get
			{
				return type;
			}
		}

		public override bool CanRead
		{
			get
			{
				return getSize != null;
			}
		}

		public override bool CanReadWithoutSideEffects
		{
			get
			{
				return getSize != null; //For now will assume no side effects. Could allow specifying this via parameter later if needed
			}
		}

		public override bool CanWrite
		{
			get
			{
				return setSize != null;
			}
		}

		public void Setup([NotNull]Type inType, [NotNull]GetSize inGetSize, SetSize inSetSize)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(inType != null);
			Debug.Assert(inGetSize != null);
			#endif

			type = inType;
			getSize = inGetSize;
			setSize = inSetSize;
		}
			
		public override void GetValue(object fieldOwner, out object result)
		{
			try
			{
				result = getSize(fieldOwner);
			}
			catch(Exception e)
			{
				Debug.LogError(GetType().Name + ".GetValue(" + StringUtils.ToString(fieldOwner) + ") with getSize="+StringUtils.ToString(getSize)+"\n"+e);
				result = Type.DefaultValue();
			}
		}
			
		public override void SetValue(ref object fieldOwner, object value)
		{
			try
			{
				setSize(ref fieldOwner, value);
			}
			catch(Exception e)
			{
				Debug.LogError(GetType().Name + ".SetValue("+ StringUtils.ToString(fieldOwner) + ", "+StringUtils.ToString(value)+") with setSize="+StringUtils.ToString(getSize)+"\n" + e);
			}
		}
			
		public override object[] GetAttributes(bool inherit = true)
		{
			return ArrayPool<object>.ZeroSizeArray;
		}

		#if UNITY_EDITOR
		public override SerializedProperty TryBuildSerializedProperty(SerializedObject serializedObject, SerializedProperty parentProperty)
		{
			return parentProperty == null ? null : parentProperty.FindPropertyRelative("Array.size");
		}
		#endif

		/// <inheritdoc/>
		public override void Dispose()
		{
			#if UNITY_EDITOR
			serializedProperty = null;
			#endif
			LinkedMemberInfoPool.Dispose(this);
		}
	}
}