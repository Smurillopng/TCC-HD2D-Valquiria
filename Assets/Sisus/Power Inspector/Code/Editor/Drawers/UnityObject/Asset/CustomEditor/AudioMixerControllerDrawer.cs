#if UNITY_EDITOR
using System;
using JetBrains.Annotations;
using Sisus.Attributes;
using Object = UnityEngine.Object;

namespace Sisus
{
	[Serializable, DrawerForAsset("UnityEditor.Audio.AudioMixerController", false, true)]
	public class AudioMixerControllerDrawer : CustomEditorAssetDrawer
	{
		#if UNITY_2018_1_OR_NEWER
		/// <inheritdoc/>
		protected override bool HasPresetIcon
		{
			get
			{
				return false;
			}
		}
		#endif
		
		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="targets"> The targets that the drawer represent. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="inspector"> The inspector in which the IDrawer are contained. Can not be null. </param>
		/// <returns> The instance, ready to be used. </returns>
		[NotNull]
		public static new AudioMixerControllerDrawer Create(Object[] targets, [CanBeNull]IParentDrawer parent, [NotNull]IInspector inspector)
		{
			AudioMixerControllerDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new AudioMixerControllerDrawer();
			}
			result.Setup(targets, null, null, parent, inspector);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc/>
		protected override float GetOptimalPrefixLabelWidthForEditor(int indentLevel)
		{
			return 140f;
		}
	}
}
#endif