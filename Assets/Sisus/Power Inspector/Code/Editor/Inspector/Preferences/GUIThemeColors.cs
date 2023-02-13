using JetBrains.Annotations;
using Sisus.Attributes;
using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Sisus
{
	[Serializable]
	public class GUIThemeColors
	{
		/// <summary>
		/// The name of the theme
		/// </summary>
		public string Name = "Default";

		[Header("Content")]
		[SerializeField, HideInInspector]
		public GUISkin guiSkin;
		
		[NotNull]
		public GUIStyles guiStyles;
		
		[UsedImplicitly(ImplicitUseKindFlags.Assign), NotNull]
		public InspectorGraphics graphics;

		[UsedImplicitly(ImplicitUseKindFlags.Assign), NotNull]
		public InspectorLabels labels;

		/// <summary>
		/// Text color of prefix labels when selected.
		/// This should be set to match the color that Unity uses internally for prefix labels for best results.
		/// </summary>
		[Header("Prefix")]
		public Color PrefixIdleText = new Color32(0, 0, 0, 255);

		/// <summary>
		/// Line color of rectangle that is drawn around selected elements (e.g. around a text field)
		/// </summary>
		[Tooltip("Line color of rectangle that is drawn around selected elements (e.g. around a text field)")]
		public Color SelectedLineIndicator = new Color32(0, 55, 255, 128);
		public Color SelectedLineIndicatorUnfocused = new Color32(71, 71, 71, 128);
		public Color PrefixSelectedText = new Color32(0, 50, 230, 255);
		public Color PrefixSelectedUnfocusedText = new Color32(71, 71, 71, 255);
		public Color PrefixMouseoveredText = new Color32(239, 239, 239, 255);
		public Color PrefixMouseoveredRectHighlight = new Color32(74, 74, 74, 166);
		public Color PrefixMouseoveredRectShadow = new Color32(32, 32, 32, 115);

		[Header("Control")]
		[PTooltip("Line color of rectangle that is drawn around selected elements (e.g. around a text field)",
			"This should be set to match the color that Unity uses internally for text fields for best results.")]
		public Color ControlSelectedRect = new Color32(51, 79, 209, 255);
		public Color ControlSelectedUnfocusedRect = new Color32(115, 115, 115, 255);
		public Color ControlSelectedTint = new Color32(128, 152, 243, 255);
		public Color ControlSelectedUnfocusedTint = new Color32(143, 143, 143, 255);
		public Color ControlMouseoveredTint = new Color32(255, 255, 255, 28);
		public Color ControlUnappliedChangesTint = new Color32(128, 255, 128, 10);
		public Color CanDragPrefixToAdjustValueTint = new Color32(128, 255, 128, 10);

		[Header("Background")]
		public Color Background = new Color32(194, 194, 194, 255);
		
		/// <summary>
		/// backgroud color of solid rectangle that is drawn behind selected elements (e.g. in the add component menu)
		/// </summary>
		[Tooltip("Backgroud color of solid rectangle that is drawn behind selected elements (e.g. in the add component menu)")]
		public Color BackgroundSelected = new Color32(62, 125, 231, 255);
		public Color BackgroundSelectedUnfocused = new Color32(143, 143, 143, 255);
		[FormerlySerializedAs("InspectorHeaderBackground")]
		public Color ComponentHeaderBackground = new Color32(203, 203, 203, 255);
		public Color ComponentMouseoveredHeaderBackground = new Color32(203, 203, 203, 255);

		[Header("Other")]
		public Color ButtonSelected = new Color32(0, 30, 138, 255);
		public Color ComponentSeparatorLine = new Color32(83, 83, 83, 255);
		
		public Color ToolbarSeparator = new Color(0f, 0f, 0f, 0.3f);

		public Color FilterHighlight = new Color(1f, 0.92f, 0.016f, 1f);
		public Color LockViewHighlight = new Color32(218, 49, 49, 255);

		public Color InvalidAction = new Color32(218, 49, 49, 255);
		public Color ToolbarItemSelected = new Color32(60, 118, 192, 255);
		public Color ToolbarItemSelectedUnfocused = new Color32(143, 143, 143, 255);

		public Color AssetHeaderBackground = new Color32(203, 203, 203, 255);

		public Color SplitViewDivider = Color.white;

		/// <summary>
		/// Syntax highlighting colors used when rendering code
		/// </summary>
		public SyntaxHiglighting SyntaxHighlight = new SyntaxHiglighting
		(
			stringColor:				new Color32(255,128,64,255),
			keywordColor:				new Color32(5,8,255,255),
			numberColor:				new Color32(210,105,30,255),
			typeColor:					new Color32(43,145,175,255),
			commentColor:				new Color32(87,166,74,255),
			preprocessorDirectiveColor: new Color32(154,154,154,255)
		);

		[SerializeField, HideInInspector]
		private int nameHash;

		public int NameHash
		{
			get
			{
				return nameHash;
			}
		}

		public bool NameHashEquals(int hashCode)
		{
			return nameHash == hashCode;
		}

		public void OnValidate()
		{
			nameHash = Name.GetHashCode();

			SyntaxHighlight.OnValidate();
		}

		public void SetBackgroundColors(Color newBackgroundColor, out BackgroundColors previousColors)
		{
			previousColors = new BackgroundColors(this);
			Background = newBackgroundColor;
			ComponentHeaderBackground = newBackgroundColor;
			AssetHeaderBackground = newBackgroundColor;
		}

		public void RestoreBackgroundColors(BackgroundColors backgroundColors)
		{
			Background = backgroundColors.background;
			ComponentHeaderBackground = backgroundColors.componentHeaderBackground;
			AssetHeaderBackground = backgroundColors.assetHeaderBackground;
		}

		public struct BackgroundColors
		{
			public readonly Color background;
			public readonly Color componentHeaderBackground;
			public readonly Color assetHeaderBackground;

			public BackgroundColors(GUIThemeColors colors)
			{
				background = colors.Background;
				componentHeaderBackground = colors.ComponentHeaderBackground;
				assetHeaderBackground = colors.AssetHeaderBackground;
			}
		}
	}
}