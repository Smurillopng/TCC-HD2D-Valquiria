#define DEBUG_DRAG_N_DROP_REFERENCES
#define DEBUG_SET_DRAG_N_DROP_VISUAL_MODE
//#define DEBUG_ADD_CURSOR_RECT
#define ENABLE_FOLDOUT_FIX

using System;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sisus
{
	public sealed class EditorGUIDrawer : DrawGUI
	{
		public const float GameObjectTitlebarHeightWithOneButtonRow
		#if UNITY_2022_2_OR_NEWER
			= 62f;
		#else
			= 52f;
		#endif
		public const float GameObjectTitlebarHeightWithTwoButtonRows = GameObjectTitlebarHeightWithOneButtonRow + 20f;
		public const float PrefabInstanceTitlebarHeight
		#if UNITY_2022_2_OR_NEWER
			= 114f;
		#else
			= GameObjectTitlebarHeightWithTwoButtonRows;
		#endif

		public const float AssetTitlebarHeightWithOneButtonRow = 45f;
		public const float AssetTitlebarHeightWithTwoButtonRows = AssetTitlebarHeightWithOneButtonRow + 30f;

		/// <summary>
		/// Event invoked before drawing the header for a component in Power Inspector.
		/// </summary>
		public static event Func<Object[], Rect, bool, float> BeforeComponentHeaderGUI;

		/// <summary>
		/// Event invoked after drawing the header for a component in Power Inspector.
		/// </summary>
		public static event Func<Object[], Rect, bool, float> AfterComponentHeaderGUI;

		private static Editor headerEditor;

		/// <summary>
		/// Foldouts inside custom property drawers will be drawn at incorrect positions when EditorGUIUtility.hierarchyMode is true
        /// unless EditorStyles.foldout.margin.left is set to the value -12. This property can be used to determine if this
		/// should be done before drawing custom editors and property drawers.
		/// </summary>
		public static bool EnableFoldoutFix
		{
			get
			{ 
				return true;
			}
		}

		/// <inheritdoc/>
		public override float InspectorTitlebarHeight
		{
			get
			{
				return 22f;
			}
		}

		public override DragAndDropVisualMode DragAndDropVisualMode
		{
			get
			{
				return DragAndDrop.visualMode;
			}

			set
			{
				#if DEV_MODE && DEBUG_SET_DRAG_N_DROP_VISUAL_MODE
				if(DragAndDrop.visualMode != value) { Debug.Log("DragAndDrop.visualMode = "+value); }
				#endif

				DragAndDrop.visualMode = value;
			}
		}
		
		/// <inheritdoc/>
		public override float AssetTitlebarHeight(bool toolbarHasTwoRowsOfButtons)
		{
			if(toolbarHasTwoRowsOfButtons)
			{
				return AssetTitlebarHeightWithTwoButtonRows;
			}
			return AssetTitlebarHeightWithOneButtonRow;
		}

		/// <summary>
		/// NOTE: Used by Hierarchy Folders - don't change the public API.
		/// </summary>
		/// <param name="isPrefab"> Is the GameObject a prefab asset? </param>
		/// <param name="isPrefabInstance"> Is the GameObject a prefab instance? </param>
		/// <returns> Height in pixels. </returns>
		public static float GameObjectTitlebarHeight(bool isPrefab = false, bool isPrefabInstance = false)
		{
			if(isPrefab)
			{
				if(AddressablesUtility.IsInstalled)
				{
					return GameObjectTitlebarHeightWithTwoButtonRows + GameObjectHeaderDrawer.OpenInPrefabModeButtonHeight;
				}

				return GameObjectTitlebarHeightWithOneButtonRow + GameObjectHeaderDrawer.OpenInPrefabModeButtonHeight;
			}

			if(isPrefabInstance)
			{
				return PrefabInstanceTitlebarHeight;
			}

			return GameObjectTitlebarHeightWithOneButtonRow;
		}

		public override void AssetHeader(Rect position, Object target, GUIContent label)
		{
			GUI.Label(position, GUIContent.none, InspectorPreferences.Styles.GameObjectHeaderBackground);

			var thumbnailRect = position;
			thumbnailRect.x += 6f;
			thumbnailRect.y += 6f;
			thumbnailRect.width = 32f;
			thumbnailRect.height = 32f;

			Texture2D preview;
			if(target != null)
			{
				preview = AssetPreview.GetAssetPreview(target);
			}
			else
			{
				preview = AssetPreview.GetMiniTypeThumbnail(Types.UnityObject);
			}

			GUI.Label(thumbnailRect, preview, InspectorPreferences.Styles.Centered);

			var labelRect = position;
			labelRect.x += 44f;
			labelRect.width -= 44f;
			GUI.Label(labelRect, label, InspectorPreferences.Styles.LargeLabel);
		}

		public override bool ComponentHeader(Rect position, bool unfolded, Object[] targets, bool expandable, HeaderPart selectedPart, HeaderPart mouseoverPart)
		{
			if(position.Contains(Event.current.mousePosition) && ((Event.current.type == EventType.MouseDown && Event.current.button == 1) || Event.current.type == EventType.ContextClick))
			{
				#if DEV_MODE
				Debug.Log("Detected ContextClick event for InspectorTitlebar. Exiting GUI to prevent context menus opening, since Power Inspector uses MouseDown event for this instead.\nEvent.current=" + StringUtils.ToString(Event.current));
				#endif

				ExitGUIUtility.ExitGUI();
				return unfolded;
			}

			position.height = InspectorTitlebarHeight;

			BeforeComponentHeaderGUI?.Invoke(targets, position, selectedPart != HeaderPart.None);

			if(Event.current.type == EventType.MouseDown)
			{
				if(Cursor.CanRequestLocalPosition && position.MouseIsOver())
				{
					if(Event.current.button == 0) 
					{
						if(mouseoverPart == HeaderPart.Base && targets.Length > 0)
						{
							DragAndDrop.PrepareStartDrag();
							DragAndDrop.objectReferences = targets;
						}
					}
					else if(Event.current.button == 2 && targets.Length > 0)
					{
						UseEvent();

						MonoBehaviour monoBehaviour;
						if(targets.Length == 1 && targets[0] != null && (monoBehaviour = targets[0] as MonoBehaviour) != null && Selection.activeGameObject == monoBehaviour.gameObject)
						{
							GUI.changed = true;
							PingObject(MonoScript.FromMonoBehaviour(monoBehaviour));
						}
						else
						{
							Ping(targets);
						}
					}
				}
			}

			bool toggleColorAltered = false;
			bool textColorAltered = false;
			
			var inspectorManager = InspectorUtility.ActiveManager;
			if(!inspectorManager.MouseDownInfo.IsDrag())
			{
				switch(mouseoverPart)
				{
					case HeaderPart.EnabledFlag:
						toggleColorAltered = true;
						GUIStyleUtility.SetInspectorTitlebarToggleColor(InspectorUtility.Preferences.theme.ControlMouseoveredTint);
						break;
					case HeaderPart.Base:
						var settings = InspectorUtility.Preferences;
						if(settings.mouseoverEffects.unityObjectHeaderTint)
						{
							textColorAltered = true;
							GUIStyleUtility.SetInspectorTitlebarTextColor(settings.theme.PrefixMouseoveredText);
						}
						break;
				}
			}
			
			switch(selectedPart)
			{
				case HeaderPart.EnabledFlag:
					toggleColorAltered = true;
					var theme = InspectorUtility.Preferences.theme;
					var color = InspectorUtility.ActiveInspectorDrawer.HasFocus ? theme.ControlSelectedTint : theme.ControlSelectedUnfocusedTint;
					GUIStyleUtility.SetInspectorTitlebarToggleColor(color);
					break;
				case HeaderPart.Base:
					textColorAltered = true;
					theme = InspectorUtility.Preferences.theme;
					color = InspectorUtility.ActiveInspectorDrawer.HasFocus ? theme.PrefixSelectedText : theme.PrefixSelectedUnfocusedText;
					GUIStyleUtility.SetInspectorTitlebarTextColor(color);
					break;
			}
			
			if(targets.Length > 0)
			{
				unfolded = EditorGUI.InspectorTitlebar(position, unfolded, targets, expandable);
			}

			if(toggleColorAltered)
			{
				GUIStyleUtility.ResetInspectorTitlebarToggleColor();
			}

			if(textColorAltered)
			{
				GUIStyleUtility.ResetInspectorTitlebarTextColor();
			}

			return unfolded;
		}

		internal static float InvokeAfterComponentHeaderGUI(Rect position, Object[] targets, HeaderPart selectedPart)
		{
			if(AfterComponentHeaderGUI is null)
			{
				return 0f;
			}

			PrefixLabelWidth = Screen.width * 0.3f;
			return AfterComponentHeaderGUI.Invoke(targets, position, selectedPart != HeaderPart.None);
		}

		public override bool InspectorTitlebar(Rect position, bool unfolded, GUIContent label, bool expandable, HeaderPart selectedPart, HeaderPart mouseoverPart)
		{
			if(position.Contains(Event.current.mousePosition) && ((Event.current.type == EventType.MouseDown && Event.current.button == 1) || Event.current.type == EventType.ContextClick))
			{
				#if DEV_MODE
				Debug.Log("Detected ContextClick event for InspectorTitlebar. Exiting GUI to prevent context menus opening, since Power Inspector uses MouseDown event for this instead.\nEvent.current=" + StringUtils.ToString(Event.current));
				#endif

				ExitGUIUtility.ExitGUI();
				return unfolded;
			}

			Color color;
			var mouseoveredColorWas = Color.white;
			GUIStyle mouseoveredStyle = null;
			var inspectorManager = InspectorUtility.ActiveManager;
			if(!inspectorManager.MouseDownInfo.IsDrag())
			{
				switch(mouseoverPart)
				{
					case HeaderPart.Base:
						mouseoveredStyle = InspectorPreferences.Styles.TitleText;
						mouseoveredColorWas = mouseoveredStyle.normal.textColor;
						color = InspectorUtility.Preferences.PrefixMouseoveredTextColor;
						mouseoveredStyle.hover.textColor = color;
						mouseoveredStyle.normal.textColor = color;
						mouseoveredStyle.onHover.textColor = color;
						mouseoveredStyle.onNormal.textColor = color;
						break;
				}
			}

			var selectedColorWas = Color.white;
			GUIStyle selectedStyle = null;
			switch(selectedPart)
			{
				case HeaderPart.Base:
					selectedStyle = InspectorPreferences.Styles.TitleText;
					selectedColorWas = selectedStyle.normal.textColor;

					var inspector = InspectorUtility.ActiveInspector;
					if(inspector == null || inspector.InspectorDrawer.HasFocus)
					{
						color = InspectorUtility.Preferences.theme.PrefixSelectedText;
					}
					else
					{
						color = inspector.Preferences.theme.PrefixSelectedUnfocusedText;
					}
					
					selectedStyle.hover.textColor = color;
					selectedStyle.normal.textColor = color;
					selectedStyle.onHover.textColor = color;
					selectedStyle.onNormal.textColor = color;
					break;
			}

			GUI.Label(position, GUIContent.none, InspectorPreferences.Styles.GameObjectHeaderBackground);

			if(expandable)
			{
				unfolded = EditorGUI.Foldout(position, unfolded, GUIContent.none);
			}

			var iconPos = position;
			iconPos.x = 10f;
			iconPos.y += 4f;
			iconPos.width = 30f;
			iconPos.height = 30f;
			GUI.DrawTexture(iconPos, InspectorUtility.Preferences.graphics.CSharpScriptIcon);

			position.x += 45f;
			position.width -= 45f;
			position.y += 4f;
			GUI.Label(position, label, InspectorPreferences.Styles.LargeLabel);

			if(mouseoveredStyle != null)
			{
				mouseoveredStyle.hover.textColor = mouseoveredColorWas;
				mouseoveredStyle.normal.textColor = mouseoveredColorWas;
				mouseoveredStyle.onHover.textColor = mouseoveredColorWas;
				mouseoveredStyle.onNormal.textColor = mouseoveredColorWas;
			}
			if(selectedStyle != null && selectedStyle != mouseoveredStyle)
			{
				selectedStyle.hover.textColor = selectedColorWas;
				selectedStyle.normal.textColor = selectedColorWas;
				selectedStyle.onHover.textColor = selectedColorWas;
				selectedStyle.onNormal.textColor = selectedColorWas;
			}

			return unfolded;
		}

		public override bool Foldout(Rect position, GUIContent label, bool unfolded, bool selected, bool mouseovered, bool unappliedChanges, Rect? highlightRect = null)
		{
			GUIStyle styleFoldout;
			if(selected)
			{
				styleFoldout = mouseovered ? foldoutStyleSelectedMouseovered : foldoutStyleSelected;
			}
			else
			{
				styleFoldout = mouseovered ? foldoutStyleMouseovered : foldoutStyle;
			}

			if(unappliedChanges)
			{
				styleFoldout.fontStyle = FontStyle.Bold;
			}
			
			unfolded = Foldout(position, label, unfolded, styleFoldout, highlightRect);

			styleFoldout.fontStyle = FontStyle.Normal;
			
			return unfolded;
		}

		/// <summary>
		/// NOTE: always returns false currently! Mouse click should be detected separately!
		/// </summary>
		public override bool Foldout(Rect position, GUIContent label, bool unfolded, GUIStyle guiStyle, Rect? highlightRect = null)
		{
			int indentWas = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;
			{
				//draw a rect behind the Foldout, so that if it's drawn on top of something
				//it won't clip. This is needed e.g. when drawing on top of the vertical splitter / resizer control.
				if(drawBackgroundBehindFoldouts)
				{
					ColorRect(position, InspectorBackgroundColor);
				}

				if(highlightRect.HasValue)
				{
					ColorRect(highlightRect.Value, InspectorUtility.Preferences.theme.FilterHighlight);
				}

				if(EnableFoldoutFix)
				{
					// ad-hoc fix for weird issue with foldout arrow positioning when addressables package is installed...
					const float FoldoutArrowSize = 12f;
					position.x += FoldoutArrowSize;
					position.width -= FoldoutArrowSize;
				}

				if(InspectorUtility.Preferences.enableTooltipIcons)
				{
					CachedLabel.text = label.text;
					
					EditorGUI.Foldout(position, unfolded, CachedLabel, true, guiStyle);
				}
				else
				{
					EditorGUI.Foldout(position, unfolded, label, true, guiStyle);
				}
			}
			EditorGUI.indentLevel = indentWas;

			return unfolded;
		}

		public override bool Toggle(Rect position, bool value)
		{
			int indentWas = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;
			{
				AddCursorRect(position, MouseCursor.Link);
				value = EditorGUI.Toggle(position, value);
			}
			EditorGUI.indentLevel = indentWas;
			return value;
		}

		public override void GameObjectHeader(Rect position, GameObject target)
		{
			if(position.Contains(Event.current.mousePosition) && ((Event.current.type == EventType.MouseDown && Event.current.button == 1) || Event.current.type == EventType.ContextClick))
			{
				#if DEV_MODE
				Debug.Log("Detected ContextClick event for GameObjectHeader. Exiting GUI to prevent context menus opening, since Power Inspector uses MouseDown event for this instead.\nEvent.current=" + StringUtils.ToString(Event.current));
				#endif

				ExitGUIUtility.ExitGUI();
				return;
			}

			GameObjectHeader(position, target, null);
		}
		
		public void GameObjectHeader(Rect position, GameObject target, Type editorType)
		{
			if(position.Contains(Event.current.mousePosition) && ((Event.current.type == EventType.MouseDown && Event.current.button == 1) || Event.current.type == EventType.ContextClick))
			{
				#if DEV_MODE
				Debug.Log("Detected ContextClick event for GameObjectHeader. Exiting GUI to prevent context menus opening, since Power Inspector uses MouseDown event for this instead.\nEvent.current=" + StringUtils.ToString(Event.current));
				#endif

				ExitGUIUtility.ExitGUI();
				return;
			}

			Editors.GetEditor(ref headerEditor, target, editorType);
			AssetHeader(position, headerEditor);
		}

		/// <summary>
		/// Draws inspector header for asset type target.
		/// </summary>
		/// <param name="position"> Position and dimensions for Rect where to draw header. </param>
		/// <param name="editor"></param>
		/// <returns>
		/// Dimensions of header drawn inside position Rect. The height can be different from height of Rect.
		/// </returns>
		public static Rect AssetHeader(Rect position, [NotNull]Editor editor, ref bool heightUndetermined)
		{
			if(position.Contains(Event.current.mousePosition) && ((Event.current.type == EventType.MouseDown && Event.current.button == 1) || Event.current.type == EventType.ContextClick))
			{
				#if DEV_MODE
				Debug.Log("Detected ContextClick event for InspectorHeader. Exiting GUI to prevent context menus opening, since Power Inspector uses MouseDown event for this instead.\nEvent.current=" + StringUtils.ToString(Event.current));
				#endif

				ExitGUIUtility.ExitGUI();
				return position;
			}

			// Editor.DrawHeader seems to alter EditorGUIUtility.labelWidth
			// so we need to restore it afterards to the value it had
			float labelWidthWas = PrefixLabelWidth;
			
			var actualDrawnRect = position;

			GUILayout.BeginArea(position);
			{
				int indentWas = EditorGUI.indentLevel;
				EditorGUI.indentLevel = 0;
				if(Event.current.type != EventType.Ignore)
				{
					try
					{
						editor.DrawHeader();

						if(!heightUndetermined)
						{
							if(Event.current.type == EventType.Repaint)
							{
								heightUndetermined = true;

								Rect lastRect;
								if(LastRectUtility.TryGetLastRect(out lastRect) && lastRect.height > 1f)
								{
									actualDrawnRect = position;
									actualDrawnRect.height = lastRect.yMax - position.y;
								}
							}
							else
                            {
								editor.Repaint();
                            }
						}
					}
					catch(Exception e)
					{
						if(ExitGUIUtility.ShouldRethrowException(e))
						{
							throw;
						}
						#if DEV_MODE
						if(e.InnerException == e) { Debug.LogError(e.Message); }
						else { Debug.LogError(e); }
						#endif

						actualDrawnRect = position;
					}
				}
				EditorGUI.indentLevel = indentWas;
			}
			GUILayout.EndArea();
			
			PrefixLabelWidth = labelWidthWas;

			return actualDrawnRect;
		}

		public override void AssetHeader(Rect position, Object target)
		{
			if(position.Contains(Event.current.mousePosition) && ((Event.current.type == EventType.MouseDown && Event.current.button == 1) || Event.current.type == EventType.ContextClick))
			{
				#if DEV_MODE
				Debug.Log("Detected ContextClick event for AssetHeader. Exiting GUI to prevent context menus opening, since Power Inspector uses MouseDown event for this instead.\nEvent.current=" + StringUtils.ToString(Event.current));
				#endif

				ExitGUIUtility.ExitGUI();
				return;
			}

			AssetHeader(position, target, null);
		}
		
		public void AssetHeader(Rect position, Object target, Type editorType)
		{
			if(position.Contains(Event.current.mousePosition) && ((Event.current.type == EventType.MouseDown && Event.current.button == 1) || Event.current.type == EventType.ContextClick))
			{
				#if DEV_MODE
				Debug.Log("Detected ContextClick event for AssetHeader. Exiting GUI to prevent context menus opening, since Power Inspector uses MouseDown event for this instead.\nEvent.current=" + StringUtils.ToString(Event.current));
				#endif

				ExitGUIUtility.ExitGUI();
				return;
			}
			
			Editors.GetEditor(ref headerEditor, target, editorType);
			AssetHeader(position, headerEditor);
		}

		public override AnimationCurve CurveField(Rect position, AnimationCurve value)
		{
			int indentWas = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;
			{
				value = EditorGUI.CurveField(position, value);
			}
			EditorGUI.indentLevel = indentWas;
			return value;
		}
		
		public override Gradient GradientField(Rect position, Gradient value)
		{
			int indentWas = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;
			value = EditorGUI.GradientField(position, value);
			EditorGUI.indentLevel = indentWas;
			return value;
		}

		public override Color ColorField(Rect position, Color value)
		{
			int indentWas = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;
			value = EditorGUI.ColorField(position, value);
			EditorGUI.indentLevel = indentWas;
			return value;
		}

		public override bool MouseDownButton(Rect position, GUIContent label)
		{
			return Runtime.MouseDownButton(position, label);
		}

		public override bool MouseDownButton(Rect position, GUIContent label, GUIStyle guiStyle)
		{
			return Runtime.MouseDownButton(position, label, guiStyle);
		}
		
		public override void TypePopup(Rect position, TypeDrawer popupField)
		{
			int indentWas = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;
			{
				popupField.DrawControlVisuals(position, popupField.Value);
			}
			EditorGUI.indentLevel = indentWas;
		}

		public override void EnumPopup(Rect position, EnumDrawer popupField)
		{
			int indentWas = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;
			{
				position.y += 1f;
				var value = EditorGUI.EnumPopup(position, popupField.Value);
				if(!value.Equals(popupField.Value) && !popupField.ReadOnly)
				{
					popupField.Value = value;
				}
			}
			EditorGUI.indentLevel = indentWas;
		}
		
		public override void EnumFlagsPopup(Rect position, EnumDrawer popupField)
		{
			int indentWas = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;
			position.y += 1f;
			popupField.Value = EditorGUI.EnumFlagsField(position, popupField.Value);
			EditorGUI.indentLevel = indentWas;
		}

		public override LayerMask MaskPopup(Rect position, LayerMask value)
		{
			int indentWas = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;
			position.y += 1f;
			value = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(EditorGUI.MaskField(position, GUIContent.none, InternalEditorUtility.LayerMaskToConcatenatedLayersMask(value), InternalEditorUtility.layers));
			EditorGUI.indentLevel = indentWas;
			return value;
		}
		
		public override Rect PrefixLabel(Rect position, GUIContent label)
		{
			if(label.text.Length == 0)
			{
				return position;
			}

			Rect labelRect;
			Rect controlRect;
			position.GetLabelAndControlRects(out labelRect, out controlRect);
			InlinedPrefixLabel(labelRect, label, false, false);
			return controlRect;
		}

		public override void PrefixLabel(Rect position, GUIContent label, bool selected, bool unappliedChanges, out Rect labelRect, out Rect controlRect)
		{
			position.GetLabelAndControlRects(out labelRect, out controlRect);
			InlinedPrefixLabel(labelRect, label, selected, unappliedChanges);
		}

		public override Rect PrefixLabel(Rect position, GUIContent label, bool selected)
		{
			var theme = InspectorUtility.Preferences.theme;
			GUI.skin.label.normal.textColor = selected ? theme.PrefixSelectedText : theme.PrefixIdleText;
			GUI.skin.label.fontStyle = FontStyle.Normal;

			var remainingSpace = PrefixLabel(position, label);

			GUI.skin.label.normal.textColor = theme.PrefixIdleText;

			return remainingSpace;
		}

		/// <summary>
		/// Draws prefix label over the entire given area without any indentations
		/// </summary>
		public override void InlinedPrefixLabel(Rect position, GUIContent label, bool selected, bool unappliedChanges)
		{
			if(selected)
			{
				if(unappliedChanges)
				{
					InlinedSelectedModifiedPrefixLabel(position, label);
				}
				else
				{
					InlinedSelectedPrefixLabel(position, label);
				}
			}
			else if(unappliedChanges)
			{
				InlinedModifiedPrefixLabel(position, label);
			}
			else
			{
				InlinedPrefixLabel(position, label);
			}
		}
		
		//Draws prefix label without any indentations
		public override void InlinedPrefixLabel(Rect position, GUIContent label)
		{
			InlinedPrefixLabel(position, label, prefixLabel);
		}

		public void InlinedPrefixLabel(Rect position, GUIContent label, [NotNull]GUIStyle style)
		{
			if(Event.current.type != EventType.Repaint)
			{
				return;
			}

			int indentWas = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;
			{
				if(InspectorUtility.Preferences.enableTooltipIcons)
				{
					// Draw label directly using style to avoid tooltips,
					// since they are handled by the tooltip icon instead.
					style.Draw(position, label, false, false, false, false);
				}
				else
				{
					GUI.Label(position, label, style);
				}
			}

			EditorGUI.indentLevel = indentWas;
		}

		public override void InlinedPrefixLabel(Rect position, string label)
		{
			CachedLabel.text = label;
			InlinedPrefixLabel(position, CachedLabel, prefixLabel);
		}

		public override void InlinedSelectedPrefixLabel(Rect position, GUIContent label)
		{
			var inspector = InspectorUtility.ActiveInspector;
			bool inspectorDrawerHasFocus = inspector == null || inspector.InspectorDrawer.HasFocus;

			if(label.image != null)
			{
				var guiColorWas = GUI.color;				
				if(inspectorDrawerHasFocus)
				{
					GUI.color = InspectorUtility.Preferences.theme.ControlSelectedTint;
				}
				InlinedPrefixLabel(position, label, prefixLabel);
				GUI.color = guiColorWas;
			}
			else
			{
				InlinedPrefixLabel(position, label, inspectorDrawerHasFocus ? prefixLabelSelected : prefixLabelSelectedUnfocused);
			}
		}

		public override void InlinedMouseoveredPrefixLabel(Rect position, GUIContent label)
		{
			if(label.image != null)
			{
				var guiColorWas = GUI.color;
				GUI.color = InspectorUtility.Preferences.theme.ControlMouseoveredTint;
				InlinedPrefixLabel(position, label, prefixLabel);
				GUI.color = guiColorWas;
			}
			else
			{
				InlinedPrefixLabel(position, label, prefixLabelMouseovered);
			}
		}

		public override void InlinedModifiedPrefixLabel(Rect position, GUIContent label)
		{
			InlinedPrefixLabel(position, label, prefixLabelModified);
		}

		public override void InlinedSelectedModifiedPrefixLabel(Rect position, GUIContent label)
		{
			var inspector = InspectorUtility.ActiveInspector;
			bool inspectorDrawerHasFocus = inspector == null || inspector.InspectorDrawer.HasFocus;

			if(label.image != null)
			{
				var guiColorWas = GUI.color;

				if(inspectorDrawerHasFocus)
				{
					GUI.color = InspectorUtility.Preferences.theme.ControlSelectedTint;
				}

				InlinedPrefixLabel(position, label, prefixLabel);
				GUI.color = guiColorWas;
			}
			else
			{
				InlinedPrefixLabel(position, label, inspectorDrawerHasFocus ? prefixLabelSelectedModified : prefixLabelSelectedModifiedUnfocused);
			}
		}

		public override void InlinedMouseoveredModifiedPrefixLabel(Rect position, GUIContent label)
		{
			if(label.image != null)
			{
				var guiColorWas = GUI.color;
				GUI.color = InspectorUtility.Preferences.theme.ControlMouseoveredTint;
				InlinedPrefixLabel(position, label, prefixLabel);
				GUI.color = guiColorWas;
			}
			else
			{
				InlinedPrefixLabel(position, label, prefixLabelMouseoveredModified);
			}
		}


		/// <summary>
		/// if the label has a tooltip, draw a hint icon which shows the tooltip on mouseover
		/// </summary>
		public override void HandleHintIcon(Rect position, GUIContent label)
		{
			if(InspectorUtility.Preferences.enableTooltipIcons && label.tooltip.Length > 0)
			{
				var hintPos = position;
				hintPos.x -= SingleLineHeight;
				HintIcon(hintPos, label.tooltip);
			}
		}

		public override void HintIcon(Rect position, string text)
		{
			position.width = SingleLineHeight;
			position.height = SingleLineHeight;

			CachedLabel.text = "";
			CachedLabel.tooltip = text;
			CachedLabel.image = InspectorUtility.Preferences.graphics.tooltipIcon;
			
			GUI.Label(position, CachedLabel);
			CachedLabel.text = "";
			CachedLabel.tooltip = "";
			CachedLabel.image = null;
		}

		public override Rect PrefixLabel(Rect position, GUIContent label, GUIStyle guiStyle)
		{
			var pos = position;
			float indent = IndentLevel * IndentWidth; 
			pos.x = position.x + LeftPadding + indent;
			pos.height = SingleLineHeight;
			float prefixWidth = PrefixLabelWidth;
			pos.width = prefixWidth - LeftPadding - indent - MiddlePadding;
			GUI.Label(pos, label, guiStyle);
			pos = position;
			pos.x = position.x + prefixWidth + MiddlePadding;
			pos.width = position.width - prefixWidth - MiddlePadding - RightPadding;
			return pos;
		}

		public override int IntField(Rect position, int value)
		{
			int indentWas = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;
			{
				position.height = SingleLineHeight - 2f;
				position.y += 1f;
				
				bool editingTextFieldWas = EditorGUIUtility.editingTextField;

				value = EditorGUI.IntField(position, value);

				OnAfterTextFieldDrawn(editingTextFieldWas);
			}
			EditorGUI.indentLevel = indentWas;
			return value;
		}

		private void OnAfterTextFieldDrawn(bool editingTextFieldWas)
		{
			if(editingTextFieldWas != EditorGUIUtility.editingTextField)
			{
				if(Event.current.isMouse || Event.current.type == EventType.Used)
				{
					#if DEV_MODE
					Debug.Log("EditorGUIUtility.editingTextField changed to "+StringUtils.ToColorizedString(EditorGUIUtility.editingTextField)+ " during EditorGUI.TextField with Event="+StringUtils.ToString(Event.current)+", probably because field was clicked. Setting DrawGUI.EditingTextField to match it.");
					#endif

					EditingTextField = !editingTextFieldWas;
				}
				else
				{
					#if DEV_MODE
					Debug.Log("EditorGUIUtility.editingTextField changed to "+ StringUtils.ToColorizedString(EditorGUIUtility.editingTextField)+ " during EditorGUI.TextField with Event=" + StringUtils.ToString(Event.current) + " for some reason. Reverting back to original value.");
					#endif

					EditorGUIUtility.editingTextField = editingTextFieldWas;
				}
			}
		}

		public override int IntField(Rect position, int value, bool delayed)
		{
			int indentWas = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;
			{
				position.height = SingleLineHeight - 2f;
				position.y += 1f;
				
				bool editingTextFieldWas = EditorGUIUtility.editingTextField;

				value = delayed ? EditorGUI.DelayedIntField(position, value) : EditorGUI.IntField(position, value);

				OnAfterTextFieldDrawn(editingTextFieldWas);
			}
			EditorGUI.indentLevel = indentWas;
			return value;
		}

		public override int Slider(Rect position, int value, int min, int max)
		{
			return Runtime.Slider(position, value, min, max);
		}

		public override float Slider(Rect position, float value, float min, float max)
		{
			return GUI.HorizontalSlider(position, value, min, max);
		}

		public override float Slider<TValue>(Rect position, TValue value, float min, float max)
		{
			float result = (float)Convert.ChangeType(value, Types.Float);
			result = GUI.HorizontalSlider(position, result, min, max);
			return result;
		}

		public override float FloatField(Rect position, float value)
		{
			int indentWas = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;
			{
				position.height = SingleLineHeight - 2f;
				position.y += 1f;

				bool editingTextFieldWas = EditorGUIUtility.editingTextField;

				value = EditorGUI.FloatField(position, value);

				OnAfterTextFieldDrawn(editingTextFieldWas);
			}
			EditorGUI.indentLevel = indentWas;
			return value;
		}

		public override float FloatField(Rect position, float value, bool delayed)
		{
			int indentWas = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;
			{
				position.height = SingleLineHeight - 2f;
				position.y += 1f;

				bool editingTextFieldWas = EditorGUIUtility.editingTextField;

				value = delayed ? EditorGUI.DelayedFloatField(position, value) : EditorGUI.FloatField(position, value);

				OnAfterTextFieldDrawn(editingTextFieldWas);
			}
			EditorGUI.indentLevel = indentWas;
			return value;
		}

		public override double DoubleField(Rect position, double value)
		{
			int indentWas = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;
			{
				position.height = SingleLineHeight - 2f;
				position.y += 1f;

				bool editingTextFieldWas = EditorGUIUtility.editingTextField;

				value = EditorGUI.DoubleField(position, value);

				OnAfterTextFieldDrawn(editingTextFieldWas);
			}
			EditorGUI.indentLevel = indentWas;
			return value;
		}

		public override decimal DecimalField(Rect position, decimal value)
		{
			int indentWas = EditorGUI.indentLevel;
			string valueString = StringUtils.ToString(value);
			EditorGUI.indentLevel = 0;
			{
				position.height = SingleLineHeight - 2f;
				position.y += 1f;
				valueString = TextField(position, valueString);
			}
			EditorGUI.indentLevel = indentWas;
			try
			{
				return decimal.Parse(valueString);
			}
			catch
			{
				return value;
			}
		}

		public override short ShortField(Rect position, short value)
		{
			int indentWas = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;
			{
				position.height = SingleLineHeight - 2f;
				position.y += 1f;
				int valueInt = value;
				
				bool editingTextFieldWas = EditorGUIUtility.editingTextField;

				int setValue = EditorGUI.IntField(position, valueInt);

				OnAfterTextFieldDrawn(editingTextFieldWas);

				if(setValue != valueInt)
				{
					if(setValue <= short.MinValue)
					{
						value  = short.MinValue;
					}
					else if(setValue >= short.MaxValue)
					{
						value = short.MaxValue;
					}
					else
					{
						value = (short)setValue;
					}
				}
			}
			EditorGUI.indentLevel = indentWas;
			return value;
		}

		public override ushort UShortField(Rect position, ushort value)
		{
			int indentWas = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;
			{
				position.height = SingleLineHeight - 2f;
				position.y += 1f;
				int valueInt = value;

				bool editingTextFieldWas = EditorGUIUtility.editingTextField;

				int setValue = EditorGUI.IntField(position, valueInt);

				OnAfterTextFieldDrawn(editingTextFieldWas);

				if(setValue != valueInt)
				{
					if(setValue <= ushort.MinValue)
					{
						value  = ushort.MinValue;
					}
					else if(setValue >= ushort.MaxValue)
					{
						value = ushort.MaxValue;
					}
					else
					{
						value = (ushort)setValue;
					}
				}
			}
			EditorGUI.indentLevel = indentWas;
			return value;
		}

		public override string TextField(Rect position, string value)
		{
			int indentWas = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;
			{
				position.height = SingleLineHeight - 2f;
				position.y += 1f;
				
				bool editingTextFieldWas = EditorGUIUtility.editingTextField;

				value = EditorGUI.TextField(position, value);

				OnAfterTextFieldDrawn(editingTextFieldWas);
			}
			EditorGUI.indentLevel = indentWas;
			return value;
		}

		public override string TextField(Rect position, string value, GUIStyle guiStyle)
		{
			int indentWas = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;
			{
				position.height = SingleLineHeight - 2f;
				position.y += 1f;
				
				bool editingTextFieldWas = EditorGUIUtility.editingTextField;

				value = EditorGUI.TextField(position, value, guiStyle);

				if(editingTextFieldWas != EditorGUIUtility.editingTextField)
				{
					EditingTextField = !editingTextFieldWas;
				}
			}
			EditorGUI.indentLevel = indentWas;
			return value;
		}

		public override string TextField(Rect position, string value, bool delayed)
		{
			int indentWas = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;
			{
				position.height = SingleLineHeight - 2f;
				position.y += 1f;

				bool editingTextFieldWas = EditorGUIUtility.editingTextField;

				value = delayed ? EditorGUI.DelayedTextField(position, value) : EditorGUI.TextField(position, value);

				if(editingTextFieldWas != EditorGUIUtility.editingTextField)
				{
					EditingTextField = !editingTextFieldWas;
				}
			}
			EditorGUI.indentLevel = indentWas;
			return value;
		}

		public override string TextField(Rect position, GUIContent label, string value, GUIStyle guiStyle)
		{
			bool editingTextFieldWas = EditorGUIUtility.editingTextField;

			value = EditorGUI.TextField(position, label, value, guiStyle);

			if(editingTextFieldWas != EditorGUIUtility.editingTextField)
			{
				EditingTextField = !editingTextFieldWas;
			}

			return value;
		}

		public override string TextArea(Rect position, string value, bool wordWrapping)
		{
			position.height = position.height - 2f;
			position.y += 1f;

			if(position.x < 1f)
			{
				float indent = LeftPadding + IndentLevel * IndentWidth;
				position.x += indent;
				position.width -= indent + RightPadding;
			}

			int indentWas = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;

			bool editingTextFieldWas = EditorGUIUtility.editingTextField;

			var style = wordWrapping ? InspectorPreferences.Styles.WordWrappedTextArea : InspectorPreferences.Styles.TextArea;
			value = EditorGUI.TextArea(position, value, style);

			if(editingTextFieldWas != EditorGUIUtility.editingTextField)
			{
				EditingTextField = !editingTextFieldWas;
			}

			EditorGUI.indentLevel = indentWas;
			return value;
		}

		public override string TextArea(Rect position, string value, GUIStyle guiStyle)
		{
			position.height = position.height - 2f;
			position.y += 1f;
			
			if(position.x < 1f)
			{
				float indent = LeftPadding + IndentLevel * IndentWidth;
				position.x += indent;
				position.width -= indent + RightPadding;
			}

			int indentWas = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;
			
			bool editingTextFieldWas = EditorGUIUtility.editingTextField;

			value = EditorGUI.TextArea(position, value, guiStyle);

			if(editingTextFieldWas != EditorGUIUtility.editingTextField)
			{
				EditingTextField = !editingTextFieldWas;
			}

			EditorGUI.indentLevel = indentWas;
			return value;
		}

		public override void Label(Rect position, GUIContent label, string styleName)
		{
			int indentWas = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;
			{
				GUI.Label(position, label, InspectorUtility.Preferences.GetStyle(styleName));
			}
			EditorGUI.indentLevel = indentWas;
		}

		public override void Label(Rect position, GUIContent label, GUIStyle style)
		{
			int indentWas = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;
			{
				GUI.Label(position, label, style);
			}
			EditorGUI.indentLevel = indentWas;
		}

		public override void HelpBox(Rect position, string message, MessageType messageType)
		{
			float leftIndex = LeftPadding + IndentLevel * IndentWidth;
			position.x += leftIndex;
			position.width -= leftIndex + RightPadding;
			ColorRect(position, InspectorBackgroundColor);
			EditorGUI.HelpBox(position, message, MessageTypeToUnityMessageType(messageType));
		}

		private static UnityEditor.MessageType MessageTypeToUnityMessageType(MessageType messageType)
		{
			switch(messageType)
			{
				case MessageType.Info:
					return UnityEditor.MessageType.Info;
				case MessageType.Warning:
					return UnityEditor.MessageType.Warning;
				case MessageType.Error:
					return UnityEditor.MessageType.Error;
				default:
					return UnityEditor.MessageType.None;
			}
		}

		/// <inheritdoc/>
		public override Object[] DragAndDropObjectReferences
		{
			get
			{
				return DragAndDrop.objectReferences;
			}

			set
			{
				if(value != null)
				{
					if(value.Length > 0)
					{
						if(value.ContainsNullObjects())
						{
							#if DEV_MODE && PI_ASSERTATIONS
							var valueWas = value;
							#endif

							value = value.RemoveNullObjects();

							#if DEV_MODE && PI_ASSERTATIONS
							Debug.Assert(valueWas.Length == value.Length + 1, StringUtils.ToString(value));
							#endif
						}

						if(!value.ContentsMatch(DragAndDrop.objectReferences))
						{
							#if DEV_MODE && PI_ASSERTATIONS
							Debug.Assert(Event.current.type == EventType.MouseDown ||Event.current.type == EventType.MouseDrag, "Drags can only be started from MouseDown or MouseDrag events");
							#endif

							DragAndDrop.PrepareStartDrag();
							DragAndDrop.objectReferences = value;
							DragAndDrop.StartDrag("Drag");

							#if DEV_MODE && DEBUG_DRAG_N_DROP_REFERENCES
							Debug.Log("Started drag of: "+StringUtils.ToString(value)+" with MouseoveredInspector="+StringUtils.ToString(InspectorUtility.ActiveManager.MouseoveredInspector));
							#endif

							#if DEV_MODE && PI_ASSERTATIONS
							Debug.Assert(value.ContentsMatch(DragAndDrop.objectReferences), StringUtils.ToString("DragAndDrop.objectReferences was still "+StringUtils.ToString(DragAndDrop.objectReferences)+" after assigning value of "+StringUtils.ToString(value)));
							#endif

							SendOnDragAndDropObjectReferencesChangedEventIfNeeded(true);
							return;
						}
					}
				}
				else
				{
					value = ArrayPool<Object>.ZeroSizeArray;
				}

				if(DragAndDrop.objectReferences.Length > 0)
				{
					#if DEV_MODE && DEBUG_DRAG_N_DROP_REFERENCES
					Debug.Log("DragAndDropObjectReferences cleared (was: "+StringUtils.ToString(DragAndDrop.objectReferences)+") with Event="+StringUtils.ToString(Event.current));
					#endif

					DragAndDrop.objectReferences = value;
				}

				SendOnDragAndDropObjectReferencesChangedEventIfNeeded(true);
			}
		}

		public override void AcceptDrag()
		{
			#if DEV_MODE && DEBUG_DRAG_N_DROP_REFERENCES
			Debug.Log("AcceptDrag: "+StringUtils.ToString(DragAndDropObjectReferences)+" with Event="+StringUtils.ToString(Event.current)+", MouseoveredInspector="+StringUtils.ToString(InspectorUtility.ActiveManager.MouseoveredInspector));
			#endif
			DragAndDrop.AcceptDrag();
		}

		public override Object ObjectField(Rect pos, Object value, Type type, bool allowSceneObjects)
		{
			pos.height = SingleLineHeight - 2f;
			pos.y += 1f;
			int indentWas = EditorGUI.indentLevel;
			Object setValue;
			EditorGUI.indentLevel = 0;
			{
				setValue = EditorGUI.ObjectField(pos, value, type, allowSceneObjects);
			}
			EditorGUI.indentLevel = indentWas;
			
			return setValue;
		}

		public override void AddCursorRect(Rect position, MouseCursor cursor)
		{
			#if DEV_MODE && DEBUG_ADD_CURSOR_RECT
			Debug.Log("AddCursorRect("+ position+", "+cursor+")");
			#endif
			EditorGUIUtility.AddCursorRect(position, cursor);
		}

		public override void PingObject(Object target)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(target != null);
			#endif

			#if DEV_MODE && DEBUG_PING
			Debug.Log("PingObject("+StringUtils.ToString(target)+") with AssetPath="+AssetDatabase.GetAssetPath(target));
			#endif

			if(AssetDatabase.Contains(target))
			{
				EditorWindowExtensions.MakeAtLeastOneInstanceSelectedTab(Types.GetInternalEditorType("UnityEditor.ProjectBrowser"), EditorWindow.focusedWindow);

				// if Project window is not yet open, should we open it?
				//if(!...)
				//{
				//	ExecuteMenuItem("Window/General/Project");
				//}
			}
			else if(target.IsSceneObject())
			{
				EditorWindowExtensions.MakeAtLeastOneInstanceSelectedTab(Types.GetInternalEditorType("UnityEditor.SceneHierarchyWindow"), EditorWindow.focusedWindow);

				// if Hierarchy window is not yet open, should we open it?
				//if(!...)
				//{
				//	ExecuteMenuItem("Window/General/Hierarchy");
				//}
			}

			EditorGUIUtility.PingObject(target);
		}

		public override int DisplayDialog(string title, string message, string button1, string button2, string button3)
		{
			//fix issue where mouse up event might not ever get received
			if(InspectorUtility.ActiveManager != null)
			{
				InspectorUtility.ActiveManager.MouseDownInfo.Clear();
			}

			return EditorUtility.DisplayDialogComplex(title, message, button1, button2, button3);
		}

		public override bool DisplayDialog(string title, string message, string ok, string cancel)
		{
			//fix issue where mouse up event might not ever get received
			if(InspectorUtility.ActiveManager != null)
			{
				InspectorUtility.ActiveManager.MouseDownInfo.Clear();
			}

			return EditorUtility.DisplayDialog(title, message, ok, cancel);
		}

		public override void DisplayDialog(string title, string message, string ok)
		{
			//fix issue where mouse up event might not ever get received
			if(InspectorUtility.ActiveManager != null)
			{
				InspectorUtility.ActiveManager.MouseDownInfo.Clear();
			}

			EditorUtility.DisplayDialog(title, message, ok);
		}

		public override string ToString()
		{
			return "EditorGUIDrawer";
		}
	}
}