using UnityEngine;
using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Object = UnityEngine.Object;

namespace Sisus
{
	/// <summary>
	/// Information about the keyboard input event, control that is going to receive the input, and the current key configurations.
	/// </summary>
	/// <param name="keyboardInputReceiver"> The control that will receive the keyboard input. </param>
	/// <param name="inputEvent"> Information about the keyboard input event including the key code. </param>
	/// <param name="keys"> The current key configuration. </param>
	/// <returns> True input was consumed by a listener and as such keyboardInputReceiver should never receive the keyboard input. </returns>
	public delegate bool KeyboardInputBeingGiven([NotNull]IDrawer keyboardInputReceiver, [NotNull]Event inputEvent, [NotNull]KeyConfigs keys);

	/// <summary>
	/// Interface that all drawers used by controls inside an inspector view implement.
	/// </summary>
	public interface IDrawer : IEquatable<IDrawer>
	{
		/// <summary> ID of the control, unique within the inspector drawer that contains it. </summary>
		int ControlID { get; }

		/// <summary> Gets the inspector that holds the drawer. </summary>
		/// <value> The inspector. </value>
		IInspector Inspector { get; }

		/// <summary>
		/// This is incremented by one every time this class instance is pooled.
		/// Can be used to detect if this instance has been pooled after an async
		/// task has been started, before applying it's effects.
		/// </summary>
		int InstanceId { get; }

		/// <summary>
		/// E.g. transform.position.x. Can be used in filtering of fields by a search string.
		/// All character are always lower case.
		/// This value should be null until the moment that it's first needed, and then
		/// BuildFullClassName should be used to generate it.
		/// </summary>
		string FullClassName { get; }

		/// <summary>
		/// Gets a value indicating whether the control is currently selected.
		/// If multiple controls are selected, this returns true for all of them.
		/// </summary>
		/// <value>
		/// True if selected, false if not.
		/// </value>
		bool Selected { get; }

		/// <summary>
		/// Gets a value indicating whether this object is being animated in the Animation window.
		/// </summary>
		/// <value>
		/// True if this object is being animated, false if not.
		/// </value>
		bool IsAnimated { get; }

		/// <summary>
		/// The total height the drawer take up in the inspector view.
		/// </summary>
		/// <value>
		/// The height.
		/// </value>
		float Height { get; }

		/// <summary> Gets the width at which the control was last drawn. </summary>
		/// <value> The width of the control. </value>
		float Width { get; }

		/// <summary>
		/// Should this drawer currently be visible in the inspector view?
		/// 
		/// This can be false for example when the drawer does not pass the current filter check.
		/// 
		/// This is true even if a drawer is currently off-screen.
		/// It also does not consider parent unfoldedness. For that use IsVisible
		/// </summary>
		/// <value>
		/// True if drawer is shown in inspector, false if not.
		/// </value>
		bool ShouldShowInInspector { get; }

		/// <summary>
		/// Get the name of the target drawn by the drawer.
		/// </summary>
		/// <value>
		/// The target name.
		/// </value>
		string Name { get; }

		/// <summary>
		/// Gets the local draw area offset of the drawer.
		/// This is the difference between (0,0) point in the local space of the drawer
		/// and in screen space. 
		/// </summary>
		/// <value> Difference between drawer local space and screenspace. </value>
		Vector2 LocalDrawAreaOffset
		{
			get;
		}

		/// <summary>
		/// Gets a value indicating whether the drawer is currently deployed and active inside an inspector view.
		/// 
		/// This is true for drawers that reside in the object pool and are not currently being used.
		/// 
		/// It can also be true for drawers whose setup process is still in progrss and because of this are not yet ready to be interacted with by external systems.
		/// </summary>
		/// <value>
		/// True if inactive, false if active.
		/// </value>
		bool Inactive { get; }

		/// <summary>
		/// Gets value indicating if this drawer should be drawn as read-only control.
		/// <para>
		/// The interactive portions of read-only drawer are greyed out in the inspector and don't react to user input.
		/// Values can still be viewed and copied, just not altered in any way via the inspector.
		/// </para>
		/// <para>
		/// Note that this is not always necessarily equatable with whether or not the class member backing the drawer is read-only.
		/// </para>
		/// </summary>
		/// <value>
		/// True if member is read-only, false if not.
		/// </value>
		bool ReadOnly { get; }

		/// <summary>
		/// Gets a value indicating whether or not the drawer has unapplied changes.
		/// 
		/// This is usually used to determine which elements of a prefab instance have
		/// unapplied changes.
		/// 
		/// The labels of drawers will be drawn in bold when their HasUnappliedChanges returns true.
		/// </summary>
		/// <value>
		/// True if this object has unapplied changes, false if not.
		/// </value>
		bool HasUnappliedChanges { get; }

