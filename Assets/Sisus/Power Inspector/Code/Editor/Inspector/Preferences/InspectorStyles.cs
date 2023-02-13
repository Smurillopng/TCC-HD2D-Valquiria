//#define DEBUG_CREATE

using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;

namespace Sisus
{
	public class InspectorStyles
	{
		public readonly GUIStyle Blank;
		public readonly GUIStyle Label;
		public readonly GUIStyle WhiteLabel;
		public readonly GUIStyle LineNumber;
		public readonly GUIStyle HeaderAttribute;
		public readonly GUIStyle HeaderToolbarButton;
		public readonly GUIStyle LargeLabel;
		public readonly GUIStyle Toolbar;
		public readonly GUIStyle ToolbarMenu;
		public readonly GUIStyle ToolbarCancel;
		public readonly GUIStyle ToolbarCancelEmpty;
		public readonly GUIStyle PreToolbar;
		public readonly GUIStyle PreToolbarCompact;
		public readonly GUIStyle PreDropDown;
		public readonly GUIStyle PreBackground;
		public readonly GUIStyle Button;
		public readonly GUIStyle MiniButton;
		public readonly GUIStyle PreToolbar2;
		public readonly GUIStyle Toggle;
		public readonly GUIStyle NullToggle;
		public readonly GUIStyle LockButton;
		public readonly GUIStyle FilterField;
		public readonly GUIStyle FilterFieldWithDropdown;
		public readonly GUIStyle ObjectField;
		public readonly GUIStyle GameObjectHeaderBackground;
		public readonly GUIStyle TitleText;
		public readonly GUIStyle TextField;
		public readonly GUIStyle WordWrappedTextArea;
		public readonly GUIStyle TextArea;
		public readonly GUIStyle MiniPopup;
		public readonly GUIStyle ToolbarLabel;
		public readonly GUIStyle SubHeader; // NOTE: InspectorPreferences.Styles.SubHeader is used by Hierarchy Folders - don't change the public API!
		public readonly GUIStyle MethodBackground;
		public readonly GUIStyle DragHandle;
		public readonly GUIStyle AddButton;
		public readonly GUIStyle Centered;
		public readonly GUIStyle SecondaryInfo;
		public readonly GUIStyle AssetLabel;
		public readonly GUIStyle PingedHeader;
		public readonly GUIStyle formattedText;
		public readonly GUIStyle SideBarItem;
		public readonly GUIStyle WhiteTexture;
		public readonly GUIStyle PopupMenuTitle;
		public readonly GUIStyle HelpBox;
		public readonly GUIStyle EyeDropper;
		public readonly GUIStyle AddComponentButton;
		public readonly GUIStyle RangeIndicator;

		public InspectorStyles([NotNull]GUISkin skin)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(skin != null, "InspectorStyles constructor called with null skin parameter.");
			Debug.Assert(Event.current != null, "InspectorStyles constructor called with Event.current null.");
			#endif

			#if DEV_MODE && DEBUG_CREATE
			Debug.Log("InspectorStyles("+skin.name+")...");
			#endif

			if(skin == null || Event.current == null)
			{
				return;
			}

			Blank = skin.GetStyle("IN Label");
			Blank.margin.left = 0;
			Blank.margin.right = 0;
			Blank.margin.top = 0;
			Blank.margin.bottom = 0;
			Blank.border.left = 0;
			Blank.border.right = 0;
			Blank.border.top = 0;
			Blank.border.bottom = 0;
			Blank.padding.left = 0;
			Blank.padding.right = 0;
			Blank.padding.top = 0;
			Blank.padding.bottom = 0;
			Blank.overflow.left = 0;
			Blank.overflow.right = 0;
			Blank.overflow.top = 0;
			Blank.overflow.bottom = 0;
			Blank.fixedHeight = 0f;
			Blank.fixedWidth = 0f;

			Label = skin.GetStyle("label");

			var preferences = InspectorUtility.Preferences;
			var theme = preferences.theme;

			WhiteLabel = new GUIStyle(Label);
			WhiteLabel.SetAllTextColors(Color.white);

			LineNumber = new GUIStyle(Label);
			LineNumber.SetAllTextColors(theme.SyntaxHighlight.TypeColor);

			HeaderAttribute = new GUIStyle(skin.GetStyle("BoldLabel"));
			HeaderAttribute.wordWrap = true;
			HeaderAttribute.richText = true;
			LargeLabel = skin.GetStyle("LargeLabel");
			
			Toolbar = skin.GetStyle("toolbarbutton");
			ToolbarCancel = skin.GetStyle("ToolbarSeachCancelButton");
			ToolbarCancelEmpty = skin.GetStyle("ToolbarSeachCancelButtonEmpty");
			ToolbarMenu = skin.GetStyle("ToolbarDropDown");
			PreToolbar = skin.GetStyle("PreToolbar");
			
			PreToolbarCompact = new GUIStyle(PreToolbar);
			var miniBoldLabel = skin.GetStyle("MiniBoldLabel");
			PreToolbarCompact.font = miniBoldLabel.font;
			PreToolbarCompact.fontSize = miniBoldLabel.fontSize;
			PreToolbarCompact.fontStyle = miniBoldLabel.fontStyle;
			
			PreDropDown = skin.GetStyle("PreDropDown");

			PreBackground = skin.GetStyle("PreBackground");
			Button = skin.GetStyle("button");
			MiniButton = skin.GetStyle("miniButton");

			ToolbarLabel = skin.GetStyle("MiniLabel");
			ToolbarLabel.alignment = TextAnchor.MiddleCenter;

