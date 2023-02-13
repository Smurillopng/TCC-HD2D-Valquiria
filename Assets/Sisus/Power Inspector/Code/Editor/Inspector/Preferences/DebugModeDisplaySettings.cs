namespace Sisus
{
	public class DebugModeDisplaySettings
	{
		public static readonly DebugModeDisplaySettings DefaultSettings = new DebugModeDisplaySettings();

		public bool Static = false;

		public bool ShowFields = true;
		public bool ShowProperties = true;
		public bool ShowMethods = false;
		
		public override string ToString()
		{
			return "f:"+ShowFields+"|p:"+ShowProperties+"|m:"+ShowMethods;
		}
	}

	public struct IncludeMembers
	{
		public bool Public;
		public bool NonPublic;

		public IncludeMembers(bool setAll)
		{
			Public = setAll;
			NonPublic = setAll;
		}
	}

	public struct IncludeFields
	{
		public bool Public;
		public bool NonPublic;
		public bool NonSerialized;
		public bool HideInInspector;
	}

	public struct IncludeProperties
	{
		public bool Public;
		public bool NonPublic;
		public bool autoProperty;
		public bool nonAutoProperty;
	}

	public struct IncludeMethods
	{
		public bool withoutParamaters;
		public bool withParameters;
		public bool withoutReturnValue;
		public bool withReturnValue;
		public bool nonGeneric;
		public bool generic;
	}
}