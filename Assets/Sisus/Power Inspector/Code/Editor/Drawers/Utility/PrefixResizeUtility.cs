#define DEBUG_DISPLAY_PREFIX_WIDTH_WHEN_RESIZING

using JetBrains.Annotations;
using UnityEngine;
using UnityEditor;

namespace Sisus
{
	public delegate void PrefixResized([NotNull]IUnityObjectDrawer subject, float newPrefixWidth);

	public class PrefixResizeUtility
	{
		public const float TopOnlyPrefixResizerHeight = 7f;
		private const float TopOnlyPrefixResizerWidth = 9f;

		public static PrefixResized OnPrefixResizingFinished;

		private static Vector2 resizeMouseDownPos;
		private static float resizeMouseDownWidth;

		private static IUnityObjectDrawer nowResizing;

		public static IUnityObjectDrawer NowResizing
		{
			get
			{
				return nowResizing;
			}

			set
			{
				if(nowResizing != null && value == null && OnPrefixResizingFinished != null)
				{
					OnPrefixResizingFinished(nowResizing, nowResizing.PrefixLabelWidth);
				}
				nowResizing = value;
			}
		}

		/// <summary> Gets interactive bounds for the target's prefix resizer. </summary>
		/// <param name="target"> Target with the prefix resizer. </param>
		/// <param name="maxHeight"> Maxiumum height for the vertically drawn prefix resizer. </param>
		/// <returns> The bounds for the prefix resizer. </returns>
		public static Rect GetPrefixResizerBounds(IUnityObjectDrawer target, float maxHeight = 0f)
		{
			switch(target.PrefixResizer)
			{
					case PrefixResizer.Disabled:
						return default(Rect);
					case PrefixResizer.TopOnly:
						return GetTopResizerRect(target);
					default:
						var targetBounds = target.Bounds;
						var prefixWidth = target.PrefixLabelWidth;
						var verticalRect = targetBounds;
						verticalRect.x = prefixWidth - 2f;
						var headerHeight = target.HeaderHeight;
						verticalRect.y += headerHeight + 5f;
						verticalRect.width = 4f;
						verticalRect.height = targetBounds.height - headerHeight - 10f;
						if(maxHeight > 0f && verticalRect.height > maxHeight)
						{
							verticalRect.height = maxHeight;
						}
						return verticalRect;
			}
		}

