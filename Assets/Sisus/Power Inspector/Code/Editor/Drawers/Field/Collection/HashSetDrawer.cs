#define SAFE_MODE

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using Sisus.Attributes;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sisus
{
	[Serializable, DrawerForField(typeof(HashSet<>), false, true)]
	public class HashSetDrawer : OneDimensionalCollectionDrawer<IEnumerable>
	{
		/// <summary> MethodInfo for the "Add" method. </summary>
		private MethodInfo addMethod;

		/// <summary> MethodInfo for the "Clear" method. </summary>
		private MethodInfo clearMethod;
		
		/// <summary> MethodInfo for the "Remove" method. </summary>
		private MethodInfo removeMethod;

		/// <summary> MethodInfo for the "m_count" field. </summary>
		private MethodInfo countField;

		/// <summary> MethodInfo for the "CopyTo" method. </summary>
		private MethodInfo copyToMethod;

		private MethodInfo containsMethod;
		
		/// <summary> Element type of the HashSet. </summary>
		private Type memberType;

		/// <inheritdoc />
		protected override bool IsFixedSize
		{
			get
			{
				return false;
			}
		}

		/// <inheritdoc />
		protected override bool IsReadOnlyCollection
		{
			get
			{
				return false;
			}
		}
		
		/// <inheritdoc />
		protected override Type MemberType
		{
			get
			{
				return memberType;
			}
		}

		private MethodInfo AddMethod
		{
			get
			{
				if(addMethod == null)
				{
					addMethod = Type.GetMethod("Add", BindingFlags.Public | BindingFlags.Instance);
				}
				return addMethod;
			}
		}

		private MethodInfo RemoveMethod
		{
			get
			{
				if(removeMethod == null)
				{
					removeMethod = Type.GetMethod("Remove", BindingFlags.Public | BindingFlags.Instance);
				}
				return removeMethod;
			}
		}

		private MethodInfo ClearMethod
		{
			get
			{
				if(clearMethod == null)
				{
					clearMethod = Type.GetMethod("Clear", BindingFlags.Public | BindingFlags.Instance);
				}
				return clearMethod;
			}
		}

		private MethodInfo CountField
		{
			get
			{
				if(countField == null)
				{
					countField = Type.GetProperty("Count", BindingFlags.Public | BindingFlags.Instance).GetGetMethod();
				}
				return countField;
			}
		}

		private MethodInfo CopyToMethod
		{
			get
			{
				if(copyToMethod == null)
				{
					var arrayType = memberType.MakeArrayType();
					copyToMethod = Type.GetMethod("CopyTo", ArrayExtensions.TempTypeArray(arrayType));
				}
				return copyToMethod;
			}
		}

		private MethodInfo ContainsMethod
		{
			get
			{
				if(containsMethod == null)
				{
					containsMethod = Type.GetMethod("Contains", BindingFlags.Public | BindingFlags.Instance);
				}
				return containsMethod;
			}
		}

		/// <inheritdoc/>
		protected override bool CanContainDuplicates
		{
			get
			{
				return false;
			}
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="value"> The starting cached value of the drawer. </param>
		/// <param name="memberInfo"> LinkedMemberInfo for the field, property or parameter that the drawer represents. Can be null. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="readOnly"> True if control should be read only. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static HashSetDrawer Create(IEnumerable value, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool readOnly)
		{
			HashSetDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new HashSetDrawer();
			}
			result.Setup(value, DrawerUtility.GetType(memberInfo, value), memberInfo, parent, label, readOnly);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc />
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			Setup(setValue as IEnumerable, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc/>
		protected override void Setup(IEnumerable setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			if(setValue == null)
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(setMemberInfo != null, ToString(setLabel)+" initial value and setMemberInfo both null. Figuring out Type is not possible.");
				#endif

				memberInfo = setMemberInfo;
				setValue = GetFirstFieldOrDefaultValue();
			}

			// prioritize value of LinkedMemberInfo when getting type, so that abstract fields can work if a non-abstract instance is provided.
			var type = setValue != null ? setValue.GetType() : setMemberInfo != null ? setMemberInfo.Type : null;
			if(type != null && type.IsGenericType && !type.IsGenericTypeDefinition)
			{
				if(!TryGetMemberType(type, out memberType))
				{
					#if DEV_MODE
					Debug.LogWarning(ToString(setLabel, setMemberInfo)+" - Setting memberType to System.Object and setReadOnly to "+StringUtils.True+" because TryGetMemberType failed for type "+StringUtils.ToStringSansNamespace(type)+".");
					#endif

					memberType = Types.SystemObject;
					setReadOnly = true;
				}
			}
			else
			{
				#if DEV_MODE
				if(type == null) { Debug.Log(ToString(setLabel, setMemberInfo)+" - Setting setReadOnly to "+StringUtils.True+" because type "+StringUtils.Null); }
				else { Debug.Log(Msg(ToString(setLabel, setMemberInfo), " - Setting setReadOnly to ", StringUtils.True, " because type ", StringUtils.ToStringSansNamespace(type), " IsGenericType=", type.IsGenericType, ", IsGenericTypeDefinition=", type.IsGenericTypeDefinition)); }
				#endif

				setReadOnly = true;
				memberType = null;
			}

			base.Setup(setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		private static bool TryGetMemberType([NotNull]Type type, [NotNull]out Type listMemberType)
		{
			Type[] genericArguments;
			if(type.TryGetGenericArgumentsFromBaseClass(Types.HashSet, out genericArguments))
			{
				if(genericArguments.Length == 1)
				{
					listMemberType = genericArguments[0];
					return true;
				}
				#if DEV_MODE
				Debug.LogWarning("HashSet.GetGenericArgumentsFromBaseClass(HashSet<>) returned "+genericArguments.Length+" generic arguments for type "+StringUtils.ToStringSansNamespace(type)+": "+StringUtils.ToString(genericArguments));
				#endif
			}
			#if DEV_MODE
			else { Debug.LogWarning("HashSet.GetGenericArgumentsFromBaseClass(HashSet<>) failed for type "+StringUtils.ToStringSansNamespace(type)+": "+StringUtils.ToString(genericArguments)); }
			#endif

			listMemberType = Types.SystemObject;
			return false;
		}

		/// <inheritdoc />
		protected override void ResizeCollection(ref IEnumerable collection, int length)
		{
			int lengthWas = GetCollectionSize(collection);
			var memberType = MemberType;
			
			//if size was reduced, then remove the leftover elements
			for(int n = lengthWas - 1; n >= length; n--)
			{
				RemoveAt(ref collection, n);
            }

			//if size was increased, then add new elements with default values at the end of the hashSet
			for(int n = lengthWas; n < length; n++)
			{
				InsertAt(ref collection, n, memberType.DefaultValue());
			}
		}
		
		/// <inheritdoc />
		protected override int GetCollectionSize(IEnumerable collection)
		{
			#if DEV_MODE
			Debug.Assert(collection != null);
			#endif
			return (int)CountField.Invoke(collection);
		}

		/// <inheritdoc />
		protected override object GetCollectionValue(IEnumerable collection, int collectionIndex)
		{
			int current = 0;
			foreach(var item in collection)
			{
				if(current == collectionIndex)
				{
					return item;
				}
				current++;
			}
			return MemberType.DefaultValue();
		}

		/// <inheritdoc />
		protected override void SetCollectionValue(ref IEnumerable collection, int collectionIndex, object memberValue)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(!ReadOnly);
			#endif

			int count = GetCollectionSize(collection);
			var values = Array.CreateInstance(MemberType, count);
			CopyToMethod.InvokeWithParameter(collection, values);
			ClearMethod.Invoke(collection);
			var add = AddMethod;

			for(int n = 0; n < collectionIndex; n++)
			{
				add.InvokeWithParameter(collection, values.GetValue(n));
			}

			#if DEV_MODE && PI_ASSERTATIONS
			if(memberValue == null)
			{
				Debug.LogError("HashSet cannot contain null values!");
			}
			else if(memberValue.GetType() != memberType)
			{
				Debug.LogError(Msg("SetCollectionValue called with memberValue ", memberValue, " with type ", memberValue.GetType(), " not matching memberType ", memberType));
			}
			#endif

			add.InvokeWithParameter(collection, memberValue);

			for(int n = collectionIndex + 1; n < count; n++)
			{
				add.InvokeWithParameter(collection, values.GetValue(n));
			}
		}

		/// <inheritdoc/>
		protected override GetSize GetGetCollectionSizeDelegate()
		{
			return GetCollectionSizeStatic;
		}

		private static object GetCollectionSizeStatic(object collection)
		{
			var countField = collection.GetType().GetProperty("Count", BindingFlags.Public | BindingFlags.Instance).GetGetMethod();
			return (int)countField.Invoke(collection);
		}

		/// <inheritdoc/>
		protected override SetSize GetSetCollectionSizeDelegate()
		{
			return Value.GetType().IsGenericType ? SetCollectionSizeStatic : null as SetSize;
		}

		private static void SetCollectionSizeStatic([NotNull]ref object collection, object size)
		{
			var hashSet = (IEnumerable)collection;

			int setSize = (int)size;
			int sizeWas = 0;
			
			var enumerator = hashSet.GetEnumerator();
			while(enumerator.MoveNext())
			{
				sizeWas++;
			}

			if(setSize == sizeWas)
			{
				#if DEV_MODE
				Debug.LogWarning("HashSet.SetCollectionSizeStatic("+setSize+") called but collection size was already "+sizeWas);
				#endif
				return;
			}

			//if size was reduced
			if(setSize < sizeWas)
			{
				var removeMethod = collection.GetType().GetMethod("Remove", BindingFlags.Public | BindingFlags.Instance);
				for(int n = 0; n < sizeWas; n++)
				{
					removeMethod.InvokeWithParameter(hashSet, n);
				}
			}
			else
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(setSize > sizeWas);
				#endif

				Type memberType;
				if(TryGetMemberType(collection.GetType(), out memberType))
				{
					var type = collection.GetType();
					var containsMethod = type.GetMethod("Contains", BindingFlags.Public | BindingFlags.Instance);
					var memberTypeDefaultValue = memberType.DefaultValue();
					if(memberTypeDefaultValue == null)
					{
						Debug.LogError("Can't increase HashSet size because default value of member type "+memberType.Name+" is null");
						return;
					}
					if((bool)containsMethod.InvokeWithParameter(hashSet, memberTypeDefaultValue))
					{
						Debug.LogError("HashSet size increased with SetCollectionSize but collection already contained default value "+StringUtils.ToString(memberTypeDefaultValue));
						return;
					}
				
					var addMethod = type.GetMethod("Add", BindingFlags.Public | BindingFlags.Instance);
					addMethod.InvokeWithParameter(hashSet, memberTypeDefaultValue);

					if(setSize - sizeWas > 1)
					{
						Debug.LogError("HashSet size increased by "+(setSize - sizeWas)+" which is not supported because HashSet can only contain one instance of the default value.");
					}
				}
				#if DEV_MODE
				else { Debug.LogWarning("HashSet.SetCollectionSize - failed to get memberType of type "+StringUtils.ToStringSansNamespace(collection.GetType())+"."); }
				#endif
			}

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(ReferenceEquals(collection, hashSet));
			enumerator = hashSet.GetEnumerator();
			int testSize = 0;
			while(enumerator.MoveNext())
			{
				testSize++;
			}
			Debug.Assert(testSize == setSize, "HashSet.SetCollectionSizeStatic("+setSize+") resulting hashset count was not expected value but "+testSize);
			#endif
		}

		/// <inheritdoc />
		protected override GetCollectionMember GetGetCollectionMemberValueDelegate()
		{
			return GetCollectionValueStatic;
		}

		private static object GetCollectionValueStatic(object collection, int collectionIndex)
		{
			int current = 0;
			var hashSet = collection as IEnumerable;
			foreach(object item in hashSet)
			{
				if(current == collectionIndex)
				{
					return item;
				}
				current++;
			}

			var genericArguments = collection.GetType().GetGenericArguments();
			var valueType = genericArguments[1];
			#if DEV_MODE
			Debug.LogError("HashSetGUI.GetCollectionValue(index=" + collectionIndex + ", collection=" + StringUtils.TypeToString(collection) + ") result=" + StringUtils.ToString(valueType)+".DefaultValue(): "+ StringUtils.ToString(valueType.DefaultValue()) + "(Type: " + StringUtils.TypeToString(valueType.DefaultValue()) + ")");
			#endif
			return valueType.DefaultValue();
		}

		/// <inheritdoc />
		protected override SetCollectionMember GetSetCollectionMemberValueDelegate()
		{
			return SetCollectionValueStatic;
		}

		private static void SetCollectionValueStatic(ref object collection, int collectionIndex, object memberValue)
		{
			var hashSet = collection as IEnumerable;
			var type = hashSet.GetType();
			var countField = type.GetProperty("Count", BindingFlags.Public | BindingFlags.Instance).GetGetMethod();
			int count = (int)countField.Invoke(collection);

			var memberType = type.GetGenericArguments()[0];
			var arrayType = memberType.MakeArrayType();
			var copyToMethod = type.GetMethod("CopyTo", ArrayExtensions.TempTypeArray(arrayType));
			
			var values = Array.CreateInstance(memberType, count);
			copyToMethod.InvokeWithParameter(collection, values);
			var clearMethod = type.GetMethod("Clear", BindingFlags.Public | BindingFlags.Instance);
			clearMethod.Invoke(collection);
			var add = type.GetMethod("Add", BindingFlags.Public | BindingFlags.Instance);;

			for(int n = 0; n < collectionIndex; n++)
			{
				add.InvokeWithParameter(collection, values.GetValue(n));
			}

			add.InvokeWithParameter(collection, memberValue);

			for(int n = collectionIndex + 1; n < count; n++)
			{
				add.InvokeWithParameter(collection, values.GetValue(n));
			}
		}

		/// <inheritdoc />
		protected override void InsertAt(ref IEnumerable collection, int collectionIndex, object memberValue)
		{
			int count = GetCollectionSize(collection);

			var values = Array.CreateInstance(MemberType, count);
			CopyToMethod.InvokeWithParameter(collection, values);
			ClearMethod.Invoke(collection);
			var add = AddMethod;

			for(int n = 0; n < collectionIndex; n++)
			{
				add.InvokeWithParameter(collection, values.GetValue(n));
			}
			
			add.InvokeWithParameter(collection, memberValue);

			for(int n = collectionIndex; n < count; n++)
			{
				add.InvokeWithParameter(collection, values.GetValue(n));
			}

			#if DEV_MODE
			Debug.Assert(GetCollectionSize(collection) == count + 1);
			#endif
		}

		/// <inheritdoc />
		protected override void RemoveAt(ref IEnumerable collection, int collectionIndex)
		{
			int current = 0;
			foreach(var item in collection)
			{
				if(current == collectionIndex)
				{
					#if DEV_MODE
					Debug.Log("<color=red>RemoveAt(" + StringUtils.TypeToString(collection) + ", " + collectionIndex + "): " + StringUtils.ToString(item) + " with Type=" + StringUtils.TypeToString(item) +"</color>");
					#endif
			
					RemoveMethod.InvokeWithParameter(collection, item);
					return;
				}
				current++;
			}
		}
		
		/// <inheritdoc/>
		protected override IDrawer BuildResizeField()
		{
			return CollectionAddFieldDrawer.Create(memberType, ValidateKey, OnAddButtonClicked, null, this, GUIContentPool.Create("Add"), ReadOnly);
		}

		private bool ValidateKey(object[] keys)
		{
			var key = keys[0];
			if(key == null)
			{
				return false;
			}
			if((bool)ContainsMethod.InvokeWithParameter(Value, key))
			{
				return false;
			}
			return true;
		}

		private void OnAddButtonClicked()
		{
			var adder = Resizer as IParentDrawer;
			var membs = adder.Members;
			var key = membs[0].GetValue();
			if(ValidateKey(key))
			{
				var values = GetCopyOfValues();
				for(int n = values.Length - 1; n >= 0; n--)
				{
					var collection = values[n] as IEnumerable;
					SetCollectionValue(ref collection, GetCollectionSize(collection), key);
				}

				#if DEV_MODE
				Debug.Log("OnAddButtonClicked - setting values to: "+StringUtils.ToString(values));
				#endif

				SetValues(values, false, true);

				RebuildMemberBuildListAndMembers();

				adder = members[0] as IParentDrawer;
				// resect the add field which is no longer selected after members were rebuilt
				adder.Members[1].Select(ReasonSelectionChanged.Initialization);

				OnCachedValueChanged(false, false);
			}
			#if DEV_MODE
			else { Debug.Log("OnAddButtonClicked - key "+StringUtils.ToString(key)+" failed validation"); }
			#endif
		}

		private bool ValidateKey(object key)
		{
			if(key == null)
			{
				return false;
			}

			if((bool)containsMethod.InvokeWithParameter(Value, key))
			{
				return false;
			}
			return true;
		}

		/// <inheritdoc />
		protected override bool ValuesAreEqual(IEnumerable a, IEnumerable b)
		{
			if(ReferenceEquals(a, b))
			{
				return true;
			}
			if(ReferenceEquals(a, null) || ReferenceEquals(b, null))
			{
				return false;
			}

			var enumerator = a.GetEnumerator();
			var otherEnumerator = b.GetEnumerator();
			while(enumerator.MoveNext())
			{
				if(!otherEnumerator.MoveNext())
				{
					return false;
				}
				var element = enumerator.Current;
				var otherElement = otherEnumerator.Current;

				if(ReferenceEquals(element, null))
				{
					if(!ReferenceEquals(otherElement, null))
					{
						return false;
					}
				}
				else
				{
					if(isUnityObjectCollection)
					{
						var obj = element as Object;
						var otherObj = otherElement as Object;
						if(obj != otherObj)
						{
							return false;
						}
					}
					else if(!element.Equals(otherElement))
					{
						return false;
					}
				}
			}

			return !otherEnumerator.MoveNext();
		}

		/// <inheritdoc />
		protected override bool Sort(ref IEnumerable collection, IComparer comparer)
		{
			int count = GetCollectionSize(collection);
			var array = Array.CreateInstance(MemberType, count);
			CopyToMethod.InvokeWithParameters(collection, array, 0);
			Array.Sort(array, comparer);

			if(!array.ContentsMatch(collection))
			{
				ClearMethod.Invoke(collection);
				for(int i = 0; i < count; i++)
				{
					AddMethod.InvokeWithParameter(collection, array.GetValue(i));
				}
				return true;
			}
			return false;
		}

		/// <inheritdoc />
		public override void Dispose()
		{
			base.Dispose();

			addMethod = null;
			clearMethod = null;
			removeMethod = null;
			countField = null;
			copyToMethod = null;
			containsMethod = null;

			memberType = null;
		}

		/// <inheritdoc />
		public override object DefaultValue(bool preferNotNull = false)
		{
			if(CanBeNull && !preferNotNull)
			{
				return null;
			}

			var constructor = Type.GetConstructor(Type.EmptyTypes);
			return constructor.Invoke(null);
		}

		/// <inheritdoc />
		protected override bool CollectionContains(IEnumerable collection, object value)
		{
			if(collection == null)
			{
				return false;
			}

			foreach(var element in collection)
			{
				if(element == value)
				{
					return true;
				}
			}

			return false;
		}

		/// <inheritdoc />
		protected override bool CanAddValueToCollection([CanBeNull]IEnumerable collection, object value)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(CollectionContains(null, value) == false);
			#endif

			return !CollectionContains(collection, value);
		}
	}
}