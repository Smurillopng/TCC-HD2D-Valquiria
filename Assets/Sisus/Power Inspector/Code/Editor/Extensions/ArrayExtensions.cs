using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;
namespace Sisus
{
	public static class ArrayExtensions
	{
		private static readonly object[] TempReusableSingleElementObjectArray = new object[1];
		private static readonly object[] TempReusableTwoElementObjectArray = new object[2];
		private static readonly Object[] TempReusableSingleElementUnityObjectArray = new Object[1];
		private static readonly char[] TempReusableSingleElementCharArray = new char[1];
		private static readonly Type[] TempReusableSingleElementTypeArray = new Type[1];
		private static readonly Type[] TempReusableTwoElementTypeArray = new Type[2];
		private static readonly string[] TempReusableSingleElementStringArray = new string[1];
		
		private static readonly Dictionary<object, int> ReusedInstanceCounter = new Dictionary<object, int>(20);

		public static object[] TempObjectArray(object content)
		{
			TempReusableSingleElementObjectArray[0] = content;
			return TempReusableSingleElementObjectArray;
		}

		public static object[] TempObjectArray(object item1, object item2)
		{
			TempReusableTwoElementObjectArray[0] = item1;
			TempReusableTwoElementObjectArray[1] = item2;
			return TempReusableTwoElementObjectArray;
		}

		public static Object[] TempUnityObjectArray(Object content)
		{
			TempReusableSingleElementUnityObjectArray[0] = content;
			return TempReusableSingleElementUnityObjectArray;
		}

		public static char[] TempCharArray(char content)
		{
			TempReusableSingleElementCharArray[0] = content;
			return TempReusableSingleElementCharArray;
		}

		public static Type[] TempTypeArray(Type content)
		{
			TempReusableSingleElementTypeArray[0] = content;
			return TempReusableSingleElementTypeArray;
		}

		public static Type[] TempTypeArray(Type item1, Type item2)
		{
			TempReusableTwoElementTypeArray[0] = item1;
			TempReusableTwoElementTypeArray[1] = item2;
			return TempReusableTwoElementTypeArray;
		}

		public static string[] TempStringArray(string content)
		{
			TempReusableSingleElementStringArray[0] = content;
			return TempReusableSingleElementStringArray;
		}

		public static void Populate<T>(this T[] arr, T value)
		{
			for(int i = 0; i < arr.Length; i++)
			{
				arr[i] = value;
			}
		}

		public static void Populate<T>(this T[] arr, Func<T> getValue)
		{
			for(int i = 0; i < arr.Length; i++)
			{
				arr[i] = getValue();
			}
		}

		public static void Shuffle<T>(this T[] array)
		{
			System.Random rng = new System.Random();
			int n = array.Length;
			while(n > 1)
			{
				int k = rng.Next(n--);
				T temp = array[n];
				array[n] = array[k];
				array[k] = temp;
			}
		}

		public static int NthIndexOf<T>(this T[] array, T target, int nth)
		{
			int count = array.Length;
			int foundAtIndex = -1;
			for(int n = 1; n <= nth; n++)
			{
				int i = foundAtIndex + 1;
				foundAtIndex = -1;
				for(i = n; i < count; i++)
				{
					if(array[i].Equals(target))
					{
						foundAtIndex = i;

						if(n == nth)
						{
							return i;
						}
						break;
					}
				}

				if(foundAtIndex == -1)
				{
					return -1;
				}
			}

			return foundAtIndex;
		}

		public static void AddIfDoesNotContain<T>([NotNull]this List<T> list, T item)
		{
			if(!list.Contains(item))
			{
				list.Add(item);
			}
		}

		public static void Add<T>(ref T[] array, T element)
		{
			int i = array.Length;
			Array.Resize(ref array, i + 1);
			array[i] = element;
		}

		public static void Shuffle<T>(ref T[] array, int startIndex)
		{
			System.Random rng = new System.Random();
			int n = array.Length;

			int end = Mathf.Max(startIndex + 1, 1);

			while(n > end)
			{
				int k = rng.Next(n--);
				T temp = array[n];
				array[n] = array[k];
				array[k] = temp;
			}
		}

