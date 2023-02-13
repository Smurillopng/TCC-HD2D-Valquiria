using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace Sisus
{
	public class SortObjectsByHierarchyOrder : IComparer<Object>
	{
		public static readonly SortObjectsByHierarchyOrder Instance = new SortObjectsByHierarchyOrder();

		/// <summary>
		/// Prevents a default instance of the class from being created. The static Instance should be used instead.
		/// </summary>
		private SortObjectsByHierarchyOrder() { }

		int IComparer<Object>.Compare(Object a, Object b)
		{
			return HierarchyUtility.CompareHierarchyOrder(a, b);
		}
	}
}