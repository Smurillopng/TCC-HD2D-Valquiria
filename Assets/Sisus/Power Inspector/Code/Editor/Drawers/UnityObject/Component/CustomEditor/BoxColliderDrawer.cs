using System;
using Sisus.Attributes;
using UnityEngine;

namespace Sisus
{
	[Serializable, DrawerForComponent(typeof(BoxCollider), false, true)]
	public class BoxColliderDrawer : CustomEditorComponentDrawer
	{
		/// <inheritdoc/>
		protected override float GetOptimalPrefixLabelWidthForEditor(int indentLevel)
		{
			return 78f;
		}
	}
}