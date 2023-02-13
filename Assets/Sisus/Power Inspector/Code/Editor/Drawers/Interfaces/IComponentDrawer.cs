using JetBrains.Annotations;
using UnityEngine;

namespace Sisus
{
	/// <summary>
	/// Interface implemented by drawers representing Components.
	/// </summary>
	public interface IComponentDrawer : IUnityObjectDrawer
	{
		/// <summary> Returns dimensions for a rectangle at the bottom of this control where another
		/// Component can be drag n dropped to trigger a reordering event. </summary>
		/// <value> The reorder drop rectangle. </value>
		Rect ReorderDropRect { get; }

		/// <summary>
		/// Gets the first Component target the drawers represent.
		/// </summary>
		/// <value>
		/// The Component target. Null if missing script.
		/// </value>
		[CanBeNull]
		Component Component { get; }

		/// <summary>
		/// Gets the Component targets the drawers represent.
		/// </summary>
		/// <value>
		/// The Component targets
		/// </value>
		[NotNull]
		Component[] Components { get; }

		/// <summary>
		/// Gets the GameObject that holds the first Component that the drawers represent.
		/// </summary>
		/// <value>
		/// The GameObject that holds the Component target
		/// </value>
		[NotNull]
		GameObject gameObject { get; }

		/// <summary>
		/// Gets parent or parent of parent that implements IGameObjectDrawer.
		/// </summary>
		[CanBeNull]
		IGameObjectDrawer GameObjectDrawer { get; }

		/// <summary> Sets the unfolded state of all Component targets. </summary>
		/// <param name="setUnfolded"> If true components are unfolded, if false they are folded. </param>
		/// <param name="collapseAllOthers"> If true all other Components in the inspector are collapsed. </param>
		/// <param name="setChildrenAlso"> If true, unfolded state of all member parent drawers inside the Component is also set. </param>
		void SetUnfolded(bool setUnfolded, bool collapseAllOthers, bool setChildrenAlso);

		/// <summary> Determine if we can move component order up in GameObject. </summary>
		/// <param name="allowMovingComponentAboveDownInstead">
		/// If true, then also check if can move the component above this component downwards instead,
		/// in practice achieving the same effect.
		/// This can be useful when MissingComponentDrawer themselves can't be moved, since they
		/// have no targets to reference, but the Component above them could still possibly be moved down.
		/// </param>
		/// <returns> True if we can move component up, false if not. </returns>
		bool CanMoveComponentUp(bool allowMovingComponentAboveDownInstead);

		/// <summary> Determine if we can move component order down in GameObject. </summary>
		/// <param name="allowMovingComponentBelowUpInstead">
		/// If true, then also check if can move the component below this component upwards instead,
		/// in practice achieving the same effect.
		/// This can be useful when MissingComponentDrawer themselves can't be moved, since they
		/// have no targets to reference, but the Component below them could still possibly be moved up.
		/// </param>
		/// <returns> True if we can move component down, false if not. </returns>
		bool CanMoveComponentDown(bool allowMovingComponentBelowUpInstead);

		#if UNITY_EDITOR
		/// <summary> Move component order up. </summary>
		void MoveUp();

		/// <summary> Move component order down. </summary>
		void MoveDown();
		#endif

		/// <summary>
		/// Callback for when the view menu is opening and an opportunity for adding items to it before it opens.
		/// </summary>
		/// <param name="menu"> [in,out] The view menu that is opening. </param>
		void AddItemsToOpeningViewMenu(ref Menu menu);
	}
}