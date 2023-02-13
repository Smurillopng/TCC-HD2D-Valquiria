#if UNITY_EDITOR && UNITY_2018_1_OR_NEWER
using System;
using JetBrains.Annotations;
using Sisus.Attributes;
using UnityEditor.Presets;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sisus
{
	[Serializable, DrawerForAsset(typeof(Preset), false, true)]
	public class PresetDrawer : CustomEditorAssetDrawer
	{
		/// <inheritdoc />
		protected override bool HasPresetIcon
		{
			get
			{
				return false;
			}
		}

		/// <inheritdoc />
		protected override bool HasReferenceIcon
		{
			get
			{
				return false;
			}
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="targets"> The targets that the drawer represent. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="inspector"> The inspector in which the IDrawer are contained. Can not be null. </param>
		/// <returns> The instance, ready to be used. </returns>
		[NotNull]
		public static new PresetDrawer Create(Object[] targets, [CanBeNull]IParentDrawer parent, [NotNull]IInspector inspector)
		{
			PresetDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new PresetDrawer();
			}
			result.Setup(targets, targets, null, parent, inspector);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc/>
		protected override void Setup(Object[] setTargets, Object[] setEditorTargets, Type setEditorType, IParentDrawer setParent, IInspector setInspector)
		{
			#if DEV_MODE && UNITY_EDITOR
			Debug.Log(Msg("PresetDrawer.Setup(", setTargets, ", ", setEditorTargets, ", ", setEditorType, ")"));
			#endif

			//#if UNITY_2018_2_OR_NEWER //not sure which version exactly prompted this change, so won't do branching
			base.Setup(setTargets, setTargets, Types.GetInternalEditorType("UnityEditor.Presets.PresetEditor"), setParent, setInspector);
			//#else
			//base.Setup(setTargets, setEditorTargets, typeof(UnityEditorInternal.AssemblyDefinitionImporter), setParent, setInspector);
			//#endif
		}

		/// <inheritdoc />
		protected override void GetHeaderSubtitle(ref GUIContent subtitle)
		{
			// subtitle text should be left empty
			// because otherwise clipping will occur
			// with the Set As Default button
		}
	}
}
#endif