		/// <summary>
		/// Gets a value indicating whether this drawer's order insaide its parent drawer can be changed via drag and drop.
		/// </summary>
		/// <value>
		/// True if this is reorderable, false if not.
		/// </value>
		bool IsReorderable { get; }

		/// <summary>
		/// Gets the LinkedMemberInfo associated with the control (if any).
		/// 
		/// This usually returns null for all drawers except those that implement IFieldDrawer.
		/// </summary>
		/// <value>
		/// LinkedMemberInfo associated with the control, null if control has no LinkedMemberInfo.
		/// </value>
		[CanBeNull]
		LinkedMemberInfo MemberInfo { get; }

		/// <summary>
		/// When a value is assigned to this delegate, it is used in place of the default
		/// validation method to determine if the drawer has an "invalid" value.
		/// 
		/// Drawers with an invalid value are usually tinted red in the inspector.
		///  </summary>
		/// <value>
		/// The override for value validation.
		/// </value>
		Func<object[], bool> OverrideValidateValue { set; }

		/// <summary>
		/// Gets the parent drawer of this drawer.
		/// Null if has no parent drawer.
		/// </summary>
		/// <value>
		/// Parent drawer.
		/// </value>
		[CanBeNull]
		IParentDrawer Parent { get; }

		/// <summary>
		/// Gets the first drawer that implements IUnityObjectDrawer up the parent chain.
		/// </summary>
		/// <value>
		/// Self, parent or grandparent that implements IUnityObjectDrawer.
		/// </value>
		[CanBeNull]
		IUnityObjectDrawer UnityObjectDrawer { get; }

		/// <summary>
		/// Gets the part of the drawer over which the cursor currently resides.
		/// </summary>
		/// <value> Mouseovered part of the drawer, or None if not mouseovered. </value>
		Part MouseoveredPart
		{
			get;
		}

		/// <summary>
		/// Gets the part of the drawer that currently has keyboard focus.
		/// </summary>
		/// <value> Selected part of the drawer, or None if not selected. </value>
		Part SelectedPart
		{
			get;
		}

		/// <summary>
		/// Gets or sets the listeneres for the OnKeyboardInputBeingGiven callback.
		/// All the listeners will be notified right before OnKeyboardInput is called for this control.
		/// 
		/// Allows external actors to capture and override or extend the functionality of the OnKeyboardInput
		/// method of other IDrawer (usually their those of their members).
		/// 
		/// If you add a delegate to this property and it returns true, then OnKeyboardInput will not be called
		/// at all in the drawer.
		/// </summary>
		/// <value>
		/// Information about the keyboard input, it's receiver and current key configurations.
		/// KeyboardInputBeingGiven should return true if input was consumed by a listener, false if not.
		/// </value>
		KeyboardInputBeingGiven OnKeyboardInputBeingGiven { get; set; }

		/// <summary>
		/// Gets the first of the UnityEngine.Object targets of the drawer.
		/// 
		/// For IComponentDrawer this returns a Component.
		/// For IGameObjectDrawer this returns a GameObject.
		/// For IAssetDrawer this returns an Object.
		/// For static classes this returns null.
		/// For IFieldDrawer this requests UnityObject from parent.
		/// </summary>
		/// <value>
		/// The first of the UnityEngine.Object targets.
		/// </value>
		[CanBeNull]
		Object UnityObject { get; }

		/// <summary>
		/// Gets the <see cref="UnityEngine.Object"/> targets of the drawer.
		/// 
		/// For IComponentDrawer this returns an array containing components.
		/// For IGameObjectDrawer this returns an array containing GameObjects.
		/// For IAssetDrawer this returns an array containing Objects.
		/// For static classes this returns an empty (zero-size) array.
		/// For IFieldDrawer this requests UnityObjects from parent.
		/// </summary>
		/// <value>
		/// The UnityEngine.Object targets. Empty if targets are not of type <see cref="UnityEngine.Object"/>.
		/// </value>
		[NotNull]
		Object[] UnityObjects { get; }

		/// <summary>
		/// Gets the <see cref="Transform"/> associated with the first of the <see cref="UnityEngine.Object"/> targets of the drawer.
		/// 
		/// For IComponentDrawer this returns the transform of the GameObject that contains the first target component.
		/// For IGameObjectDrawer this returns the transform on the first target GameObject.
		/// For IAssetDrawer this returns null.
		/// For static classes this returns null.
		/// For IFieldDrawer this retrieves Transform from parent drawers recursively.
		/// </summary>
		/// <value> The Transform component on first target GameObject. </value>
		[CanBeNull]
		Transform Transform { get; }

		/// <summary>
		/// Gets the Transform associated with the first of the UnityEngine.Object targets of the drawer.
		/// 
		/// For IComponentDrawer this returns the transforms of the GameObjects that contain the target components.
		/// For IGameObjectDrawer this returns the transforms on the target GameObjects.
		/// For IAssetDrawer this returns an empty array.
		/// For static classes this returns an empty array.
		/// For IFieldDrawer this retrieves Transforms from parent drawers recursively.
		/// </summary>
		/// <value> The <see cref="Transform"/> components on <see cref="GameObject"/> targets. Empty if targets are not of type <see cref="GameObject"/>. </value>
		[NotNull]
		Transform[] Transforms { get; }

