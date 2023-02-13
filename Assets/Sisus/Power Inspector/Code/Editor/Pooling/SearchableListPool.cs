using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sisus
{
	public static class SearchableListPool
	{
		private static readonly Dictionary<string[], SearchableList> cache = new Dictionary<string[], SearchableList>(3, new StringArrayEqualityComparer());
		
		public static SearchableList Create(string[] content)
		{
			SearchableList result;
			if(cache.TryGetValue(content, out result))
			{
				#if DEV_MODE
				Debug.Assert(result.Items.Length == content.Length);
				#endif
				return result;
			}
			return new SearchableList(content);
		}
		
		public static void Dispose(ref SearchableList disposing)
		{
			disposing.Filter = "";
			cache[disposing.Items] = disposing;
			disposing = null;
		}

		/// <summary> 
		/// Compares equality of two string arrays, where arrays are considered equal
		/// if the number of elements is the same and element at each index is the same.
		/// </summary>
		private class StringArrayEqualityComparer : IEqualityComparer<string[]>
		{
			public bool Equals(string[] x, string[] y)
			{
				if(ReferenceEquals(x, null))
				{
					return ReferenceEquals(y, null);
				}
				if(ReferenceEquals(y, null))
				{
					return false;
				}

				int count = x.Length;
				if(y.Length != count)
				{
					return false;
				}

				for(int n = count - 1; n >= 0; n--)
				{
					if(!string.Equals(x[n], y[n]))
					{
						return false;
					}
				}
				return true;
			}

			public int GetHashCode(string[] items)
			{
				unchecked
				{
					int hash = 0;
					for(int n = items.Length - 1; n >= 0; n--)
					{
						hash = hash * 7717 + items[n].GetHashCode();
					}
					return hash;
				}
			}
		}
	}
}