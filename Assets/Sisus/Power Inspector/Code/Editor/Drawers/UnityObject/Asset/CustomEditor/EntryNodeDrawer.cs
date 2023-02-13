#if UNITY_EDITOR
using System;
using Sisus.Attributes;

namespace Sisus
{
	[Serializable, DrawerForAsset("UnityEditor.Graphs.AnimationStateMachine.EntryNode", false, true)]
	public class EntryNodeDrawer : CustomEditorAssetDrawer
	{
		/// <inheritdoc />
		protected override float GetOptimalPrefixLabelWidthForEditor(int indentLevel)
		{
			return 94f;
		}
	}
}
#endif