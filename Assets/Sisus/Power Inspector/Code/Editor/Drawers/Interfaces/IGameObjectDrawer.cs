using System;
using System.Collections.Generic;
using UnityEngine;
using JetBrains.Annotations;
using Sisus.Attributes;

namespace Sisus
{
	/// <summary>
	/// Interface that should be implemented by drawers representing GameObjects.
	/// </summary>
	public interface IGameObjectDrawer : IAssetDrawer, IReorderableParent, IEnumerable<IComponentDrawer>
	{
		/// <summary>
		/// Gets the first target GameObject the drawer represents.
		/// </summary>
		/// <value>
		/// The target GameObjects.
		/// </value>
		[NotNull]
		GameObject GameObject { get; }

		/// <summary>
		/// Gets the target GameObjects the drawer represents.
		/// </summary>
		/// <value>
		/// The target GameObjects.
		/// </value>
		[NotNull]
		GameObject[] GameObjects { get; }

		/// <summary>
		/// Gets the drawer for the Add Component button member.
		/// </summary>
		/// <value>
		/// Add Component button GUI drawer.
		/// </value>
		[CanBeNull]
		AddComponentButtonDrawer AddComponentButton {get; }

		/// <summary>
		/// Move components underneath components.
		/// Also supports copying Components over in cases where where source and target GameObjects don't match.
		/// </summary>
		/// <param name="componentsToMove"> The components that are to be moved. </param>
		/// <param name="moveUnderneath"> The components under which the moved components are to be placed. </param>
		/// <value>
		/// The Components[] that were moved or copied over.
		/// </value>
		[NotNull]
		Component[] MoveComponentsUnderComponents([NotNull]Component[] componentsToMove, [NotNull]Component[] moveUnderneath);

		#if DEV_MODE
		/// <summary>
		/// Developer-only method. Logs errors if members don't pass assertations.
		/// </summary>
		void ValidateMembers();
		#endif

		/// <summary> Sets up an instance of the drawer for usage. </summary>
		/// <param name="setTarget"> The target that the drawers represents. Can not be null. </param>
		/// <param name="setParent"> The parent drawers of the created drawers. Can be null. </param>
		/// <param name="setInspector"> The inspector in which the IDrawer are contained. Can not be null. </param>
		void Setup([NotNull]GameObject setTarget, [CanBeNull]IParentDrawer setParent, [NotNull]IInspector setInspector);

		/// <summary> Sets up an instance of the drawers ready for usage. </summary>
		/// <param name="setTargets"> The targets that the drawers represent. Can not be null. </param>
		/// <param name="setParent"> The parent drawers of the created drawers. Can be null. </param>
		/// <param name="setInspector"> The inspector in which the IDrawer are contained. Can not be null. </param>
		void Setup([NotNull]GameObject[] setTargets, [CanBeNull]IParentDrawer setParent, [NotNull]IInspector setInspector);

		/// <summary>
		/// Gets number of visible members that implement IComponentDrawer.
		/// </summary>
		/// <returns>
		/// Number of visible members that implement IComponentDrawer.
		/// </returns>
		int VisibleComponentMemberCount();

		/// <summary>
		/// Add Component Drawer at given index in members.
		/// </summary>
		/// <param name="memberIndex"> Index of new component in members array. </param>
		/// <param name="componentDrawer"> New component drawer to add. </param>
		void AddComponentMember(int memberIndex, [NotNull]IComponentDrawer componentDrawer);


		/// <summary>
		/// Add component of type to all GameObject targets of the GameObjectDrawer
		/// and update the members of the GameObjectDrawer to contain the drawer for the added component if needed.
		/// </summary>
		/// <param name="type"> Type of component to add to all target GameObjects</param>
		/// <param name="scrollToShow"> Should we scroll to show the new drawer once it has been added? </param>
		/// <param name="index">
		/// Index in members array that is used to determine where in GameObject targets the component should be added.
		/// If -1 then component will be added as the last component on each target GameObject.
		/// </param>
		/// <returns> The drawer for the added component. Null if could not add component. </returns>
		[CanBeNull]
		IComponentDrawer AddComponent([NotNull]Type type, bool scrollToShow = true, int index = -1);

		/// <summary>
		/// Add components of types to all GameObject targets of the GameObjectDrawer
		/// and update the members of the GameObjectDrawer to contain the drawer for the added components if needed.
		/// </summary>
		/// <param name="types"> List containing types of components to add to all target GameObjects. </param>
		/// <param name="scrollToShow"> Should we scroll to show the new drawer once it has been added? </param>
		/// <returns> The drawers for the added components. Empty array if could not add any components. </returns>
		[NotNull]
		IComponentDrawer[] AddComponents([NotNullOrEmpty]List<Type> types, bool scrollToShow = true);

		/// <summary>
		/// Replace component drawer with new component drawer that represents a component of given type.
		/// </summary>
		/// <param name="replace"> Existing component drawer to replace. </param>
		/// <param name="replacementType"> Component type that replacing component drawer represents. </param>
		/// <param name="scrollToShow"> Should we scroll to show the new drawer once it has been added? </param>
		/// <param name="checkForDependencies">
		/// If true then will check if removed component has any dependencies, and prompt user to remove all dependent components also if any were found.
		/// If false then will try to remove component without checking any dependencies. This should also be done, if you already know that there are no dependencies.
		/// </param>
		/// <returns> The new component drawer that replaced the existing one. Null if could not add new component. </returns>
		[CanBeNull]
		IComponentDrawer ReplaceComponent([NotNull]IComponentDrawer replace, [NotNull]Type replacementType, bool scrollToShow = true, bool checkForDependencies = false);

		/// <summary>
		/// Remove component targets of given component drawer member from target GameObjects.
		/// </summary>
		/// <param name="remove"> Drawer of components that should be removed. </param>
		/// <returns>
		/// If a direct member of the GameObject drawer was removed, returns index of said member. If no direct member was removed, returns -1.
		/// </returns>
		int RemoveComponent([NotNull]IComponentDrawer remove);

		void RebuildMaterialDrawers();
	}
}