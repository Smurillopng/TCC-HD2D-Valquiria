namespace Sisus
{
	/// <summary>
	/// An interface that the top-level drawer of an inspector can implement in order to
	/// receive events whenever there are changes to the hierarchy of an active scene or to the
	/// assets in the project.
	/// </summary>
	public interface IOnProjectOrHierarchyChanged
	{
		/// <summary> Called when the hierarchy of an active scene has changes. </summary>
		/// <param name="changed"> Enum describing what has changed. </param>
		/// <param name="hasNullReferences">
		/// Set to true if IOnProjectOrHierarchyChanged had missing references for mandatory
		/// targets and now has an invalid state.
		/// </param>
		void OnProjectOrHierarchyChanged(OnChangedEventSubject changed, ref bool hasNullReferences);
	}
}