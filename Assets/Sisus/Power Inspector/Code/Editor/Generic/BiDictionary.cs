#define USE_SERIALIZATION_CALLBACK_RECEIVER

//#define DEBUG_ADD
//#define DEBUG_ADD_DETAILED //NOTE: This prints whole dictionary state with each Add, so can increase log sizes fast
//#define DEBUG_CONSTRUCTOR

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace Sisus
{
	/// <summary>
	/// A bi-directional Dictionary.
	/// </summary>
	/// <typeparam name="T1"> First key type. </typeparam>
	/// <typeparam name="T2"> Second key type. </typeparam>
	public class BiDictionary<T1, T2> : ICollection<KeyValuePair<T1, T2>>, IDictionary
	{
		[NonSerialized]
		private Dictionary<T1, T2> firstToSecond;
		[NonSerialized]
		private Dictionary<T2, T1> secondToFirst;
		
		[CanBeNull]
		public T2 this[[NotNull]T1 first]
		{
			get
			{
				return Get(first);
			}

			set
			{
				firstToSecond[first] = value;
				if(value != null)
				{
					secondToFirst[value] = first;
				}
			}
		}
		
		public T1 this[[NotNull]T2 second]
		{
			get
			{
				return GetBySecond(second);
			}
		}
		
		public ICollection<T1> Firsts
		{
			get
			{
				return firstToSecond.Keys;
			}
		}

		public ICollection<T2> Seconds
		{
			get
			{
				return secondToFirst.Keys;
			}
		}

		public void CopyTo(Array array, int index)
		{
			CopyTo((KeyValuePair<T1,T2>[])array, index);
		}

		public int Count
		{
			get
			{
				return firstToSecond.Count;
			}
		}

		bool IDictionary.IsFixedSize
		{
			get
			{
				return false;
			}
		}

		bool IDictionary.IsReadOnly
		{
			get
			{
				return false;
			}
		}

		object IDictionary.this[object key]
		{
			get
			{
				return Get((T1)key);
			}
			
			set
			{
				Set((T1)key, (T2)value);
			}
		}

		bool ICollection<KeyValuePair<T1, T2>>.IsReadOnly
		{
			get
			{
				return false;
			}
		}

		ICollection IDictionary.Keys
		{
			get
			{
				return firstToSecond.Keys;
			}
		}

		ICollection IDictionary.Values
		{ 
			get
			{
				return firstToSecond.Values;
			}
		}

		bool ICollection.IsSynchronized
		{
			get
			{
				return false;
			}
		}

		object ICollection.SyncRoot
		{
			get
			{
				return (firstToSecond as ICollection).SyncRoot;
			}
		}
		
		public BiDictionary()
		{
			#if DEV_MODE && DEBUG_CONSTRUCTOR
			Debug.Log(StringUtils.ToStringSansNamespace(GetType())+" Constructor");
			#endif

			firstToSecond = new Dictionary<T1, T2>();
			secondToFirst = new Dictionary<T2, T1>();
		}

		public BiDictionary(int count)
		{
			#if DEV_MODE && DEBUG_CONSTRUCTOR
			Debug.Log(GetType().Name+" Constructor");
			#endif
			firstToSecond = new Dictionary<T1, T2>(count);
			secondToFirst = new Dictionary<T2, T1>(count);
		}

		public void Add([CanBeNull]T1 first, [CanBeNull]T2 second)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(first != null || second != null);
			#endif

			#if DEV_MODE && DEBUG_ADD_DETAILED
			if(firstToSecond.Count < 50)
			{
				Debug.Log(GetType().Name+".Add(" + StringUtils.ToString(first) + ", " + StringUtils.ToString(second) + ") called.\nfirstToSecond:\n"+StringUtils.ToString(firstToSecond, "\n")+"\nsecondToFirst:\n"+StringUtils.ToString(secondToFirst, "\n"));
			}
			else
			#endif
			#if DEV_MODE && (DEBUG_ADD || DEBUG_ADD_DETAILED)
			{
				Debug.Log(GetType().Name+".Add(" + StringUtils.ToString(first) + ", " + StringUtils.ToString(second) + ") called.\nfirstToSecond:\n"+StringUtils.ToString(firstToSecond.Count)+"\nsecondToFirst:\n"+StringUtils.ToString(secondToFirst.Count));
			}
			#endif

			if(first != null)
			{
				try
				{
					firstToSecond.Add(first, second);
				}
				catch(ArgumentException)
				{
					Debug.LogError(GetType().Name+".Add(" + StringUtils.ToString(first) + ", " + StringUtils.ToString(second) + ") firstToSecond already contained first key of type "+StringUtils.TypeToString(first));
					firstToSecond[first] = second;
				}
			}

			if(second != null)
			{
				try
				{
					secondToFirst.Add(second, first);
				}
				catch(ArgumentException)
				{
					Debug.LogError(GetType().Name+".Add(" + StringUtils.ToString(first) + ", " + StringUtils.ToString(second) + ") secondToFirst already contained second key of type "+StringUtils.TypeToString(second));
					secondToFirst[second] = first;
				}
			}
		}
		
		public void Set([CanBeNull]T1 first, [CanBeNull]T2 second)
		{
			#if DEV_MODE && DEBUG_ADD
			Debug.Log(GetType().Name+".Set(" + StringUtils.ToString(first) + ", " + StringUtils.ToString(second) + ") called.\nfirstToSecond:\n"+StringUtils.ToString(firstToSecond, "\n")+"\nsecondToFirst:\n"+StringUtils.ToString(secondToFirst, "\n"));
			#endif

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(first != null || second != null);
			#endif

			if(first != null)
			{
				firstToSecond[first] = second;
			}

			if(second != null)
			{
				secondToFirst[second] = first;
			}
		}

		public bool TryGet([NotNull]T1 first, [CanBeNull]out T2 second)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(first != null);
			if(firstToSecond.TryGetValue(first, out second))
			{
				T1 shouldEqualFirst;
				Debug.Assert(secondToFirst.TryGetValue(second, out shouldEqualFirst));
				Debug.Assert(first.Equals(shouldEqualFirst));
			}
			#endif
			return firstToSecond.TryGetValue(first, out second);
		}

		public bool TryGetBySecond([NotNull]T2 second, [CanBeNull]out T1 first)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(second != null);
			if(secondToFirst.TryGetValue(second, out first))
			{
				T2 shouldEqualSecond;
				Debug.Assert(firstToSecond.TryGetValue(first, out shouldEqualSecond));
				Debug.Assert(second.Equals(shouldEqualSecond));
			}
			#endif
			return secondToFirst.TryGetValue(second, out first);
		}

		public T2 Get([NotNull]T1 first)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(first != null);
			var result = firstToSecond[first];
			T1 shouldEqualFirst;
			Debug.Assert(secondToFirst.TryGetValue(result, out shouldEqualFirst));
			Debug.Assert(first.Equals(shouldEqualFirst));
			#endif
			return firstToSecond[first];
		}

		public T1 GetBySecond([NotNull]T2 second)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(second != null);
			var result = secondToFirst[second];
			T2 shouldEqualSecond;
			Debug.Assert(firstToSecond.TryGetValue(result, out shouldEqualSecond));
			Debug.Assert(second.Equals(shouldEqualSecond));
			#endif
			return secondToFirst[second];
		}

		public void Remove([NotNull]T1 first)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(first != null);
			#endif

			var second = firstToSecond[first];

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(second != null);
			#endif

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(firstToSecond.Remove(first));
			Debug.Assert(secondToFirst.Remove(second));
			#else
			firstToSecond.Remove(first);
			secondToFirst.Remove(second);
			#endif
		}

		public void RemoveBySecond([NotNull]T2 second)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(second != null);
			#endif

			var first = secondToFirst[second];

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(first != null);
			#endif

			secondToFirst.Remove(second);

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(firstToSecond.Remove(first));
			Debug.Assert(secondToFirst.Remove(second));
			#else
			firstToSecond.Remove(first);
			secondToFirst.Remove(second);
			#endif
		}

		public bool Remove([NotNull]T1 first, [NotNull]T2 second)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(first != null);
			Debug.Assert(second != null);
			#endif

			bool removedFirst = firstToSecond.Remove(first);
			bool removedSecond = secondToFirst.Remove(second);

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(removedFirst == removedSecond);
			#endif
			
			return removedFirst || removedSecond;
		}

		public bool Contains(T1 first)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			T2 second;
			if(firstToSecond.TryGetValue(first, out second))
			{
				T1 shouldEqualFirst;
				Debug.Assert(secondToFirst.TryGetValue(second, out shouldEqualFirst));
				Debug.Assert(first.Equals(shouldEqualFirst));
			}
			#endif

			return firstToSecond.ContainsKey(first);
		}

		public bool Contains(T2 second)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			T1 first;
			if(secondToFirst.TryGetValue(second, out first))
			{
				T2 shouldEqualSecond;
				Debug.Assert(firstToSecond.TryGetValue(first, out shouldEqualSecond));
				Debug.Assert(second.Equals(shouldEqualSecond));
			}
			#endif

			return secondToFirst.ContainsKey(second);
		}

		public bool ContainsFirst(T1 first)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			T2 second;
			if(firstToSecond.TryGetValue(first, out second))
			{
				T1 shouldEqualFirst;
				Debug.Assert(secondToFirst.TryGetValue(second, out shouldEqualFirst));
				Debug.Assert(first.Equals(shouldEqualFirst));
			}
			#endif

			return firstToSecond.ContainsKey(first);
		}

		public bool ContainsSecond(T2 second)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			T1 first;
			if(secondToFirst.TryGetValue(second, out first))
			{
				T2 shouldEqualSecond;
				Debug.Assert(firstToSecond.TryGetValue(first, out shouldEqualSecond));
				Debug.Assert(second.Equals(shouldEqualSecond));
			}
			#endif

			return secondToFirst.ContainsKey(second);
		}

		public void Add(KeyValuePair<T1, T2> item)
		{
			#if DEV_MODE && DEBUG_ADD
			Debug.Log(GetType().Name+".Add(" + StringUtils.ToString(item)+") called.\nfirstToSecond:\n"+StringUtils.ToString(firstToSecond, "\n")+"\nsecondToFirst:\n"+StringUtils.ToString(secondToFirst, "\n"));
			#endif

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(item.Key != null);
			Debug.Assert(item.Value != null);
			#endif

			firstToSecond.Add(item.Key, item.Value);
			secondToFirst.Add(item.Value, item.Key);
		}
		
		public void Clear()
		{
			firstToSecond.Clear();
			secondToFirst.Clear();
		}
		
		public int IndexOf(KeyValuePair<T1, T2> item)
		{
			int index = 0;
			foreach(var test in firstToSecond)
			{
				if(test.Key.Equals(item.Key))
				{
					return index;
				}
				index++;
			}
			return -1;
		}
		
		public bool Contains(KeyValuePair<T1, T2> item)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			bool firstContains = firstToSecond.Contains(item);
			bool secondContains = secondToFirst.ContainsKey(item.Value);
			Debug.Assert(firstContains == secondContains);
			#endif

			#if DEV_MODE && PI_ASSERTATIONS
			if(firstToSecond.Contains(item))
			{
				Debug.Assert(secondToFirst.ContainsKey(item.Value));
			}
			else
			{
				Debug.Assert(!secondToFirst.ContainsKey(item.Value));
			}
			#endif

			return firstToSecond.Contains(item);
		}

		public void CopyTo(KeyValuePair<T1, T2>[] array, int arrayIndex)
		{
			//var list = firstToSecond.ToList();
			//list.CopyTo(array, arrayIndex);
			(firstToSecond as ICollection).CopyTo(array, arrayIndex);
		}

		public bool Remove(KeyValuePair<T1, T2> item)
		{
			if(firstToSecond.Remove(item.Key))
			{
				secondToFirst.Remove(item.Value);
				return true;
			}
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(!secondToFirst.ContainsKey(item.Value));
			#endif
			return false;
		}

		public Dictionary<T1, T2> FirstToSecondDictionary()
		{
			return firstToSecond;
		}

		public Dictionary<T2, T1> SecondToFirstDictionary()
		{
			return secondToFirst;
		}

		void IDictionary.Add(object key, object value)
		{
			Add((T1)key, (T2)value);
		}

		bool IDictionary.Contains(object value)
		{
			return Contains((KeyValuePair<T1,T2>)value);
		}

		void IDictionary.Remove(object value)
		{
			Remove((KeyValuePair<T1,T2>)value);
		}

		public IEnumerator<KeyValuePair<T1, T2>> GetEnumerator()
		{
			return firstToSecond.GetEnumerator();
		}

		IDictionaryEnumerator IDictionary.GetEnumerator()
		{
			return firstToSecond.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return firstToSecond.GetEnumerator();
		}
	}
}