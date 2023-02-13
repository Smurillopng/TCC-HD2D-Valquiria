namespace Sisus
{
	public enum OverrideFieldFocusing
	{
		/// <summary> Never override field focusing system. </summary>
		Never = 0,

		/// <summary> Switch between built-in and overridden field focusing systems on a case-by-case basis. </summary>
		Dynamic = 1,

		/// <summary> Always override field focusing system. </summary>
		Always = 2
	}
}