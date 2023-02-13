using UnityEngine;

namespace Sisus
{
	/// <summary>
	/// Interface implemented by drawers whose prefix label can sometimes be be dragged to adjust the value of one or more of their members.
	/// </summary>
	public interface IDraggablePrefixAffectsMember : IDraggablePrefix
	{
		/// <summary>
		/// Gets a value indicating whether or not dragging the prefix of the drawer adjusts the value of the given member.
		/// </summary>
		/// <param name="member"> Member of drawer to test. </param>
		/// <returns> True if prefix dragging affects member, otherwise false. </returns>
		bool DraggingPrefixAffectsMember(IDrawer member);
	}
}