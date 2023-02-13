#if UNITY_EDITOR
using System;
using JetBrains.Annotations;
using Sisus.Attributes;
using UnityEditor;
using UnityEngine;
using UnityEngine.Video;
using Object = UnityEngine.Object;

namespace Sisus
{
	[Serializable, DrawerForAsset(typeof(VideoClip), false, true)]
	public class VideoClipDrawer : CustomEditorAssetDrawer
	{
		/// <inheritdoc/>
		protected override Editor HeaderEditor
		{
			get
			{
				// needed to get preset icon and Import Version drop down to show
				return Editor;
			}
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="targets"> The targets that the drawer represent. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="inspector"> The inspector in which the IDrawer are contained. Can not be null. </param>
		/// <returns> The instance, ready to be used. </returns>
		[NotNull]
		public static new VideoClipDrawer Create(Object[] targets, [CanBeNull]IParentDrawer parent, [NotNull]IInspector inspector)
		{
			VideoClipDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new VideoClipDrawer();
			}
			result.Setup(targets, null, null, parent, inspector);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc/>
		protected override float GetOptimalPrefixLabelWidthForEditor(int indentLevel)
		{
			return 120f;
		}

		/// <inheritdoc/>
		protected override void GetHeaderSubtitle(ref GUIContent subtitle)
		{
			// no subtitle text to prevent clipping with the Import Version dropdown element
		}
	}
}
#endif