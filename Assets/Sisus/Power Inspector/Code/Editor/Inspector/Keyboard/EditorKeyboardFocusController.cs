#define DEBUG_GET_NEXT_CONTROL_UP
#define DEBUG_GET_NEXT_CONTROL_DOWN
#define DEBUG_GET_NEXT_CONTROL_LEFT
#define DEBUG_GET_NEXT_CONTROL_RIGHT
#define DEBUG_SKIP_PHANTOM_CONTROL
#define DEBUG_GET_EDITOR_CONTROL_AT_RECT
#define DEBUG_IS_FIRST_FIELD
#define DEBUG_IS_LAST_FIELD

#if UNITY_EDITOR
using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Sisus
{
	public delegate IDrawer GetNextDrawerUpOrDown(int column, IDrawer requester);
	public delegate IDrawer GetNextDrawerLeftOrRight(bool moveToNextControlAfterReachingEnd, IDrawer requester);

	public delegate void SelectHeaderPart(HeaderPart headerPart, bool setKeyboardControl = true);
	public delegate void SelectNextHeaderPart(bool moveToNextControlAfterReachingEnd);

	/// <summary>
	/// Class responsible for determining which control inside an Editor should get keyboard focus next
	/// based on given keyboard input.
	/// </summary>
	public class EditorKeyboardFocusController
	{
		#if DEV_MODE
		private static bool debug;
		#endif

		//In Camera Editor this was 50 before a valid field was found. For Particle System Editor had to raise it to 100 or it wouldn't work.
		private const int GetNextFieldMaxOutOfBoundsCount = 100;

		private readonly float prefixResizerHeight;
		
		/// <summary>
		/// Height of a single row inside the Editor. I.e. the default distance between the start of
		/// a control drawn on one row, and the start of a control drawn on the row below.
		/// </summary>
		/// <value> Row height in pixels. </value>
		private readonly float rowHeight;

		/// <summary> Offset of items inside Editor from the top edge in pixels. </summary>
		private readonly float topMargin;

		/// <summary> Offset of items inside Editor from the left edge in pixels. </summary>
		private readonly float leftMargin;

		/// <summary> Offset of items inside Editor from the right edge in pixels. </summary>
		private readonly float rightMargin;

		/// <summary> Offset of items inside Editor from the bottom edge in pixels. </summary>
		private readonly float bottomMargin;
		
		/// <summary> Offset between end of prefix label column end and control part start inside the Editor. </summary>
		private readonly float prefixEndToControlStartOffset;

		/// <summary> When multiple controls are drawn on a single row, this is the  horizontal offset between them. </summary>
		private readonly float multiControlOffset;

		private readonly int appendLastCheckedId;

		#if DEV_MODE
		private System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<int, Rect>> allControlRects = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<int, Rect>>();
		#endif

		/// <summary> Initializes a new instance of the EditorKeyboardFocusController class. </summary>
		/// <param name="prefixColumnResizerHeight"> Height of the drag handle below the Editor header in pixels. </param>
		/// <param name="appendLastCheckedControlID">
		/// How many keyboard focusable items to check beyond the control ID fetched after drawing of Editor has finished.
		/// Keeping the value at zero reduces changes of "ghost" fields getting focused accidentally, however in a few Editors
		/// some valid items might have IDs beyond the one at the end of the Editor.
		/// </param>
		/// <param name="editorRowHeight">
		/// Height of a single row inside the Editor. I.e. the default distance between the start of
		/// a control drawn on one row, and the start of a control drawn on the row below.
		/// </param>
		/// <param name="editorTopMargin"> Offset of items inside Editor from the top edge in pixels. </param>
		/// <param name="editorLeftMargin"> Offset of items inside Editor from the left edge in pixels. </param>
		/// <param name="editorRightMargin"> Offset of items inside Editor from the right edge in pixels. </param>
		/// <param name="editorBottomMargin"> Offset of items inside Editor from the bottom edge in pixels. </param>
		/// <param name="prefixColumnEndToControlStartOffset">
		/// Offset between end of prefix label column end and control part start inside the Editor in pixels.
		/// </param>
		/// <param name="multiControlHorizontalOffset">
		/// When multiple controls are drawn on a single row, this is the horizontal offset between them in pixels.
		/// </param>
		public EditorKeyboardFocusController(float prefixColumnResizerHeight = PrefixResizeUtility.TopOnlyPrefixResizerHeight, int appendLastCheckedControlID = 0, float editorRowHeight = DrawGUI.SingleLineHeight, float editorTopMargin = 1f, float editorLeftMargin = 13f, float editorRightMargin = 4f, float editorBottomMargin = 4f, float prefixColumnEndToControlStartOffset = 6f, float multiControlHorizontalOffset = 2f)
		{
			appendLastCheckedId = appendLastCheckedControlID;
			prefixResizerHeight = prefixColumnResizerHeight;
			rowHeight = editorRowHeight;
			topMargin = editorTopMargin;
			leftMargin = editorLeftMargin;
			rightMargin = editorRightMargin;
			bottomMargin = editorBottomMargin;

			prefixEndToControlStartOffset = prefixColumnEndToControlStartOffset;
			multiControlOffset = multiControlHorizontalOffset;
		}
		
		public void SelectNextEditorControlUp(ICustomEditorDrawer subject, int endControlId, bool useBuiltInFieldFocusingSystem, SelectHeaderPart selectHeaderPart)
		{
			#if DEV_MODE
			#if DEBUG_GET_NEXT_CONTROL_UP
			debug = true;
			#else
			debug = false;
			#endif
			#endif

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(subject.Height > subject.HeaderHeight);
			#endif
			
			if(!useBuiltInFieldFocusingSystem)
			{
				int idWas = KeyboardControlUtility.KeyboardControl;
				if(idWas == 0)
				{
					SelectLastField(subject, endControlId, selectHeaderPart);
					ScrollToShow(subject.Inspector);
					return;
				}

				var setId = subject.HeaderHeight <= subject.Height ? 0 : GetNextEditorControlUp(subject, KeyboardControlUtility.Info.KeyboardRect);

				if(setId <= 0)
				{
					selectHeaderPart(HeaderPart.Base);
					ScrollToShow(subject.Inspector);
				}
				else
				{
					KeyboardControlUtility.SetKeyboardControl(setId, 3);
					selectHeaderPart(HeaderPart.None);
					ScrollToShow(subject.Inspector);
				}
			}
			else
			{
				selectHeaderPart(HeaderPart.None, false);
				HandleSelectNextFieldUpLeavingBounds(subject, selectHeaderPart);
				subject.OnNextLayout(ScrollToShow);
			}
		}

		private void ScrollToShow(IDrawer subject)
		{
			ScrollToShow(subject.Inspector);
		}

		private void HandleSelectNextFieldUpLeavingBounds(ICustomEditorDrawer subject, SelectHeaderPart selectHeaderPart)
		{
			HandleSelectNextFieldLeavingBounds(subject, ()=>
			{
				#if DEV_MODE
				if(debug) { Debug.LogWarning(ToString()+ ".HandleSelectNextFieldUpLeavingBounds - control out of body bounds! keyboardControl=" + KeyboardControlUtility.KeyboardControl); }
				#endif

				selectHeaderPart(HeaderPart.Base);
			});
		}

		private void HandleSelectNextFieldLeavingBounds(ICustomEditorDrawer subject, [NotNull]Action invokeIfLeftBounds, int repeatTimes = 3)
		{
			if(IsKeyboardControlOutOfEditorBounds(subject))
			{
				#if DEV_MODE && (DEBUG_GET_NEXT_CONTROL_UP || DEBUG_GET_NEXT_CONTROL_DOWN || DEBUG_GET_NEXT_CONTROL_LEFT || DEBUG_GET_NEXT_CONTROL_RIGHT)
				if(debug) { Debug.LogWarning(ToString()+ ".HandleSelectNextFieldLeavingBounds - control out of Editor bounds!"); }
				#endif

				invokeIfLeftBounds();
			}
			//it can take multiple frames before Unitys internal systems changed the focused control
			else if(repeatTimes > 0)
			{
				repeatTimes--;
				subject.OnNextLayout(() => HandleSelectNextFieldLeavingBounds(subject, invokeIfLeftBounds, repeatTimes));
			}
			#if DEV_MODE && (DEBUG_GET_NEXT_CONTROL_UP || DEBUG_GET_NEXT_CONTROL_DOWN || DEBUG_GET_NEXT_CONTROL_LEFT || DEBUG_GET_NEXT_CONTROL_RIGHT)
			else if(debug) { Debug.Log(ToString() + ".HandleSelectNextFieldLeavingBounds - control within body bounds."); }
			#endif
		}

		public bool IsFirstFieldSelected(ICustomEditorDrawer subject)
		{
			if(KeyboardControlUtility.KeyboardControl == 0)
			{
				#if DEV_MODE && DEBUG_IS_LAST_FIELD
				Debug.Log("IsFirstFieldSelected: "+StringUtils.False);
				#endif
				return false;
			}

			var rect = KeyboardControlUtility.Info.KeyboardRect;
			var bounds = EditorBoundsForControls(subject);

			// with enum fields and other zero rect fields, it's pretty much impossible to know, unfortunately...
			if(rect.IsZero())
			{
				if(bounds.height < DrawGUI.SingleLineHeight * 2f)
				{
					#if DEV_MODE && DEBUG_IS_LAST_FIELD
					Debug.Log("IsFirstFieldSelected: "+StringUtils.True+" (rect was zero, but was only control)");
					#endif
					return true;
				}
				#if DEV_MODE
				Debug.LogWarning("IsFirstFieldSelected returning "+StringUtils.False+" because rect was zero. This can happen e.g. with popup fields.");
				#endif
				return false;
			}
			
			// if control resides on first row...
			if(bounds.y - rect.y < DrawGUI.SingleLineHeight)
			{
				float fullEditorWidth = subject.Width;
				float prefixLabelWidth = subject.PrefixLabelWidth;
				int member3Index = GetControlIndexInRowWith3Controls(fullEditorWidth, prefixLabelWidth, rect);
				
				// if control is only control on row (-1), or first control in row with multiple controls (0)
				if(member3Index <= 0)
				{
					#if DEV_MODE && DEBUG_IS_FIRST_FIELD
					Debug.Log(StringUtils.ToColorizedString("IsFirstFieldSelected: ", StringUtils.True, ", member3Index="+member3Index));
					#endif
					return true;
				}
				#if DEV_MODE && DEBUG_IS_FIRST_FIELD
				Debug.Log(StringUtils.ToColorizedString("IsFirstFieldSelected: ", StringUtils.False, ", member3Index="+member3Index));
				#endif
			}
			#if DEV_MODE && DEBUG_IS_FIRST_FIELD
			Debug.Log(StringUtils.ToColorizedString("IsFirstFieldSelected: ", StringUtils.False, ", with bounds.yMax=", bounds.yMax, ", rect.yMax=", rect.yMax));
			#endif
			return false;
		}

		public bool IsLastFieldSelected(ICustomEditorDrawer subject)
		{
			if(KeyboardControlUtility.KeyboardControl == 0)
			{
				#if DEV_MODE && DEBUG_IS_LAST_FIELD
				Debug.Log("IsLastFieldSelected: "+StringUtils.False);
				#endif
				return false;
			}

			var rect = KeyboardControlUtility.Info.KeyboardRect;
			var bounds = EditorBoundsForControls(subject);

			// with enum fields and other zero rect fields, it's pretty much impossible to know, unfortunately...
			if(rect.IsZero())
			{
				if(bounds.height < DrawGUI.SingleLineHeight * 2f)
				{
					#if DEV_MODE && DEBUG_IS_LAST_FIELD
					Debug.Log("IsLastFieldSelected: "+StringUtils.True+" (rect was zero, but was only control)");
					#endif
					return true;
				}
				#if DEV_MODE
				Debug.LogWarning("IsLastFieldSelected returning "+StringUtils.False+" because rect was zero. This can happen e.g. with popup fields.");
				#endif
				return false;
			}

			// if control resides on last row...
			if(bounds.yMax - rect.yMax < DrawGUI.SingleLineHeight)
			{
				float fullEditorWidth = subject.Width;
				float prefixLabelWidth = subject.PrefixLabelWidth;
				int member3Index = GetControlIndexInRowWith3Controls(fullEditorWidth, prefixLabelWidth, rect);
				
				// if control is only control on row (-1), or last control in row with multiple controls (2)
				if(member3Index == -1 || member3Index == 2)
				{
					#if DEV_MODE && DEBUG_IS_LAST_FIELD
					Debug.Log(StringUtils.ToColorizedString("IsLastFieldSelected: ", StringUtils.True, ", member3Index="+member3Index));
					#endif
					return true;
				}
				#if DEV_MODE && DEBUG_IS_LAST_FIELD
				Debug.Log(StringUtils.ToColorizedString("IsLastFieldSelected: ", StringUtils.False, ", member3Index="+member3Index));
				#endif
			}
			#if DEV_MODE && DEBUG_IS_LAST_FIELD
			Debug.Log(StringUtils.ToColorizedString("IsLastFieldSelected: ", StringUtils.False, ", with bounds.yMax=", bounds.yMax, ", rect.yMax=", rect.yMax));
			#endif
			return false;
		}

		private bool IsKeyboardControlOutOfEditorBounds(ICustomEditorDrawer subject)
		{
			var id = KeyboardControlUtility.KeyboardControl;

			if(id == 0)
			{
				return false; //?
			}

			var rect = KeyboardControlUtility.Info.KeyboardRect;
			var bounds = EditorBoundsForControls(subject);

			if(rect.y < bounds.y || rect.yMax > bounds.yMax)
			{
				#if DEV_MODE && (DEBUG_GET_NEXT_CONTROL_UP || DEBUG_GET_NEXT_CONTROL_DOWN || DEBUG_GET_NEXT_CONTROL_LEFT || DEBUG_GET_NEXT_CONTROL_RIGHT)
				if(debug) { Debug.Log(ToString()+" Control " + id + " is out of bounds because y component (" + rect.y + "..." + rect.yMax + ") outside bounds (" + bounds.y + "..." + bounds.yMax + ")..."); }
				#endif
				return true;
			}

			if(rect.x < bounds.x || rect.xMax > bounds.xMax)
			{
				#if DEV_MODE && (DEBUG_GET_NEXT_CONTROL_UP || DEBUG_GET_NEXT_CONTROL_DOWN || DEBUG_GET_NEXT_CONTROL_LEFT || DEBUG_GET_NEXT_CONTROL_RIGHT)
				if(debug) { Debug.Log(ToString() + " Control " + id + " is out of bounds because x component (" + rect.x + "..." + rect.xMax + ") outside bounds (" + bounds.x + "..." + bounds.xMax + ")..."); }
				#endif
				return true;
			}

			return false;
		}

		/// <summary> Move focus to next control inside Editor above currently focused control. </summary>
		/// <param name="previousControlRect"> Position and dimensions of the currenly focused control. </param>
		/// <returns> ControlID of next control above currently focused one within this Editor, or -1 if none found. </returns>
		private int GetNextEditorControlUp(ICustomEditorDrawer subject, Rect previousControlRect)
		{
			#if DEV_MODE
			#if DEBUG_GET_NEXT_CONTROL_UP
			debug = true;
			#else
			debug = false;
			#endif
			#endif

			#if DEV_MODE && ENABLE_ASSERTATIONS
			Debug.Assert(height > HeaderHeight);
			#endif
			
			int idWas = KeyboardControlUtility.KeyboardControl;

			int beforeHeaderControlId = subject.ControlID;

			#if DEV_MODE
			if(debug) { Debug.Log(ToString() + ".GetNextEditorControlUp(previousControlRect=" + previousControlRect + ") with idWas="+ idWas+", beforeHeaderControlId="+beforeHeaderControlId); }
			#endif
			
			if(idWas <= beforeHeaderControlId) //probably zero
			{
				#if DEV_MODE
				if(subject.ControlID != 0 && debug) { Debug.Log("GetNextEditorControlUp: -1 because idWas ("+idWas+") <= beforeHeaderControlId ("+beforeHeaderControlId+")"); }
				#endif
				return -1;
			}

			//is something consuming this event? Why does it not change the selection automatically?
			int bestId = 0;

			var bounds = EditorBoundsForControls(subject);
			float editorEnd = bounds.yMax;

			// if last rect is above bounds bottom then move bounds bottom
			// to start at last rect top
			if(previousControlRect.y < bounds.yMax)
			{
				bounds.yMax = previousControlRect.y;
			}
			
			//float preferredMaxY = Mathf.Min(editorEnd, previousControlRect.y - 2f);
			float optimalY = Mathf.Min(editorEnd, previousControlRect.y - 2f);

			float bestMatch = Mathf.Infinity;
			var rect = KeyboardControlUtility.Info.KeyboardRect;

			float fullEditorWidth = subject.Width;
			float prefixLabelWidth = subject.PrefixLabelWidth;
			int prevMember3Index = GetControlIndexInRowWith3Controls(fullEditorWidth, prefixLabelWidth, previousControlRect);

			for(int id = idWas - 1; id > beforeHeaderControlId; id--) //Should this start from something like GUIControlUtility.KeyboardControl + 100 instead?
			{
				KeyboardControlUtility.KeyboardControl = id;

				var nextRect = KeyboardControlUtility.Info.KeyboardRect;
				bool outOfBounds;
				if(GetShouldSkipControl(fullEditorWidth, rect, nextRect, bounds, id, out outOfBounds))
				{
					rect = nextRect;
					continue;
				}
				rect = nextRect;
				
				float diffY = Mathf.Abs(optimalY - rect.yMax);
				
				int diffId = GetMatchPreviousID(idWas, id);
				float diffX = Mathf.Abs(previousControlRect.x - rect.x);
				float matchBase = 100f * diffY + 5f * diffX + diffId; //UPDATE had to greatly reduce x multiplier, or x position differences could cause valid fields to get skipped
				
				float addID = GetMatchIDScore(diffId);
				float addY = GetUpDownYMatchScore(rect, diffY);
				float addX = GetUpDownXMatchScore(fullEditorWidth, prefixLabelWidth, previousControlRect, rect, diffX, prevMember3Index);
				float addHeight = GetUpDownHeightMatchScore(rect);
				float addWidth = GetUpDownWidthMatchScore(fullEditorWidth, prefixLabelWidth, previousControlRect.width, rect);

				float match = matchBase + addID + addY + addX + addHeight + addWidth;

				if(match <= bestMatch)
				{
					bestMatch = match;
					bestId = id;
					#if DEV_MODE
					if(debug) { Debug.Log("<color=green>BEST MATCH! id=" + id + ", rect=" + rect + "\nmatch=" + match + ", (base="+ matchBase+", diffY=" + diffY + ", diffX=" + diffX + ", diffID=" + diffId + ", addX="+ addX+", addY="+ addY+", addHeight="+addHeight+", addWidth="+addWidth+ ", addID=" + addID +")</color>"); }
					#endif
				}
				#if DEV_MODE
				else if(debug) { Debug.Log("(not best) id=" + id + ", rect=" + rect + "\nmatch=" + match + ", (base=" + matchBase + ", diffY=" + diffY + ", diffX=" + diffX + ", diffID=" + diffId + ", addX=" + addX + ", addY=" + addY + ", addHeight=" + addHeight + ", addWidth=" + addWidth + ", addID=" + addID + ")"); }
				#endif
			}

			return bestId;
		}

		private static int GetMatchPreviousID(int idWas, int id)
		{
			int optimalId = idWas - 1;
			return Mathf.Abs(id - optimalId);
		}
		
		private void ScrollToShow(IInspector inspector)
		{
			inspector.ScrollToShow(KeyboardControlUtility.Info.KeyboardRect);
		}

		public void SelectLastField(ICustomEditorDrawer subject, int endControlId, SelectHeaderPart selectHeaderPart)
		{
			var fullEditorHeight = subject.Height;
			if(fullEditorHeight <= subject.HeaderHeight)
			{
				#if DEV_MODE
				Debug.LogWarning(StringUtils.ToColorizedString("SelectLastField selecting HeaderPart.Base because Height (", fullEditorHeight, ") <= HeaderHeight (", subject.HeaderHeight, ")"));
				#endif
				selectHeaderPart(HeaderPart.Base);
				ScrollToShow(subject.Inspector);
				return;
			}

			var bounds = subject.Bounds;
			var rectBelowLastField = new Rect(leftMargin, bounds.yMax, bounds.width - rightMargin + leftMargin, rowHeight);

			// First try finding only controls that are smaller than endControlID + 1.
			int startCheckFromId = endControlId + appendLastCheckedId + 1;
			
			KeyboardControlUtility.KeyboardControl = startCheckFromId;
			
			#if DEV_MODE
			if(debug) { Debug.Log("-------"+ToString() + ".SelectLastField - rectBelowLastField=" + rectBelowLastField+", KeyboardControl="+KeyboardControlUtility.KeyboardControl+", beforeHeaderControlId="+subject.ControlID+ ", endControlID="+ endControlId); }
			#endif
			
			var setId = GetNextEditorControlUp(subject, rectBelowLastField);

			#if DEV_MODE
			Debug.Log("SelectLastField GetNextEditorControlUp result: " + StringUtils.ToColorizedString(setId));
			#endif

			if(setId == -1)
			{
				selectHeaderPart(HeaderPart.Base);
				ScrollToShow(subject.Inspector);
				return;
			}

			if(setId == 0)
			{
				// If failed to find valid values, try a larger range.
				startCheckFromId += 200;
				KeyboardControlUtility.KeyboardControl = startCheckFromId;
				setId = GetNextEditorControlUp(subject, rectBelowLastField);
				if(setId == -1 || setId == 0)
				{
					selectHeaderPart(HeaderPart.Base);
					ScrollToShow(subject.Inspector);
					return;
				}
			}

			KeyboardControlUtility.SetKeyboardControl(setId, 3);
			ScrollToShow(subject.Inspector);
		}

		public void SelectFirstField(ICustomEditorDrawer subject, int beforeHeaderControlId, int endControlId, SelectHeaderPart selectHeaderPart, GetNextDrawerUpOrDown getNextDrawerDown, bool useBuiltInFieldFocusingSystem)
		{
			selectHeaderPart(HeaderPart.Base, false);
			KeyboardControlUtility.KeyboardControl = beforeHeaderControlId;
			SelectNextFieldDown(subject, beforeHeaderControlId, endControlId, selectHeaderPart, getNextDrawerDown, useBuiltInFieldFocusingSystem);
		}
		
		public void SelectNextFieldDown(ICustomEditorDrawer subject, int beforeHeaderControlId, int endControlId, SelectHeaderPart selectHeaderPart, GetNextDrawerUpOrDown getNextDrawer, bool useBuiltInFieldFocusingSystem)
		{
			if(subject.Height <= subject.HeaderHeight)
			{
				KeyboardControlUtility.KeyboardControl = 0;
				subject.Inspector.Select(subject.GetNextSelectableDrawerDown(0, subject), ReasonSelectionChanged.SelectControlDown);
				return;
			}

			if(!useBuiltInFieldFocusingSystem)
			{
				var setId = subject.Unfoldedness <= 0f ? 0 : GetNextEditorControlDown(subject, beforeHeaderControlId, endControlId);
				if(setId == 0)
				{
					KeyboardControlUtility.KeyboardControl = 0;
					subject.Inspector.Select(getNextDrawer(0, subject), ReasonSelectionChanged.SelectControlDown);
					return;
				}

				KeyboardControlUtility.SetKeyboardControl(setId, 3);
				selectHeaderPart(HeaderPart.None);
				subject.OnNextLayout(ScrollToShow);
			}
			else
			{
				selectHeaderPart(HeaderPart.None, false);
				HandleSelectNextFieldDownLeavingBounds(subject, beforeHeaderControlId, getNextDrawer);
				subject.OnNextLayout(ScrollToShow);
			}
		}

		/// <summary> Gets editor control at rectangle. Useful for restoring focused control after SelectNextOfType. </summary>
		/// <param name="subject"> The subject inside which to find control. </param>
		/// <param name="localRect"> The bounds for the control to find, relative to subject Bounds. </param>
		/// <param name="beforeHeaderControlId"> ID for control before header drawing starts. </param>
		/// <param name="endControlId"> ID for first control after Editor body has finished drawing. </param>
		/// <param name="secondAttempt"> (Optional) True if this is the second attempt. </param>
		/// <returns> The ControlID for Editor control with bounds matching localRect, or -1 if not found. </returns>
		public int GetEditorControlAtRect(ICustomEditorDrawer subject, Rect localRect, int beforeHeaderControlId, int endControlId, bool secondAttempt = false)
		{
			#if DEV_MODE && DEBUG_GET_EDITOR_CONTROL_AT_RECT
			debug = true;
			#endif

			if(!subject.Unfolded || subject.Height <= subject.HeaderHeight)
			{
				#if DEV_MODE
				if(debug) { Debug.Log("GetEditorControlAtRect(" + localRect + ", beforeHeaderControlId=" + beforeHeaderControlId+", endControlId="+endControlId+", secondAttempt="+secondAttempt+") : <color=red>-1</color> because subject.Unfolded or Height <= HeaderHeight"); }
				#endif
				return -1;
			}

			int idWas = KeyboardControlUtility.KeyboardControl;
			
			// sometimes control IDs are higher than endControlID, it's not a very reliable method, unfortunately
			int start = subject.HeaderIsSelected || idWas <= beforeHeaderControlId ? beforeHeaderControlId + 1 : idWas + 1;
			if(start == idWas)
			{
				start++;
			}
			
			int stop = Mathf.Max(endControlId, idWas) + appendLastCheckedId;

			//UPDATE: had to add this to make things work with CameraInspector
			if(secondAttempt)
			{
				stop += 200;
			}
			
			var targetRect = localRect;
			var subjectBounds = subject.Bounds;
			targetRect.x += subjectBounds.x;
			targetRect.y += subjectBounds.y;

			for(int id = start; id < stop; id++)
			{
				var info = KeyboardControlUtility.Info;

				if(!info.CanHaveKeyboardFocus(id))
				{
					#if DEV_MODE
					if(debug && info.KeyboardRect.Equals(targetRect)) { Debug.LogWarning("GetEditorControlAtRect(" + localRect + ") ignoring "+id+" even though Rect is perfect match, because CanHaveKeyboardFocus returned false..."); }
					#endif
					continue;
				}

				KeyboardControlUtility.KeyboardControl = id;
				var testRect = KeyboardControlUtility.Info.KeyboardRect;

				if(testRect.Equals(targetRect))
				{
					#if DEV_MODE
					if(debug) { Debug.Log("GetEditorControlAtRect(" + localRect + ", beforeHeaderControlId=" + beforeHeaderControlId+", endControlId="+endControlId+", secondAttempt="+secondAttempt+") : <color=green>"+id+"</color>"); }
					#endif
					KeyboardControlUtility.KeyboardControl = idWas;
					return id;
				}

				#if DEV_MODE
				if(debug) { Debug.Log("GetEditorControlAtRect(" + StringUtils.ToString(localRect) + "): "+id+" rect "+StringUtils.ToString(testRect)+" != target "+StringUtils.ToString(targetRect)+".\r\nbeforeHeaderControlId=" + beforeHeaderControlId+", endControlId="+endControlId+", secondAttempt="+secondAttempt+", subjectBounds="+StringUtils.ToString(subjectBounds)); }
				#endif
			}

			KeyboardControlUtility.KeyboardControl = idWas;
			if(!secondAttempt && appendLastCheckedId == 0)
			{
				KeyboardControlUtility.KeyboardControl = idWas;
				return GetEditorControlAtRect(subject, localRect, beforeHeaderControlId, endControlId, true);
			}

			#if DEV_MODE
			if(debug) { Debug.Log("GetEditorControlAtRect(" + localRect + ", beforeHeaderControlId=" + beforeHeaderControlId+", endControlId="+endControlId+", secondAttempt="+secondAttempt+") : <color=red>-1</color> - result not found among "+(stop - start)+" tested controls."); }
			#endif
			return -1;
		}

		/// <summary> Gets editor control at position. Useful for mouseover effects. </summary>
		/// <param name="subject"> The subject inside which to find control. </param>
		/// <param name="localPosition"> The position for the control to find, relative to subject Bounds. </param>
		/// <param name="beforeHeaderControlId"> ID for control before header drawing starts. </param>
		/// <param name="endControlId"> ID for first control after Editor body has finished drawing. </param>
		/// <param name="secondAttempt"> (Optional) True if this is the second attempt. </param>
		/// <returns> The ControlID for Editor control with bounds matching localRect, or -1 if not found. </returns>
		public int GetEditorControlAtPosition(ICustomEditorDrawer subject, Vector2 localPosition, int beforeHeaderControlId, int endControlId, bool secondAttempt = false)
		{
			if(!subject.Unfolded || subject.Height <= subject.HeaderHeight)
			{
				return -1;
			}

			int idWas = KeyboardControlUtility.KeyboardControl;
			
			// sometimes control IDs are higher than endControlID, it's not a very reliable method, unfortunately
			int start = subject.HeaderIsSelected || idWas <= beforeHeaderControlId ? beforeHeaderControlId + 1 : idWas + 1;
			if(start == idWas)
			{
				start++;
			}
			
			int stop = Mathf.Max(endControlId, idWas) + appendLastCheckedId;

			//UPDATE: had to add this to make things work with CameraInspector
			if(secondAttempt)
			{
				stop += 200;
			}

			#if DEV_MODE && DEBUG_GET_NEXT_CONTROL
			Debug.Log("<color=blue>GetNextEditorControlDown: idWas=" + idWas + ", beforeHeaderControlId=" + beforeHeaderControlId+", start="+start+", stop="+stop+"</color>\r\nrectWas="+previousControlRect+", bounds="+bounds+"\r\nprevMember3Index="+ prevMember3Index+ ", preferredMaxY="+ preferredMaxY+ ", secondAttempt="+ secondAttempt +"\n" + ToString());
			#endif

			var targetPosition = localPosition;
			var subjectBounds = subject.Bounds;
			targetPosition.x += subjectBounds.x;
			targetPosition.y += subjectBounds.y;

			int outOfBoundsCounter = 0;

			for(int id = start; id < stop; id++)
			{
				var info = KeyboardControlUtility.Info;
				if(!info.CanHaveKeyboardFocus(id))
				{
					continue;
				}

				KeyboardControlUtility.KeyboardControl = id;

				if(IsKeyboardControlOutOfEditorBounds(subject))
				{
					outOfBoundsCounter++;
					if(outOfBoundsCounter >= GetNextFieldMaxOutOfBoundsCount)
					{
						#if DEV_MODE && DEBUG_GET_NEXT_CONTROL
						Debug.Log("Giving up because outOfBoundsCounter >= "+ GetNextFieldMaxOutOfBoundsCount);
						#endif
						break;
					}
					continue;
				}
				
				var testRect = KeyboardControlUtility.Info.KeyboardRect;
				if(testRect.Contains(targetPosition))
				{
					KeyboardControlUtility.KeyboardControl = idWas;
					return id;
				}
			}

			KeyboardControlUtility.KeyboardControl = idWas;
			if(!secondAttempt && appendLastCheckedId == 0)
			{
				KeyboardControlUtility.KeyboardControl = idWas;
				return GetEditorControlAtPosition(subject, localPosition, beforeHeaderControlId, endControlId, true);
			}
			return -1;
		}

		public int GetNextEditorControlDown(ICustomEditorDrawer subject, int beforeHeaderControlId, int endControlId, bool secondAttempt = false)
		{
			int idWas = KeyboardControlUtility.KeyboardControl;
			
			var editorBounds = EditorBoundsForControls(subject);
			
			float optimalY;
			Rect previousControlRect;

			// if no control inside Editor body is currently selected,
			// generate rect for imaginary control just above the first field
			// and use that as the previously selected control rect.
			if(idWas == 0 || subject.HeaderIsSelected)
			{
				var rectAboveEditor = editorBounds;
				rectAboveEditor.y -= rowHeight;
				rectAboveEditor.height = rowHeight;

				previousControlRect = rectAboveEditor;
				optimalY = editorBounds.y;
			}
			else
			{
				previousControlRect = KeyboardControlUtility.Info.KeyboardRect;
				optimalY = previousControlRect.yMax + 2f; // + ControlsRowHeight; // 2f is offset between controls (margin x 2)

				// if bottom of last selected control is below bounds top
				// then adjust bounds so that resulting control cannot be above
				// previously selected control
				if(previousControlRect.yMax > editorBounds.y)
				{
					float removeFromTop = previousControlRect.yMax - editorBounds.y;
				
					#if DEV_MODE && DEBUG_SELECT_NEXT_FIELD
					Debug.Log("Removing "+removeFromTop+" from bounds top because previousControlRect.yMax ("+previousControlRect.yMax+") > bounds.y ("+bounds.y+")\nbounds="+bounds+", previousControlRect="+previousControlRect);
					#endif

					editorBounds.y += removeFromTop;
					editorBounds.height -= removeFromTop;
				}
			}

			int bestId = 0;
			int outOfBoundsCounter = 0;
			
			//float preferredMaxY = editorBounds.yMax - rowHeight;
			float bestMatch = Mathf.Infinity;
			
			float fullEditorWidth = subject.Width;
			float prefixLabelWidth = subject.PrefixLabelWidth;
			int prevMember3Index = GetControlIndexInRowWith3Controls(fullEditorWidth, prefixLabelWidth, previousControlRect);

			//int beforeHeaderControlId = subject.ControlID;

			// sometimes control IDs are higher than endControlID, it's not a very reliable method, unfortunately
			int start = subject.HeaderIsSelected || idWas <= beforeHeaderControlId ? beforeHeaderControlId + 1 : idWas + 1;
			if(start == idWas)
			{
				start++;
			}
			
			int stop = Mathf.Max(endControlId, idWas) + appendLastCheckedId;
			//UPDATE: had to add this to make things work with CameraInspector
			if(secondAttempt)
			{
				stop += 200;
			}

			#if DEV_MODE && DEBUG_GET_NEXT_CONTROL
			Debug.Log("<color=blue>GetNextEditorControlDown: idWas=" + idWas + ", beforeHeaderControlId=" + beforeHeaderControlId+", start="+start+", stop="+stop+"</color>\r\nrectWas="+previousControlRect+", bounds="+bounds+"\r\nprevMember3Index="+ prevMember3Index+ ", preferredMaxY="+ preferredMaxY+ ", secondAttempt="+ secondAttempt +"\n" + ToString());
			#endif

			float optimalX = previousControlRect.x;

			var rect = previousControlRect;
			for(int id = start; id < stop; id++)
			{
				KeyboardControlUtility.KeyboardControl = id;

				var nextRect = KeyboardControlUtility.Info.KeyboardRect;
				bool outOfBounds;
				if(GetShouldSkipControl(fullEditorWidth, rect, nextRect, editorBounds, id, out outOfBounds))
				{
					if(outOfBounds)
					{
						outOfBoundsCounter++;
						if(outOfBoundsCounter >= GetNextFieldMaxOutOfBoundsCount) //In NetworkTransform this was 41 before a valid field was found UPDATE: Was 50 for CameraDrawer
						{
							#if DEV_MODE && DEBUG_GET_NEXT_CONTROL
							Debug.Log("Giving up because outOfBoundsCounter >= "+ GetNextFieldMaxOutOfBoundsCount);
							#endif
							break;
						}
					}
					rect = nextRect;
					continue;
				}
				rect = nextRect;

				//float optimalY = bounds.y + 2f;
				float diffY = Mathf.Abs(rect.y - optimalY);
				int diffId = GetMatchNextID(idWas, id);
				float diffX = Mathf.Abs(rect.x - optimalX);
				float match = 100f * diffY + 5f * diffX + diffId;

				//if(rect.yMax > preferredMaxY)
				//{
				//	match += 10000f;
				//}

				float addID = GetMatchIDScore(diffId);
				float addY = GetUpDownYMatchScore(rect, diffY); 
				float addX = GetUpDownXMatchScore(fullEditorWidth, prefixLabelWidth, previousControlRect, rect, diffX, prevMember3Index);
				float addHeight = GetUpDownHeightMatchScore(rect);
				float addWidth = GetUpDownWidthMatchScore(fullEditorWidth, prefixLabelWidth, previousControlRect.width, rect);

				match = match + addID + addY + addX + addHeight + addWidth;

				if(match <= bestMatch)
				{
					bestMatch = match;
					bestId = id;
					#if DEV_MODE && DEBUG_GET_NEXT_CONTROL
					Debug.Log("<color=green>BEST MATCH! id=" + id + "</color>\r\nrect=" + rect + "\r\nmatch=" + match + " (addID="+addID+", addY="+addY+", addX="+addX+", addHeight="+addHeight+ ", addWidth="+ addWidth+", diffY=" + diffY + ", diffX=" + diffX + ", diffID=" + diffId + ")");
					#endif
				}
				#if DEV_MODE && DEBUG_GET_NEXT_CONTROL
				else { Debug.Log("(not best) id=" + id + "\r\nrect=" + rect + "\r\nmatch=" + match + " (addID=" + addID + ", addY=" + addY + ", addX=" + addX + ", addHeight=" + addHeight + ", addWidth=" + addWidth + ", diffY=" + diffY + ", diffX=" + diffX + ", diffID=" + diffId + ")"); }
				#endif
			}

			if(bestId == 0)
			{
				if(!secondAttempt && appendLastCheckedId == 0)
				{
					KeyboardControlUtility.KeyboardControl = idWas;
					return GetNextEditorControlDown(subject, beforeHeaderControlId, endControlId, true);
				}
			}
			return bestId;
		}
		
		private void HandleSelectNextFieldDownLeavingBounds(ICustomEditorDrawer subject, int beforeHeaderControlId, GetNextDrawerUpOrDown getNextDrawer)
		{
			HandleSelectNextFieldLeavingBounds(subject, ()=>
			{
				#if DEV_MODE
				if(debug) { Debug.LogWarning(ToString()+ ".HandleSelectNextFieldDownLeavingBounds - control out of body bounds! keyboardControl=" + KeyboardControlUtility.KeyboardControl); }
				#endif

				KeyboardControlUtility.KeyboardControl = beforeHeaderControlId;
				subject.Inspector.Select(getNextDrawer(0, subject), ReasonSelectionChanged.SelectControlDown);
			});
		}

		public void SelectNextEditorControlRight(ICustomEditorDrawer subject, int beforeHeaderControlId, int endControlId)
		{
			#if DEV_MODE
			#if DEBUG_GET_NEXT_CONTROL_RIGHT
			debug = true;
			#else
			debug = false;
			#endif
			#endif

			int idWas = KeyboardControlUtility.KeyboardControl;

			if(idWas == 0)
			{
				#if DEV_MODE
				Debug.LogError("SelectNextEditorControlRight - No control is focused currently. Should select next header part instead?");
				#endif
				return;
			}

			int setId = GetNextEditorControlRight(subject, beforeHeaderControlId, endControlId);

			if(setId > idWas)
			{
				KeyboardControlUtility.SetKeyboardControl(setId, 3);
				ScrollToShow(subject.Inspector);
			}
			#if DEV_MODE
			else if(setId != idWas) { Debug.LogWarning("SelectNextEditorControlRight - ignoring setId "+setId+" because < idWas "+idWas); }
			#endif
		}

		public int GetNextEditorControlRight(ICustomEditorDrawer subject, int beforeHeaderControlId, int endControlId)
		{
			#if DEV_MODE
			#if DEBUG_GET_NEXT_CONTROL_RIGHT
			debug = true;
			#else
			debug = false;
			#endif
			#endif

			int idWas = KeyboardControlUtility.KeyboardControl;

			if(idWas <= beforeHeaderControlId) //probably zero
			{
				#if DEV_MODE
				if(subject.ControlID != 0 && debug) { Debug.Log("GetNextEditorControlUp: -1 because idWas ("+idWas+") <= beforeHeaderControlId ("+beforeHeaderControlId+")"); }
				#endif
				return -1;
			}

			var rectWas = KeyboardControlUtility.Info.KeyboardRect;

			var fullEditorWidth = subject.Width;

			// If focused control is already very close to the right edge
			// there is not enough space for other focusable controls to exist in that direction.
			if(rectWas.xMax >= fullEditorWidth - rightMargin - 11f)
			{
				#if DEV_MODE
				if(subject.ControlID != 0 && debug) { Debug.Log("GetNextEditorControlRight: -1 because rectWas.x ("+rectWas.x+") >= "+(rightMargin - 11f)); }
				#endif
				return - 1;
			}

			var previousControlRect = KeyboardControlUtility.Info.KeyboardRect;
			
			int bestID = idWas;

			int outOfBoundsCounter = 0;

			var bounds = EditorBoundsForControls(subject);
			bounds.y = previousControlRect.y;
			bounds.yMax = previousControlRect.yMax;
			bounds.x = previousControlRect.x;
			
			//float fullEditorWidth = subject.Width;
			float prefixLabelWidth = subject.PrefixLabelWidth;

			float preferredMinX = fullEditorWidth - previousControlRect.width <= 18f ? subject.PrefixLabelWidth + 4f : previousControlRect.xMax + 2f;
			float bestMatch = Mathf.Infinity;
			var rect = previousControlRect;
			
			//sometimes control IDs are higher than endControlID, it's not very reliable unfortunately :(
			int stop = Mathf.Max(endControlId, KeyboardControlUtility.KeyboardControl + 100);
			int start = KeyboardControlUtility.KeyboardControl <= beforeHeaderControlId ? beforeHeaderControlId + 1 : KeyboardControlUtility.KeyboardControl + 1;

			#if DEV_MODE
			if(debug) { Debug.Log("<color=blue>SelectNextEditorControlRight: idWas=" + idWas + ", beforeHeaderControlId=" + beforeHeaderControlId+", rect="+rect+ ", bounds="+ bounds+", start="+start+", stop="+stop+ "</color>\n" + ToString()); }
			#endif
			
			for(int id = start; id < stop; id++)
			{
				KeyboardControlUtility.KeyboardControl = id;

				var nextRect = KeyboardControlUtility.Info.KeyboardRect;
				bool outOfBounds;
				if(GetShouldSkipControl(fullEditorWidth, rect, nextRect, bounds, id, out outOfBounds))
				{
					if(outOfBounds)
					{
						outOfBoundsCounter++;
						if(outOfBoundsCounter >= GetNextFieldMaxOutOfBoundsCount)
						{
							break;
						}
					}
					rect = nextRect;
					continue;
				}
				rect = nextRect;

				float diffY = Mathf.Abs(bounds.y - rect.y);

				int matchY = (int)diffY;
				int matchID = GetMatchNextID(idWas, id);
				float matchX = Mathf.Abs(rect.x - preferredMinX);
				float match = 100f * matchY + 50f * matchX + matchID;

				float addID = GetMatchIDScore(matchID);
				float addY = GetYMatchScoreBase(rect);
				float addX = GetRightXMatchScore(fullEditorWidth, prefixLabelWidth, previousControlRect, rect, matchX);
				float addHeight = GetUpDownHeightMatchScore(rect);
				float addWidth = GetUpDownWidthMatchScore(fullEditorWidth, prefixLabelWidth, previousControlRect.width, rect);

				match = match + addID + addY + addX + addHeight + addWidth;

				if(rect.x < preferredMinX)
				{
					match += 10000f;
				}

				if(match <= bestMatch)
				{
					bestMatch = match;
					bestID = id;
					#if DEV_MODE
					if(debug) {Debug.Log("<color=green>BEST MATCH! id=" + id + ", rect=" + rect + ", match=" + match + ", (addID=" + addID + ", addY=" + addY + ", addX=" + addX + ", addHeight=" + addHeight + ", addWidth=" + addWidth + ", y=" + matchY + ", x=" + matchX + ", id=" + matchID + ")</color>"); }
					#endif
				}
				#if DEV_MODE && DEBUG_GET_NEXT_CONTROL_RIGHT
				else if(debug) { Debug.Log("(not best) id=" + id + ", rect=" + rect + ", match=" + match + ", (addID=" + addID + ", addY=" + addY + ", addX=" + addX + ", addHeight=" + addHeight + ", addWidth=" + addWidth + ", y=" + matchY + ", x=" + matchX + ", id=" + matchID + ")"); }
				#endif
			}
			
			if(bestID > idWas)
			{
				return bestID;
			}

			#if DEV_MODE
			if(bestID != idWas) { Debug.LogWarning("SelectNextEditorControlRight - ignoring bestID "+bestID+" because < idWas "+idWas); }
			#endif

			return -1;
		}

		public void SelectNextEditorControlLeft(ICustomEditorDrawer subject, int beforeHeaderControlId)
		{
			#if DEV_MODE
			#if DEBUG_GET_NEXT_CONTROL_LEFT
			debug = true;
			#else
			debug = false;
			#endif
			#endif

			int idWas = KeyboardControlUtility.KeyboardControl;

			if(idWas == 0)
			{
				#if DEV_MODE
				Debug.LogError("SelectNextEditorControlLeft - No control is focused currently. Should select next header part instead?");
				#endif
				return;
			}

			int setId = GetNextEditorControlLeft(subject, beforeHeaderControlId);

			if(setId < idWas && setId != -1)
			{
				KeyboardControlUtility.SetKeyboardControl(setId, 3);
				ScrollToShow(subject.Inspector);
			}
			#if DEV_MODE
			else if(setId != idWas) { Debug.LogWarning("SelectNextEditorControlLeft - ignoring setId "+setId+" because < idWas "+idWas); }
			#endif
		}

		public int GetNextEditorControlLeft(ICustomEditorDrawer subject, int beforeHeaderControlId)
		{
			int idWas = KeyboardControlUtility.KeyboardControl;

			if(idWas <= beforeHeaderControlId) //probably zero
			{
				#if DEV_MODE
				if(subject.ControlID != 0 && debug) { Debug.Log("GetNextEditorControlLeft: -1 because idWas ("+idWas+") <= beforeHeaderControlId ("+beforeHeaderControlId+")"); }
				#endif
				return idWas; //-1;
			}
			
			var rectWas = KeyboardControlUtility.Info.KeyboardRect;

			// If focused control is already very close to the left edge
			// there is not enough space for other focusable controls to exist in that direction.
			if(rectWas.x <= leftMargin + 11f)
			{
				#if DEV_MODE
				if(subject.ControlID != 0 && debug) { Debug.Log("GetNextEditorControlLeft: -1 because rectWas.x ("+rectWas.x+") <= "+(leftMargin + 11f)); }
				#endif
				return idWas; //-1;
			}
			
			int bestID = idWas;

			int outOfBoundsCounter = 0;

			var bounds = EditorBoundsForControls(subject);

			//UPDATE: when going left, support case like Rect where
			//parent prefix is higher up than current member field.
			//Not sure if we should support even higher differences?
			//Maybe for something like array?
			//bounds.y = rectWas.y;
			bounds.y = rectWas.y - 16f;

			//UPDATE: also added +16 here so that can e.g. move from
			//x member of Rect field to Rect prefix
			bounds.yMax = rectWas.yMax + 16f;
			//is any control ever without any offset? should I split the -2f to preferredMaxX ?
			float preferredMinY = rectWas.y;
			float preferredMaxX = rectWas.x - 2f;

			float bestMatch = Mathf.Infinity;
			var rect = rectWas;

			#if DEV_MODE
			Debug.Log("<color=blue>SelectNextEditorControlLeft: idWas=" + idWas + ", beforeHeaderControlId=" + beforeHeaderControlId+", rect="+rect+ ", bounds="+ bounds+", start="+(KeyboardControlUtility.KeyboardControl - 1) +", stop="+ beforeHeaderControlId + "</color>\n" + ToString());
			#endif

			float fullEditorWidth = subject.Width;
			float prefixLabelWidth = subject.PrefixLabelWidth;

			for(int id = KeyboardControlUtility.KeyboardControl - 1; id > beforeHeaderControlId; id--)
			{
				KeyboardControlUtility.KeyboardControl = id;

				var nextRect = KeyboardControlUtility.Info.KeyboardRect;
				bool outOfBounds;
				if(GetShouldSkipControl(fullEditorWidth, rect, nextRect, bounds, id, out outOfBounds))
				{
					if(outOfBounds)
					{
						outOfBoundsCounter++;
						if(outOfBoundsCounter >= GetNextFieldMaxOutOfBoundsCount)
						{
							break;
						}
					}
					rect = nextRect;
					continue;
				}
				rect = nextRect;

				float diffY = Mathf.Abs(preferredMinY - rect.y);

				#if DEV_MODE
				if(debug) { Debug.Log("KeyboardControl " + id + " diffY=" + diffY + " (rect=" + rect + ")"); }
				#endif

				int matchY = (int)diffY;
				int matchID = GetMatchPreviousID(idWas, id);
				float matchX = Mathf.Abs(rect.xMax - preferredMaxX);
				float match = 100f * matchY + 50f * matchX + matchID;
				
				float addID = GetMatchIDScore(matchID);
				float addY = GetYMatchScoreBase(rect);
				float addX = GetLeftXMatchScore(fullEditorWidth, prefixLabelWidth, rectWas, rect, matchX);
				float addHeight = GetUpDownHeightMatchScore(rect);
				float addWidth = GetUpDownWidthMatchScore(fullEditorWidth, prefixLabelWidth, rectWas.width, rect);

				match = match + addID + addY + addX + addHeight + addWidth;

				if(rect.x > preferredMaxX)
				{
					match += 10000f;
				}

				if(match <= bestMatch)
				{
					bestMatch = match;
					bestID = id;
					#if DEV_MODE
					if(debug) { Debug.Log("<color=green>BEST MATCH! id=" + id + ", rect=" + rect + ", match=" + match + ", (addID=" + addID + ", addY=" + addY + ", addX=" + addX + ", addHeight=" + addHeight + ", addWidth=" + addWidth + ", y=" + matchY + ", x=" + matchX + ", id=" + matchID + ")</color>"); }
					#endif
				}
				#if DEV_MODE
				else if(debug) { Debug.Log("(not best) id=" + id + ", rect=" + rect + ", match=" + match + ", (addID=" + addID + ", addY=" + addY + ", addX=" + addX + ", addHeight=" + addHeight + ", addWidth=" + addWidth + ", y=" + matchY + ", x=" + matchX + ", id=" + matchID + ")"); }
				#endif
			}

			if(bestID < idWas)
			{
				return bestID;
			}

			#if DEV_MODE
			if(bestID != idWas) { Debug.LogWarning("SelectNextEditorControlLeft - ignoring bestID "+bestID+" because > idWas "+idWas); }
			#endif

			return -1;

			//return bestID;
			//KeyboardControlUtility.KeyboardControl = bestID;
			//ScrollToShow();
		}

		private static int GetMatchNextID(int idWas, int id)
		{
			int optimalId = idWas + 1;
			return Mathf.Abs(id - optimalId);
		}

		protected virtual bool GetShouldSkipControl(float fullEditorWidth, Rect prevRect, Rect rect, Rect bounds, int id, out bool wasOutOfBounds)
		{
			if(!KeyboardControlUtility.Info.CanHaveKeyboardFocus(id))
			{
				#if DEV_MODE && DEBUG_SKIP_CAN_NOT_HAVE_KEYBOARD_CONTROL
				Debug.Log("!!!!! control " + id + " CanHaveKeyboardFocus was false!");
				#endif
				wasOutOfBounds = false;
				return true;
			}

			if(rect == prevRect)
			{
				wasOutOfBounds = false;
				return true;
			}

			return GetShouldSkipControl(fullEditorWidth, rect, bounds, id, out wasOutOfBounds);
		}

		private bool GetShouldSkipControl(float fullEditorWidth, Rect rect, Rect bounds, int id, out bool wasOutOfBounds)
		{
			wasOutOfBounds = false;

			if(GetIsKeyboardSelectedControlInvalid(rect, id, fullEditorWidth))
			{
				return true;
			}
			
			if(IsOutOfVerticalBounds(rect, bounds))
			{
				#if DEV_MODE && DEBUG_SKIP_OUT_OF_BOUNDS
				Debug.Log("Skipping control " + id + " because y component (" + rect.y + "..." + rect.yMax + ") outside bounds (" + bounds.y + "..." + bounds.yMax + ")...");
				#endif
				wasOutOfBounds = true;
				return true;
			}

			if(IsOutOfHorizontalBounds(rect, bounds))
			{
				#if DEV_MODE && DEBUG_SKIP_OUT_OF_BOUNDS
				Debug.Log("Skipping control " + id + " because x component (" + rect.x + "..." + rect.xMax + ") outside bounds (" + bounds.x + "..." + bounds.xMax + ")...");
				#endif
				wasOutOfBounds = true;
				return true;
			}

			return false;
		}

		private bool GetIsKeyboardSelectedControlInvalid(Rect rect, int id, float fullEditorWidth)
		{
			float inspectorWidthDiff = fullEditorWidth - rect.width;

			if(rect.x.Equals(leftMargin))
			{
				switch((int)inspectorWidthDiff)
				{
					case 210:
					case 156:
					case 33:
					case 54:
						#if DEV_MODE && DEBUG_SKIP_PHANTOM_CONTROL
						Debug.Log("Skipping control " + id + " because inspectorWidthDiff is " + inspectorWidthDiff + ". This is probably a phantom control.");
						#endif
						return true;
				}
			}
			#if DEV_MODE && PI_ASSERTATIONS
			else { Debug.Assert(Mathf.Abs(rect.x - leftMargin) > 0.99f, "rect.x="+rect.x+", leftMargin="+leftMargin); }
			#endif

			return false;
		}

		private static bool IsOutOfBounds(Rect rect, Rect bounds)
		{
			return IsOutOfHorizontalBounds(rect, bounds) || IsOutOfVerticalBounds(rect, bounds);
		}

		private static bool IsOutOfHorizontalBounds(Rect rect, Rect bounds)
		{
			return rect.x < bounds.x || rect.xMax > bounds.xMax;
		}

		private static bool IsOutOfVerticalBounds(Rect rect, Rect bounds)
		{
			return rect.y < bounds.y || rect.yMax > bounds.yMax;
		}

		/// <summary>
		/// Gets a score based on how close the ID is to being a perfect match
		/// in the context of figuring out the next field to gain focus when moving keyboard focus.
		/// </summary>
		/// <param name="diffID"> Identifier offset from perfect match. </param>
		/// <returns> Score. Smaller is better. </returns>
		protected virtual float GetMatchIDScore(int diffID)
		{
			if(diffID >= 40)
			{
				return 0f;
			}

			if(diffID >= 20)
			{
				// UPDATE: also changed from -2500f to improve Light component results
				// (actually problems are caused by above changes, since wrong id with 17 diff was beating right id with 37 diff - so could also merge these two checks)
				return -1500f;
			}

			if(diffID > 0)
			{
				// UPDATE: changed from -2500f to improve Light component results
				return -3000f;
			}

			// was -100f before, because resulted in some false positive earlier? But does that still happen?
			return -5000f;
		}

		/// <summary>
		/// Gets a score based on how close the control y position is to being a perfect match
		/// in the context of figuring out the next field to gain focus when moving keyboard focus up or down.
		/// </summary>
		/// <param name="rect"> Position and dimensions of the control to test. </param>
		/// <param name="diffY"> y position offset from perfect match. </param>
		/// <returns> Score. Smaller is better. </returns>
		protected virtual float GetUpDownYMatchScore(Rect rect, float diffY)
		{
			int diffYInt = (int)diffY;
			float result = GetYMatchScoreBase(rect);

			if(!((float)diffYInt).Equals(diffY))
			{
				#if DEV_MODE && (DEBUG_GET_NEXT_CONTROL_UP || DEBUG_GET_NEXT_CONTROL_DOWN)
				Debug.Log("rect "+rect+" returning "+result+" + 21000f because diffYInt ("+diffYInt+") != diffY ("+diffY+") with difference="+Mathf.Abs(diffYInt-diffY));
				#endif
				return result + 21000f;
			}

			switch(diffYInt)
			{
				case 0:
					//next row, with 1px margin for both fields
					return result - 10000f;
				case 8:
					//next row with a small gap between (e.g. RectTransform, AudioSource)
					return result - 8000f;
				case 14:
					//two row difference
					return result - 6000f;
				/*
				case 45:
					//two fields with a help box between them (e.g. MeshRenderer)
					return result - 7000f;
				*/
				default:
					#if DEV_MODE && (DEBUG_GET_NEXT_CONTROL_UP || DEBUG_GET_NEXT_CONTROL_DOWN)
					Debug.Log("rect "+rect+" returning "+result+" + 20000f because diffYInt ("+diffYInt+") was not a value we like");
					#endif
					return result + 20000f;
			}
		}

		protected virtual float GetYMatchScoreBase(Rect rect)
		{
			//not 100% sure about this. Can a HelpBox e.g. change y value to be not divisible by 2?
			if(!(rect.y % 2f).Equals(0f))
			{
				return 1000f;
			}
			return 0f;
		}

		/// <summary>
		/// Gets a score based on how close the control x position is to being a perfect match
		/// in the context of figuring out the next field to gain focus when moving keyboard focus up or down.
		/// </summary>
		/// <param name="fullEditorWidth"> Full width of the Editor view. </param>
		/// <param name="prefixLabelWidth"> Prefix label column width used when drawing Editor. </param>
		/// <param name="prevRect"> Rectangle for the previously selected control. </param>
		/// <param name="rect"> Position and dimensions of the control to test. </param>
		/// <param name="diffX"> x position offset from perfect match. </param>
		/// <param name="prevMember3Index"> Zero-based index of control being tested in row that has three controls on it. -1 if not applicable. </param>
		/// <returns> Score. Smaller is better. </returns>
		protected virtual float GetUpDownXMatchScore(float fullEditorWidth, float prefixLabelWidth, Rect prevRect, Rect rect, float diffX, int prevMember3Index)
		{
			bool startsFromLeftEdge = rect.x.Equals(leftMargin);

			float result = GetXMatchScoreBase(fullEditorWidth, prefixLabelWidth, rect);
			if(diffX.Equals(0f))
			{
				if(startsFromLeftEdge)
				{
					result -= 12000f;
				}
				else
				{
					result -= 11000f;
				}
			}

			float prefixWidth = prefixLabelWidth;
			float fullWidthDiff = fullEditorWidth - rect.width - leftMargin - rightMargin;

			if(startsFromLeftEdge)
			{
				// if the control starts from the left edge of the control, then it's width needs to basically match
				// the width of the inspector, or it's probably a "phantom" control (from another window maybe?).
				if(fullWidthDiff > 1f || fullWidthDiff < 0f)
				{
					#if DEV_MODE && (DEBUG_GET_NEXT_CONTROL_UP || DEBUG_GET_NEXT_CONTROL_DOWN)
					Debug.Log("rect "+rect+" fullWidthDiff ("+fullWidthDiff+") != "+(leftMargin + rightMargin)+" with fullEditorWidth="+fullEditorWidth);
					#endif
					return result + 50000f;
				}

				if(prevRect.x.Equals(prefixWidth + 5f))
				{
					return result - 11000f;
				}
				
				return result - 5000f;
			}

			//test if the field width is one third of the control column width
			//E.g. Vector3's x, y and z fields have this width
			int member3Index = GetControlIndexInRowWith3Controls(fullEditorWidth, prefixLabelWidth, rect);
			if(member3Index != -1)
			{
				float vertDiff = MathUtils.VerticalDifference(rect, prevRect);
				bool vertDiffMatch = vertDiff.Equals(0f) || vertDiff.Equals(10f); //10 is found in RectTransform at least
				float vertMult = vertDiffMatch ? 10f : 1f;

				switch(member3Index)
				{
					case 0:
						switch(prevMember3Index)
						{
							case 0:
								return result - 2500f * vertMult;
							case 1:
								return result - 1500f * vertMult;
							case 2:
								return result - 1250f * vertMult;
							default:
								if(prevRect.x.Equals(leftMargin))
								{
									return result - 2500f * vertMult;
								}
								return result - 1000f * vertMult;
						}
					case 1:
						switch(prevMember3Index)
						{
							case 1:
								return result - 2500f * vertMult;
							case 0:
							case 2:
								return result - 1500f * vertMult;
							default:
								return result - 1000f * vertMult;
						}
					case 2:
						switch(prevMember3Index)
						{
							case 2:
								return result - 2500f * vertMult;
							case 1:
								return result - 1500f * vertMult;
							case 0:
								return result - 1250f * vertMult;
							default:
								return result - 1000f * vertMult;
						}
				}
			}

			return result;
		}

		/// <summary>
		/// Gets the zero-based index of control in row where there are three controls.
		/// If three members are not drawn on same row with control, returns -1.
		/// </summary>
		/// <param name="fullEditorWidth"> Full width of the Editor view. </param>
		/// <param name="prefixLabelWidth"> Prefix label column width used when drawing Editor. </param>
		/// <param name="rect"> Position and dimensions of the control to test. </param>
		/// <returns> Zero-based index, or -1 if not applicable. </returns>
		private int GetControlIndexInRowWith3Controls(float fullEditorWidth, float prefixLabelWidth, Rect rect)
		{
			//RectTransform fields have double height
			if(rect.height.Equals(16f) || rect.height.Equals(32f))
			{
				float afterPrefix = prefixLabelWidth + prefixEndToControlStartOffset;
				if(rect.x >= afterPrefix)
				{
					float third = GetControlWidth(fullEditorWidth, prefixLabelWidth, 3);
					//width matches!
					if(MathUtils.Approximately(rect.width, third))
					{
						float localX = rect.x - afterPrefix;
						if(Mathf.Approximately(localX, 0f))
						{
							#if DEV_MODE && DEBUG_MULTIPLE_CONTROLS_PER_ROW
							Debug.Log("GetControlIndexInRowWith3Controls: 0");
							#endif
							return 0;
						}
						if(Mathf.Approximately(localX, third + multiControlOffset))
						{
							#if DEV_MODE && DEBUG_MULTIPLE_CONTROLS_PER_ROW
							Debug.Log("GetControlIndexInRowWith3Controls: 1");
							#endif
							return 1;
						}
						if(Mathf.Approximately(localX, third + third + multiControlOffset + multiControlOffset))
						{
							#if DEV_MODE && DEBUG_MULTIPLE_CONTROLS_PER_ROW
							Debug.Log("GetControlIndexInRowWith3Controls: 2");
							#endif
							return 2;
						}
						#if DEV_MODE && DEBUG_MULTIPLE_CONTROLS_PER_ROW
						Debug.Log("Mathf.Abs(rect.width - third) was < 0.001 but localX "+localX+ " was not equal to 0, "+ (third + multiControlOffset)+ " or "+(third + third + multiControlOffset + multiControlOffset));
						#endif
					}
				}
			}

			return -1;
		}

		/// <summary>
		/// Gets the zero-based index of control in row where there are three controls.
		/// If three members are not drawn on same row with control, returns -1.
		/// </summary>
		/// <param name="fullEditorWidth"> Full width of the Editor view. </param>
		/// <param name="prefixLabelWidth"> Prefix label column width used when drawing Editor. </param>
		/// <returns> Zero-based index, or -1 if not applicable. </returns>
		public int GetMouseoveredControlIndexInRowWith3Controls(float fullEditorWidth, float prefixLabelWidth)
		{
			float afterPrefix = prefixLabelWidth + prefixEndToControlStartOffset;
			var posX = Cursor.LocalPosition.x;
			if(posX >= afterPrefix)
			{
				float third = GetControlWidth(fullEditorWidth, prefixLabelWidth, 3);
				
				float endOfFirst = afterPrefix + third;
				if(posX < endOfFirst)
				{
					return 0;
				}

				float endOfSecond = endOfFirst + third;
				if(posX < endOfSecond)
				{
					return 1;
				}
				return 2;
			}
			return -1;
		}

		private float GetControlWidth(float fullEditorWidth, float prefixLabelWidth, int numberOfControlsOnRow)
		{
			float controlsColumnWidth = fullEditorWidth - prefixLabelWidth - prefixEndToControlStartOffset - rightMargin;
			return (controlsColumnWidth - multiControlOffset * (numberOfControlsOnRow - 1)) / numberOfControlsOnRow;
		}

		protected virtual float GetUpDownHeightMatchScore(Rect rect)
		{
			int heightWholeNumber = (int)rect.height;
			if(!rect.height.Equals(heightWholeNumber))
			{
				return 5000f;
			}
			
			switch(heightWholeNumber)
			{
				case 16:
					//most common control height
					return -5f;
				case 32:
					//two rows is quite common
					return -3f;
				case 42:
					//text area
					return -1f;
				default:
					if((rect.height % rowHeight).Equals(0f))
					{
						#if DEV_MODE && (DEBUG_GET_NEXT_CONTROL_UP || DEBUG_GET_NEXT_CONTROL_DOWN)
						Debug.Log("GetUpDownHeightMatchScore("+rect+"): -1 because divisible by "+rowHeight);
						#endif

						return -1f;
					}

					#if DEV_MODE && (DEBUG_GET_NEXT_CONTROL_UP || DEBUG_GET_NEXT_CONTROL_DOWN)
					Debug.Log("GetUpDownHeightMatchScore("+rect+"): 0 because not divisible by "+rowHeight);
					#endif

					return 0f;
			}
		}
		
		protected float GetLeftXMatchScore(float fullEditorWidth, float prefixLabelWidth, Rect prevRect, Rect rect, float matchX)
		{
			float result = GetXMatchScoreBase(fullEditorWidth, prefixLabelWidth, rect);
			if(matchX.Equals(0f))
			{
				result -= 11000f;
			}

			if(rect.x.Equals(leftMargin))
			{
				if(GetControlIndexInRowWith3Controls(fullEditorWidth, prefixLabelWidth, prevRect) == 0)
				{
					return result - 50000f;
				}
				return result - 1000f;
			}

			//test if the field width is one third of the control column width
			//E.g. Vector3's x, y and z fields have this width
			int member3Index = GetControlIndexInRowWith3Controls(fullEditorWidth, prefixLabelWidth, rect);
			if(member3Index != -1)
			{
				switch(member3Index)
				{
					case 0:
					case 1:
						if(GetControlIndexInRowWith3Controls(fullEditorWidth, prefixLabelWidth, prevRect) == member3Index - 1)
						{
							return result - 50000f;
						}
						return result + 50000f;
				}
			}
			return result;
		}

		protected virtual float GetRightXMatchScore(float fullEditorWidth, float prefixLabelWidth, Rect prevRect, Rect rect, float diffX)
		{
			float result = GetXMatchScoreBase(fullEditorWidth, prefixLabelWidth, rect);
			if(diffX.Equals(0f))
			{
				result -= 11000f;
			}

			//test if the field width is one third of the control column width
			//E.g. Vector3's x, y and z fields have this width
			int member3Index = GetControlIndexInRowWith3Controls(fullEditorWidth, prefixLabelWidth, rect);
			if(member3Index != -1)
			{
				switch(member3Index)
				{
					case 0:
						if(prevRect.x.Equals(leftMargin))
						{
							return result - 50000f;
						}
						return result + 50000f;
					default:
						if(GetControlIndexInRowWith3Controls(fullEditorWidth, prefixLabelWidth, prevRect) == member3Index + 1)
						{
							return result - 50000f;
						}
						return result + 50000f;
				}
			}
			return result;
		}

		protected virtual float GetXMatchScoreBase(float fullEditorWidth, float prefixLabelWidth, Rect rect)
		{
			if(rect.x > 18f && rect.x < prefixLabelWidth)
			{
				// new: support toggle field, where rect doesn't contain prefix portion
				if(rect.x.Equals(fullEditorWidth - leftMargin - prefixLabelWidth))
				{
					return 0f;
				}

				#if DEV_MODE && (DEBUG_GET_NEXT_CONTROL_UP || DEBUG_GET_NEXT_CONTROL_DOWN || DEBUG_GET_NEXT_CONTROL_LEFT || DEBUG_GET_NEXT_CONTROL_RIGHT)
				if(debug) { Debug.Log("XBase: rect "+rect+" returning "+50000f+" because rect.x ("+rect.x+") > 18 and not equal to "+(fullEditorWidth - leftMargin - prefixLabelWidth)); }
				#endif

				return 50000f;
			}
			return 0f;
		}

		/// <summary>
		/// Gets a score based on how close the control rect width is to being a perfect match
		/// in the context of figuring out the next field to gain focus when moving keyboard focus up or down.
		/// </summary>
		/// <param name="fullEditorWidth"> Full width of the Editor view. </param>
		/// <param name="prefixLabelWidth"> Prefix label column width used when drawing Editor. </param>
		/// <param name="prevControlWidth"> fullEditorWidth of previously selected control. </param>
		/// <param name="rect"> Position and dimensions of the control to test. </param>
		/// <returns> Score. Smaller is better. </returns>
		protected virtual float GetUpDownWidthMatchScore(float fullEditorWidth, float prefixLabelWidth, float prevControlWidth, Rect rect)
		{
			if(prevControlWidth.Equals(rect.width))
			{
				return -100f; //too much?
			}

			if(rect.x.Equals(leftMargin))
			{
				float widthWithoutLeftMargin = fullEditorWidth - leftMargin;
				float fullWidthDiff = widthWithoutLeftMargin - rect.width - rightMargin;
				if(fullWidthDiff > 1f || fullWidthDiff < 0f)
				{
					float widthWithoutPrefixLabel = widthWithoutLeftMargin - prefixLabelWidth;

					// new: support toggle field, where rect doesn't contain prefix portion
					if(rect.x.Equals(widthWithoutPrefixLabel))
					{
						return 0f;
					}

					#if DEV_MODE && DEBUG_SKIP_PHANTOM_CONTROL
					Debug.LogWarning("Phantom control?: "+rect+"\r\nfullWidth="+fullEditorWidth+", prefixLabelWidth="+prefixLabelWidth+"\r\nControlsLeftMargin="+leftMargin+", rightMargin="+rightMargin+"\r\nfullWidthDiff="+fullWidthDiff+"\r\nwidthWithoutPrefixLabel="+widthWithoutPrefixLabel+"\r\nwidthWithoutPrefixLabelDiff="+(rect.x - widthWithoutPrefixLabel));
					#endif
					return 10000f;
				}
			}

			return 0f;
		}

		/// <summary>
		/// Bounds inside which controls should be
		/// if they belong to this Editor
		/// </summary>
		/// <returns></returns>
		protected virtual Rect EditorBoundsForControls(ICustomEditorDrawer subject)
		{
			float totalHeightIncludingHeader = subject.Height;
			float headerHeight = subject.HeaderHeight;

			// if unolded or no controls inside body, return bounds with zero height.
			if(totalHeightIncludingHeader <= headerHeight)
			{
				return Rect.zero;
			}
			
			var bounds = subject.Bounds;
			
			bounds.width = subject.Width - leftMargin - rightMargin;
			bounds.x = leftMargin;

			float topOffset = headerHeight + prefixResizerHeight + topMargin;
			bounds.height = totalHeightIncludingHeader - topOffset - bottomMargin;
			bounds.y += topOffset;

			return bounds;
		}

		public void SelectNextFieldRight(ICustomEditorDrawer subject, SelectNextHeaderPart selectNextHeaderPartRight, SelectHeaderPart selectHeaderPart, int beforeHeaderControlId, int endControlId, bool useBuiltInFieldFocusingSystem, bool moveToNextControlAfterReachingEnd)
		{
			#if DEV_MODE
			#if DEBUG_GET_NEXT_CONTROL_RIGHT
			debug = true;
			#else
			debug = false;
			#endif
			#endif

			#if DEV_MODE
			if(debug) { Debug.Log(StringUtils.ToColorizedString(ToString(), ".SelectNextFieldRight(", moveToNextControlAfterReachingEnd, ") HeaderIsSelected=", subject.HeaderIsSelected, ", Foldable=", subject.Foldable, ", Unfolded=", subject.Unfolded, ", Height=", subject.Height, ", HeaderHeight=", subject.HeaderHeight)); }
			#endif

			//int previouslyFocusedControlId = KeyboardControlUtility.KeyboardControl;

			int idWas = KeyboardControlUtility.KeyboardControl;

			if(idWas == 0 || subject.HeaderIsSelected)
			{
				selectNextHeaderPartRight(moveToNextControlAfterReachingEnd);
				ScrollToShow(subject);
			}
			//if CustomEditor custom selection logic is enabled
			//and this is an arrow right action, not tab action
			//select next field to the right
			else if(!useBuiltInFieldFocusingSystem && !moveToNextControlAfterReachingEnd)
			{
				//SelectNextEditorControlRight(subject, selectNextHeaderPartRight, beforeHeaderControlId, endControlId, useBuiltInFieldFocusingSystem, selectHeaderPart);
				SelectNextEditorControlRight(subject, beforeHeaderControlId, endControlId);
			}
			//rely on Unity's internal selection logic for switching between fields
			//the only thing we need to do is detect when the selection has moved out
			//of the bounds of the Editor and react accordingly (move to next component)
			else
			{
				HandleSelectNextFieldRightLeavingBounds(moveToNextControlAfterReachingEnd, subject, selectHeaderPart);
			}
		}

		private void HandleSelectNextFieldRightLeavingBounds(bool moveToNextControlAfterReachingEnd, ICustomEditorDrawer subject, SelectHeaderPart selectHeaderPart)
		{
			int previouslyFocusedControlId = subject.Inspector.State.previousKeyboardControl;

			HandleSelectNextFieldLeavingBounds(subject, ()=>
			{
				#if DEV_MODE
				if(debug) { Debug.LogWarning(ToString()+ ".HandleSelectNextFieldRightLeavingBounds - control out of body bounds! keyboardControl=" + KeyboardControlUtility.KeyboardControl); }
				#endif

				if(moveToNextControlAfterReachingEnd)
				{
					selectHeaderPart(HeaderPart.ContextMenuIcon);
				}
				else
				{
					KeyboardControlUtility.KeyboardControl = previouslyFocusedControlId;
				}
			});
		}
		
		#if DEV_MODE
		public void DrawDebugVisualization(ICustomEditorDrawer subject, bool visualizeAll, int beforeHeaderControlId, int endControlId)
		{
			var bounds = subject.Bounds;
			if(bounds.height > 0f)
			{
				var pos = bounds;

				bool repaint = Event.current.type == EventType.Repaint;
				
				int focused = KeyboardControlUtility.KeyboardControl;

				if(repaint)
				{
					var color = Color.red;
					color.a = 0.5f;
					DrawGUI.Active.ColorRect(EditorBoundsForControls(subject), color);

					var text = new GUIContent(StringUtils.ToString(beforeHeaderControlId), "beforeHeaderControlId\nbounds: "+bounds);
					pos.width = InspectorPreferences.Styles.MiniButton.CalcSize(text).x;
					pos.height = subject.HeaderHeight;
					GUI.Label(pos, StringUtils.ToString(beforeHeaderControlId), InspectorPreferences.Styles.MiniButton);
					
					if(focused > 0)
					{
						var info = KeyboardControlUtility.Info;
						var rect = info.KeyboardRect;
						if(rect.width > 0f && rect.height > 0f)
						{
							color = Color.blue;
							color.a = 0.5f;
							DrawGUI.Active.ColorRect(rect, color);
						}
						else
						{
							rect.height = DrawGUI.SingleLineHeight;
							if(rect.y <= 0f)
							{
								rect.y = bounds.y + subject.HeaderHeight;
							}
						}

						text = new GUIContent(StringUtils.ToString(focused), "KeyboardControl\nKeyboardRect: "+rect);
						rect.width = InspectorPreferences.Styles.MiniButton.CalcSize(text).x;
						if(GUI.Button(rect, text, InspectorPreferences.Styles.MiniButton))
						{
							Debug.Log("KEYBOARD RECT: "+info.KeyboardRect);
						}
					}

					if(visualizeAll)
					{
						color = Color.white;
						color.a = 0.5f;
						for(int n = allControlRects.Count - 1; n >= 0; n--)
						{
							int id = allControlRects[n].Key;
							if(id != focused)
							{
								var rect = allControlRects[n].Value;
								DrawGUI.Active.ColorRect(rect, color);
								GUI.Label(rect, StringUtils.ToString(id));
							}
						}
					}
				}
				else if(visualizeAll && (Event.current.isMouse || Event.current.isKey))
				{
					subject.OnNextLayout(()=>subject.OnNextLayout(()=>UpdateAllControlRects(subject, beforeHeaderControlId, endControlId)));
				}

				if(repaint)
				{
					pos.y = bounds.yMax - DrawGUI.SingleLineHeight;
					pos.height = DrawGUI.SingleLineHeight;
					var text = new GUIContent(StringUtils.ToString(endControlId), "endControlId");
					pos.width = InspectorPreferences.Styles.MiniButton.CalcSize(text).x;
					GUI.Label(pos, text, InspectorPreferences.Styles.MiniButton);
				}
			}
		}
		#endif
		
		#if DEV_MODE
		private void UpdateAllControlRects(ICustomEditorDrawer subject, int beforeHeaderControlId, int endControlId)
		{
			allControlRects.Clear();

			if(!subject.Unfolded || subject.Height <= subject.HeaderHeight)
			{
				return;
			}

			int idWas = KeyboardControlUtility.KeyboardControl;
			
			// sometimes control IDs are higher than endControlID, it's not a very reliable method, unfortunately
			int start = subject.HeaderIsSelected || idWas <= beforeHeaderControlId ? beforeHeaderControlId + 1 : idWas + 1;
			if(start == idWas)
			{
				start++;
			}
			
			float fullEditorWidth = subject.Width;

			int stop = Mathf.Max(endControlId, idWas) + appendLastCheckedId;

			//UPDATE: had to add this to make things work with CameraInspector
			stop += 200;
			
			var editorBounds = EditorBoundsForControls(subject);

			Rect previousControlRect;
			
			// if no control inside Editor body is currently selected,
			// generate rect for imaginary control just above the first field
			// and use that as the previously selected control rect.
			if(idWas == 0 || subject.HeaderIsSelected)
			{
				var rectAboveEditor = editorBounds;
				rectAboveEditor.y -= rowHeight;
				rectAboveEditor.height = rowHeight;
				previousControlRect = rectAboveEditor;
			}
			else
			{
				previousControlRect = KeyboardControlUtility.Info.KeyboardRect;
			}

			int outOfBoundsCounter = 0;
			var rect = previousControlRect;

			for(int id = start; id < stop; id++)
			{
				KeyboardControlUtility.KeyboardControl = id;

				var nextRect = KeyboardControlUtility.Info.KeyboardRect;
				bool outOfBounds;
				if(GetShouldSkipControl(fullEditorWidth, rect, nextRect, editorBounds, id, out outOfBounds))
				{
					if(outOfBounds)
					{
						outOfBoundsCounter++;
						if(outOfBoundsCounter >= GetNextFieldMaxOutOfBoundsCount) //In NetworkTransform this was 41 before a valid field was found UPDATE: Was 50 for CameraDrawer
						{
							break;
						}
					}
					rect = nextRect;
					continue;
				}
				rect = nextRect;
				allControlRects.Add(new System.Collections.Generic.KeyValuePair<int, Rect>(id, rect));
			}

			KeyboardControlUtility.KeyboardControl = idWas;
		}
		#endif
	}
}
#endif