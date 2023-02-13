#define WARN_IF_POOLING_EXTERNALLY_CREATED_ITEMS

using System;
using System.Collections.Generic;
using JetBrains.Annotations;

#if DEV_MODE
using Debug = UnityEngine.Debug;
#endif

namespace Sisus
{
	public static class ArrayPool<T>
	{
		public static readonly T[] ZeroSizeArray = new T[0];

		/// <summary>
		/// The pools.
		/// </summary>
		private static Dictionary<int, PolymorphicPool<T[]>> pools = new Dictionary<int, PolymorphicPool<T[]>>(3);

		#if DEV_MODE && WARN_IF_POOLING_EXTERNALLY_CREATED_ITEMS
		private static Dictionary<int, PolymorphicPool<T[]>> created = new Dictionary<int, PolymorphicPool<T[]>>(3);
		#endif

		/// <summary>
		/// Casts array with elements of type <typeparamref name="T"/> to array of elements of type <typeparamref name="TTo"/>
		/// </summary>
		/// <typeparam name="TTo">
		/// Cast to an array with members of this type </typeparam>
		/// <param name="sourceArray">
		/// Array whose contents will be cast to target type.  </param>
		/// <param name="disposeSourceArray">
		/// True if we are allowed to pool the source array after the casting has been completed. Note that this does not guarantee that the array will get disposed.</param>
		/// <returns>
		/// An array of type TTo[]. If target and source types are the same, simply returns sourceArray.
		/// </returns>
		public static TTo[] Cast<TTo>(T[] sourceArray, bool disposeSourceArray = false) where TTo : class
		{
			var fromType = sourceArray.GetType();
			var toType = typeof(TTo[]);

			if(disposeSourceArray)
			{
				if(fromType == toType)
				{
					#if DEV_MODE
					Debug.LogWarning("No need to cast from "+fromType.Name+" to "+toType.Name+" because they are the same");
					#endif
					return sourceArray as TTo[];
				}
			}

			int count = sourceArray.Length;
			var result = ArrayPool<TTo>.CreateInternal(count, false);
			for(int n = count - 1; n >= 0; n--)
			{
				result[n] = sourceArray[n] as TTo;
			}

			if(disposeSourceArray)
			{
				Dispose(ref sourceArray);

				#if DEV_MODE && PI_ASSERTATIONS
				var inspectors = InspectorUtility.ActiveManager.ActiveInstances;
				for(int n = inspectors.Count - 1; n >= 0; n--)
				{
					Debug.Assert(!inspectors[n].State.inspected.ContainsNullObjects());
				}
				#endif
			}

			return result;
		}

		/// <summary>
		/// Casts array with elements of type <typeparamref name="T"/> to array of elements of type <typeparamref name="TTo"/>
		/// </summary>
		/// <typeparam name="TTo">
		/// Cast to an array with members of this type </typeparam>
		/// <param name="sourceArray">
		/// Array whose contents will be cast to target type.  </param>
		/// <param name="disposeSourceArray">
		/// True if we are allowed to pool the source array after the casting has been completed. Note that this does not guarantee that the array will get disposed.</param>
		/// <returns>
		/// An array of type TTo[]. If target and source types are the same, simply returns sourceArray.
		/// </returns>
		public static TToStruct[] CastToValueTypeArray<TToStruct>(T[] sourceArray, bool disposeSourceArray = false) where TToStruct : struct
		{
			var fromType = sourceArray.GetType();
			var toType = typeof(TToStruct[]);

			if(disposeSourceArray)
			{
				if(fromType == toType)
				{
					#if DEV_MODE
					Debug.LogWarning("No need to cast from "+fromType.Name+" to "+toType.Name+" because they are the same");
					#endif
					return sourceArray as TToStruct[];
				}
			}

			int count = sourceArray.Length;
			var result = ArrayPool<TToStruct>.CreateInternal(count, false);
			for(int n = count - 1; n >= 0; n--)
			{
				result[n] = (TToStruct)(object)sourceArray[n];
			}

			if(disposeSourceArray)
			{
				Dispose(ref sourceArray);

				#if DEV_MODE && PI_ASSERTATIONS
				var inspectors = InspectorUtility.ActiveManager.ActiveInstances;
				for(int n = inspectors.Count - 1; n >= 0; n--)
				{
					Debug.Assert(!inspectors[n].State.inspected.ContainsNullObjects());
				}
				#endif
			}

			return result;
		}

