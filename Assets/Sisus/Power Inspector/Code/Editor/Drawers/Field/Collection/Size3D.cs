using System;

namespace Sisus
{
	[Serializable]
	public struct Size3D : IEquatable<Size3D>, IEquatable<int>
	{
		/// <summary> Used for representing mixed values. </summary>
		public static readonly Size3D Invalid = new Size3D(-1, -1, -1);
		public static readonly Size3D Zero = new Size3D(0, 0, 0);

		public int height;
		public int width;
		public int depth;

		/// <summary>
		/// Returns element count for 3D collection where height is width and width is height.
		/// </summary>
		/// <value> Element count of 3D Collection. </value>
		public int Count
		{
			get
			{
				return height * width * depth;
			}
		}
		
		public int this[int index]
		{
			get
			{
				switch(index)
				{
					case 0:
						return height;
					case 1:
						return width;
					case 2:
						return depth;
					default:
						throw new IndexOutOfRangeException(StringUtils.Concat("Size3D[", index, "] index can't be other than 0 or 1."));
				}
			}
		}

		public Size3D(Array array)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			UnityEngine.Debug.Assert(array.Rank == 3, "array.Rank "+array.Rank+" != 3");
			#endif

			height = array.Height();
			width = array.Width();
			depth = array.Depth();
		}

		public Size3D(int setHeight, int setWidth, int setDepth)
		{
			height = setHeight;
			width = setWidth;
			depth = setDepth;

			#if DEV_MODE && PI_ASSERTATIONS
			if(setHeight < 0)
			{
				if(setHeight != -1 || setWidth != -1 || setDepth != -1) { UnityEngine.Debug.LogWarning("Size3D contructor called with unexpected parameters: " + setHeight + "x" + setWidth + "x" + setDepth + "."); }
			}
			else if(setWidth < 0) { UnityEngine.Debug.LogWarning("Size3D contructor called with unexpected parameters: " + setHeight + "x" + setWidth + "x" + setDepth); }
			else if(setDepth < 0) { UnityEngine.Debug.LogWarning("Size3D contructor called with unexpected parameters: " + setHeight + "x" + setWidth + "x" + setDepth); }
			#endif
		}

		/// <summary>
		/// Where this Size3D represents the width, height and depth of a three-dimensional collection,
		/// converts given flattened index to height, width and depth indexes of a member in the collection.
		/// in the collection.
		/// </summary>
		/// <param name="flattenedIndex"> Zero-based flattened index of a member in a three-dimensional collection. </param>
		/// <returns> The three-dimensional index of a member in the collection. </returns>
		public Xyz Get3DIndex(int flattenedIndex)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			UnityEngine.Debug.Assert(flattenedIndex >= 0);
			#endif
			return Xyz.Get3DIndex(width, depth, flattenedIndex);
		}
		
		public bool Equals(Size3D other)
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
				return Equals((Size3D)obj);
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

		public static void SetHeight(ref Size3D size3D, int height)
		{
			size3D = new Size3D(height, size3D.width, size3D.depth);
		}

		public static void SetWidth(ref Size3D size3D, int width)
		{
			size3D = new Size3D(size3D.height, width, size3D.depth);
		}

		public static void SetDepth(ref Size3D size3D, int depth)
		{
			size3D = new Size3D(size3D.height, size3D.width, depth);
		}

		public static void Set(out Size3D size3D, int height, int width, int depth)
		{
			size3D = new Size3D(height, width, depth);
		}

		/// <summary> Changes negative height, width and depth member values in size3D to zero. </summary>
		/// <param name="size3D"> [in,out] The size 3D. </param>
		public static void Positive(ref Size3D size3D)
		{
			if(size3D.height < 0)
			{
				if(size3D.width < 0)
				{
					size3D = size3D.depth < 0 ? Zero : new Size3D(0, 0, size3D.depth);
				}
				else
				{
					size3D = size3D.depth < 0 ? new Size3D(0, size3D.width, 0) : new Size3D(0, size3D.width, size3D.depth);
				}
			}
			else if(size3D.width < 0)
			{
				size3D = size3D.depth < 0 ? new Size3D(size3D.height, 0, 0) : new Size3D(size3D.height, 0, size3D.depth);
			}
			else if(size3D.depth < 0)
			{
				SetDepth(ref size3D, 0);
			}
		}

		public static Size3D Min(Size3D a, Size3D b)
		{
			return new Size3D(MathUtils.Min(a.height, b.height), MathUtils.Min(a.width, b.width), MathUtils.Min(a.depth, b.depth));
		}

		public override string ToString()
		{
			//return StringUtils.Concat("(", height, ",", width, ",", depth, ")");
			return StringUtils.Concat(height, " x ", width, " x ", depth);
		}

		public bool IsValid()
		{
			return height >= 0 && width >= 0 && depth >= 0;
		}
	}
}