		/// <summary>
		/// Gets the type of the drawer's target.
		/// </summary>
		/// <value>
		/// The drawer target type.
		/// </value>
		[NotNull]
		Type Type { get; }

		/// <summary>
		/// Gets or sets the listeners for the OnValueChanged callback.
		/// </summary>
		/// <value>
		/// The OnValueChanged callback
		/// </value>
		[CanBeNull]
		OnValueChanged OnValueChanged { get; set; }

		/// <summary>
		/// Gets or sets the label for this drawer.
		/// 
		/// The label is usually used when drawing the prefix label in the inspector.
		/// </summary>
		/// <value>
		/// The label.
		/// </value>
		[NotNull]
		GUIContent Label { get; set; }

		/// <summary>
		/// Gets a value indicating whether the drawer is selectable in the inspector.
		/// This usually also directly determines whether or not a control can be clicked, however it is possible to
		/// override Clickable to allow the clicking of an unselectable drawer.
		/// </summary>
		/// <value>
		/// True if selectable, false if not.
		/// </value>
		bool Selectable { get; }

		/// <summary>
		/// Gets a value indicating whether the drawer reacts to clicking.
		/// 
		/// Usually this is equal to the value of Selectable. However in rare instances one might want an auxilary element
		/// inside a parent drawer to be clickable, but not to be selectable. In this case the parent usually should be
		/// selected instead when the member control is clicked.
		/// </summary>
		/// <value>
		/// True if clickable, false if not.
		/// </value>
		bool Clickable { get; }

		/// <summary>
		/// Gets the region of the drawer that can be clicked in order to select the drawer inside the inspector.
		/// </summary>
		/// <value>
		/// The click-to-select area.
		/// </value>
		Rect ClickToSelectArea { get; }

		/// <summary>
		/// Gets the bounds, i.e. the last draw position and dimensions, of the drawer.
		/// </summary>
		/// <value>
		/// The bounds Rect.
		/// </value>
		Rect Bounds { get; }

		/// <summary>
		/// Gets a value indicating whether Debug Mode+ is currently turned on for this drawer.
		/// </summary>
		/// <value>
		/// True if Debug Mode is on, false if not.
		/// </value>
		bool DebugMode { get; }

		/// <summary>
		/// Gets a value indicating whether the current data of the control is all valid.
		/// This information can be used for things such as tinting controls red and/or displaying
		/// warning texts when the control contains invalid data.
		/// Examples of invalid data can contain values that are unserializable or don't follow the
		/// rules established by Attributes.
		/// </summary>
		/// <value>
		/// True if all data is valid, false if not.
		/// </value>
		bool DataIsValid { get; }

		/// <summary>
		/// Gets a value indicating whether the prefix width can be adjusted by dragging the prefix
		/// spltter line over this control.
		/// Controls that aren't split into prefix and control portions, but take up the full width
		/// of the inspector in a continous manner, should return false here.
		/// </summary>
		/// <value>
		/// True if prefix resizing enabled over control, false if not.
		/// </value>
		bool PrefixResizingEnabledOverControl { get; }

		/// <summary>
		/// Gets a value indicating whether the control requires constant repainting.
		/// This should generally be false and only be true for controls that are
		/// currently playing animations of some sort.
		/// </summary>
		/// <value>
		/// True if requires constant repainting, false if not.
		/// </value>
		bool RequiresConstantRepaint { get; }

		/// <summary>
		/// Gets the bounds (last position and size) of the are that can be right-clicked
		/// to open the context menu for the control.
		/// </summary>
		/// <value>
		/// The area that can be right-clicked to open the context menu.
		/// </value>
		Rect RightClickArea { get; }

		/// <summary>
		/// Gets a value indicating whether or not the control's cached value should constantly
		/// be updated with the current value of the member. This should be true if anything
		/// external to the control itself can change the value of the member it represents
		/// (like external code changing the value of the field that is being inspected)
		/// </summary>
		/// <value>
		/// True if cached values need to be constantly updated, false if not.
		/// </value>
		bool CachedValuesNeedUpdating { get; }

		/// <summary>
		/// Gets a value indicating whether this drawer or any drawer up its parent chain are for a prefab asset.
		/// Prefab instances are not counted as being prefab assets.
		/// </summary>
		/// <value> True if the drawer or its parents are for a prefab, false if not. </value>
		bool IsPrefab { get; }

		/// <summary>
		/// Gets a value indicating whether this drawer or any drawer up its parent chain are for a prefab instance.
		/// </summary>
		/// <value> True if the drawer or its parents are for a prefab instance, false if not. </value>
		bool IsPrefabInstance { get; }

