namespace Sisus
{
	/// <summary>
	/// Interface for IDrawer that internally handle their content being scrollable
	/// in cases where they have so much content it doesn't otherwise fit inside the Inspector View.
	/// </summary>
	public interface IScrollable : IDrawer
	{
		/// <summary>
		/// Returns a value indicating if the drawer is currently drawering a scrollable view.
		/// </summary>
		bool HasScrollView { get; }
	}
}