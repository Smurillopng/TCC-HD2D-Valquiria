namespace Sisus
{
	/// <summary> Utility for checking, enabling and disabling development mode for Power Inspector. </summary>
	public static class DevMode
	{
		public const string Define = "DEV_MODE";
		public const string AssertationsDefine = "PI_ASSERTATIONS";

		public static bool Enabled
		{
			get
			{
				return IsEnabled();
			}

			set
			{
				if(value)
				{
					Enable();
				}
				else
				{
					Disable();
				}
			}
		}

		public static bool IsEnabled()
		{
			return ScriptingDefines.Contains(Define);
		}

		public static void Enable()
		{
			if(ScriptingDefines.Add(Define, AssertationsDefine))
			{
				if(InspectorUtility.ActiveManager != null && InspectorUtility.ActiveManager.SelectedInspector != null)
				{
					InspectorUtility.ActiveManager.SelectedInspector.Message("Enabling Development Mode...", null, MessageType.Info, false);
					//UnityEngine.Debug.Log("Added "+Define+" and "+AssertationsDefine+" to Scripting Define Symbols in Player Settings.");
				}
			}
		}

		public static void Disable()
		{
			if(ScriptingDefines.Remove(Define, AssertationsDefine))
			{
				if(InspectorUtility.ActiveManager != null && InspectorUtility.ActiveManager.SelectedInspector != null)
				{
					InspectorUtility.ActiveManager.SelectedInspector.Message("Disabing Development Mode...", null, MessageType.Info, false);
					//UnityEngine.Debug.Log("Removed "+Define+" and "+AssertationsDefine+" from Scripting Define Symbols in Player Settings.");
				}
			}
		}
	}
}