		/// <summary> Handles the resizing. </summary>
		/// <param name="target"> Target whose prefix label resizing should be handled. </param>
		/// <param name="prefixResizerMouseovered"> [out] True if prefix resizer is currently mouseovered. </param>
		/// <param name="maxHeight"> (Optional) The maximum height. </param>
		/// <param name="minPrefixLabelWidth"> (Optional) Minimum width for the prefix label when resizing. Some Editors like RectTransform might require a larger min width than the default. </param>
		/// <param name="fillBackground"> (Optional) True to fill area behind the resizer with background color, preventing clipping with any elements drawn behidn the resizer. </param>
		/// <returns> New prefix size. </returns>
		public static float HandleResizing(IUnityObjectDrawer target, out bool prefixResizerMouseovered, float maxHeight, float minPrefixLabelWidth, float maxPrefixLabelWidth)
		{
			bool drawVerticalSplitter = target.PrefixResizer == PrefixResizer.Vertical;
			bool nowResizingThis = nowResizing == target;

			#if DEV_MODE
			// make it easier to accurately find optimal prefix width for CustomEditorBaseDrawer
			// by drawing the vertical splitter
			if(nowResizingThis)
			{
				drawVerticalSplitter = true;
			}
			#endif

			var targetBounds = target.Bounds;
			var prefixWidthWas = target.PrefixLabelWidth;
			var prefixWidth = prefixWidthWas;

			var verticalRect = targetBounds;
			{
				verticalRect.x = prefixWidth - 1f;
				var headerHeight = target.HeaderHeight;
				verticalRect.y += headerHeight + 5f;
				verticalRect.width = 2f;
				verticalRect.height = targetBounds.height - headerHeight - 10f;
				if(maxHeight > 0f && verticalRect.height > maxHeight)
				{
					verticalRect.height = maxHeight;
				}
			}

			var inspector = target.Inspector;
			var preferences = inspector.Preferences;
			var backgroundColor = preferences.theme.Background;

			bool drawTopSplitter;
			Rect topResizerPos;
			if(drawVerticalSplitter)
			{
				drawTopSplitter = false;
				topResizerPos = default(Rect);

				var bgRect = verticalRect;

				//some clipping over the controls would occur without this fix on CustomEditors.
				//even now there's very slight clipping over sliders and text fields,
				//so it's not a perfect approach
				float clippingFix = 1.5f;
				bgRect.width = DrawGUI.MiddlePadding + DrawGUI.MiddlePadding - clippingFix - clippingFix;
				bgRect.x = prefixWidth - DrawGUI.MiddlePadding + clippingFix;

				DrawGUI.Active.ColorRect(bgRect, backgroundColor);

				GUI.DrawTexture(verticalRect, preferences.graphics.splitterBg, ScaleMode.StretchToFill);

				verticalRect.width += 2f;
				verticalRect.x -= 1f;

				prefixResizerMouseovered = verticalRect.MouseIsOver();
			}
			else if(target.PrefixResizer == PrefixResizer.TopOnly)
			{
				drawTopSplitter = true;
				topResizerPos = GetTopResizerRect(target);
				prefixResizerMouseovered = topResizerPos.MouseIsOver();
			}
			else
			{
				drawTopSplitter = false;
				topResizerPos = default(Rect);
				prefixResizerMouseovered = false;
			}

			var manager = inspector.Manager;

			// Resising should not be allowed over certain controls that extend over
			// the full width of the inspector (like the UnityEvent drawer).
			if(nowResizing != null || manager.MouseoveredSelectable == null || (manager.MouseoveredInspector == target.Inspector && manager.MouseoveredSelectable.PrefixResizingEnabledOverControl))
			{
				var e = Event.current;
				var eventType = manager.MouseDownInfo.GetEventTypeForMouseUpDetection(e);

				if(nowResizingThis)
				{
					DrawGUI.Active.SetCursor(MouseCursor.ResizeHorizontal);
					prefixResizerMouseovered = true;
				}
				else if(prefixResizerMouseovered)
				{
					if(drawVerticalSplitter)
					{
						DrawGUI.Active.AddCursorRect(verticalRect, MouseCursor.ResizeHorizontal);
					}
					else if(drawTopSplitter)
					{
						DrawGUI.Active.AddCursorRect(topResizerPos, MouseCursor.ResizeHorizontal);
					}

					if(eventType == EventType.MouseDown)
					{
						target.Select(ReasonSelectionChanged.ControlClicked);

						KeyboardControlUtility.JustClickedControl = 0;
						KeyboardControlUtility.KeyboardControl = 0;
					
						if(e.clickCount == 2)
						{
							var type = target.Type;
							if(type != null)
							{
								inspector.InspectorDrawer.PrefixColumnWidths.Clear(type);
							}

							prefixWidth = target.GetOptimalPrefixLabelWidth(0, true);

							#if DEV_MODE
							Debug.Log(target.ToString() + ".HandleResizing() was double clicked, prefixWidth was set to: " + prefixWidth);
							#endif
						}

						NowResizing = target;
						resizeMouseDownPos = e.mousePosition;
						resizeMouseDownWidth = prefixWidth;

						DrawGUI.UseEvent();
					}
				}

				if(nowResizingThis)
				{
					if(eventType == EventType.MouseUp)
					{
						NowResizing = null;
						DrawGUI.UseEvent();
					}
					else if(eventType == EventType.MouseDrag)
					{
						DrawGUI.UseEvent();
						prefixWidth = resizeMouseDownWidth + (e.mousePosition.x - resizeMouseDownPos.x);
						GUI.changed = true; //trigger a repaint for a more responsive drag handle
					}

					#if UNITY_EDITOR && DEV_MODE && DEBUG_DISPLAY_PREFIX_WIDTH_WHEN_RESIZING
					if(nowResizing != null)
					{
						var debugRect = target.ClickToSelectArea;
						debugRect.x = prefixWidth + 10f;
						debugRect.y += debugRect.height;
						debugRect.width = 30f;
						debugRect.height = DrawGUI.SingleLineHeight;
						if(drawVerticalSplitter)
						{
							InspectorUtility.SetActiveTooltip(target.Inspector, debugRect, StringUtils.ToString(Mathf.RoundToInt(prefixWidth)));
						}
						else
						{
							GUI.Label(debugRect, StringUtils.ToString(Mathf.RoundToInt(prefixWidth)), "MiniLabel");
						}
					}
					#endif
				}
				else if(nowResizing != null)
				{
					if(nowResizing.Inactive || !manager.MouseDownInfo.MouseButtonIsDown)
					{
						#if DEV_MODE
						Debug.LogWarning("PrefixResizeUtility force stopping resizing prefix of "+nowResizing+" with  nowResizing.Inactive="+nowResizing.Inactive+", manager.MouseDownInfo.MouseButtonIsDown="+manager.MouseDownInfo.MouseButtonIsDown);
						#endif
						NowResizing = null;
					}
				}
			}

			prefixWidth = Mathf.Clamp(prefixWidth, minPrefixLabelWidth, maxPrefixLabelWidth);
			DrawGUI.PrefixLabelWidth = prefixWidth;

			if(prefixWidthWas != prefixWidth)
			{
				var type = target.Type;
				if(type != null)
				{
					inspector.InspectorDrawer.PrefixColumnWidths.Save(type, prefixWidth);
				}
			}

			if(drawTopSplitter)
			{
				if(target.Unfoldedness >= 0.5f)
				{
					//middle
					DrawGUI.Active.ColorRect(topResizerPos, backgroundColor);
					GUI.DrawTexture(topResizerPos, InspectorUtility.Preferences.graphics.prefixColumnResizeHandle);

					//left
					topResizerPos.x = DrawGUI.LeftPadding;
					topResizerPos.height = 2f;
					topResizerPos.width = prefixWidth - DrawGUI.LeftPadding - 5f;
					if(topResizerPos.width > 0f)
					{
						GUI.DrawTexture(topResizerPos, InspectorUtility.Preferences.graphics.prefixColumnResizeTrackLeft);
					}

					//right
					topResizerPos.x += topResizerPos.width + TopOnlyPrefixResizerWidth;
					topResizerPos.width = target.Bounds.width - prefixWidth + 4f - DrawGUI.RightPadding;
					GUI.DrawTexture(topResizerPos, InspectorUtility.Preferences.graphics.prefixColumnResizeTrackRight);
				}
				// the top resizer middle part starts looking a ugly when it's scaled to be only a few pixels in height, so don't draw it at those scales.
				else
				{
					//left
					topResizerPos.x = DrawGUI.LeftPadding;
					topResizerPos.height = 2f;
					topResizerPos.width = prefixWidth - DrawGUI.LeftPadding;
					if(topResizerPos.width > 0f)
					{
						GUI.DrawTexture(topResizerPos, InspectorUtility.Preferences.graphics.prefixColumnResizeTrackLeft);
					}

					//right
					topResizerPos.x += topResizerPos.width;
					topResizerPos.width = target.Bounds.width - prefixWidth - DrawGUI.RightPadding;
					GUI.DrawTexture(topResizerPos, InspectorUtility.Preferences.graphics.prefixColumnResizeTrackRight);
				}
			}

			return prefixWidth;
		}
		
