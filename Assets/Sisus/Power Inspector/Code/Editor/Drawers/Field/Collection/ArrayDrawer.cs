#define DEBUG_NULL_MEMBERS
#define DEBUG_RESIZE
//#define DEBUG_CREATE_MEMBERS

#define SAFE_MODE

using System;
using System.Collections;
using Sisus.Attributes;
using JetBrains.Annotations;
using UnityEngine;

namespace Sisus
{
	[DrawerForField(typeof(Array), true, true)]
	public class ArrayDrawer : OneDimensionalCollectionDrawer<Array>
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
		protected override Type MemberType
		{
			get
			{
				return Type.GetElementType();
			}
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="value"> The starting cached value of the drawer. </param>
		/// <param name="memberInfo"> LinkedMemberInfo for the field, property or parameter that the drawer represents. Can be null. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="readOnly"> True if control should be read only. </param>
		/// <returns>
		/// The instance, ready to be used. If value is null and can't generate an instance based on memberInfo (e.g. because member type is
		/// a generic type definition), then returns null.  </returns>
		[CanBeNull]
		public static ICollectionDrawer Create(Array value, LinkedMemberInfo memberInfo, [CanBeNull]IParentDrawer parent, GUIContent label, bool readOnly)
		{
			if(value == null && memberInfo != null && !memberInfo.CanBeNull)
			{
				#if DEV_MODE
				Debug.Assert(memberInfo != null, "Array.Create - both value and memberInfo null!");
				#endif

				value = memberInfo.DefaultValue() as Array;
				if(value == null)
				{
					#if DEV_MODE
					Debug.LogError("Array.Create("+ memberInfo+") of type "+StringUtils.ToString(memberInfo.Type)+": value was null and memberInfo.DefaultValue() returned null!");
					#endif
					return null;
				}

				if(memberInfo.CanWrite)
				{
					memberInfo.SetValue(value);
				}

				
				memberInfo.Type.GetArrayRank();
			}

			var type = DrawerUtility.GetType(memberInfo, value);
			switch(type.GetArrayRank())
			{
				case 1:
					break;
				case 2:
					return Array2DDrawer.Create(value, memberInfo, parent, label, readOnly);
				case 3:
					return Array3DDrawer.Create(value, memberInfo, parent, label, readOnly);
				default:
					return null;
			}
			
			ArrayDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new ArrayDrawer();
			}
			result.Setup(value, type, memberInfo, parent, label, readOnly);
			result.LateSetup();
			return result;
		}
		
		/// <inheritdoc />
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			var array = setValue as Array;

			#if DEV_MODE && PI_ASSERTATIONS
			var type = DrawerUtility.GetType(setMemberInfo, array);	
			Debug.Assert(type != null, "ArrayDrawer - failed to figure out type from value / member info.");
			var typeFullName = type.FullName;
			Debug.Assert(type.IsArray, typeFullName + " not an array.");
			Debug.Assert(type.GetArrayRank() == 1, typeFullName + " array rank not 1.");
			#endif
			Setup(array, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}
		
		/// <inheritdoc />
		protected override void ResizeCollection(ref Array collection, int length)
		{
			if(ReadOnly)
			{
				#if DEV_MODE
				Debug.LogWarning(ToString()+".ResizeCollection called but ReadOnly was "+StringUtils.True);
				#endif
				return;
			}

			int lengthWas = collection == null ? 0 : collection.Length;
			var memberType = MemberType;

			var result = Array.CreateInstance(memberType, length);

			//fill in old array values
			if(lengthWas > 0)
			{
				Array.Copy(collection, result, Math.Min(lengthWas, length));
			}

			//if array size was increased, then fill the new elements with default values
			if(length > lengthWas)
			{
				for(int n = lengthWas; n < length; n++)
				{
					result.SetValue(memberType.DefaultValue(), n);
				}
			}

			collection = result;
		}
		
		/// <inheritdoc />
		protected override int GetCollectionSize([CanBeNull]Array collection)
		{
			return collection == null ? 0 : collection.Length;
		}

		/// <inheritdoc />
		protected override object GetCollectionValue(Array collection, int index)
		{
			if(collection == null)
			{
				return 0;
			}

			if(index < 0 || index >= collection.Length)
			{
				#if DEV_MODE
				Debug.LogError(ToString()+ ".GetCollectionValue("+index+") OutOfRangeException with array.Length="+collection.Length);
				#endif
				return 0;
			}

			return collection.GetValue(index);
		}
		
