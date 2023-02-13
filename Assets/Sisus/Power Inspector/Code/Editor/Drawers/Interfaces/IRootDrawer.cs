#if UNITY_EDITOR
using UnityEngine;
#endif

namespace Sisus
{
	/// <summary>
	/// Interface implemented by drawer representing root level drawer placed directly under the top-level DrawerGroup
	/// representing the inspected targets.
	/// Both IGameObjectDrawer and IAssetDrawer implement this.
	/// </summary>
	public interface IRootDrawer : IParentDrawer
	{
		#if UNITY_EDITOR
		/// <summary>
		/// The asset labels of the target if it's an asset, otherwise a zero-length array.
		/// If there are multiple targets, contains only asset labels that are found on all of the targets.
		/// </summary>
		GUIContent[] AssetLabels { get; }
		
		/// <summary>
		/// If multiple assets are selected, returns asset labels that are found only on
		/// some of the targets, otherwise returns a zero-length array.
		/// </summary>
		GUIContent[] AssetLabelsOnlyOnSomeTargets { get; }
		#endif

		/// <summary> Gets a value indicating whether the drawer would prefer it if there was no search box visible in the Inspector toolbar.. </summary>
		/// <value> True if wants search box disabled, false if not. </value>
		bool WantsSearchBoxDisabled
		{
			get;
		}

		/// <summary>
		/// Callback for when the view menu is opening and an opportunity for adding items to it before it opens.
		/// </summary>
		/// <param name="menu"> [in,out] The view menu that is opening. </param>
		void AddItemsToOpeningViewMenu(ref Menu menu);
	}
}