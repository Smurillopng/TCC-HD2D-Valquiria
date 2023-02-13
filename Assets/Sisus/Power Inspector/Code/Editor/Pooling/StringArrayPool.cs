using System.Collections.Generic;

namespace Sisus
{
	public static class StringArrayPool
	{
		private static readonly string[] TempReusableSingleSizeArray = new string[1];

		public static string[] TempArray(string content)
		{
			TempReusableSingleSizeArray[0] = content;
			return TempReusableSingleSizeArray;
		}

		private static Dictionary<int, Pool<string[]>> pools = new Dictionary<int, Pool<string[]>>(3);

		public static string[] Create(int length)
		{
			return CreateInternal(length, true);
		}

		public static string[] Create(List<string> list)
		{
			int count = list.Count;
			var result = CreateInternal(count, false);
			for(int n = count - 1; n >= 0; n--)
			{
				result[n] = list[n];
			}
			return result;
		}
		
		public static void Dispose(ref string[] disposing)
		{
			int length = disposing.Length;

			//don't pool zero-length arrays since we'll be using
			//ArrayPool<string>.ZeroSizeArray field for those purposes
			if(length > 0)
			{
				#if DEV_MODE || SAFE_MODE
				if(disposing == TempReusableSingleSizeArray)
				{
					UnityEngine.Debug.LogError("StringArrayPool.Dispose was called for TempReusableSingleSizeArray!");
					return;
				}
				#endif

				//ClearContent(ref disposing);

				Pool<string[]> pool;
				if(!pools.TryGetValue(length, out pool))
				{
					pool = new Pool<string[]>(1);
					pools[length] = pool;
				}
				pool.Dispose(ref disposing);
			}
			disposing = null;
		}
		
		/// <summary>
		/// Sets all members to ""
		/// </summary>
		public static void ClearContent(ref string[] disposing)
		{
			int length = disposing.Length;
			for(int n = length - 1; n >= 0; n--)
			{
				disposing[n] = "";
			}
		}

		public static void InsertAt(ref string[] target, int index, string value, bool disposeOriginal = true)
		{
			int oldSize = target.Length;
			int newSize = oldSize + 1;
			var result = CreateInternal(newSize, false);

			for(int n = oldSize - 1; n >= index; n--)
			{
				result[n+1] = target[n];
				target[n] = null;
			}

			result[index] = value;

			for(int n = index - 1; n >= 0; n--)
			{
				result[n] = target[n];
				target[n] = null;
			}

			if(disposeOriginal)
			{
				Dispose(ref target);
			}

			target = result;
		}

		public static void RemoveAt(ref string[] target, int index, bool disposeOriginal = true)
		{
			int oldSize = target.Length;
			int newSize = oldSize - 1;
			var result = CreateInternal(newSize, false);

			for(int n = oldSize - 1; n > index; n--)
			{
				result[n-1] = target[n];
			}
			
			for(int n = index - 1; n >= 0; n--)
			{
				result[n] = target[n];
			}

			if(disposeOriginal)
			{
				Dispose(ref target);
			}

			target = result;
		}

		public static void ToZeroSizeArray(ref string[] disposing)
		{
			if(disposing == null)
			{
				disposing = ArrayPool<string>.ZeroSizeArray;
			}
			else if(disposing.Length != 0)
			{
				Dispose(ref disposing);
				disposing = ArrayPool<string>.ZeroSizeArray;
			}
		}

		private static string[] CreateInternal(int length, bool clearContent)
		{
			if(length == 0)
			{
				return ArrayPool<string>.ZeroSizeArray;
			}

			Pool<string[]> pool;
			if(!pools.TryGetValue(length, out pool))
			{
				return new string[length];
			}
			string[] result;
			if(!pool.TryGet(out result))
			{
				return new string[length];
			}
			if(clearContent)
			{
				ClearContent(ref result);
			}
			return result;
		}
	}
}