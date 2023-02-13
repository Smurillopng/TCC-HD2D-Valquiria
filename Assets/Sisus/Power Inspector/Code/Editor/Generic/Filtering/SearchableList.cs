//#define DEBUG_SEARCH_MATCH

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sisus
{
	[Serializable]
	public class SearchableList
	{
		/// <summary>
		/// Filter by which database should be sorted.
		/// </summary>
		[SerializeField]
		private FuzzyComparable filter;

		/// <summary>
		/// Items converted to format optimized for easy fuzzy comparison,
		/// ordered by matchness with filter.
		/// </summary>
		[SerializeField]
		private FuzzyComparable[] database;

		/// <summary> Items from which database was generated in original order. </summary>
		internal readonly string[] Items;

		public string Filter
		{
			get
			{
				return filter.text;
			}

			set
			{
				if(!string.Equals(filter.text, value))
				{
					filter = new FuzzyComparable(value);
					Sort();
				}
			}
		}

		public string BestMatch
		{
			get
			{
				return database.Length == 0 ? "" : database[0].text;
			}
		}
		
		public int Count
		{
			get
			{
				return database.Length;
			}
		}
		
		public SearchableList(string[] content)
		{
			Items = content;

			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.BeginSample("SearchableList.Setup");
			#endif
			
			filter = FuzzyComparable.Empty;

			int count = content.Length;
			database = new FuzzyComparable[count];
			for(int n = count - 1; n >= 0; n--)
			{
				database[n] = new FuzzyComparable(content[n]);
			}

			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.EndSample();
			#endif
		}

		public string[] GetValues(int maxMismatchThreshold)
		{
			int count = database.Length;
			var results = new List<string>(count);
			for(int n = 0; n < count; n++)
			{
				var item = database[n];

				#if DEV_MODE && DEBUG_SEARCH_MATCH
				Debug.Log("item["+n+"] "+item.text+" searchMatch: "+ item.searchMatch+"/"+maxMismatchThreshold);
				#endif

				if(item.searchMatch <= maxMismatchThreshold)
				{
					results.Add(item.text);
				}
				else
				{
					// because database is sorted we can stop once we find
					// the first target that doesn't pass the threshold
					break;
				}
			}
			return results.ToArray();
		}

		/// <summary> Sorts the database based on matchness against current filter. </summary>
		private void Sort()
		{
			FuzzyComparable.SortBySearchStringMatchness(ref filter, ref database);

			#if DEV_MODE && DEBUG_SORT
			Debug.Log(database.Length+" items Sorted by filter \""+filter+"\" with BestMatch="+BestMatch);
			#endif
		}
	}
}