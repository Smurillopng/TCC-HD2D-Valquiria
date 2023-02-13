using System;
using JetBrains.Annotations;
using Sisus.Attributes;
using UnityEngine;

namespace Sisus
{
	[Serializable, DrawerForComponent(typeof(Light), false, true)]
	public class LightDrawer : CustomEditorComponentDrawer
	{
		/// <inheritdoc />
		protected override int AppendLastCheckedId
		{
			get
			{
				return 200;
			}
		}

		/// <inheritdoc />
		protected override float EstimatedUnfoldedHeight
		{
			get
			{
				return 408f;
			}
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="targets"> The targets that the drawer represent. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="inspector"> The inspector in which the IDrawer are contained. Can not be null. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static LightDrawer Create(Component[] targets, [CanBeNull]IParentDrawer parent, [NotNull]IInspector inspector)
		{
			LightDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new LightDrawer();
			}
			result.Setup(targets, parent, inspector, null);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc/>
		protected override float GetOptimalPrefixLabelWidthForEditor(int indentLevel)
		{
			//UPDATE: If not using vertical, and so can overlap the "Realtime Shadows" header, then can reduce the width somewhat
			return ((Light)Target).shadows == LightShadows.None ? 120f : 163f;
		}
	}
}