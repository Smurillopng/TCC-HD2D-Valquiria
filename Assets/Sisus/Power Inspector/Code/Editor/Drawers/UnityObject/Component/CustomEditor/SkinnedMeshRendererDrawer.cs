#if UNITY_EDITOR
using System;
using JetBrains.Annotations;
using Sisus.Attributes;
using UnityEngine;

namespace Sisus
{
	[Serializable, DrawerForComponent(typeof(SkinnedMeshRenderer), false, true)]
	public class SkinnedMeshRendererDrawer : RendererDrawer
	{
		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="targets"> The targets that the drawer represent. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="inspector"> The inspector in which the IDrawer are contained. Can not be null. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static SkinnedMeshRendererDrawer Create(Component[] targets, [CanBeNull]IParentDrawer parent, [NotNull]IInspector inspector)
		{
			SkinnedMeshRendererDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new SkinnedMeshRendererDrawer();
			}
			result.Setup(targets, parent, inspector, null);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc/>
		protected override float GetOptimalPrefixLabelWidthForEditor(int indentLevel)
		{
			return 159f;
		}
	}
}
#endif