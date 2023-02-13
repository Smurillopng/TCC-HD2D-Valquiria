using JetBrains.Annotations;
using UnityEngine;
using UnityEditor;

namespace Sisus
{
	/// <summary> Welcome screen window for Power Inspector. </summary>
	public class WelcomeScreenWindow : EditorWindow
	{
		private static readonly GUIContent openLabel = new GUIContent("Open Window", "Open a new Power Inspector window.\n\nYou can also do this using the menu item\nWindow > Power Inspector > New Window");
		private static readonly GUIContent documentationLabel = new GUIContent("Documentation", "Open documentation for Power Inspector.\n\nYou can also do this using the menu item\nWindow > Power Inspector > Documentation");
		private static readonly GUIContent preferencesLabel = new GUIContent("Preferences", "Open preferences for Power Inspector.\n\nYou can also do this using the menu item\nWindow > Power Inspector > Preferences.");
		private static readonly GUIContent storePageLabel = new GUIContent("Store\nPage", "Vist the store page to check for updates or to leave a review for Power Inspector.\n\nYou can also do this using the menu item\nWindow > Power Inspector > Store Page");
		private static readonly GUIContent forumsLabel = new GUIContent("Visit\nForum", "Visit forum where you can discuss Power Inspector.\n\nYou can also do this using the menu item\nWindow > Power Inspector > Help > Forums");
		private static readonly GUIContent demoSceneLabel = new GUIContent("Demo\nScene", "Open demo scene showcasing the different features of Power Inspector.\n\nYou can also do this using the menu item\nWindow > Power Inspector > Demo Scene");

		[SerializeField]
		private GUIStyle titleStyle;
		[SerializeField]
		private GUIStyle textStyle;
		[SerializeField]
		private GUIStyle buttonStyle;
		[SerializeField]
		private GUIStyle largerButtonStyle;
		[SerializeField]
		private GUIStyle largestButtonStyle;
		[SerializeField]
		private GUIStyle versionNumberStyle;
		[SerializeField]
		private GUIContent versionNumberLabel;
		
		private bool setupDone;
		
		[MenuItem(PowerInspectorMenuItemPaths.WelcomeScreen, false, PowerInspectorMenuItemPaths.WelcomeScreenPriority), UsedImplicitly]
		private static void OpenWelcomeScreenWindow()
		{
			var created = CreateInstance<WelcomeScreenWindow>();
			created.name = "Power Inspector Welcome Window";
			created.titleContent = new GUIContent("");
			float width = 410f;
			float height = 470f;
			var rect = new Rect(Screen.width * 0.5f - width * 0.5f, Screen.height * 0.5f - height * 0.5f, width, height);
			created.position = rect;
			created.minSize = rect.size;
			created.maxSize = created.minSize;
			created.ShowUtility();
			created.Focus();
		}

