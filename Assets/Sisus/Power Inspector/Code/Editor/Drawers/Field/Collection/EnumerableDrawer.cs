#define SAFE_MODE

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace Sisus
{
    [Serializable] //, DrawerForField(typeof(IEnumerable<>), true, true)]
	public abstract class EnumerableDrawer<T> : OneDimensionalCollectionDrawer<IEnumerable<T>>
	{
		/// <inheritdoc />
		protected override bool IsFixedSize
		{
			get
			{
				return true;
			}
		}

		/// <inheritdoc />
		protected override bool IsReadOnlyCollection
		{
			get
			{
				return true;
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
		/// <param name="enumerable"> The starting cached value of the drawer. </param>
		/// <param name="memberInfo"> LinkedMemberInfo for the field, property or parameter that the drawer represents. Can be null. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="readOnly"> True if control should be read only. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static TEnumerableDrawer Create<TEnumerableDrawer>(IEnumerable<T> enumerable, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label) where TEnumerableDrawer : EnumerableDrawer<T>, new()
		{
			TEnumerableDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new TEnumerableDrawer();
			}
			result.Setup(enumerable, DrawerUtility.GetType(memberInfo, enumerable), memberInfo, parent, label, true);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc />
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			Setup(setValue as IEnumerable<T>, setValueType, setMemberInfo, setParent, setLabel, true);
		}
		
		/// <inheritdoc />
		protected override void Setup(IEnumerable<T> enumerable, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			if(enumerable == null && !CanBeNull)
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(setMemberInfo != null, ToString(setLabel)+" initial value and setMemberInfo both null. Figuring out Type is not possible.");
				#endif

				memberInfo = setMemberInfo;
				enumerable = GetFirstFieldOrDefaultValue();
			}
			
			base.Setup(enumerable, setValueType, setMemberInfo, setParent, setLabel, true);
		}

		/// <inheritdoc />
		protected override void ResizeCollection(ref IEnumerable<T> collection, int length)
		{
			Debug.LogWarning("Can't resize IEnumerable");
		}

		/// <inheritdoc />
		protected override int GetCollectionSize(IEnumerable<T> collection)
		{
			if(collection == null)
            {
				return 0;
            }
			return collection.Count();
		}

		/// <inheritdoc />
		protected override object GetCollectionValue(IEnumerable<T> collection, int collectionIndex)
		{
			foreach(var item in collection)
			{
				if(collectionIndex == 0)
				{
					return item;
				}
				collectionIndex--;
			}
			Debug.LogWarning("Index out of range of IEnumerable size: " + collectionIndex);
			return null;
		}

		/// <inheritdoc />
		protected override void SetCollectionValue(ref IEnumerable<T> collection, int collectionIndex, object memberValue)
		{
			Debug.LogWarning("Can't set value of IEnumerable element");
		}

		/// <inheritdoc/>
		protected override GetSize GetGetCollectionSizeDelegate()
		{
			return GetCollectionSizeStatic;
		}

		private static object GetCollectionSizeStatic(object collection)
		{
			return ((IEnumerable<T>)collection).Count();
		}

		/// <inheritdoc/>
		protected override SetSize GetSetCollectionSizeDelegate()
		{
			return SetCollectionSizeStatic;
		}

		private static void SetCollectionSizeStatic(ref object collection, object size)
		{
			Debug.LogWarning("Can't set size of IEnumerable");
		}

		/// <inheritdoc />
		protected override GetCollectionMember GetGetCollectionMemberValueDelegate()
		{
			return GetCollectionValueStatic;
		}

		private static object GetCollectionValueStatic(object collection, int index)
		{
			foreach(var item in collection as IEnumerable)
            {
				if(index == 0)
                {
					return item;
                }
				index--;
            }

			Debug.LogWarning("Index out of range of IEnumerable size: "+index);
			return null;
		}

		/// <inheritdoc />
		protected override SetCollectionMember GetSetCollectionMemberValueDelegate()
		{
			return SetCollectionValueStatic;
		}

		private static void SetCollectionValueStatic(ref object collection, int index, object memberValue)
		{
			Debug.LogWarning("Can't set value of IEnumerable");
		}

		/// <inheritdoc />
		protected override void InsertAt(ref IEnumerable<T> collection, int collectionIndex, object memberValue)
		{
			Debug.LogWarning("Can't use InsertAt with IEnumerable");
		}
		
		/// <inheritdoc />
		protected override void RemoveAt(ref IEnumerable<T> collection, int collectionIndex)
		{
			Debug.LogWarning("Can't use RemoveAt with IEnumerable");
		}

		/// <inheritdoc />
		protected override bool ValuesAreEqual(IEnumerable<T> a, IEnumerable<T> b)
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
		protected override bool Sort(ref IEnumerable<T> collection, IComparer comparer)
		{
			return false;
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
		protected override IEnumerable<T> GetCopyOfValue(IEnumerable<T> source)
		{
			return source;
		}

		/// <inheritdoc />
		protected override bool CollectionContains(IEnumerable<T> collection, object value)
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