using System;
using Sisus.Attributes;
using UnityEngine;

namespace Sisus
{
	[Serializable, DrawerForComponent(typeof(AudioSource), false, true)]
	public class AudioSourceDrawer : CustomEditorComponentDrawer
	{
		/// <inheritdoc/>
		protected override float GetOptimalPrefixLabelWidthForEditor(int indentLevel)
		{
			return 158f;
		}
	}
}