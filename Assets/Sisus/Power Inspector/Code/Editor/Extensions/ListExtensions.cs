using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Sisus
{
    public static class ListExtensions
    {
        public static void AddRangeSorted<T>([NotNull] this List<T> @this, IEnumerable<T> items) where T : IComparable<T>
        {
            foreach(var item in items)
            {
                @this.AddSorted(item);
            }
        }

        public static void AddSorted<T>([NotNull] this List<T> @this, T item) where T : IComparable<T>
        {
            if(@this.Count == 0)
            {
                @this.Add(item);
                return;
            }

            if(@this[@this.Count - 1].CompareTo(item) <= 0)
            {
                @this.Add(item);
                return;
            }

            if(@this[0].CompareTo(item) >= 0)
            {
                @this.Insert(0, item);
                return;
            }

            int index = @this.BinarySearch(item);
            if(index < 0)
            {
                index = ~index;
            }
            @this.Insert(index, item);
        }

        public static int GetSortedIndex<T>([NotNull] this List<T> @this, T item) where T : IComparable<T>
        {
            if(@this.Count == 0)
            {
                return 0;
            }

            if(@this[@this.Count - 1].CompareTo(item) <= 0)
            {
                return @this.Count;
            }

            if(@this[0].CompareTo(item) >= 0)
            {
                return 0;
            }

            int index = @this.BinarySearch(item);
            if(index < 0)
            {
                index = ~index;
            }
            return index;
        }
    }
}