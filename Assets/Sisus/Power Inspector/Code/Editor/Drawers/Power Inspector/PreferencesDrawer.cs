using System;
using UnityEngine;
using Sisus.Attributes;
using Object = UnityEngine.Object;
using JetBrains.Annotations;

namespace Sisus
{
	/// <summary>
	/// Handles drawing the InspectorPreferences component inside the inspector view.
	/// </summary>
	[Serializable, DrawerForAsset(typeof(InspectorPreferences), true, false)]
	public sealed class PreferencesDrawer : AssetWithSideBarDrawer
	{
		private const int ExpectedViewCount = 17;
		private const float ExpectedSideBarItemHeight = 25f;
		#if UNITY_EDITOR
		private const float ExpectedHeaderHeight = EditorGUIDrawer.AssetTitlebarHeightWithOneButtonRow;
		#else
		private const float ExpectedHeaderHeight = DrawGUI.SingleLineHeight;
		#endif
		private const float ExpectedToolbarHeight = PreferencesToolbar.DefaultToolbarHeight;

		private float height;

		private int documentationButtonIndex;
		private string lastShownDocumentationUrl = "";

		/// <inheritdoc />
		protected override bool HasExecuteMethodIcon
		{
			get
			{
				return DebugMode;
			}
		}

		public static Vector2 GetExpectedMinSize(float toolbarHeight = ExpectedToolbarHeight)
		{
			return new Vector2(800f, GetExpectedHeight(toolbarHeight));
		}

		public static Vector2 GetExpectedMaxSize(float toolbarHeight = ExpectedToolbarHeight)
		{
			return Vector2.zero;
		}

		public static float GetExpectedHeight(float toolbarHeight = ExpectedToolbarHeight)
		{
			return ExpectedViewCount * ExpectedSideBarItemHeight  + ExpectedToolbarHeight + ExpectedHeaderHeight - 4f;
		}

		private bool IsOpenInPreferencesInspector
		{
			get
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(inspector != null, "PreferencesDrawer inspector was null!");
				#endif

				return inspector is PreferencesInspector;
			}
		}

		/// <inheritdoc/>
		protected override string OverrideDocumentationUrl(out string documentationTitle)
		{
			documentationTitle = "Power Inspector Preferences";
			return "powerinspector/category/preferences";
		}

		/// <inheritdoc/>
		public override string DocumentationPageUrl
		{
			get
			{
				return "";
			}
		}

		/// <inheritdoc/>
		protected override void Setup(Object[] setTargets, IParentDrawer setParent, GUIContent setLabel, IInspector setInspector)
		{
			#if DEV_MODE
			Debug.Assert(this is IScrollable);
			#endif
			
			setInspector.Manager.OnSelectionChanged += OnSelectionChanged;

			base.Setup(setTargets, setParent, setLabel, setInspector);

			#if UNITY_EDITOR
			if(setInspector is PreferencesInspector)
			{
				UpdateMinMaxSize();
			}
			#endif

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(inspector != null);
			#endif
		}

		private void OnSelectionChanged(IInspector selectedInspector, InspectorPart inspectorPart, IDrawer focusedDrawer)
		{
			#if DEV_MODE
			Debug.Log(StringUtils.ToColorizedString("OnSelectionChanged: inspector=", selectedInspector, ", drawer=", focusedDrawer));
			#endif

			if(selectedInspector == inspector && focusedDrawer != null)
			{
				ShowDocumentationForMember(focusedDrawer);
			}
		}

		private void OnFailToLoadDocumentationPage([NotNull]string failedToLoadUrl)
		{
			#if DEV_MODE
			Debug.LogWarning("Failed to load documentation page: "+failedToLoadUrl);
			#endif
			
			if(!string.IsNullOrEmpty(failedToLoadUrl))
			{
				PowerInspectorDocumentation.ShowPreferencesIfWindowOpen("");
			}
		}

