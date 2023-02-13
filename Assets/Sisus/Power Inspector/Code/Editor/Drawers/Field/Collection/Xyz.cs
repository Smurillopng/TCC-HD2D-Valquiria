using System;

namespace Sisus
{
	[Serializable]
	public struct Xyz : IEquatable<Xyz>
	{
		public static readonly Xyz Zero = new Xyz(0, 0, 0);

		/// <summary> The x-coordinate or width. </summary>
		public readonly int x;

		/// <summary> The y-coordinate or height. </summary>
		public readonly int y;

		/// <summary> The z-coordinate or depth. </summary>
		public readonly int z;
		
		public int this[int index]
		{
			get
			{
				switch(index)
				{
					case 0:
						return x;
					case 1:
						return y;
					case 2:
						return z;
					default:
						throw new IndexOutOfRangeException(StringUtils.Concat("Xyz[", index, "] index can't be other than 0 or 1."));
				}
			}
		}

		public Xyz(int setX, int setY, int setZ)
		{
			x = setX;
			y = setY;
			z = setZ;
		}
		
		public int ToFlattenedIndex(Array array)
		{
			return ToFlattenedIndex(array.Width(), array.Depth());
		}

		/// <summary>
		/// Where this Xyz represents the x, ya and z indexes of a members in a three-dimensional collection,
		/// given the width of the collection that contains the member, returns the flattened index of the member
		/// in the collection.
		/// </summary>
		/// <param name="width"> The width of the collection. </param>
		/// <param name="depth"> The depth of the collection. </param>
		/// <returns> Flattened index of collection member. </returns>
		public int ToFlattenedIndex(int width, int depth)
		{
			return ToFlattenedIndex(width, depth, x, y, z);
		}
	
		public bool Equals(Xyz other)
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
				return Equals((Xyz)obj);
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
		
		public static void SetX(ref Xyz xyz, int x)
		{
			xyz = new Xyz(x, xyz.y, xyz.z);
		}

		public static void SetY(ref Xyz xyz, int y)
		{
			xyz = new Xyz(xyz.x, y, xyz.z);
		}

		public static void SetZ(ref Xyz xyz, int z)
		{
			xyz = new Xyz(xyz.x, xyz.y, z);
		}

		public static void Set(out Xyz xyz, int x, int y, int z)
		{
			xyz = new Xyz(x, y, z);
		}

		public static void Positive(ref Xyz xyz)
		{
			if(xyz.x < 0)
			{
				if(xyz.y < 0)
				{
					xyz = xyz.z < 0 ? Zero : new Xyz(0, 0, xyz.z);
				}
				else
				{
					xyz = new Xyz(0, xyz.y, xyz.z < 0 ? 0 : xyz.z);
				}
			}
			else if(xyz.y < 0)
			{
				xyz = new Xyz(xyz.x, 0, xyz.z < 0 ? 0 : xyz.z);
			}
			else if(xyz.z < 0)
			{
				SetZ(ref xyz, 0);
			}
		}

		public static Xyz Min(Xyz a, Xyz b)
		{
			return new Xyz(MathUtils.Min(a.x, b.x), MathUtils.Min(a.y, b.y), MathUtils.Min(a.z, b.z));
		}

		public static Xyz Get3DIndex(Array array, int flattenedIndex)
		{
			return Get3DIndex(array.Width(), array.Depth(), flattenedIndex);
		}

		public static Xyz Get3DIndex(int width, int depth, int flattenedIndex)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			UnityEngine.Debug.Assert(width > 0, StringUtils.ToString(width));
			UnityEngine.Debug.Assert(depth > 0, StringUtils.ToString(depth));
			#endif

			int x = flattenedIndex / (depth * width);
			int y = (flattenedIndex - width * depth * x) / depth;
			int z = flattenedIndex - width * depth * x - depth * y;

			#if DEV_MODE && PI_ASSERTATIONS
			UnityEngine.Debug.Assert(x >= 0, StringUtils.ToString(x));
			UnityEngine.Debug.Assert(y >= 0, StringUtils.ToString(y));
			UnityEngine.Debug.Assert(z >= 0, StringUtils.ToString(z));
			UnityEngine.Debug.Assert(y < width, StringUtils.ToString(y) + " >= "+ StringUtils.ToString(width));
			UnityEngine.Debug.Assert(z < depth, StringUtils.ToString(z) + " >= "+ StringUtils.ToString(depth));
			#endif

			return new Xyz(x, y, z);
		}

		public static int ToFlattenedIndex(Array array, int x, int y, int z)
		{
			return ToFlattenedIndex(array.Width(), array.Depth(), x, y, z);
		}

		public static int ToFlattenedIndex(int width, int depth, int x, int y, int z)
		{
			return (x * width + y) * depth + z;
		}
		
		public override string ToString()
		{
			return StringUtils.Concat("(", x, ",", y, ",", z, ")");
		}
	}
}