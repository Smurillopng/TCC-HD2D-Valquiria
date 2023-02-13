#define SAFE_MODE

//#define DEBUG_HIDE_LABELS
//#define DEBUG_SINGLE_LETTER_WIDTH

using UnityEngine;

namespace Sisus
{
	public static class RectExtensions
	{
		public static Rect Intersection(this Rect a, Rect b)
		{
			var c = a;
			c.x = Mathf.Max(a.x, b.x);
			var width = Mathf.Min(a.xMax, b.xMax) - c.x;
			c.y = Mathf.Max(a.y, b.y);
			var height = Mathf.Min(a.yMax, b.yMax) - c.y;

			if(width <= 0 || height <= 0)
			{
				c.width = 0f;
				c.height = 0f;
				return c;
			}

			c.width = width;
			c.height = height;
			return c;
		}

		public static Vector2 Clamp(this Vector2 value, Vector2 min, Vector2 max)
		{
			return new Vector2(Mathf.Clamp(value.x, min.x, max.x), Mathf.Clamp(value.y, min.y, max.y));
		}

		/// <summary>
		/// Split the Rect into prefix label and control components.
		/// If fullRect is the full width of the inspector, then labelRect width will be set to DrawGUI.PrefixLabelWidth,
		/// and controlRect width will basically be the remainder of the full width.
		/// If fullRect width is less than the width of the inspector, then labelRect width will be just enough to display a single letter
		/// and the controlRect will get all the rest of the fullRect's width.
		/// </summary>
		/// <param name="fullRect">
		/// The fullRect to act on. </param>
		/// <param name="label">
		/// The label which will be displayed inside the labelRect.
		/// If the label has no text, then the control will take the whole width of the fullRect.
		/// If the label has a tooltip, and placeTooltipsBehindIcons setting is true, then the
		/// controlRect x position and width will be adjusted to leave room for the tooltip icon. </param>
		/// <param name="labelRect">
		/// [out] The rectangle where the label should be drawn. </param>
		/// <param name="controlRect">
		/// [out] The rectangle where the control should be drawn. </param>
		public static void GetLabelAndControlRects(this Rect fullRect, GUIContent label, out Rect labelRect, out Rect controlRect)
		{
			#if SAFE_MODE
			if(label == null)
			{
				#if DEV_MODE
				Debug.LogWarning("GetLabelAndControlRects - label was null!");
				#endif
				label = GUIContent.none;
			}

			if(label.text == null)
			{
				#if DEV_MODE
				Debug.LogWarning("GetLabelAndControlRects - label.text was null!");
				#endif
				if(label.image == null)
				{
					label = GUIContent.none;
				}
				else
				{
					label = new GUIContent("", label.image, label.tooltip);
				}
			}
			#endif

			var fullDrawAreaWidth = DrawGUI.GetCurrentDrawAreaWidth();

			if(label.text.Length == 0 && label.image == null)
			{
				//added this fix for stuff like Button not respecting edge paddings
				//when drawn in full width with no label
				if(fullRect.width + DrawGUI.LeftPadding >= fullDrawAreaWidth)
				{
					float leftOffset = DrawGUI.LeftPadding + DrawGUI.IndentLevel * DrawGUI.IndentWidth;
					fullRect.x = fullRect.x + leftOffset;
					fullRect.width = fullRect.width - leftOffset - DrawGUI.RightPadding;
				}

				labelRect = fullRect;
				labelRect.width = 0f;
				controlRect = fullRect;
			}
			//indent width reduction was needed after adding fix for property drawers being drawn
			//too close to left edge of inspector
			else
			{
				if(fullDrawAreaWidth - fullRect.width > DrawGUI.ScrollBarWidth)
				{
					labelRect = fullRect;
					controlRect = fullRect;
					if(fullRect.width >= DrawGUI.MinWidthWithSingleLetterPrefix)
					{
						labelRect.width = InspectorPreferences.Styles.Label.CalcSize(label).x;
						controlRect.x += labelRect.width;
						controlRect.width -= labelRect.width;

						#if DEV_MODE && DEBUG_SINGLE_LETTER_WIDTH
						DebugUtility.LogChanges(label.text+"_SingleLetterWidth"+fullRect, "Changing "+label.text+" labelRect.width to SingleLetterPrefixWidth because InspectorWidth ("+DrawGUI.InspectorWidth+") - fullRect.width ("+fullRect.width+") > "+DrawGUI.ScrollBarWidth);
						#endif
					}
					//dynamically hide the prefix label if there's not enough space
					else
					{
						#if DEV_MODE && DEBUG_HIDE_LABELS
						Debug.LogWarning("Hiding "+label.text+" labelRect because not enough space...");
						#endif

						labelRect.width = 0f;
					}
				}
				else
				{
					GetLabelAndControlRects(fullRect, out labelRect, out controlRect);
				}
			}

			//Add room for the tooltip icon
			if(InspectorUtility.Preferences.enableTooltipIcons && label.tooltip.Length > 0)
			{
				controlRect.x += DrawGUI.SingleLineHeight;
				controlRect.width -= DrawGUI.SingleLineHeight;
			}
		}