		/// <summary>
		/// Called right after SetupInterface has finished.
		/// Finishes setting up the Drawer so that it is ready to be used.
		/// </summary>
		void LateSetup();

		/// <summary>
		/// This should always be called after the control has been assigned
		/// as the member of a IParentDrawer instance.
		/// Some parts of setup process of drawers might only be doable
		/// once their parent has been assigned, and as such they should take
		/// place not in their Setup or LateSetup methods but in their OnParentAssigned
		/// method.
		/// </summary>
		/// <param name="newParent"> The new parent of the member. This cannot be null. </param>
		void OnParentAssigned([NotNull]IParentDrawer newParent);

		/// <summary>
		/// Draws the control at the given position. Control will be drawn at the position specified by the
		/// x and y components of position, and with the width specified by teh widht component of position.
		/// The height component of position however will be ignored in most cases, and the control will be
		/// drawn with whatever height it requires (and pollable using the Height property of the drawer).
		/// </summary>
		/// <param name="position">
		/// The position. </param>
		/// <returns>
		/// True if anything changed inside the GUI during this frame, false if nothing changed.
		/// </returns>
		bool Draw(Rect position);

		/// <summary>
		/// Called during every repaint event before the Draw method for all visible drawers when the search box has a filter.
		/// </summary>
		/// <param name="filter"> Filter. </param>
		/// <param name="color"> Color to use for the higlighting effect. </param>
		void DrawFilterHighlight(SearchFilter filter, Color color);

		/// <summary>
		/// Called every frame when the control is selected, after the Draw method has been called for all drawers.
		/// This can be used to overlay visuals indicating the selected state of the control.
		/// NOTE: things should usually be drawn at a Rect that was cached when Draw method was last called during a layout event.
		/// </summary>
		void DrawSelectionRect();

		/// <summary>
		/// Like OnMouseover this is called every frame during which the control is being mouseovered, with
		/// the difference that this is called before the Draw method, while OnMouseover is called after it.
		void OnMouseoverBeforeDraw();

		/// <summary>
		/// Called every frame during which the control is being mouseovered. However, this will NOT be called when:
		/// 1. a control is being reordered (OnBeingReordered is called instead),
		/// 2. a prefix is being dragged (OnPrefixDragged is called instead)
		/// 3. or when it's a drag event (OnMouseoverDuringDrag is called instead).
		/// This will be called after the main Draw method has been called for all drawers, and thus
		/// it can overlay visuals on top of things drawn there.
		/// A target is considered to be mouseovered when the cursor resides over its ClickToSelectArea
		/// but does not resides over the ClickToSelectArea of any of it's members.
		/// </summary>
		void OnMouseover();

		/// <summary>
		/// Called every frame when something is being dragged and the control is mouseovered. However, this will NOT be called when:
		/// 1. a control is being reordered (OnSubjectOverDropTarget is called instead) and Drag n Drop Object reference count is zero
		/// 2. or a prefix is being dragged (OnPrefixDragged is called instead).
		/// This will be called after the main Draw method has been called for all drawers, and thus
		/// it can overlay visuals on top of things drawn there.
		/// A target is considered to be mouseovered when the cursor resides over its ClickToSelectArea
		/// but does not resides over the ClickToSelectArea of any of it's members.
		/// </summary>
		/// <param name="mouseDownInfo"> Information related to mouse down like which control was under the cursor at that time. </param>
		/// <param name="dragAndDropObjectReferences"> Object references related to the drag. </param>
		void OnMouseoverDuringDrag(MouseDownInfo mouseDownInfo, Object[] dragAndDropObjectReferences);

		/// <summary>
		/// Called every frame during which the target's right click area is mouseover, except when a
		/// control is being reordered (then OnBeingReordered is called)
		/// or a prefix is being dragged (then OnPrefixDragged is called instead).
		/// This will be called after the main Draw method has been called for all drawers, and thus
		/// it can overlay visuals on top of things drawn there.
		/// A target's right click area is considered to be mouseovered when the cursor resides over its
		/// RightClickArea but does not resides over the RightClickArea of any of it's members.
		/// </summary>
		void OnRightClickAreaMouseover();

		/// <summary>
		/// Updates cached value for this drawer and all visible member drawers, where applicable.
		/// </summary>
		void UpdateCachedValuesFromFieldsRecursively();

		/// <summary>
		/// Set value of all target members (as well as the cached value)
		/// </summary>
		/// <param name="setValue"> The value to set all targets to. </param>
		/// <returns> True if cached value changed, false if nothing happened. </returns>
		bool SetValue(object setValue);

		/// <summary>
		/// Sets cached value and has paramaters for controlling whether or not
		/// value should also get applied to all target fields and whether or
		/// not cached values of member drawers should be updated.
		/// </summary>
		/// <param name="setValue"> The value to set the cached value. </param>
		/// <param name="applyToField"> True to apply value to target fields. </param>
		/// <param name="updateMembers"> True to update cached values of member drawers. </param>
		/// <returns> True if cached value changed, false if nothing happened. </returns>
		bool SetValue(object setValue, bool applyToField, bool updateMembers);

