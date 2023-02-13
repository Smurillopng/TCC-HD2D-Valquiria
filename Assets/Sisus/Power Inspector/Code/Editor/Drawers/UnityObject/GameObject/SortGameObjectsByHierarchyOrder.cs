using UnityEngine;
using System.Collections.Generic;

namespace Sisus
{
	public class SortGameObjectsByHierarchyOrder : IComparer<GameObject>
	{
		public static readonly SortGameObjectsByHierarchyOrder Instance = new SortGameObjectsByHierarchyOrder();

		/// <summary>
		/// Prevents a default instance of the class from being created. The static Instance should be used instead.
		/// </summary>
		private SortGameObjectsByHierarchyOrder() { }

		int IComparer<GameObject>.Compare(GameObject a, GameObject b)
		{
			return HierarchyUtility.CompareHierarchyOrder(a, b);
		}
	}
}