		public static void GetSingleRowControlRects(this Rect bodyDrawRect, IDrawer[] members, ref Rect[] rects)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(rects.Length == members.Length, rects.Length + " != " + members.Length);
			#endif

			int count = members.Length;
			switch(count)
			{
				case 0:
					return;
				case 1:
					rects[0] = bodyDrawRect;
					return;
				default:
					break;
			}

			var label = members[0].Label;
			float labelWidth;
			if(label == null)
			{
				#if DEV_MODE
				Debug.LogWarning("GetSingleRowControlRects - members[0] " + members[0] + " Label was null.\nParent="+(members[0].Parent == null ? "null" : members[0].Parent.ToString()));
				#endif
				labelWidth = 0f;
			}
			else
			{
				labelWidth = InspectorPreferences.Styles.Label.CalcSize(label).x;
			}

			rects[0].width = labelWidth;
			float labelsWidth = labelWidth;

			for(int n = 1; n < count; n++)
			{
				labelWidth = InspectorPreferences.Styles.Label.CalcSize(members[n].Label).x;
				rects[n].width = labelWidth;
				const float padding = 3f;
				labelsWidth += padding + labelWidth;
			}

			float controlsWidth = bodyDrawRect.width - labelsWidth;
			if(controlsWidth >= 0f)
			{
				float controlWidth = controlsWidth / count;
				float x = bodyDrawRect.x;
				for(int n = 0; n < count; n++)
				{
					var rect = rects[n];
					float width = rect.width + controlWidth;
					rect.width = width;
					rect.x = x;
					rect.y = bodyDrawRect.y;
					rect.height = bodyDrawRect.height;
					rects[n] = rect;
					x += width;
				}
			}
			else
			{
				float multiplier = bodyDrawRect.width / labelsWidth;
				for(int n = 0; n < count; n++)
				{
					rects[n].width *= multiplier;
				}
			}
		}

		public static void GetSingleRowControlRects(this Rect bodyDrawRect, GUIContent[] controlLabels, ref Rect[] rects)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(rects.Length == controlLabels.Length);
			#endif

			int count = controlLabels.Length;
			switch(count)
			{
				case 0:
					return;
				case 1:
					rects[0] = bodyDrawRect;
					return;
				default:
					break;
			}

			float labelWidth = InspectorPreferences.Styles.Label.CalcSize(controlLabels[0]).x;
			rects[0].width = labelWidth;
			float labelsWidth = labelWidth;

			for(int n = 1; n < count; n++)
			{
				labelWidth = InspectorPreferences.Styles.Label.CalcSize(controlLabels[n]).x;
				rects[n].width = labelWidth;
				const float padding = 3f;
				labelsWidth += padding + labelWidth;
			}