		/// <summary> Gets the cached value of the drawer. </summary>
		/// <returns> The cached value. </returns>
		object GetValue();
		
		/// <summary>
		/// Get value of target UnityEngine.Object at index.
		/// </summary>
		/// <param name="index"> Zero-based index of the UnityEngine.Object target. </param>
		/// <returns> The value. </returns>
		object GetValue(int index);

		/// <summary>
		/// Get values from all UnityEngine.Object targets.
		/// </summary>
		/// <returns>
		/// An object array containing member values in all targets
		/// </returns>
		object[] GetValues();

		/// <summary>
		/// Called whenever Inspector filter field text is changed.
		/// This should handle updating the value of ShowInInspector,
		/// based on whether or not the control passes the filter check.
		/// </summary>
		/// <param name="filter">
		/// The filter that was just set. </param>
		void OnFilterChanged(SearchFilter filter);

		/// <summary>
		/// Gets optimal prefix label width for the drawer, i.e. the smallest possible width where prefix texts fit without clipping.
		/// For IParentDrawer should also consider all member drawers.
		/// </summary>
		/// <param name="indentLevel"> The indent level used when the member is drawn. </param>
		/// <returns> The optimal prefix label width. </returns>
		float GetOptimalPrefixLabelWidth(int indentLevel);

		/// <summary>
		/// Gets optimal prefix label width for the drawer, i.e. the smallest possible width where prefix texts fit without clipping.
		/// For IParentDrawer should also consider all member drawers.
		/// </summary>
		/// <param name="indentLevel"> The indent level used when the member is drawn. </param>
		/// <param name="sanitize"> If value is too low, convert to more sensible value. </param>
		/// <returns> The optimal prefix label width. </returns>
		float GetOptimalPrefixLabelWidth(int indentLevel, bool sanitize);

		/// <summary>
		/// Open context menu for drawer at cursor position or at given position.
		/// </summary>
		/// <param name="inputEvent">
		/// Input event that should be used if a context menu is opened.
		/// </param>
		/// <param name="openAtPosition">
		/// If not null, context menu will be opened at this position, otherwise
		/// context menu will be opened at cursor position.
		/// </param>
		/// <param name="isLocalPosition"> Is this called from inside a Draw method, with openAtPosition using local coordinate system? </param>
		/// <param name="subjectPart"> Contain information about the part of the subject for which the context menu is opened. </param>
		/// <returns> True if a context menu was opened, false if not. </returns>
		bool OpenContextMenu(Event inputEvent, Rect? openAtPosition, bool isLocalPosition, Part subjectPart);

		
		/// <summary>
		/// Applies action on this drawer and in all member drawers (if any)
		/// </summary>
		/// <param name="action">
		/// The action. </param>
		void ApplyInChildren(Action<IDrawer> action);

		/// <summary>
		/// Applies action on this drawer and in all visible member drawers (if any).
		/// </summary>
		/// <param name="action">
		/// The action. </param>
		void ApplyInVisibleChildren(Action<IDrawer> action);

		/// <summary>
		/// Tests this drawer and then all of its member drawers (if any)
		/// with given test until first drawer returns true, then returns said drawer.
		/// </summary>
		/// <param name="test"> The test to do for each child. </param>
		/// <returns> First tested IDrawer to pass the test. </returns>
		[CanBeNull]
		IDrawer TestChildrenUntilTrue(Func<IDrawer, bool> test);

		/// <summary>
		/// Tests this drawer and then on all of its visible member drawers (if any)
		/// with given test until first drawer returns true, and returns said drawer.
		/// </summary>
		/// <param name="test"> The test to do for each child. </param>
		/// <returns> First tested IDrawer to pass the test. </returns>
		[CanBeNull]
		IDrawer TestVisibleChildrenUntilTrue(Func<IDrawer, bool> test);

		/// <summary>
		/// Cuts value of drawer target to clipboard and sends a message to the user about it.
		/// </summary>
		void CutToClipboard();

		/// <summary>
		/// Copies value of drawer target to clipboard and sends a message to the user about it.
		/// </summary>
		void CopyToClipboard();

		/// <summary>
		/// Determine if we can paste value from clipboard.
		/// </summary>
		/// <returns>
		/// True if we can paste value from clipboard, false if not.
		/// </returns>
		bool CanPasteFromClipboard();

		/// <summary>
		/// Pastes control value from clipboard and sends a message to the user about it.
		/// </summary>
		void PasteFromClipboard();

		/// <summary>
		/// Select previous component.
		/// </summary>
		void SelectPreviousComponent();

		/// <summary>
		/// Select next component.
		/// </summary>
		void SelectNextComponent();

