namespace Sisus
{
	public enum UseEditorsOverDrawers
	{
		/// <summary>
		/// Always use Power Inspector's drawers if targets have no Custom Editors.
		/// 
		/// This gives you the greatest access to features of Power Inspector, at the cost of
		/// reduced compatibility with other plug-ins that rely on the usage of Editors.
		/// </summary>
		OnlyIfHasCustomEditor = 0,
		 
		/// <summary>
		/// (Default) Use Editors to handle drawing of class members when targets have Custom Editors, or when plug-ins "want" to use their own Editor type for the target.
		///
		///	When using this setting, Power Inspector not only tries to take into consideration whether or not plug-ins sometimes requiring the use Editors have been installed, but also whether or not the usage of Editors is needed for a specific target type, based on aspects such as the current preferences of other plug-ins.
		/// </summary>
		BasedOnPluginPreferences = 1,
		
		/// <summary>
		/// Always use Editors for drawing class members, if plug-ins that sometimes require the usage of Editors are detected to be installed,	otherwise always use Power Inspector's drawers.
		///
		/// This might be a good option for you if would prefer a consistent inspector experience with Editors always being used, over one where you gain and lose features based on the current context.
		/// </summary>
		BasedOnPlugins = 5,

		/// <summary>
		/// Always use Editors for all targets.
		/// 
		/// Always use Editors for all targets, never Power Inspector's drawers.
		///
		///	You will lose access to some features of Power Inspector, but compatibility with other plug-ins will be maximized.
		///
		///	This might be a good option if you have plug-ins or scripts that rely on Editors being used for drawing class members, but they are not detected by Power Inspector automatically when using the other available settings.
		/// </summary>
		Always = 10
	}
}