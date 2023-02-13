namespace Sisus
{
	public enum ReasonSelectionChanged
	{
		Unknown = 0,

		PrefixClicked,
		ControlClicked,
		ThisClicked,
		OtherClicked,

		SelectControlUp,
		SelectControlLeft,
		SelectControlDown,
		SelectControlRight,
		SelectPrevControl,
		SelectNextControl,
		SelectPrevComponent,
		SelectNextComponent,
		SelectPrevOfType,
		SelectNextOfType,
		KeyPressShortcut,
		KeyPressOther,
		Peek,
		Initialization,
		Dispose,
		BecameInvisible,
		GainedFocus,
		LostFocus,
		Command
	}
}