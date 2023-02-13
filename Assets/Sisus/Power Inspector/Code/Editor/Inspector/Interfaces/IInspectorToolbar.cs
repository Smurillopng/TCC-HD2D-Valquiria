using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Sisus
{
	public interface IInspectorToolbar
	{
		/// <summary> Gets the height of the toolbar. </summary>
		/// <value> The height in pixels. </value>
		float Height { get; }

		/// <summary> Gets the item on that toolbar that is currently selected. </summary>
		/// <value> The selected item or null if none is selected. </value>
		[CanBeNull]
		IInspectorToolbarItem SelectedItem { get; }

		/// <summary> Gets all toolbar items that exist on the toolbar. </summary>
		/// <value> All toolbar items. This will never be null. </value>
		[NotNull]
		IInspectorToolbarItem[] Items { get; }

		/// <summary> Setups the toolbar for the given inspector. </summary>
		/// <param name="setInspector"> The inspector that holds the toolbar. This cannot be null. </param>
		void Setup([NotNull]IInspector setInspector);

		/// <summary> Called when the toolbar is selected. </summary>
		/// <param name="reason"> The reason for the toolbar being selected. </param>
		void OnSelected(ReasonSelectionChanged reason);

		/// <summary> Called when the toolbar is deselected. </summary>
		/// <param name="reason"> The reason for the toolbar being deselected. </param>
		void OnDeselected(ReasonSelectionChanged reason);

		/// <summary> Gets item that assignable from given type that exists on the toolbar. </summary>
		/// <typeparam name="TToolbarItemType"> Type of the toolbar item. </typeparam>
		/// <returns> The item, or null if didn't exist on the toolbar. </returns>
		[CanBeNull]
		TToolbarItemType GetItem<TToolbarItemType>() where TToolbarItemType : class, IInspectorToolbarItem;

		IInspectorToolbarItem GetItemByType(Type itemType);

		/// <summary>
		/// Sets selected toolbar item to given value.
		/// Handles calling OnDeselected and OnSelected in items.
		/// </summary>
		/// <param name="item"> The item. This can be null. </param>
		/// <param name="reason"> The reason why the selection is changing. </param>
		void SetSelectedItem([CanBeNull]IInspectorToolbarItem item, ReasonSelectionChanged reason);

		/// <summary>
		/// Called when the inspector containing the toolbar is focused and the user executes the find command.
		/// </summary>
		/// <returns> True if toolbar consumes the find command, false if not. </returns>
		bool OnFindCommandGiven();

		/// <summary>
		/// Called when the inspector containing the toolbar is focused and the user executes a command.
		/// </summary>
		/// <param name="e"> The event for the command. This cannot be null. </param>
		void OnValidateCommand([NotNull]Event e);

		/// <summary> Draws the toolbar at the given position. </summary>
		/// <param name="toolbarPosition"> The position and size at which the toolbar should be drawn. </param>
		void Draw(Rect toolbarPosition);

		/// <summary> Called when the toolbar is clicked with the left mouse button by the user. </summary>
		/// <param name="inputEvent"> The click event. This cannot be null. </param>
		/// <returns> True if toolbar consumes the click event, false if not. </returns>
		bool OnClick([NotNull]Event inputEvent);

		/// <summary> Called when the toolbar is clicked with the right mouse button by the user. </summary>
		/// <param name="inputEvent"> The click event. This cannot be null. </param>
		/// <returns> True if toolbar consumes the click event, false if not. </returns>
		bool OnRightClick([NotNull]Event inputEvent);

		/// <summary> Called when the toolbar is clicked with the middle mouse button by the user. </summary>
		/// <param name="inputEvent"> The click event. This cannot be null. </param>
		/// <returns> True if toolbar consumes the click event, false if not. </returns>
		bool OnMiddleClick([NotNull]Event inputEvent);

		/// <summary> Called when keyboard input is given by the user when the toolbar does not have keyboard focus. </summary>
		/// <param name="inputEvent"> The keyboard input event. This cannot be null. </param>
		/// <param name="keys"> The keyboard configuration settings of the user. This cannot be null. </param>
		/// <returns> True if toolbar consumes the click event, false if not. </returns>
		bool OnKeyboardInputGivenWhenNotSelected([NotNull]Event inputEvent, [NotNull]KeyConfigs keys);

		/// <summary> Called when keyboard input is given by the user when the toolbar has keyboard focus. </summary>
		/// <param name="inputEvent"> The keyboard input event. This cannot be null. </param>
		/// <param name="keys"> The keyboard configuration settings of the user. This cannot be null. </param>
		/// <returns> True if toolbar consumes the click event, false if not. </returns>
		bool OnKeyboardInputGiven([NotNull]Event inputEvent, [NotNull]KeyConfigs keys);

		/// <summary>
		/// Reset the toolbar instance to its initial state for object pooling and later reuse.
		/// </summary>
		void Dispose();
	}
}