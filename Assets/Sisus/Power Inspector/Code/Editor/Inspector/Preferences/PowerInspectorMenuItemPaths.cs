namespace Sisus
{
	public class PowerInspectorMenuItemPaths
	{
		public const string Base = "Window/Power Inspector/";

		public const string NewWindow = Base + "New Window";
		public const string FocusOrOpenWindow = Base + "Focus Or Open New";
		public const string NewTab = Base + "New Tab %#T"; //ctrl+shift+T (ctrl+T stopped working after Unity 2018.2 for some reason)
		public const string CloseTab = Base + "Close Tab %W"; //ctrl+W

		public const string Preferences = Base + "Preferences\tConfigure";

		public const string Documentation = Base + "Documentation %#D";
		public const string DemoScene = Base + "Demo Scene";

		public const string CreateScriptWizard = Base + "C# Script Wizard\tCreate Scripts";
		public const string CreateScriptWizardFromCreateMenu = "Assets/Create/C# Script Wizard";

		public const string WelcomeScreen = Base + "Welcome Screen\tQuick Links";
		public const string CheckForUpdates = Base + "Store Page\tCheck For Updates";

		
		
		public const string Forums = Base + "Help/Forums";
		public const string IssueTracker = Base + "Help/Issue Tracker";
		public const string ContactSupport = Base + "Help/Contact Support";
		
		public const string ViewInPowerInspector = "CONTEXT/Object/View In Power Inspector";
		public const string PeekInPowerInspector = "CONTEXT/Object/Peek In Power Inspector";

		public const string Peek = "Edit/Peek\tMMB";
		public const string Reset = "Edit/Reset\tBackspace";



		public const int NewWindowPriority = -103;
		public const int NewTabPriority = -102;
		public const int CloseTabPriority = -101;
		public const int FocusOrOpenWindowPriority = -100;

		public const int PreferencesPriority = 100;

		public const int DocumentationPriority = 150;
		public const int DemoScenePriority = 151;

		public const int CreateScriptWizardPriority = 200;
		public const int CreateScriptWizardFromCreateMenuPrority = 82;

		public const int WelcomeScreenPriority = 300;
		public const int CheckForUpdatesPriority = 301;

		public const int RatePowerInspectorPriority = 350;

		public const int ForumsPriority = 401;
		public const int IssueTrackerPriority = 402;
		public const int ContactSupportPriority = 402;
	}
}