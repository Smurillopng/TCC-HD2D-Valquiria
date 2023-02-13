#define DEBUG_NULL_MEMBERS

#define DEBUG_CREATE_MEMBERS
#define DEBUG_RESIZE

using System;
using System.Collections;
using UnityEngine;

namespace Sisus
{
	/// <summary>
	/// ArrayDrawer and ListDrawer inherit from this
	/// </summary>
	[Serializable]
	public abstract class OneDimensionalCollectionDrawer<TValue> : CollectionDrawer<TValue, int, int> where TValue : class, IEnumerable
	{
		/// <inheritdoc />
		protected sealed override int GetElementCount(int collectionIndex)
		{
			return collectionIndex;
		}

		/// <inheritdoc />
		protected sealed override int GetCollectionIndex(int memberIndex)
		{
			return memberIndex;
		}

		/// <inheritdoc />
		protected sealed override int Rank
		{
			get
			{
				return 1;
			}
		}

		/// <inheritdoc />
		protected override IDrawer BuildResizeField()
		{
			var resizerMemberInfo = ResizerMemberInfo;
			var resizer = DelayedIntDrawer.Create(MixedSize ? 0 : elementCount, resizerMemberInfo, this, GUIContentPool.Create("Size"), ReadOnly || resizerMemberInfo == null);
			return resizer;
		}

		/// <inheritdoc />
		protected sealed override GUIContent GetIndexBasedLabelForMember(int collectionIndex)
		{
			return GUIContentPool.Create(StringUtils.Concat("[", collectionIndex, "]"));
		}
	}
}