		/// <inheritdoc />
		protected override void SetCollectionValue(ref Array collection, int index, object memberValue)
		{
			try
			{
				collection.SetValue(memberValue, index);
			}
			#if DEV_MODE
			catch(IndexOutOfRangeException)
			{
				Debug.LogError(ToString()+ ".SetCollectionValue("+index+") OutOfRangeException with array.Length="+collection.Length);
			#else
			catch(IndexOutOfRangeException)
			{
			#endif
				return;
			}
			#if DEV_MODE
			catch(NullReferenceException)
			{
				Debug.LogError(ToString()+ ".SetCollectionValue called for null collection!");
			#else
			catch(NullReferenceException)
			{
			#endif
				return;
			}
		}

		/// <inheritdoc/>
		protected override GetSize GetGetCollectionSizeDelegate()
		{
			return GetCollectionSizeStatic;
		}

		private static object GetCollectionSizeStatic([CanBeNull]object collection)
		{
			if(collection == null)
			{
				return -1;
			}
			return ((Array)collection).Length;
		}

		/// <inheritdoc/>
		protected override SetSize GetSetCollectionSizeDelegate()
		{
			return SetArraySizeStatic;
		}

		private static void SetArraySizeStatic([CanBeNull]ref object collection, object size)
		{
			#if DEV_MODE && DEBUG_RESIZE
			Debug.Log("SetCollectionSizeStatic("+size+"): "+StringUtils.ToString(collection));
			#endif

			var array = (Array)collection;

			bool wasNull = array == null;
			int sizeWas = wasNull ? - 1 : array.Length;
			int setSize = (int)size;

			if(sizeWas == setSize)
			{
				#if DEV_MODE
				Debug.LogWarning("ArrayDrawer.SetArraySizeStatic(" + setSize+") called but collection size was already "+sizeWas);
				#endif
				return;
			}

			if(wasNull)
			{
				#if DEV_MODE
				Debug.LogError("ArrayDrawer.SetArraySizeStatic(" + setSize + ") can't figure out array type because existing instance was null.");
				#endif
				return;
			}

			var memberType = collection.GetType().GetElementType();

			var result = Array.CreateInstance(memberType, setSize);

			// fill in old array values
			if(sizeWas > 0)
			{
				Array.Copy(array, result, Math.Min(sizeWas, setSize));
			}

			//if array size was increased, then fill the new elements with default values
			if(setSize > sizeWas)
			{
				for(int n = sizeWas; n < setSize; n++)
				{
					result.SetValue(memberType.DefaultValue(), n);
				}
			}

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(result.Length == setSize, "Array.SetCollectionSizeStatic("+size+") resulting array length was not expected value but "+result.Length);
			#endif

			collection = result;
		}

		/// <inheritdoc />
		protected override GetCollectionMember GetGetCollectionMemberValueDelegate()
		{
			return GetCollectionValueStatic;
		}

		private static object GetCollectionValueStatic(object collection, int index)
		{
			return (collection as Array).GetValue(index);
		}

		/// <inheritdoc />
		protected override SetCollectionMember GetSetCollectionMemberValueDelegate()
		{
			return SetCollectionValueStatic;
		}

		private static void SetCollectionValueStatic(ref object collection, int index, object memberValue)
		{
			(collection as Array).SetValue(memberValue, index);
		}

		/// <inheritdoc />
		protected override void InsertAt(ref Array collection, int index, object memberValue)
		{
			collection = collection.InsertAt(index, memberValue);
		}
		
		/// <inheritdoc />
		protected override void RemoveAt(ref Array collection, int index)
		{
			ArrayExtensions.RemoveAt(ref collection, index);
		}

		/// <inheritdoc />
		protected override bool ValuesAreEqual(Array a, Array b)
		{
			if(ReferenceEquals(a, b))
			{
				return true;
			}
			if(ReferenceEquals(a, null) || ReferenceEquals(b, null))
			{
				return false;
			}
			
			if(isUnityObjectCollection)
			{
				var aObjs = (UnityEngine.Object[])a;
				var bObjs = (UnityEngine.Object[])b;
				return aObjs.ContentsMatch(bObjs);
			}
			return a.ContentsMatch(b);
		}

		/// <inheritdoc />
		protected override bool Sort(ref Array collection, IComparer comparer)
		{
			//created sorted copy of the array
			int count = GetCollectionSize(collection);
			var sorted = Array.CreateInstance(MemberType, count);
			Array.Copy(collection, sorted, count);
			Array.Sort(sorted, comparer);

			if(!sorted.ContentsMatch(collection))
			{
				//set the collection to refer to the sorted array
				collection = sorted;
				return true;
			}
			return false;
		}

		/// <inheritdoc/>
		protected override void OnFieldBackedValueChanged()
		{
			#if UNITY_EDITOR
			//update serialized property so that arraySize property of Array is updated to correct value
			UpdateSerializedObject();
			#endif

			base.OnFieldBackedValueChanged();
		}

		/// <inheritdoc/>
		protected override Array GetCopyOfValue(Array source)
		{
			if(source == null)
			{
				return null;
			}
			int length = source.Length;
			var copy = Array.CreateInstance(source.GetType().GetElementType(), length);
			Array.Copy(source, copy, length);
			return copy;
		}

		/// <inheritdoc />
		protected override bool CollectionContains(Array collection, object value)
		{
			return collection == null ? false : Array.IndexOf(collection, value) != -1;
		}
	}
}