		/// <summary>
		/// Select previous of type.
		/// E.g. if this is a Component existing in the scene hierarchy,
		/// then finds the previous Component of the same type in the hierarchy
		/// and selects it.
		/// </summary>
		void SelectPreviousOfType();

		/// <summary>
		/// Select next of type.
		/// E.g. if this is a Component existing in the scene hierarchy,
		/// then finds the next Component of the same type in the hierarchy
		/// and selects it.
		/// </summary>
		void SelectNextOfType();

		/// <summary>
		/// Selects the control
		/// </summary>
		/// <param name="reason">
		/// The reason why the control was selected. </param>
		void Select(ReasonSelectionChanged reason);

		/// <summary>
		/// Called after control was just selected.
		/// </summary>
		/// <param name="reason"> The reason why the drawer was selected. </param>
		/// <param name="previous"> The drawer that was selected before. </param>
		/// <param name="isMultiSelection"> Is this drawer being selected as only one of multiple selected drawers? </param>
		void OnSelected(ReasonSelectionChanged reason, [CanBeNull] IDrawer previous, bool isMultiSelection);

		/// <summary>
		/// Called after control was just deselected.
		/// </summary>
		/// <param name="reason"> The reason why the drawer was deselected. </param>
		/// <param name="losingFocusTo"> The drawer which is gaining focus next. </param>
		void OnDeselected(ReasonSelectionChanged reason, [CanBeNull]IDrawer losingFocusTo);

		/// <summary>
		/// Gets the next selectable drawer above this one to which focus should move
		/// if keyboard input is given for selecting a control in that direction.
		/// </summary>
		/// <param name="column">
		/// The column on which the requester resides. E.g. in a Vector3 field the prefix would be
		/// at index 0, x at index 1, y at index 2 and z at index 3.
		/// </param>
		/// <param name="requester">
		/// The drawer that is requesting the information. Knowing when the requester is a parent or a child
		/// of the drawer can sometimes be important in deciding what result to return.
		/// </param>
		/// <returns> The next drawer above this one. </returns>
		IDrawer GetNextSelectableDrawerUp(int column, [CanBeNull]IDrawer requester);

		/// <summary>
		/// Gets the next selectable drawer below this one to which focus should move
		/// if keyboard input is given for selecting a control in that direction.
		/// </summary>
		/// <param name="column">
		/// The column on which the requester resides. E.g. in a Vector3 field the prefix would be
		/// at index 0, x at index 1, y at index 2 and z at index 3.
		/// </param>
		/// <param name="requester">
		/// The drawer that is requesting the information. Knowing when the requester is a parent or a child
		/// of the drawer can sometimes be important in deciding what result to return.
		/// </param>
		/// <returns> The next drawer below this one. </returns>
		IDrawer GetNextSelectableDrawerDown(int column, [CanBeNull]IDrawer requester);

		/// <summary>
		/// Gets the next selectable drawer to the left of this one to which focus should move
		/// if keyboard input is given for selecting a control in that direction.
		/// </summary>
		/// <param name="moveToNextControlAfterReachingEnd">
		/// True if should move to next control in above row after reaching first control on this row. </param>
		/// <param name="requester">
		/// The drawer that is requesting the information. Knowing when the requester is a parent or a child
		/// of the drawer can sometimes be important in deciding what result to return.
		/// </param>
		/// <returns> The next drawer to the left of this one. </returns>
		IDrawer GetNextSelectableDrawerLeft(bool moveToNextControlAfterReachingEnd, [CanBeNull]IDrawer requester);

		/// <summary>
		/// Gets the next selectable drawer to the right of this one to which focus should move
		/// if keyboard input is given for selecting a control in that direction.
		/// </summary>
		/// <param name="moveToNextControlAfterReachingEnd">
		/// True if should move to next control in below row after reaching last control on this row. </param>
		/// <param name="requester">
		/// The drawer that is requesting the information. Knowing when the requester is a parent or a child
		/// of the drawer can sometimes be important in deciding what result to return.
		/// </param>
		/// <returns> The next drawer to the right of this one. </returns>
		IDrawer GetNextSelectableDrawerRight(bool moveToNextControlAfterReachingEnd, [CanBeNull]IDrawer requester);

		/// <summary> Resets the object to its default state. </summary>
		/// <param name="messageUser"> True if should send message about reset taking place to user. </param>
		void Reset(bool messageUser);

		/// <summary>
		/// Determines whether or not the cursor currently resides over this control,
		/// and not any of its members (if it has any).
		/// </summary>
		/// <param name="mousePosition"> Position of cursor in local coordinate space. </param>
		/// <returns>
		/// True if this is mouseovered, false if not.
		/// </returns>
		bool DetectMouseover(Vector2 mousePosition);