		/// <summary>
		/// Casts array with elements of type <typeparamref name="T"/> to array of elements of type <typeparamref name="TTo"/>.
		/// Will skip elements that cannot be cast to target type.
		/// </summary>
		/// <typeparam name="TTo">
		/// Cast to an array with members of this type. </typeparam>
		/// <param name="sourceArray">
		/// Array whose contents will be cast to target type.  </param>
		/// <param name="disposeSourceArray">
		/// True if we are allowed to pool the source array after the casting has been completed. Note that this does not guarantee that the array will get disposed.</param>
		/// <returns>
		/// An array of type TTo[]. If target and source types are the same, simply returns sourceArray. Length of array will be equal or less than that of sourceArray, depending
		/// on whether or not all members can be cast.
		/// </returns>
		public static TTo[] CastWhereCastable<TTo>(T[] sourceArray, bool disposeSourceArray) where TTo : class, T
		{
			var fromType = sourceArray.GetType();
			var toType = typeof(TTo[]);

			if(disposeSourceArray)
			{
				if(fromType.Equals(toType))
				{
					#if DEV_MODE
					Debug.LogWarning("No need to cast from "+fromType.Name+" to "+toType.Name+" because they are the same");
					#endif
					return sourceArray as TTo[];
				}
			}

			int count = sourceArray.Length;
			var result = ArrayPool<TTo>.CreateInternal(count, false);
			for(int n = count - 1; n >= 0; n--)
			{
				try
				{
					result[n] = (TTo)sourceArray[n];
				}
				catch(InvalidCastException)
				{
					result = result.RemoveAt(n);
				}
			}

			if(disposeSourceArray)
			{
				Dispose(ref sourceArray);
			}

			return result;
		}

		/// <summary>
		/// Tries to cast arrays elements of type <typeparamref name="T"/> to array result, containing elements of type <typeparamref name="TTo"/>.
		/// If casting fails for any member, will return false and set result to null.
		/// </summary>
		/// <typeparam name="TTo">
		/// Cast to an array with members of this type. </typeparam>
		/// <param name="sourceArray">
		/// Array whose contents will be cast to target type.</param>
		/// <param name="disposeSourceArray">
		/// True if we are allowed to pool the source array after the casting has been completed. Note that this does not guarantee that the array will get disposed.</param>
		/// <param name="result">
		/// An array of type TTo[]. If target and source types are the same, simply equals sourceArray. Length of array will be equal or less than that of sourceArray, depending
		/// on whether or not all members can be cast.
		/// </param>
		/// <returns>
		/// True if casting succeeds for all array elements, otherwise returns false.
		/// </returns>
		public static bool TryCast<TTo>(T[] sourceArray, bool disposeSourceArray, out TTo[] result) where TTo : T
		{
			var fromType = sourceArray.GetType();
			var toType = typeof(TTo[]);

			if(disposeSourceArray)
			{
				if(fromType.Equals(toType))
				{
					#if DEV_MODE
					Debug.LogWarning("No need to cast from "+fromType.Name+" to "+toType.Name+" because they are the same");
					#endif
					result = sourceArray as TTo[];
					return true;
				}
			}

			int count = sourceArray.Length;
			result = ArrayPool<TTo>.CreateInternal(count, false);
			try
			{
				for(int n = count - 1; n >= 0; n--)
				{
					result[n] = (TTo)sourceArray[n];
				}
			}
			catch(InvalidCastException)
			{
				result = null;
				return false;
			}

			if(disposeSourceArray)
			{
				Dispose(ref sourceArray);
			}

			return true;
		}
		
		public static T[] Create(int length)
		{
			return CreateInternal(length, true);
		}

		public static T[] Create([NotNull]List<T> list)
		{
			int count = list.Count;
			var result = CreateInternal(count, false);
			for(int n = count - 1; n >= 0; n--)
			{
				result[n] = list[n];
			}
			return result;
		}

		public static T[] CreateWithContent(T item)
		{
			var result = CreateInternal(1, false);
			result[0] = item;
			return result;
		}

		public static T[] CreateWithContent(T item1, T item2)
		{
			var result = CreateInternal(2, false);
			result[0] = item1;
			result[1] = item2;
			return result;
		}

