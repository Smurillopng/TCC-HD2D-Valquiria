using JetBrains.Annotations;

namespace Sisus
{
	public static class IParentDrawerExtensions
	{
		/// <summary>
		/// Finds and returns a direct visible member with the given name.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="memberName"></param>
		/// <returns></returns>
		[CanBeNull]
		public static IDrawer FindVisibleMember([NotNull]this IParentDrawer parent, [NotNull]string memberName)
		{
			var visibleMembers = parent.VisibleMembers;
			for(int n = visibleMembers.Length - 1; n >= 0; n--)
			{
				var member = visibleMembers[n];
				if(string.Equals(member.Name, memberName))
				{
					return member;
				}
			}
			return null;
		}

		/// <summary>
		/// Returns value indicating whether this is the parent or grand parent of the drawer.
		/// </summary>
		/// <param name="drawer"> Drawer to test. </param>
		/// <returns> True if is parent or grand parent of drawer. </returns>
		[CanBeNull]
		public static bool IsParentOrGrandParentOf([NotNull]this IParentDrawer parent, [NotNull]IDrawer drawer)
		{
			if(parent == drawer.Parent)
			{
				return true;
			}

			var visibleMembers = parent.VisibleMembers;
			for(int n = visibleMembers.Length - 1; n >= 0; n--)
			{
				var childAsParent = visibleMembers[n] as IParentDrawer;
				if(childAsParent != null && childAsParent.IsParentOrGrandParentOf(drawer))
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Moves keyboard focus to visible member or nested visible member at given path.
		/// If at any point while traversing down the member path the method fails to find the next member, then it will select the searched parent drawer instead.
		/// </summary>
		/// <param name="parent"> The parent drawer relative to which we are making the selection. </param>
		/// <param name="memberPath">
		/// The path to the member drawer.
		/// If selecting a direct member, then this equates to the name of the drawer.
		/// If selecting a nested member, then the first part of the path should be the name of the direct member drawer, followed by a slash character ('/') separator, followed by the rest of the path to the nested member.
		/// </param>
		/// <param name="reason"></param>
		/// <param name="unfoldParentsAsNeeded"></param>
		/// <returns> Selected drawer. </returns>
		public static IDrawer Select([NotNull]this IParentDrawer parent, [NotNull]string memberPath, ReasonSelectionChanged reason, bool unfoldParentsAsNeeded)
		{
			int directMemberNameEndsAt = memberPath.IndexOf('/');
			string directMemberName;
			if(directMemberNameEndsAt != -1)
			{
				directMemberName = memberPath.Substring(0, directMemberNameEndsAt);
				memberPath = memberPath.Substring(directMemberNameEndsAt + 1);
			}
			else
			{
				directMemberName = memberPath;
			}

			if(!parent.Unfolded && parent.Foldable)
			{
				if(unfoldParentsAsNeeded)
				{
					parent.Select(reason);
					return parent;
				}
				parent.SetUnfolded(true);
			}

			var member = parent.FindVisibleMember(directMemberName);
			if(member == null)
			{
				parent.Select(reason);
				return parent;
			}

			if(memberPath.Length == 0)
			{
				member.Select(reason);
				return member;
			}

			var memberAsParent = member as IParentDrawer;
			if(memberAsParent == null)
			{
				member.Select(reason);
				return member;
			}


			return memberAsParent.Select(memberPath, reason, unfoldParentsAsNeeded);
		}
	}
}