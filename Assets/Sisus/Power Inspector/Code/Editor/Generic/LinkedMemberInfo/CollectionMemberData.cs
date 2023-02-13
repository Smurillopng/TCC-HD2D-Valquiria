using System;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Sisus
{
	/// <summary> (Serializable) Represents a member of a collection (list or array). This class cannot be inherited. </summary>
	public sealed class CollectionMemberData : MemberData
	{
		private int collectionIndex;

		private Type type;

		private GetCollectionMember get;
		private SetCollectionMember set;
		
		#if UNITY_EDITOR
		private SerializedProperty serializedProperty;
		#endif

		#if UNITY_EDITOR
		/// <inheritdoc />
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

		/// <inheritdoc />
		public override MulticastDelegate GetDelegate
		{
			get
			{
				return get;
			}
		}

		/// <inheritdoc />
		public override MulticastDelegate SetDelegate
		{
			get
			{
				return set;
			}
		}

		/// <inheritdoc />
		public override MemberTypes MemberType
		{
			get { return MemberTypes.Method; }
		}

		/// <inheritdoc />
		public override LinkedMemberType LinkedMemberType
		{
			get
			{
				return LinkedMemberType.CollectionMember;
			}
		}

		/// <inheritdoc />
		public override string Name
		{
			get
			{
				return "CollectionMember["+collectionIndex+"]";
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

		/// <inheritdoc />
		public override MemberInfo MemberInfo
		{
			get { return null; }
		}

		/// <inheritdoc />
		public override MemberInfo SecondMemberInfo
		{
			get { return null; }
		}

		/// <inheritdoc />
		public override Type Type
		{
			get
			{
				return type;
			}
		}

		/// <inheritdoc />
		public override bool CanRead
		{
			get
			{
				return get != null;
			}
		}

		/// <inheritdoc />
		public override bool CanReadWithoutSideEffects
		{
			get
			{
				return get != null;
			}
		}

		/// <inheritdoc />
		public override bool CanWrite
		{
			get
			{
				return set != null;
			}
		}

		/// <inheritdoc />
		public override int CollectionIndex
		{
			get
			{
				return collectionIndex;
			}
		}

		/// <inheritdoc />
		public override object[] IndexParameters
		{
			get
			{
				return ArrayPool<object>.CreateWithContent(collectionIndex);
			}
		}

		public void Setup(int inCollectionIndex, [NotNull]Type inType, GetCollectionMember inGetDelegate, SetCollectionMember inSetDelegate)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(inType != null);
			#endif

			collectionIndex = inCollectionIndex;
			type = inType;
			get = inGetDelegate;
			set = inSetDelegate;

			#if DEV_MODE && DEBUG_SETUP_COLLECTION_MEMBER
			Debug.Log("CollectionMemberData.Setup(type="+inType.Name+", get="+StringUtils.ToString(get)+", set="+StringUtils.ToString(set)+")");
			#endif
		}

		#if UNITY_EDITOR
		public void Setup(int inCollectionIndex, [NotNull]Type inType, GetCollectionMember inGetDelegate, SetCollectionMember inSetDelegate, SerializedProperty inSerializedProperty)
		{
			serializedProperty = inSerializedProperty;
			Setup(inCollectionIndex, inType, inGetDelegate, inSetDelegate);
		}
		#endif

		/// <inheritdoc />
		public override bool Equals(MemberData other)
		{
			var b = other as CollectionMemberData;
			if(b == null)
			{
				return false;
			}

			if(get == null)
			{
				if(b.get != null)
				{
					return false;
				}
			}
			else
			{
				return get.Equals(b.get);
			}
			return true;
		}

		/// <inheritdoc />
		public override void GetValue(object fieldOwner, out object result)
		{
			try
			{
				result = get(fieldOwner, collectionIndex);
			}
			catch(Exception e)
			{
				Debug.LogError(ToString() + ".GetValue(" + StringUtils.ToStringCompact(fieldOwner) + ") with collectionIndex="+ collectionIndex+ ", fieldOwner.Count="+StringUtils.CountToString(fieldOwner as System.Collections.ICollection)+"\n"  + e);
				result = Type.DefaultValue();
			}
		}

		/// <inheritdoc />
		public override void SetValue(ref object fieldOwner, object value)
		{
			try
			{
				set(ref fieldOwner, collectionIndex, value);
			}
			catch(Exception e)
			{
				Debug.LogError(ToString() + ".SetValue("+ StringUtils.ToStringCompact(fieldOwner) + ", "+StringUtils.ToStringCompact(value)+") " + e);
			}
		}
			
		/// <inheritdoc />
		public override object[] GetAttributes(bool inherit = true)
		{
			return ArrayPool<object>.ZeroSizeArray;
		}

		#if UNITY_EDITOR
		/// <inheritdoc />
		public override SerializedProperty TryBuildSerializedProperty(SerializedObject serializedObject, SerializedProperty parentProperty)
		{
			// Handle collection types not serialized by Unity (i.e. everything except arrays and lists).
			if(!parentProperty.isArray)
			{
				#if DEV_MODE
				Debug.LogWarning(GetType().Name + ".GetSerializedProperty(" + (serializedObject == null ? "null" : serializedObject.targetObject.name) + ") index=" + collectionIndex + ", parentProperty.isArray=" + StringUtils.False + ")");
				#endif
				return null;
			}

			// Handle index out of bounds. This bug can happen e.g. if collection has been resized via SerializedProperty
			// but a CollectionMemberData still exists for a collection element that no longer exists.
			if(collectionIndex < 0 || collectionIndex >= parentProperty.arraySize)
			{
				#if DEV_MODE
				Debug.LogError(GetType().Name + ".GetSerializedProperty(" + (serializedObject == null ? "null" : serializedObject.targetObject.name) + ") index=" + collectionIndex + " >= arraySize=" + parentProperty.arraySize);
				#endif
				return null;
			}

			// Disable warning CS1058: A previous catch clause already catches all exceptions. All non-exceptions thrown will be wrapped in a `System.Runtime.CompilerServices.RuntimeWrappedException',
			// because in practice not all exceptions are caught.
			#pragma warning disable 1058
			try
			{
				return parentProperty.GetArrayElementAtIndex(collectionIndex);
			}
			#if DEV_MODE
			catch(Exception e)
			{
				Debug.LogError(GetType().Name +".GetSerializedProperty("+(serializedObject == null ? "null" : serializedObject.targetObject.name)+") (index="+collectionIndex+", arraySize="+parentProperty.arraySize+") Exception: "+e);
				return null;
			}
			#endif

			catch //causes warning CS1058, but is not actually redundant
			{
				#if DEV_MODE
				Debug.LogError(GetType().Name +".GetSerializedProperty("+(serializedObject == null ? "null" : serializedObject.targetObject.name)+") (index="+collectionIndex+", arraySize="+parentProperty.arraySize+") Non-CLS compliant exception");
				#endif
				return null;
			}
			#pragma warning restore 1058
		}
		#endif

		/// <inheritdoc />
		public override void Dispose()
		{
			#if UNITY_EDITOR
			serializedProperty = null;
			#endif

			LinkedMemberInfoPool.Dispose(this);
		}
	}
}