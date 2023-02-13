using System;
using JetBrains.Annotations;
using Object = UnityEngine.Object;

namespace Sisus
{
	public interface ICustomEditorAssetDrawer : IAssetDrawer, ICustomEditorDrawer
	{
		/// <summary>
		/// Sets up the ICustomEditorAssetDrawer so that they're ready to be used.
		/// LateSetup should always be called right after this.
		/// </summary>
		/// <param name="setTargets"> The targets that the drawer represent. Can not be null. </param>
		/// <param name="setEditorTargets"> Targets for use when creating the custom editor. E.g. AssetImporters for targets. </param>
		/// <param name="setEditorType"> The type to use for the custom editor. </param>
		/// <param name="setParent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="setInspector"> The inspector in which the IDrawer are contained. Can not be null. </param>
		void SetupInterface(Object[] setTargets, [CanBeNull]Object[] setEditorTargets, [CanBeNull]Type setEditorType, IParentDrawer setParent, [NotNull]IInspector setInspector);
	}
}