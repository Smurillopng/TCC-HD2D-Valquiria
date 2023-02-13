namespace Sisus
{
	/// <summary>
	/// Interface for an asset drawer that contains a side bar on the left side with a list of views,
	/// only one of which can be active at any given time.
	/// </summary>
	public interface IAssetWithSideBarDrawer : IEditorlessAssetDrawer, IScrollable
	{
		/// <summary> Gets the active view. </summary>
		/// <value> The active view. </value>
		string ActiveView
		{
			get;
		}

		/// <summary> Gets the index of the active view. </summary>
		/// <value> The active view index. </value>
		int ActiveViewIndex
		{
			get;
		}

		/// <summary> Gets list of all views on the side bar. </summary>
		/// <value> The views on the side bar. </value>
		string[] Views
		{
			get;
		}

		/// <summary> Sets given view on the side bar active. </summary>
		/// <param name="setActive"> The view to set active. </param>
		void SetActiveView(string setActive);

		/// <summary> Sets previous view in side bar active. </summary>
		/// <param name="loopToEndIfOutOfBounds"> True if should move to end of list when if first item on list is currently selected. </param>
		void SetPreviousViewActive(bool loopToEndIfOutOfBounds);

		/// <summary> Sets next view in side bar active. </summary>
		/// <param name="loopToBeginningIfOutOfBounds"> True if should move to first item on list if last item on list is currently selected. </param>
		void SetNextViewActive(bool loopToBeginningIfOutOfBounds);
	}
}