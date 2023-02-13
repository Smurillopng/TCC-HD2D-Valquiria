#if UNITY_EDITOR && UNITY_2017_3_OR_NEWER //AssemblyDefinitionAsset did not exist before Unity version 2017.3
using System;
using Sisus.Attributes;
using Object = UnityEngine.Object;

namespace Sisus
{
	[Serializable, DrawerForAsset(typeof(UnityEditorInternal.AssemblyDefinitionAsset), false, true), DrawerForAsset(typeof(UnityEditorInternal.AssemblyDefinitionImporter), false, true)]
	public class AssemblyDefinitionDrawer : CustomEditorAssetDrawer
	{
		#if UNITY_2018_1_OR_NEWER // Presets were added in Unity 2018.1
		/// <inheritdoc />
		protected override bool HasPresetIcon
		{
			get
			{
				return false;
			}
		}
		#endif

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
		public new static AssemblyDefinitionDrawer Create(Object[] targets, IParentDrawer parent, IInspector inspector)
		{
			AssemblyDefinitionDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new AssemblyDefinitionDrawer();
			}
			result.Setup(targets, targets, null, parent, inspector);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc />
		protected override void Setup(Object[] setTargets, Object[] setEditorTargets, Type setEditorType, IParentDrawer setParent, IInspector setInspector)
		{
			if(setEditorTargets == null)
			{
				AssetImporters.TryGet(setTargets, ref setEditorTargets);
			}
			base.Setup(setTargets, setEditorTargets, null, setParent, setInspector);
		}

		/// <inheritdoc />
		protected override float GetOptimalPrefixLabelWidthForEditor(int indentLevel)
		{
			return 150f;
		}
	}
}
#endif