		/// <summary>
		/// NOTE: Does not alter the original array, but returns a copy! 
		/// </summary>
		[Pure]
		public static T[] RemoveNullMembers<T>(this T[] source)
		{
			#if DEV_MODE
			Debug.Assert(source != null);
			#endif

			for(int n = source.Length - 1; n >= 0; n--)
			{
				if(source[n] == null)
				{
					#if DEV_MODE
					Debug.Log("Removing null member @ source["+n+"]");
					#endif
					source = source.RemoveAt(n);
				}
			}
			return source;
		}

		/// <summary>
		/// NOTE: Does not alter the original array, but returns a copy! 
		/// </summary>
		[Pure]
		public static TObject[] RemoveNullObjects<TObject>(this TObject[] source) where TObject : Object
		{
			#if DEV_MODE
			Debug.Assert(source != null);
			#endif

			for(int n = source.Length - 1; n >= 0; n--)
			{
				if(source[n] == null)
				{
					#if DEV_MODE
					Debug.Log("Removing null member @ source["+n+"]");
					#endif

					if(source.Length == 1)
					{
						return ArrayPool<TObject>.ZeroSizeArray;
					}
					source = source.RemoveAt(n);
				}
			}
			return source;
		}
		
		public static bool ContainsNullObjects<TObject>([NotNull]this TObject[] subject) where TObject : Object
		{
			for(int n = subject.Length - 1; n >= 0; n--)
			{
				if(subject[n] == null)
				{
					#if DEV_MODE
					Debug.Log(typeof(TObject).Name+"[].ContainsNullObjects - null found @ source[" + n + "]");
					#endif
					return true;
				}
			}
			return false;
		}

		public static bool ContainsObjectsOfType<TObject>([NotNull]this TObject[] subject, [NotNull]Type type) where TObject : Object
		{
			for(int n = subject.Length - 1; n >= 0; n--)
			{
				var test = subject[n];
				if(test != null && test.GetType() == type)
				{
					return true;
				}
			}
			return false;
		}

		public static int CountNulls<T>(this T[] subject)
		{
			int count = 0;
			for(int n = subject.Length - 1; n >= 0; n--)
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(!(subject[n] is Object));
				#endif

				if(subject[n] == null)
				{
					count++;
				}
			}
			return count;
		}
		
