using UnityEngine;
using System.Collections.Generic;

namespace Sisus
{
	public class SortComponentsByHierarchyOrder : IComparer<Component>
	{
		public static readonly SortComponentsByHierarchyOrder Instance = new SortComponentsByHierarchyOrder();

		/// <summary>
		/// Prevents a default instance of the class from being created. The static Instance should be used instead.
		/// </summary>
		private SortComponentsByHierarchyOrder() { }

		int IComparer<Component>.Compare(Component a, Component b)
		{
			return HierarchyUtility.CompareHierarchyOrder(a, b);
		}
	}
}