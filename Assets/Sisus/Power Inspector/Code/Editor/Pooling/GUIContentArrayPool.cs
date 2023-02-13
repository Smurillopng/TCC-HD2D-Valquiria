#define SAFE_MODE

//#define DISABLE_POOLING

using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Sisus
{
	public static class GUIContentArrayPool
	{
		private static Dictionary<int, Pool<GUIContent[]>> pools = new Dictionary<int, Pool<GUIContent[]>>(3);

		public static GUIContent[] Create(int length)
		{
			if(length == 0)
			{
				return ArrayPool<GUIContent>.ZeroSizeArray;
			}

			Pool<GUIContent[]> pool;
			if(!pools.TryGetValue(length, out pool))
			{
				return new GUIContent[length];
			}
			GUIContent[] result;
			if(!pool.TryGet(out result))
			{
				return new GUIContent[length];
			}
			return result;
		}

		public static GUIContent[] Create(string[] labelTexts)
		{
			int count = labelTexts.Length;
			var result = Create(count);
			for(int n = count - 1; n >= 0; n--)
			{
				result[n] = GUIContentPool.Create(labelTexts[n]);
			}
			return result;
		}

		public static GUIContent[] Create(List<string> labelTexts)
		{
			int count = labelTexts.Count;
			var result = Create(count);
			for(int n = count - 1; n >= 0; n--)
			{
				result[n] = GUIContentPool.Create(labelTexts[n]);
			}
			return result;
		}

		public static GUIContent[] Create(GUIContent[] labels)
		{
			int count = labels.Length;
			var result = Create(count);
			for(int n = count - 1; n >= 0; n--)
			{
				result[n] = labels[n];
			}
			return result;
		}

		public static void Create(string[] labelTexts, ref GUIContent[] result)
		{
			int count = labelTexts.Length;

			if(result == null)
			{
				result = Create(count);
			}
			else if(result.Length != count)
			{
				Dispose(ref result);
				result = Create(count);
			}
			else
			{
				DisposeContent(ref result);
			}

			for(int n = count - 1; n >= 0; n--)
			{
				result[n] = GUIContentPool.Create(labelTexts[n]);
			}
		}

		public static GUIContent[] Create(List<GUIContent> list)
		{
			int count = list.Count;
			var result = Create(count);
			for(int n = count - 1; n >= 0; n--)
			{
				result[n] = list[n];
			}
			return result;
		}
		
		public static void Copy(GUIContent[] source, ref GUIContent[] result)
		{
			#if DEV_MODE
			Debug.Assert(source != null);
			#endif

			int count = source.Length;

			if(result == null)
			{
				result = Create(count);
			}
			else if(result.Length != count)
			{
				Dispose(ref result);
				result = Create(count);
			}
			else
			{
				DisposeContent(ref result);
			}

			for(int n = count - 1; n >= 0; n--)
			{
				result[n] = GUIContentPool.Create(source[n]);
			}
		}

		public static void Dispose(ref GUIContent[] disposing, bool disposeContent = true)
		{
			#if SAFE_MODE
			if(disposing == null)
			{
				#if DEV_MODE
				Debug.LogError("GUIContentArrayPool.Dispose called for null target!");
				#endif
				return;
			}
			#endif

			int length = disposing.Length;

			//don't pool zero-length arrays since we'll be using
			//ZeroSizeArray field for those purposes
			if(length > 0)
			{
				if(disposeContent)
				{
					DisposeContent(ref disposing);
				}
				else
				{
					ClearContent(ref disposing);
				}

				#if !DISABLE_POOLING
				Pool<GUIContent[]> pool;
				if(!pools.TryGetValue(length, out pool))
				{
					pool = new Pool<GUIContent[]>(1);
					pools[length] = pool;
				}
				pool.Dispose(ref disposing);
				#endif
			}
			disposing = null;
		}

		public static void DisposeContent(ref GUIContent[] disposing)
		{
			int length = disposing.Length;
			for(int n = length - 1; n >= 0; n--)
			{
				var member = disposing[n];
				if(member != null)
				{
					GUIContentPool.Dispose(ref member);
				}
			}
		}

		/// <summary>
		/// Sets all members to null, without actually disposing them or anything
		/// </summary>
		public static void ClearContent(ref GUIContent[] disposing)
		{
			int length = disposing.Length;
			for(int n = length - 1; n >= 0; n--)
			{
				disposing[n] = null;
			}
		}

		public static void InsertAt(ref GUIContent[] target, int index, GUIContent value)
		{
			int oldSize = target.Length;
			int newSize = oldSize + 1;
			var result = Create(newSize);

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

			Dispose(ref target, false);

			target = result;
		}

		public static void Resize([CanBeNull]ref GUIContent[] target, int length, bool disposeOriginal = true, bool disposeOriginalContent = true)
		{
			if(target == null)
			{
				target = Create(length);
			}
			else
			{
				int lengthWas = target.Length;
				if(lengthWas != length)
				{
					var result = Create(length);
					int min = lengthWas;
					if(min > length)
					{
						min = length;
					}
					for(int n = min - 1; n >= 0; n--)
					{
						result[n] = target[n];
						target[n] = null;
					}
					if(disposeOriginal)
					{
						Dispose(ref target, disposeOriginalContent);
					}
					else if(disposeOriginalContent)
					{
						DisposeContent(ref target);
					}
					target = result;
				}
			}
		}

		public static void RemoveAt(ref GUIContent[] target, int index)
		{
			int oldSize = target.Length;
			int newSize = oldSize - 1;
			var result = Create(newSize);

			for(int n = oldSize - 1; n > index; n--)
			{
				result[n-1] = target[n];
				target[n] = null;
			}

			GUIContentPool.Dispose(ref target[index]);
			
			for(int n = index - 1; n >= 0; n--)
			{
				result[n] = target[n];
				target[n] = null;
			}

			Dispose(ref target, false);

			target = result;
		}

		public static void ToZeroSizeArray(ref GUIContent[] disposing, bool disposeContent)
		{
			if(disposing == null)
			{
				disposing = ArrayPool<GUIContent>.ZeroSizeArray;
			}
			else if(disposing.Length != 0)
			{
				Dispose(ref disposing, disposeContent);
				disposing = ArrayPool<GUIContent>.ZeroSizeArray;
			}
		}
	}
}