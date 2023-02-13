using JetBrains.Annotations;
using Object = UnityEngine.Object;

namespace Sisus
{
	public interface ISplittableInspectorDrawer : IInspectorDrawer
	{
		/// <summary>
		/// Gets the bottom split view.
		/// </summary>
		/// <value> The split view or null if view is not split. </value>
		[CanBeNull]
		IInspector SplitView { get; }

		/// <summary>
		/// Gets a value indicating whether the view has been split in two.
		/// </summary>
		/// <value> True if view is split, false if not. </value>
		bool ViewIsSplit { get; }

		/// <summary>
		/// Splits the view in two or closes the bottom view.
		/// </summary>
		/// <param name="enabled"> True to split the view, false to close bottom view if open. </param>
		void SetSplitView(bool enabled);

		/// <summary>
		/// Closes the main view, and makes the bottom split view as the new main view.
		/// This should only be called when ViewIsSplit is true.
		/// </summary>
		void CloseMainView();

		/// <summary>
		/// Opens a new split view.
		/// This should only be called when ViewIsSplit is false.
		/// </summary>
		void OpenSplitView();

		/// <summary>
		/// Closes the split view.
		/// This should only be called when ViewIsSplit is true.
		/// </summary>
		void CloseSplitView();
		
		/// <summary>
		/// "Peeks" the given targets, splitting the inspector in two (if not already) and
		/// inspecting them in the bottom split view.
		/// </summary>
		/// <param name="inspect"> The UnityEngine.Object targets to peek. </param>
		void ShowInSplitView([NotNull]params Object[] inspect);

		/// <summary>
		/// "Peeks" the given target, splitting the inspector in two (if not already) and
		/// inspecting it in the bottom split view.
		/// </summary>
		/// <param name="inspect"> The UnityEngine.Object target to peek. </param>
		/// <param name="throwExitGUIException">
		/// If true an ExitGUIException will be thrown to avoid errors from GUI Layout being changed
		/// between the Layout and Repaint events.
		/// </param>
		void ShowInSplitView(Object target, bool throwExitGUIException);
	}
}