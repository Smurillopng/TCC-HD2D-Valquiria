#define USE_IL_FOR_GET_AND_SET

//#define DEBUG_SET_VALUE
//#define DEBUG_GET_VALUE

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
	public sealed class PropertyData : MemberData
	{
		private PropertyInfo propertyInfo;

		#if CSHARP_7_3_OR_NEWER && USE_IL_FOR_GET_AND_SET
		MemberGetter<object,object> getValue;
		MemberSetter<object,object> setValue;
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

		public override MemberTypes MemberType
		{
			get { return MemberTypes.Property; }
		}

		public override LinkedMemberType LinkedMemberType
		{
			get
			{
				return LinkedMemberType.Property;
			}
		}

		public override string Name
		{
			get
			{
				if(propertyInfo == null)
				{
					#if DEV_MODE
					Debug.LogWarning("PropertyInfo.Name was called with fieldInfo null. This can happen when ToString is called during Setup phase.");
					#endif
					return "";
				}
				return propertyInfo.Name;
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

		public override bool IsStatic
		{
			get
			{
				return propertyInfo.CanRead ? propertyInfo.GetGetMethod(true).IsStatic : propertyInfo.GetSetMethod(true).IsStatic;
			}
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
				#if CSHARP_7_3_OR_NEWER && USE_IL_FOR_GET_AND_SET
				return getValue != null;
				#else
				return propertyInfo.CanRead;
				#endif
			}
		}

		public override bool CanReadWithoutSideEffects
		{
			get
			{
				return CanRead && propertyInfo.IsAutoProperty();
			}
		}

		public override bool CanWrite
		{
			get
			{
				#if CSHARP_7_3_OR_NEWER && USE_IL_FOR_GET_AND_SET
				return setValue != null;
				#else
				return propertyInfo.CanWrite;
				#endif
			}
		}
			
		#if UNITY_EDITOR
		public void Setup([NotNull]PropertyInfo setPropertyInfo, [NotNull]SerializedProperty setSerializedProperty)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(setPropertyInfo != null);
			Debug.Assert(setSerializedProperty != null, "("+setPropertyInfo.PropertyType+")\""+setPropertyInfo.Name+"\"");
			#endif

			serializedProperty = setSerializedProperty;
			Setup(setPropertyInfo);
		}
		#endif

		public void Setup([NotNull]PropertyInfo inPropertyInfo)
		{
			propertyInfo = inPropertyInfo;
			
			#if CSHARP_7_3_OR_NEWER && USE_IL_FOR_GET_AND_SET

			// FastReflection can't handle pointers I think.
			if(inPropertyInfo.PropertyType.IsPointer)
			{
				#if DEV_MODE
				Debug.LogWarning("Won't use FastReflection for field \"" + inPropertyInfo.Name + "\" of type " + StringUtils.ToString(inPropertyInfo.PropertyType) + " because it is a pointer.");
				#endif
				return;
			}

			if(inPropertyInfo.CanRead)
			{
				try
				{
					getValue = inPropertyInfo.DelegateForGet();
				}
				catch(InvalidProgramException e)
				{
					Debug.LogError("PropertyData.Setup Property \""+inPropertyInfo.Name + "\" of type "+StringUtils.ToStringSansNamespace(inPropertyInfo.PropertyType)+ " with IndexParameters "+StringUtils.ToString(inPropertyInfo.GetIndexParameters() + " InvalidProgramException:\n" + e));
					getValue = null;
				}
			}
			else
			{
				getValue = null;
			}

			if(inPropertyInfo.CanWrite)
			{
				try
				{
					setValue = inPropertyInfo.DelegateForSet();
				}
				catch(InvalidProgramException e)
				{
					Debug.LogError(inPropertyInfo.Name + " of type " + StringUtils.ToStringSansNamespace(inPropertyInfo.PropertyType) + " with IndexParameters "+StringUtils.ToString(inPropertyInfo.GetIndexParameters() + " " + e));
					setValue = null;
				}
			}
			else
			{
				setValue = null;
			}
			#endif
		}

		public override bool Equals(MemberData other)
		{
			var b = other as PropertyData;
			if(b == null)
			{
				return false;
			}

			return b.propertyInfo != null && propertyInfo.EqualTo(b.propertyInfo);
		}

		public override void GetValue(object fieldOwner, out object result)
		{
			#if DEV_MODE
			Debug.Assert(fieldOwner != null || IsStatic, ToString() + ".GetValue called with null fieldOwner but IsStatic was false!");
			#endif

			#if CSHARP_7_3_OR_NEWER && USE_IL_FOR_GET_AND_SET
			Debug.Assert(getValue != null, ToString() + " getValue null");
			#endif

			#if DEV_MODE && DEBUG_GET_VALUE
			Debug.Log(ToString() + ".GetValue(fieldOwner=" + StringUtils.ToString(fieldOwner)+")");
			#endif

			#if CSHARP_7_3_OR_NEWER && USE_IL_FOR_GET_AND_SET
			if(getValue != null)
			{
				try
				{
					result = getValue(fieldOwner);
				}
				catch(InvalidCastException e)
				{
					Debug.LogError(ToString() + ".GetValue(" + StringUtils.ToString(fieldOwner) + "), ownerType=" + StringUtils.TypeToString(fieldOwner) + " " + e + "\ngetValue.Type=" + StringUtils.TypeToString(getValue)+", CanRead=" + CanRead);
					throw;
				}
				catch(NullReferenceException e)
				{
					Debug.LogError(ToString() + ".GetValue(" + StringUtils.ToString(fieldOwner) + "), ownerType=" + StringUtils.TypeToString(fieldOwner) + " " + e + "\ngetValue.Type=" + StringUtils.TypeToString(getValue)+", CanRead=" + CanRead);
					throw;
				}
				catch(MissingReferenceException e)
				{
					Debug.LogError(ToString()+".GetValue(" + StringUtils.ToString(fieldOwner) + "), ownerType=" + StringUtils.TypeToString(fieldOwner) + " " + e + "\ngetValue.Type=" + StringUtils.TypeToString(getValue)+", CanRead=" + CanRead);
					throw;
				}
				return;
			}
			#endif

			try
			{
				result = propertyInfo.GetValue(fieldOwner, null);
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
			Debug.Log(ToString() + ".SetValue(fieldOwner=" + StringUtils.ToString(fieldOwner)+", value="+StringUtils.ToString(value)+")");
			#endif

			#if CSHARP_7_3_OR_NEWER && USE_IL_FOR_GET_AND_SET
			if(setValue != null)
			{
				try
				{
					setValue(ref fieldOwner, value);
				}
				catch(Exception e)
				{
					Debug.LogError(e);
				}
				return;
			}
			#endif

			try
			{
				propertyInfo.SetValue(fieldOwner, value, null);
			}
			catch(Exception e)
			{
				#if CSHARP_7_3_OR_NEWER && USE_IL_FOR_GET_AND_SET
				Debug.LogError("\"" + propertyInfo.Name + "\" SetValue(" + StringUtils.ToString(fieldOwner) + ", " + StringUtils.ToString(value) + ") InvalidCastException\nType=" + StringUtils.ToString(Type) + ", value.GetType()=" + StringUtils.TypeToString(value) + ", setValue().GetType()=" + StringUtils.TypeToString(setValue) + ", \n" + e);
				#else
				Debug.LogError("\"" + propertyInfo.Name + "\" SetValue(" + StringUtils.ToString(fieldOwner) + ", " + StringUtils.ToString(value) + ") InvalidCastException\nType=" + StringUtils.ToString(Type) + ", value.GetType()=" + StringUtils.TypeToString(value) + ", \n" + e);
				#endif
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