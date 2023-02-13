#define SAFE_MODE

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using Sisus.Attributes;
using UnityEngine;

namespace Sisus
{
    [Serializable, DrawerForField(typeof(Stack<>), true, true)]
	public class StackDrawer<T> : OneDimensionalCollectionDrawer<Stack<T>>
	{
		/// <inheritdoc />
		protected override bool IsFixedSize
		{
			get
			{
				return false;
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
		/// <param name="stack"> The starting cached value of the drawer. </param>
		/// <param name="memberInfo"> LinkedMemberInfo for the field, property or parameter that the drawer represents. Can be null. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="readOnly"> True if control should be read only. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static StackDrawer<T> Create(Stack<T> stack, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool readOnly)
		{
			StackDrawer<T> result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new StackDrawer<T>();
			}
			result.Setup(stack, typeof(Stack<T>), memberInfo, parent, label, readOnly);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc />
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			Setup(setValue as Stack<T>, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}
		
		/// <inheritdoc />
		protected override void Setup(Stack<T> stack, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			if(stack == null && !CanBeNull)
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(setMemberInfo != null, ToString(setLabel)+" initial value and setMemberInfo both null. Figuring out Type is not possible.");
				#endif

				memberInfo = setMemberInfo;
				stack = GetFirstFieldOrDefaultValue();
			}
			
			base.Setup(stack, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc />
		protected override void ResizeCollection(ref Stack<T> collection, int length)
		{
			int lengthWas = collection.Count;

			//if size was reduced, then remove the leftover elements
			for(int n = lengthWas - 1; n >= length; n--)
			{
				collection.Pop();
			}

			//if size was increased, then add new elements with default values at the end of the stack
			for(int n = lengthWas; n < length; n++)
			{
				collection.Push((T)typeof(T).DefaultValue());
			}
		}
		
		/// <inheritdoc />
		protected override void RemoveAt(ref Stack<T> collection, int collectionIndex)
		{
			int countWas = collection.Count;
			var array = (T[])Array.CreateInstance(typeof(T), countWas);
			collection.CopyTo(array, 0);
			
			collection.Clear();

			for(int n = 0; n < collectionIndex; n++)
			{
				collection.Push(array[n]);
			}
			for(int n = collectionIndex + 1; n < countWas; n++)
			{
				collection.Push(array[n]);
			}
		}

		/// <inheritdoc />
		protected override int GetCollectionSize(Stack<T> collection)
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
		protected override object GetCollectionValue(Stack<T> collection, int collectionIndex)
		{
			int currentIndex = 0;
			foreach(var item in collection)
			{		
				if(currentIndex == collectionIndex)
				{
					return item;
				}
				currentIndex++;
			}
			throw new IndexOutOfRangeException();
		}

		/// <inheritdoc />
		protected override void SetCollectionValue(ref Stack<T> collection, int collectionIndex, object memberValue)
		{
			object stack = collection;
			SetCollectionValueStatic(ref stack, collectionIndex, memberValue);
		}

		/// <inheritdoc/>
		protected override GetSize GetGetCollectionSizeDelegate()
		{
			return GetCollectionSizeStatic;
		}

		private static object GetCollectionSizeStatic(object collection)
		{
			return ((Stack<T>)collection).Count;
		}

		/// <inheritdoc/>
		protected override SetSize GetSetCollectionSizeDelegate()
		{
			return SetCollectionSizeStatic;
		}

		private static void SetCollectionSizeStatic([CanBeNull]ref object collection, object size)
		{
			var stack = (Stack<T>)collection;

			int sizeWas = stack.Count;
			int setSize = (int)size;

			//if size was reduced, then remove the leftover elements
			if(setSize < sizeWas)
			{
				var popMethod = typeof(Stack<T>).GetMethod("Pop", BindingFlags.Public | BindingFlags.Instance);
				for(int n = sizeWas - 1; n >= setSize; n--)
				{
					popMethod.Invoke(collection);
				}
			}
			//if size was increased, then add new elements with default values at the end of the stack
			else if(setSize > sizeWas)
			{
				if(collection == null)
				{
					collection = typeof(Stack<T>).CreateInstance(Types.Int, setSize);
				}

				var pushMethod = typeof(Stack<T>).GetMethod("Push", BindingFlags.Public | BindingFlags.Instance);
				for(int n = sizeWas; n < setSize; n++)
				{
					pushMethod.InvokeWithParameter(collection, (T)typeof(T).DefaultValue());
				}
			}
			#if DEV_MODE
			else { Debug.LogWarning("Stack<"+typeof(T).Name+">.SetCollectionSizeStatic("+size+") called but collection size was already "+sizeWas); }
			#endif
		}

		/// <inheritdoc />
		protected override GetCollectionMember GetGetCollectionMemberValueDelegate()
		{
			return GetCollectionValueStatic;
		}

		private static object GetCollectionValueStatic(object collection, int index)
		{
			int currentIndex = 0;
			foreach(var item in (Stack<T>)collection)
			{
				if(currentIndex == index)
				{
					return item;
				}
				currentIndex++;
			}
			throw new IndexOutOfRangeException();
		}

		/// <inheritdoc />
		protected override SetCollectionMember GetSetCollectionMemberValueDelegate()
		{
			return SetCollectionValueStatic;
		}

		private static void SetCollectionValueStatic(ref object collection, int index, object memberValue)
		{
			var stack = (Stack<T>)collection;
			int countWas = stack.Count;
			var array = (T[])Array.CreateInstance(typeof(T), countWas);
			stack.CopyTo(array, 0);

			typeof(Stack<T>).GetMethod("Clear", BindingFlags.Public | BindingFlags.Instance).Invoke(stack);

			var pushMethod = typeof(Stack<T>).GetMethod("Push", BindingFlags.Public | BindingFlags.Instance);

			for(int n = 0; n < index; n++)
			{
				pushMethod.InvokeWithParameter(stack, array[n]);
			}
			pushMethod.InvokeWithParameter(stack, (T)memberValue);
			for(int n = index + 1; n < countWas; n++)
			{
				pushMethod.InvokeWithParameter(stack, array[n]);
			}
		}

		/// <inheritdoc />
		protected override void InsertAt(ref Stack<T> collection, int collectionIndex, object memberValue)
		{
			int countWas = GetCollectionSize(collection);
			var array = (T[])Array.CreateInstance(typeof(T), countWas);
			collection.CopyTo(array, 0);

			collection.Clear();

			for(int n = 0; n < collectionIndex; n++)
			{
				collection.Push(array[n]);
			}

			collection.Push((T)memberValue);

			for(int n = collectionIndex; n < countWas; n++)
			{
				collection.Push(array[n]);
			}
		}

		/// <inheritdoc />
		protected override bool ValuesAreEqual(Stack<T> a, Stack<T> b)
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
		protected override bool Sort(ref Stack<T> collection, IComparer comparer)
		{
			int count = collection.Count;
			
			var sortedArray = (T[])Array.CreateInstance(MemberType, count);
			collection.CopyTo(sortedArray, 0);
			Array.Sort(sortedArray, comparer);

			if(!sortedArray.ContentsMatch(collection))
			{
				collection.Clear();

				for(int i = 0; i < count; i++)
				{
					collection.Push(sortedArray[i]);
				}
				return true;
			}
			return false;
		}

		/// <inheritdoc/>
		protected override void OnFieldBackedValueChanged()
		{
			#if UNITY_EDITOR
			//update serialized property so that arraySize property of Stack is updated to correct value
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
				Debug.LogWarning("Stack.TryGetGenericArgumentsFromInterface(IEnumerable<>) returned "+genericArguments.Length+" generic arguments for type "+StringUtils.ToStringSansNamespace(type)+": "+StringUtils.ToString(genericArguments));
				#endif
			}
			#if DEV_MODE
			else { Debug.LogWarning("Stack.TryGetGenericArgumentsFromInterface(IEnumerable<>) failed for type "+StringUtils.ToStringSansNamespace(type)+": "+StringUtils.ToString(genericArguments)); }
			#endif

			listMemberType = Types.SystemObject;
			return false;
		}

		/// <inheritdoc/>
		protected override Stack<T> GetCopyOfValue(Stack<T> source)
		{
			if(source == null)
			{
				return null;
			}
			return new Stack<T>(source);
		}

		/// <inheritdoc />
		protected override bool CollectionContains(Stack<T> collection, object value)
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