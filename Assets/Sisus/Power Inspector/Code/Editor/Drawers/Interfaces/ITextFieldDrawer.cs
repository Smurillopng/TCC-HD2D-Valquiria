using UnityEngine;

namespace Sisus
{
	public interface ITextFieldDrawer : IFieldDrawer
	{
		/// <summary> Start editing text field. </summary>
		void StartEditingField();

		/// <summary> Stop editing text field. </summary>
		void StopEditingField();

		/// <summary>
		/// When called during the OnClick event, handles selecting the field,
		/// starting field editing and using the event.
		/// </summary>
		/// <param name="inputEvent"> The click event. </param>
		/// <param name="reason"> The reason why selection changed; what was clicked? </param>
		void HandleOnClickSelection(Event inputEvent, ReasonSelectionChanged reason);
	}
}