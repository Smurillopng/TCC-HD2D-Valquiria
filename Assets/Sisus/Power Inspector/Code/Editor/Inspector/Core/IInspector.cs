using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sisus
{
	public interface IInspector
	{
		/// <summary> Gets class containing preferences of the Inspector. </summary>
		/// <value> The inspector preferences container. </value>
		[NotNull]
		InspectorPreferences Preferences { get; }

		/// <summary>
		/// Gets the drawer of the inspector, which handles passing Unity events such as OnGUI to the inspector.
		/// </summary>
		/// <value> The inspector's drawer. Usually an EditorWindow (in the editor) or a MonoBehaviour (in builds). </value>
		[NotNull]
		IInspectorDrawer InspectorDrawer { get; }

		/// <summary>
		/// Gets the manager of the inspector, which holds data shared between multiple Inspectors.
		/// </summary>
		/// <value> The manager. </value>
		[NotNull]
		IInspectorManager Manager { get; }

		/// <summary> Gets instance of class that is by default responsible for determining which drawers should be used for which Unity Object targets and class members inside the inspector view. </summary>
		/// <value> The drawer provider. </value>
		[NotNull]
		IDrawerProvider DrawerProvider { get; }

		/// <summary> Gets class containing information pertaining to the current state of the Inspector. </summary>
		/// <value> The inspector state container class. </value>
		[NotNull]
		InspectorState State { get; }

		/// <summary> Gets a value indicating whether inspector's filter field currently has a filter
		/// which potentially affects what content is displayed for the selected target. </summary>
		/// <value> True if view is currently filtered, false if not. </value>
		bool HasFilterAffectingInspectedTargetContent { get; }

		/// <summary> Gets a value indicating whether the inspector is currently selected. </summary>
		/// <value> True if selected, false if not. </value>
		bool Selected { get; }

		/// <summary>
		/// Gets the drawer situated inside this inspector which has focus at this time (if any).
		/// </summary>
		/// <value> The focused drawer. Null if inspector is not selected or it has no selected drawer. </value>
		[CanBeNull]
		IDrawer FocusedDrawer { get; }
		
		/// <summary> Gets the toolbar of the inspector, if it has one. </summary>
		/// <value> The inspector toolbar, or null if the inspector has no toolbar. </value>
		[CanBeNull]
		IInspectorToolbar Toolbar { get; }

		/// <summary> Gets the height of the inspector toolbar. </summary>
		/// <value> The height of the toolbar, or zero if inspector has no toolbar. </value>
		float ToolbarHeight { get; }

		/// <summary> Gets the height of the currently open preview area. </summary>
		/// <value> The height of the preview area, or zero if no preview area is open. </value>
		float PreviewAreaHeight { get; }

		/// <summary>
		/// Gets or sets the part of the inspector which is currently being drawn during an OnGUI event.
		/// This is set to true right before the Draw method of an inspector part is called, and false right after.
		/// </summary>
		/// <value> The part currently being drawn, or None if no part is currently being drawn. </value>
		InspectorPart NowDrawingPart { get; set; }

		/// <summary> Gets the selected objects. </summary>
		/// <value> The selected objects. </value>
		[NotNull]
		Object[] SelectedObjects { get; }

		/// <summary> Gets or sets the delegate callback for when the filter of the inspector is changing. </summary>
		/// <value> Callback invoked right before the filter changes to a new value. </value>
		[CanBeNull]
		Action<SearchFilter> OnFilterChanging { get; set; }
		
		/// <summary> Time in seconds since startup when mouse or keyboard input was last given to this inspector by the user. </summary>
		/// <value> The last input time. </value>
		float LastInputTime { get; set; }

		/// <summary> Has Setup phase been completed for the inspector? </summary>
		/// <value> Is setup done value. </value>
		bool SetupDone { get; }

		/// <summary> Gets the part of the Inspector which is currently mouseovered. </summary>
		/// <value> If the cursor is over the inspector, returns the mouseovered part, else returns None. </value>
		InspectorPart MouseoveredPart
		{
			get;
		}

		/// <summary>
		/// Requests that the drawer of the inspector refreshes the view, rebuilding the layout and repainting it.
		/// </summary>
		void RefreshView();

		/// <summary>
		/// Sets Debug Mode+ on for all targets shown by this inspector.
		/// </summary>
		void EnableDebugMode();

		/// <summary>
		/// Sets Debug Mode+ off for all targets shown by this inspector.
		/// </summary>
		void DisableDebugMode();

		/// <summary>
		/// Returns true if the UnityEngine.Object is currently selected.
		/// In Editor context checks if target is Selection.activeObject
		/// In Runtime context comparison can be done against the inspected Objects,
		/// as there might be no separation between the selected and inspected targets.
		/// </summary>
		/// <param name="target">The UnityEngine.Object to test.</param>
		bool IsSelected([NotNull]Object target);

		/// <summary>
		/// Returns true if the drawer is the focused drawer or one of the multi-selected drawers.
		/// </summary>
		/// <param name="target"> The drawer to check. </param>
		bool IsSelected(IDrawer target);

		/// <summary> Query if 'rect' is outside viewport. </summary>
		/// <param name="rect"> The rectangle to test. </param>
		/// <returns> True if outside viewport, false if inside it. </returns>
		bool IsOutsideViewport(Rect rect);

		/// <summary> Query if point along the vertical axis is above viewport. </summary>
		/// <param name="verticalPoint"> The vertical point. </param>
		/// <returns> True if above viewport, false if not. </returns>
		bool IsAboveViewport(float verticalPoint);
		
		/// <summary> Query if point along the vertical axis is below viewport. </summary>
		/// <param name="verticalPoint"> The vertical point. </param>
		/// <returns> True if below viewport, false if not. </returns>
		bool IsBelowViewport(float verticalPoint);

		/// <summary>
		/// Selects the UnityEngine.Object. In Editor context this usually sets Selection.activeObject,
		/// and the actual inspected targets of the inspector might not change to reflect the target
		/// if the view is locked.
		/// In Runtime context this might have the same effect as calling RebuildDrawers,
		/// as there might be no separation between the selected and inspected targets.
		/// </summary>
		/// <param name="target">The UnityEngine.Object to select.</param>
		void Select([CanBeNull]Object target);
		
		/// <summary>
		/// Selects the UnityEngine.Objects. In Editor context this usually sets Selection.objects,
		/// and the actual inspected targets of the inspector might not change to reflect the targets
		/// if the view is locked.
		/// In Runtime context this might have the same effect as calling RebuildDrawers,
		/// as there might be no separation between the selected and inspected targets.
		/// </summary>
		/// <param name="targets">The UnityEngine.Objects to select.</param>
		void Select([NotNull]Object[] targets);

		/// <summary> Sets the drawer as the focused drawer. </summary>
		/// <param name="drawer"> The drawer to select, or null to deselect current drawer. </param>
		/// <param name="reason"> The reason why the drawer is being selected. </param>
		void Select([CanBeNull]IDrawer drawer, ReasonSelectionChanged reason);
		
		/// <summary> Adds the drawer to the current multi-selected drawers. </summary>
		/// <param name="drawer"> The drawer to add to selection. </param>
		/// <param name="reason"> The reason why the drawer is being selected. </param>
		void AddToSelection([NotNull]IDrawer drawer, ReasonSelectionChanged reason);

		/// <summary> Removes the drawer from the current multi-selected controls. </summary>
		/// <param name="drawer"> The drawer to remmove from the selection. </param>
		/// <param name="reason"> The reason why the drawer is being deselected. </param>
		void RemoveFromSelection([NotNull]IDrawer drawer, ReasonSelectionChanged reason);

		/// <summary>
		/// Selects UnityEngine.Object and shows it in the inspector.
		/// </summary>
		/// <param name="target"> UnityEngine.Object target to select and show. </param>
		/// <param name="reason"> The reason why the target is being selected. </param>
		void SelectAndShow([NotNull]Object target, ReasonSelectionChanged reason);

		/// <summary>
		/// Selects GameObject and shows it in the inspector.
		/// </summary>
		/// <param name="gameObject"> GameObject target to select and show. </param>
		/// <param name="reason"> The reason why the target is being selected. </param>
		void SelectAndShow([NotNull]GameObject gameObject, ReasonSelectionChanged reason);

		/// <summary>
		/// Selects GameObject containing Component and shows it in the inspector,
		/// then scrolls the inspector view to where the drawer representing the Component is.
		/// </summary>
		/// <param name="component"> Component target to select and show. </param>
		/// <param name="reason"> The reason why the target is being selected. </param>
		void SelectAndShow([NotNull]Component component, ReasonSelectionChanged reason);

		/// <summary>
		/// Selects target containing member and shows it in the inspector,
		/// then scrolls the inspector view to where the drawer representing the member is.
		/// </summary>
		/// <param name="memberInfo"> LinkedMemberInfo to select and show. </param>
		/// <param name="reason"> The reason why the target is being selected. </param>
		void SelectAndShow([NotNull]LinkedMemberInfo memberInfo, ReasonSelectionChanged reason);

		/// <summary> Called whenver the selected part of the inspector is changed. </summary>
		/// <param name="from"> Part that was selected prior to the new one. </param>
		/// <param name="to"> The newly selected part. </param>
		/// <param name="reason"> The reason why the change in selection took place. </param>
		void OnSelectedPartChanged(InspectorPart from, InspectorPart to, ReasonSelectionChanged reason);

		/// <summary>
		/// Try and find drawer for target among the inspected target drawers, and if found, scroll to show it.
		/// </summary>
		void ScrollToShow([NotNull]Object target);
		
		/// <summary>
		/// Scroll this view, if needed, to display the drawer.
		/// </summary>
		void ScrollToShow([NotNull]IDrawer drawer);

		/// <summary>
		/// instantly scrolls the view upwards or downwards the minimum amount
		/// necessary to show the whole area. If the area is larger than the current
		/// view, then position it so it starts from the very top of the view.
		/// </summary>
		/// <param name="area">rectangle inside viewport which should be shown</param>
		void ScrollToShow(Rect area);

		/// <summary>
		/// Rebuilds drawers shown on inspector from targets.
		/// </summary>
		/// <returns> True if shown drawers changed, false if nothing changed.</returns>
		bool RebuildDrawers([NotNull]Object[] selected, bool evenIfTargetsTheSame);

		/// <summary>
		/// Rebuilds drawers shown on inspector from targets.
		/// <para>
		/// Will not draw the contents of the inspector to update dimensions.
		/// Use this method when you need to rebuild the drawers immediately outside
		/// of the OnGUI Layout event to avoid errors from the layout system.
		/// </para>
		/// </summary>
		/// <returns> True if shown drawers changed, false if nothing changed.</returns>
		bool ForceRebuildDrawersWithoutDrawingToUpdateDimentions();

		/// <summary>
		/// Rebuilds drawers for target instance of type.
		/// If type is a static class target should be null.
		/// </summary>
		/// <param name="target">
		/// Target instance to inspect. If type is a static class target should be null. </param>
		/// <param name="type">
		/// The type of the target to inspect. </param>
		/// <returns>
		/// True if drawers changed, false if no changes were made.
		/// </returns>
		bool RebuildDrawers([CanBeNull]object target, Type type);

		/// <summary>
		/// Rebuilds drawers, even if inspected targets remain unchanged.
		/// This can be useful if the targets might not have changed, but their
		/// contents - such as what Components they have - might have.
		/// </summary>
		void ForceRebuildDrawers();

		/// <summary>
		/// Rebuilds drawers for inspector targets, but only if targets have changed.
		/// When view is locked, drawers get rebuilt if any of the targets have been destroyed,
		/// or the Scene containing them has been unloaded.
		/// If view is not locked, then drawers can also rebuilt if the current selection has changed.
		/// </summary>
		void RebuildDrawersIfTargetsChanged();
		
		/// <summary>
		/// Tries to figure out which part of the inspector is currently being mouseovered.
		/// </summary>
		InspectorPart GetMouseoveredPartUpdated(ref bool anyInspectorPartMouseovered);
		
		/// <summary>
		/// True when toolbar mouse inputs should be ignored.
		/// Reasons might include things like the cursor not residing over the toolbar's
		/// bounds, or the fact that a popup menu is open.
		/// </summary>
		bool IgnoreToolbarMouseInputs();

		/// <summary>
		/// True when viewport mouse inputs should be ignored.
		/// Reasons might include things like the cursor not residing over the drawer's
		/// bounds, or the fact that a popup menu is open.
		/// </summary>
		bool IgnoreViewportMouseInputs();
		
		/// <summary>
		/// True when preview area mouse inputs should be ignored.
		/// Reasons might include things like the cursor not residing over the preview area's
		/// bounds, or the fact that a popup menu is open.
		/// </summary>
		bool IgnorePreviewAreaMouseInputs();

		/// <summary> Called when any mouse button is pressed down. </summary>
		/// <param name="e"> The input event. </param>
		void OnMouseDown([NotNull]Event e);

		/// <summary>
		/// Gets called during ContextClick events of OnGUI.
		/// </summary>
		/// <param name="e"> Information about the event. </param>
		void OnContextClick([NotNull]Event e);

		/// <summary>
		/// Can be used to broadcast custom Events to the drawer of the inspector.
		/// </summary>
		/// <param name="e"> The event to broadcast. </param>
		bool SendEvent([NotNull]Event e);

		/// <summary>
		/// Gets called during ValidateCommand events of OnGUI.
		/// </summary>
		/// <param name="e"> Information about the event. </param>
		void OnValidateCommand([NotNull]Event e);

		/// <summary>
		/// Gets called during ExecuteCommand events of OnGUI.
		/// </summary>
		/// <param name="e"> Information about the event. </param>
		void OnExecuteCommand([NotNull]Event e);

		/// <summary>
		/// Sends a message to the user of the Inspector.
		/// In the editor the Console can be used.
		/// </summary>
		/// <param name="message"> The message to show. </param>
		/// <param name="context"> (Optional) The UnityEngine.Object context for the message. </param>
		/// <param name="messageType"> (Optional) Type of the message. </param>
		/// <param name="alsoLogToConsole"> (Optional) If true message will also be logged to console, if false it will only be shown as a popup message. </param>
		void Message([NotNull]string message, Object context = null, MessageType messageType = MessageType.Info, bool alsoLogToConsole = true);
		
		/// <summary>
		/// Sends a message to the user of the Inspector.
		/// In the editor the Console can be used.
		/// </summary>
		/// <param name="message"> The message to show. </param>
		/// <param name="context"> (Optional) The UnityEngine.Object context for the message. </param>
		/// <param name="messageType"> (Optional) Type of the message. </param>
		///  <param name="alsoLogToConsole"> (Optional) If true message will also be logged to console, if false it will only be shown as a popup message. </param>
		void Message([NotNull]GUIContent message, Object context = null, MessageType messageType = MessageType.Info, bool alsoLogToConsole = true);

		/// <summary>
		/// Invokes Action during the beginning of the next Layout event of OnGUI.
		/// </summary>
		/// <param name="action"> The action to invoke. </param>
		void OnNextLayout([NotNull]Action action);

		/// <summary>
		/// Invokes action during the beginning of the next Layout event of OnGUI, unless target has been
		/// placed in the object pool before the time that the action should otherwise be invoked.
		/// </summary>drawer
		/// <param name="action"> Information about the action to invoke and the target drawer instance. </param>
		void OnNextLayout([NotNull]IDrawerDelayableAction action);

		/// <summary>
		/// Invokes Action the next time that the inspected targets have changed.
		/// </summary>
		/// <param name="action"> The action to invoke. </param>
		void OnNextInspectedChanged([NotNull]Action action);

		/// <summary>
		/// Cancels invoking an Action that was supposed to be invoked the next time that the inspected targets changed.
		/// </summary>
		/// <param name="action">
		/// The action to cancel.
		/// </param>
		void CancelOnNextInspectedChanged([NotNull]Action action);

		/// <summary>
		/// Sets the filter text input in the inspector's toolbar to the given value.
		/// </summary>
		/// <param name="setFilterValue"> The filter text input. </param>
		void SetFilter([NotNull]string setFilterValue);

		/// <summary>
		/// Folds all Components on the targets currently inspected on the inspector (if any).
		/// </summary>
		void FoldAllComponents();

		/// <summary>
		/// Folds all Components on the targets currently inspected on the inspector except for the one specified
		/// whose unfoldedness will remain unchanged.
		/// </summary>
		/// <param name="skipFoldingThis"> The Component whose unfoldedness should not be altered. </param>
		void FoldAllComponents(IComponentDrawer skipFoldingThis);

		/// <summary>
		/// Unfolds all Components on the targets currently inspected on the inspector (if any).
		/// </summary>
		void UnfoldAllComponents();

		/// <summary>
		/// Selects targets that were inspected last before the currently inspected targets
		/// in the selection history.
		/// </summary>
		bool StepBackInSelectionHistory();

		/// <summary>
		/// Selects targets that were inspected next after to the currently inspected targets
		/// in the selection history.
		/// </summary>
		bool StepForwardInSelectionHistory();

		/// <summary>
		/// Recursively crawls through all the inspected drawers and updates their cached
		/// values from the fields, properties and UnityEngine.Objects that they represent.
		/// This should usually be called from the Update method every time a certain number of milliseconds has passed.
		/// </summary>
		void UpdateCachedValuesFromFields();

		void OnCursorPositionOrLayoutChanged();

		/// <summary>
		/// Cached values are usually updated from fields and properties continuously when a certain number of
		/// milliseconds has passed. Whenever values are updated, the timer for the next update should be reset
		/// to zero using this method.
		/// </summary>
		void ResetNextUpdateCachedValues();

		void ReloadPreviewInstances();

		/// <summary>
		/// Calls OnFilterChanged for each drawer inside the inspector.
		/// </summary>
		void BroadcastOnFilterChanged();

		/// <summary>
		/// Reset the inspector instance to its initial state for object pooling and later reuse.
		/// </summary>
		void Dispose();

		/// <summary>
		/// Event that is received whenever there are changes to the hierarchy
		/// of an active scene or to the assets in the project.
		/// </summary>
		/// <param name="changed"> Enum describing what has changed. </param>
		/// <param name="forceRebuildDrawers"> If true drawers will be rebuilt no matter what. </param>
		void OnProjectOrHierarchyChanged(OnChangedEventSubject changed, bool forceRebuildDrawers);

		/// <summary>
		/// Called before assemblies are reloaded.
		/// </summary>
		void OnBeforeAssemblyReload();

		/// <summary> Called when currently Selected UnityEngine.Objects get changed. </summary>
		void OnSelectionChange();

		/// <summary>
		/// Rebuild drawers, using currently Selected targets if view is not locked, otherwise rebuilds
		/// from inspected targets, which will have no effect unless some targets have been destroyed.
		/// </summary>
		/// <param name="evenIfTargetsTheSame">
		/// If true, rebuilds drawers even if inspected targets match selected targets, or view is locked,
		/// and none of the inspected targets have been destroyed.
		/// </param>
		/// <returns> True if inspected drawers changed, false if not. </returns>
		bool RebuildDrawers(bool evenIfTargetsTheSame);

		/// <summary> This should be called by the IInspectorDrawer of the inspector during every OnGUI event. </summary>
		/// <param name="inspectorDimensions"> The position and bounds for where the inspecto should be drawn. </param>
		/// <param name="anyInspectorPartMouseovered"> True if any inspector part is currently mouseovered. </param>
		void OnGUI(Rect inspectorDimensions, bool anyInspectorPartMouseovered);

		/// <summary>
		/// Initializes the Inspector for use with the given drawer, preferences and state data.
		/// 
		/// All inspector instances should be created through an IInspectorManager, so that the
		/// IInspectorManager->IInspectorDrawer->IInspector hierarchy can be properly set up. 
		/// </summary>
		/// <param name="drawer"> The drawer. </param>
		/// <param name="setPreferences"> The preferences. </param>
		/// <param name="inspected"> The targets to show on the inspector. </param>
		/// <param name="scrollPos"> The viewport scroll position. </param>
		/// <param name="viewIsLocked"> True if view is locked. </param>
		void Setup(IInspectorDrawer drawer, InspectorPreferences setPreferences, Object[] inspected, Vector2 scrollPos, bool viewIsLocked);

		/// <summary>
		/// True if any drawer or preview drawer currently requires the view to be constantly repainted
		/// </summary>
		/// <returns></returns>
		bool RequiresConstantRepaint();

		/// <summary>
		/// Rebuilds previews shown in preview area.
		/// </summary>
		void RebuildPreviews();
	}
}