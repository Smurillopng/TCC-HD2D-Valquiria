using System;

namespace Sisus
{
	/// <summary>
	/// Interface implemented by drawer representing UnityEngine.Objects other than GameObjects.
	/// This includes Components, ScriptableObjects and assets.
	///
	/// NOTE: Not implemented by ObjectReferenceDrawer, which only represents a *reference* to an
	/// UnityEngine.Object.
	/// </summary>
	public interface IUnityObjectDrawer : IParentDrawer
	{
		/// <summary>
		/// Gets the minimum width for the prefix label column.
		/// Some Editors might need this to be larger than the default value.
		/// </summary>
		/// <value> The minimum width for prefix label column. </value>
		float MinPrefixLabelWidth { get; }

		/// <summary>
		/// Gets the maximum width for the prefix label column.
		/// Some Editors might need this to be larger than the default value.
		/// </summary>
		/// <value> The maximum width for prefix label column. </value>
		float MaxPrefixLabelWidth { get; }

		/// <summary> Gets a value indicating whether the prefix resizer control is currently mouseovered. </summary>
		/// <value> True if prefix resizer is mouseovered, false if not. </value>
		bool PrefixResizerMouseovered { get; }

		/// <summary>
		/// Gets a value indicating whether the header has currently keyboard focus.
		/// This is true if any part of the header has keyboard focus, not just the base.
		/// </summary>
		/// <value> True if header is selected, false if not. </value>
		bool HeaderIsSelected { get; }

		/// <summary> Delegate callback invoked every time that Inspector width or prefix width has changed. </summary>
		/// <value> The on widths changed. </value>
		Action OnWidthsChanged { get; set; }

		/// <summary> Gets or sets the width of the prefix label. </summary>
		/// <value> The width of the prefix label. </value>
		float PrefixLabelWidth { get; set; }

		/// <summary> Gets the prefix resizer type to use for the drawer. </summary>
		/// <value> The prefix resizer type. </value>
		PrefixResizer PrefixResizer { get; }

		/// <summary> Enables Debug Mode+ for the drawer. </summary>
		void EnableDebugMode();
		
		/// <summary> Disables Debug Mode+ for the drawer. </summary>
		void DisableDebugMode();

		/// <summary> Visually pings the drawer in the inspector. </summary>
		void Ping();
	}
}