using JetBrains.Annotations;
using UnityEngine;

namespace Sisus
{
	/// <summary> Interface for editorless asset graphical user interface drawer. </summary>
	public interface IEditorlessAssetDrawer : IAssetDrawer
	{
		/// <summary>
		/// Sets up the drawer that implements IEditorlessAssetDrawer so that it is ready to be used.
		/// LateSetup should always be called right after this.
		/// </summary>
		/// <param name="setTargets"> The asset targets that the drawer represent. Can not be null. </param>
		/// <param name="setParent"> The parent drawer of these drawer. Can be null. </param>
		/// <param name="setInspector"> The inspector in which the IDrawer are contained. Can not be null. </param>
		void SetupInterface([NotNull] Object[] setTargets, [CanBeNull]IParentDrawer setParent, [NotNull]IInspector setInspector);
	}
}