		public static bool ContainsNullMembers(this IList subject)
		{
			for(int n = subject.Count - 1; n >= 0; n--)
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(!(subject[n] is Object));
				#endif

				if(subject[n] == null)
				{
					#if DEV_MODE
					Debug.Log("null member @ source[" + n + "]");
					#endif
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// NOTE: Does not alter the original array, but returns a copy! 
		/// </summary>
		[Pure]
		public static T[] RemoveAt<T>(this T[] source, int index)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(index >= 0);
			#endif

			int sourceLength = source.Length;

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(index < sourceLength);
			#endif

			if(sourceLength == 1)
			{
				if(index != 0)
				{
					throw new IndexOutOfRangeException();
				}
				return ArrayPool<T>.ZeroSizeArray;
			}

			var result = new T[sourceLength - 1];

			if(index > 0)
			{
				Array.Copy(source, 0, result, 0, index);
			}

			if(index < sourceLength - 1)
			{
				Array.Copy(source, index + 1, result, index, sourceLength - index - 1);
			}

			#if UNITY_EDITOR && DEBUG_REMOVE_AT

			string d = ""+typeof(T)+"[].RemoveAt("+index+") before: ";
			int n;
			for(n = 0; n < sourceLength; n++)
			{
				if(n == index)
				{
					d += "[";
				}
				
				d += source[n].ToString();
				
				if(n == index)
				{
					d += "]";
				}

				if(n != sourceLength - 1)
				{
					d += ", ";
				}
			}

			UnityEngine.Debug.Log(d);
			
			d = ""+typeof(T)+"[].RemoveAt("+index+") after: ";
			for(n = 0; n < sourceLength - 1; n++)
			{
				d += result[n].ToString();
				
				if(n != sourceLength - 2)
				{
					d += ", ";
				}
			}
			
			UnityEngine.Debug.Log(d);

			#endif

			return result;
		}

		public static void RemoveAt([NotNull]ref Array source, int index)
		{
			int sourceLength = source.Length;

			var result = Array.CreateInstance(source.GetType().GetElementType(), sourceLength - 1);
			if(index > 0)
			{
				Array.Copy(source, 0, result, 0, index);
			}

			if(index < sourceLength - 1)
			{
				Array.Copy(source, index + 1, result, index, sourceLength - index - 1);
			}

			source = result;
		}

		/// <summary>
		/// NOTE: Does not alter the original array, but returns a copy! 
		/// </summary>
		[Pure]
		public static Array InsertAt([NotNull]this Array source, int index, object item)
		{
			int count = source.Length;
			var result = Array.CreateInstance(source.GetType().GetElementType(), count + 1);
			Array.Copy(source, 0, result, 0, index);
			result.SetValue(item, index);
			Array.Copy(source, index, result, index + 1, count - index);
			return result;
		}

		/// <summary>
		/// NOTE: Does not alter the original array, but returns a copy! 
		/// </summary>
		[Pure]
		public static T[] Add<T>([NotNull]this T[] source, T item)
		{
			return source.InsertAt(source.Length, item);
		}

		/// <summary>
		/// NOTE: Does not alter the original array, but returns a copy! 
		/// </summary>
		[Pure]
		public static T[] Add<T>([NotNull]this T[] source, params T[] items)
		{
			int countWas = source.Length;
			int addCount = items.Length;
			int resultLength = countWas + addCount;
			
			var result = new T[resultLength];

			if(countWas > 0)
			{
				Array.Copy(source, 0, result, 0, countWas);
			}
			
			for(int n = 0; n < addCount; n++)
			{
				result[n + countWas] = items[n];
			}

			return result;
		}

		/// <summary>
		/// NOTE: Does not alter the original array, but returns a copy! 
		/// </summary>
		[Pure]
		public static T[] InsertAt<T>([NotNull]this T[] source, int index, T item)
		{
			int sourceLength = source.Length;
			int resultLength = sourceLength + 1;

			var result = new T[resultLength];

			if(index > 0)
			{
				Array.Copy(source, 0, result, 0, index);
			}
			result[index] = item;
			if(index < resultLength)
			{
				Array.Copy(source, index, result, index + 1, sourceLength - index);
			}

			#if UNITY_EDITOR && DEBUG_INSERT_AT

			string d = ""+typeof(T)+"[].InsertAt("+index+") before: ";
			int n;
			for(n = 0; n < sourceLength; n++)
			{
				if(n == index)
				{
					d += "[";
				}
				
				d += source[n].ToString();
				
				if(n == index)
				{
					d += "]";
				}

				if(n != sourceLength - 1)
				{
					d += ", ";
				}
			}

			UnityEngine.Debug.Log(d);
			
			d = ""+typeof(T)+"[].InsertAt("+index+") after: ";
			int resultLength = sourceLength + 1;
			for(n = 0; n < resultLength; n++)
			{
				d += result[n].ToString();
				
				if(n != resultLength - 1)
				{
					d += ", ";
				}
			}
			
			UnityEngine.Debug.Log(d);

			#endif

			return result;
		}

		/// <summary>
		/// NOTE: Does not alter the original array, but returns a copy! 
		/// </summary>
		[Pure]
		public static T[] Swap<T>([NotNull]this T[] source, int indexA, int indexB)
		{
			T[] result = new T[source.Length];

			Array.Copy(source, result, source.Length);

			result[indexA] = source[indexB];
			result[indexB] = source[indexA];

			return result;
		}

		/// <summary>
		/// Shifts element in array from zero-based index to another zero-based index.
		/// E.g. shifting array {A,B,C} from 0 to 2 would result in array {B,A,C}.
		/// NOTE: Does not alter the original array, but returns a copy!
		/// </summary>
		/// <typeparam name="T"> Array element type. </typeparam>
		/// <param name="source"> The array to act on. This cannot be null. </param>
		/// <param name="from"> Index from which shifting starts. </param>
		/// <param name="to"> Index where shifting ends. </param>
		/// <returns> new array where element has been shifted. </returns>
		[Pure]
		public static T[] Shift<T>([NotNull]this T[] source, int from, int to)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(from >= 0);
			Debug.Assert(to >= 0);
			Debug.Assert(from != to);
			Debug.Assert(from != to - 1);
			Debug.Assert(source.Length >= 2);
			Debug.Assert(from < source.Length);
			Debug.Assert(to <= source.Length);
			#endif

			#if DEV_MODE && DEBUG_SHIFT
			Debug.Log("Shifting from "+from+" to "+to+" in array "+StringUtils.ToString(source)+"...");
			#endif

			if(from < to)
			{
				int stop = to - 2;
				for(int n = from; n <= stop; n++)
				{
					int nextIndex = n + 1;
					var move = source[n];
					source[n] = source[nextIndex];
					source[nextIndex] = move;
				}
			}
			else
			{
				for(int n = from; n > to; n--)
				{
					int nextIndex = n - 1;
					var move = source[n];
					source[n] = source[nextIndex];
					source[nextIndex] = move;
				}
			}

			#if DEV_MODE && DEBUG_SHIFT
			Debug.Log("Shifted from "+from+" to "+to+" result: "+StringUtils.ToString(source)+".");
			#endif

			return source;
		}
		
