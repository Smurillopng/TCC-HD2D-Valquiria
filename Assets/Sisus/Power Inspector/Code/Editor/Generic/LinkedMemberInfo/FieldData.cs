#define USE_IL_FOR_GET_AND_SET

#define DEBUG_SET_VALUE
//#define DEBUG_BUILD_SERIALIZED_PROPERTY

using System;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;

#if CSHARP_7_3_OR_NEWER && USE_IL_FOR_GET_AND_SET
using Sisus.Vexe.FastReflection;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Sisus
{
	public sealed class FieldData : MemberData
	{
		private FieldInfo fieldInfo;

		#if CSHARP_7_3_OR_NEWER && USE_IL_FOR_GET_AND_SET
		private MemberGetter<object,object> getValue;
		private MemberSetter<object,object> setValue;
		#endif

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

		public override string Name
		{
			get
			{
				if(fieldInfo == null)
				{
					#if DEV_MODE
					Debug.LogWarning("FieldData.Name was called with fieldInfo null. This can happen when ToString is called during Setup phase.");
					#endif
					return "";
				}
				return fieldInfo.Name;
			}
		}

		public override MemberTypes MemberType
		{
			get { return MemberTypes.Field; }
		}

		public override LinkedMemberType LinkedMemberType
		{
			get
			{
				return LinkedMemberType.Field;
			}
		}

		public override MemberInfo MemberInfo
		{
			get { return fieldInfo; }
		}

		public override MemberInfo SecondMemberInfo
		{
			get { return null; }
		}

		public override bool IsStatic
		{
			get
			{
				return fieldInfo.IsStatic;
			}
		}

		public override Type Type
		{
			get
			{
				return fieldInfo.FieldType;
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
				#if CSHARP_7_3_OR_NEWER && USE_IL_FOR_GET_AND_SET
				return setValue != null;
				#else
				
				#if UNITY_2018_1_OR_NEWER
				if(fieldInfo.FieldType.IsDefined(typeof(Unity.Collections.ReadOnlyAttribute), true))
				{
					return false;
				}
				#endif
				
				return !fieldInfo.IsInitOnly && !fieldInfo.IsLiteral && !fieldInfo.IsDefined(typeof(System.ComponentModel.ImmutableObjectAttribute), true) && !fieldInfo.FieldType.IsDefined(typeof(System.ComponentModel.ReadOnlyAttribute), true);
				#endif
			}
		}
	
		public void Setup([NotNull]FieldInfo setFieldInfo)
		{
			fieldInfo = setFieldInfo;
			
			#if CSHARP_7_3_OR_NEWER && USE_IL_FOR_GET_AND_SET

			// FastReflection can only handle certain types of const values.
			if(setFieldInfo.IsLiteral && setFieldInfo.IsStatic)
			{
				var fieldType = setFieldInfo.FieldType;

				// FastReflection only supports following types when it comes to constants
				if(fieldType != Types.Bool && fieldType != Types.Int && fieldType != Types.Float && fieldType != Types.Double)
				{
					return;
				}
			}

			// FastReflection can't handle pointers I think.
			if(setFieldInfo.FieldType.IsPointer)
			{
				#if DEV_MODE
				Debug.LogWarning("Won't use FastReflection for field \"" + setFieldInfo.Name + "\" of type " + StringUtils.ToString(setFieldInfo.FieldType) + " because it is a pointer.");
				#endif
				return;
			}

			getValue = setFieldInfo.DelegateForGet();
			
			#if UNITY_2018_1_OR_NEWER
			if(setFieldInfo.IsDefined(typeof(Unity.Collections.ReadOnlyAttribute), false))
			{
				return;
			}
			#endif

			if(!setFieldInfo.IsInitOnly && !setFieldInfo.IsLiteral && !setFieldInfo.IsDefined(typeof(System.ComponentModel.ImmutableObjectAttribute), false) && !setFieldInfo.FieldType.IsDefined(typeof(System.ComponentModel.ReadOnlyAttribute), true))
			{
				setValue = setFieldInfo.DelegateForSet();

				#if DEV_MODE && DEBUG_CAN_WRITE
				Debug.Log(StringUtils.ToColorizedString(setFieldInfo.Name, ".CanWrite=", true, " with IsInitOnly=", setFieldInfo.IsInitOnly));
				#endif
			}
			#endif
		}

		#if UNITY_EDITOR
		public void Setup([NotNull]FieldInfo setFieldInfo, [NotNull]SerializedProperty setSerializedProperty)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(setFieldInfo != null);
			Debug.Assert(setSerializedProperty != null, "FieldData(\""+setFieldInfo.Name+"\") ("+setFieldInfo.FieldType+") serializedProperty was null");
			#endif

			serializedProperty = setSerializedProperty;
			Setup(setFieldInfo);
		}
		#endif

		public override bool Equals(MemberData other)
		{
			var b = other as FieldData;
			return b != null && fieldInfo.Equals(b.fieldInfo);
		}
		
		public override void GetValue(object fieldOwner, out object result)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(IsStatic || fieldOwner != null, ToString());
			#endif

			#if CSHARP_7_3_OR_NEWER && USE_IL_FOR_GET_AND_SET
			if(getValue != null)
			{
				try
				{
					result = getValue(fieldOwner);
				}
				catch(Exception e)
				{
					Debug.LogError(ToString()+".GetValue(owner="+StringUtils.ToString(fieldOwner) +"): "+e);
					result = Type.DefaultValue();
				}
				return;
			}
			#endif

			try
			{
				result = fieldInfo.GetValue(fieldOwner);
			}
			catch(Exception e)
			{
				Debug.LogError(ToString() + ".GetValue(owner=" + StringUtils.ToString(fieldOwner) + "), ownerType=" + StringUtils.TypeToString(fieldOwner)+" "+e);
				result = DefaultValue();
			}
		}
		
		public override void SetValue(ref object fieldOwner, object value)
		{
			#if DEV_MODE && DEBUG_SET_VALUE
			Debug.Log(ToString()+".SetValue(owner="+StringUtils.ToString(fieldOwner)+", value="+ StringUtils.ToString(value)+")");
			#endif

			#if CSHARP_7_3_OR_NEWER && USE_IL_FOR_GET_AND_SET
			if(setValue != null)
			{
				try
				{
					setValue(ref fieldOwner, value);
				}
				catch(InvalidCastException e)
				{
					Debug.LogError("\"" + fieldInfo.Name + "\" SetValue("+ StringUtils.ToString(fieldOwner) + ", "+StringUtils.ToString(value)+ ") InvalidCastException\nType="+StringUtils.ToString(Type)+", value.GetType()="+StringUtils.TypeToString(value)+ ", setValue().GetType()=" + StringUtils.TypeToString(setValue) + ", \n" + e);
				}
				return;
			}
			#endif

			fieldInfo.SetValue(fieldOwner, value);
		}

		#if UNITY_EDITOR
		public override SerializedProperty TryBuildSerializedProperty(SerializedObject serializedObject, SerializedProperty parentProperty)
		{
			if(parentProperty == null)
			{
				if(serializedObject != null)
				{
					#if DEV_MODE && DEBUG_BUILD_SERIALIZED_PROPERTY
					Debug.Log(ToString()+ " serializedObject("+serializedObject.targetObject.name+").FindProperty("+ fieldInfo.Name+")");
					#endif
					return serializedObject.FindProperty(fieldInfo.Name);
				}
				#if DEV_MODE && DEBUG_BUILD_SERIALIZED_PROPERTY
				Debug.Log(ToString()+ " TryBuildSerializedProperty returning null because serializedObject was null.");
				#endif
				return null;
			}
			#if DEV_MODE && DEBUG_BUILD_SERIALIZED_PROPERTY
			Debug.Log(ToString()+ " parentProperty("+ parentProperty.propertyPath+ ").FindProperty(\"" + fieldInfo.Name+ "\")");
			#endif
			return parentProperty.FindPropertyRelative(fieldInfo.Name);
		}
		#endif

		public override object[] GetAttributes(bool inherit = true)
		{
			return Attribute<Attribute>.GetAll(fieldInfo, inherit) as object[];
		}

		/// <inheritdoc/>
		public override void Dispose()
		{
			#if CSHARP_7_3_OR_NEWER && USE_IL_FOR_GET_AND_SET
			getValue = null;
			setValue = null;
			#endif

			#if UNITY_EDITOR
			serializedProperty = null;
			#endif

			LinkedMemberInfoPool.Dispose(this);
		}
	}
}