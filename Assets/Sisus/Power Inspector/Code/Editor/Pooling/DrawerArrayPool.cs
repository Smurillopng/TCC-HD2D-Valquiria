using System;
using System.Collections.Generic;

namespace Sisus
{
	public static class DrawerArrayPool
	{
		private static Dictionary<int, PolymorphicPool<IDrawer[]>> pools = new Dictionary<int, PolymorphicPool<IDrawer[]>>(3);

		public static IDrawer[] Create(int length)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			UnityEngine.Debug.Assert(length >= 0, "DrawerArrayPool.Create called with invalid length: " + StringUtils.ToColorizedString(length));
			UnityEngine.Debug.Assert(length < 10000, "DrawerArrayPool.Create called with supiciously large length: " + StringUtils.ToColorizedString(length));
			#endif

			if(length <= 0)
			{
				return ArrayPool<IDrawer>.ZeroSizeArray;
			}

			PolymorphicPool<IDrawer[]> pool;
			if(!pools.TryGetValue(length, out pool))
			{
				return new IDrawer[length];
			}

			IDrawer[] result;
			if(!pool.TryGet(out result))
			{
				return new IDrawer[length];
			}

			#if DEV_MODE && PI_ASSERTATIONS
			UnityEngine.Debug.Assert(result != null);
			UnityEngine.Debug.Assert(result.Length == length);
			#endif

			return result;
		}

		public static void Create(int length, ref IDrawer[] result)
		{
			if(result == null)
			{
				result = Create(length);
			}
			else if(result.Length != length)
			{
				Dispose(ref result, false);
				result = Create(length);
			}
			else
			{
				DisposeContent(ref result);
			}
		}

		public static IDrawer[] Create(IDrawer member)
		{
			var result = Create(1);
			result[0] = member;
			return result;
		}

		/// <summary>
		/// Resizes target array to size, just like Array.Resize would.
		/// Will NOT dispose existing members when reducing size!
		/// </summary>
		public static void Resize(ref IDrawer[] target, int length)
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
					Dispose(ref target, false);
					target = result;
				}
			}
		}

		public static IDrawer[] Create(List<IDrawer> list)
		{
			int count = list.Count;
			var result = Create(count);
			for(int n = count - 1; n >= 0; n--)
			{
				result[n] = list[n];
			}
			return result;
		}

		public static IDrawer[] CreateAndDisposeList(ref List<IDrawer> list)
		{
			var result = Create(list);
			DrawerListPool.Dispose(ref list);
			return result;
		}

		public static void ListToArrayAndDispose(ref List<IDrawer> list, ref IDrawer[] array, bool disposeArrayContent)
		{
			if(array == null)
			{
				array = Create(list);
			}
			else
			{
				int size = list.Count;
				if(array.Length == size)
				{
					if(disposeArrayContent)
					{
						for(int n = size - 1; n >= 0; n--)
						{
							var existing = array[n];
							if(existing != null)
							{
								existing.Dispose();
							}
							array[n] = list[n];
						}
					}
					else
					{
						for(int n = size - 1; n >= 0; n--)
						{
							array[n] = list[n];
						}
					}
				}
				else
				{
					Dispose(ref array, disposeArrayContent);
					array = Create(list);
				}
			}
			
			DrawerListPool.Dispose(ref list);
		}

		public static IDrawer[] Clone(IDrawer[] content)
		{
			int count = content.Length;
			var result = Create(count);
			for(int n = count - 1; n >= 0; n--)
			{
				result[n] = content[n];
			}
			return result;
		}

		/// <summary>
		/// Disposes the array to the pool and sets the reference to it to null,
		/// after either disposing all its children, or simply setting references
		/// to them to null, depending on the value of disposeContent.
		/// </summary>
		/// <param name="disposing"> The array to dispose. This should not be null when the method is called. It will be set to null once the method has finished. </param>
		/// <param name="disposeContent"> If true, Dispose will be called for each drawer inside the array. </param>
		public static void Dispose(ref IDrawer[] disposing, bool disposeContent)
		{
			// Don't pool zero-length arrays since we'll be using ZeroSizeArray field for those purposes.
			int size = disposing.Length;
			if(size == 0)
			{
				disposing = null;
				return;
			}

			if(disposeContent)
			{
				DisposeContent(ref disposing);
			}
			else
			{
				ClearContent(ref disposing);
			}

			PolymorphicPool<IDrawer[]> pool;
			if(!pools.TryGetValue(size, out pool))
			{
				pool = new PolymorphicPool<IDrawer[]>(1, 250);
			}
			pool.Pool(ref disposing);
		}

		public static void DisposeContent(ref IDrawer[] disposing)
		{
			for(int n = disposing.Length - 1; n >= 0; n--)
			{
				var member = disposing[n];
				if(member != null)
				{
					member.Dispose();
					disposing[n] = null;
				}
			}
		}

		/// <summary>
		/// Sets all members to null, without actually disposing them or anything
		/// </summary>
		public static void ClearContent(ref IDrawer[] disposing)
		{
			for(int n = disposing.Length - 1; n >= 0; n--)
			{
				disposing[n] = null;
			}
		}

		public static void InsertAt(ref IDrawer[] target, int index, IDrawer value, bool disposeOriginalArray)
		{
			if(index < 0)
			{
				throw new IndexOutOfRangeException();
			}

			int oldSize = target.Length;

			if(index > oldSize)
			{
				throw new IndexOutOfRangeException();
			}

			int newSize = oldSize + 1;
			var result = Create(newSize);

			for(int n = oldSize - 1; n >= index; n--)
			{
				result[n + 1] = target[n];
			}

			result[index] = value;

			for(int n = index - 1; n >= 0; n--)
			{
				result[n] = target[n];
			}

			if(disposeOriginalArray)
			{
				Dispose(ref target, false);
			}

			target = result;

			#if DEV_MODE && PI_ASSERTATIONS
			UnityEngine.Debug.Assert(Array.IndexOf(target, value) == index);
			#endif
		}
		
		public static void RemoveAt(ref IDrawer[] target, int index, bool disposeOriginalArray, bool disposeRemovedDrawer) //if needed, could add flag for this
		{
			#if DEV_MODE && PI_ASSERTATIONS
			UnityEngine.Debug.Assert(index >= 0, StringUtils.ToColorizedString(index));
			UnityEngine.Debug.Assert(index <= target.Length, StringUtils.ToColorizedString(index));
			#endif

			int oldSize = target.Length;
			int newSize = oldSize - 1;
			var result = Create(newSize);

			for(int n = oldSize - 1; n > index; n--)
			{
				result[n-1] = target[n];
				target[n] = null;
			}

			if(disposeRemovedDrawer)
			{
				var removed = target[index];
				if(removed != null)
				{
					DrawerPool.Pool(removed);
				}
			}
			
			for(int n = index - 1; n >= 0; n--)
			{
				result[n] = target[n];
				target[n] = null;
			}

			if(disposeOriginalArray)
			{
				Dispose(ref target, false);
			}

			target = result;
		}
	}
}