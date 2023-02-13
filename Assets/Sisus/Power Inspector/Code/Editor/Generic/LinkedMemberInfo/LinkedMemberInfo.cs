//warning CS1058: A previous catch clause already catches all exceptions. All non-exceptions thrown will be wrapped in a `System.Runtime.CompilerServices.RuntimeWrappedException'
//in practice not all exceptions are caught
#pragma warning disable 1058

#define CALL_ON_VALIDATE_FOR_PROPERTIES

//#define DEBUG_NULL_SERIALIZED_PROPERTY
//#define DEBUG_DISPOSE
#define DEBUG_SET_VALUES
//#define DEBUG_ON_VALIDATE
//#define DEBUG_SERIALIZED_PROPERTY
//#define DEBUG_IS_UNITY_SERIALIZED
#define DEBUG_SET_DIRTY
//#define DEBUG_GET_ATTRIBUTES

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;
using Sisus.Attributes;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Sisus
{
	/// <summary>
	/// Represents data about a specific field, property or method and one or several owners.
	/// Automatically handles things like Undo recording when value is about to be changed,
	/// detecting when multiple targets have mixed content, changing the value of value type
	/// fields within value type fields (as long as eventually finds a parent referencing
	/// a class).
	/// Note that although this is marked as Serializable, Unity can't handle serializing it's
	/// parent field
	/// </summary>
	[Serializable]
	public class LinkedMemberInfo
	{
		public const string MixedContentString = "—"; //em dash

		public static readonly TooltipDatabase TooltipDatabase = new TooltipDatabase();
		private static readonly List<Object> ChangingTargetsList = new List<Object>();
		private static readonly List<object> ChangingFieldOwnersList = new List<object>();

		private static readonly List<object> ReusableObjectList = new List<object>();

		[CanBeNull]
		private LinkedMemberInfo parent;

		private LinkedMemberParent parentType = LinkedMemberParent.Undetermined;

		private bool parentChainIsBroken;
		
		private LinkedMemberHierarchy hierarchy;

		private MemberData memberData;

		/// <summary>
		/// if true the LinkedMemberInfo refers to multiple targets
		/// that all don't share the same value
		/// </summary>
		private bool mixedContentCached;
		private float mixedContentLastCached;
		
		#if UNITY_EDITOR
		private bool dontAutoFetchSerializedProperty;
		private string serializedPropertyRelativePath;
		#endif
		
		private bool valueNeedsToBeUpdatedRecursively;

		private bool isUnitySerialized;

		private string displayName;

		private bool canRead;
		private bool canWrite;

		private bool canReadWithoutSideEffects;

		public bool CanBeNull
		{
			get
			{
				if(IsValueType)
				{
					return false;
				}
				
				var type = Type;
				if(type.IsAbstract || Nullable.GetUnderlyingType(type) != null)
				{
					return true;
				}

				if(HasAttribute<CanBeNullAttribute>())
				{
					return true;
				}

				#if UNITY_2019_3_OR_NEWER
				if(HasAttribute<SerializeReference>())
				{
					return true;
				}
				#endif

				if(!IsUnitySerialized)
				{
					return true;
				}

				// Properties could always be null.
				var propertyInfo = memberData.MemberInfo as PropertyInfo;
				if(propertyInfo != null /* && propertyInfo.IsAutoProperty()*/)
				{
					return true;
				}

				return false;
			}
		}

		public bool ParentChainIsBroken
		{
			get
			{
				return parentChainIsBroken;
			}
		}

		public bool IsCollectionOrCollectionMember
		{
			get
			{
				return CollectionIndex != 1 || IsCollection;
			}
		}

		/// <summary>
		/// Gets a value indicating whether target implements the ICollection interface. </summary>
		/// <value> True if target implements ICollection, false if not. </value>
		public bool IsCollection
		{
			get
			{
				return Type.IsCollection();
			}
		}

		/// <summary>
		/// Gets or sets the LinkedMemberHierarchy to which this member belongs. </summary>
		/// <value> The hierarchy of the  </value>
		public LinkedMemberHierarchy Hierarchy
		{
			get
			{
				return hierarchy;
			}
			
			set
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(value != null);
				#endif
				hierarchy = value;
			}
		}

		/// <summary>
		/// If this is a member of a collection returns zero-based index of member in said collection.
		/// If member represents a resizer field for a collection returns -2. Otherwise returns -1.
		/// </summary>
		/// <value>
		/// The collection index if a collection member, -2 if collection resizer, else -1.
		/// </value>
		public int CollectionIndex
		{
			get
			{
				return memberData.CollectionIndex;
			}
		}

		/// <summary>
		/// Gets or sets the index parameter values of this member.
		/// 
		/// For indexers, this returns an object array containing the default values
		/// for each index parameter type, or the last cached values for said parameter.
		/// 
		/// For collection members, this returns an single-element object array containing
		/// the collection index of the member.
		/// 
		/// If this is of type that does not support index parameters, then 
		/// returns null, and calling set throws NotSupportedException.
		///  </summary>
		/// <value> Index parameters or null if not applicable. </value>
		public object[] IndexParameters
		{
			get
			{
				return memberData.IndexParameters;
			}

			set
			{
				memberData.IndexParameters = value;
			}
		}

		/// <summary>
		/// Gets the number of UnityEngine.Object targets that the member represents. </summary>
		/// <value> The number of UnityEngine.Object targets. </value>
		public int TargetCount
		{
			get
			{
				return hierarchy.TargetCount;
			}
		}

		/// <summary>
		/// Gets the direct parent LinkedMemberInfo of this 
		/// For root members contained directly on a UnityEngine.Object, returns null.
		/// </summary>
		/// <value> The parent, or null if has none. </value>
		[CanBeNull]
		public LinkedMemberInfo Parent
		{
			get
			{
				return parent;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this represents a static field, property or method.
		/// Also returns true for parameters, because their values can be get and set without
		/// the need for an owning instance.
		/// </summary>
		/// <value> True if instance is not needed for getting or setting value. </value>
		public bool IsStatic
		{
			get
			{
				return memberData.IsStatic;
			}
		}

		/// <summary>
		/// Gets a value indicating if this MemberData also expects an owner instance to be provided
		/// when calling GetValue or SetValue.
		/// 
		/// This is usually true for instance members and false for static members, but there are
		/// exceptions like CollectionResizerData.
		/// </summary>
		/// <value> True if has an owner. </value>
		public bool HasOwner
		{
			get
			{
				return memberData.HasOwner;
			}
		}

		/// <summary>
		/// Gets a value indicating if this MemberData doesn't require a non-null owner instance to be provided
		/// when calling GetValue or SetValue.
		/// 
		/// This is usually true for static members and false for instance members.
		/// </summary>
		/// <value> True if has no owner or if owner value can be null. </value>
		public bool OwnerCanBeNull
		{
			get
			{
				return memberData.OwnerCanBeNull;
			}
		}

		/// <summary>
		/// Gets a value indicating whether the represented control is a class member serialized by Unity.
		/// 
		/// This is used to determine if Unity's internal Undo handling can be used when the value is changed,
		/// as well as whether or not Objects should be marked as dirty when the field value is changed.
		/// </summary>
		/// <value> True if this object is serialized by Unity, false if not. </value>
		public bool IsUnitySerialized
		{
			get
			{
				return isUnitySerialized;
			}

			private set
			{
				#if DEV_MODE && (DEBUG_IS_UNITY_SERIALIZED || DEBUG_SERIALIZED_PROPERTY)
				Debug.Log(ToString()+".IsUnitySerialized = "+StringUtils.ToColorizedString(value)+" (and dontAutoFetchSerializedProperty="+StringUtils.True+")");
				#endif

				isUnitySerialized = value;
				#if UNITY_EDITOR
				if(!isUnitySerialized)
				{
					dontAutoFetchSerializedProperty = true;
				}
				#endif
			}
		}

		public bool IsRootMember
		{
			get
			{
				return parentType == LinkedMemberParent.UnityObject || parentType == LinkedMemberParent.Static || parentType == LinkedMemberParent.ClassInstance;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this represents members of multiple UnityEngine.Object targets with differing values.
		/// This state is only updated every 0.1s, so it's not guaranteed to be up-to-date.
		/// </summary>
		/// <value>
		/// True if multiple targets have mixed values, false if only has one target or all targets have same values.
		/// </value>
		public bool MixedContent
		{
			get
			{
				if(TargetCount < 2 || !CanReadWithoutSideEffects)
				{
					return false;
				}

				float timeSinceLastUpdatedMixedContentCached = Platform.Time - mixedContentLastCached;

				if(timeSinceLastUpdatedMixedContentCached > 0.1f)
				{
					return GetHasMixedContentUpdated();
				}
				return mixedContentCached;
			}

			set
			{
				mixedContentLastCached = Platform.Time;
				mixedContentCached = value;
			}
		}

		/// <summary>
		/// Returns instance of the default value of type of the member that this represents.
		/// E.g. for int returns 0 and for UnityEngine.Object returns null.
		/// </summary>
		/// <returns> Default value. </returns>
		[CanBeNull]
		public object DefaultValue()
		{
			return memberData.DefaultValue();
		}

		#if UNITY_EDITOR
		/// <summary>
		/// Gets a value indicating whether represented member is currently being animated
		/// via the Unity Editor's animation window.
		/// </summary>
		/// <value> True if member is currently being animated, false if not. </value>
		public bool IsAnimated
		{
			get
			{
				var sp = SerializedProperty;
				return sp != null && sp.isAnimated;
			}
		}
		#endif

		/// <summary> Gets a value indicating whether this represents members on multiple UnityEngine.Object targets. </summary>
		/// <value> True if represents fields on multiple targets, false if not. </value>
		public bool MultiField
		{
			get
			{
				return hierarchy.MultiField;
			}
		}

		/// <summary> Gets the type of the member (field, property or method return value) that this represents. </summary>
		/// <value> The member type. </value>
		[NotNull]
		public Type Type
		{
			get
			{
				return memberData.Type;
			}
		}

		public bool IsValueType
		{
			get
			{
				return Type.IsValueType;
			}
		}
		
		public MemberTypes MemberType
		{
			get
			{
				return memberData.MemberType;
			}
		}

		public LinkedMemberType LinkedMemberType
		{
			get
			{
				return memberData.LinkedMemberType;
			}
		}

		/// <summary> Gets a value indicating whether value we can read by calling one of the GetValue methods. </summary>
		/// <value> True if we can read value, false if not. </value>
		public bool CanRead
		{
			get
			{
				return canRead;
			}
		}

		/// <summary>
		/// Gets a value indicating whether it is possible to write a value using one of the SetValue methods.
		/// 
		/// Note that this can be false even if System.Reflection.MemberInfo.CanWrite is true, because this
		/// also returns false if the parent chain is broken.
		/// </summary>
		/// <value> True if we can read value, false if not. </value>
		public bool CanWrite
		{
			get
			{
				return canWrite;
			}
		}

		/// <summary>
		/// Gets a value indicating whether member can read read without a reasonable risk of side effects.
		/// This is false for fields and auto-properties, and true for properties and methods by default.
		/// However this is also true for properties that are drawn just like normal fields (e.g. when marked with ShowInInspectorAttribute).
		/// 
		/// If this returns false then reading the value of the field should be avoided unless the user specifically requests it, for example
		/// by pressing the getter button on a PropertyDrawer or the invoke button on a MethodDrawer.
		/// </summary>
		/// <value> True if we can read value without risk of side effects. </value>
		public bool CanReadWithoutSideEffects
		{
			get
			{
				// UPDATE: allowing this to be overridden manually, because properties are considered to be safe to read without side effects
				// when marked with the ShowInInspector attribute, or when drawers are otherwise displaying them as normal fields.
				//return memberData.CanReadWithoutSideEffects;
				return canReadWithoutSideEffects;
			}

			set
			{
				canReadWithoutSideEffects = value;
			}
		}

		[CanBeNull]
		public FieldInfo FieldInfo
		{
			get
			{
				return memberData.MemberInfo as FieldInfo;
			}
		}

		[CanBeNull]
		public PropertyInfo PropertyInfo
		{
			get
			{
				return memberData.MemberInfo as PropertyInfo;
			}
		}

		[CanBeNull]
		public MethodInfo MethodInfo
		{
			get
			{
				return memberData.MemberInfo as MethodInfo;
			}
		}

		[CanBeNull]
		public ParameterInfo ParameterInfo
		{
			get
			{
				return memberData.AttributeProvider as ParameterInfo;
			}
		}

		/// <summary>
		/// Gets the MemberInfo used internally by the LinkedMemberInfo
		/// for things such as getting the value.
		/// </summary>
		/// <value> MemberInfo. </value>
		[CanBeNull]
		public MemberInfo MemberInfo
		{
			get
			{
				return memberData.MemberInfo;
			}
		}

		/// <summary>
		/// Some LinkedMemberInfos (MethodData and CollectionResizerData) can have
		/// two separate MemberInfos, one used for getting the value and the other one
		/// for setting it. In those instances, this returns the MethodInfo used for setting
		/// the value, if one is available, otherwise returns null;
		/// </summary>
		/// <value> The second MemberInfo. </value>
		[CanBeNull]
		public MemberInfo SecondMemberInfo
		{
			get
			{
				return memberData.SecondMemberInfo;
			}
		}

		public MulticastDelegate GetDelegate
		{
			get
			{
				return memberData.GetDelegate;
			}
		}

		public MulticastDelegate SetDelegate
		{
			get
			{
				return memberData.SetDelegate;
			}
		}

		/// <summary> Gets the MemberInfo or ParameterInfo used internally by the  </summary>
		/// <value> MemberInfo or ParameterInfo. </value>
		public ICustomAttributeProvider AttributeProvider
		{
			get
			{
				return memberData.AttributeProvider;
			}
		}

		[NotNull]
		public MemberData Data
		{
			get
			{
				return memberData;
			}

			set
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(value != null);
				#endif

				memberData = value;
			}
		}

		public string FullPath
		{
			get
			{
				if(parent == null)
				{
					if(TargetCount == 1)
					{
						return string.Concat(StringUtils.ToStringSansNamespace(hierarchy.Target.GetType()), ".", Name);
					}
					return Name;
				}
				return string.Concat(parent.FullPath, ".", Name);
			}
		}

		public string Name
		{
			get
			{
				if(memberData == null)
				{
					#if DEV_MODE
					Debug.LogWarning("LinkedMemberInfo.Name was called with memberData null. This might happen when certain methods are called during the Setup phase.");
					#endif
					return "";
				}
				return memberData.Name;
			}
		}
		
		public string DisplayName
		{
			get
			{
				if(displayName == null)
				{
					if(memberData == null)
					{
						return "";
					}
					displayName = StringUtils.MakeFieldNameHumanReadable(memberData.Name);
				}
				return displayName;
			}
		}
		
		public string Tooltip
		{
			get
			{
				return TooltipDatabase.GetTooltip(this);
			}
		}
		
		public GUIContent GetLabel()
		{
			#if DEV_MODE
			Debug.Assert(memberData != null);
			#endif
			return GUIContentPool.Create(DisplayName, Tooltip);
		}

		public Component Component
		{
			get
			{
				return hierarchy.Target as Component;
			}
		}

		public Object UnityObject
		{
			get
			{
				return hierarchy.Target;
			}
		}

		public Object[] UnityObjects
		{
			get
			{
				return hierarchy.Targets;
			}
		}

		#if UNITY_EDITOR
		public SerializedProperty SerializedProperty
		{
			get
			{
				var serializedProperty = memberData.SerializedProperty;
				if(serializedProperty == null && !dontAutoFetchSerializedProperty)
				{
					RebuildSerializedProperty();
					return memberData.SerializedProperty;
				}
				return serializedProperty;
			}
		}
		#endif
		
		/// <summary>
		/// Gets a value indicating whether this LinkedMemberInfo is persistent, i.e. it will always be
		/// found in all instances of target type in it's current form, no matter the state of the targets.
		/// E.g. members of a generic type
		/// </summary>
		/// <value>
		/// True if this object is persistent, false if not.
		/// </value>
		public bool IsPersistent
		{
			get
			{
				return true;
			}
		}

		public bool HasUnappliedChanges
		{
			get
			{
				return GetHasUnappliedChanges();
			}
		}
		
		//prevent instances of this type being created automatically by deserialization systems etc.
		//by making the default constructor private
		private LinkedMemberInfo() { }

		public LinkedMemberInfo([NotNull]LinkedMemberHierarchy inHierarchy, [NotNull]MemberData inMemberData)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(inHierarchy != null);
			Debug.Assert(inMemberData != null);
			#endif

			hierarchy = inHierarchy;
			memberData = inMemberData;
		}

		#if UNITY_EDITOR
		public void SetSerializedProperty(SerializedProperty setSerializedProperty)
		{
			#if DEV_MODE && DEBUG_SERIALIZED_PROPERTY
			Debug.Log(ToString()+".SetSerializedProperty("+(setSerializedProperty == null ? "null" : setSerializedProperty.propertyPath)+") (also dontAutoFetchSerializedProperty="+StringUtils.True+")");
			#endif

			memberData.SerializedProperty = setSerializedProperty;
			dontAutoFetchSerializedProperty = true;
		}
		#endif
		
		#if UNITY_EDITOR
		private SerializedProperty GetSerializedProperty()
		{
			SerializedProperty serializedProperty;
			if(TryGetSerializedProperty(serializedPropertyRelativePath, out serializedProperty, MemberInfo))
			{
				return serializedProperty;
			}

			if(parent == null)
			{
				return memberData.TryBuildSerializedProperty(hierarchy.SerializedObject, null);
			}
			
			var parentProperty = parent.SerializedProperty;
			if(parentProperty == null)
			{
				#if DEV_MODE && DEBUG_NULL_SERIALIZED_PROPERTY
				// This can happen e.g. with children of read-only parents...
				Debug.LogWarning(ToString()+ ".GetSerializedProperty failed to get SerializedProperty of parent "+parent+". This is normal if parent is readonly (parent.CanWrite:"+(parent == null ? StringUtils.Null : StringUtils.ToColorizedString(parent.CanWrite)));
				#endif
				return null;
			}
			return memberData.TryBuildSerializedProperty(parentProperty.serializedObject, parentProperty);
		}
		#endif

		#if UNITY_EDITOR
		public void RebuildSerializedProperty()
		{
			var serializedProperty = GetSerializedProperty();
			if(serializedProperty == null)
			{
				#if DEV_MODE && DEBUG_SERIALIZED_PROPERTY
				Debug.Log(ToString()+".dontAutoFetchSerializedProperty = "+StringUtils.True+" because GetSerializedProperty() returned "+StringUtils.Null);
				#endif
				dontAutoFetchSerializedProperty = true;
				memberData.SerializedProperty = null;
			}
			else
			{
				memberData.SerializedProperty = serializedProperty;
			}
		}
		#endif

		public override string ToString()
		{
			try
			{
				// this can happen if ToString is called during Setup phase
				if(memberData == null)
				{
					return "LinkedMemberInfo(null)";
				}

				int collectionIndex = memberData.CollectionIndex;

				string name = memberData.Name;

				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(name != null);
				#endif

				if(name.Length == 1 && parent != null)
				{
					if(collectionIndex != -1)
					{
						return StringUtils.Concat("\"", parent.Name, ".", name, "\"(", memberData.GetType().Name, ")[", collectionIndex, "]");
					}

					return StringUtils.Concat("\"", parent.Name, ".", name, "\"(", memberData.GetType().Name, ")");
				}
			
				if(collectionIndex != -1)
				{
					return StringUtils.Concat("\"", name, "\"(", memberData.GetType().Name, ")[", collectionIndex, "]");
				}

				return StringUtils.Concat("\"", name, "\"(", memberData.GetType().Name, ")");
			}
			catch(NullReferenceException)
			{
				#if DEV_MODE
				Debug.LogError("LinkedMemberInfo(\"" + displayName + "\") NullReferenceException with memberData="+(memberData == null ? StringUtils.Null : "NotNull"));
				#endif
				return StringUtils.Concat("LinkedMemberInfo(\"", displayName, "\")");
			}
		}
		
		public string ValueToString()
		{
			if(mixedContentCached)
			{
				return MixedContentString;
			}
			return StringUtils.ToString(GetValue(0));
		}

		public string ValueToStringForFiltering()
		{
			if(MixedContent)
			{
				return MixedContentString;
			}
			var value = GetValue(0);
			if(value == null)
			{
				return "null";
			}
			var obj = value as Object;
			if(obj != null)
			{
				return obj.HierarchyOrAssetPath();
			}
			return StringUtils.ToString(value);
		}
		
		private void UpdateCanReadAndWrite()
		{
			canRead = memberData.CanRead;
			if(memberData.CanWrite)
			{
				if(valueNeedsToBeUpdatedRecursively)
				{
					canWrite = parent.CanWrite;
				}
				else
				{
					canWrite = true;
				}
			}
			else
			{
				canWrite = false;
			}			
		}

		/// <summary>
		/// Gets value indicating whether any parent is missing from the chain that should lead to the containing target Object.
		/// </summary>
		private bool GetParentChainIsBrokenUpdated()
		{
			var checkMissingParent = this;
			
			do
			{
				if(checkMissingParent.parentType == LinkedMemberParent.Missing)
				{
					return true;
				}
				checkMissingParent = checkMissingParent.parent;
			}
			while(checkMissingParent != null);

			return false;
		}

		/// <summary>
		/// Setups LinkedMemberInfo for a collection resizer.
		/// </summary>
		/// <param name="setParent"> The parent. </param>
		/// <param name="type"> Type of the resizer field value. Usually int. </param>
		/// <param name="getSizeDelegate"> Delegate used to get the size size of the collection. </param>
		/// <param name="setSizeDelegate"> Delegate used to set the size size of the collection. </param>
		public void SetupCollectionResizer([NotNull]LinkedMemberInfo setParent, [NotNull]Type type, [NotNull]GetSize getSizeDelegate, SetSize setSizeDelegate)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(setParent != null, "SetupCollectionResizer setParent was null. type="+StringUtils.ToString(type));
			Debug.Assert(type != null);
			Debug.Assert(getSizeDelegate != null, "SetupCollectionResizer getSizeDelegate was null. type="+StringUtils.ToString(type));
			#endif

			parent = setParent;
			parentType = LinkedMemberParent.LinkedMemberInfo;

			(memberData as CollectionResizerData).Setup(type, getSizeDelegate, setSizeDelegate);
			IsUnitySerialized = true;
			
			#if UNITY_EDITOR
			if(setParent != null)
			{
				SetSerializedProperty(setParent.SerializedProperty);
			}
			#endif

			UpdateCanReadAndWrite();
			canReadWithoutSideEffects = canRead;

			if(GetParentChainIsBrokenUpdated())
			{
				parentChainIsBroken = true;
				valueNeedsToBeUpdatedRecursively = false;
			}
			else
			{
				parentChainIsBroken = false;
				UpdateValueNeedsToBeUpdatedRecursively();
			}

			if(canReadWithoutSideEffects)
			{
				MixedContent = hierarchy.GetHasMixedContentUpdated(this);
			}
		}

		/// <summary>
		/// Setups LinkedMemberInfo for a method.
		/// </summary>
		/// <param name="setParent"> The parent. </param>
		/// <param name="getMethodInfo"> The MethodInfo used when "getting" the value. This is used when the invoke button is pressed, or when method drawer value is copied. </param>
		/// <param name="setMethodInfo"> The MethodInfo used when "setting" the value. This is almost always null for methods, marking them as readonly. </param>
		/// <param name="setParentType"></param>
		public void Setup(LinkedMemberInfo setParent, [NotNull]MethodInfo getMethodInfo, MethodInfo setMethodInfo, LinkedMemberParent setParentType)
		{
			parent = setParent;
			parentType = setParentType;

			(memberData as MethodData).Setup(getMethodInfo, setMethodInfo);
			
			IsUnitySerialized = false;

			canReadWithoutSideEffects = false;
			UpdateCanReadAndWrite();

			if(GetParentChainIsBrokenUpdated())
			{
				parentChainIsBroken = true;
				valueNeedsToBeUpdatedRecursively = false;
			}
			else
			{
				parentChainIsBroken = false;
				UpdateValueNeedsToBeUpdatedRecursively();
			}
		}
		
		public void Setup(LinkedMemberInfo setParent, ParameterInfo parameterInfo)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(setParent != null);
			#endif

			parent = setParent;
			parentType = LinkedMemberParent.LinkedMemberInfo;
			
			(memberData as ParameterData).Setup(parameterInfo);
			
			IsUnitySerialized = false;

			UpdateCanReadAndWrite();
			canReadWithoutSideEffects = canRead;

			if(GetParentChainIsBrokenUpdated())
			{
				parentChainIsBroken = true;
				valueNeedsToBeUpdatedRecursively = false;
			}
			else
			{
				parentChainIsBroken = false;
				UpdateValueNeedsToBeUpdatedRecursively();
			}

			if(canReadWithoutSideEffects)
			{
				MixedContent = hierarchy.GetHasMixedContentUpdated(this);
			}
		}

		public void Setup(LinkedMemberInfo setParent, Type genericTypeArgument, int argumentIndex)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(setParent != null);
			#endif

			parent = setParent;
			parentType = LinkedMemberParent.LinkedMemberInfo;
			
			(memberData as GenericTypeArgumentData).Setup(genericTypeArgument, argumentIndex);
			
			IsUnitySerialized = false;

			UpdateCanReadAndWrite();
			canReadWithoutSideEffects = canRead;

			if(GetParentChainIsBrokenUpdated())
			{
				parentChainIsBroken = true;
				valueNeedsToBeUpdatedRecursively = false;
			}
			else
			{
				parentChainIsBroken = false;
				UpdateValueNeedsToBeUpdatedRecursively();
			}

			if(canReadWithoutSideEffects)
			{
				MixedContent = hierarchy.GetHasMixedContentUpdated(this);
			}
		}
		
		public void SetupCollectionMember([NotNull]LinkedMemberInfo setParent, Type type, int collectionIndex, GetCollectionMember getDelegate, SetCollectionMember setDelegate, string setSerializedPropertyRelativePath = null)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(setParent != null);
			#endif

			parent = setParent;
			parentType = LinkedMemberParent.LinkedMemberInfo;

			var data = memberData as CollectionMemberData;

			bool hasSerializedProperty;

			#if UNITY_EDITOR
			serializedPropertyRelativePath = setSerializedPropertyRelativePath;
			SerializedProperty serializedProperty;
			if(TryGetSerializedProperty(setSerializedPropertyRelativePath, out serializedProperty, null))
			{
				data.Setup(collectionIndex, type, getDelegate, setDelegate, serializedProperty);
				hasSerializedProperty = true;
			}
			else
			#endif
			{
				data.Setup(collectionIndex, type, getDelegate, setDelegate);
				hasSerializedProperty = false;
			}

			UpdateCanReadAndWrite();
			canReadWithoutSideEffects = canRead;

			if(GetParentChainIsBrokenUpdated())
			{
				parentChainIsBroken = true;
				IsUnitySerialized = false;
				#if UNITY_EDITOR
				dontAutoFetchSerializedProperty = false;
				#endif
				valueNeedsToBeUpdatedRecursively = false;
			}
			else
			{
				parentChainIsBroken = false;

				// getting this right is pretty critical, because if it's wrong it can break Undo and asset dirtying...
				IsUnitySerialized = hasSerializedProperty || setParent.IsUnitySerialized;
				
				UpdateValueNeedsToBeUpdatedRecursively();
			}

			if(canReadWithoutSideEffects)
			{
				MixedContent = hierarchy.GetHasMixedContentUpdated(this);
			}

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(memberData.CollectionIndex == collectionIndex);
			#endif
		}
		
		public void Setup([CanBeNull]LinkedMemberInfo setParent, [NotNull]FieldInfo fieldInfo, LinkedMemberParent setParentType, string setSerializedPropertyRelativePath = null)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			if(displayName != null) { Debug.LogError("LinkedMemberInfo.Setup - displayName (\""+displayName+"\") not null! fieldInfo="+fieldInfo.Name); }
			#endif

			parent = setParent;
			parentType = setParentType;

			var data = memberData as FieldData;

			bool hasSerializedProperty;

			#if UNITY_EDITOR
			SerializedProperty serializedProperty;
			serializedPropertyRelativePath = setSerializedPropertyRelativePath;
			if(TryGetSerializedProperty(setSerializedPropertyRelativePath, out serializedProperty, fieldInfo))
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(serializedProperty != null, setSerializedPropertyRelativePath);
				#endif

				data.Setup(fieldInfo, serializedProperty);
				hasSerializedProperty = true;
			}
			else
			#endif
			{
				#if DEV_MODE && DEBUG_SETUP_FIELD_WITHOUT_SERIALIZED_PROPERTY
				Debug.Log("Calling FieldInfo.Setup for \""+fieldInfo.Name+"\" of type "+StringUtils.ToString(fieldInfo.FieldType) + ", parentType "+ setParentType+ ", IsPointer="+ fieldInfo.FieldType.IsPointer+", propertyPath =" + StringUtils.ToString(setSerializedPropertyRelativePath)+", assembly="+fieldInfo.FieldType.Assembly.GetName().ToString()+", attributes="+ StringUtils.TypesToString(AttributeUtility.GetAttributes(fieldInfo)));
				#endif

				data.Setup(fieldInfo);
				hasSerializedProperty = false;
			}

			if(GetParentChainIsBrokenUpdated())
			{
				IsUnitySerialized = false;
				parentChainIsBroken = true;
				#if UNITY_EDITOR
				dontAutoFetchSerializedProperty = false;
				#endif
				valueNeedsToBeUpdatedRecursively = false;
				canRead = false;
				canWrite = false;
				canReadWithoutSideEffects = false;
			}
			else
			{
				parentChainIsBroken = false;

				// Getting this right is pretty critical, because if it's wrong it can break Undo and asset dirtying...
				// UPDATE: Now if any parent in chain is not serializable, also mark children as such?
				if(hasSerializedProperty)
				{
					IsUnitySerialized = AllParentAreUnitySerializable();
				}
				else
				{
					IsUnitySerialized = GuessIfIsUnitySerialized(fieldInfo);
				}
				
				canRead = data.CanRead;
				canWrite = data.CanWrite;
				canReadWithoutSideEffects = canRead;

				UpdateValueNeedsToBeUpdatedRecursively();

				if(canReadWithoutSideEffects)
				{
					MixedContent = hierarchy.GetHasMixedContentUpdated(this);
				}
			}
		}

		public void Setup([CanBeNull]LinkedMemberInfo setParent, [NotNull]PropertyInfo propertyInfo, LinkedMemberParent setParentType, string setSerializedPropertyRelativePath = null)
		{
			parent = setParent;
			parentType = setParentType;
			
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(memberData != null, "LinkedMemberInfo Setup called with memberData being null!\nparent=" + StringUtils.ToString(setParent)+", propertyInfo="+propertyInfo+", serializedPropertyPath="+StringUtils.ToString(setSerializedPropertyRelativePath));
			if(displayName != null) { Debug.LogError("LinkedMemberInfo.Setup - displayName (\""+displayName+"\") not null! propertyInfo="+propertyInfo.Name); }
			#endif

			var data = memberData as PropertyData;
			
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(data != null);
			Debug.Assert(setSerializedPropertyRelativePath == null || (propertyInfo != null && !propertyInfo.IsStatic()), "static property had serializedPropertyRelativePath="+StringUtils.ToString(setSerializedPropertyRelativePath));
			#endif

			bool hasSerializedProperty;

			#if UNITY_EDITOR
			serializedPropertyRelativePath = setSerializedPropertyRelativePath;
			SerializedProperty serializedProperty;
			if(TryGetSerializedProperty(setSerializedPropertyRelativePath, out serializedProperty, propertyInfo))
			{
				data.Setup(propertyInfo, serializedProperty);
				//IsUnitySerialized = true;
				hasSerializedProperty = true;
			}
			else
			#endif
			{
				data.Setup(propertyInfo);

				hasSerializedProperty = false;
			}

			// Default to false. This can still be overridden manually for properties that are displayed as normal fields.
			canReadWithoutSideEffects = false;

			if(GetParentChainIsBrokenUpdated())
			{
				parentChainIsBroken = true;

				IsUnitySerialized = false;
				
				#if UNITY_EDITOR
				dontAutoFetchSerializedProperty = false;
				#endif
				valueNeedsToBeUpdatedRecursively = false;
				canRead = false;
				canWrite = false;
			}
			else
			{
				parentChainIsBroken = false;

				// Getting this right is pretty critical, because if it's wrong it can break Undo and asset dirtying...
				// UPDATE: Now if any parent in chain is not serializable, also mark children as such?
				if(hasSerializedProperty)
				{
					IsUnitySerialized = AllParentAreUnitySerializable();
				}
				else
				{
					IsUnitySerialized = GuessIfIsUnitySerialized(propertyInfo);
				}

				canRead = data.CanRead;
				canWrite = data.CanWrite;

				UpdateValueNeedsToBeUpdatedRecursively();

				if(canReadWithoutSideEffects)
				{
					MixedContent = hierarchy.GetHasMixedContentUpdated(this);
				}
			}
		}

		private bool AllParentAreUnitySerializable()
		{
			for(var p = parent; p != null; p = p.parent)
			{
				if(!p.IsUnitySerialized)
				{
					return false;
				}
			}
			return true;
		}

		public void SetupIndexer(LinkedMemberInfo setParent, [NotNull]PropertyInfo propertyInfo, LinkedMemberParent setParentType)
		{
			parent = setParent;
			parentType = setParentType;

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(memberData != null, "LinkedMemberInfo Setup called with memberData being null!\nparent=" + StringUtils.ToString(setParent)+", propertyInfo="+propertyInfo);
			#endif

			var data = memberData as IndexerData;
			
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(data != null);
			#endif

			data.Setup(propertyInfo);

			UpdateCanReadAndWrite();
			canReadWithoutSideEffects = false;

			IsUnitySerialized = false;

			if(GetParentChainIsBrokenUpdated())
			{
				parentChainIsBroken = true;
				valueNeedsToBeUpdatedRecursively = false;
			}
			else
			{
				parentChainIsBroken = false;
				UpdateValueNeedsToBeUpdatedRecursively();
			}
		}
		
		public T GetValue<T>(int index)
		{
			#if DEV_MODE
			object result;
			try
			{
				result = GetValue(index);
			}
			catch(Exception e)
			{
				Debug.LogError(ToString()+".GetValue<"+typeof(T).FullName+">("+index+") Type="+Type.FullName+" - " + e);
				throw;
			}
			
			try
			{
				return (T)result;
			}
			catch(InvalidCastException e)
			{
				Debug.LogError(ToString()+".GetValue<"+StringUtils.ToString(typeof(T))+">("+index+") Type="+StringUtils.ToString(Type)+", resultType="+StringUtils.TypeToString(result)+", result="+StringUtils.ToString(result)+" - " + e);
				throw;
			}
			#else
			try
			{
				return (T)GetValue(index);
			}
			catch(Exception e)
			{
				Debug.LogError(ToString() + ".GetValue<" + typeof(T).FullName + ">(" + index + ") Type=" + Type.FullName + " - " + e);
				throw;
			}
			#endif
		}

		public object GetValue(int index)
		{
			return hierarchy.GetValue(this, index);
		}

		public object GetStaticValue()
		{
			return memberData.GetValue(null);
		}

		public T GetStaticValue<T>()
		{
			object fieldOwner = null;
			return (T)memberData.GetValue(fieldOwner);
		}

		/// <summary>
		/// Gets member's value in fieldOwner instance.
		/// For static members, fieldOwner should be null.
		/// </summary>
		/// <param name="fieldOwner"> The owner of the member. </param>
		/// <returns>
		/// The value of the member in fieldOwner.
		/// </returns>
		public object GetValue(object fieldOwner)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(fieldOwner != null || OwnerCanBeNull, ToString()+".GetValue called with null fieldOwner");
			#endif

			return memberData.GetValue(fieldOwner);
		}

		public void GetValue(object fieldOwner, out object result)
		{
			memberData.GetValue(fieldOwner, out result);
		}

		/// <summary>
		/// Gets an array containing member values in all UnityEngine.Object targets.
		/// For static members, returns an array containing a single element.
		/// </summary>
		/// <returns>
		/// An array containing member values in all UnityEngine.Object targets.
		/// </returns>
		public object[] GetValues()
		{
			return hierarchy.GetValues(this);
		}

		/// <summary>
		/// Gets an array containing member values in all UnityEngine.Object targets.
		/// For static members, returns an array containing a single element.
		/// </summary>
		/// <returns>
		/// An array containing member values in all UnityEngine.Object targets.
		/// </returns>
		public T[] GetValues<T>()
		{
			return hierarchy.GetValues<T>(this);
		}
		
		/// <summary>
		/// Sets value of members in all targets to given value.
		/// </summary>
		/// <param name="value">
		/// The value. </param>
		/// <returns>
		/// True if an instance field backed value was changed and OnValidate should get called.
		/// False if value did not change, or member was static.
		/// </returns>
		public bool SetValue(object value)
		{
			return hierarchy.SetValue(this, value);
		}

		private bool IsUndoable()
		{
			return LinkedMemberType != LinkedMemberType.Method && LinkedMemberType != LinkedMemberType.Parameter && LinkedMemberType != LinkedMemberType.GenericTypeArgument && CanRead;
		}

		/// <summary>
		/// Sets values of members in fieldOwner instances of UnityEngine.Object targets to given value.
		/// When setting value for static members, fieldOwner and unityObject should be null.
		/// If member is root member of unityObject, then fieldOwner should equal unityObject.
		/// </summary>
		/// <param name="targets"> UnityEngine.Object instances which contain or equals fieldOwners and which contains this member. Empty array if member is static. </param>
		/// <param name="fieldOwners"> instance that contains the member and in which the member value is set. Empty array if member is static. </param>
		/// <param name="value"> The value. </param>
		/// <returns>
		/// True if an instance field backed value was changed and OnValidate should get called.
		/// False if value did not change, or member was static.
		/// </returns>
		public bool SetValues([NotNull]Object[] targets, [NotNull]ref object[] fieldOwners, object value)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(targets.ContentsMatch(hierarchy.Targets));
			#endif
			
			if(parentChainIsBroken)
			{
				#if DEV_MODE
				Debug.LogWarning(ToString()+ ".SetValues aborting because parentChainIsBroken...");
				#endif
				return false;
			}

			MixedContent = false;
			
			int count = targets.Length;

			#if DEV_MODE && DEBUG_SET_VALUES
			Debug.Log(StringUtils.ToColorizedString(ToString()+".SetValues(", fieldOwners, ", val=", value, ") with valueNeedsToBeUpdatedRecursively=", valueNeedsToBeUpdatedRecursively, ", IsStatic=", IsStatic+ ", count="+ count));
			#endif

			if(valueNeedsToBeUpdatedRecursively)
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(!IsStatic);
				#endif

				bool changes = false;
				for(int n = count - 1; n >= 0; n--)
				{
					var fieldOwner = fieldOwners[n];
					
					if(!ValueEquals(fieldOwner, value))
					{
						bool undoWasEnabled = UndoHandler.Enabled;
						UndoHandler.Disable();

						memberData.SetValue(ref fieldOwner, value);

						if(undoWasEnabled)
						{
							UndoHandler.Enable();
						}

						fieldOwners[n] = fieldOwner;
						changes = true;
					}
				}

				//apply new value to parent
				if(changes)
				{
					#if DEV_MODE && PI_ASSERTATIONS
					Debug.Assert(parent != null, ToString()+".SetValues - valueNeedsToBeUpdatedRecursively="+StringUtils.True+" but parent="+StringUtils.Null);
					#endif

					parent.SetValues(fieldOwners);
					return true;
				}
				return false;
			}
			
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(CanWrite);
			#endif
			
			if(IsStatic)
			{
				if(IsUndoable())
				{
					UndoHandler.RegisterUndoableAction(ArrayPool<Object>.ZeroSizeArray, this, value, UndoHandler.GetSetValueMenuText(DisplayName), false);
				}
				object fieldOwner = null;
				memberData.SetValue(ref fieldOwner, value);
				return true;
			}
			
			if(MemberType == MemberTypes.Method)
			{
				ChangingTargetsList.AddRange(targets);
				ChangingFieldOwnersList.AddRange(fieldOwners);
			}
			else if(CanRead)
			{
				for(int n = count - 1; n >= 0; n--)
				{
					var fieldOwner = fieldOwners[n];
					if(!ValueEquals(fieldOwner, value))
					{
						ChangingTargetsList.Add(targets[n]);
						ChangingFieldOwnersList.Add(fieldOwner);
					}
				}
			}

			// TO DO: instead of registering complete object undo
			// could also just use parent LinkedMemberInfo RegisterUndoableAction
			// however what makes that hard is generating the right valueTo parameter
			// for the whole array. As such, doing this would be more feasile on the IDrawer level.
			bool registerCompleteleObjectUndo = memberData is CollectionResizerData;

			var changingTargets = ArrayPool<Object>.Create(ChangingTargetsList);
			ChangingTargetsList.Clear();

			#if UNITY_EDITOR
			bool setTargetsDirty = CanRead && UndoHandler.RegisterUndoableAction(changingTargets, this, value, UndoHandler.GetSetValueMenuText(DisplayName), registerCompleteleObjectUndo);
			#else
			UndoHandler.RegisterUndoableAction(changingTargets, this, value, UndoHandler.GetSetValueMenuText(DisplayName), registerCompleteleObjectUndo);
			#endif

			bool instanceFieldBackedValuesChanged;
			int changingCount = ChangingFieldOwnersList.Count;
			if(changingCount > 0)
			{
				instanceFieldBackedValuesChanged = true;

				for(int n = changingCount - 1; n >= 0; n--)
				{
					var fieldOwner = ChangingFieldOwnersList[n];
					memberData.SetValue(ref fieldOwner, value);
				}

				ChangingFieldOwnersList.Clear();

				OnValidateHandler.CallForTargets(changingTargets);
			}
			else
			{
				instanceFieldBackedValuesChanged = false;
			}

			#if UNITY_EDITOR
			if(setTargetsDirty)
			{
				#if DEV_MODE && DEBUG_SET_DIRTY
				Debug.Log("Setting changing targets dirty: "+StringUtils.ToString(changingTargets));
				#endif

				for(int n = changingTargets.Length - 1; n >= 0; n--)
				{
					EditorUtility.SetDirty(changingTargets[n]);
				}

				// new test: rebuild SerializedProperty to make sure prefab modifications are updated immediately
				if(hierarchy.isPrefabInstance && changingTargets.Length > 0 && memberData.SerializedProperty != null)
				{
					RebuildSerializedProperty();
				}
			}
			#if DEV_MODE && DEBUG_SET_DIRTY
			else { Debug.Log(StringUtils.ToColorizedString("NOT Setting changing targets dirty: ", StringUtils.ToString(changingTargets), "\nCanRead=", CanRead)); }
			#endif
			#endif
			
			return instanceFieldBackedValuesChanged;
		}

		/// <summary>
		/// Sets values of members in fieldOwner instances of UnityEngine.Object targets to given values.
		/// When setting value for static members, fieldOwner and unityObject should be null.
		/// If member is root member of unityObject, then fieldOwner should equal unityObject.
		/// </summary>
		/// <param name="targets">UnityEngine.Object instances which contain or equal the fieldOwners and which contains this member. Ignored if member is static. </param>
		/// <param name="fieldOwners">instances that contain the member and in which the member value is set. Ignored if member is static. </param>
		/// <param name="values"> The values for each field owner. </param>
		/// <returns>
		/// True if an instance field backed value was changed and OnValidate should get called.
		/// False if value did not change, or member was static.
		/// </returns>
		public bool SetValues([NotNull]Object[] targets, [NotNull]ref object[] fieldOwners, [NotNull]object[] values)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			if(!IsStatic)
			{
				Debug.Assert(targets.Length == fieldOwners.Length, ToString()+".SetValues - targets.Length ("+ targets.Length + ") != fieldOwners.Length ("+ fieldOwners.Length + ")");
				Debug.Assert(fieldOwners.Length == values.Length, ToString() + ".SetValues - targets.Length (" + fieldOwners.Length + ") != fieldOwners.Length (" + values.Length + ")");
			}
			#endif

			if(parentChainIsBroken)
			{
				#if DEV_MODE
				Debug.LogWarning(ToString()+ ".SetValues aborting because parentChainIsBroken...");
				#endif
				return false;
			}

			int targetCount = targets.Length;

			#if DEV_MODE && DEBUG_SET_VALUES
			Debug.Log(StringUtils.ToColorizedString("SetValues(owners=", StringUtils.ToString(fieldOwners), ", values=", StringUtils.ToString(values), ") with valueNeedsToBeUpdatedRecursively=", valueNeedsToBeUpdatedRecursively, ", IsStatic=", IsStatic));
			#endif

			if(valueNeedsToBeUpdatedRecursively)
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(!IsStatic);
				#endif

				bool changes = false;
				for(int n = targetCount - 1; n >= 0; n--)
				{
					var value = values[n];
					var fieldOwner = fieldOwners[n];
					if(ValueEquals(fieldOwner, value))
					{
						continue;
					}
					memberData.SetValue(ref fieldOwner, value);
					fieldOwners[n] = fieldOwner;
					changes = true;
				}

				//apply new value to parent
				if(changes)
				{
					// parent should never be null if valueNeedsToBeUpdatedRecursively is true
					parent.SetValues(fieldOwners);
					return true;
				}
				return false;
			}
			
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(CanWrite);
			#endif
			
			if(IsStatic)
			{
				if(CanRead)
				{
					UndoHandler.RegisterUndoableAction(ArrayPool<Object>.ZeroSizeArray, this, values, StringUtils.Concat("Set ", DisplayName, " Value"), false);
				}

				object fieldOwner = null;
				memberData.SetValue(ref fieldOwner, values[0]);
				return true;
			}
			
			if(MemberType == MemberTypes.Method)
			{
				ChangingTargetsList.AddRange(targets);
				ChangingFieldOwnersList.AddRange(fieldOwners);
			}
			else
			{
				for(int n = 0; n < targetCount; n++)
				{
					var fieldOwner = fieldOwners[n];
					var value = values[n];
					if(!ValueEquals(fieldOwner, value))
					{
						ChangingTargetsList.Add(targets[n]);
						ChangingFieldOwnersList.Add(fieldOwner);
					}
				}
			}

			//TO DO: use changingTargets instead of all targets for undo, for more optimized results
			bool registerCompleteleObjectUndo = memberData is CollectionResizerData;

			var changingTargets = ArrayPool<Object>.Create(ChangingTargetsList);
			ChangingTargetsList.Clear();

			#if UNITY_EDITOR
			bool setTargetsDirty = CanRead && UndoHandler.RegisterUndoableAction(changingTargets, this, values, StringUtils.Concat("Set ", DisplayName, " Value"), registerCompleteleObjectUndo);
			#else
			UndoHandler.RegisterUndoableAction(changingTargets, this, values, StringUtils.Concat("Set ", DisplayName, " Value"), registerCompleteleObjectUndo);
			#endif

			bool instanceFieldBackedValuesChanged;
			if(ChangingFieldOwnersList.Count > 0)
			{
				instanceFieldBackedValuesChanged = true;

				for(int n = targetCount - 1; n >= 0; n--)
				{
					var fieldOwner = fieldOwners[n];
					var value = values[n];
					memberData.SetValue(ref fieldOwner, value);
				}
				
				ChangingFieldOwnersList.Clear();

				OnValidateHandler.CallForTargets(changingTargets);
			}
			else
			{
				instanceFieldBackedValuesChanged = false;
			}

			#if UNITY_EDITOR
			if(setTargetsDirty)
			{
				#if DEV_MODE && DEBUG_SET_DIRTY
				Debug.Log("Setting changing targets dirty: "+StringUtils.ToString(changingTargets));
				#endif

				for(int n = changingTargets.Length - 1; n >= 0; n--)
				{
					EditorUtility.SetDirty(changingTargets[n]);
				}

				// new test: rebuild SerializedProperty to make sure prefab modifications are updated immediately
				if(changingTargets.Length > 0 && changingTargets[0].IsPrefabInstance() && memberData.SerializedProperty != null)
				{
					RebuildSerializedProperty();
				}
			}
			#endif
			
			OnValueChanged();

			return instanceFieldBackedValuesChanged;
		}
		
		public bool SetValues(object[] values)
		{
			return values.Length == 1 ? hierarchy.SetValue(this, values[0]) : hierarchy.SetValues(this, values);
		}

		/// <summary>
		/// Does value of member in fieldOwner instance equal given value?
		/// For static members, fieldOwner should be null.
		/// </summary>
		/// <param name="fieldOwner">instance that contains the member and in which the member value is set. Null if member is static. </param>
		/// <param name="value"> The value that member value should be compared against. </param>
		/// <returns>
		/// True if values are equal to each other (Equals(object) returns true), otherwise false.
		/// </returns>
		private bool ValueEquals(object fieldOwner, object value)
		{
			if(!CanRead)
			{
				return false;
			}
			
			var targetValue = GetValue(fieldOwner);
			if(targetValue == null)
			{
				return value == null;
			}
			if(value == null)
			{
				return false;
			}

			return targetValue.Equals(value);
		}

		public bool Represents(LinkedMemberInfo parentInfo, ParameterInfo parameterInfo)
		{
			return parameterInfo == (AttributeProvider as ParameterInfo) && Equals(parent, parentInfo);
		}

		public bool Represents([NotNull]LinkedMemberInfo parentInfo, int collectionIndex)
		{
			return CollectionIndex == collectionIndex && Equals(parent, parentInfo);
		}

		public bool Represents(LinkedMemberInfo parentInfo, MemberInfo memberInfo)
		{
			return memberInfo == MemberInfo && Equals(parentInfo, parent);
		}

		private void OnValueChanged()
		{
			if(!CanRead)
			{
				#if DEV_MODE
				if(TargetCount > 1) { Debug.LogError(ToString()+".OnValueChanged called but !CanRead. Won't update MixedContentCached."); }
				#endif
				return;
			}

			#if DEV_MODE && DEBUG_SIDE_EFFECTS
			if(!CanReadWithoutSideEffects)
			{
				#if DEV_MODE
				if(TargetCount > 1) { Debug.LogWarning("OnValueChanged called but !CanReadWithoutSideEffects. This might be dangerous unless explicitly requested by the user or done with class members we know to be safe?"); }
				#endif
			}
			#endif

			MixedContent = hierarchy.GetHasMixedContentUpdated(this);
		}

		/// <summary>
		/// Gets all attributes applied to this LinkedMemberInfo.
		/// 
		/// Can also fetch attributes from parent in the case of PropertyAttributes on collections
		/// as well as attributes that implement ITargetableAttribute.
		/// </summary>
		/// <param name="inherit"> True to search this member's inheritance chain to find the attributes; otherwise, false. This parameter is ignored for properties and events. </param>
		/// <param name="considerAttributeTarget"> True to search parent for attributes that target this LinkedMemberInfo and to skip attributes on this field targeting members. </param>
		/// <returns> All attributes that target this LinkedMemberInfo. </returns>
		[NotNull]
		public IEnumerable<object> GetAttributes(bool inherit = true, bool considerAttributeTarget = true)
		{
			var attributes = memberData.GetAttributes(inherit);

			if(!considerAttributeTarget)
			{
				foreach(var attribute in attributes)
                {
					yield return attribute;
                }
				yield break;
			}

			for(int n = 0, count = attributes.Length; n < count; n++)
			{
				var attribute = attributes[n];
				if(AttributeTargetsThis(attribute))
				{
					yield return attribute;
				}
			}

			// Support fetching attributes from parent in the case of PropertyAttributes on collections
			// as well as attributes that implement ITargetableAttribute.
			if(parent != null)
			{
				var parentAttributes = parent.memberData.GetAttributes(inherit);
				for(int n = 0, count = parentAttributes.Length; n < count; n++)
				{
					var attribute = parentAttributes[n];
					if(parent.AttributeTargetsMembers(attribute))
					{
						yield return attribute;
					}
				}
			}
		}

		/// <summary>
		/// Gets all attributes of given type applied to this LinkedMemberInfo.
		/// 
		/// Can also fetch attributes from parent in the case of PropertyAttributes on collections
		/// as well as attributes that implement ITargetableAttribute.
		/// </summary>
		/// <param name="inherit"> True to search this member's inheritance chain to find the attributes; otherwise, false. This parameter is ignored for properties and events. </param>
		/// <param name="considerAttributeTarget"> True to search parent for attributes that target this LinkedMemberInfo and to skip attributes on this field targeting members. </param>
		/// <returns> All attributes of given type that target this LinkedMemberInfo. </returns>
		[NotNull]
		public object[] GetAttributes(Type attributeType, bool inherit = true, bool considerAttributeTarget = true)
		{
			var attributes = memberData.GetAttributes(inherit);

			List<object> list = null;
			for(int n = 0, count = attributes.Length; n < count; n++)
			{
				var attribute = attributes[n];
				if(attributeType.IsAssignableFrom(attribute.GetType()))
				{
					if(considerAttributeTarget && !AttributeTargetsThis(attribute))
					{
						continue;
					}

					if(list == null)
					{
						list = ReusableObjectList;
					}
					list.Add(attribute);
				}
			}

			if(considerAttributeTarget)
			{
				// Support fetching attributes from parent in the case of PropertyAttributes
				// and attributes that implement ITargetableAttribute.
				if(parent != null)
				{
					attributes = parent.memberData.GetAttributes(inherit);
					for(int n = 0, count = attributes.Length; n < count; n++)
					{
						var attribute = attributes[n];
						if(attributeType.IsAssignableFrom(attribute.GetType()))
						{
							if(parent.AttributeTargetsMembers(attribute))
							{
								if(list == null)
								{
									list = ReusableObjectList;
								}
								list.Add(attribute);
							}
						}
					}
				}
			}

			if(list != null)
			{ 
				var result = list.ToArray();
				list.Clear();
				return result;
			}
			return ArrayPool<object>.ZeroSizeArray;
		}

		/// <summary>
		/// Checks if any of the given attributes target this LinkedMemberInfo.
		/// 
		/// Can also search parent in the case of PropertyAttributes on collections as well as attributes that implement ITargetableAttribute.
		/// </summary>
		/// <typeparam name="T"> The type of the attribute to find. </typeparam>
		/// <param name="inherit"> True to search this member's inheritance chain to find the attributes; otherwise, false. This parameter is ignored for properties and events. </param>
		/// <param name="considerAttributeTarget"> True to search parent for attributes that target this LinkedMemberInfo and to skip attributes on this field targeting members. </param>
		/// <returns> Attribute of given type that targets this LinkedMemberInfo. Null if none were found. </returns>
		[CanBeNull]
		public bool HasAttribute<T>(bool inherit = true, bool considerAttributeTarget = true) where T : class
		{
			var attributes = memberData.GetAttributes(inherit);
			for(int n = 0, count = attributes.Length; n < count; n++)
			{
				var attribute = attributes[n];
				if(attribute is T)
				{
					if(considerAttributeTarget && !AttributeTargetsThis(attribute))
					{
						continue;
					}

					return true;
				}
			}

			if(!considerAttributeTarget)
			{
				return false;
			}

			// Support fetching attributes from parent in the case of PropertyAttributes
			// and attributes that implement ITargetableAttribute.
			if(parent != null)
			{
				attributes = parent.memberData.GetAttributes(inherit);
				for(int n = 0, count = attributes.Length; n < count; n++)
				{
					var attribute = attributes[n];
					if(attribute is T)
					{
						if(parent.AttributeTargetsMembers(attribute))
						{
							return true;
						}
					}
				}
			}

			return false;
		}

		/// <summary>
		/// Checks if any of the given attributes target this LinkedMemberInfo.
		/// 
		/// Can also search parent in the case of PropertyAttributes on collections as well as attributes that implement ITargetableAttribute.
		/// </summary>
		/// <typeparam name="T"> The type of the attribute to find. </typeparam>
		/// <param name="inherit"> True to search this member's inheritance chain to find the attributes; otherwise, false. This parameter is ignored for properties and events. </param>
		/// <param name="considerAttributeTarget"> True to search parent for attributes that target this LinkedMemberInfo and to skip attributes on this field targeting members. </param>
		/// <returns> Attribute of given type that targets this LinkedMemberInfo. Null if none were found. </returns>
		[CanBeNull]
		public bool HasAnyAttribute<T1, T2, T3, T4>(bool inherit = true, bool considerAttributeTarget = true) where T1 : class where T2 : class where T3 : class where T4 : class
		{
			var attributes = memberData.GetAttributes(inherit);
			for(int n = 0, count = attributes.Length; n < count; n++)
			{
				var attribute = attributes[n];
				if(attribute is T1 || attribute is T2 || attribute is T3 || attribute is T4)
				{
					if(considerAttributeTarget && !AttributeTargetsThis(attribute))
					{
						continue;
					}

					return true;
				}
			}

			if(!considerAttributeTarget)
			{
				return false;
			}

			// Support fetching attributes from parent in the case of PropertyAttributes
			// and attributes that implement ITargetableAttribute.
			if(parent != null)
			{
				attributes = parent.memberData.GetAttributes(inherit);
				for(int n = 0, count = attributes.Length; n < count; n++)
				{
					var attribute = attributes[n];
					if(attribute is T1 || attribute is T2 || attribute is T3 || attribute is T4)
					{
						if(parent.AttributeTargetsMembers(attribute))
						{
							return true;
						}
					}
				}
			}

			return false;
		}

		/// <summary>
		/// Tries to find an attribute of given type that has been applied to this LinkedMemberInfo.
		/// 
		/// Can also search parent in the case of PropertyAttributes on collections as well as attributes that implement ITargetableAttribute.
		/// </summary>
		/// <typeparam name="T"> The type of the attribute to find. </typeparam>
		/// <param name="inherit"> True to search this member's inheritance chain to find the attributes; otherwise, false. This parameter is ignored for properties and events. </param>
		/// <param name="considerAttributeTarget"> True to search parent for attributes that target this LinkedMemberInfo and to skip attributes on this field targeting members. </param>
		/// <returns> Attribute of given type that targets this LinkedMemberInfo. Null if none were found. </returns>
		[CanBeNull]
		public T GetAttribute<T>(bool inherit = true, bool considerAttributeTarget = true) where T : class
		{
			var attributes = memberData.GetAttributes(inherit);
			for(int n = 0, count = attributes.Length; n < count; n++)
			{
				var attribute = attributes[n];
				var result = attribute as T;
				if(result != null)
				{
					if(considerAttributeTarget && !AttributeTargetsThis(attribute))
					{
						continue;
					}

					return result;
				}
			}

			if(!considerAttributeTarget)
			{
				return null;
			}

			// Support fetching attributes from parent in the case of PropertyAttributes
			// and attributes that implement ITargetableAttribute.
			if(parent != null)
			{
				attributes = parent.memberData.GetAttributes();
				for(int n = 0, count = attributes.Length; n < count; n++)
				{
					var attribute = attributes[n];
					var result = attribute as T;
					if(result != null)
					{
						if(parent.AttributeTargetsMembers(attribute))
						{
							return result;
						}
					}
				}
			}

			return null;
		}

		/// <summary>
		/// Gets all attributes of given type applied to this LinkedMemberInfo.
		/// 
		/// Can also fetch attributes from parent in the case of PropertyAttributes on collections
		/// as well as attributes that implement ITargetableAttribute.
		/// </summary>
		/// <typeparam name="T"> The type of the attributes to find. </typeparam>
		/// <param name="inherit"> True to search this member's inheritance chain to find the attributes; otherwise, false. This parameter is ignored for properties and events. </param>
		/// <param name="considerAttributeTarget"> True to search parent for attributes that target this LinkedMemberInfo and to skip attributes on this field targeting members. </param>
		/// <returns> All attributes of given type that target this LinkedMemberInfo. </returns>
		[NotNull]
		public T[] GetAttributes<T>(bool inherit = true, bool considerAttributeTarget = true) where T : class
		{
			var attributes = memberData.GetAttributes(inherit);

			List<T> list = null;
			for(int n = 0, count = attributes.Length; n < count; n++)
			{
				var attribute = attributes[n] as T;
				if(attribute != null)
				{
					if(considerAttributeTarget && !AttributeTargetsThis(attribute))
					{
						#if DEV_MODE && DEBUG_GET_ATTRIBUTES
						Debug.Log("Skipping attribute "+attribute+" because does not target this");
						#endif
						continue;
					}

					if(list == null)
					{
						list = new List<T>();
					}
					list.Add(attribute);
				}
			}

			if(considerAttributeTarget)
			{
				// Support fetching attributes from parent in the case of PropertyAttributes
				// and attributes that implement ITargetableAttribute.
				if(parent != null)
				{
					attributes = parent.memberData.GetAttributes();
					for(int n = attributes.Length - 1; n >= 0; n--)
					{
						var attribute = attributes[n] as T;
						if(attribute != null)
						{
							if(parent.AttributeTargetsMembers(attribute))
							{
								if(list == null)
								{
									list = new List<T>();
								}
								list.Add(attribute);
							}
						}
					}
				}
			}

			return list == null ? ArrayPool<T>.ZeroSizeArray : list.ToArray();
		}
		
		private bool AttributeTargetsThis(object attribute)
		{
			var targetableAttribute = attribute as ITargetableAttribute;
			if(targetableAttribute != null)
			{
				var target = targetableAttribute.Target;
				if(target == Target.Members && IsCollection)
				{
					return false;
				}
				if(target == Target.Default && attribute is PropertyAttribute && IsCollection)
				{
					return false;
				}
			}
			else if(attribute is PropertyAttribute && IsCollection)
			{
				#if UNITY_EDITOR
				if(CustomEditorUtility.AttributeHasDecoratorDrawer(attribute.GetType()))
				{
					#if DEV_MODE
					Debug.Log("AttributeTargetsThis(" + attribute.GetType().Name + "): "+StringUtils.True+", because has decorator drawer");
					#endif
					return true;
				}
				#if DEV_MODE
				Debug.Log("AttributeTargetsThis(" + attribute.GetType().Name + "): "+StringUtils.False+", because IsCollection and has no decorator drawer");
				#endif
				#endif
				return false;
			}
			return true;
		}

		private bool AttributeTargetsMembers(object attribute)
		{
			var targetableAttribute = attribute as ITargetableAttribute;
			if(targetableAttribute != null)
			{
				var target = targetableAttribute.Target;
				if(target == Target.Members && IsCollection)
				{
					return true;
				}
				if(target == Target.Default && attribute is PropertyAttribute && IsCollection)
				{
					return true;
				}
			}
			else if(attribute is PropertyAttribute && IsCollection)
			{
				#if UNITY_EDITOR
				if(CustomEditorUtility.AttributeHasDecoratorDrawer(attribute.GetType()))
				{
					return false;
				}
				#endif

				return true;
			}
			return false;
		}

		/// <summary>
		/// Gets a value indicating whether this represents members of multiple UnityEngine.Object targets with differing values.
		/// This method uses no caching, and so value is always up-to-date.
		/// </summary>
		/// <value>
		/// True if multiple targets have mixed values, false if only has one target or all targets have same values.
		/// </value>
		public bool GetHasMixedContentUpdated()
		{
			MixedContent = hierarchy.GetHasMixedContentUpdated(this);
			return mixedContentCached;
		}

		public Object GetUnityObject(int index)
		{
			return hierarchy.GetTarget(index);
		}
		
		public void InvokeStaticInOwners(string methodName)
		{
			var type = GetParentType();
			var flags = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
			var method = type.GetMethod(methodName, flags);
			method.Invoke(null, null);
		}
		
		public object[] GetFieldOwners()
		{
			switch(parentType)
			{
				case LinkedMemberParent.ClassInstance:
					return hierarchy.NonUnityObjectTargets;
				case LinkedMemberParent.LinkedMemberInfo:
					return parent.GetValues();
				case LinkedMemberParent.UnityObject:
					return UnityObjects;
				case LinkedMemberParent.Missing:
					#if DEV_MODE && PI_ASSERTATIONS
					Debug.Assert(parent == null);
					#endif
					return ArrayPool<object>.ZeroSizeArray;
				case LinkedMemberParent.Static:
					#if DEV_MODE && PI_ASSERTATIONS
					Debug.Assert(IsStatic);
					#endif
					return ArrayPool<object>.ZeroSizeArray;
				default:
					throw new NotSupportedException(parentType.ToString());
			}
		}

		public object GetFieldOwner(int index)
		{
			switch(parentType)
			{
				case LinkedMemberParent.ClassInstance:
					return hierarchy.GetNonUnityObjectTarget(index);
				case LinkedMemberParent.LinkedMemberInfo:
					return parent.GetValue(index);
				case LinkedMemberParent.UnityObject:
					return GetUnityObject(index);
				case LinkedMemberParent.Missing:
					#if DEV_MODE && PI_ASSERTATIONS
					Debug.Assert(parent == null);
					#endif
					return null;
				case LinkedMemberParent.Static:
					#if DEV_MODE && PI_ASSERTATIONS
					Debug.Assert(IsStatic);
					#endif
					return null;
				default:
					throw new NotSupportedException(parentType.ToString());
			}
		}

		public void InvokeInOwners(string methodName, object[] parameters = null, BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
		{
			var fieldOwners = GetFieldOwners();
			int count  = fieldOwners.Length;
			if(count == 0)
			{
				return;
			}
			var fieldOwner = fieldOwners[0];
			var type = fieldOwner.GetType();
			var method = type.GetMethod(methodName, flags);
			method.Invoke(fieldOwner, parameters);
			for(int n = 1; n < count; n++)
			{
				method.Invoke(fieldOwners[n], parameters);
			}
		}

		public void InvokeInOwners(MethodInfo method, object[] parameters = null, bool startIfCoroutine = true)
		{
			var fieldOwners = GetFieldOwners();
			int count  = fieldOwners.Length;
			if(count == 0)
			{
				return;
			}

			bool isCoroutine = method.ReturnType == typeof(IEnumerator);

			if(isCoroutine && startIfCoroutine)
			{
				for(int n = 0; n < count; n++)
				{
					var owner = fieldOwners[n];
					var ienumerator = (IEnumerator)method.Invoke(owner, parameters);
					var monoBehaviour = owner as MonoBehaviour;
					if(!Application.isPlaying)
					{
						StaticCoroutine.StartCoroutineInEditMode(ienumerator);
					}
					else if(monoBehaviour != null)
					{
						monoBehaviour.StartCoroutine(ienumerator);
					}
					else
					{
						StaticCoroutine.StartCoroutine(ienumerator);
					}
				}
			}
			else
			{
				for(int n = 0; n < count; n++)
				{
					method.Invoke(fieldOwners[n], parameters);
				}
			}
		}

		public void InvokeInUnityObjects(string methodName, BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
		{
			var unityObjects = UnityObjects;
			int count  = unityObjects.Length;
			for(int n = count - 1; n >= 0; n--)
			{
				unityObjects[n].GetType().GetMethod(methodName, flags).Invoke(unityObjects[n], null);
			}
		}
		
		public bool IsDuplicateReference(bool checkEvenIfCanNotReadWithoutSideEffects)
		{
			if(!CanReadWithoutSideEffects && !checkEvenIfCanNotReadWithoutSideEffects)
			{
				return false;
			}

			return InspectorValues.IsDuplicateReference(this);
		}

		private bool Equals([CanBeNull]LinkedMemberInfo other)
		{
			if(ReferenceEquals(other, null))
			{
				return false;
			}

			if(ReferenceEquals(memberData, null))
			{
				if(!ReferenceEquals(other.memberData, null))
				{
					return false;
				}
			}
			else if(!memberData.Equals(other.memberData))
			{
				return false;
			}

			return Equals(parent, other.parent);
		}

		private static bool Equals([CanBeNull]LinkedMemberInfo a, [CanBeNull]LinkedMemberInfo b)
		{
			return ReferenceEquals(b, null) ? ReferenceEquals(a, null) : b.Equals(a);
		}

		public void Dispose()
		{
			#if DEV_MODE && DEBUG_DISPOSE
			Debug.Log((memberData != null ? ToString() : (GetType().Name + " (\"" + displayName + "\")"))+".Dispose()");
			#endif

			if(memberData != null)
			{
				memberData.Dispose();
				memberData = null;
			}
			#if DEV_MODE
			else { Debug.LogWarning(GetType().Name + " (\"" + displayName + "\") memberData.Dispose called with memberData=" + StringUtils.Null); }
			#endif

			hierarchy = null;
			parent = null;
			displayName = null;
			
			canRead = false;
			canWrite = false;

			#if UNITY_EDITOR
			dontAutoFetchSerializedProperty = false;
			#endif
			
			isUnitySerialized = false;

			LinkedMemberInfoPool.Dispose(this);
		}

		#if UNITY_EDITOR
		private bool TryGetSerializedProperty([CanBeNull]string relativePath, out SerializedProperty serializedProperty, [CanBeNull]MemberInfo memberInfo)
		{
			if(relativePath == null)
			{
				serializedProperty = null;

				#if DEV_MODE && DEBUG_NULL_SERIALIZED_PROPERTY
				Debug.Log(ToString()+".TryGetSerializedProperty serializedProperty="+StringUtils.Null+" because relativePath was null");
				#endif

				return false;
			}
			dontAutoFetchSerializedProperty = true;
			
			if(parent != null)
			{
				var parentSerializedProperty = parent.SerializedProperty;
				if(parentSerializedProperty != null)
				{
					serializedProperty = parentSerializedProperty.FindPropertyRelative(relativePath);
				}
				else
				{
					#if DEV_MODE && DEBUG_SERIALIZED_PROPERTY
					Debug.Log(ToString()+".TryGetSerializedProperty serializedProperty="+StringUtils.Null+" because parent was not null but parentSerializedProperty was null");
					#endif

					serializedProperty = null;
					return false;
				}
			}
			else
			{
				serializedProperty = hierarchy.SerializedObject.FindProperty(relativePath);
			}

			#if DEV_MODE && PI_ASSERTATIONS
			if(serializedProperty == null && GuessIfIsUnitySerialized(memberInfo))
			{
				// This can currently display unnecessary warnings when called during Setup phase, when called before MemberInfo has been assigned
				Debug.LogWarning("Failed to find SerializedProperty with relativePath \""+ relativePath+ "\" under parent SerializedProperty " + (parent == null ? "n/a" : parent.SerializedProperty == null ? StringUtils.Null : "\"" + parent.SerializedProperty.propertyPath + "\"") + "\nmemberInfo=" + (memberInfo == null ? StringUtils.Null : memberInfo.GetType().Name));
			}
			#endif

			return serializedProperty != null;
		}
		#endif

		private bool GuessIfIsUnitySerialized()
		{
			return GuessIfIsUnitySerialized(MemberInfo);
		}

		private bool GuessIfIsUnitySerialized([CanBeNull]MemberInfo memberInfo)
		{
			if(!AllParentAreUnitySerializable())
			{
				#if DEV_MODE && DEBUG_IS_UNITY_SERIALIZED
				Debug.Log(StringUtils.ToColorizedString(ToString(), ".GuessIfIsUnitySerialized: ", false, " because !parent.isUnitySerialized"));
				#endif
				return false;
			}
			else if(UnityObject == null)
			{
				#if DEV_MODE && DEBUG_IS_UNITY_SERIALIZED
				Debug.Log(StringUtils.ToColorizedString(ToString(), ".GuessIfIsUnitySerialized: ", false, " because UnityObject==", null));
				#endif
				return false;
			}

			if(memberInfo != null)
			{
				if(!PrettySerializer.GuessIfUnityWillSerialize(memberInfo))
				{
					#if DEV_MODE && DEBUG_IS_UNITY_SERIALIZED
					Debug.Log(StringUtils.ToColorizedString(ToString(), ".GuessIfIsUnitySerialized: ", false, " because PrettySerializer.GuessIfUnityWillSerialize("+memberInfo+") was ", false));
					#endif
					return false;
				}
				#if DEV_MODE && DEBUG_IS_UNITY_SERIALIZED
				else { Debug.Log(StringUtils.ToColorizedString(ToString(), ".GuessIfIsUnitySerialized: ", true, " because PrettySerializer.GuessIfUnityWillSerialize("+memberInfo+") was ", true)); }
				#endif
			}
			#if DEV_MODE && DEBUG_IS_UNITY_SERIALIZED
			else { Debug.Log(StringUtils.ToColorizedString(ToString(), ".GuessIfIsUnitySerialized: ", true, " because memberInfo was ", null)); }
			#endif

			return true;
		}

		private void UpdateValueNeedsToBeUpdatedRecursively()
		{
			valueNeedsToBeUpdatedRecursively = false;
			
			if(parentChainIsBroken)
			{
				#if DEV_MODE
				Debug.Log(StringUtils.ToColorizedString(ToString()+".valueNeedsToBeUpdatedRecursively = ", false, " (because parentChainIsBroken)"));
				#endif
				return;
			}

			// Because arrays can't be resized (unlike lists, dictionaries etc.)
			// the array value needs to be updated recursively.
			if(LinkedMemberType == LinkedMemberType.CollectionResizer)
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(parent != null, ToString()+" parent was null");
				#endif

				if(parent == null)
				{
					#if DEV_MODE
					Debug.LogError("CollectionResizer parent was null! Will be unable to resize.");
					#endif
					valueNeedsToBeUpdatedRecursively = false;
					return;
				}

				var typeOfParent = parent.Type;
				if(typeOfParent.IsArray)
				{
					valueNeedsToBeUpdatedRecursively = true;
					return;
				}
				
				bool mightNeedToToBeUpdatedRecursively = true;
				if(typeOfParent.IsGenericType)
				{
					var parentTypeDefinition = typeOfParent.GetGenericTypeDefinition();
					if(parentTypeDefinition == Types.List || parentTypeDefinition == Types.Dictionary)
					{
						mightNeedToToBeUpdatedRecursively = false;
					}
				}

				if(mightNeedToToBeUpdatedRecursively && CanReadWithoutSideEffects)
				{
					if(typeof(IList).IsAssignableFrom(parent.Type))
					{
						if(parent.Type.IsArray)
						{
							valueNeedsToBeUpdatedRecursively = true;
							return;
						}

						var list = parent.GetValue(0) as IList;
						if(list != null && list.IsFixedSize)
						{
							valueNeedsToBeUpdatedRecursively = true;
							return;
						}
					}
				}
				return;
			}

			if(!IsStatic)
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(LinkedMemberType != LinkedMemberType.Parameter, this + " - ParameterDrawer are expected to return true for IsStatic!");
				Debug.Assert(LinkedMemberType != LinkedMemberType.GenericTypeArgument, this + " - GenericTypeArgumentDrawer are expected to return true for IsStatic!");
				#endif
				
				var checkIsValueType = parent;
				while(checkIsValueType != null && checkIsValueType.CanRead && checkIsValueType.CanWrite)
				{
					if(checkIsValueType.IsValueType)
					{
						valueNeedsToBeUpdatedRecursively = true;
						break;
					}
					checkIsValueType = checkIsValueType.parent;
				}
			}
		}

		[CanBeNull]
		private Type GetParentType()
		{
			if(parent != null)
			{
				return parent.Type;
			}
			var target = UnityObject;
			if(target != null)
			{
				return target.GetType();
			}
			return null;
		}

		private bool GetHasUnappliedChanges()
		{
			#if UNITY_EDITOR
			var serializedProperty = SerializedProperty;
			if(serializedProperty == null)
			{
				return false;
			}

			#if DEV_MODE && PI_ASSERTATIONS
			string s = ToString();
			Debug.Assert(!IsStatic, s);
			Debug.Assert(IsUnitySerialized, s);
			#endif

			try
			{
				return serializedProperty.prefabOverride;
			}
			#if DEV_MODE
			catch(Exception e)
			{
				Debug.LogError(ToString()+" "+e);
			#else
			catch
			{
			#endif
				if(TryToRebuildSerializedProperty(ref serializedProperty))
				{
					return serializedProperty.prefabOverride;
				}
				return false;
			}
			#else
			return false;
			#endif
		}

		#if UNITY_EDITOR
		private bool TryToRebuildSerializedProperty(ref SerializedProperty serializedProperty)
		{
			var serializedObject = hierarchy.SerializedObject;
			try
			{
				if(serializedObject.targetObjects.ContainsNullObjects())
				{
					serializedProperty = null;
					return false;
				}
			}
			#if DEV_MODE
			catch(Exception e)
			{
				Debug.LogWarning(e);
			#else
			catch
			{
			#endif
				serializedProperty = null;
				return false;
			}

			string propertyPath;
			try
			{
				propertyPath = serializedProperty.propertyPath;
			}
			#if DEV_MODE
			catch(Exception e)
			{
				Debug.LogWarning(e);
			#else
			catch
			{
			#endif
				serializedProperty = GetSerializedProperty();
				SetSerializedProperty(serializedProperty);
				return serializedProperty != null;
			}
			
			try
			{
				serializedProperty = hierarchy.SerializedObject.FindProperty(propertyPath);
			}
			#if DEV_MODE
			catch(Exception e)
			{
				Debug.LogWarning(e);
			#else
			catch
			{
			#endif
				serializedProperty = GetSerializedProperty();
				SetSerializedProperty(serializedProperty);
			}
			return serializedProperty != null;
		}
		#endif
	}
}