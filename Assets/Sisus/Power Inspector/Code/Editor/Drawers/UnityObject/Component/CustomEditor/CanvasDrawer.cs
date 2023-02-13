using System;
using Sisus.Attributes;
using UnityEngine;

namespace Sisus
{
	[Serializable, DrawerForComponent(typeof(Canvas), false, true)]
	public class CanvasDrawer : CustomEditorComponentDrawer
	{
		/// <inheritdoc />
		protected override float EstimatedUnfoldedHeight
		{
			get
			{
				return 137f;
			}
		}

		/// <inheritdoc/>
		protected override float GetOptimalPrefixLabelWidthForEditor(int indentLevel)
		{
			return 181f;
		}
	}
}