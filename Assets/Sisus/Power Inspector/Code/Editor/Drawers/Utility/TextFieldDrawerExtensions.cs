namespace Sisus
{
	public static class TextFieldDrawerExtensions
	{
		public static bool CanEditField(this ITextFieldDrawer target)
		{
			return !target.ReadOnly && !InspectorUtility.ActiveManager.HasMultiSelectedControls;
		}
	}
}