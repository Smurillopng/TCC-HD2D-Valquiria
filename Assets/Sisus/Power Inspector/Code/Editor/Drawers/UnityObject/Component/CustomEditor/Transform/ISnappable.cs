using JetBrains.Annotations;
using System;

namespace Sisus
{
	public interface ISnappable : IParentDrawer
	{
		/// <summary>
		/// Snap value based on current Snapping preferences
		/// </summary>
		void Snap();

		/// <summary> Size of step when snapping mode is turned on for the drawer. </summary>
		/// <param name="memberIndex"> Zero-based index of the member being snapped. </param>
		/// <value> Step size. </value>
		float GetSnapStep(int memberIndex);

		/// <summary> Is snapping mode turned on for the drawer. </summary>
		/// <value> True if snapping enabled, false if not. </value>
		bool SnappingEnabled { get; }

		/// <summary>
		/// Snap member value based on current Snapping preferences
		/// </summary>
		/// <param name="memberIndex"> Zero-based index of the member. </param>
		/// <param name="memberValue"> [in,out] The member value to snap. </param>
		/// <param name="nicifyAndConvert">
		/// [in,out] Method that handles nicifying the value (rounding of excessive decimal points) and converting it from double to float.
		/// </param>
		void SnapMemberValue(int memberIndex, ref float memberValue, [NotNull]Func<double, float> nicifyAndConvert);
	}
}