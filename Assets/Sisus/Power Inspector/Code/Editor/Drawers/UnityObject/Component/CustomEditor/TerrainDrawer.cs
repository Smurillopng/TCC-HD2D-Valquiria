#if UNITY_EDITOR
using System;
using JetBrains.Annotations;
using Sisus.Attributes;
using UnityEngine;

namespace Sisus
{
	[Serializable, DrawerForComponent(typeof(Terrain), false, true)]
	public class TerrainDrawer : CustomEditorComponentDrawer
	{
		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="targets"> The targets that the drawer represent. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="inspector"> The inspector in which the IDrawer are contained. Can not be null. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static TerrainDrawer Create(Component[] targets, [CanBeNull]IParentDrawer parent, [NotNull]IInspector inspector)
		{
			TerrainDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new TerrainDrawer();
			}
			result.Setup(targets, parent, inspector, null);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc/>
		protected override bool WantsToOverrideFieldFocusing()
		{
			return false;
		}
	}
}
#endif