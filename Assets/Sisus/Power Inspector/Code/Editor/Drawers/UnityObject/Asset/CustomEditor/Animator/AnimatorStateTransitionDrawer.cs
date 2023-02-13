#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEditor.Animations;
using Sisus.Attributes;

namespace Sisus
{
	[Serializable, DrawerForAsset(typeof(AnimatorStateTransition), false, true)]
	public class AnimatorStateTransitionDrawer : CustomEditorAssetDrawer
	{
		/// <inheritdoc />
		protected override float GetOptimalPrefixLabelWidthForEditor(int indentLevel)
		{
			return 164f;
		}

		/// <inheritdoc />
		protected override void GetHeaderSubtitle(ref GUIContent subtitle)
		{
			// hide the subtitle because it overlaps with built-in "X AnimatorTransitionBase" subtitle
			subtitle.text = "";
			subtitle.tooltip = "";
		}
	}
}
#endif