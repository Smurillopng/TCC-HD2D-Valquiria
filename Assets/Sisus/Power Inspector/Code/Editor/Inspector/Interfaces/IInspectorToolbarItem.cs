using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Sisus
{
	public interface IInspectorToolbarItem
	{
		/// <summary> Gets the item alignment on the toolbar. </summary>
		/// <value> Alignment on toolbar. </value>
		ToolbarItemAlignment Alignment
		{
			get;
		}

		/// <summary>
		/// Gets a value indicating whether this item is a search box.
		/// This can be used e.g. to determine if the item should be given focus
		/// when the user executes the Find command.
		/// </summary>
		/// <value> True if this is a search box, false if not. </value>
		bool IsSearchBox { get; }

		/// <summary> Gets a value indicating whether this toolbar item reacts to clicks. </summary>
		/// <value> True if reacts to click events, false if not. </value>
		bool Clickable { get; }

		/// <summary> Gets a value indicating whether this toolbar item is selectable. </summary>
		/// <value> True if selectable, false if not. </value>
		bool Selectable { get;  }

		/// <summary> Gets the desired minimum width for the toolbar item. </summary>
		/// <value> The minimum width. </value>
		float MinWidth { get; }

		/// <summary> Gets the maximum width for the toolbar item. </summary>
		/// <value> The maximum width. </value>
		float MaxWidth { get; }

		/// <summary>
		/// Gets the position and size of the item.
		/// This should be cached when Draw is called an it is the Layout event.
		/// </summary>
		/// <value> The position and size of the item. </value>
		Rect Bounds { get; set; }
		
		/// <summary>
		/// Delegate that will be called when item is being activated, but before the effects of the activation have been applied.
		///  
		/// An activation of an item usually triggers when the item is clicked or it has keyboard focus and an activation keyboard input is given,
		/// such as the return key being pressed.
		/// 
		/// For example if a this toolbar item is a button that opens a menu, then the callback is invoked after the button is pressed and before
		/// the menu has opened.
		/// 
		/// If Event.current is used during the invocation of this callback, then the ToolbarItem itself won't react to the activation input.
		/// 
		/// </summary>
		/// <value> delegate </value>
		[CanBeNull]
		Action<IInspectorToolbarItem, Rect, ActivationMethod> OnBeingActivated { get; set; }

		/// <summary>
		/// Gets the url to the documentation page for the toolbar item.
		/// Returns an empty string if has no documentation page.
		/// </summary>
		[NotNull]
		string DocumentationPageUrl
		{
			get;
		}

		/// <summary> Setups the inspector toolbar item. </summary>
		/// <param name="inspectorContainingToolbar"> The inspector that contains the toolbar that holds this item. This cannot be null. </param>
		/// <param name="toolbarContainingItem"> The toolbar that holds this item. </param>
		void Setup([NotNull]IInspector inspectorContainingToolbar, [NotNull]IInspectorToolbar toolbarContainingItem, ToolbarItemAlignment alignment);

		/// <summary>
		/// Draw the background for the item at the given position.
		/// This is called during every repaint event before Draw is called.
		/// </summary>
		/// <param name="itemPosition"> The position and size at which to draw the item. </param>
		/// <param name="toolbarBackgroundStyle"> The style used by the toolbar by default to draw the background. </param>
		void DrawBackground(Rect itemPosition, GUIStyle toolbarBackgroundStyle);

		/// <summary> Draw the item at the given position. </summary>
		/// <param name="itemPosition"> The position and size at which to draw the item. </param>
		/// <param name="mouseovered"> Is this item currently mouseovered? </param>
		void Draw(Rect itemPosition, bool mouseovered);

		/// <summary>
		/// Called every frame when the item is selected, after the Draw method has been called for all toolbar items.
		/// This can be used to overlay visuals indicating the selected state of the item.
		/// </summary>
		void DrawSelectionRect(Rect itemPosition);

		/// <summary> Called when the item is clicked with the left mouse button. </summary>
		/// <param name="inputEvent"> The input event for the click. </param>
		/// <returns> True if item consumed the click event, false if not. </returns>
		bool OnClick([NotNull]Event inputEvent);

		/// <summary> Called when the item is clicked with the right mouse button. </summary>
		/// <param name="inputEvent"> The input event for the click. </param>
		/// <returns> True if item consumed the click event, false if not. </returns>
		bool OnRightClick([NotNull]Event inputEvent);

		/// <summary> Called when the item is clicked with the middle mouse button. </summary>
		/// <param name="inputEvent"> The input event for the click. </param>
		/// <returns> True if item consumed the click event, false if not. </returns>
		bool OnMiddleClick([NotNull]Event inputEvent);

		/// <summary> Called when keyboard input is given with the toolbar item is NOT selected. </summary>
		/// <param name="inputEvent"> The keyboard input event. </param>
		/// <param name="keys"> Information about current user key configuration. </param>
		/// <returns> True if item consumed the input event, false if not. </returns>
		bool OnKeyboardInputGivenWhenNotSelected([NotNull]Event inputEvent, [NotNull]KeyConfigs keys);

		/// <summary> Called when keyboard input is given with the item selected. </summary>
		/// <param name="inputEvent"> The keyboard input event. </param>
		/// <param name="keys"> Information about current user key configuration. </param>
		/// <returns> True if item consumed the input event, false if not. </returns>
		bool OnKeyboardInputGiven([NotNull]Event inputEvent, [NotNull]KeyConfigs keys);
				
		/// <summary>
		/// Called during the validate command event.
		/// </summary>
		/// <param name="e"> The validate command event. </param>
		void OnValidateCommand([NotNull]Event e);

		/// <summary>
		/// Called when user has given the find command with the inspector selected.
		/// This event is first sent to the selected toolbar item (if any) and then
		/// to all other toolbar items in order from left to right, stopping if any
		/// of them consumes the event (returns true).
		/// </summary>
		/// <returns> True if item consumed the input event, false if not. </returns>
		bool OnFindCommandGiven();

		/// <summary> Called when the toolbar item is selected. </summary>
		/// <param name="reason"> The reason why the item was selected. </param>
		void OnSelected(ReasonSelectionChanged reason);

		/// <summary> Called when the toolbar item is deselected. </summary>
		/// <param name="reason"> The reason why the item was deselected. </param>
		void OnDeselected(ReasonSelectionChanged reason);

		/// <summary> Determine if toolbar item should be shown at this time. </summary>
		/// <returns> True if should show, false if should hide. </returns>
		bool ShouldShow();

		/// <summary> Called when the item becomes invisible. </summary>
		void OnBecameInvisible();

		/// <summary>
		/// Called when inspector containing item is disposed to the object pool.
		/// Should unsubscribe from all relevant items.
		/// </summary>
		void Dispose();
	}
}