		public static void ArrayToZeroSize<T>([CanBeNull]ref T[] array)
		{
			if(array == null || array.Length != 0)
			{
				array = ArrayPool<T>.ZeroSizeArray;
			}
		}

		/// <summary>
		/// NOTE: Does not alter the original array, but returns a copy! 
		/// </summary>
		[Pure]
		public static T[] Join<T>([NotNull]this T[] a, [NotNull]T[] b)
		{
			int aLength = a.Length;
			int bLength = b.Length;
			
			if(aLength == 0)
			{
				if(bLength == 0)
				{
					return ArrayPool<T>.ZeroSizeArray;
				}
				return b;
			}
			if(bLength == 0)
			{
				return a;
			}

			T[] result = new T[aLength + bLength];

			Array.Copy(a, 0, result, 0, aLength);
			Array.Copy(b, 0, result, aLength, bLength);
			
			return result;
		}
		
		public static bool ContentsMatch([NotNull]this Object[] source, [CanBeNull]Object[] other)
		{
			if(source == other)
			{
				return true;
			}

			if(other == null)
			{
				return false;
			}

			int count = source.Length;
			if(count != other.Length)
			{
				return false;
			}

			for(int n = 0; n < count; n++)
			{
				var item = source[n];
				if(item == null)
				{
					if(other[n] != null)
					{
						return false;
					}
				}
				else if(!item.Equals(other[n]))
				{
					return false;
				}
			}
			return true;
		}

		public static bool ContentsMatch<TObject>([NotNull]this TObject[] source, [CanBeNull]GameObject[] other) where TObject : Object
		{
			if(ReferenceEquals(source, other))
			{
				return true;
			}

			if(other == null)
			{
				return false;
			}

			int count = source.Length;
			if(count != other.Length)
			{
				return false;
			}

			for(int n = 0; n < count; n++)
			{
				var item = source[n];
				if(item == null)
				{
					if(other[n] != null)
					{
						return false;
					}
				}
				else if(!item.Equals(other[n]))
				{
					return false;
				}
			}
			return true;
		}

		public static bool ContentsMatch([NotNull]this Object[] source, [CanBeNull]IList other)
		{
			if(ReferenceEquals(source, other))
			{
				return true;
			}

			if(other == null)
			{
				return false;
			}

			int count = source.Length;
			if(count != other.Count)
			{
				return false;
			}

			for(int n = 0; n < count; n++)
			{
				if(source[n] != other[n] as Object)
				{
					return false;
				}
			}
			return true;
		}

		public static bool ContentsMatch<T>([NotNull]this T[] source, [CanBeNull]T[] other)
		{
			if(source == other)
			{
				return true;
			}

			if(other == null)
			{
				return false;
			}

			int count = source.Length;
			if(count != other.Length)
			{
				return false;
			}

			for(int n = 0; n < count; n++)
			{
				var item = source[n];
				if(item == null)
				{
					if(other[n] != null)
					{
						return false;
					}
				}
				else if(!item.Equals(other[n]))
				{
					return false;
				}
			}
			return true;
		}

