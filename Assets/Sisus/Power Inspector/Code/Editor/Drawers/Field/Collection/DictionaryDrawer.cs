#define SAFE_MODE

using JetBrains.Annotations;
using Sisus.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sisus
{
	[Serializable, DrawerForField(typeof(IDictionary), true, true)]
	public class DictionaryDrawer : OneDimensionalCollectionDrawer<IDictionary>
	{
		private static readonly List<object> RemoveList = new List<object>();

		private Type keyType;
		private Type valueType;

		/// <inheritdoc/>
		protected override bool IsFixedSize
		{
			get
			{
				try
				{
					return (Value as IList).IsFixedSize;
				}
				catch(NullReferenceException)
				{
					return false;
				}
			}
		}

		/// <inheritdoc/>
		public override string DocumentationPageUrl
		{
			get
			{
				return PowerInspectorDocumentation.GetDrawerInfoUrl("dictionary-drawer");
			}
		}

		
		/// <inheritdoc/>
		protected override Type MemberType
		{
			get
			{
				return Types.DictionaryEntry;
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
		public static DictionaryDrawer Create(IDictionary value, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool readOnly)
		{
			DictionaryDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new DictionaryDrawer();
			}
			result.Setup(value, DrawerUtility.GetType(memberInfo, value), memberInfo, parent, label, readOnly);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc />
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			Setup(setValue as IDictionary, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc />
		protected override void Setup(IDictionary setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			// Prioritize value of LinkedMemberInfo when getting type, so that abstract fields (e.g. IDictionary) can work if a non-abstract instance is provided.
			var type = setValue != null ? setValue.GetType() : setMemberInfo != null ? setMemberInfo.Type : null;
			if(type != null && type.IsGenericType && !type.IsGenericTypeDefinition)
			{
				if(!TryGetMemberTypes(type, out keyType, out valueType))
				{
					#if DEV_MODE
					Debug.LogWarning(ToString(setLabel, setMemberInfo)+" - Setting memberTypes to System.Object and setReadOnly to "+StringUtils.True+" because TryGetMemberTypes failed for type "+StringUtils.ToStringSansNamespace(type)+".");
					#endif

					setReadOnly = true;
					keyType = Types.SystemObject;
					valueType = Types.SystemObject;
				}
			}
			else
			{
				#if DEV_MODE
				if(type == null) { Debug.Log(ToString(setLabel, setMemberInfo)+" - Setting setReadOnly to "+StringUtils.True+" because type "+StringUtils.Null); }
				else { Debug.Log(Msg(ToString(setLabel, setMemberInfo), " - Setting setReadOnly to ", StringUtils.True, " because type ", StringUtils.ToStringSansNamespace(type), " IsGenericType=", type.IsGenericType, ", IsGenericTypeDefinition=", type.IsGenericTypeDefinition)); }
				#endif

				setReadOnly = true;
				keyType = Types.SystemObject;
				valueType = Types.SystemObject;
			}
			
			base.Setup(setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		private static bool TryGetMemberTypes([NotNull]Type type, [NotNull]out Type keyType, [NotNull]out Type valueType)
		{
			Type[] genericArguments;
			if(type.TryGetGenericArgumentsFromBaseClass(Types.Dictionary, out genericArguments))
			{
				if(genericArguments.Length == 2)
				{
					keyType = genericArguments[0];
					valueType = genericArguments[1];
					return true;
				}
				#if DEV_MODE
				Debug.LogWarning("Dictionary.TryGetGenericArgumentsFromBaseClass(Dictionary<,>) returned "+genericArguments.Length+" generic arguments for type "+StringUtils.ToStringSansNamespace(type)+": "+StringUtils.ToString(genericArguments));
				#endif
			}
			#if DEV_MODE
			else { Debug.LogWarning("Dictionary.TryGetGenericArgumentsFromBaseClass(Dictionary<,>) failed for type "+StringUtils.ToStringSansNamespace(type)+": "+StringUtils.ToString(genericArguments)); }
			#endif

			keyType = Types.SystemObject;
			valueType = Types.SystemObject;
			return false;
		}

		/// <inheritdoc />
		protected override void ResizeCollection(ref IDictionary collection, int length)
		{
			int lengthWas = collection.Count;
			
			#if DEV_MODE
			Debug.Log("ResizeCollection<" + StringUtils.ToString(Type) + ">(" + length + ") with memberType " + StringUtils.ToString(MemberType)+ "\nKeyType="+StringUtils.ToString(keyType)+ ", KeyType.DefaultValue()="+ StringUtils.ToString(keyType.DefaultValue()) + "\nValueType=" + StringUtils.ToString(valueType));
			Debug.Assert(!ReadOnly);
			Debug.Assert(keyType != null);
			Debug.Assert(valueType != null);
			#endif

			//if size was reduced
			if(length < lengthWas) 
			{
				int collectionIndex = 0;
				foreach(DictionaryEntry entry in collection)
				{
					if(collectionIndex >= length)
					{
						RemoveList.Add(entry.Key);
					}
					collectionIndex++;
				}

				for(int n = RemoveList.Count - 1; n >= 0; n--)
				{
					collection.Remove(RemoveList[n]);
				}
				RemoveList.Clear();
			}
			else if(length > lengthWas)
			{
				var keyDefaultValue = keyType.DefaultValue();
				if(collection.Contains(keyDefaultValue))
				{
					throw new NotSupportedException();
				}
				collection.Add(keyDefaultValue, valueType.DefaultValue());

				if(length - lengthWas > 1)
				{
					throw new NotSupportedException();
				}
			}
		}

		/// <inheritdoc />
		protected override object MemberDefaultValue()
		{
			var result = new DictionaryEntry(keyType.DefaultValue(), valueType.DefaultValue());
			return result;
		}

		/// <inheritdoc />
		protected override int GetCollectionSize(IDictionary collection)
		{
			#if SAFE_MODE
			if(collection == null)
			{
				#if DEV_MODE
				Debug.LogWarning(ToString()+"GetCollectionSize was called for null collection!");
				#endif
				return 0;
			}
			#endif
			return collection.Count;
		}

		/// <inheritdoc />
		protected override object GetCollectionValue(IDictionary collection, int collectionIndex)
		{
			int current = 0;
			foreach(DictionaryEntry dictionaryEntry in collection)
			{
				if(current == collectionIndex)
				{
					#if DEV_MODE && DEBUG_GET_VALUE
					Debug.Log(ToString()+ ".GetCollectionValue(index=" + collectionIndex + ", collection=" + StringUtils.TypeToString(collection)+ ") result="+StringUtils.ToString(dictionaryEntry) + "(Type: "+StringUtils.TypeToString(dictionaryEntry) +")");
					#endif
					return dictionaryEntry;
				}
				current++;
			}
			#if DEV_MODE
			Debug.LogError(ToString() + ".GetCollectionValue(index=" + collectionIndex + ", collection=" + StringUtils.TypeToString(collection) + ") result=" + StringUtils.ToString(valueType)+".DefaultValue(): "+ StringUtils.ToString(valueType.DefaultValue()) + "(Type: " + StringUtils.TypeToString(valueType.DefaultValue()) + ")");
			#endif
			return valueType.DefaultValue();
		}

		/// <inheritdoc />
		protected override void SetCollectionValue(ref IDictionary collection, int collectionIndex, [NotNull]object dictionaryEntry)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(dictionaryEntry != null, "SetCollectionValue("+StringUtils.ToString(dictionaryEntry)+"<"+ StringUtils.TypeToString(dictionaryEntry) + ">, "+collectionIndex+ ") called with null dictionaryEntry\nMemberType=" + StringUtils.ToString(MemberType)+ ", MemberType.DefaultValue()="+ StringUtils.ToString(MemberType.DefaultValue()));
			#endif

			var set = (DictionaryEntry)dictionaryEntry;

			var key = set.Key;
			var value = set.Value;

			#if DEV_MODE
			Debug.Assert(key != null, "getDictionaryEntryKey("+StringUtils.ToString(dictionaryEntry)+"<"+ StringUtils.TypeToString(dictionaryEntry) + ">) returned null\nMemberType="+ StringUtils.ToString(MemberType)+ ", MemberType.DefaultValue()="+ StringUtils.ToString(MemberType.DefaultValue()));
			#endif

			//collectionIndex is ignored at least for now
			//to try and keep the order right, could theoretically rebuild the dictionary again from scratch,
			//placing items inside it in the same order as before?
			collection[key] = value;
		}
		
		/// <inheritdoc/>
		protected override GetSize GetGetCollectionSizeDelegate()
		{
			return GetCollectionSizeStatic;
		}

		private static object GetCollectionSizeStatic(object collection)
		{
			return ((IDictionary)collection).Count;
		}

		/// <inheritdoc/>
		protected override SetSize GetSetCollectionSizeDelegate()
		{
			var value = Value;
			if(value == null)
			{
				return null;
			}
			return value.GetType().IsGenericType ? SetCollectionSizeStatic : null as SetSize;
		}

		private static void SetCollectionSizeStatic(ref object collection, object size)
		{
			var dictionary = (IDictionary)collection;

			int sizeWas = dictionary.Count;
			int setSize = (int)size;

			if(sizeWas == setSize)
			{
				#if DEV_MODE
				Debug.LogWarning("Dictionary.SetCollectionSizeStatic("+size+") called but collection size was already "+sizeWas);
				#endif
				return;
			}

			//if size was reduced
			if(setSize < sizeWas)
			{
				int collectionIndex = 0;
				foreach(DictionaryEntry entry in dictionary)
				{
					if(collectionIndex >= setSize)
					{
						RemoveList.Add(entry.Key);
					}
					collectionIndex++;
				}

				for(int n = RemoveList.Count - 1; n >= 0; n--)
				{
					dictionary.Remove(RemoveList[n]);
				}
				RemoveList.Clear();
			}
			else if(setSize > sizeWas)
			{
				Type keyType;
				Type valueType;
				if(TryGetMemberTypes(collection.GetType(), out keyType, out valueType))
				{
					var keyDefaultValue = keyType.DefaultValue();
					if(dictionary.Contains(keyDefaultValue))
					{
						throw new NotSupportedException();
					}
					dictionary.Add(keyDefaultValue, valueType.DefaultValue());

					if(setSize - sizeWas > 1)
					{
						throw new NotSupportedException();
					}
				}
				#if DEV_MODE
				else { Debug.LogError("Dictionary.SetCollectionSizeStatic failed to get memberTypes of type "+StringUtils.TypeToString(collection)); }
				#endif
			}

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(dictionary.Count == setSize, "Dictionary.SetCollectionSizeStatic("+setSize+") dictionary count was not expected value but "+dictionary.Count);
			Debug.Assert(ReferenceEquals(collection, dictionary));
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
			var dictionary = collection as IDictionary;
			foreach(DictionaryEntry dictionaryEntry in dictionary)
			{
				if(current == collectionIndex)
				{
					return dictionaryEntry;
				}
				current++;
			}

			var genericArguments = collection.GetType().GetGenericArguments();
			var valueType = genericArguments[1];
			#if DEV_MODE
			Debug.LogError("DictionaryGUI.GetCollectionValue(index=" + collectionIndex + ", collection=" + StringUtils.TypeToString(collection) + ") result=" + StringUtils.ToString(valueType)+".DefaultValue(): "+ StringUtils.ToString(valueType.DefaultValue()) + "(Type: " + StringUtils.TypeToString(valueType.DefaultValue()) + ")");
			#endif
			return valueType.DefaultValue();
		}

		/// <inheritdoc />
		protected override SetCollectionMember GetSetCollectionMemberValueDelegate()
		{
			return SetCollectionValueStatic;
		}

		private static void SetCollectionValueStatic(ref object collection, int flattenedIndex, object memberValue)
		{
			var dictionaryEntry = (DictionaryEntry)memberValue;

			var key = dictionaryEntry.Key;
			var value = dictionaryEntry.Value;

			//collectionIndex is ignored at least for now
			//to try and keep the order right, could theoretically rebuild the dictionary again from scratch,
			//placing items inside it in the same order as before?
			(collection as IDictionary)[key] = value;
		}

		/// <inheritdoc />
		protected override void InsertAt(ref IDictionary collection, int collectionIndex, object memberValue)
		{
			if(!ReadOnly)
			{
				SetCollectionValue(ref collection, collectionIndex, memberValue);
			}
		}

		/// <inheritdoc />
		protected override void RemoveAt(ref IDictionary collection, int collectionIndex)
		{
			int current = 0;
			foreach(DictionaryEntry dictionaryEntry in collection)
			{
				if(current == collectionIndex)
				{
					#if DEV_MODE
					Debug.Log("<color=red>RemoveAt(" + StringUtils.TypeToString(collection) + ", " + collectionIndex + "): " + StringUtils.ToString(dictionaryEntry) + " with Type=" + StringUtils.TypeToString(dictionaryEntry)+"</color>");
					#endif
			
					collection.Remove(dictionaryEntry.Key);
					return;
				}
				current++;
			}

			#if DEV_MODE
			Debug.LogError("RemoveAt index "+collectionIndex+" out of bounds with collection.Count "+collection.Count);
			#endif
		}

		/// <inheritdoc/>
		protected override IDrawer BuildResizeField()
		{
			return CollectionAddFieldDrawer.Create(keyType, ValidateNewKey, OnAddButtonClicked, null, this, GUIContentPool.Create("Add"), ReadOnly);
		}

		private bool ValidateNewKey(object[] keys)
		{
			for(int n = keys.Length - 1; n >= 0; n--)
			{
				var key = keys[n];
				if(key == null)
				{
					return false;
				}

				var dictionary = (IDictionary)GetValue(n);
				if(dictionary != null && dictionary.Contains(key))
				{
					return false;
				}
			}
			return true;
		}

		private bool ValidateNewKey(object key)
		{
			if(key == null)
			{
				return false;
			}

			var dictionary = Value;
			return dictionary == null || !dictionary.Contains(key);
		}

		private bool ValidateExistingKey(int elementIndex, object[] keys)
		{
			for(int n = keys.Length - 1; n >= 0; n--)
			{
				foreach(var element in (IDictionary)GetValue(n))
				{
					var key = keys[n];
					if(key == null)
					{
						return false;
					}

					if(n == elementIndex)
					{
						continue;
					}

					if(key.Equals(element))
					{
						return false;
					}
				}
			}
			return true;
		}

		private void OnAddButtonClicked()
		{
			var adder = Resizer as IParentDrawer;
			var membs = adder.Members;
			var key = membs[0].GetValue();
			if(ValidateNewKey(key))
			{
				var dictionaryEntry = new DictionaryEntry(key, valueType.DefaultValue());

				var values = GetCopyOfValues();
				for(int n = values.Length - 1; n >= 0; n--)
				{
					var collection = values[n] as IDictionary;
					SetCollectionValue(ref collection, collection.Count, dictionaryEntry);
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
		}
		
		/// <inheritdoc />
		protected override bool Sort(ref IDictionary collection, IComparer comparer)
		{
			int count = collection.Count;

			//get array containing keys in the dictionary
			var keysCollection = collection.Keys;
			object[] keys = new object[count];
			keysCollection.CopyTo(keys, 0);

			//get array containing values in the dictionary
			var valuesCollection = collection.Values;
			var values = ArrayPool<object>.Create(count);
			valuesCollection.CopyTo(values, 0);

			//sort the keys and the values by the keys
			Array.Sort(keys, values, comparer);

			if(!keys.ContentsMatch(keysCollection))
			{
				collection.Clear();
				for(int p = 0; p < count; p++)
				{
					collection.Add(keys[p], values[p]);
				}
				return true;
			}
			return false;
		}

		/// <inheritdoc />
		protected override Type ElementComparerDefiningType()
		{
			return keyType;
		}

		/// <inheritdoc/>
		protected override IDrawer CreateMemberDrawer(object value, Type memberType, LinkedMemberInfo memberFieldInfo, GUIContent memberLabel)
		{
			return DictionaryEntryDrawer.Create((DictionaryEntry)value, keyType, valueType, memberFieldInfo, this, memberLabel, ReadOnly || IsReadOnlyCollection, ValidateExistingKey);
		}

		/// <inheritdoc/>
		protected override IDictionary GetCopyOfValue(IDictionary source)
		{
			if(source == null)
			{
				return null;
			}

			var type = source.GetType();
			
			Type keyType;
			Type valueType;
			if(!TryGetMemberTypes(type, out keyType, out valueType))
			{
				#if DEV_MODE
				Debug.LogError(ToString()+".GetCopyOfValue failed for source of type "+StringUtils.TypeToString(source));
				#endif
				return source;
			}

			var equalityComparerType = typeof(IEqualityComparer<>).MakeGenericType(keyType);
			var parameterTypes = ArrayExtensions.TempTypeArray(type, equalityComparerType);
			//try to get the constructor Dictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer)
			var constructor = type.GetConstructor(parameterTypes);
			if(constructor != null)
			{
				var parameterValues = ArrayExtensions.TempObjectArray(source, null);

				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(parameterValues.Length == constructor.GetParameters().Length);
				#endif

				var result = (IDictionary)constructor.Invoke(parameterValues);

				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(result.Count == source.Count);
				Debug.Assert(result.ContentsMatch(source));
				#endif

				return result;
			}
			
			var instance = type.DefaultValue() as IDictionary;
			if(instance != null)
			{
				foreach(var key in source.Keys)
				{
					instance.Add(key, source[key]);
				}
				return instance;
			}
			
			#if DEV_MODE
			Debug.LogError(ToString()+".GetCopyOfValue failed to find Dictionary constructor that takes "+StringUtils.ToString(parameterTypes)+" as parameter.");
			#endif

			return source;
		}

		/// <inheritdoc />
		public override bool MemberIsReorderable(IReorderable member)
		{
			return false;
		}

		/// <inheritdoc />
		protected override bool CollectionContains(IDictionary collection, object value)
		{
			var dictionaryEntry = (DictionaryEntry)value;
			if(dictionaryEntry.Key == null)
			{
				return false;
			}
			return collection == null ? false : collection.Contains(dictionaryEntry.Key);
		}

		/// <inheritdoc />
		protected override bool CanAddValueToCollection([CanBeNull]IDictionary collection, object value)
		{
			var dictionaryEntry = (DictionaryEntry)value;
			if(dictionaryEntry.Key == null)
			{
				return false;
			}
			return collection == null ? true : !collection.Contains(dictionaryEntry.Key);
		}
	}
}