		private void ShowDocumentationForMember([NotNull]IDrawer member)
		{
			if(member == this)
			{
				PowerInspectorDocumentation.ShowPreferencesIfWindowOpen("", OnFailToLoadDocumentationPage);
				OnAutoDocumentationUpdated(PowerInspectorDocumentation.PreferencesUrl);
				return;
			}

			var parent = member.Parent;
			if(parent != this && member != this && parent != null)
			{
				switch(parent.Name)
				{
					// Usually we want to get the documentation page for the member that is at the root of the preferences drawer.
					default:
						for(; member.Parent != this && member.Parent != null; member = member.Parent);
						break;
					// Sometimes we have specific documentation pages for nested members too.
					case "Mouseover Effects":
					case "Default Inspector":
					case "Create Script Wizard":
						break;
				}
			}

			var memberInfo = member.MemberInfo;
			if(memberInfo != null)
			{
				var name = memberInfo.DisplayName;
				var url = GetDocumentationPageUrl(name);

				#if DEV_MODE
				Debug.Log("Showing preferences documentation: "+ url);
				#endif
				
				PowerInspectorDocumentation.ShowPreferencesIfWindowOpen(url, OnFailToLoadDocumentationPage);
				OnAutoDocumentationUpdated(url);
			}
		}


		private void OnAutoDocumentationUpdated(string url)
		{
			#if DEV_MODE
			Debug.Log("OnAutoDocumentationUpdated(\""+url+ "\") with lastShownDocumentationUrl=\""+ lastShownDocumentationUrl+"\"");
			#endif

			if(!string.Equals(lastShownDocumentationUrl, url))
			{
				lastShownDocumentationUrl = url;
				headerButtons[documentationButtonIndex].Color = new Color32(234, 255, 238, 255);
				var inspector = Inspector;
				StaticCoroutine.LaunchDelayed(0.3f, ()=>RestoreDocumentationButtonColor(inspector), true);
			}
		}

		private void RestoreDocumentationButtonColor(IInspector inspector)
		{
			if(inactive)
			{
				return;
			}

			InspectorUtility.ActiveManager.ActiveInspector = inspector;
			headerButtons[documentationButtonIndex].Color = DrawGUI.UniversalColorTint;
			inspector.InspectorDrawer.Repaint();
		}

		/// <summary>
		/// Given the display name of a drawer that is a member of the PreferencesDrawer returns the name of the page
		/// that contains the documentation for the preference item in question.
		/// 
		/// If member has no specific documentation page, returns an empty string.
		/// </summary>
		/// <param name="memberName"> Name of member drawer. </param>
		/// <returns> Preferences documentation page name, or empty string if none exists. </returns>
		[NotNull]
		private string GetDocumentationPageUrl(string memberName)
		{
			switch(memberName)
			{
				case "":
					return "";
				case "Curly Braces On New Line":
					return "create-script-wizard-curly-braces-on-new-line";
				case "Add Comments":
					return "create-script-wizard-add-comments";
				case "Add Comments As Summary":
					return "create-script-wizard-add-comments-as-summary";
				case "Word Wrap Comments After Characters":
					return "create-script-wizard-word-wrap-comments-after-characters";
				case "Add Used Implicitly":
					return "create-script-wizard-add-used-implicitly";
				case "Space After Method Name":
					return "create-script-wizard-space-after-method-name";
				case "New Line":
					return "create-script-wizard-new-line";
				case "Using Namespace Options":
					return "create-script-wizard-using-namespace-options";
				case "Unity Object Header":
					return "mouseover-effects-unity-object-header";
				case "Unity Object Header Tint":
					return "mouseover-effects-unity-object-header-tint";
				case "Header Button":
					return "mouseover-effects-header-button";
				case "Prefix Label":
					return "mouseover-effects-prefix-label";
				case "Prefix Label Tint":
					return "mouseover-effects-prefix-label-tint";
				case "Override GameObject Editor":
					return "default-inspector-override-gameObject-editor";
				case "Override Component Editors":
					return "default-inspector-override-component-editors";
				case "Override Asset Editors":
					return "default-inspector-override-asset-editors";
				case "Enhance Field Context Menu":
					return "default-inspector-enhance-field-context-menu";
				case "Enhance Unity Object Context Menu":
					return "default-inspector-enhance-unity-object-context-menu";
				case "Message Display Method":
					return "messages-message-display-method";
				case "Display Duration Per Word":
					return "messages-display-duration-per-word";
				case "Min Display Duration":
					return "messages-min-display-duration";
				case "Max Display Duration":
					return "messages-max-display-duration";
				case "Position Label":
					return "transform-drawer-position-label";
				case "Rotation Label":
					return "transform-drawer-rotation-label";
				case "Scale Label":
					return "transform-drawer-scale-label";
				case "X Label":
					return "transform-drawer-x-label";
				case "Y Label":
					return "transform-drawer-y-label";
				case "Z Label":
					return "transform-drawer-z-label";
				case "Tint XYZ Labels":
					return "transform-drawer-tint-xyz-labels";
				case "Generate From Add Component Menu":
					return "categorized-components-generate-from-add-component-menu";
				case "Classic Look":
				case "Compact Look":
				case "Iconographic Look":
				case "Colorful XYZ":
					return "transform-drawer";
				default:
					return memberName.ToLower().Replace('-', ' ').Replace(' ', '-');
			}
		}

