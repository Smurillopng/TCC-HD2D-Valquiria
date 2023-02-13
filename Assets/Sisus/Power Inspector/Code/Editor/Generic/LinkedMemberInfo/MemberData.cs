using System;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Sisus
{
	public abstract class MemberData
	{
		/// <summary> Gets value indicating what kind of a class member this MemberData represents. </summary>
		/// <value> The type of the class member. </value>
		public abstract MemberTypes MemberType { get; }

		/// <summary> Gets value indicating what kind of a class member this MemberData represents. </summary>
		/// <value> The type of the class member. </value>
		public abstract LinkedMemberType LinkedMemberType { get; }

		/// <summary> Gets the MemberInfo that the MemberData wraps. </summary>
		/// <value> The wrapped MemberInfo. This may be null. </value>
		[CanBeNull]
		public abstract MemberInfo MemberInfo { get; }
		
		[CanBeNull]
		public abstract MemberInfo SecondMemberInfo { get; }

		/// <summary> Returns the delegate that is used internally for getting the value of the represented class member. </summary>
		/// <value> The delegate used for getting the value. </value>
		[CanBeNull]
		public virtual MulticastDelegate GetDelegate
		{
			get
			{
				return null;
			}
		}

		/// <summary> Returns the delegate that is used internally for setting the value of the represented class member. </summary>
		/// <value> The delegate used for setting the value. </value>
		[CanBeNull]
		public virtual MulticastDelegate SetDelegate
		{
			get
			{
				return null;
			}
		}

		[CanBeNull]
		public virtual ICustomAttributeProvider AttributeProvider
		{
			get { return MemberInfo; }
		}

		/// <summary> Gets the type of the class member. </summary>
		/// <value> The type. This will never be null. </value>
		[NotNull]
		public abstract Type Type { get; }

		public abstract bool CanRead { get; }

		/// <summary>
		/// Gets a value indicating whether member value can read read without a reasonable risk of side effects.
		/// This is false for fields and auto-properties, and true for properties and methods by default.
		/// However this is also true for properties that are drawn just like normal fields (e.g. when marked with ShowInInspectorAttribute).
		/// 
		/// If this returns false then reading the value of the field should be avoided unless the user specifically requests it, for example
		/// by pressing the getter button on a PropertyDrawer or the invoke button on a MethodDrawer.
		/// </summary>
		/// <value> True if we can read value without risk of side effects. </value>
		public abstract bool CanReadWithoutSideEffects { get; }

		public abstract bool CanWrite { get; }

		/// <summary> Gets the name of the class member. </summary>
		/// <value> The name. </value>
		[NotNull]
		public abstract string Name
		{
			get;
		}

		#if UNITY_EDITOR
		/// <summary> Gets or sets the serialized property of the class member. </summary>
		/// <value> The serialized property. This may be null, if does not represent a field or property that is serialized by Unity. </value>
		[CanBeNull]
		public abstract SerializedProperty SerializedProperty
		{
			get;
			set;
		}
		#endif

		/// <summary> Gets the current value from the class member. </summary>
		/// <param name="fieldOwner"> The owner of the class member. This may be null when IsStatic is true. </param>
		/// <param name="result"> [out] The value. </param>
		public abstract void GetValue([CanBeNull]object fieldOwner, out object result);

		/// <summary>
		/// Gets a value indicating if this MemberData is not tied to a specific instance and
		/// requires no target to be provided when calling GetValue or SetValue.
		/// </summary>
		/// <value> True if this does not represent an instance target, false if it does. </value>
		public abstract bool IsStatic { get; }

		/// <summary>
		/// Gets a value indicating if this MemberData also expects an owner instance to be provided
		/// when calling GetValue or SetValue.
		/// 
		/// This is usually true for instance members and false for static members, but there are
		/// exceptions like CollectionResizerData.
		/// </summary>
		/// <value> True if has an owner. </value>
		public virtual bool HasOwner
		{
			get
			{
				return !IsStatic;
			}
		}

		/// <summary>
		/// Gets a value indicating if this MemberData doesn't require a non-null owner instance to be provided
		/// when calling GetValue or SetValue.
		/// 
		/// This is usually true for static members and false for instance members.
		/// </summary>
		/// <value> True if has no owner or if owner value can be null. </value>
		public virtual bool OwnerCanBeNull
		{
			get
			{
				return IsStatic;
			}
		}

		public virtual int CollectionIndex
		{
			get
			{
				return -1;
			}
		}

		[CanBeNull]
		public virtual object[] IndexParameters
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

		/// <summary>
		/// Does this refer to same target member as MemberData (ignoring reflected type)?
		/// </summary>
		/// <param name="other">
		/// The member data to compare to this object. </param>
		/// <returns>
		/// True if the objects are considered equal, false if they are not.
		/// </returns>
		public abstract bool Equals(MemberData other);
		
		[CanBeNull]
		public virtual object DefaultValue()
		{
			return Type.DefaultValue();
		}

		public object GetValue([CanBeNull]object fieldOwner)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(OwnerCanBeNull || fieldOwner != null, ToString());
			#endif

			object result;
			GetValue(fieldOwner, out result);
			return result;
		}
		
		public abstract void SetValue(ref object fieldOwner, object value);

		/// <summary>
		/// Gets all attributes that have been applied to the member.
		/// </summary>
		/// <param name="inherit"> True to search this member's inheritance chain to find the attributes; otherwise, false. This parameter is ignored for properties and events. </param>
		/// <returns></returns>
		[NotNull]
		public abstract object[] GetAttributes(bool inherit = true);

		/// <summary>
		/// Called when the parent LinkedMemberInfo is disposed.
		/// Should revert all data to default value.
		/// If has a SerializedProperty, should set it null.
		/// At the minimum should call LinkedMemberInfoPool.Dispose(this);
		/// </summary>
		public abstract void Dispose();

		#if UNITY_EDITOR
		public virtual SerializedProperty TryBuildSerializedProperty([NotNull]SerializedObject serializedObject, [CanBeNull]SerializedProperty parentProperty)
		{
			return null;
		}
		#endif
		
		public override string ToString()
		{
			try
			{
				int collectionIndex = CollectionIndex;
				if(collectionIndex != -1)
				{
					return StringUtils.Concat(GetType().Name, "(", Type, ":\"", Name, "\")[", collectionIndex, "]");
				}
				return StringUtils.Concat(GetType().Name, "(", Type, ":\"", Name, "\")");
			}
			catch(NullReferenceException)
			{
				string result = GetType().Name;
				#if DEV_MODE
				Debug.LogError(result+".ToString() NullReferenceException");
				#endif
				return result;
			}
		}
	}
}