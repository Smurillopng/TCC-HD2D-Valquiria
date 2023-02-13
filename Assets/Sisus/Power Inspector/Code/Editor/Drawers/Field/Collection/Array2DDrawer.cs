#define DEBUG_NULL_MEMBERS

#define DEBUG_CREATE_MEMBERS
#define DEBUG_RESIZE

#define SAFE_MODE

using System;
using System.Collections;
using UnityEngine;

namespace Sisus
{
	[Serializable]
	public class Array2DDrawer : CollectionDrawer<Array, Size2D, Xy>
	{
		/// <inheritdoc />
		protected override bool IsFixedSize
		{
			get
			{
				return true;
			}
		}

		/// <summary>
		/// Returns the one-dimensional equivalent of the two-dimensional Array's type,
		/// i.e. the type of the single-dimensional Arrays that are contained inside
		/// the two-dimensional Array. E.g. If Array type is T[,] than returns T[].
		/// </summary>
		/// <value>
		/// The one-dimensional equivalent of the two-dimensional Array's type
		/// </value>
		protected override Type MemberType
		{
			get
			{
				return Type.GetElementType();
			}
		}

		/// <inheritdoc />
		protected override int Rank
		{
			get
			{
				return 2;
			}
		}
		
		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="array"> The starting cached value of the drawer. </param>
		/// <param name="memberInfo"> LinkedMemberInfo for the field, property or parameter that the drawer represents. Can be null. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="readOnly"> True if control should be read only. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static Array2DDrawer Create(Array array, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool readOnly)
		{
			Array2DDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new Array2DDrawer();
			}
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(DrawerUtility.GetType(memberInfo, array).GetArrayRank() == 2);
			#endif
			result.Setup(array, DrawerUtility.GetType(memberInfo, array), memberInfo, parent, label, readOnly);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc />
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(DrawerUtility.GetType(setMemberInfo, setValue).GetArrayRank() == 2);
			#endif

			Setup(setValue as Array, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc />
		protected override int GetElementCount(Size2D collectionIndex)
		{
			return collectionIndex.Count;
		}

		/// <inheritdoc />
		protected override IDrawer BuildResizeField()
		{
			var resizerMemberInfo = ResizerMemberInfo;
			return Size2DDrawer.Create(size.IsValid() ? size : Size2D.Zero, resizerMemberInfo, this, GUIContentPool.Create("Sizes"), ReadOnly || resizerMemberInfo == null);
		}

		/// <inheritdoc />
		protected override Xy GetCollectionIndex(int flattenedIndex)
		{
			return size.Get2DIndex(flattenedIndex);
		}

		/// <inheritdoc />
		protected override void ResizeCollection(ref Array collection, Size2D setSize)
		{
			if(ReadOnly)
			{
				return;
			}

			int elementCountWas = collection.Length;
			var memberType = MemberType;

			var result = Array.CreateInstance(memberType, setSize.height, setSize.width);
			
			var lastIndexWas = GetCollectionIndex(elementCountWas);
			int setElementcount = setSize.Count;

			//fill in old array values
			if(elementCountWas > 0)
			{
				int stopX = Mathf.Min(lastIndexWas.x, setSize.height);
				int stopY = Mathf.Min(lastIndexWas.y, setSize.width);
				for(int y = stopY - 1; y >= 0; y--)
				{
					for(int x = stopX - 1; x >= 0; x--)
					{
						result.SetValue(collection.GetValue(x, y), x, y);
					}
				}
			}

			//if array size was increased, then fill the new elements with default values
			if(setElementcount > elementCountWas)
			{
				for(int y = lastIndexWas.y; y < setSize.width; y++)
				{
					for(int x = lastIndexWas.x; x < setSize.height; x++)
					{
						result.SetValue(memberType.DefaultValue(), x, y);
					}
				}
			}

			collection = result;
		}

		/// <inheritdoc />
		protected override Size2D GetCollectionSize(Array array2D)
		{
			#if SAFE_MODE || DEV_MODE
			if(array2D == null)
			{
				#if DEV_MODE
				Debug.LogWarning(ToString()+"GetCollectionSize was called for null collection!");
				#endif
				return Size2D.Zero;
			}
			#endif

			return new Size2D(array2D);
		}

		/// <inheritdoc />
		protected override object GetCollectionValue(Array array2D, Xy collectionIndex)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			if(array2D.Height() <= collectionIndex.x)
			{
				throw new IndexOutOfRangeException("GetCollectionValue("+collectionIndex+") array2D Height ("+array2D.Height()+") < index.x ("+collectionIndex.x+") for array: "+StringUtils.ToString(array2D));
			}
			if(array2D.Width() <= collectionIndex.y)
			{
				throw new IndexOutOfRangeException("GetCollectionValue("+collectionIndex+") array2D Width ("+array2D.Width()+") < index.y ("+collectionIndex.y+") for array: "+StringUtils.ToString(array2D));
			}
			#endif

			return array2D.GetValue(collectionIndex);
		}

		/// <inheritdoc />
		protected override void SetCollectionValue(ref Array array2D, Xy collectionIndex, object memberValue)
		{
			array2D.SetValue(memberValue, collectionIndex.x, collectionIndex.y);
		}

		/// <inheritdoc />
		protected override GetCollectionMember GetGetCollectionMemberValueDelegate()
		{
			return GetCollectionValueStatic;
		}

