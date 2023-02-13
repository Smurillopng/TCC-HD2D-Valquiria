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
	public class Array3DDrawer : CollectionDrawer<Array, Size3D, Xyz>
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
		/// Returns the one-dimensional equivalent of the three-dimensional Array's type,
		/// i.e. the type of the single-dimensional Arrays that are contained inside
		/// the three-dimensional Array. E.g. If Array type is T[,] than returns T[].
		/// </summary>
		/// <value>
		/// The one-dimensional equivalent of the three-dimensional Array's type
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
				return 3;
			}
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="array"> The starting cached value of the drawer. </param>
		/// <param name="memberInfo"> LinkedMemberInfo for the field, property or parameter that the drawer represents. Can be null. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="readOnly"> True if control should be read only. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static Array3DDrawer Create(Array array, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool readOnly)
		{
			Array3DDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new Array3DDrawer();
			}
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(DrawerUtility.GetType(memberInfo, array).GetArrayRank() == 3);
			#endif
			result.Setup(array, DrawerUtility.GetType(memberInfo, array), memberInfo, parent, label, readOnly);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc />
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(DrawerUtility.GetType(setMemberInfo, setValue).GetArrayRank() == 3);
			#endif
			Setup(setValue as Array, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc />
		protected override int GetElementCount(Size3D collectionIndex)
		{
			return collectionIndex.Count;
		}

		/// <inheritdoc />
		protected override IDrawer BuildResizeField()
		{
			var resizerMemberInfo = ResizerMemberInfo;
			return Size3DDrawer.Create(size.IsValid() ? size : Size3D.Zero, resizerMemberInfo, this, GUIContentPool.Create("Sizes"), ReadOnly || resizerMemberInfo == null);
		}

		/// <inheritdoc />
		protected override Xyz GetCollectionIndex(int flattenedIndex)
		{
			return size.Get3DIndex(flattenedIndex);
		}

		/// <inheritdoc />
		protected override void ResizeCollection(ref Array collection, Size3D setSize)
		{
			if(ReadOnly)
			{
				return;
			}

			int elementCountWas = collection.Length;
			var memberType = MemberType;

			var result = Array.CreateInstance(memberType, setSize.height, setSize.width, setSize.depth);
			
			var lastIndexWas = GetCollectionIndex(elementCountWas);
			int setElementcount = setSize.Count;

			//fill in old array values
			if(elementCountWas > 0)
			{
				int stopX = Mathf.Min(lastIndexWas.x, setSize.height);
				int stopY = Mathf.Min(lastIndexWas.y, setSize.width);
				int stopZ = Mathf.Min(lastIndexWas.z, setSize.depth);
				for(int z = stopZ - 1; z >= 0; z--)
				{
					for(int y = stopY - 1; y >= 0; y--)
					{
						for(int x = stopX - 1; x >= 0; x--)
						{
							result.SetValue(collection.GetValue(x, y, z), x, y, z);
						}
					}
				}
			}

			//if array size was increased, then fill the new elements with default values
			if(setElementcount > elementCountWas)
			{
				for(int z = lastIndexWas.y; z < setSize.depth; z++)
				{
					for(int y = lastIndexWas.y; y < setSize.width; y++)
					{
						for(int x = lastIndexWas.x; x < setSize.height; x++)
						{
							result.SetValue(memberType.DefaultValue(), x, y, z);
						}
					}
				}
			}

			collection = result;
		}

		/// <inheritdoc />
		protected override Size3D GetCollectionSize(Array array3D)
		{
			#if SAFE_MODE || DEV_MODE
			if(array3D == null)
			{
				#if DEV_MODE
				Debug.LogWarning(ToString()+"GetCollectionSize was called for null collection!");
				#endif
				return Size3D.Zero;
			}
			#endif

			return new Size3D(array3D);
		}

		/// <inheritdoc />
		protected override object GetCollectionValue(Array array3D, Xyz collectionIndex)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			if(array3D.Height() <= collectionIndex.x)
			{
				throw new IndexOutOfRangeException("GetCollectionValue array3D Height ("+array3D.Height()+") < index.x ("+collectionIndex.x+") for array: "+StringUtils.ToString(array3D));
			}
			if(array3D.Width() <= collectionIndex.y)
			{
				throw new IndexOutOfRangeException("GetCollectionValue array3D Width ("+array3D.Width()+") < index.y ("+collectionIndex.y+") for array: "+StringUtils.ToString(array3D));
			}
			if(array3D.Depth() <= collectionIndex.z)
			{
				throw new IndexOutOfRangeException("GetCollectionValue array3D Depth ("+array3D.Width()+") < index.z ("+collectionIndex.y+") for array: "+StringUtils.ToString(array3D));
			}
			#endif

			return array3D.GetValue(collectionIndex);
		}

		/// <inheritdoc />
		protected override void SetCollectionValue(ref Array array3D, Xyz collectionIndex, object memberValue)
		{
			array3D.SetValue(memberValue, collectionIndex.x, collectionIndex.y, collectionIndex.z);
		}

		/// <inheritdoc/>
		protected override GetSize GetGetCollectionSizeDelegate()
		{
			return GetCollectionSizeStatic;
		}

		private static object GetCollectionSizeStatic(object collection)
		{
			return new Size3D((Array)collection);
		}

		/// <inheritdoc/>
		protected override SetSize GetSetCollectionSizeDelegate()
		{
			return SetCollectionSizeStatic;
		}

		private static void SetCollectionSizeStatic(ref object collection, object size)
		{
			var array = (Array)collection;
			var sizeWas = new Size3D(array);
			var setSize = (Size3D)size;

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
					for(int index3 = sizeWas[2]; index3 < setSize[2]; index3++)
					{
						array.SetValue(memberType.DefaultValue(), index1, index2, index3);
					}
				}
			}

			collection = result;
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
				return null;
			}

			var index = array.FlatTo3DIndex(flattenedIndex);
			try
			{
				return array.GetValue(index.x, index.y, index.z);
			}
			catch(IndexOutOfRangeException)
			{
				#if DEV_MODE
				Debug.LogError("GetCollectionValueStatic("+flattenedIndex+") IndexOutOfRangeException with index="+index);
				#endif
				return collection.GetType().GetElementType().DefaultValue();
			}
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

			var index = array.FlatTo3DIndex(flattenedIndex);
			array.SetValue(memberValue, index.x, index.y, index.z);
		}

		/// <inheritdoc />
		protected override void InsertAt(ref Array array3D, Xyz collectionIndex, object memberValue)
		{
			InspectorUtility.ActiveInspector.Message("Can't insert a single value into a 3-dimensional array. Use resize instead.", null, MessageType.Error);

			//InsertAt doesn't really work for a 3D array with a single value (would need a whole row or column of data),
			//So simply using SetCollectionValue instead
			SetCollectionValue(ref array3D, collectionIndex, memberValue);
		}

		/// <inheritdoc />
		protected override void RemoveAt(ref Array array3D, Xyz collectionIndex)
		{
			InspectorUtility.ActiveInspector.Message("Can't delete a single value from a 3-dimensional array. Use resize instead.", null, MessageType.Error);

			//RemoveAt doesn't really work for a 3D array with a single value (would need to remove a whole row or column of data),
			//So simply setting value at collectionIndex to default value instead.
			SetCollectionValue(ref array3D, collectionIndex, MemberDefaultValue());
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
			var sorted = array1D.MakeThreeDimensional(collection.GetLength(0), collection.GetLength(1), collection.GetLength(2));
			if(!sorted.ContentsMatch(collection))
			{
				collection = sorted;
				return true;
			}
			return false;
		}

		/// <inheritdoc />
		protected override GUIContent GetIndexBasedLabelForMember(Xyz collectionIndex)
		{
			return GUIContentPool.Create(StringUtils.Concat("[", collectionIndex.x, ",", collectionIndex.y, ",", collectionIndex.z, "]"));
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
			int depth = source.Depth();
			var copy = Array.CreateInstance(source.GetType().GetElementType(), height, width, depth);
			Array.Copy(source, copy, height * width * depth);
			return copy;
		}

		/// <inheritdoc />
		protected override bool CollectionContains(Array collection, object value)
		{
			return collection == null ? false : Array.IndexOf(collection, value) != -1;
		}
	}
}