			float controlsWidth = bodyDrawRect.width - labelsWidth;
			if(controlsWidth >= 0f)
			{
				float controlWidth = controlsWidth / count;
				float x = bodyDrawRect.x;
				for(int n = 0; n < count; n++)
				{
					var rect = rects[n];
					float width = rect.width + controlWidth;
					rect.width = width;
					rect.x = x;
					rect.y = bodyDrawRect.y;
					rect.height = bodyDrawRect.height;
					rects[n] = rect;
					x += width;
				}
			}
			else
			{
				float multiplier = bodyDrawRect.width / labelsWidth;
				for(int n = 0; n < count; n++)
				{
					rects[n].width *= multiplier;
				}
			}
		}

		public static void GetLabelAndControlRects(this Rect fullRect, out Rect labelRect, out Rect controlRect)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(fullRect.width.Equals(DrawGUI.GetCurrentDrawAreaWidth()), "GetLabelAndControlRects : fullRect.width (" + fullRect.width + ") != DrawGUI.GetCurrentDrawAreaWidth() (" + DrawGUI.GetCurrentDrawAreaWidth() + ")");
			#endif

			labelRect = fullRect;

			float indent = DrawGUI.IndentLevel * DrawGUI.IndentWidth;

			labelRect.x = fullRect.x + DrawGUI.LeftPadding + indent;
			labelRect.height = DrawGUI.SingleLineHeight;
			float prefixWidth = DrawGUI.PrefixLabelWidth;
			labelRect.width = prefixWidth - DrawGUI.LeftPadding - indent - DrawGUI.MiddlePadding;
			
			controlRect = fullRect;
			controlRect.x = fullRect.x + prefixWidth + DrawGUI.MiddlePadding;
			controlRect.width = fullRect.width - prefixWidth - DrawGUI.MiddlePadding - DrawGUI.RightPadding;

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(labelRect.width > 0f || DrawGUI.PrefixLabelWidth <= DrawGUI.LeftPadding + indent + DrawGUI.MiddlePadding, "labelRect " + labelRect + " from fullRect " + fullRect + " with PrefixLabelWidth=" + DrawGUI.PrefixLabelWidth + ", Indent=" + indent+", LeftPadding = " + DrawGUI.LeftPadding+" and MiddlePadding="+DrawGUI.MiddlePadding);
			Debug.Assert(controlRect.width > 0f, "controlRect "+controlRect + " from fullRect "+fullRect);
			#endif
		}

		public static bool DetectClick(this Rect clickableArea, InspectorPart inspectorPart = InspectorPart.Viewport)
		{
			return DetectMouseButtonEvent(clickableArea, EventType.MouseDown, 0);
		}

		public static bool DetectRightClick(this Rect clickableArea)
		{
			return DetectMouseButtonEvent(clickableArea, EventType.MouseUp, 1);
		}

		public static bool DetectMouseButtonEvent(this Rect clickableArea, EventType eventType, int mouseButton, InspectorPart inspectorPart = InspectorPart.Viewport)
		{
			var e = Event.current;
			if(e.type == eventType && e.button == mouseButton)
			{
				return MouseIsOver(clickableArea, e.mousePosition, inspectorPart);
			}
			return false;
		}

		public static bool MouseIsOver(this Rect area)
		{
			return area.MouseIsOver(Cursor.LocalPosition);
		}

		public static bool MouseIsOver(this Rect area, Event inputEvent)
		{
			return area.MouseIsOver(inputEvent.mousePosition);
		}
		
		public static bool MouseIsOver(this Rect area, Vector2 mousePosition, InspectorPart inspectorPart = InspectorPart.Viewport)
		{
			if(area.Contains(mousePosition))
			{
				var manager = InspectorUtility.ActiveManager;
				if(manager != null)
				{
					if(manager.MouseoveredInspectorPart != inspectorPart)
					{
						#if DEV_MODE
						if(Event.current != null && Event.current.type == EventType.MouseDown)
						{ Debug.LogWarning("MouseIsOver - ignoring click because MouseoveredInspectorPart (" + manager.MouseoveredInspectorPart + ") did not match required inspectorPart "+ inspectorPart + "!"); }
						#endif
						return false;
					}

					if(manager.IgnoreAllMouseInputs)
					{
						#if DEV_MODE
						if(Event.current != null && Event.current.type == EventType.MouseDown) { Debug.LogWarning("MouseIsOver - ignoring click because IgnoreAllMouseInputs was true!"); }
						#endif
						return false;
					}

					if(manager.ActiveInspector != manager.MouseoveredInspector)
					{
						#if DEV_MODE
						if(Event.current != null && Event.current.type == EventType.MouseDown)
						{ Debug.LogWarning("MouseIsOver - ignoring click because ActiveInspector (" + StringUtils.ToString(manager.ActiveInspector) +") != MouseoveredInspector ("+ StringUtils.ToString(manager.MouseoveredInspector) + ")!"); }
						#endif
						return false;
					}
				}
					
				return true;
			}
			return false;
		}

		public static bool IsZero(this Rect r)
		{
			return r.x == 0f && r.y == 0f && r.width == 0f && r.height == 0f;
		}
	}
}