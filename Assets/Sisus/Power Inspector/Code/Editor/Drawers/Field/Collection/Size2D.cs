using System;

namespace Sisus
{
	[Serializable]
	public struct Size2D : IEquatable<Size2D>, IEquatable<int>
	{
		/// <summary> Used for representing mixed values. </summary>
		public static readonly Size2D Invalid = new Size2D(-1, -1);
		public static readonly Size2D Zero = new Size2D(0, 0);

		public int height;
		public int width;

		/// <summary>
		/// Returns element count for 2D collection where height is width and width is height.
		/// </summary>
		/// <value> Element count of 2D Collection. </value>
		public int Count
		{
			get
			{
				return height * width;
			}
		}
		
		public int this[int index]
		{
			get
			{
				if(index == 0)
				{
					return height;
				}
				if(index == 1)
				{
					return width;
				}
				throw new IndexOutOfRangeException(StringUtils.Concat("Size2D[", index, "] index can't be other than 0 or 1."));
			}
		}

		public Size2D(Array array)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			UnityEngine.Debug.Assert(array.Rank == 2, "array.Rank "+array.Rank+" != 2");
			#endif

			height = array.Height();
			width = array.Width();
		}

		public Size2D(int setHeight, int setWidth)
		{
			height = setHeight;
			width = setWidth;

			#if DEV_MODE && PI_ASSERTATIONS
			if(setHeight < 0)
			{
				if(setHeight != -1 || setWidth != -1) { UnityEngine.Debug.LogWarning("Size2D contructor called with unexpected parameters: "+setHeight+"x"+setWidth+ "."); }
			}
			else if(setWidth < 0) { UnityEngine.Debug.LogWarning("Size2D contructor called with unexpected parameters: " + setHeight + "x" + setWidth + "."); }
			#endif
		}

		/// <summary>
		/// Where this Size2D represents the width and height of a two-dimensional collection,
		/// converts given flattened index to height and width indexes of a member in the collection.
		/// in the collection.
		/// </summary>
		/// <param name="flattenedIndex"> Zero-based flattened index of a member in a two-dimensional collection. </param>
		/// <returns> The two-dimensional index of a member in the collection. </returns>
		public Xy Get2DIndex(int flattenedIndex)
		{
			return Xy.Get2DIndex(width, flattenedIndex);
		}
		
		public bool Equals(Size2D other)
		{
			return height == other.height && width == other.width;
		}

		public bool Equals(int other)
		{
			return Count == other;
		}

		public override bool Equals(object obj)
		{
			if(ReferenceEquals(obj, null))
			{
				return false;
			}

			try
			{
				return Equals((Size2D)obj);
			}
			catch(InvalidCastException)
			{
				try
				{
					return Equals((int)obj);
				}
				catch(InvalidCastException)
				{
					return false;
				}
			}
		}
	
		public override int GetHashCode()
		{
			unchecked
			{
				return (height * 397) ^ width;
			}
		}

		public static void SetHeight(ref Size2D size2D, int height)
		{
			size2D = new Size2D(height, size2D.width);
		}

		public static void SetWidth(ref Size2D size2D, int width)
		{
			size2D = new Size2D(size2D.height, width);
		}

		public static void Set(out Size2D size2D, int height, int width)
		{
			size2D = new Size2D(height, width);
		}

		/// <summary> Changes negative height, width and depth member values in size3D to zero. </summary>
		/// <param name="size2D"> [in,out] The size2D. </param>
		public static void Positive(ref Size2D size2D)
		{
			if(size2D.height < 0)
			{
				size2D = size2D.width < 0 ? Zero : new Size2D(0, size2D.width);
			}
			else if(size2D.width < 0)
			{
				size2D = new Size2D(size2D.height, 0);
			}
		}

		public static Size2D Min(Size2D a, Size2D b)
		{
			return new Size2D(MathUtils.Min(a.height, b.height), MathUtils.Min(a.width, b.width));
		}

		public override string ToString()
		{
			//return StringUtils.Concat("(", height, ",", width, ")");
			return StringUtils.Concat(height, " x ", width);
		}
		
		public bool IsValid()
		{
			return height >= 0 && width >= 0;
		}
	}
}