#if UNITY_EDITOR
using System;
using Sisus.Attributes;
using UnityEngine;

namespace Sisus
{
	[Serializable, DrawerForComponent(typeof(MeshCollider), false, true)]
	public class MeshColliderDrawer : CustomEditorComponentDrawer
	{
		/// <inheritdoc />
		protected override float EstimatedUnfoldedHeight
		{
			get
			{
				return 112f;
			}
		}

		/// <inheritdoc/>
		protected override float GetOptimalPrefixLabelWidthForEditor(int indentLevel)
		{
			return 114f;
		}
	}
}
#endif