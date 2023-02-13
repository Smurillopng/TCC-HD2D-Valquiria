using UnityEngine;
using System.Collections.Generic;

namespace Sisus
{
	public class SortTransformsByHierarchyOrder : IComparer<Transform>
	{
		int IComparer<Transform>.Compare(Transform a, Transform b)
		{
			return HierarchyUtility.CompareHierarchyOrder(a, b);
		}
	}
}