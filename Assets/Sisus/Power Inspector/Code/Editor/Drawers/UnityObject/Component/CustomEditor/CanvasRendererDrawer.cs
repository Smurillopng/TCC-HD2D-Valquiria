using System;
using Sisus.Attributes;
using UnityEngine;

namespace Sisus
{
	[Serializable, DrawerForComponent(typeof(CanvasRenderer), false, true)]
	public class CanvasRendererDrawer : CustomEditorComponentDrawer
	{
		/// <inheritdoc />
		protected override float EstimatedUnfoldedHeight
		{
			get
			{
				return 57f;
			}
		}

		/// <inheritdoc/>
		protected override float GetOptimalPrefixLabelWidthForEditor(int indentLevel)
		{
			return 152f;
		}
	}
}