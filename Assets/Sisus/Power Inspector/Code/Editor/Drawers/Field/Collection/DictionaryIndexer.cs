using System;
using System.Collections;

namespace Sisus
{
	/// <summary>
	/// Helper class that enables accessing for a dictionary by int-based member index, like you would with an array.
	/// </summary>
	public class DictionaryIndexer
	{
		private IDictionary dictionary;

		public DictionaryIndexer(IDictionary setDictionary)
		{
			dictionary = setDictionary;
		}
		
		/// <summary>
		/// Indexer to get or set items within this collection using array index syntax.
		/// </summary>
		/// <exception cref="IndexOutOfRangeException">
		/// Thrown when the index is outside the required
		/// range. </exception>
		/// <param name="index">
		/// Zero-based index of the entry to access. </param>
		/// <returns>
		/// The indexed item.
		/// </returns>
		public DictionaryEntry this[int index]
		{
			get
			{
				//UnityEngine.Debug.Log("DictionaryIndexer["+index+"] called for dictionary "+ StringUtils.ToString(dictionary));
				int current = 0;
				foreach(DictionaryEntry dictionaryEntry in dictionary)
				{
					if(current == index)
					{
						//UnityEngine.Debug.Log("GetCollectionValue("+StringUtils.TypeToString(dictionary) +", "+index+"): "+StringUtils.ToString(dictionaryEntry) + "(Type: "+StringUtils.TypeToString(dictionaryEntry) +")");
						return dictionaryEntry;
					}
					current++;
				}
				throw new IndexOutOfRangeException("DictionaryIndexer["+index+ "] IndexOutOfRangeException for dictionary of length "+dictionary.Count+": " + StringUtils.ToString(dictionary));
			}

			//NOTE: the index is ignored when setting a value
			set
			{
				dictionary[value.Key] = value.Value;
			}
		}

		public void SetDictionary(IDictionary value)
		{
			dictionary = value;
		}
	}
}