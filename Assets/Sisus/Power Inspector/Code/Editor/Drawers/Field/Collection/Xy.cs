using System;

namespace Sisus
{
	[Serializable]
	public struct Xy : IEquatable<Xy>
	{
		public static readonly Xy Zero = new Xy(0, 0);

		/// <summary> The x-coordinate or width. </summary>
		public readonly int x;

		/// <summary> The y-coordinate or height. </summary>
		public readonly int y;
		
		public int this[int index]
		{
			get
			{
				if(index == 0)
				{
					return x;
				}
				if(index == 1)
				{
					return y;
				}
				throw new IndexOutOfRangeException(StringUtils.Concat("Xy[", index, "] index can't be other than 0 or 1."));
			}
		}

		public Xy(int setX, int setY)
		{
			x = setX;
			y = setY;
		}
		
		/// <summary>
		/// Where this Size2D represents the x and y indexes of a members in a two-dimensional collection,
		/// given the width of the collection that contains the member, returns the flattened index of the member
		/// in the collection.
		/// </summary>
		/// <param name="array"> The two-dimensional collection. </param>
		/// <returns> Flattened index of collection member. </returns>
		public int ToFlattenedIndex(Array array)
		{
			return ToFlattenedIndex(array.Width());
		}

		/// <summary>
		/// Where this Xy represents the x and y indexes of a members in a two-dimensional collection,
		/// given the width of the collection that contains the member, returns the flattened index of the member
		/// in the collection.
		/// </summary>
		/// <param name="width"> The width of the collection. </param>
		/// <returns> Flattened index of collection member. </returns>
		public int ToFlattenedIndex(int width)
		{
			return ToFlattenedIndex(width, x, y);
		}
	
		public bool Equals(Xy other)
		{
			return x == other.x && y == other.y;
		}

		public override bool Equals(object obj)
		{
			if(ReferenceEquals(obj, null))
			{
				return false;
			}

			try
			{
				return Equals((Xy)obj);
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
				return (x * 397) ^ y;
			}
		}

		public static void SetX(ref Xy xy, int x)
		{
			xy = new Xy(x, xy.y);
		}

		public static void SetY(ref Xy xy, int y)
		{
			xy = new Xy(xy.x, y);
		}

		public static void Set(out Xy xy, int x, int y)
		{
			xy = new Xy(x, y);
		}

		public static void Positive(ref Xy xy)
		{
			if(xy.x < 0)
			{
				xy = xy.y < 0 ? Zero : new Xy(0, xy.y);
			}
			else if(xy.y < 0)
			{
				xy = new Xy(xy.x, 0);
			}
		}

		public static Xy Min(Xy a, Xy b)
		{
			return new Xy(MathUtils.Min(a.x, b.x), MathUtils.Min(a.y, b.y));
		}

		public static Xy Get2DIndex(Array array, int flattenedIndex)
		{
			return Get2DIndex(array.Width(), flattenedIndex);
		}

		public static Xy Get2DIndex(int width, int flattenedIndex)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			UnityEngine.Debug.Assert(width > 0, StringUtils.ToString(width));
			#endif

			int x = flattenedIndex / width;
			int y = flattenedIndex - width * x;

			#if DEV_MODE && PI_ASSERTATIONS
			UnityEngine.Debug.Assert(x >= 0, StringUtils.ToString(x));
			UnityEngine.Debug.Assert(y >= 0, StringUtils.ToString(y));
			UnityEngine.Debug.Assert(y < width, StringUtils.ToString(y) + " >= "+ StringUtils.ToString(width));
			#endif

			return new Xy(x, y);
		}

		public static int ToFlattenedIndex(Array array, int x, int y)
		{
			return ToFlattenedIndex(array.Width(), x, y);
		}

		public static int ToFlattenedIndex(int width, int x, int y)
		{
			return x * width + y;
		}

		public override string ToString()
		{
			return StringUtils.Concat("(", x, ",", y, ")");
		}
	}
}