			PreToolbar2 = skin.GetStyle("preToolbar2");
			Toggle = skin.toggle; //skin.GetStyle("IN Toggle");
			
			LockButton = skin.GetStyle("IN LockButton");
			#if UNITY_2019_3_OR_NEWER
			LockButton.fixedWidth = 15f;
			LockButton.fixedHeight = 15f;
			#endif
			FilterField = skin.GetStyle("ToolbarSeachTextField");
			FilterFieldWithDropdown = skin.GetStyle("ToolbarSeachTextFieldPopup");

			#if UNITY_2018_1_OR_NEWER && UNITY_EDITOR
			GameObjectHeaderBackground = (GUIStyle)typeof(UnityEditor.EditorStyles).GetProperty("inspectorBig", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null, null);
			#else
			GameObjectHeaderBackground = skin.GetStyle("IN GameObjectHeader");
			#endif
			ObjectField = skin.GetStyle("ObjectField");
			
			TextField = skin.GetStyle("textfield");			
			TextArea = new GUIStyle(UnityEditor.EditorStyles.textArea);
			TextArea.wordWrap = false;
			WordWrappedTextArea = new GUIStyle(TextArea);
			WordWrappedTextArea.wordWrap = true;

			TitleText = skin.GetStyle("IN TitleText");
			MiniPopup = skin.GetStyle("MiniPopup");
			MethodBackground = skin.GetStyle("HelpBox");
			DragHandle = skin.GetStyle("RL DragHandle");
			AddButton = skin.GetStyle("OL Plus");

			SubHeader = new GUIStyle(Label);
			SubHeader.SetAllTextColors(new Color32(86, 156, 214, 255));
			SubHeader.clipping = TextClipping.Clip;

			Centered = new GUIStyle();
			Centered.alignment = TextAnchor.MiddleCenter;
			Centered.SetAllTextColors(Label.normal.textColor);

			SecondaryInfo = new GUIStyle(skin.GetStyle("ShurikenValue"));
			SecondaryInfo.alignment = TextAnchor.MiddleRight;

			HeaderToolbarButton = Blank;
			HeaderToolbarButton.alignment = TextAnchor.MiddleCenter;
			HeaderToolbarButton.imagePosition = ImagePosition.ImageOnly;
			HeaderToolbarButton.stretchHeight = false;
			HeaderToolbarButton.stretchWidth = false;

			NullToggle = skin.GetStyle("OL Toggle");
			AssetLabel = skin.GetStyle("AssetLabel");

			var pingStyle = skin.FindStyle("OL Ping");
			if(pingStyle != null)
			{
				PingedHeader = new GUIStyle(pingStyle);
				PingedHeader.font = TitleText.font;
				PingedHeader.fontSize = TitleText.fontSize;
				PingedHeader.contentOffset = TitleText.contentOffset;
				PingedHeader.margin = TitleText.margin;
				PingedHeader.border = TitleText.border;
			}
			else
			{
				PingedHeader = new GUIStyle(LargeLabel);
			}

			formattedText = new GUIStyle(skin.label);
			formattedText.richText = true;

			#if UNITY_2019_1_OR_NEWER
			SideBarItem = new GUIStyle(skin.GetStyle("GroupBox"));
			#else
			SideBarItem = new GUIStyle(skin.button);
			#endif
			SideBarItem.alignment = TextAnchor.MiddleCenter;
			SideBarItem.clipping = TextClipping.Overflow;

			var proTheme = preferences.themes.Pro;
			var proTextColor = proTheme.PrefixIdleText;
			
			Label.normal.textColor = proTextColor;

			formattedText.normal.textColor = proTextColor;
			//remove color effects on mouseover etc.
			formattedText.active.textColor = formattedText.normal.textColor;
			formattedText.focused.textColor = formattedText.normal.textColor;
			formattedText.hover.textColor = formattedText.normal.textColor;

			HelpBox = skin.GetStyle("HelpBox");

			var acBoldHeader = skin.FindStyle("AC BoldHeader");
			if(acBoldHeader != null)
			{
				PopupMenuTitle = new GUIStyle(acBoldHeader);
				PopupMenuTitle.alignment = TextAnchor.MiddleCenter;
				PopupMenuTitle.clipping = TextClipping.Clip;
			}
			else
			{
				PopupMenuTitle = new GUIStyle(skin.GetStyle("BoldLabel"));
				PopupMenuTitle.alignment = TextAnchor.MiddleCenter;
				PopupMenuTitle.clipping = TextClipping.Clip;
			}

			WhiteTexture = new GUIStyle(skin.label)
			{
				stretchHeight = true,
				stretchWidth = true,
				border = new RectOffset(),
				margin = new RectOffset(),
				padding = new RectOffset(),
			};
			WhiteTexture.SetAllBackgrounds(DrawGUI.WhiteTexture);

			EyeDropper = skin.GetStyle("ColorField");

			AddComponentButton = skin.GetStyle("AC Button");

			#if UNITY_2019_3_OR_NEWER
			RangeIndicator = skin.FindStyle("StaticDropdown");
			#else
			RangeIndicator = skin.FindStyle("Icon.Connector");
			#endif
			if(RangeIndicator == null)
			{
				#if DEV_MODE
				Debug.LogError("Failed to find style Icon.Connector from source skin " + skin.name + ".");
				#endif
				RangeIndicator = GUI.skin.label;
			}

			#if DEV_MODE && PI_ASSERTATIONS
			var fields = GetType().GetFields();
			foreach(var field in fields)
			{
				Debug.Assert(field.GetValue(this) != null, "Field " + field.Name + " value was " + StringUtils.Null + " from source skin " + skin.name + ".");
			}
			#endif
		}
	}
}