		[UsedImplicitly]
		private void OnGUI()
		{
			if(!setupDone)
			{
				Setup();
			}

			GUILayout.Space(17f);

			GUILayout.Label("Thank you for choosing", titleStyle);
			
			GUILayout.Space(13f);

			GUILayout.Label(InspectorUtility.Preferences.graphics.PowerInspectorLogo, textStyle); // 326 x 68     was: 346 x 102 -> 34 diff

			var rect = GUILayoutUtility.GetLastRect();
			
			rect.y += 63f;

			GUI.Label(rect, "Let's help you get started...", textStyle);

			GUILayout.Space(58f);

			GUILayout.BeginHorizontal();
			{
				GUILayout.Space(50f);

				if(GUILayout.Button(openLabel, largestButtonStyle, GUILayout.Height(40f)))
				{
					DrawGUI.ExecuteMenuItem(PowerInspectorMenuItemPaths.NewWindow);
					GUIUtility.ExitGUI();
				}
				
				GUILayout.Space(50f);
			}
			GUILayout.EndHorizontal();

			GUILayout.Space(15f);

			GUILayout.BeginHorizontal();
			{
				GUILayout.Space(50f);
				
				if(GUILayout.Button(documentationLabel, largestButtonStyle, GUILayout.Height(60f)))
				{
					DrawGUI.ExecuteMenuItem(PowerInspectorMenuItemPaths.Documentation);
					GUIUtility.ExitGUI();
				}
				
				GUILayout.Space(50f);
			}
			GUILayout.EndHorizontal();

			GUILayout.Space(15f);

			GUILayout.BeginHorizontal();
			{
				GUILayout.Space(50f);

				if(GUILayout.Button(preferencesLabel, largerButtonStyle, GUILayout.Height(40f)))
				{
					DrawGUI.ExecuteMenuItem(PowerInspectorMenuItemPaths.Preferences);
					GUIUtility.ExitGUI();
				}
				
				GUILayout.Space(50f);
			}
			GUILayout.EndHorizontal();

			GUILayout.Space(15f);

			GUILayout.BeginHorizontal();
			{
				GUILayout.Space(50f);

				if(GUILayout.Button(storePageLabel, buttonStyle, GUILayout.Width(90f), GUILayout.Height(50f)))
				{
					Application.OpenURL("http://u3d.as/1sNc");
					GUIUtility.ExitGUI();
				}

				GUILayout.Space(15f);

				if(GUILayout.Button(forumsLabel, buttonStyle, GUILayout.Width(90f), GUILayout.Height(50f)))
				{
					DrawGUI.ExecuteMenuItem(PowerInspectorMenuItemPaths.Forums);
					GUIUtility.ExitGUI();
				}

				GUILayout.Space(15f);

				if(GUILayout.Button(demoSceneLabel, buttonStyle, GUILayout.Width(90f), GUILayout.Height(50f)))
				{
					DrawGUI.ExecuteMenuItem(PowerInspectorMenuItemPaths.DemoScene);
					GUIUtility.ExitGUI();
				}
				
				GUILayout.Space(50f);
			}
			GUILayout.EndHorizontal();

			GUILayout.Space(12f);

			GUILayout.BeginHorizontal();
			{
				GUILayout.FlexibleSpace();
				GUILayout.Label(versionNumberLabel, versionNumberStyle);
				GUILayout.FlexibleSpace();
			}
			GUILayout.EndHorizontal();
		}

		private void Setup()
		{
			titleStyle = new GUIStyle(GUI.skin.label);
			titleStyle.alignment = TextAnchor.MiddleCenter;
			titleStyle.SetAllTextColors(DrawGUI.IsProSkin ? Color.white : Color.black);
			titleStyle.fontSize = 16;			
			titleStyle.fontStyle = FontStyle.Bold;

			textStyle = new GUIStyle(GUI.skin.label);
			textStyle.alignment = TextAnchor.MiddleCenter;
			textStyle.SetAllTextColors(DrawGUI.IsProSkin ? new Color(0.9f, 0.9f, 0.9f, 0.9f) : new Color(0.1f, 0.1f, 0.1f, 0.9f));
			textStyle.fontSize = 14;
			textStyle.fontStyle = FontStyle.Bold;

			buttonStyle = new GUIStyle(GUI.skin.button);
			buttonStyle.fontSize = 13;
			
			largerButtonStyle = new GUIStyle(GUI.skin.button);
			largerButtonStyle.fontSize = 14;

			largestButtonStyle = new GUIStyle(GUI.skin.button);
			largestButtonStyle.fontSize = 15;

			versionNumberStyle = new GUIStyle(GUI.skin.label);
			versionNumberStyle.alignment = TextAnchor.MiddleCenter;
			versionNumberStyle.SetAllTextColors(Color.grey);
			versionNumberStyle.fontSize = 10;

			versionNumberLabel = new GUIContent("Version "+Version.Current);

			var graphics = InspectorUtility.Preferences.graphics;

			//storePageLabel.image = graphics.AssetStoreIcon;
			//forumsLabel.image = graphics.SpeechBubbleIcon;
			//demoSceneLabel.image = graphics.SceneAssetIcon;

			setupDone = true;
		}
	}
}