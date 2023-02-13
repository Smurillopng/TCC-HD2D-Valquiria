namespace Sisus
{
	public interface ICollectionDrawer : IParentDrawer
	{
		/// <summary>
		/// The amount you need to add to a member's index to get the
		/// index of the first collection element.
		/// </summary>
		/// <value>
		/// difference between IDrawer member index and collection element index
		/// </value>
		int FirstCollectionMemberIndexOffset { get; }

		/// <summary>
		/// how much do you need to subtract from members.Length to get the
		/// index of the last collection element
		/// </summary>
		/// <value>
		/// difference between IDrawer member count and last collection element index
		/// </value>
		int LastCollectionMemberCountOffset { get; }

		/// <summary>
		/// Gets the zero-based index of the first IDrawer in Members
		/// array which represents a value of an element in the collection
		/// (instead of representing an auxilary control, like a resize field)
		/// </summary>
		/// <value> index of first member representing an element in collection, -1 if there are none </value>
		int FirstCollectionMemberIndex { get; }
		
		/// <summary>
		/// Gets the zero-based index of the last IDrawer in Members
		/// array which represents a value of an element in the collection
		/// (instead of representing an auxilary control, like an add component button)
		/// </summary>
		/// <value> index of last member representing an element in collection, -1 if there are none </value>
		int LastCollectionMemberIndex { get; }

		/// <summary>
		/// Gets the zero-based index of the first IDrawer in VisibleMembers
		/// array which represents a value of an element in the collection
		/// (instead of representing an auxilary control, like a resize field)
		/// </summary>
		/// <value> index of first visible member representing an element in collection, -1 if there are none </value>
		int FirstVisibleCollectionMemberIndex { get; }

		/// <summary>
		/// Gets the zero-based index of the last IDrawer in VisibleMembers
		/// array which represents a value of an element in the collection
		/// (instead of representing an auxilary control, like an add component button)
		/// </summary>
		/// <value> index of last visible member representing an element in collection, -1 if there are none </value>
		int LastVisibleCollectionMemberIndex { get; }

		/// <summary> Get index of member in Members collection. </summary>
		/// <param name="member"> The member. </param>
		/// <returns> The member index in Members collection. </returns>
		int GetMemberIndexInCollection(IDrawer member);

		/// <summary>
		/// Deletes the given member from collection members and updates values of all target collections,
		/// cached value, memberBuildList and visible members to match this change.
		/// </summary>
		/// <param name="member"> The member which should be deleted. </param>
		void DeleteMember(IDrawer member);

		/// <summary>
		/// Duplicates the given member in collection members and updates values of all target collections,
		/// cached value, memberBuildList and visible members to match this change.
		/// </summary>
		/// <param name="member"> The member which should be duplicates. </param>
		void DuplicateMember(IFieldDrawer member);
	}
}