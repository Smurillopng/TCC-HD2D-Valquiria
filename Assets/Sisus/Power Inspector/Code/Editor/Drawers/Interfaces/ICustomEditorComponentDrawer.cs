using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Sisus
{
	/// <summary>
	/// Interface implemented by drawer representing Components that (can) use an Editor for drawing their members.
	/// </summary>
	public interface ICustomEditorComponentDrawer : IComponentDrawer, ICustomEditorDrawer
	{
		/// <summary>
		/// Sets up the drawer that implements ICustomEditorComponentDrawer so that it is ready to be used.
		/// LateSetup should always be called right after this.
		/// </summary>
		/// <param name="setEditorType"> The type of the CustomEditor that can be used for for drawing the Component. Can be null. </param>
		/// <param name="setTargets"> The Component targets that the drawer represent. </param>
		/// <param name="setParent">
		/// The drawer that contain these drawer. Usually GameObjectDrawer target represent the GameObjects that
		/// contain the target Components represented by these drawer. Can be null.
		/// </param>
		/// <param name="setInspector"> The inspector in which the IDrawer are contained. Can not be null. </param>
		void SetupInterface([CanBeNull] Type setEditorType, [NotNull]Component[] setTargets, [CanBeNull]IParentDrawer setParent, [NotNull] IInspector setInspector);
	}
}