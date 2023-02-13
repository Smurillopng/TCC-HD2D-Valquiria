using System;
using JetBrains.Annotations;
using Sisus.Attributes;
using UnityEngine;

namespace Sisus
{
	[Serializable, DrawerForComponent(typeof(BoxCollider2D), false, true)]
	public class BoxCollider2DDrawer : CustomEditorComponentDrawer
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
		protected override float PrefixResizerMaxHeight
		{
			get
			{
				return 325f;
			}
		}
		
		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="targets"> The targets that the drawer represent. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="inspector"> The inspector in which the IDrawer are contained. Can not be null. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static BoxCollider2DDrawer Create(Component[] targets, [CanBeNull]IParentDrawer parent, [NotNull]IInspector inspector)
		{
			BoxCollider2DDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new BoxCollider2DDrawer();
			}
			result.Setup(targets, parent, inspector, null);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc/>
		protected override float GetOptimalPrefixLabelWidthForEditor(int indentLevel)
		{
			return 136f;
		}
	}
}