		/// <summary>
		/// Resizes target array to size, just like Array.Resize would.
		/// If target is null creates a new array.
		/// If target is not null can and length does not match desired length, it will be disposed to the array pool.
		/// </summary>
		/// <param name="target"> [in,out] The array to resize. A null array can be provided. It will never be null after method has finished. </param>
		/// <param name="length"> The length to which target will be resized. </param>
		public static void Resize(ref T[] target, int length)
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
						target[n] = default(T);
					}
					Dispose(ref target);
					target = result;
				}
			}
		}
		
		/// <summary>
		/// Sets all members of array to default value
		/// </summary>
		public static void ClearContent([NotNull]ref T[] disposing)
		{
			int length = disposing.Length;
			for(int n = length - 1; n >= 0; n--)
			{
				disposing[n] = default(T);
			}
		}

		public static void InsertAt([NotNull]ref T[] target, int index, T value)
		{
			int oldSize = target.Length;
			int newSize = oldSize + 1;
			var result = CreateInternal(newSize, false);

			for(int n = oldSize - 1; n >= index; n--)
			{
				result[n+1] = target[n];
				target[n] = default(T);
			}

			result[index] = value;

			for(int n = index - 1; n >= 0; n--)
			{
				result[n] = target[n];
				target[n] = default(T);
			}

			Dispose(ref target);

			target = result;
		}

		public static void RemoveAt([NotNull]ref T[] target, int index)
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

			Dispose(ref target);

			target = result;
		}

		public static void ToZeroSizeArray(ref T[] disposing)
		{
			if(disposing == null)
			{
				disposing = ZeroSizeArray;
			}
			else if(disposing.Length != 0)
			{
				Dispose(ref disposing);
				disposing = ZeroSizeArray;
			}
		}
		
		private static T[] CreateInternal(int length, bool clearContent)
		{
			if(length == 0)
			{
				return ZeroSizeArray;
			}

			#if DEV_MODE && WARN_IF_POOLING_EXTERNALLY_CREATED_ITEMS
			PolymorphicPool<T[]> createdPool;
			if(!created.TryGetValue(length, out createdPool))
			{
				createdPool = new PolymorphicPool<T[]>(1, 1000000);
				created[length] = createdPool;
			}
			#endif
			

			PolymorphicPool<T[]> pool;
			if(!pools.TryGetValue(length, out pool))
			{
				#if DEV_MODE && WARN_IF_POOLING_EXTERNALLY_CREATED_ITEMS
				var createdArray = new T[length];
				var poolCreatedArray = createdArray;
				createdPool.Pool(ref poolCreatedArray);
				return createdArray;
				#else
				return new T[length];
				#endif
			}
			T[] result;
			if(!pool.TryGet(out result))
			{
				#if DEV_MODE && WARN_IF_POOLING_EXTERNALLY_CREATED_ITEMS
				var createdArray = new T[length];
				var poolCreatedArray = createdArray;
				createdPool.Pool(ref poolCreatedArray);
				return createdArray;
				#else
				return new T[length];
				#endif
			}
			if(clearContent)
			{
				ClearContent(ref result);
			}

			#if DEV_MODE && WARN_IF_POOLING_EXTERNALLY_CREATED_ITEMS
			{
				var poolArray = result;
				createdPool.Pool(ref poolArray);
			}
			#endif

			return result;
		}

		public static void Dispose(ref T[] disposing)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(!typeof(IDrawer).IsAssignableFrom(typeof(T)), "Use DrawerArrayPool instead");
			Debug.Assert(typeof(T) != Types.UnityObject || !LinkedMemberHierarchy.AnyHierarchyTargetsArrayEquals(disposing as UnityEngine.Object[]));
			#endif

			int length = disposing.Length;

			//don't pool zero-length arrays since we'll be using ZeroSizeArray field for those purposes
			if(length > 0)
			{
				PolymorphicPool<T[]> pool;

				#if DEV_MODE && WARN_IF_POOLING_EXTERNALLY_CREATED_ITEMS
				if(!created.TryGetValue(length, out pool) || !pool.Contains(disposing))
				{
					Debug.LogWarning("ArrayPool<"+StringUtils.ToString(typeof(T))+ ">.Dispose was called for array that was not created by ArrayPool. This could lead to bugs:\ndisposing: " + StringUtils.ToString(disposing));
				}
				else
				{
					pool.Remove(disposing);
				}
				#endif
				
				if(!pools.TryGetValue(length, out pool))
				{
					pool = new PolymorphicPool<T[]>(1, 25);
					pools[length] = pool;
				}
				pool.Pool(ref disposing);
			}
			disposing = null;
		}
	}
}