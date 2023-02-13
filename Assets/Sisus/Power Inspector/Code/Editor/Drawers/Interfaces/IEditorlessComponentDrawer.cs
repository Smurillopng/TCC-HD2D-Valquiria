using JetBrains.Annotations;
using UnityEngine;

namespace Sisus
{
	/// <summary>
	/// Interface implemented by drawers representing Components that do NOT use an Editor for drawing their members,
	/// but nested member drawers for drawing them.
	/// </summary>
	public interface IEditorlessComponentDrawer : IComponentDrawer
	{
		/// <summary>
		/// Sets up the drawer that implements IEditorlessComponentDrawer so that it is ready to be used.
		/// LateSetup should always be called right after this.
		/// </summary>
		/// <param name="setTargets"> The Component targets that the drawers represent. </param>
		/// <param name="setParent">
		/// The drawers that contain these drawers. Usually GameObjectDrawer that represent the GameObjects that
		/// contain the target Components represented by these drawers. Can be null.
		/// </param>
		/// <param name="setInspector"> The inspector in which the IDrawer are contained. Can not be null. </param>
		void SetupInterface([NotNull]Component[] setTargets, [NotNull]IParentDrawer setParent, [NotNull]IInspector setInspector);
	}
}