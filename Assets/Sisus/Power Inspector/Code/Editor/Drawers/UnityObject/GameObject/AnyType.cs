using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Sisus
{
	public class AnyType
	{
		public static readonly TypeComparer TypeComparer = new TypeComparer();

		[NotNull]
		public readonly Type[] types;
		private readonly int hashCode;

		public int Count
		{
			get
			{
				return types.Length;
			}
		}

		public Type this[int index]
		{
			get
			{
				return types[index];
			}
		}

		public AnyType([NotNull]params Type[] setTypes)
		{
			for(int count = setTypes.Length, n = count - 1; n >= 0; n--)
			{
				var type = setTypes[n];
				if(type.IsAbstract || type.IsBaseComponentType())
				{
					var extendingTypes = type.GetExtendingTypes(false, false);
					var list = new List<Type>(setTypes.Length + extendingTypes.Count() - 1);
					list.AddRange(setTypes);
					list.RemoveAt(n);
					list.AddRange(extendingTypes);
					setTypes = list.ToArray();
				}
			}

			types = setTypes;

			Array.Sort(types, TypeComparer);

			unchecked
			{
				hashCode = 17;
				for(int n = types.Length - 1; n >= 0; n--)
				{
					hashCode += 397 * types[n].GetHashCode();
				}
			}
		}

		public override int GetHashCode()
		{
			return hashCode;
		}

		public override bool Equals(object obj)
		{
			var other = obj as AnyType;
			if(other == null)
			{
				return false;
			}
			return types.ContentsMatch(other.types);
		}
	}

	public class TypeComparer : IComparer<Type>
	{
		public int Compare(Type x, Type y)
		{
			return x.FullName.CompareTo(y.FullName);
		}
	}

	public class AnyTypeEqualityComparer : IEqualityComparer<AnyType>
	{
		public bool Equals(AnyType x, AnyType y)
		{
			if(ReferenceEquals(x, y))
			{
				return true;
			}

			if(ReferenceEquals(x, null) || ReferenceEquals(y, null))
			{
				return false;
			}

			return x.types.ContentsMatch(y.types);
		}

		public int GetHashCode(AnyType obj)
		{
			return obj.GetHashCode();
		}
	}
}