		public static bool ContentsMatch([NotNull]this Array source, [CanBeNull]Array other)
		{
			if(source == other)
			{
				return true;
			}

			if(other == null)
			{
				return false;
			}

			int count = source.Length;
			if(count != other.Length)
			{
				return false;
			}
			
			int rank = source.Rank;
			if(other.Rank != rank)
			{
				return false;
			}

			switch(rank)
			{
				case 1:
					for(int n = 0; n < count; n++)
					{
						var item = source.GetValue(n);

						#if DEV_MODE && PI_ASSERTATIONS
						Debug.Assert(!(item is Object), "ContentsMatch called for array which contained UnityEngine.Objects! Use the Object-specific method instead so that the overloaded equality operators are used.");
						#endif

						if(item == null)
						{
							if(other.GetValue(n) != null)
							{
								return false;
							}
						}
						else if(!item.Equals(other.GetValue(n)))
						{
							return false;
						}
					}
					return true;
				case 2:
					int sizeX = source.GetLength(0);
					int sizeY = source.GetLength(1);
					for(int y = sizeY - 1; y >= 0; y--)
					{
						for(int x = sizeX - 1; x >= 0; x--)
						{
							var item = source.GetValue(x, y);

							#if DEV_MODE && PI_ASSERTATIONS
							Debug.Assert(!(item is Object));
							#endif

							if(item == null)
							{
								return false;
							}

							if(!item.Equals(other.GetValue(x, y)))
							{
								return false;
							}
						}
					}
					return true;
				case 3:
					sizeX = source.GetLength(0);
					sizeY = source.GetLength(1);
					int sizeZ = source.GetLength(2);
					for(int z = sizeZ - 1; z >= 0; z--)
					{
						for(int y = sizeY - 1; y >= 0; y--)
						{
							for(int x = sizeX - 1; x >= 0; x--)
							{
								var item = source.GetValue(x,y,z);

								#if DEV_MODE && PI_ASSERTATIONS
								Debug.Assert(!(item is Object));
								#endif

								if(item == null)
								{
									return false;
								}
								
								if(!item.Equals(other.GetValue(x, y, z)))
								{
									return false;
								}
							}
						}
					}
					return true;
				default:
					throw new NotSupportedException();
			}
		}

		public static bool ContentsMatch([NotNull]this IList source, IList other, bool mustBeInSameOrder)
		{
			if(mustBeInSameOrder)
			{
				return source.ContentsMatch(other);
			}
			
			int count = source.Count;
			if(count != other.Count)
			{
				return false;
			}

			if(count == 0)
			{
				return true;
			}

			int nullCounter = 0;

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(ReusedInstanceCounter.Count == 0);
			#endif

			// count number of instances of each type found in first list
			int lastIndex = count - 1;
			for(int n = lastIndex; n >= 0; n--)
			{
				var item = source[n];

				#if DEV_MODE
				Debug.Assert(!(item is Object));
				#endif

				if(item == null)
				{
					nullCounter++;
				}
				else
				{
					int instanceCount;
					if(!ReusedInstanceCounter.TryGetValue(item, out instanceCount))
					{
						instanceCount = 0;
					}
					ReusedInstanceCounter[item] = instanceCount;
				}
			}

			for(int o = lastIndex; o >= 0; o--)
			{
				var otherItem = other[o];

				#if DEV_MODE
				Debug.Assert(!(otherItem is Object));
				#endif

				if(otherItem == null)
				{
					if(nullCounter == 0)
					{
						return false;
					}
					nullCounter--;
				}
				else
				{
					int instanceCount;
					if(!ReusedInstanceCounter.TryGetValue(otherItem, out instanceCount))
					{
						return false;
					}
					if(instanceCount == 1)
					{
						ReusedInstanceCounter.Remove(otherItem);
					}
					else
					{
						ReusedInstanceCounter[otherItem] = instanceCount - 1;
					}
				}
			}

			ReusedInstanceCounter.Clear();

			return true;
		}

		public static bool ContentsMatch([NotNull]this IList source, IList other)
		{
			if(ReferenceEquals(source, other))
			{
				return true;
			}

			if(other == null)
			{
				return false;
			}

			int count = source.Count;
			if(count != other.Count)
			{
				return false;
			}

			for(int n = count - 1; n >= 0; n--)
			{
				var sourceItem = source[n];
				var otherItem = other[n];

				#if DEV_MODE
				Debug.Assert(!(sourceItem is Object));
				Debug.Assert(!(otherItem is Object));
				#endif

				if(sourceItem == null)
				{
					if(otherItem != null)
					{
						return false;
					}
				}
				else if(!sourceItem.Equals(otherItem))
				{
					return false;
				}
			}
			return true;
		}

