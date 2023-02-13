﻿#define SAFE_MODE

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JetBrains.Annotations;
using Sisus.Attributes;
using UnityEngine;

namespace Sisus
{
	[Serializable, DrawerForField(typeof(IList<>), true, true)]
	public class ListDrawer<T> : OneDimensionalCollectionDrawer<IList<T>>
	{
		/// <inheritdoc />
		protected override bool IsFixedSize
		{
			get
			{
				var value = Value as IList;
				return value != null ? value.IsFixedSize : Type == typeof(ReadOnlyCollection<T>);
			}
		}

		/// <inheritdoc />
		protected override bool IsReadOnlyCollection
		{
			get
			{
				var value = Value;
				return value != null ? value.IsReadOnly : Type == typeof(ReadOnlyCollection<T>);
			}
		}
		
		/// <inheritdoc />
		protected sealed override Type MemberType
		{
			get
			{
				return typeof(T);
			}
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="list"> The starting cached value of the drawer. </param>
		/// <param name="memberInfo"> LinkedMemberInfo for the field, property or parameter that the drawer represents. Can be null. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="readOnly"> True if control should be read only. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static ListDrawer<T> Create(IList<T> list, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool readOnly)
		{
			ListDrawer<T> result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new ListDrawer<T>();
			}
			result.Setup(list, DrawerUtility.GetType(memberInfo, list), memberInfo, parent, label, readOnly);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc />
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			Setup(setValue as IList<T>, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}
		
		/// <inheritdoc />
		protected override void Setup(IList<T> list, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			if(list == null && !CanBeNull)
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(setMemberInfo != null, ToString(setLabel)+" initial value and setMemberInfo both null. Figuring out Type is not possible.");
				#endif

				memberInfo = setMemberInfo;
				list = GetFirstFieldOrDefaultValue();
			}

			if(list != null)
			{
				if(list.IsReadOnly)
				{
					setReadOnly = true;
				}
			}
			else if(setMemberInfo != null)
			{
				// prioritize value of LinkedMemberInfo when getting type, so that abstract fields (e.g. IList) can work if a non-abstract instance is provided.
				var type = setMemberInfo.Type;
				if(type == typeof(ReadOnlyCollection<T>))
				{
					setReadOnly = true;
				}
			}
			else
			{
				#if DEV_MODE
				Debug.LogError(GetType().Name+" both value and memberInfo were null!");
				#endif
			}
			
			base.Setup(list, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc />
		protected override void ResizeCollection(ref IList<T> collection, int length)
		{
			int lengthWas = collection.Count;
			var memberType = MemberType;
			
			//if size was reduced, then remove the leftover elements
			for(int n = lengthWas - 1; n >= length; n--)
			{
				collection.RemoveAt(n);
            }

			//if size was increased, then add new elements with default values at the end of the list
			for(int n = lengthWas; n < length; n++)
			{
				collection.Add((T)memberType.DefaultValue());
			}
		}

		/// <inheritdoc />
		protected override int GetCollectionSize(IList<T> collection)
		{
			try
			{
				return collection.Count;
			}
			catch(NullReferenceException e)
			{
				if(collection == null)
				{
					Debug.LogError(ToString()+ ".GetCollectionSize called for null collection!\n"+e);
				}
				else
				{
					Debug.LogError(ToString() + ".GetCollectionSize - NullReferenceException when invoking Count getter for collection of type "+StringUtils.TypeToString(collection)+ "!\n" + e);
				}
				return 0;
			}
		}

		/// <inheritdoc />
		protected override object GetCollectionValue(IList<T> collection, int collectionIndex)
		{
			return collection[collectionIndex];
		}

		/// <inheritdoc />
		protected override void SetCollectionValue(ref IList<T> collection, int collectionIndex, object memberValue)
		{
			#if DEV_MODE
			Debug.Assert(!Value.IsReadOnly);
			Debug.Assert(!ReadOnly);
			#endif

			try
			{
				collection[collectionIndex] = (T)memberValue;
			}
			catch(ArgumentOutOfRangeException e)
			{
				Debug.LogError("SetCollectionValue("+StringUtils.TypeToString(collection)+", "+collectionIndex+ ") with GetCollectionSize="+ GetCollectionSize(collection)+" "+e);
			}
		}

		/// <inheritdoc/>
		protected override GetSize GetGetCollectionSizeDelegate()
		{
			return GetCollectionSizeStatic;
		}

		private static object GetCollectionSizeStatic(object collection)
		{
			return ((IList<T>)collection).Count;
		}

		/// <inheritdoc/>
		protected override SetSize GetSetCollectionSizeDelegate()
		{
			return SetCollectionSizeStatic;
		}

		private static void SetCollectionSizeStatic(ref object collection, object size)
		{
			var list = (IList<T>)collection;

			int sizeWas = list.Count;
			int setSize = (int)size;

			if(sizeWas == setSize)
			{
				#if DEV_MODE
				Debug.LogWarning("List<"+typeof(T).Name+">.SetCollectionSizeStatic("+size+") called but collection size was already "+sizeWas);
				#endif
				return;
			}

			if(setSize < sizeWas)
			{
				//if size was reduced, then remove the leftover elements
				for(int n = sizeWas - 1; n >= setSize; n--)
				{
					list.RemoveAt(n);
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
					//if size was increased, then add new elements with default values at the end of the list
					for(int n = sizeWas; n < setSize; n++)
					{
						list.Add((T)memberType.DefaultValue());
					}
				}
				#if DEV_MODE
				else { Debug.LogError("List<"+typeof(T).Name+">.SetCollectionSizeStatic failed to get memberType of type "+StringUtils.TypeToString(collection)); }
				#endif
			}

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(list.Count == setSize, "List<"+typeof(T).Name+">.SetCollectionSizeStatic("+setSize+") list count was not expected value but "+list.Count);
			Debug.Assert(ReferenceEquals(collection, list));
			#endif
		}

		/// <inheritdoc />
		protected override GetCollectionMember GetGetCollectionMemberValueDelegate()
		{
			return GetCollectionValueStatic;
		}

		private static object GetCollectionValueStatic(object collection, int index)
		{
			return (collection as IList<T>)[index];
		}

		/// <inheritdoc />
		protected override SetCollectionMember GetSetCollectionMemberValueDelegate()
		{
			return SetCollectionValueStatic;
		}

		private static void SetCollectionValueStatic(ref object collection, int index, object memberValue)
		{
			(collection as IList<T>)[index] = (T)memberValue;
		}

		/// <inheritdoc />
		protected override void InsertAt(ref IList<T> collection, int collectionIndex, object memberValue)
		{
			collection.Insert(collectionIndex, (T)memberValue);
		}
		
		/// <inheritdoc />
		protected override void RemoveAt(ref IList<T> collection, int collectionIndex)
		{
			collection.RemoveAt(collectionIndex);
		}

		/// <inheritdoc />
		protected override bool ValuesAreEqual(IList<T> a, IList<T> b)
		{
			if(ReferenceEquals(a, b))
			{
				return true;
			}
			if(ReferenceEquals(a, null) || ReferenceEquals(b, null))
			{
				return false;
			}
			return a.ContentsMatch(b);
		}

		/// <inheritdoc />
		protected override bool Sort(ref IList<T> collection, IComparer comparer)
		{
			int count = collection.Count;
			
			var sortedArray = Array.CreateInstance(MemberType, count) as T[];
			collection.CopyTo(sortedArray, 0);
			Array.Sort(sortedArray, comparer);

			if(!sortedArray.ContentsMatch(collection))
			{
				collection.Clear();
				for(int i = 0; i < count; i++)
				{
					collection.Add(sortedArray[i]);
				}
				return true;
			}
			return false;
		}

		/// <inheritdoc/>
		protected override void OnFieldBackedValueChanged()
		{
			#if UNITY_EDITOR
			//update serialized property so that arraySize property of List is updated to correct value
			UpdateSerializedObject();
			#endif

			base.OnFieldBackedValueChanged();
		}
		
		private static bool TryGetMemberType([NotNull]Type type, [NotNull]out Type listMemberType)
		{
			Type[] genericArguments;
			if(type.TryGetGenericArgumentsFromInterface(Types.IEnumerableGeneric, out genericArguments))
			{
				if(genericArguments.Length == 1)
				{
					listMemberType = genericArguments[0];
					return true;
				}
				#if DEV_MODE
				Debug.LogWarning("List.TryGetGenericArgumentsFromInterface(IEnumerable<>) returned "+genericArguments.Length+" generic arguments for type "+StringUtils.ToStringSansNamespace(type)+": "+StringUtils.ToString(genericArguments));
				#endif
			}
			#if DEV_MODE
			else { Debug.LogWarning("List.TryGetGenericArgumentsFromInterface(IEnumerable<>) failed for type "+StringUtils.ToStringSansNamespace(type)+": "+StringUtils.ToString(genericArguments)); }
			#endif

			listMemberType = Types.SystemObject;
			return false;
		}

		/// <inheritdoc/>
		protected override IList<T> GetCopyOfValue(IList<T> source)
		{
			if(source == null)
			{
				return null;
			}

			var type = source.GetType();
			Type listMemberType;
			if(!TryGetMemberType(type, out listMemberType))
			{
				#if DEV_MODE
				Debug.LogError(ToString()+".GetCopyOfValue failed for source of type "+StringUtils.TypeToString(source));
				#endif
				return source;
			}

			var parameterType = typeof(IEnumerable<>).MakeGenericType(listMemberType);
			var parameterTypes = ArrayExtensions.TempTypeArray(parameterType);
			var constructor = type.GetConstructor(parameterTypes);
			if(constructor != null)
			{
				var parameterValues = ArrayExtensions.TempObjectArray(source);
				var result = (IList<T>)constructor.Invoke(parameterValues);

				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(result.Count == source.Count);
				Debug.Assert(result.ContentsMatch(source));
				#endif

				return result;
			}

			var instance = type.DefaultValue() as IList<T>;
			if(instance != null)
			{
				for(int n = 0, count = source.Count; n < count; n++)
				{
					instance.Add(source[n]);
				}
				return instance;
			}
			
			#if DEV_MODE
			Debug.LogError(ToString()+".GetCopyOfValue Failed to find List constructor that takes "+StringUtils.ToString(parameterType)+" as parameter.");
			#endif

			return source;
		}

		/// <inheritdoc />
		protected override bool CollectionContains(IList<T> collection, object value)
		{
			try
			{
				return collection == null ? false : collection.Contains((T)value);
			}
			#if DEV_MODE
			catch(InvalidCastException e)
			{
				Debug.LogWarning(ToString()+".CollectionContains - could not cast value of type "+StringUtils.TypeToString(value)+" to "+ StringUtils.TypeToString(typeof(T))+": "+e);
			#else
			catch(InvalidCastException)
			{
			#endif
				return false;
			}
		}
	}
}