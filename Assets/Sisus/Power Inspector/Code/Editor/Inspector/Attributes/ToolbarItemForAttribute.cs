using System;
using JetBrains.Annotations;

namespace Sisus.Attributes
{
	/// <summary>
	/// Attribute that can be used to specify for which inspector class type this toolbar is used.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true), MeansImplicitUse(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature, ImplicitUseTargetFlags.Itself)]
	public class ToolbarItemForAttribute : Attribute
	{
		/// <summary>
		/// Type of the inspector toolbar class into which this item should be attached.
		/// The class needs to implement IInspectorToolbar.
		/// </summary>
		[NotNull]
		public readonly Type inspectorToolbarType;

		/// <summary>
		/// Determines whether or not the indexInToolbar starts counting from the left or the right.
		/// </summary>
		public readonly ToolbarItemAlignment alignment;

		/// <summary>
		/// The desired zero-based index at which the item wants to be placed on the toolbar.
		/// 
		/// If alignment is Left this gives the left-to-right order, otherwise this gives the right-to-left order.
		/// 
		/// If multiple items want to occupy the same index, only one will be selected for that slot.
		/// Items where isFallback has been set to false in the ToolbarItemForAttribute have higher priority in this situation.
		/// 
		/// It does not matter that all toolbar item indexes are consecutive, they are merely used for sorting the items into order, and any
		/// gaps between the indexes are ignored.
		/// </summary>
		/// <example>
		/// There are five toolbar items that target the same toolbar class:
		/// A, B, C, D and E.
		/// The alignment values for the five items are the following:
		/// Right, Right, Right, Right, Left
		/// The IndexInToolbar values for the five items are the following:
		/// 0, -1, 0, 100 and 100.
		/// The items have the following isFallback values:
		/// true, false, false, false, false.
		/// 
		/// The targeted toolbar will contain the following items and in this order:
		/// E - placed first because it is the only item with Left alignment.
		/// D - placed second because it has the largest index in the right-to-left order.
		/// C - both A and C occupy the same slot and, and A is discarded because it is a fallback item.
		/// B - placed last because it has the smallest index from the right.
		/// </example>
		public readonly int indexInToolbar;

		/// <summary>
		/// If true the toolbar item class which has this attribute has lower priority than any toolbar item classes with the attribute where this is false.
		/// </summary>
		public readonly bool isFallback;
		
		public ToolbarItemForAttribute([NotNull]Type setInspectorToolbarType, int setIndexInToolbar = 100, ToolbarItemAlignment setAlignment = ToolbarItemAlignment.Left, bool setIsFallback = false)
		{
			inspectorToolbarType = setInspectorToolbarType;
			alignment = setAlignment;
			indexInToolbar = setIndexInToolbar;
			isFallback = setIsFallback;

			#if DEV_MODE && PI_ASSERTATIONS
			UnityEngine.Debug.Assert(inspectorToolbarType == setInspectorToolbarType);
			UnityEngine.Debug.Assert(alignment == setAlignment);
			UnityEngine.Debug.Assert(indexInToolbar == setIndexInToolbar);
			UnityEngine.Debug.Assert(inspectorToolbarType != null);
			UnityEngine.Debug.Assert(typeof(IInspectorToolbar).IsAssignableFrom(inspectorToolbarType));
			#endif
		}
	}
}