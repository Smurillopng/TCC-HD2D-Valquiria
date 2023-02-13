#if !CSHARP_7_3_OR_NEWER
using System.Collections.Generic;

namespace System.Collections.Concurrent
{
	/// <summary>
	/// Class for adding backwards compatibility with ConcurrentDictionary class in .NET 2.0.
	/// This is not actually threadsafe.
	/// </summary>
	public class ConcurrentDictionary<TKey, TValue> : Dictionary<TKey, TValue>
	{
		public bool TryAdd(TKey key, TValue value)
		{
			if(ContainsKey(key))
			{
				return false;
			}
			Add(key, value);
			return true;
		}
	}
}
#endif