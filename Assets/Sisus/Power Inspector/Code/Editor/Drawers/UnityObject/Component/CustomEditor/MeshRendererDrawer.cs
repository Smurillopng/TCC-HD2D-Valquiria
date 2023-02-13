#if UNITY_EDITOR
using System;
using Sisus.Attributes;
using UnityEngine;

namespace Sisus
{
	[Serializable, DrawerForComponent(typeof(MeshRenderer), false, true)]
	public class MeshRendererDrawer : RendererDrawer
	{
		/// <inheritdoc/>
		protected override float EstimatedUnfoldedHeight
		{
			get
			{
				return 113f;
			}
		}

		/// <inheritdoc/>
		protected override float GetOptimalPrefixLabelWidthForEditor(int indentLevel)
		{
			return 136f;
		}
	}
}
#endif