		/// <summary>
		/// Determines whether or not the cursor currently resides over this control
		/// or any of its members (if it has any).
		/// </summary>
		/// <param name="mousePosition"> Position of cursor in local coordinate space. </param>
		/// <returns>
		/// True if this or any of its members (including nested members) are mouseovered, false if not.
		/// </returns>
		bool DetectMouseoverForSelfAndChildren(Vector2 mousePosition);

		/// <summary>
		/// Determines whether or not the cursor currently resides over the right click area of this control,
		/// i.e. the region where if the user clicks the right mouse button, the context menu should be opened.
		/// </summary>
		/// <param name="mousePosition"> Position of cursor in local coordinate space. </param>
		/// <returns> True if right-click area is mouseovered, false if not.
		/// </returns>
		bool DetectRightClickAreaMouseover(Vector2 mousePosition);

		/// <summary>
		/// Called during the frame that the cursor enters the Bounds (last draw position) of the control.
		/// </summary>
		/// <param name="inputEvent"> The event for the mouse input. </param>
		/// <param name="isDrag"> True if this is a drag event, false if not. </param>
		void OnMouseoverEnter(Event inputEvent, bool isDrag);

		/// <summary>
		/// Called during the frame that the cursor exits the Bounds (last draw position) of the control.
		/// </summary>
		/// <param name="inputEvent"> The event for the mouse input. </param>
		void OnMouseoverExit(Event inputEvent);

		/// <summary>
		/// Called when any part of the drawer is clicked.
		/// 
		/// When determining whether or not a drawer is clicked, the members of the drawer take precedence over the parent.
		/// 
		/// The OnClick event is sent during the MouseDown event.
		/// </summary>
		/// <param name="inputEvent"> The MouseDown event for the mouse input. </param>
		/// <returns> True if click event should be consumed </returns>
		bool OnClick(Event inputEvent);

		/// <summary>
		/// Called when any part of the control is double-clicked.
		/// </summary>
		/// <param name="inputEvent"> The MouseDown event for the mouse input. </param>
		/// <returns> True if click event should be consumed </returns>
		bool OnDoubleClick(Event inputEvent);

		/// <summary>
		/// Called when the right clickable area of the control is right-clicked.
		/// </summary>
		/// <param name="inputEvent"> The MouseDown or ContextClick event for the mouse input. </param>
		/// <returns> True if click event should be consumed </returns>
		bool OnRightClick(Event inputEvent);

		/// <summary>
		/// Called when the user presses down the middle mouse button with the cursor
		/// being over any part of this control (and not over any of its members).
		/// </summary>
		/// <param name="inputEvent"> The MouseDown event for the mouse input. </param>
		/// <returns> True if click event should be consumed </returns>
		void OnMiddleClick(Event inputEvent);

		/// <summary>
		/// Called every frame after left mouse button was pressed down over this control,
		/// until after the left mouse button is released.
		/// </summary>
		/// <param name="inputEvent"> The event for the mouse input. </param>
		void OnDrag(Event inputEvent);

		/// <summary>
		/// Called during the frame that the left mouse button is being released, if it was pressed down
		/// while being located over this control.
		/// </summary>
		/// <param name="inputEvent"> The event for the mouse input. </param>
		/// <param name="isClick"> True if the control was clicked, false if cursor moved between mouse button being pressed down and lifted. </param>
		void OnMouseUpAfterDownOverControl(Event inputEvent, bool isClick);

		/// <summary>
		/// called when this is the selected control and the user gives keyboard input.
		/// </summary>
		/// <param name="inputEvent"> The input event. </param>
		/// <param name="keys"> key configuration data. </param>
		/// <returns> True if input event should be consumed, false if not. </returns>
		bool OnKeyboardInputGiven(Event inputEvent, KeyConfigs keys);

		/// <summary>
		/// Duplicates the drawer targets.
		/// 
		/// With Components drawers, this adds a duplicate of the target Components on the GameObjects that hold the Components.
		/// 
		/// With drawers of collection member, this duplicates the collection member value and inserts this copy into the collection
		/// below the index of the duplicated member.
		/// </summary>
		/// <exception cref="NotSupportedException"> Thrown when duplicate command is not supported by the drawer.</exception>
		void Duplicate();

		/// <summary>
		/// Called when the control was previously invisible and has now become visible.
		/// Mostly this happen when the parent or a grand-parent of the control was unfolded.
		/// </summary>
		void OnSelfOrParentBecameVisible();

		/// <summary>
		/// Called when the control was previously visible and has now become invisible.
		/// Mostly this happen when the parent or a grand-parent of the control was folded.
		/// </summary>
		void OnBecameInvisible();

		/// <summary>
		/// Convert this object into a string representation.
		/// </summary>
		/// <returns>
		/// A string that represents this object.
		/// </returns>
		string ToString();

		/// <summary>
		/// Recursively collect PreviewWrappers that should be shown in the preview area
		/// from self and any UnityObject members.
		/// </summary>
		/// <param name="previews">
		/// [in,out] The previews. </param>
		void AddPreviewWrappers(ref List<IPreviewableWrapper> previews);