		public static bool ContentsMatch([NotNull]this IEnumerable source, IEnumerable other)
		{
			if(ReferenceEquals(source, other))
			{
				return true;
			}

			if(other == null)
			{
				return false;
			}

			var sourceEnumerator = source.GetEnumerator();
			var otherEnumerator = other.GetEnumerator();
			
			while(sourceEnumerator.MoveNext())
			{
				if(!otherEnumerator.MoveNext())
				{
					return false;
				}

				var item = sourceEnumerator.Current;
				var otherItem = otherEnumerator.Current;

				#if DEV_MODE
				Debug.Assert(!(item is Object));
				Debug.Assert(!(otherItem is Object));
				#endif

				if(item == null)
				{
					if(otherItem != null)
					{
						return false;
					}
				}
				else if(!item.Equals(otherItem))
				{
					return false;
				}
			}

			return true;
		}

		public static bool ContentsMatch(this IList source, ICollection other)
		{
			if(ReferenceEquals(source, other))
			{
				return true;
			}

			if(source == null)
			{
				return other == null;
			}

			int count = source.Count;
			if(count != other.Count)
			{
				return false;
			}

			int n = 0;
			foreach(var otherItem in other)
			{
				var item = source[n];

				#if DEV_MODE
				Debug.Assert(!(item is Object));
				Debug.Assert(!(otherItem is Object));
				#endif

				if(item == null)
				{
					if(otherItem != null)
					{
						return false;
					}
				}
				else if(!item.Equals(otherItem))
				{
					return false;
				}
				n++;
			}
			return true;
		}

		public static bool ContentsMatch(this IList source, IEnumerable other)
		{
			if(ReferenceEquals(source, other))
			{
				return true;
			}

			if(source == null)
			{
				return other == null;
			}

			int count = source.Count;

			int n = 0;
			foreach(var otherItem in other)
			{
				if(n >= count)
				{
					return false;
				}

				var item = source[n];
				if(item == null)
				{
					if(otherItem != null)
					{
						return false;
					}
				}
				else if(!item.Equals(otherItem))
				{
					return false;
				}
				n++;
			}
			return true;
		}

		[Pure]
		public static Array MakeOneDimensional(this Array array)
		{
			int rank = array.Rank;
			switch(rank)
			{
				case 1:
					return array;
				case 2:
					int width = array.GetLength(0);
					int height = array.GetLength(1);
					int length = height * width;
					var result = Array.CreateInstance(array.GetType().GetElementType(), length);
					for(int y = height - 1; y >= 0; y--)
					{
						for(int x = width - 1; x >= 0; x--)
						{
							int index = y * width + x;
							result.SetValue(array.GetValue(x, y), index);
						}
					}
					return result;
				case 3:
					width = array.GetLength(0);
					height = array.GetLength(1);
					int depth = array.GetLength(2);

					length = height * width;
					result = Array.CreateInstance(array.GetType().GetElementType(), length);
					for(int z = depth - 1; z >= 0; z--)
					{
						for(int y = height - 1; y >= 0; y--)
						{
							for(int x = width - 1; x >= 0; x--)
							{
								int index = z * width * height + y * width + x;
								result.SetValue(array.GetValue(x, y, z), index);
							}
						}
					}
					return result;
				default:
					throw new NotSupportedException("Array.MakeOneDimensional not supported for Array of rank "+array.Rank);
			}
		}

		[Pure]
		public static Array MakeTwoDimensional(this Array array, int width, int height)
		{
			int rank = array.Rank;
			switch(rank)
			{
				case 1:
					#if DEV_MODE
					Debug.Assert(array.Length == width * height);
					#endif

					var result = Array.CreateInstance(array.GetType().GetElementType(), height, width);
					for(int y = height - 1; y >= 0; y--)
					{
						for(int x = width - 1; x >= 0; x--)
						{
							int index = y * width + x;
							result.SetValue(array.GetValue(index), x, y);
						}
					}
					return result;
				case 2:
					return array;
				default:
					return array.MakeOneDimensional().MakeTwoDimensional(width, height);
			}
		}