		public static PrefixResizer GetPrefixResizerType(IUnityObjectDrawer target, bool usesEditorForDrawingBody)
		{
			var visibleMembers = target.VisibleMembers;
			int count = visibleMembers.Length;
			if(count == 0 && !usesEditorForDrawingBody)
			{
				return PrefixResizer.Disabled;
			}

			switch(usesEditorForDrawingBody ? target.Inspector.Preferences.prefixResizerForEditors : target.Inspector.Preferences.prefixResizerForEditorless)
			{
				case PrefixResizerPositioning.Disabled:
					return PrefixResizer.Disabled;
				case PrefixResizerPositioning.AlwaysTopOnly:
					return PrefixResizer.TopOnly;
				case PrefixResizerPositioning.AlwaysVertical:
					return PrefixResizer.Vertical;
			}
			
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert((usesEditorForDrawingBody ? target.Inspector.Preferences.prefixResizerForEditors : target.Inspector.Preferences.prefixResizerForEditorless) == PrefixResizerPositioning.Dynamic);
			#endif

			for(int n = visibleMembers.Length - 1; n >= 0; n--)
			{
				if(visibleMembers[n].PrefixResizingEnabledOverControl)
				{
					return PrefixResizer.Vertical;
				}
			}
			return PrefixResizer.TopOnly;
		}
		
		#if UNITY_EDITOR
		public static void ApplyPrefixLabelWidthToEditorGUIUtility(float drawGUIPrefixLabelWidth)
		{
			UnityEditor.EditorGUIUtility.labelWidth = LabelWidthFromDrawGUIToEditorGUIUtility(drawGUIPrefixLabelWidth);
		}

		public static float GetPrefixLabelWidthFromEditorGUIUtility()
		{
			return LabelWidthFromEditorGUIUtilityToDrawGUI(UnityEditor.EditorGUIUtility.labelWidth);
		}

		public static float LabelWidthFromDrawGUIToEditorGUIUtility(float drawGUIPrefixLabelWidth)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(drawGUIPrefixLabelWidth >= DrawGUI.MiddlePadding, drawGUIPrefixLabelWidth);
			#endif

			return Mathf.Max(drawGUIPrefixLabelWidth - DrawGUI.MiddlePadding, 1f);
		}

		public static float LabelWidthFromEditorGUIUtilityToDrawGUI(float editorGUIUtilityLabelWidth)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(editorGUIUtilityLabelWidth >= 0f);
			#endif

			return Mathf.Max(editorGUIUtilityLabelWidth + DrawGUI.MiddlePadding, 1f);
		}
		#endif

		private static Rect GetTopResizerRect(IUnityObjectDrawer target)
		{
			var resizerPos = target.Bounds;
			resizerPos.y += target.HeaderHeight;
			resizerPos.height = TopOnlyPrefixResizerHeight * target.Unfoldedness;
			resizerPos.x = target.PrefixLabelWidth - 5f;
			resizerPos.width = TopOnlyPrefixResizerWidth;
			return resizerPos;
		}
	}
}