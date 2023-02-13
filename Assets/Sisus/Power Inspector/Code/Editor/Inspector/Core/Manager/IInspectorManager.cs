using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sisus
{
	public interface IInspectorManager
	{
		SelectionChange OnSelectionChanged { get; set; }
		OnActiveInspectorChangedCallback OnActiveInspectorChanged { get; set; }
		Action<IInspector> OnNewInspectorRegistered { get; set; }

		/// <summary>
		/// Gets or sets the inspector that is active or was last active. 
		/// Inspectors are should be active when being drawn or interacted with in some way.
		/// </summary>
		/// <value>
		/// The last the active inspector. 
		/// </value>
		IInspector ActiveInspector { get; set; }

		/// <summary>
		/// Gets all currently active inspector instances.
		/// 
		/// When new inspector instances are created, they are added to the end of this list.
		/// </summary>
		/// <value> All active inspector. </value>
		List<IInspector> ActiveInstances { get; }

		/// <summary>
		/// Gets first inspector amongst all instances, or null if no inspector instances exist.
		/// </summary>
		[CanBeNull]
		IInspector FirstInspector
		{
			get;
		}

		/// <summary>
		/// Gets last inspector amongst all instances, or null if no inspector instances exist.
		/// </summary>
		[CanBeNull]
		IInspector LastInspector
		{
			get;
		}

		/// <summary>
		/// Gets first inspector drawer amongst all instances, or null if no inspector drawer instances exist.
		/// </summary>
		[CanBeNull]
		IInspectorDrawer FirstInspectorDrawer
		{
			get;
		}

		/// <summary>
		/// Gets or sets a value indicating whether the ignore all mouse inputs.
		/// This might be true e.g. when an overlay dialog is open.
		/// </summary>
		/// <value> True if ignore all mouse inputs, false if not. </value>
		bool IgnoreAllMouseInputs { get; set; }
		
		/// <summary>
		/// Gets drawer that currently has keyboard focus.
		/// </summary>
		[CanBeNull]
		IDrawer FocusedDrawer { get; }

		/// <summary>
		/// Gets drawers that are currently selected as part of a multiselection.
		/// </summary>
		[NotNull]
		List<IDrawer> MultiSelectedControls { get; }
		
		/// <summary>
		/// Gets inspector that is currently selected, or null if no inspector is selected.
		/// </summary>
		[CanBeNull]
		IInspector SelectedInspector { get; }

		/// <summary>
		/// Gets part of currently selected inspector that has keyboard focus.
		/// 
		/// If no inspector is currently selected, returns None.
		/// </summary>
		InspectorPart SelectedInspectorPart { get; }

		/// <summary>
		/// Gets selectable drawer over which the cursor is currently positioned, or null if no selectable drawer is currently mouseovered.
		/// 
		/// If cursor overlaps the ClickToSelect areas of multiple drawers, then member drawers are prioritized over parent drawers.
		/// </summary>
		[CanBeNull]
		IDrawer MouseoveredSelectable { get; }

		/// <summary>
		/// Gets right-clickable drawer over which the cursor is currently positioned, or null if no right-clickable drawer is currently mouseovered.
		/// 
		/// If cursor overlaps the RightClickArea of multiple drawers, then member drawers are prioritized over parent drawers.
		/// </summary>
		[CanBeNull]
		IDrawer MouseoveredRightClickable { get; }

		/// <summary>
		/// Gets inspector which is currently mouseovered, or null if no inspector is currently mouseovered.
		/// </summary>
		[CanBeNull]
		IInspector MouseoveredInspector { get; }

		/// <summary>
		/// Gets part of currently mouseovered inspector over which the cursor resides.
		/// 
		/// If no inspector is currently mouseovered, returns None.
		/// </summary>
		InspectorPart MouseoveredInspectorPart { get; }

		/// <summary>
		/// Gets information related to the state of the left mouse button, as well as clicking, dragging and reordering.
		/// </summary>
		[NotNull]
		MouseDownInfo MouseDownInfo { get; }

		/// <summary>
		/// Gets information related to the state of the right mouse button.
		/// </summary>
		[NotNull]
		RightClickInfo RightClickInfo { get; }

		/// <summary>
		/// True if multiple drawers are currently selected inside some inspector.
		/// </summary>
		bool HasMultiSelectedControls { get; }

		/// <summary>
		/// Method that is called during every OnLayout event.
		/// </summary>
		void OnLayout();

		/// <summary>
		/// Invokes action during the beginning of the next Layout event of OnGUI.
		/// </summary>
		/// <param name="action"> The action to invoke. </param>
		/// <param name="changingInspectorDrawer">
		/// (Optional) InspectorDrawer whose view might get changed as a result of the action.
		/// If not null, RefreshView will get called on it.
		/// </param>
		void OnNextLayout([NotNull]Action action, IInspectorDrawer changingInspectorDrawer = null);

		/// <summary>
		/// Invokes action during the beginning of the next OnGUI event.
		/// </summary>
		/// <param name="action"> The action to invoke. </param>
		/// <param name="changingInspectorDrawer">
		/// (Optional) InspectorDrawer whose view might get changed as a result of the action.
		/// If not null, RefreshView will get called on it.
		/// </param>
		void OnNextOnGUI([NotNull]Action action, IInspectorDrawer changingInspectorDrawer = null);

		/// <summary>
		/// Invokes action during the beginning of the next Layout event of OnGUI, unless target has been
		/// placed in the object pool before the time that the action should otherwise be invoked.
		/// </summary>
		/// <param name="action"> Information about the action to invoke and the target drawers instance. </param>
		/// <param name="changingInspectorDrawer">
		/// (Optional) InspectorDrawer whose view might get changed as a result of the action.
		/// If not null, RefreshView will get called on it.
		/// </param>
		void OnNextLayout([NotNull]IDrawerDelayableAction action, IInspectorDrawer changingInspectorDrawer = null);

		/// <summary>
		/// Method that is called during every frame that a keyboard key is released.
		/// </summary>
		/// <param name="e"> Information about the event, including they KeyCode of the released key. </param>
		void OnKeyUp([NotNull]Event e);
		
		/// <summary> Finds an existing Inspector instance (if any), favoring the last active (drawn or used) instance. </summary>
		/// <returns> An inspector instance. Null if none exist. </returns>
		[CanBeNull]
		IInspector ActiveSelectedOrDefaultInspector();

		/// <summary> Finds an existing Inspector instance (if any), favoring the last selected instance. </summary>
		/// <returns> An inspector instance. Null if none exist. </returns>
		[CanBeNull]
		IInspector LastSelectedActiveOrDefaultInspector();

		/// <summary> Finds an existing Inspector instance (if any), favoring the last selected instance. </summary>
		/// <param name="targetingMode">
		/// Required targeting mode for inspector. If this is set to Hierarchy, then inspectors targeting Project view
		/// will be ignored and vice versa. If this is set to All, then any inspector instances can be returned, no
		/// matter what their targeting mode is.
		/// </param>
		/// <returns> An inspector instance. Null if none exist. </returns>
		[CanBeNull]
		IInspector LastSelectedActiveOrDefaultInspector(InspectorTargetingMode targetingMode);

		/// <summary> Finds an existing Inspector instance (if any), favoring the last selected instance. </summary>
		/// <param name="targetingMode">
		/// Required targeting mode for inspector. If this is set to Hierarchy, then inspectors targeting Project view
		/// will be ignored. If this is set to Project, then inspectors targeting Hierarchy view will be ignored.
		/// If this is set to All, then any inspector instance can be returned, no matter what their targeting mode is.
		/// </param>
		/// <param name="splittability">
		/// Required splittability for inspector.
		/// If this is set to IsSplittable, then inspectors whose drawer DO NOT implement ISplittableInspectorDrawer will be ignored.
		/// If this is set to NotSplittable, then inspectors whose drawer DO implement ISplittableInspectorDrawer will be ignored.
		/// If this is set to Any, then any inspector instance can be returned, no matter whether or not they implement ISplittableInspectorDrawer.
		/// </param>
		/// <returns> An inspector instance. Null if none exist. </returns>
		[CanBeNull]
		IInspector LastSelectedActiveOrDefaultInspector(InspectorTargetingMode targetingMode, InspectorSplittability splittability);

		/// <summary>
		/// Selects the inspector part for the given reason.
		/// </summary>
		/// <param name="inspector"> Inspector that is being selected, or null if an inspector is being deselected. </param>
		/// <param name="part"> Part of inspector that is being selected, or None if an inspector is being deselected. </param>
		/// <param name="reason"> Reason why the change in selection is taking place. </param>
		void Select([CanBeNull]IInspector inspector, InspectorPart part, ReasonSelectionChanged reason);

		/// <summary>
		/// Selects the drawer inside the specified part of an inspector for the given reason.
		/// </summary>
		/// <param name="inspector"> Inspector that is selected or is being selected. If a drawer is being selected, then this is the inspector that contains the drawer. Null if an inspector is being deselected. </param>
		/// <param name="part"> Part of inspector that is selected or being selected, or None if an inspector is being deselected. If a drawer is selected, this should be Viewport. </param>
		/// <param name="control"> Drawer that is being selected, or null if no drawer should be set as selected. </param>
		/// <param name="reason"> Reason why the change in selection is taking place. </param>
		void Select([CanBeNull]IInspector inspector, InspectorPart part, IDrawer control, ReasonSelectionChanged reason);

		/// <summary>
		/// Adds the given drawer to the current multi-selection.
		/// </summary>
		/// <param name="add"> Drawer to add to selection. </param>
		/// <param name="reason"> Reason why the change in selection is taking place. </param>
		void AddToSelection([NotNull]IDrawer add, ReasonSelectionChanged reason);

		/// <summary>
		/// Removes the given drawer from the current multi-selection.
		/// </summary>
		/// <param name="add"> Drawer to remove from selection. </param>
		/// <param name="reason"> Reason why the change in selection is taking place. </param>
		void RemoveFromSelection([NotNull]IDrawer remove, ReasonSelectionChanged reason);

		/// <summary>
		/// Determines whether or not the given drawer is currently selected.
		/// 
		/// Returns true if drawer has keyboard focus, or if drawer is part of a multiselection.
		/// </summary>
		/// <param name="target"> The drawer to test. </param>
		/// <returns> True if drawer is focused or selected. </returns>
		bool IsSelected([NotNull]IDrawer target);

		/// <summary>
		/// Determines whether or not drawer is part of a multi-selection but does not have keyboard focus.
		/// </summary>
		/// <param name="testControl"> The drawer to test. </param>
		/// <returns> True if drawer is part of multi-selection but does not have keyboard focus. </returns>
		bool IsFocusedButNotSelected([NotNull]IDrawer testControl);

		/// <summary>
		/// Sets the currently mouseovered inspector and selectable drawer to the given values.
		/// </summary>
		/// <param name="inspector"> The inspector that is currently mouseovered, or null if no inspector is mouseovered. </param>
		/// <param name="control"> The selectable mouseovered drawer, or null. </param>
		void SetMouseoveredSelectable([CanBeNull]IInspector inspector, [CanBeNull]IDrawer control);

		/// <summary>
		/// Sets the currently mouseovered inspector and right-clickable drawer to the given values.
		/// </summary>
		/// <param name="inspector"> The inspector that is currently mouseovered, or null if no inspector is mouseovered. </param>
		/// <param name="control"> The right-clickable mouseovered drawer, or null. </param>
		void SetMouseoveredRightClickable(IInspector inspector, IDrawer control);

		/// <summary>
		/// Sets the currently mouseovered inspector and inspector part.
		/// </summary>
		/// <param name="inspector"> The inspector that is currently mouseovered, or null if no inspector is mouseovered. </param>
		/// <param name="inspectorPart"> The inspector part that is currently mouseovered, or None if no inspector is mouseovered. </param>
		void SetMouseoveredInspector(IInspector inspector, InspectorPart inspectorPart);

		/// <summary>
		/// Registers given key as being held down. If given key is held down long enough, then the inspector manager can
		/// start broadcasting simulated OnKeyDown / OnKeyUp events for the given key in rapid succession, until the key is relesed.
		/// </summary>
		/// <param name="keyCode"> </param>
		/// <param name="key"></param>
		void RegisterKeyHeldDown(KeyCode keyCode, string key);

		/// <summary>
		/// Attempts to find drawer for given Unity Object target inside the selected or active inspector.
		/// </summary>
		/// <param name="target"></param>
		/// <returns></returns>
		IDrawer FindDrawer(Object target);

		/// <summary> Gets instance of type that implements IInspector that was last selected (if any). </summary>
		/// <param name="inspectorType"> Type of the inspector. This cannot be null. </param>
		/// <returns> The last selected instance, or null if no inspector of type exists that has been previously selected. </returns>
		[CanBeNull]
		IInspector GetLastSelectedInspector([NotNull]Type inspectorType);

		/// <summary>
		/// Gets first inspector (if any) that is currently visible.
		/// If the EditorWindow containing the inspector is not the focused tab in its tab group, then the inspector is not visible.
		/// </summary>
		/// <returns> A visible inspector. </returns>
		[CanBeNull]
		IInspector GetFirstVisibleInspector();

		/// <summary> Gets instance of type that implements IInspectorDrawer that was last selected (if any). </summary>
		/// <param name="inspectorDrawerType"> Type of the inspector. This cannot be null. </param>
		/// <returns> The last selected instance, or null if no inspector of type exists that has been previously selected. </returns>
		[CanBeNull]
		IInspectorDrawer GetLastSelectedInspectorDrawer([NotNull]Type inspectorDrawerType);

		/// <summary> Gets instance of EditorWindow and is currently or last selected (if any). </summary>
		/// <returns> The last selected editor window, or null if EditorWindow has been previously selected. </returns>
		[CanBeNull]
		UnityEditor.EditorWindow GetLastSelectedEditorWindow();

		/// <summary>
		/// Returns a unique name for the inspector based on its type and number of instances of same type that existed when the inspecto
		/// r was created.
		/// The name should remain stable through the lifetime of the inspector, though it could change slightly during assembly reloads.
		/// <param name="inspector"> The target inspector. This cannot be null. </param>
		/// <returns> Unique name. </returns>
		string GetUniqueName([NotNull]IInspector inspector);

		/// <summary>
		/// Adds inspector to active instances.
		/// </summary>
		/// <param name="inspector"></param>
		void AddToActiveInstances([NotNull]IInspector inspector);
	}
}