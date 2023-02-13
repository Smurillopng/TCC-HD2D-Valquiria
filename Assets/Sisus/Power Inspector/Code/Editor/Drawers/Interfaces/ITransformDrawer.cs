using UnityEngine;

namespace Sisus
{
	/// <summary>
	/// Interface that should be implemented by drawers representing Transforms.
	/// </summary>
	public interface ITransformDrawer : IComponentDrawer
	{
		/// <summary>
		/// Does a downward raycast to find solid ground below the target Transforms.
		/// </summary>
		/// <returns>For each target returns raycast hit info if solid ground was found below them, or null if not. </returns>
		RaycastHit?[] RaycastGround();
	}
}