		private static object GetCollectionValueStatic(object collection, int flattenedIndex)
		{
			var array = collection as Array;
			if(array == null)
			{
				#if DEV_MODE
				Debug.LogError("GetCollectionValueStatic: returning null because target collection was null!");
				#endif
				return null;
			}
			
			var index = array.FlatTo2DIndex(flattenedIndex);

			#if DEV_MODE
			Debug.Log(StringUtils.ToColorizedString("flattenedIndex ", flattenedIndex, " => (", index.x, ", ", index.y, ") with with GetLength(0)=", array.GetLength(0), ", GetLength(1)=", array.GetLength(1)));
			#endif

			try
			{
				return array.GetValue(index.x, index.y);
			}
			catch(IndexOutOfRangeException)
			{
				#if DEV_MODE
				Debug.LogError("GetCollectionValueStatic("+flattenedIndex+") IndexOutOfRangeException with index="+index);
				#endif
				return collection.GetType().GetElementType().DefaultValue();
			}
		}

		/// <inheritdoc/>
		protected override GetSize GetGetCollectionSizeDelegate()
		{
			return GetCollectionSizeStatic;
		}

		private static object GetCollectionSizeStatic(object collection)
		{
			return new Size2D((Array)collection);
		}

		/// <inheritdoc/>
		protected override SetSize GetSetCollectionSizeDelegate()
		{
			return SetCollectionSizeStatic;
		}

		private static void SetCollectionSizeStatic(ref object collection, object size)
		{
			var array = (Array)collection;
			var sizeWas = new Size2D(array);
			var setSize = (Size2D)size;

			if(sizeWas.Equals(setSize))
			{
				#if DEV_MODE
				Debug.LogWarning("Array2D.SetCollectionSizeStatic("+setSize+") called but collection size was already "+sizeWas);
				#endif
				return;
			}

			var memberType = collection.GetType().GetElementType();

			var result = Array.CreateInstance(memberType, setSize[0], setSize[1]);

			var countWas = sizeWas.Count;
			var setCount = setSize.Count;
			
			// fill in old array values
			if(countWas > 0)
			{
				Array.Copy(array, result, Math.Min(countWas, setCount));
			}
			
			// fill the new elements with default values
			for(int index1 = sizeWas[0]; index1 < setSize[0]; index1++)
			{
				for(int index2 = sizeWas[1]; index2 < setSize[1]; index2++)
				{
					array.SetValue(memberType.DefaultValue(), index1, index2);
				}
			}

			collection = result;
		}

		/// <inheritdoc />
		protected override SetCollectionMember GetSetCollectionMemberValueDelegate()
		{
			return SetCollectionValueStatic;
		}

		private static void SetCollectionValueStatic(ref object collection, int flattenedIndex, object memberValue)
		{
			var array = collection as Array;
			if(array == null)
			{
				throw new NullReferenceException();
			}

			var index = array.FlatTo2DIndex(flattenedIndex);

			#if DEV_MODE
			Debug.Log(StringUtils.ToColorizedString("flattenedIndex ", flattenedIndex, " => (", index.x, ", ", index.y, ")"));
			#endif

			array.SetValue(memberValue, index.x, index.y);
		}

		/// <inheritdoc />
		protected override void InsertAt(ref Array array2D, Xy collectionIndex, object memberValue)
		{
			InspectorUtility.ActiveInspector.Message("Can't insert a single value into a 2-dimensional array. Use resize instead.", null, MessageType.Error);

			//InsertAt doesn't really work for a 2D array with a single value (would need a whole row or column of data),
			//So simply using SetCollectionValue instead
			SetCollectionValue(ref array2D, collectionIndex, memberValue);
		}

		/// <inheritdoc />
		protected override void RemoveAt(ref Array array2D, Xy collectionIndex)
		{
			InspectorUtility.ActiveInspector.Message("Can't delete a single value from a 2-dimensional array. Use resize instead.", null, MessageType.Error);

			//RemoveAt doesn't really work for a 2D array with a single value (would need to remove a whole row or column of data),
			//So simply setting value at collectionIndex to default value instead.
			SetCollectionValue(ref array2D, collectionIndex, MemberDefaultValue());
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
			var array1D = collection.MakeOneDimensional();
			Array.Sort(array1D);
			var sorted = array1D.MakeTwoDimensional(collection.GetLength(0), collection.GetLength(1));
			if(!sorted.ContentsMatch(collection))
			{
				collection = sorted;
				return true;
			}
			return false;
		}

		/// <inheritdoc />
		protected override GUIContent GetIndexBasedLabelForMember(Xy collectionIndex)
		{
			return GUIContentPool.Create(StringUtils.Concat("[", collectionIndex.x, ",", collectionIndex.y, "]"));
		}

		/// <inheritdoc/>
		protected override Array GetCopyOfValue(Array source)
		{
			if(source == null)
			{
				return null;
			}

			int height = source.Height();
			int width = source.Width();
			var copy = Array.CreateInstance(source.GetType().GetElementType(), height, width);
			Array.Copy(source, copy, width * height);
			return copy;
		}

		/// <inheritdoc />
		protected override bool CollectionContains(Array collection, object value)
		{
			return collection == null ? false : Array.IndexOf(collection, value) != -1;
		}
	}
}