		#if UNITY_EDITOR
		private void UpdateMinMaxSize()
		{
			if(IsOpenInPreferencesInspector)
			{
				var window = inspector.InspectorDrawer as UnityEditor.EditorWindow;
				if(window != null)
				{
					window.minSize = new Vector2(800f, height);
					window.maxSize = new Vector2(4000f, 4000f);
				}
			}
		}
		#endif

		#if UNITY_EDITOR
		/// <inheritdoc/>
		protected override void DoGenerateMemberBuildList()
		{
			#if DEV_MODE
			Debug.Log(ToString()+ ".DoGenerateMemberBuildList");
			#endif

			base.DoGenerateMemberBuildList();

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(Views.Length > 0);
			Debug.Assert(sideBarItemHeight > 0f);
			Debug.Assert(inspector.ToolbarHeight > 0f);
			#endif

			height = Views.Length * sideBarItemHeight  + inspector.ToolbarHeight + HeaderHeight - 4f;

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(ExpectedViewCount == Views.Length, "ExpectedViewCount " + ExpectedViewCount + " != Views.Length " + Views.Length);
			Debug.Assert(ExpectedSideBarItemHeight == sideBarItemHeight);
			Debug.Assert(ExpectedHeaderHeight == HeaderHeight, "ExpectedHeaderHeight "+ ExpectedHeaderHeight+" != "+HeaderHeight);
			Debug.Assert(ExpectedToolbarHeight == inspector.ToolbarHeight, "ExpectedToolbarHeight "+ExpectedToolbarHeight+" != "+inspector.ToolbarHeight);
			Debug.Assert(GetExpectedHeight() == height, "GetExpectedHeight() "+GetExpectedHeight()+" != "+height);
			Debug.Assert(GetExpectedHeight(inspector.ToolbarHeight) == height, "GetExpectedHeight(ToolbarHeight) "+GetExpectedHeight(inspector.ToolbarHeight)+" != "+height);
			#endif

			UpdateMinMaxSize();

			PowerInspectorDocumentation.ShowPreferencesIfWindowOpen("", OnFailToLoadDocumentationPage);
		}
		#endif

		/// <inheritdoc/>
		public override bool Draw(Rect position)
		{
			if(!IsOpenInPreferencesInspector)
			{
				var buttonRect = position;
				buttonRect.x += 10f;
				buttonRect.y += 10f;
				buttonRect.width = 130f;
				buttonRect.height = 30f;
				if(GUI.Button(buttonRect, "Edit Preferences"))
				{
					DrawGUI.ExecuteMenuItem(PowerInspectorMenuItemPaths.Preferences);
					return true;
				}
				return false;
			}
			
			return base.Draw(position);
		}

		#if UNITY_EDITOR
		/// <inheritdoc/>
		protected override void OnLayoutEvent(Rect position)
		{
			UpdateMinMaxSize();
			base.OnLayoutEvent(position);
		}
		#endif

		/// <inheritdoc/>
		protected override void DoBuildHeaderButtons()
		{
			AddHeaderButton(Button.Create(new GUIContent("Restore Defaults"), ConfirmRestoreDefaults));
			AddHeaderButton(Button.Create(new GUIContent("Documentation"), PowerInspectorDocumentation.ShowPreferences));
			documentationButtonIndex = headerButtons.Count - 1;
		}

		private void ConfirmRestoreDefaults()
		{
			if(DrawGUI.Active.DisplayDialog("Reset All Preferences To Default Values?", "Are you sure you want to restore all preference items to their default values?\n\nThis affects all items, and not just the ones in the currently open view.", "Reset All", "Cancel"))
			{
				for(int n = targets.Length - 1; n >= 0; n--)
				{
					var target = (InspectorPreferences)targets[n];
					target.ResetToDefaults();
				}

				RebuildMemberBuildListAndMembers();

				if(Event.current != null)
				{
					ExitGUIUtility.ExitGUI();
				}
			}
		}

		/// <inheritdoc/>		
		public override void Dispose()
		{
			inspector.Manager.OnSelectionChanged -= OnSelectionChanged;

			base.Dispose();
		}
	}
}