		[Pure]
		public static Array MakeThreeDimensional(this Array array, int width, int height, int depth)
		{
			int rank = array.Rank;
			switch(rank)
			{
				case 1:
					#if DEV_MODE
					Debug.Assert(array.Length == width * height);
					#endif

					var result = Array.CreateInstance(array.GetType().GetElementType(), height, width, depth);
					for(int z = depth - 1; z >= 0; z--)
					{
						for(int y = height - 1; y >= 0; y--)
						{
							for(int x = width - 1; x >= 0; x--)
							{
								int index = z * width * height + y * width + x;
								result.SetValue(array.GetValue(index), x, y, z);
							}
						}
					}
					return result;
				case 3:
					return array;
				default:
					return array.MakeOneDimensional().MakeThreeDimensional(width, height, depth);
			}
		}

		public static bool AllSameType(this Object[] targets)
		{
			int count = targets.Length;
			if(count <= 1)
			{
				return true;
			}
			
			var first = targets[0];
			if(first != null)
			{
				var type = first.GetType();
				for(int n = count - 1; n >= 1; n--)
				{
					var test = targets[n];
					if(test == null || test.GetType() != type)
					{
						return false;
					}
				}
				return true;
			}

			for(int n = count - 1; n >= 1; n--)
			{
				if(targets[n] != null)
				{
					return false;
				}
			}
			return true;
		}

		public static void SetAll<T>(this T[] targets, T value)
		{
			for(int n = targets.Length - 1; n >= 0; n--)
			{
				targets[n] = value;
			}
		}
		
		public static int Height(this Array array)
		{
			return array.GetLength(0);
		}

		public static int Width(this Array array)
		{
			return array.GetLength(1);
		}
		
		public static int Depth(this Array array)
		{
			return array.GetLength(2);
		}

		public static int FlattenIndex(this Array array, Xyz index)
		{
			return index.ToFlattenedIndex(array);
		}

		public static int FlattenIndex(this Array array, Xy index)
		{
			return index.ToFlattenedIndex(array);
		}

		public static Xy FlatTo2DIndex(this Array array, int flattenedIndex)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			var test = Xy.Get2DIndex(array, flattenedIndex);
			Debug.Assert(test.ToFlattenedIndex(array) == flattenedIndex);
			#endif

			return Xy.Get2DIndex(array, flattenedIndex);
		}

		public static Xyz FlatTo3DIndex(this Array array, int flattenedIndex)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			var test = Xyz.Get3DIndex(array, flattenedIndex);
			Debug.Assert(test.ToFlattenedIndex(array) == flattenedIndex);
			#endif

			return Xyz.Get3DIndex(array.Width(), array.Depth(), flattenedIndex);
		}

		public static object GetValue(this Array array, Xy index)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(array.Rank == 2, "Array.GetValue rank "+array.Rank+" not valid for two-dimensional index.");
			if(array.Height() <= index.x)
			{
				throw new IndexOutOfRangeException("GetCollectionValue array2D Height ("+array.Height()+") < index.x ("+index.x+") for array: "+StringUtils.ToString(array));
			}
			if(array.Width() <= index.y)
			{
				throw new IndexOutOfRangeException("GetCollectionValue array2D Width ("+array.Width()+") < index.y ("+index.y+") for array: "+StringUtils.ToString(array));
			}
			#endif
			
			return array.GetValue(index.x, index.y);
		}

		public static object GetValue(this Array array, Xyz index)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(array.Rank == 3, "Array.GetValue rank "+array.Rank+" not valid for three-dimensional index.");
			if(array.Height() <= index.x)
			{
				throw new IndexOutOfRangeException("GetCollectionValue array3D Height ("+array.Height()+") < index.x ("+index.x+") for array: "+StringUtils.ToString(array));
			}
			if(array.Width() <= index.y)
			{
				throw new IndexOutOfRangeException("GetCollectionValue array3D Width ("+array.Width()+") < index.y ("+index.y+") for array: "+StringUtils.ToString(array));
			}
			if(array.Depth() <= index.z)
			{
				throw new IndexOutOfRangeException("GetCollectionValue array3D Depth ("+array.Width()+") < index.z ("+index.y+") for array: "+StringUtils.ToString(array));
			}
			#endif

			return array.GetValue(index.x, index.y, index.z);
		}

		public static T GetValue<T>(this T[,] array2D, Xy index)
		{
			return array2D[index.x, index.y];
		}

		public static T GetValue<T>(this T[][] jaggedArray, Xy index)
		{
			return jaggedArray[index.x][index.y];
		}

		public static T GetValue<T>(this T[,,] array3D, Xyz index)
		{
			return array3D[index.x, index.y, index.z];
		}
	}
}