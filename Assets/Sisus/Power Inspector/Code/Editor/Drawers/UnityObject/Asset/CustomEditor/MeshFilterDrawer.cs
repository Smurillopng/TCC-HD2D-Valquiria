#if UNITY_EDITOR
using System;
using Sisus.Attributes;
using UnityEngine;

namespace Sisus
{
	[Serializable, DrawerForComponent(typeof(MeshFilter), false, true)]
	public class MeshFilterDrawer : CustomEditorComponentDrawer
	{
		/// <inheritdoc />
		public override PrefixResizer PrefixResizer
		{
			get
			{
				return PrefixResizer.Vertical;
			}
		}

		/// <inheritdoc />
		protected override float EstimatedUnfoldedHeight
		{
			get
			{
				#if UNITY_2019_3_OR_NEWER
				return 52f;
				#else
				return 41f;
				#endif
			}
		}

		/// <inheritdoc/>
		protected override float GetOptimalPrefixLabelWidthForEditor(int indentLevel)
		{
			#if UNITY_2019_3_OR_NEWER
			return 56f;
			#else
			return 54f;
			#endif
		}
	}
}
#endif