#if UNITY_EDITOR
using System;
using UnityEditor.Animations;
using Sisus.Attributes;

namespace Sisus
{
	[Serializable, DrawerForAsset(typeof(AnimatorStateMachine), false, true)]
	public class AnimatorStateMachineDrawer : CustomEditorAssetDrawer
	{
		/// <inheritdoc />
		public override PrefixResizer PrefixResizer
		{
			get
			{
				return PrefixResizer.Disabled;
			}
		}

		/// <inheritdoc />
		public override bool RequiresConstantRepaint
		{
			get
			{
				return false;
			}
		}

		/// <inheritdoc />
		protected override bool HasDebugModeIcon
		{
			get
			{
				// hide the icon because it overlaps with the name field
				return false;
			}
		}

		/// <inheritdoc />
		protected override bool HasExecuteMethodIcon
		{
			get
			{
				// hide the icon because it overlaps with the name field
				return false;
			}
		}

		#if UNITY_2018_1_OR_NEWER // Presets were added in Unity 2018.1
		/// <inheritdoc />
		protected override bool HasPresetIcon
		{
			get
			{
				return true;
			}
		}
		#endif
	}
}
#endif