		/// <summary>
		/// Gets the zero-based index of the selected control amongst all the selectable controls on this row.
		/// </summary>
		/// <returns>
		/// The index of the selected control on this row.
		/// </returns>
		int GetSelectedRowIndex();

		/// <summary>
		/// Gets number of selectable controls that currently exist on the same row with this control.
		/// </summary>
		/// <returns>
		/// The number of selectable controls on this row.
		/// </returns>
		int GetRowSelectableCount();

		/// <summary>
		/// Called during the ExecuteCommand event
		/// </summary>
		/// <param name="commandEvent">
		/// The command event. </param>
		void OnExecuteCommand(Event commandEvent);

		/// <summary>
		/// Randomizes the values of the drawer and notifies the user about it.
		/// 
		/// This should only be called for drawers that are not ReadOnly.
		/// </summary>
		void Randomize();

		/// <summary>
		/// Randomizes the values of the drawer and optionally notifies the user about it.
		/// 
		/// This should only be called for drawers that are not ReadOnly.
		/// </summary>
		/// <param name="alsoShowMessage"> If true then a message about the randomization taking place will be shown to the user. </param>
		void Randomize(bool alsoShowMessage);

		/// <summary>
		/// Resets the drawers to its initial state and places it into the object pool for later reuse.
		/// </summary>
		void Dispose();

		/// <summary>
		/// Returns the default value for the drawer.
		/// 
		/// If CanBeNull is true then this will return null, otherwise it try to create an instance.
		/// 
		/// When Reset is called value will be set to this value.
		/// </summary>
		/// <param name="preferNotNull"> If true, then will try to return non-null value even if CanBeNull is true for the drawer. </param>
		/// <returns> Default value. </returns>
		object DefaultValue(bool preferNotNull = false);

		/// <summary>
		/// Generates an array containing member index of this instruction in its parent's members,
		/// member index of its parent in it's grandparent's members, etc. up the parent-chain, until
		/// index in parent stopAfter has been added to the array. If stopAfter is null, then will keep
		/// going until there are no more grandparents.
		/// </summary>
		/// <param name="stopAfter">
		/// The parent whose member's index should be the last item in the
		/// results. If null, then will continue up the parent-chain until there
		/// are no longer any parents. </param>
		/// <returns>
		/// An array of indexes. If stopAfter was not null, but it was never found, returns null.
		/// </returns>
		[CanBeNull]
		int[] GenerateMemberIndexPath([CanBeNull]IParentDrawer stopAfter);

		/// <summary>
		/// Select member at index path.
		/// </summary>
		/// <param name="memberIndexPath">
		/// Full pathname of the member index file. </param>
		/// <param name="reason">
		/// The reason. </param>
		void SelectMemberAtIndexPath(int[] memberIndexPath, ReasonSelectionChanged reason);

		/// <summary> Returns drawer's value in string form for use with filtering fields by value. </summary>
		/// <returns>
		/// A string representing the value of the drawer.
		/// If this is not a leaf drawer (it has members), returns null.
		/// If value is null, returns text "null".
		/// </returns>
		[CanBeNull]
		string ValueToStringForFiltering();

		/// <summary> Invokes the action during the next Layout event of onGUI. </summary>
		/// <param name="action"> Delegate to invoke. </param>
		void OnNextLayout([NotNull]Action action);

		/// <inheritdoc cref="OnNextLayout(Action)" />
		void OnNextLayout([NotNull]Action<IDrawer> action);

		/// <summary>
		/// Gets name for use with messages displayed to the user.
		/// </summary>
		/// <returns> The name used in messages. </returns>
		string GetFieldNameForMessages();

		/// <summary>
		/// When the value of a sibling (drawer that has same parent drawer) is changed, this gets called.
		/// 
		/// This can get called when user changes value through the inspector view, but also when external sources change
		/// the value, in which case the UpdateCachedValues method causes this to get called.
		/// </summary>
		/// <param name="memberIndex"> Zero-based index of the member whose value changed. </param>
		/// <param name="memberValue"> The new value of the member. </param>
		/// <param name="memberLinkedMemberInfo"> the linked member info of the changed member</param>
		void OnSiblingValueChanged(int memberIndex, object memberValue, [CanBeNull]LinkedMemberInfo memberLinkedMemberInfo);

		/// <summary>
		/// Called when the inspector drawer gains focus with this drawer being selected.
		/// </summary>
		void OnInspectorGainedFocusWhileSelected();

		/// <summary>
		/// Called when the inspector drawer loses focus with this drawer being selected.
		/// </summary>
		void OnInspectorLostFocusWhileSelected();

		/// <summary>
		/// Is the drawer currently being animated e.g. via a parent playing an unfolding animation?
		/// </summary>
		/// <returns> True if is being animated, false if not. </returns>
		bool BeingAnimated();
	}
}