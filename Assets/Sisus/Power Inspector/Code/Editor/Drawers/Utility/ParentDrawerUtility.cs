#define ENABLE_UNFOLD_ANIMATIONS
#define ENABLE_UNFOLD_ANIMATIONS_ALPHA_TWEENING
#define SAFE_MODE

#define DEBUG_NULL_MEMBERS
//#define DEBUG_SET_UNFOLDED

#define DEBUG_UPDATE_VISIBLE_MEMBERS
#define DEBUG_UPDATE_VISIBLE_MEMBERS_STEPS

//#define DEBUG_MEMBER_VISIBILITY_CHANGED

//#define DEBUG_BUILD_INHERITED_FIELDS
//#define DEBUG_BUILD_INHERITED_PROPERTIES
//#define DEBUG_BUILD_INHERITED_METHODS

//#define DEBUG_SKIP_BUILD_INHERITED_FIELDS
//#define DEBUG_SKIP_BUILD_INHERITED_PROPERTIES
//#define DEBUG_SKIP_BUILD_INHERITED_METHODS

//#define DEBUG_NEXT_FIELD
//#define DEBUG_PASSES_SEARCH_FILTER

//#define DEBUG_MOUSEOVER_DETECTION

using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Sisus
{
	/// <summary>
	/// A utility class that contains many methods that are very useful for drawers that implement the IParent interface.
	/// 
	/// Centralizing these method implementations into a static class was done because there are two different base classes
	/// that implement IParent: ParentDrawer and FieldParentDrawer.
	/// 
	/// They are not implemented as extension methods neither, so that the functionality can easly be overridden by extending classes.
	///  </summary>
	public static class ParentDrawerUtility
	{
		public const int MaxMembersDrawnInSingleDraw = 4;
		private const int MaxDrawerDepth = 8;

		// DeclaredOnly causes inherited class members to be ignored which is useful when manually looping through base types
		public const BindingFlags BindingFlagsInstance = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
		public const BindingFlags BindingFlagsStatic = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly;
		public const BindingFlags BindingFlagsInstanceAndStatic = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

		/// <summary>
		/// Used when updating visible members to figure out which members should be pushed to the visible list
		/// </summary>
		public static bool ShouldShowInInspector(IParentDrawer subject, bool passedLastFilterCheck)
        {
			if(!passedLastFilterCheck)
			{
				if(subject.MembersAreVisible)
				{
					//var members = subject.VisibleMembers;
					var members = subject.MembersBuilt;
					for(int n = members.Length - 1; n >= 0; n--)
					{
						var memb = members[n];
						if(memb != null)
						{
							//if any member passed search filter
							//then parent should also pass the search filter
							//since otherwise the child would not be drawn
							if(memb.ShouldShowInInspector)
							{
								return true;
							}
						}
						#if DEV_MODE && DEBUG_NULL_MEMBERS
						else { Debug.LogError(subject.GetType().Name + ".ShowInInspector - null member at #"+n); }
						#endif
					}
				}
				return false;
			}
			return true;
		}

		public static float GetOptimalPrefixLabelWidth(IParentDrawer subject, int indentLevel, bool prefixMayOverlapPrefixSeparator = true)
		{
			#if DEV_MODE && DEBUG_GET_OPTIMAL_PREFIX_WIDTH
			Debug.Log(subject.ToString()+ ".GetOptimalPrefixLabelWidth with DrawInSingleRow=" + subject.DrawInSingleRow+ ", prefixMayOverlapPrefixSeparator="+ prefixMayOverlapPrefixSeparator+ ", MembersAreVisible="+ subject.MembersAreVisible+ "; VisibleMembers="+ subject.VisibleMembers.Length);
			#endif

			var label = subject.Label;

			if(subject.DrawInSingleRow)
			{
				if(label == null)
				{
					return 0f;
				}
				return DrawerUtility.GetOptimalPrefixLabelWidth(indentLevel, label, subject.HasUnappliedChanges);
			}
			
			float prefixWidth;

			//Most parent drawers (DataSet etc.) labels can extend past the prefix
			//separator line, and so there's no need to consider their label width
			if(prefixMayOverlapPrefixSeparator || label == null)
			{
				prefixWidth = 0f;
			}
			else
			{
				const float foldoutArrowWidth = DrawGUI.IndentWidth;
				prefixWidth = DrawerUtility.GetOptimalPrefixLabelWidth(indentLevel, label, subject.HasUnappliedChanges) + foldoutArrowWidth;
			}
			
			if(!subject.MembersAreVisible)
			{
				return prefixWidth;
			}

			indentLevel += subject.AppendIndentLevel;
			
			for(int n = subject.VisibleMembers.Length - 1; n >= 0; n--)
			{
				var memb = subject.VisibleMembers[n];
				if(memb != null)
				{
					float width = memb.GetOptimalPrefixLabelWidth(indentLevel);
					if(width > prefixWidth)
					{
						prefixWidth = width;
					}
				}
				#if DEV_MODE && DEBUG_NULL_MEMBERS
				else { Debug.LogError(subject.GetType().Name + ".GetOptimalPrefixLabelWidth - null member at #"+n); }
				#endif
			}

			prefixWidth = Mathf.Clamp(prefixWidth, DrawGUI.MinPrefixLabelWidth, DrawGUI.InspectorWidth - DrawGUI.MinControlFieldWidth);

			return prefixWidth;
		}

		public static bool DetectMouseover(IParentDrawer subject)
		{
			return DetectMouseover(subject, Event.current.mousePosition);
		}

		public static bool DetectMouseover(IParentDrawer subject, Vector2 mousePosition)
		{
			if(subject.Clickable && subject.ClickToSelectArea.Contains(mousePosition))
			{
				#if DEV_MODE && DEBUG_MOUSEOVER_DETECTION
				Debug.Log(StringUtils.ToColorizedString("DetectMouseover(", subject, "): Contains(mousePos): ", true, " with "+subject.VisibleMembers.Length+ " VisibleMembers...\nsubject.ClickToSelectArea=", subject.ClickToSelectArea, ", mousePosition=", mousePosition));
				#endif
				
				if(!subject.PrefixResizingEnabledOverControl)
				{
					var unityObject = subject.UnityObjectDrawer;
					if(unityObject != null && unityObject.PrefixResizerMouseovered)
					{
						#if DEV_MODE && DEBUG_MOUSEOVER_DETECTION
						Debug.Log(StringUtils.ToColorizedString("DetectMouseover(", subject, "): ", subject == unityObject, " because !PrefixResizingEnabledOverControl and PrefixResizerMouseovered"));
						#endif
						return subject == unityObject;
					}
				}

				if(DetectMouseoverForVisibleMembers(subject, mousePosition))
				{
					return false;
				}
				
				#if DEV_MODE && DEBUG_MOUSEOVER_DETECTION
				Debug.Log(StringUtils.ToColorizedString("DetectMouseover(", subject, "): ", true, "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!"));
				#endif

				return true;
			}

			#if DEV_MODE && DEBUG_MOUSEOVER_DETECTION
			if(!subject.Clickable) { Debug.Log(StringUtils.ToColorizedString("DetectMouseover(", subject, "): ", false, " because Clickable=", false,", with Selectable=", subject.Selectable, ", ShouldShowInInspector=", subject.ShouldShowInInspector)); }
			else { Debug.Log(StringUtils.ToColorizedString("DetectMouseover(", subject, "): ", false, " because !", subject.ClickToSelectArea, ".Contains(", mousePosition, ")")); }
			#endif

			return false;
		}

		public static bool DetectMouseoverForSubjectAndChildren(IParentDrawer subject, Vector2 mousePosition)
		{
			if(subject.Clickable && subject.ClickToSelectArea.Contains(mousePosition))
			{
				#if DEV_MODE && DEBUG_MOUSEOVER_DETECTION
				Debug.Log(StringUtils.ToColorizedString("DetectMouseoverForSubjectAndChildren(", subject, "): ", true, "! (via self)"));
				#endif
				return true;
			}

			#if DEV_MODE && DEBUG_MOUSEOVER_DETECTION
			Debug.Log(StringUtils.ToColorizedString("DetectMouseoverForSubjectAndChildren(", subject, "): ", subject.ClickToSelectArea, ".MouseIsOver: ", false, ", testing ", subject.VisibleMembers.Length, " VisibleMembers (out of ", subject.Members.Length, " total)..."));
			#endif

			return DetectMouseoverForVisibleMembers(subject, mousePosition);
		}

		public static bool DetectMouseoverForVisibleMembers(IParentDrawer subject, Vector2 mousePosition)
		{
			var visibleMembers = subject.VisibleMembers;
			
			for(int n = visibleMembers.Length - 1; n >= 0; n--)
			{
				var member = visibleMembers[n];
				if(member != null)
				{
					if(member.DetectMouseoverForSelfAndChildren(mousePosition))
					{
						#if DEV_MODE && DEBUG_MOUSEOVER_DETECTION
						Debug.Log(StringUtils.ToColorizedString("DetectMouseoverForVisibleMembers(", subject, "): ", true, "! (via visibleMembers[", (n+1), "/", visibleMembers.Length, "])"));
						#endif
						return true;
					}
					#if DEV_MODE && DEBUG_MOUSEOVER_DETECTION
					Debug.Log(StringUtils.ToColorizedString("DetectMouseoverForVisibleMembers(", subject, "): visibleMembers[", (n + 1), "/",visibleMembers.Length,"].DetectMouseoverForSelfAndChildren=", false, "..."));
					#endif
				}
				#if DEV_MODE && DEBUG_NULL_MEMBERS
				else { Debug.LogError(subject.GetType().Name + " / \""+subject.Name+"\".DetectMouseoverForVisibleMembers() - visibleMembers["+ (n + 1) + "/"+visibleMembers.Length+"] was null"); }
				#endif
			}

			return false;
		}

		public static void SetUnfolded([NotNull]IParentDrawer subject, bool setUnfolded, bool setChildrenAlso)
		{
			// new test to allow expanding unfolded components if setUnfolded is true
			// this way e.g. if Expandable is false due to there being a filter we can still unfold a target
			if(!subject.Foldable && (!setUnfolded || subject.Unfolded) && !setChildrenAlso)
			{
				#if DEV_MODE
				Debug.LogWarning(subject+".SetUnfolded("+setUnfolded+", setChildrenAlso="+setChildrenAlso+ ") called with subject.Expandable="+StringUtils.False+ ", subject.Unfolded="+StringUtils.ToColorizedString(subject.Unfolded));
				#endif

				return;
			}

			// Prevent folding things when there's a filter.
			if(!setUnfolded && InspectorUtility.ActiveInspector.HasFilterAffectingInspectedTargetContent)
			{
				#if DEV_MODE
				Debug.LogWarning(StringUtils.ToColorizedString("Ignoring ", subject, ".SetUnfolded(", StringUtils.False, ", setChildrenAlso=", setChildrenAlso, ") because HasFilter=", StringUtils.True));
				#endif
				return;
			}

			if(subject.Unfolded != setUnfolded || setChildrenAlso)
			{
				#if DEV_MODE && DEBUG_SET_UNFOLDED
				Debug.Log(StringUtils.ToColorizedString(subject.ToString(), ".SetUnfolded(setUnfolded=", setUnfolded, ", setChildrenAlso=", setChildrenAlso, ") with subject.Foldable=", subject.Foldable, ", subject.Inactive=", subject.Inactive));
				#endif

				if(subject.Foldable || (setUnfolded && !subject.Unfolded))
				{
					subject.Unfolded = setUnfolded;
				}
				#if DEV_MODE
				else if(!setChildrenAlso) { Debug.LogWarning(subject+".SetUnfolded("+setUnfolded+", setChildrenAlso="+setChildrenAlso+ ") called with subject.Expandable="+StringUtils.False+ ", subject.Unfolded="+StringUtils.ToColorizedString(subject.Unfolded)); }
				#endif

				if(setChildrenAlso)
				{
					Action<IDrawer> action;
					if(setUnfolded)
					{
						action = TryUnfoldWithChildren;
					}
					else
					{
						action = TryFoldWithChildren;
					}

					var membs = subject.VisibleMembers;
					for(int n = membs.Length - 1; n >= 0; n--)
					{
						var memb = membs[n];
						if(memb != null)
						{
							memb.ApplyInChildren(action);
						}
					}
				}

				subject.OnChildLayoutChanged();
				
				InspectorUtility.ActiveInspector.RefreshView();
			}
		}

		public static bool HandleKeyboardInput(IParentDrawer subject, Event inputEvent, KeyConfigs keys)
		{
			if(!subject.Foldable)
			{
				return false;
			}
			
			if(keys.collapseRecursively.DetectAndUseInput(inputEvent))
			{
				subject.SetUnfolded(false, true);
				return true;
			}
			if(keys.uncollapseRecursively.DetectAndUseInput(inputEvent))
			{
				subject.SetUnfolded(true, true);
				return true;
			}

			switch(inputEvent.keyCode)
			{
				case KeyCode.Return:
				case KeyCode.Space:
				case KeyCode.KeypadEnter:
					if(inputEvent.modifiers == EventModifiers.None)
					{
						GUI.changed = true;
						DrawGUI.Use(inputEvent);
						subject.SetUnfolded(!subject.Unfolded);
						return true;
					}
					return false;
				case KeyCode.LeftArrow:
					if(subject.Unfolded && inputEvent.modifiers == EventModifiers.FunctionKey)
					{
						GUI.changed = true;
						DrawGUI.Use(inputEvent);

						subject.SetUnfolded(false);
						return true;
					}
					return false;
				case KeyCode.RightArrow:
					if(!subject.Unfolded && inputEvent.modifiers == EventModifiers.FunctionKey)
					{
						GUI.changed = true;
						DrawGUI.Use(inputEvent);

						subject.SetUnfolded(true);
						return true;
					}
					return false;
				default:
					return false;
			}
		}

		/// <summary> Call this when all the members of the parent just changed from being invisible to being visible or vise versa. </summary>
		/// <param name="parent"> The parent drawer whose members' visibility changed. </param>
		/// <param name="membersAreNowVisible"> True if members became visible, false if members became invisible. </param>
		public static void OnMemberVisibilityChanged([NotNull]IParentDrawer parent, bool membersAreNowVisible)
		{
			#if DEV_MODE && DEBUG_MEMBER_VISIBILITY_CHANGED
			Debug.Log(StringUtils.ToColorizedString(parent.ToString(), ".OnMemberVisibilityChanged(unfolded=", membersAreNowVisible, ") with Unfoldedness=", parent.Unfoldedness));
			#endif

			#if DEV_MODE
			Debug.Assert(!parent.Inactive);
			#endif

			var membs = parent.Members;
			
			if(membersAreNowVisible)
			{
				for(int n = membs.Length - 1; n >= 0; n--)
				{
					#if DEV_MODE && PI_ASSERTATIONS
					if(membs[n] == null) { Debug.LogError(parent.ToString()+".Members["+n+"] was null!"); }
					#endif

					membs[n].ApplyInChildren(InvokeOnSelfOrParentBecameVisible);
				}
			}
			else
			{
				for(int n = membs.Length - 1; n >= 0; n--)
				{
					#if DEV_MODE && PI_ASSERTATIONS
					if(membs[n] == null) { Debug.LogError(parent.ToString()+".Members["+n+"] was null!"); }
					#endif

					membs[n].ApplyInChildren(InvokeOnBecameInvisible);
				}
			}
		}

		/// <summary>
		/// Call this when the members of the parent drawer that are visible have just changed.
		/// 
		/// This will handle calling OnSelfOrParentBecameVisible and OnBecameInvisible on the applicable members.
		/// </summary>
		/// <param name="parent"> The parent drawer whose members changed. </param>
		/// <param name="previouslyVisibleMembers"> The previously visible members. </param>
		/// <param name="nowVisibleMembers"> The now visible members. </param>
		public static void OnVisibleMembersChanged([NotNull]IParentDrawer parent, [NotNull]IDrawer[] previouslyVisibleMembers, [NotNull]IDrawer[] nowVisibleMembers)
		{
			#if SAFE_MODE || DEV_MODE
			if(previouslyVisibleMembers == null)
			{
				#if DEV_MODE
				Debug.LogError("OnVisibleMembersChanged("+parent+") called with previouslyVisibleMembers null");
				#endif
				return;
			}

			if(nowVisibleMembers == null)
			{
				#if DEV_MODE
				Debug.LogError("OnVisibleMembersChanged("+parent+") called with nowVisibleMembers null");
				#endif
				return;
			}
			#endif

			BroadcastOnBecameInvisibleEvents(parent, previouslyVisibleMembers, nowVisibleMembers);
			BroadcastOnSelfOrParentBecameVisibleEvents(parent, previouslyVisibleMembers, nowVisibleMembers);
		}

		/// <summary>
		/// Call this when the members of the parent drawer that are visible have just changed.
		/// 
		/// This will handle calling OnSelfOrParentBecameVisible and OnBecameInvisible on the applicable members.
		/// </summary>
		/// <param name="parent"> The parent drawer whose members changed. </param>
		/// <param name="previouslyVisibleMembers"> The previously visible members. </param>
		/// <param name="nowVisibleMembers"> The now visible members. </param>
		public static void BroadcastOnBecameInvisibleEvents([NotNull]IParentDrawer parent, [NotNull]IDrawer[] previouslyVisibleMembers, [NotNull]IDrawer[] nowVisibleMembers)
		{
			#if SAFE_MODE || DEV_MODE
			if(previouslyVisibleMembers == null)
			{
				#if DEV_MODE
				Debug.LogError("BroadcastOnBecameInvisibleEvents("+parent+") called with previouslyVisibleMembers null");
				#endif
				return;
			}

			if(nowVisibleMembers == null)
			{
				#if DEV_MODE
				Debug.LogError("BroadcastOnBecameInvisibleEvents("+parent+") called with nowVisibleMembers null");
				#endif
				return;
			}
			#endif

			for(int n = previouslyVisibleMembers.Length - 1; n >= 0; n--)
			{
				var previouslyVisible = previouslyVisibleMembers[n];
				
				#if SAFE_MODE || DEV_MODE
				if(previouslyVisible == null)
				{
					#if DEV_MODE
					Debug.LogError("BroadcastOnBecameInvisibleEvents("+parent+") called with previouslyVisibleMembers["+n+"] null!");
					#endif
					continue;
				}
				#endif
				

				// If the drawer has already been Disposed don't call OnBecameInvisible.
				if(previouslyVisible.Inactive)
				{
					continue;
				}

				bool becomeInvisible = Array.IndexOf(nowVisibleMembers, previouslyVisible) == -1;
				if(becomeInvisible)
				{
					previouslyVisible.OnBecameInvisible();
				}
			}
		}

		/// <summary>
		/// Call this when the members of the parent drawer that are visible have just changed.
		/// 
		/// This will handle calling OnSelfOrParentBecameVisible and OnBecameInvisible on the applicable members.
		/// </summary>
		/// <param name="parent"> The parent drawer whose members changed. </param>
		/// <param name="previouslyVisibleMembers"> The previously visible members. </param>
		/// <param name="nowVisibleMembers"> The now visible members. </param>
		public static void BroadcastOnSelfOrParentBecameVisibleEvents([NotNull]IParentDrawer parent, [NotNull]IDrawer[] previouslyVisibleMembers, [NotNull]IDrawer[] nowVisibleMembers)
		{
			#if SAFE_MODE || DEV_MODE
			if(previouslyVisibleMembers == null)
			{
				#if DEV_MODE
				Debug.LogError("BroadcastOnSelfOrParentBecameVisibleEvents("+parent+") called with previouslyVisibleMembers null");
				#endif
				return;
			}

			if(nowVisibleMembers == null)
			{
				#if DEV_MODE
				Debug.LogError("BroadcastOnSelfOrParentBecameVisibleEvents("+parent+") called with nowVisibleMembers null");
				#endif
				return;
			}
			#endif

			#if DEV_MODE && DEBUG_MEMBER_VISIBILITY_CHANGED
			Debug.Log(StringUtils.ToColorizedString(parent.ToString(), ".BroadcastOnSelfOrParentBecameVisibleEvents from "+previouslyVisibleMembers.Length+" to "+nowVisibleMembers.Length+" members (out of "+parent.Members.Length+" total)."));
			#endif

			#if DEV_MODE
			Debug.Assert(!parent.Inactive);
			#endif

			for(int n = nowVisibleMembers.Length - 1; n >= 0; n--)
			{
				var nowVisible = nowVisibleMembers[n];
				bool becomeVisible = Array.IndexOf(previouslyVisibleMembers, nowVisible) == -1;
				if(becomeVisible)
				{
					nowVisible.OnSelfOrParentBecameVisible();
				}
			}
		}

		/// <summary>
		/// Sets the members of the parent drawer, sets forceVisibleMembersChangedCall true
		/// and calls OnAfterMembersBuilt, which should trigger a rebuild of visible members.
		/// </summary>
		/// <param name="parent"> The parent drawer whose members are being changed. </param>
		/// <param name="members"> [in,out] The current members of the parent drawer. </param>
		/// <param name="setMembers"> The new members for parent drawer. </param>
		/// <param name="visibleMembers"> [in,out] The visible members. </param>
		/// <param name="memberBuildState"> [in,out] State of the member build. </param>
		/// <param name="forceVisibleMembersChangedCall"> [in,out] True to force visible members changed
		/// 											  call. </param>
		/// <param name="updateCachedValuesFor"> The update cached values for. </param>
		/// <param name="sendVisibilityChangedEvents">
		/// True to broadcast events OnBecameInvisible and OnSelfOrParentBecameVisible events to applicable members.
		/// This should generally speaking be false during Setup and Dispose phases and otherwise true.
		/// </param>
		public static void SetMembers([NotNull]IParentDrawer parent, [NotNull]ref IDrawer[] members, [NotNull]IDrawer[] setMembers, [NotNull]ref IDrawer[] visibleMembers, ref MemberBuildState memberBuildState, ref bool forceVisibleMembersChangedCall, List<IDrawer> updateCachedValuesFor, bool sendVisibilityChangedEvents)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(!sendVisibilityChangedEvents || setMembers.Length > 0);
			#endif

			var previouslyVisibleMembers = visibleMembers;

			// Clear visible members first, so that when members are set the OnParentAssigned won't cause side effects.
			SetVisibleMembers(parent, ref visibleMembers, ArrayPool<IDrawer>.ZeroSizeArray, updateCachedValuesFor, false, false);

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(!previouslyVisibleMembers.ContainsNullMembers());
			#endif

			// Make sure that OnVisibleMembersChanged gets called, even if there are no visible members
			forceVisibleMembersChangedCall = true;
			
			SetMembersInternal(parent, ref members, setMembers);

			memberBuildState = MemberBuildState.MembersBuilt;

			parent.OnAfterMembersBuilt();

			if(sendVisibilityChangedEvents)
			{
				BroadcastOnBecameInvisibleEvents(parent, previouslyVisibleMembers, visibleMembers);
			}

			if(previouslyVisibleMembers != visibleMembers)
			{
				DrawerArrayPool.Dispose(ref previouslyVisibleMembers, false);
			}
			#if DEV_MODE
			else if(visibleMembers.Length > 0) { Debug.LogWarning("SetMembers("+StringUtils.ToString(setMembers)+") - Won't dispose previouslyVisibleMembers " + StringUtils.ToString(previouslyVisibleMembers)+" because they match new visibleMembers."); }
			#endif
		}

		/// <summary>
		/// Sets the members and visible members of the parent drawer simultaneously.
		/// If visible members changed, calls OnVisibleMembersChanged and OnChildLayoutChanged.
		/// </summary>
		/// <param name="setMembers"> The new members. </param>
		/// <param name="setVisibleMembers"> The new visible members. </param>
		/// <param name="sendVisibilityChangedEvents">
		/// True to broadcast events OnBecameInvisible and OnSelfOrParentBecameVisible events to applicable members.
		/// This should generally speaking be false during Setup and Dispose phases and otherwise true.
		/// </param>
		public static void SetMembers([NotNull]IParentDrawer parent, [NotNull]ref IDrawer[] members, [NotNull]IDrawer[] setMembers, [NotNull]ref IDrawer[] visibleMembers, [NotNull]IDrawer[] setVisibleMembers, ref MemberBuildState memberBuildState, List<IDrawer> updateCachedValuesFor, bool sendVisibilityChangedEvents)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(!sendVisibilityChangedEvents || setMembers.Length > 0);
			#endif

			bool visibleMembersChanged;
			if(ReferenceEquals(visibleMembers, setVisibleMembers))
			{
				visibleMembersChanged = false;
			}
			else
			{
				visibleMembersChanged = visibleMembers.ContentsMatch(setVisibleMembers);			
				SetVisibleMembers(parent, ref visibleMembers, setVisibleMembers, updateCachedValuesFor, sendVisibilityChangedEvents && visibleMembersChanged, true);
			}
			
			SetMembersInternal(parent, ref members, setMembers);
			
			memberBuildState = MemberBuildState.MembersBuilt;

			if(visibleMembersChanged)
			{
				parent.OnVisibleMembersChanged();
				parent.OnChildLayoutChanged();
			}
		}

		private static void SetMembersInternal([NotNull]IParentDrawer parent, [NotNull]ref IDrawer[] members, [NotNull]IDrawer[] setMembers)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(setMembers != null);
			Debug.Assert(setMembers != parent.VisibleMembers || setMembers == null || setMembers.Length == 0, parent.ToString()+ " members array is same as visible members - This can lead to bugs!");
			Debug.Assert(!setMembers.ContainsNullMembers());
			#endif

			if(members == setMembers)
			{
				return;
			}

			// Dispose only previous members that are not contained in the new set of members.
			for(int n = members.Length - 1; n >= 0; n--)
			{
				var member = members[n];
				if(member != null && !member.Inactive && Array.IndexOf(setMembers, member) == -1)
				{
					member.Dispose();
				}
			}

			// Don't dispose contents as it was already handled above.
			DrawerArrayPool.Dispose(ref members, false);

			members = setMembers;

			SendOnParentAssignedEvents(parent, members);
		}

		/// <summary> Sets visible members. </summary>
		/// <param name="newVisibleMembers"> The new visible members. </param>
		/// <param name="sendVisibilityChangedEvents">
		/// True to broadcast events OnBecameInvisible and OnSelfOrParentBecameVisible events to applicable members.
		/// This should generally speaking be false during Setup and Dispose phases and otherwise true.
		/// </param>
		public static void SetVisibleMembers([NotNull]IParentDrawer parent, [NotNull]ref IDrawer[] visibleMembers, [NotNull]IDrawer[] newVisibleMembers, [NotNull]List<IDrawer> updateCachedValuesFor, bool sendVisibilityChangedEvents, bool disposePreviousArray)
		{
			if(visibleMembers != newVisibleMembers)
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(newVisibleMembers != null);
				Debug.Assert(newVisibleMembers != parent.Members || newVisibleMembers.Length == 0, parent.ToString() + " member value equals visible members value - this can lead to bugs!");
				Debug.Assert(!newVisibleMembers.ContainsNullMembers());
				Debug.Assert(!sendVisibilityChangedEvents || !parent.Inactive);
				Debug.Assert(!sendVisibilityChangedEvents || visibleMembers != null, "visibleMembers were null and sendVisibilityChangedEvents was true");
				#endif

				var previouslyVisibleMembers = visibleMembers;
				visibleMembers = newVisibleMembers;

				if(sendVisibilityChangedEvents)
				{
					#if DEV_MODE && PI_ASSERTATIONS
					Debug.Assert(previouslyVisibleMembers != null);
					#endif
					OnVisibleMembersChanged(parent, previouslyVisibleMembers, newVisibleMembers);
				}

				if(disposePreviousArray)
				{
					// Don't dispose previously visible members, they might still be used in the members array
					DrawerArrayPool.Dispose(ref previouslyVisibleMembers, false);
				}

				updateCachedValuesFor.Clear();
			}
		}

		private static void InvokeOnSelfOrParentBecameVisible([NotNull]IDrawer member)
		{
			member.OnSelfOrParentBecameVisible();
		}

		private static void InvokeOnBecameInvisible([NotNull]IDrawer member)
		{
			member.OnBecameInvisible();
		}

		private static void TryUnfoldWithChildren([CanBeNull]IDrawer member)
		{
			var parent = member as IParentDrawer;
			if(parent != null)
			{
				parent.SetUnfolded(true, true);
			}
		}

		private static void TryFoldWithChildren([CanBeNull]IDrawer member)
		{
			var parent = member as IParentDrawer;
			if(parent != null)
			{
				parent.SetUnfolded(false, true);
			}
		}

		public static bool DrawBody([NotNull]IParentDrawer target, Rect bodyDrawPosition)
		{
			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.BeginSample("ParentDrawerUtility.DrawBody");
			#endif
			
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(bodyDrawPosition.width > 0f, target + ".DrawBody called with bodyDrawPosition "+bodyDrawPosition);
			#endif

			bool dirty;
			if(target.Unfoldedness <= 0f)
			{
				dirty = false;
			}
			else if(target.DrawInSingleRow)
			{
				dirty = target.DrawBodySingleRow(bodyDrawPosition);
			}
			else
			{
				dirty = target.DrawBodyMultiRow(bodyDrawPosition);
			}
			
			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.EndSample();
			#endif

			return dirty;
		}

		/// <summary>
		/// Draws the body on a single row.
		/// E.g. Vector2Drawer.
		/// </summary>
		public static bool DrawBodySingleRow([NotNull]IParentDrawer target, Rect position)
		{
			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.BeginSample("ParentDrawer.DrawBodySingleRow");
			#endif

			var label = target.Label;

			if(InspectorUtility.Preferences.enableTooltipIcons && label.tooltip.Length > 0)
			{
				var hintPos = position;
				hintPos.x -= DrawGUI.SingleLineHeight;
				hintPos.width = DrawGUI.SingleLineHeight;
				DrawGUI.Active.HintIcon(hintPos, label.tooltip);
			}
			
			bool dirty = false;
			
			var visibleMembers = target.VisibleMembers;

			int memberCount = visibleMembers.Length;
			if(memberCount > 0)
			{
				#if DEV_MODE && PI_ASSERTATIONS
				if(memberCount > 4){ Debug.LogError(target+".DrawBodySingleRow called with memberCount "+memberCount+" > 4:\n"+StringUtils.ToString(visibleMembers, "\n")); }
				#endif

				const float padding = 3f;
				float totalWidth = (position.width - padding * (memberCount - 1));
				int remainder = (int)totalWidth % memberCount;
				int memberWidth = Mathf.FloorToInt(totalWidth / memberCount);
				
				#if DEV_MODE && PI_ASSERTATIONS
				if(totalWidth < 0f){ Debug.LogError(target+".DrawBodySingleRow totalWidth (" + totalWidth + ") < 0f, with position="+position+", memberCount="+memberCount+", memberWidth="+memberWidth); }
				if(memberWidth <= 0f){ Debug.LogError(target+".DrawBodySingleRow memberWidth (" + memberWidth + ") <= 0f, with position="+position+", memberCount="+memberCount+", totalWidth="+totalWidth); }
				#endif

				for(int n = 0; n < memberCount; n++)
				{
					position.width = memberWidth;
					if(remainder > 0)
					{
						position.width += 1f;
						remainder -= 1;
					}

					var member = visibleMembers[n];
					
					if(member == null)
					{
						Debug.LogError(target+".DrawBodySingleRow visibleMembers[" + n + "] was null! Did the members of parent "+(target.Parent == null ? "null" : target.Parent.ToString())+" change during the for loop?\nvisibleMembers=" + StringUtils.ToString(visibleMembers)+"\nmembers="+ StringUtils.ToString(target.Members));
						position.x += position.width + padding;
						continue;
					}
					
					if(member.Draw(position))
					{
						dirty = true;

						visibleMembers = target.VisibleMembers;
						memberCount = visibleMembers.Length;
					}

					position.x += position.width + padding;
				}
				
				#if DEV_MODE || PROFILE_POWER_INSPECTOR
				Profiler.EndSample();
				#endif
			}
			return dirty;
		}

		/// <summary>
		/// Draws the body on a single row. E.g. Vector2Drawer.
		/// </summary>
		public static bool DrawBodySingleRow([NotNull]IParentDrawer target, Rect position1, Rect position2)
		{
			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.BeginSample("ParentDrawer.DrawBodySingleRow(2)");
			#endif

			HandleTooltipBeforeControl(target.Label, position1);
			
			bool dirty = false;
			
			var members = target.Members;

			int memberCount = members.Length;
			if(memberCount > 0)
			{
				var member = members[0];
				if(member == null)
				{
					#if DEV_MODE
					Debug.LogError(target+".DrawBodySingleRow members[0] was null! target.Parent="+(target.Parent == null ? "null" : target.Parent.ToString())+"\nmembers="+ StringUtils.ToString(target.Members));
					#endif
				}
				else if(member.ShouldShowInInspector && member.Draw(position1))
				{
					dirty = true;
					members = target.Members;
					memberCount = members.Length;
				}

				if(memberCount > 1)
				{
					member = members[1];
					if(member == null)
					{
						#if DEV_MODE
						Debug.LogError(target+".DrawBodySingleRow members[0] was null! target.Parent="+(target.Parent == null ? "null" : target.Parent.ToString())+"\nmembers="+ StringUtils.ToString(target.Members));
						#endif
					}
					else if(member.ShouldShowInInspector && member.Draw(position2))
					{
						dirty = true;
					}
				}
			}
			
			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.EndSample();
			#endif
			return dirty;
		}
		
		public static void HandleTooltipBeforeControl([NotNull]GUIContent label, Rect beforeControlPosition)
		{
			if(label.tooltip.Length > 0 && InspectorUtility.Preferences.enableTooltipIcons)
			{
				var hintPos = beforeControlPosition;
				hintPos.x -= DrawGUI.SingleLineHeight;
				hintPos.width = DrawGUI.SingleLineHeight;
				DrawGUI.Active.HintIcon(hintPos, label.tooltip);
			}
		}

		public static void HandleErrorOrWarningBeforeControl([NotNull]GUIContent errorOrWarningLabel, Rect beforeControlPosition, bool isWarning)
		{
			if(errorOrWarningLabel.tooltip.Length > 0)
			{
				var iconRect = beforeControlPosition;
				iconRect.x -= DrawGUI.SingleLineHeight;
				iconRect.width = DrawGUI.SingleLineHeight;

				if(DrawGUI.Active.Button(iconRect, errorOrWarningLabel, InspectorPreferences.Styles.Label))
				{
					if(Event.current.button == 0)
					{
						Clipboard.Copy(errorOrWarningLabel.tooltip);
						if(isWarning)
						{
							Clipboard.SendCopyToClipboardMessage("Warning message");
						}
						else
						{
							Clipboard.SendCopyToClipboardMessage("Error message");
						}
					}
					else if(Event.current.button == 1)
					{
						var menu = Menu.Create();
						if(isWarning)
						{
							menu.Add("Copy Warning", () => Clipboard.Copy(errorOrWarningLabel.tooltip));
							Clipboard.SendCopyToClipboardMessage("Warning message");
						}
						else
						{
							menu.Add("Copy Error", ()=>Clipboard.Copy(errorOrWarningLabel.tooltip));
							Clipboard.SendCopyToClipboardMessage("Error message");
						}
						menu.OpenAt(iconRect);
					}
				}
			}
		}

		public static void HandleTooltipAt([NotNull]GUIContent label, Rect beforeControlPosition)
		{
			if(label.tooltip.Length > 0 && InspectorUtility.Preferences.enableTooltipIcons)
			{
				var hintPos = beforeControlPosition;
				hintPos.x -= DrawGUI.SingleLineHeight;
				hintPos.width = DrawGUI.SingleLineHeight;
				DrawGUI.Active.HintIcon(hintPos, label.tooltip);
			}
		}

		/// <summary>
		/// Draws the body on a single row.
		/// E.g. Vector2Drawer.
		/// </summary>
		public static bool DrawBodySingleRow([NotNull]IParentDrawer target, Rect position1, Rect position2, Rect position3)
		{
			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.BeginSample("ParentDrawer.DrawBodySingleRow(3)");
			#endif

			HandleTooltipBeforeControl(target.Label, position1);

			bool dirty = false;
			
			IDrawer[] members;
			bool draw1, draw2, draw3;
			UpdateMembersToDraw(target, out members, out draw1, out draw2, out draw3);
			
			if(draw1 && members[0].Draw(position1))
			{
				dirty = true;
				UpdateMembersToDraw(target, out members, out draw1, out draw2, out draw3);
			}

			if(draw2 && members[1].Draw(position2))
			{
				dirty = true;
				UpdateMembersToDraw(target, out members, out draw1, out draw2, out draw3);
			}

			if(draw3 && members[2].Draw(position3))
			{
				dirty = true;
			}

			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.EndSample();
			#endif
			return dirty;
		}

		/// <summary>
		/// Draws the body on a single row.
		/// E.g. Vector2Drawer.
		/// </summary>
		public static bool DrawBodySingleRow([NotNull]IParentDrawer target, Rect[] memberRects)
		{
			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.BeginSample("ParentDrawer.DrawBodySingleRow(n)");
			#endif

			bool dirty = false;

			var members = target.VisibleMembers;
			for(int n = 0, count = memberRects.Length; n < count; n++)
			{
				var rect = memberRects[n];
				if(n == 0)
				{
					HandleTooltipBeforeControl(target.Label, rect);
				}
				if(members[n].Draw(rect))
				{
					dirty = true;
				}
			}

			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.EndSample();
			#endif

			return dirty;
		}

		public static void UpdateMembersToDraw([NotNull]IParentDrawer parent, [NotNull]out IDrawer[] members, out bool draw1, out bool draw2)
		{
			members = parent.Members;
			switch(members.Length)
			{
				case 0:
					draw1 = false;
					draw2 = false;
					return;
				case 1:
					try
					{
						draw1 = members[0].ShouldShowInInspector;
					}
					catch(NullReferenceException)
					{
						draw1 = false;
						Debug.LogError(parent+ ".UpdateMembersToDraw(2) members[0] was null!\nparent.Parent=" + StringUtils.ToString(parent.Parent)+"\nmembers="+ StringUtils.ToString(members));
					}
					draw2 = false;
					return;
				default:
					try
					{
						draw1 = members[0].ShouldShowInInspector;
					}
					catch(NullReferenceException)
					{
						draw1 = false;
						Debug.LogError(parent + ".UpdateMembersToDraw(2) members[0] was null!\nparent.Parent=" + StringUtils.ToString(parent.Parent) + "\nmembers=" + StringUtils.ToString(members));
					}
					try
					{
						draw2 = members[1].ShouldShowInInspector;
					}
					catch(NullReferenceException)
					{
						draw2 = false;
						Debug.LogError(parent+ ".UpdateMembersToDraw(2) members[1] was null!\nparent.Parent=" + StringUtils.ToString(parent.Parent)+"\nmembers="+ StringUtils.ToString(members));
					}
					return;
			}
		}

		public static void UpdateMembersToDraw([NotNull]IParentDrawer parent, [NotNull]out IDrawer[] members, out bool draw1, out bool draw2, out bool draw3)
		{
			members = parent.Members;
			switch(members.Length)
			{
				case 0:
					draw1 = false;
					draw2 = false;
					draw3 = false;
					return;
				case 1:
					try
					{
						draw1 = members[0].ShouldShowInInspector;
					}
					catch(NullReferenceException)
					{
						draw1 = false;
						Debug.LogError(parent+ ".UpdateMembersToDraw(3) members[0] was null!\nparent.Parent=" + StringUtils.ToString(parent.Parent)+"\nmembers="+ StringUtils.ToString(members));
					}
					draw2 = false;
					draw3 = false;
					return;
				case 2:
					try
					{
						draw1 = members[0].ShouldShowInInspector;
					}
					catch(NullReferenceException)
					{
						draw1 = false;
						Debug.LogError(parent + ".UpdateMembersToDraw(3) members[0] was null!\nparent.Parent=" + StringUtils.ToString(parent.Parent) + "\nmembers=" + StringUtils.ToString(members));
					}
					try
					{
						draw2 = members[1].ShouldShowInInspector;
					}
					catch(NullReferenceException)
					{
						draw2 = false;
						Debug.LogError(parent+ ".UpdateMembersToDraw(3) members[1] was null!\nparent.Parent=" + StringUtils.ToString(parent.Parent)+"\nmembers="+ StringUtils.ToString(members));
					}
					draw3 = false;
					return;
				default:
					try
					{
						draw1 = members[0].ShouldShowInInspector;
					}
					catch(NullReferenceException)
					{
						draw1 = false;
						Debug.LogError(parent + ".UpdateMembersToDraw(3) members[0] was null!\nparent.Parent=" + StringUtils.ToString(parent.Parent) + "\nmembers=" + StringUtils.ToString(members));
					}
					try
					{
						draw2 = members[1].ShouldShowInInspector;
					}
					catch(NullReferenceException)
					{
						draw2 = false;
						Debug.LogError(parent + ".UpdateMembersToDraw(3) members[1] was null!\nparent.Parent=" + StringUtils.ToString(parent.Parent) + "\nmembers=" + StringUtils.ToString(members));
					}
					try
					{
						draw3 = members[2].ShouldShowInInspector;
					}
					catch(NullReferenceException)
					{
						draw3 = false;
						Debug.LogError(parent + ".UpdateMembersToDraw(3) members[2] was null!\nparent.Parent=" + StringUtils.ToString(parent.Parent) + "\nmembers=" + StringUtils.ToString(members));
					}
					return;
			}
		}

		public static float CalculateHeight([NotNull]IParentDrawer target)
		{
			float unfoldedness = target.Unfoldedness;

			if(unfoldedness <= 0f || target.DrawInSingleRow)
			{
				return target.HeaderHeight;
			}

			float membersHeight = 0f;
			var visibleMembers = target.VisibleMembers;
			for(int n = visibleMembers.Length - 1; n >= 0; n--)
			{
				membersHeight += visibleMembers[n].Height;
			}
			return target.HeaderHeight + membersHeight * unfoldedness;
		}

		/// <summary>
		/// Draws members of target one on top of each other on multiple rows.
		/// E.g. ArraysDrawer.
		/// </summary>
		/// <param name="target"> Target whose body should be drawn. </param>
		/// <param name="position"> The position at which the body should be drawn. </param>
		/// <returns>
		/// True if GUI changed, false if not.
		/// </returns>
		public static bool DrawBodyMultiRow([NotNull]IParentDrawer target, Rect position)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			if(position.width <= 0f && DrawGUI.PrefixLabelWidth > DrawGUI.LeftPadding) { Debug.LogError("DrawBodyMultiRow("+target.ToString()+") position.width <= 0f: "+position);	}
			#endif

			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.BeginSample("ParentDrawer.DrawBodyMultiRow");
			#endif

			bool dirty = false;

			#if GROUP_MEMBERS_INSIDE_BOX
			var boxRect = position;
			boxRect.height = Height - HeaderHeight;
			DrawGUI.AddMarginsAndIndentation(ref boxRect);
			//GUI.Box(boxRect, GUIContent.none, UnityEditor.EditorStyles.helpBox);
			#endif

			int appendIndentLevel = target.AppendIndentLevel;

			DrawGUI.IndentLevel += appendIndentLevel;
			{
				float unfoldedness = target.Unfoldedness;

				if(unfoldedness >= 1f)
				{
					DrawFoldableContent(target, position);
				}
				else
				{
					using(new MemberScaler(position.min, target.Unfoldedness))
					{
						DrawFoldableContent(target, position);
					}
				}
			}
			DrawGUI.IndentLevel -= appendIndentLevel;
			
			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.EndSample();
			#endif

			return dirty;
		}

		/// <summary>
		/// Draws everything of multi-row parent drawer that will be hidden when unfolded.
		/// Does not handle things like appending indent level or scaling members, just
		/// draws the GUI content that is shown when unfolded. Use DrawBodyMultiRow instead
		/// to handle everything related to drawing the body of a multi-row parent drawer.
		/// </summary>
		/// <param name="target"> Target whose body should be drawn. </param>
		/// <param name="position"> The position at which the body should be drawn. </param>
		/// <returns>
		/// True if GUI changed, false if not.
		/// </returns>
		public static bool DrawFoldableContent(IParentDrawer target, Rect position)
		{
			bool dirty = false;

			var inspector = InspectorUtility.ActiveInspector;
			bool hasInspector = inspector != null;

			var visibleMembers = target.VisibleMembers;
			int count = visibleMembers.Length;

			int n = 0;
			while(n < count)
			{
				float height = visibleMembers[n].Height;
				position.height = height;
					
				//don't draw controls that are off-screen for performance reasons
				if(hasInspector && !inspector.IsAboveViewport(position.yMax))
				{
					break;
				}

				position.y += height;
				n++;
			}

			try
			{
				while(n < count)
				{
					var member = visibleMembers[n];

					if(member == null)
					{
						Debug.LogError(target+ ".DrawBodyMultiRow visibleMembers[" + n + "] was null! Did the members of parent "+(target.Parent == null ? "null" : target.Parent.ToString())+" change during the for loop?\nvisibleMembers=" + StringUtils.ToString(visibleMembers)+"\ntarget.VisibleMembers="+target.VisibleMembers+"\ntarget.Members="+ StringUtils.ToString(target.Members));
						position.height += DrawGUI.SingleLineHeight;
						continue;
					}

					//don't draw controls that are off-screen for performance reasons
					position.height = member.Height;
				
					if(hasInspector && inspector.IsBelowViewport(position.y))
					{
						break;
					}
				
					GUI.changed = false;

					if(member.Draw(position) || GUI.changed)
					{
						dirty = true;
						GUI.changed = true;
						visibleMembers = target.VisibleMembers;
						count = visibleMembers.Length;
					}
					position.y += position.height;
					n++;
				}
			}
			catch(Exception e)
			{
				if(ExitGUIUtility.ShouldRethrowException(e))
				{
					throw;
				}

				#if DEV_MODE
				Debug.LogWarning(e);
				#endif
			}

			#if GROUP_MEMBERS_INSIDE_BOX
			DrawGUI.DrawRect(boxRect, new Color(1f, 1f, 1f, 0.1f));
			boxRect.x += 1f;
			boxRect.y += 1f;
			boxRect.width -= 2f;
			boxRect.height -= 2f;
			DrawGUI.DrawRect(boxRect, new Color(0f, 0f, 0f, 0.1f));
			#endif

			return dirty;
		}

		/// <summary>
		/// Draws members of target one on top of each other on multiple rows.
		/// E.g. ArraysDrawer.
		/// </summary>
		/// <param name="target"> Target whose body should be drawn. </param>
		/// <param name="position"> The position at which the body should be drawn. </param>
		/// <returns>
		/// True if it succeeds, false if it fails.
		/// </returns>
		public static bool DrawBodyMultiRowDuringUnfolding([NotNull]IParentDrawer target, Rect position)
		{
			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.BeginSample("ParentDrawer.DrawBodyMultiRow");
			#endif

			bool dirty = false;

			#if GROUP_MEMBERS_INSIDE_BOX
			var boxRect = position;
			boxRect.height = Height - HeaderHeight;
			DrawGUI.AddMarginsAndIndentation(ref boxRect);
			//GUI.Box(boxRect, GUIContent.none, UnityEditor.EditorStyles.helpBox);
			#endif

			var inspector = InspectorUtility.ActiveInspector;
			bool hasInspector = inspector != null;

			int appendIndentLevel = target.AppendIndentLevel;

			DrawGUI.IndentLevel += appendIndentLevel;
			{
				var visibleMembers = target.VisibleMembers;
				int count = visibleMembers.Length;

				int n = 0;
				while(n < count)
				{
					float height = visibleMembers[n].Height;
					position.height = height;

					//don't draw controls that are off-screen for performance reasons
					if(hasInspector && !inspector.IsAboveViewport(position.yMax))
					{
						break;
					}

					position.y += height;
					n++;
				}

				while(n < count)
				{
					var member = visibleMembers[n];

					if(member == null)
					{
						Debug.LogError(target+ ".DrawBodyMultiRow visibleMembers[" + n + "] was null! Did the members of parent "+(target.Parent == null ? "null" : target.Parent.ToString())+" change during the for loop?\nvisibleMembers=" + StringUtils.ToString(visibleMembers)+"\ntarget.VisibleMembers="+target.VisibleMembers+"\ntarget.Members="+ StringUtils.ToString(target.Members));
						position.height += DrawGUI.SingleLineHeight;
						continue;
					}

					//don't draw controls that are off-screen for performance reasons
					position.height = member.Height;
					
					if(hasInspector && inspector.IsBelowViewport(position.y))
					{
						break;
					}
					
					GUI.changed = false;
					if(member.Draw(position) || GUI.changed)
					{
						dirty = true;
						GUI.changed = true;
						visibleMembers = target.VisibleMembers;
						count = visibleMembers.Length;
					}
					position.y += position.height;
					n++;
				}

				#if GROUP_MEMBERS_INSIDE_BOX
				DrawGUI.DrawRect(boxRect, new Color(1f, 1f, 1f, 0.1f));
				boxRect.x += 1f;
				boxRect.y += 1f;
				boxRect.width -= 2f;
				boxRect.height -= 2f;
				DrawGUI.DrawRect(boxRect, new Color(0f, 0f, 0f, 0.1f));
				#endif
			}
			DrawGUI.IndentLevel -= appendIndentLevel;
			
			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.EndSample();
			#endif

			return dirty;
		}

		public static IDrawer GetNextSelectableDrawerLeft([NotNull]IParentDrawer subject, bool moveToNextControlAfterReachingEnd, IDrawer requester, bool membersListedVertically)
		{
			var result = GetNextInternalSelectableFieldLeftInternal(subject, moveToNextControlAfterReachingEnd, requester, membersListedVertically);

			if(result != null && result.Selectable)
			{
				return result;
			}

			if(requester != null && requester.Selectable)
			{
				#if DEV_MODE && DEBUG_NEXT_FIELD
				Debug.Log("GetNextSelectableDrawerLeft returning requester ("+requester+") because result ("+result+") was "+(result == null ? StringUtils.Null : "not selectable"));
				#endif
				return requester;
			}

			#if DEV_MODE && DEBUG_NEXT_FIELD
			Debug.Log("GetNextSelectableDrawerLeft returning "+StringUtils.Null+" because result ("+ result + ") was "+(result == null ? StringUtils.Null : "not selectable")+" and requester ("+ requester + ") was "+(requester == null ? StringUtils.Null : "not selectable"));
			#endif
			return null;
		}

		private static IDrawer GetNextInternalSelectableFieldLeftInternal([NotNull]IParentDrawer subject, bool moveToNextControlAfterReachingEnd, IDrawer requester, bool membersListedVertically)
		{
			IDrawer[] members = subject.VisibleMembers;
			int count = members.Length;

			int index = -1;
			for(int n = count - 1; n >= 0; n--)
			{
				if(members[n] == requester)
				{
					index = n;
					break;
				}
			}

			if(membersListedVertically)
			{
				if(!moveToNextControlAfterReachingEnd)
				{
					#if DEV_MODE && DEBUG_NEXT_FIELD
					Debug.Log("\""+subject.Name+"\" - "+subject.GetType().Name + ".GetNextSelectableDrawerLeft("+moveToNextControlAfterReachingEnd+"): requester (because membersListedVertically and !moveToNextControlAfterReachingEnd)");
					#endif
					return requester;
				}

				if(requester == subject)
				{
					if(subject.Parent != null)
					{
						#if DEV_MODE && DEBUG_NEXT_FIELD
						Debug.Log("\""+subject.Name+"\" - "+subject.GetType().Name + ".GetNextSelectableDrawerLeft("+moveToNextControlAfterReachingEnd+"): parent.NextLeft (because requester was subject)");
						#endif
						var result = subject.Parent.GetNextSelectableDrawerLeft(true, subject);
						if(result != null)
						{
							return result;
						}
					}
					
					#if DEV_MODE && DEBUG_NEXT_FIELD
					Debug.Log("\""+subject.Name+"\" - "+subject.GetType().Name + ".GetNextSelectableDrawerLeft("+moveToNextControlAfterReachingEnd+ "): subject (because membersListedVertically and subject.parent null)");
					#endif
					return subject;
				}

				if(count == 0)
				{
					#if DEV_MODE && DEBUG_NEXT_FIELD
					Debug.Log("\""+subject.Name+"\" - "+subject.GetType().Name + ".GetNextSelectableDrawerLeft("+moveToNextControlAfterReachingEnd+"): subject (because member count was zero)");
					#endif
					
					return subject;
				}

				if(index == -1)
				{
					index = count - 1;
				}
				else
				{
					index--;
				}

				if(index >= 0)
				{
					#if DEV_MODE && DEBUG_NEXT_FIELD
					Debug.Log("\""+subject.Name+"\" - "+subject.GetType().Name + ".GetNextSelectableDrawerLeft("+moveToNextControlAfterReachingEnd+"): members["+index+"].NextLeft");
					#endif
					return members[index].GetNextSelectableDrawerLeft(true, subject);
				}
				
				#if DEV_MODE && DEBUG_NEXT_FIELD
				Debug.Log("\""+subject.Name+"\" - "+subject.GetType().Name + ".GetNextSelectableDrawerLeft("+moveToNextControlAfterReachingEnd+"): subject (because index ("+index+") was less than zero)");
				#endif
				return subject;
			}

			if(requester == subject)
			{
				if(moveToNextControlAfterReachingEnd)
				{
					if(subject.Parent != null)
					{
						#if DEV_MODE && DEBUG_NEXT_FIELD
						Debug.Log("\""+subject.Name+"\" - "+subject.GetType().Name + ".GetNextSelectableDrawerLeft("+moveToNextControlAfterReachingEnd+"): Parent.NextLeft (because requester == subject)");
						#endif
						return subject.Parent.GetNextSelectableDrawerLeft(true, subject);
						
					}
					#if DEV_MODE && DEBUG_NEXT_FIELD
					Debug.Log("\""+subject.Name+"\" - "+subject.GetType().Name + ".GetNextSelectableDrawerLeft("+moveToNextControlAfterReachingEnd+"): subject (because requester was subject and Parent was null)");
					#endif
					return subject;
				}
				return subject;
			}

			if(index != -1)
			{
				for(int n = index - 1; n >= 0; n--)
				{
					var test = members[n];
					if(test.Selectable)
					{
						#if DEV_MODE && DEBUG_NEXT_FIELD
						Debug.Log("\""+subject.Name+"\" - "+subject.GetType().Name + ".GetNextSelectableDrawerLeft(" + moveToNextControlAfterReachingEnd+"): members["+n+"] (requester != subject)");
						#endif

						return test;
					}

					#if DEV_MODE && DEBUG_NEXT_FIELD
					Debug.Log("\""+subject.Name+"\" - "+subject.GetType().Name + ".GetNextSelectableDrawerLeft(" + moveToNextControlAfterReachingEnd+"): skipping member "+(n+1)+"/"+count+" because !Selectable...");
					#endif
				}

				return subject;
				/*
				if(index == 0)
				{
					#if DEV_MODE && DEBUG_NEXT_FIELD
					Debug.Log("\""+subject.Name+"\" - "+subject.GetType().Name + ".GetNextSelectableDrawerLeft("+moveToNextControlAfterReachingEnd+"): subject (because index was 0)");
					#endif
					return subject;
				}
				#if DEV_MODE && DEBUG_NEXT_FIELD
				Debug.Log("\""+subject.Name+"\" - "+subject.GetType().Name + ".GetNextSelectableDrawerLeft("+moveToNextControlAfterReachingEnd+"): members["+index+"]");
				#endif
				return members[index - 1];
				*/
			}

			if(count > 0)
			{
				index = count - 1;
				#if DEV_MODE && DEBUG_NEXT_FIELD
				Debug.Log("\""+subject.Name+"\" - "+subject.GetType().Name + ".GetNextSelectableDrawerLeft("+moveToNextControlAfterReachingEnd+"): members["+index+"] (last index)");
				#endif
				return members[index];
			}
			#if DEV_MODE && DEBUG_NEXT_FIELD
			Debug.Log("\""+subject.Name+"\" - "+subject.GetType().Name + ".GetNextSelectableDrawerLeft("+moveToNextControlAfterReachingEnd+"): subject (because count was zero)");
			#endif
			return subject;
		}

		/// <summary>
		/// Gets the next selectable drawer to the right of the subject to which focus should move
		/// if keyboard input is given for selecting a control in that direction.
		/// </summary>
		/// <param name="subject"> The subject parent drawer from which the search for the next selectable drawer begins. This cannot be null. </param>
		/// <param name="moveToNextControlAfterReachingEnd">
		/// True if should move to next control in below row after reaching last control on this row. </param>
		/// <param name="requester">
		/// The drawer that is requesting the information. Knowing when the requester is a parent or a child
		/// of the drawer can sometimes be important in deciding what result to return.
		/// This can be null in cases where something else besides a drawer is calling this method (like an inspector).
		/// </param>
		/// <returns> The next drawer to the right of this one. </returns>
		[CanBeNull]
		public static IDrawer GetNextSelectableDrawerRight([NotNull]IParentDrawer subject, bool moveToNextControlAfterReachingEnd, [CanBeNull]IDrawer requester, bool membersListedVertically)
		{
			var result = GetNextInternalSelectableFieldRightInternal(subject, moveToNextControlAfterReachingEnd, requester, membersListedVertically);
			
			if(result != null && result.Selectable)
			{
				return result;
			}
			
			if(requester != null && requester.Selectable)
			{
				#if DEV_MODE && DEBUG_NEXT_FIELD
				Debug.Log("GetNextSelectableDrawerRight returning requester (" + requester+") because result ("+result+") was "+(result == null ? StringUtils.Null : "not selectable"));
				#endif
				return requester;
			}

			#if DEV_MODE && DEBUG_NEXT_FIELD
			Debug.Log("GetNextSelectableDrawerRight returning " + StringUtils.Null+" because result ("+ result + ") was "+(result == null ? StringUtils.Null : "not selectable")+" and requester ("+ requester + ") was "+(requester == null ? StringUtils.Null : "not selectable"));
			#endif
			return null;
		}

		/// <summary>
		/// Gets the next selectable drawer to the right of the subject to which focus should move if keyboard input is
		/// given for selecting a control in that direction.
		/// </summary>
		/// <param name="subject"> The subject parent drawer from which the search for the next selectable drawer begins. This cannot be null. </param>
		/// <param name="moveToNextControlAfterReachingEnd">
		/// True if should move to next control in below row after reaching last control on this row. </param>
		/// <param name="requester">
		/// The drawer that is requesting the information. Knowing when the requester is a parent or a child
		/// of the drawer can sometimes be important in deciding what result to return.
		/// This can be null in cases where something else besides a drawer is calling this method (like an inspector).
		/// </param>
		/// <returns> The next drawer to the right of this one. </returns>
		[CanBeNull]
		private static IDrawer GetNextInternalSelectableFieldRightInternal([NotNull]IParentDrawer subject, bool moveToNextControlAfterReachingEnd, [CanBeNull]IDrawer requester, bool membersListedVertically)
		{
			var members = subject.VisibleMembers;
			int count = members.Length;

			int requesterMemberIndex = -1;
			for(int n = count - 1; n >= 0; n--)
			{
				if(members[n] == requester)
				{
					requesterMemberIndex = n;
					break;
				}
			}

			if(membersListedVertically)
			{
				if(!moveToNextControlAfterReachingEnd)
				{
					return requester;
				}

				int nextMemberIndex;
				if(requester == subject)
				{
					nextMemberIndex = 0;
				}
				else
				{
					nextMemberIndex = requesterMemberIndex == -1 ? -1 : requesterMemberIndex + 1;
				}

				// if requester is not a member nor subject itself (could be a parent)
				if(nextMemberIndex == -1)
				{
					if(subject.Selectable)
					{
						#if DEV_MODE && DEBUG_NEXT_FIELD
						Debug.Log("\""+subject.Name+"\" - "+subject.GetType().Name + ".GetNextSelectableDrawerRight("+moveToNextControlAfterReachingEnd+"): subject (because requester member index was -1)");
						#endif
						return subject;
					}
					//move past the subject to its members if subject is not selectable
					nextMemberIndex = 0;
				}

				// if no next member to select, then move upwards in parent chain
				if(nextMemberIndex >= count)
				{
					// if has no parent...
					if(subject.Parent == null)
					{
						// ...then select subject itself (if selectable).
						if(subject.Selectable)
						{
							#if DEV_MODE && DEBUG_NEXT_FIELD
							Debug.Log("\""+subject.Name+"\" - "+subject.GetType().Name + ".GetNextSelectableDrawerRight("+moveToNextControlAfterReachingEnd+"): subject (because nextMemberIndex ("+nextMemberIndex+") >= count and had no parent)");
							#endif
							return subject;
						}
						
						// if subject wasn't selectable, move to its first member (if has one, and it wasn't the requester)
						if(count > 0 && members[0] != requester)
						{
							#if DEV_MODE && DEBUG_NEXT_FIELD
							Debug.Log("\""+subject.Name+"\" - "+subject.GetType().Name + ".GetNextSelectableDrawerRight("+moveToNextControlAfterReachingEnd+ "): members[0] (because had no parent and subject wasn't selectable, but had members)");
							#endif
							return members[0].GetNextSelectableDrawerRight(true, subject);
						}

						#if DEV_MODE && DEBUG_NEXT_FIELD
						Debug.Log("\""+subject.Name+"\" - "+subject.GetType().Name + ".GetNextSelectableDrawerRight("+moveToNextControlAfterReachingEnd+"): requester (because subject is not selectable and has no members)");
						#endif

						// if subject wasn't selectable, and had no members, nor a parent, return null
						return null;
					}
					#if DEV_MODE && DEBUG_NEXT_FIELD
					Debug.Log("\""+subject.Name+"\" - "+subject.GetType().Name + ".GetNextSelectableDrawerRight("+moveToNextControlAfterReachingEnd+"): parent.NextRight (because nextMemberIndex ("+nextMemberIndex+") >= count)");
					#endif
					return subject.Parent.GetNextSelectableDrawerRight(true, subject);
				}
				#if DEV_MODE && DEBUG_NEXT_FIELD
				Debug.Log("\""+subject.Name+"\" - "+subject.GetType().Name + ".GetNextSelectableDrawerRight("+moveToNextControlAfterReachingEnd+"): members["+nextMemberIndex+"].NextRight");
				#endif
				return members[nextMemberIndex].GetNextSelectableDrawerRight(true, subject);
			}

			if(requester == subject)
			{
				for(int n = 0; n < count; n++)
				{
					var test = members[n];
					if(test.Selectable)
					{
						#if DEV_MODE && DEBUG_NEXT_FIELD
						Debug.Log("\""+subject.Name+"\" - "+subject.GetType().Name + ".GetNextSelectableDrawerRight("+moveToNextControlAfterReachingEnd+"): members["+n+"] (requester == subject and count > 0)");
						#endif

						return test;
					}

					#if DEV_MODE && DEBUG_NEXT_FIELD
					Debug.Log("\""+subject.Name+"\" - "+subject.GetType().Name + ".GetNextSelectableDrawerRight("+moveToNextControlAfterReachingEnd+"): skipping member "+(n+1)+"/"+count+" because !Selectable...");
					#endif
				}
				
				if(moveToNextControlAfterReachingEnd && subject.Parent != null)
				{
					#if DEV_MODE && DEBUG_NEXT_FIELD
					Debug.Log("\""+subject.Name+"\" - "+subject.GetType().Name + ".GetNextSelectableDrawerRight("+moveToNextControlAfterReachingEnd+"): parent (requester == subject and count is zero)");
					#endif
					return subject.Parent.GetNextSelectableDrawerRight(true, subject);
				}
				#if DEV_MODE && DEBUG_NEXT_FIELD
				Debug.Log("\""+subject.Name+"\" - "+subject.GetType().Name + ".GetNextSelectableDrawerRight("+moveToNextControlAfterReachingEnd+"): subject (requester == subject and count is zero and parent null OR !moveToNextControlAfterReachingEnd)");
				#endif
				return subject;
			}

			if(requesterMemberIndex != -1)
			{
				requesterMemberIndex++;
				if(requesterMemberIndex >= count)
				{
					if(moveToNextControlAfterReachingEnd)
					{
						if(subject.Parent != null)
						{
							#if DEV_MODE && DEBUG_NEXT_FIELD
							Debug.Log("\""+subject.Name+"\" - "+subject.GetType().Name + ".GetNextSelectableDrawerRight("+moveToNextControlAfterReachingEnd+"): parent (index >= count)");
							#endif
							return subject.Parent.GetNextSelectableDrawerRight(true, subject);
						}
						#if DEV_MODE && DEBUG_NEXT_FIELD
						Debug.Log("\""+subject.Name+"\" - "+subject.GetType().Name + ".GetNextSelectableDrawerRight("+moveToNextControlAfterReachingEnd+"): parent (index >= count and parent null)");
						#endif
						return subject;
					}

					requesterMemberIndex = count - 1;
					#if DEV_MODE && DEBUG_NEXT_FIELD
					Debug.Log("\""+subject.Name+"\" - "+subject.GetType().Name + ".GetNextSelectableDrawerRight("+moveToNextControlAfterReachingEnd+"): members["+requesterMemberIndex+"] (stay on last index because !moveToNextControlAfterReachingEnd)");
					#endif
					return members[requesterMemberIndex];
				}
				#if DEV_MODE && DEBUG_NEXT_FIELD
				Debug.Log("\""+subject.Name+"\" - "+subject.GetType().Name + ".GetNextSelectableDrawerRight("+moveToNextControlAfterReachingEnd+"): members["+requesterMemberIndex+"]");
				#endif
				return members[requesterMemberIndex];
			}

			#if DEV_MODE && DEBUG_NEXT_FIELD
			Debug.Log("\""+subject.Name+"\" - "+subject.GetType().Name + ".GetNextSelectableDrawerRight("+moveToNextControlAfterReachingEnd+"): subject");
			#endif
			return subject;
		}
	
		/// <summary>
		/// Gets the next selectable upwards of the subject to which focus should move if keyboard input is
		/// given for selecting a control in that direction.
		/// </summary>
		/// <param name="subject"> The subject parent drawer from which the search for the next selectable drawer begins. This cannot be null. </param>
		/// <param name="moveToNextControlAfterReachingEnd">
		/// True if should move to next control in below row after reaching last control on this row. </param>
		/// <param name="requester">
		/// The drawer that is requesting the information. Knowing when the requester is a parent or a child
		/// of the drawer can sometimes be important in deciding what result to return.
		/// This can be null in cases where something else besides a drawer is calling this method (like an inspector).
		/// </param>
		/// <returns> The next drawer to the right of this one. </returns>
		/// <param name="column">
		/// The zero-based horizontal left-to-right index of the requester in the current row.
		/// Column 0 is where the prefix label is.
		/// Column 1 is where the first member of the subject is, when membersListedVertically is false.
		/// etc.
		/// -1 means the column is still undetermined. </param>
		/// <param name="requester"> The requester. </param>
		/// <param name="membersListedVertically"> True to members listed vertically. </param>
		/// <returns> The next selectable drawer up. This may be null. </returns>
		[CanBeNull]
		public static IDrawer GetNextSelectableDrawerUp(IParentDrawer subject, int column, IDrawer requester, bool membersListedVertically)
		{
			var result = GetNextSelectableDrawerUpInternal(subject, column, requester, membersListedVertically);

			if(result != null && result.Selectable)
			{
				return result;
			}

			if(requester != null && requester.Selectable)
			{
				return requester;
			}

			return null;
		}

		/// <summary>
		/// Gets the next selectable upwards of the subject to which focus should move if keyboard input is
		/// given for selecting a control in that direction.
		/// </summary>
		/// <param name="subject"> The subject parent drawer from which the search for the next selectable drawer begins. This cannot be null. </param>
		/// <param name="moveToNextControlAfterReachingEnd">
		/// True if should move to next control in below row after reaching last control on this row. </param>
		/// <param name="requester">
		/// The drawer that is requesting the information. Knowing when the requester is a parent or a child
		/// of the drawer can sometimes be important in deciding what result to return.
		/// This can be null in cases where something else besides a drawer is calling this method (like an inspector).
		/// </param>
		/// <returns> The next drawer to the right of this one. </returns>
		/// <param name="column">
		/// The zero-based horizontal left-to-right index of the requester in the current row.
		/// Column 0 is where the prefix label is.
		/// Column 1 is where the first member of the subject is, when membersListedVertically is false.
		/// etc.
		/// -1 means the column is still undetermined. </param>
		/// <param name="requester"> The requester. </param>
		/// <param name="membersListedVertically"> True to members listed vertically. </param>
		/// <returns> The next selectable drawer up. This may be null. </returns>
		private static IDrawer GetNextSelectableDrawerUpInternal(IParentDrawer subject, int column, IDrawer requester, bool membersListedVertically)
		{
			//if requester is self, move request up to parent to move towards next row
			if(requester == subject)
			{
				if(subject.Parent != null)
				{
					#if DEV_MODE && DEBUG_NEXT_FIELD
					Debug.Log("\""+subject.Name+"\" - "+subject.GetType().Name + ".GetNextSelectableDrawerUp("+0+"): parent");
					#endif
					return subject.Parent.GetNextSelectableDrawerUp(0, subject);
				}

				#if DEV_MODE && DEBUG_NEXT_FIELD
				Debug.Log("\""+subject.Name+"\" - "+subject.GetType().Name + ".GetNextSelectableDrawerUp("+column+"): subject (because parent null)");
				#endif

				//TO DO: Select inspector toolbar? or above inspector?
				
				return subject;
			}

			var members = subject.VisibleMembers;
			int count = members.Length;
			if(count > 0)
			{
				int index = -1;
				for(int n = count - 1; n >= 0; n--)
				{
					if(members[n] == requester)
					{
						index = n;
						break;
					}
				}

				if(membersListedVertically)
				{
					if(column == -1)
					{
						column = 0;
					}

					if(count > 0)
					{
						int nextIndex;
						if(index == -1)
						{
							nextIndex = count - 1;
						}
						else
						{
							nextIndex = index - 1;
						}
				
						if(nextIndex >= 0)
						{
							#if DEV_MODE && DEBUG_NEXT_FIELD
							Debug.Log("\""+subject.Name+"\" - "+subject.GetType().Name + ".GetNextSelectableDrawerUp("+column+"): members["+ nextIndex + "] (because membersListedVertically and nextIndex >= 0)");
							#endif
							return members[nextIndex].GetNextSelectableDrawerUp(column, subject);
						}
					}
				}
				else //if members drawn horizontally
				{
					//if request comes from a member, move request up to parent to move towards next row
					if(index != -1)
					{
						column = index + 1;

						#if DEV_MODE && DEBUG_NEXT_FIELD
						Debug.Log("\""+subject.Name+"\" - "+subject.GetType().Name + ".parent.GetNextSelectableDrawerUp("+column+"): parent (because !membersListedVertically and requester was member)");
						#endif
						return subject.Parent.GetNextSelectableDrawerUp(column, subject);
					}

					//if active column is not prefix label column (0)
					//select member based on active column
					if(column > 0)
					{
						int nextIndex = column - 1;
						if(nextIndex >= count)
						{
							nextIndex = count - 1;
						}

						#if DEV_MODE && DEBUG_NEXT_FIELD
						Debug.Log("\""+subject.Name+"\" - "+subject.GetType().Name + ".GetNextSelectableDrawerDown("+column+"): members["+ nextIndex + "] (because !membersListedVertically nextIndex >= 0)");
						#endif
						return members[nextIndex].GetNextSelectableDrawerUp(column, subject);
					}
				}
			}

			//if no members or requester was first child, select self - if possible
			if(subject.Selectable)
			{
				#if DEV_MODE && DEBUG_NEXT_FIELD
				Debug.Log("\""+subject.Name+"\" - "+subject.GetType().Name + ".GetNextSelectableDrawerUp("+column+"): self (because index < 0)");
				#endif
				return subject;
			}

			if(subject.Parent != null)
			{
				#if DEV_MODE && DEBUG_NEXT_FIELD
				Debug.Log("\""+subject.Name+"\" - "+subject.GetType().Name + ".GetNextSelectableDrawerUp("+column+"): parent (because !subject.Selectable)");
				#endif
				return subject.Parent.GetNextSelectableDrawerUp(column, subject);
			}

			if(requester.Selectable)
			{
				#if DEV_MODE && DEBUG_NEXT_FIELD
				Debug.Log("\""+subject.Name+"\" - "+subject.GetType().Name + ".GetNextSelectableDrawerUp("+column+"): requester (because Parent null)");
				#endif
				return requester;
			}

			#if DEV_MODE && DEBUG_NEXT_FIELD
			Debug.Log("\""+subject.Name+"\" - "+subject.GetType().Name + ".GetNextSelectableDrawerUp("+column+"): requester (because !requester.Selectable)");
			#endif
			return InspectorUtility.ActiveManager.FocusedDrawer;
		}

		/// <summary>
		/// Gets the next selectable downwards of the subject to which focus should move if keyboard input is
		/// given for selecting a control in that direction.
		/// </summary>
		/// <param name="subject"> The subject parent drawer from which the search for the next selectable drawer begins. This cannot be null. </param>
		/// <param name="moveToNextControlAfterReachingEnd">
		/// True if should move to next control in below row after reaching last control on this row. </param>
		/// <param name="requester">
		/// The drawer that is requesting the information. Knowing when the requester is a parent or a child
		/// of the drawer can sometimes be important in deciding what result to return.
		/// This can be null in cases where something else besides a drawer is calling this method (like an inspector).
		/// </param>
		/// <returns> The next drawer to the right of this one. </returns>
		/// <param name="column">
		/// The zero-based horizontal left-to-right index of the requester in the current row.
		/// Column 0 is where the prefix label is.
		/// Column 1 is where the first member of the subject is, when membersListedVertically is false.
		/// etc.
		/// -1 means the column is still undetermined. </param>
		/// <param name="requester"> The requester. </param>
		/// <param name="membersListedVertically"> True to members listed vertically. </param>
		/// <returns> The next selectable drawer up. This may be null. </returns>
		public static IDrawer GetNextSelectableDrawerDown(IParentDrawer subject, int column, IDrawer requester, bool membersListedVertically)
		{
			var result = GetNextSelectableDrawerDownInternal(subject, column, requester, membersListedVertically);

			if(result != null && result.Selectable)
			{
				return result;
			}

			if(requester != null && requester.Selectable)
			{
				return requester;
			}

			return null;
		}

		/// <summary>
		/// Gets the next selectable downwards of the subject to which focus should move if keyboard input is
		/// given for selecting a control in that direction.
		/// </summary>
		/// <param name="subject"> The subject parent drawer from which the search for the next selectable drawer begins. This cannot be null. </param>
		/// <param name="moveToNextControlAfterReachingEnd">
		/// True if should move to next control in below row after reaching last control on this row. </param>
		/// <param name="requester">
		/// The drawer that is requesting the information. Knowing when the requester is a parent or a child
		/// of the drawer can sometimes be important in deciding what result to return.
		/// This can be null in cases where something else besides a drawer is calling this method (like an inspector).
		/// </param>
		/// <returns> The next drawer to the right of this one. </returns>
		/// <param name="column">
		/// The zero-based horizontal left-to-right index of the requester in the current row.
		/// Column 0 is where the prefix label is.
		/// Column 1 is where the first member of the subject is, when membersListedVertically is false.
		/// etc.
		/// -1 means the column is still undetermined. </param>
		/// <param name="requester"> The requester. </param>
		/// <param name="membersListedVertically"> True to members listed vertically. </param>
		/// <returns> The next selectable drawer up. This may be null. </returns>
		private static IDrawer GetNextSelectableDrawerDownInternal([NotNull]IParentDrawer subject, int column, [CanBeNull]IDrawer requester, bool membersListedVertically)
		{
			var members = subject.VisibleMembers;
			int count = members.Length;
			
			#if DEV_MODE && DEBUG_NEXT_FIELD
			Debug.Log(subject + ".GetNextDrawerDown("+ column + ") called with requester=" + (requester == null ? "null" : requester.ToString())+", visibleMembers.Length="+count+", membersListedVertically="+ membersListedVertically+"...");
			#endif

			if(count > 0)
			{
				if(membersListedVertically)
				{
					if(requester == subject)
					{
						#if DEV_MODE && DEBUG_NEXT_FIELD
						Debug.Log(subject + ".GetNextDrawerDown(" + column+"): members[0] (because requester is self)");
						#endif
						return members[0].GetNextSelectableDrawerDown(0, subject);
					}

					int index = -1;
					for(int n = count - 1; n >= 0; n--)
					{
						if(members[n] == requester)
						{
							index = n;
							break;
						}
					}

					if(index == -1)
					{
						if(!subject.Selectable)
						{
							#if DEV_MODE && DEBUG_NEXT_FIELD
							Debug.Log(subject + ".GetNextDrawerDown(" + column+"): members[0] (because requester not member and subject not selectable)");
							#endif
							return members[0].GetNextSelectableDrawerDown(0, subject);
						}

						#if DEV_MODE && DEBUG_NEXT_FIELD
						Debug.Log(subject + ".GetNextDrawerDown(" + column+"): self (because requester not member)");
						#endif
						return subject;
					}

					if(column == -1)
					{
						column = 1;
					}

					int nextIndex = index + 1;

					if(nextIndex < count)
					{
						#if DEV_MODE && DEBUG_NEXT_FIELD
						Debug.Log(subject + ".GetNextDrawerDown(" + column+"): members["+ nextIndex + "] (because nextIndex < count)");
						#endif
						return members[nextIndex].GetNextSelectableDrawerDown(column, subject);
					}
					#if DEV_MODE && DEBUG_NEXT_FIELD
					Debug.Log(subject + "nextIndex "+nextIndex+" >= count "+count+") with requester="+(requester == null ? "null" : requester.GetType().Name)+"...");
					#endif
				}
				else //members listed horizontally
				{
					if(requester == subject && subject.Parent != null)
					{
						#if DEV_MODE && DEBUG_NEXT_FIELD
						Debug.Log(subject + ".GetNextDrawerDown(" + column+"): parent (because requester is self)");
						#endif
						return subject.Parent.GetNextSelectableDrawerDown(0, subject);
					}

					int index = -1;
					for(int n = count - 1; n >= 0; n--)
					{
						if(members[n] == requester)
						{
							index = n;
							column = index + 1;
							break;
						}
					}

					if(index == -1)
					{
						if(column <= 0)
						{
							#if DEV_MODE && DEBUG_NEXT_FIELD
							Debug.Log(subject + ".GetNextDrawerDown(" + column+ "): self (because column <= 1)");
							#endif

							return subject;
						}

						int nextIndex = Mathf.Min(column - 1, count - 1);
			
						#if DEV_MODE && DEBUG_NEXT_FIELD
						Debug.Log(subject + ".GetNextDrawerDown(" + column + "): members[" + nextIndex + "] (because column > 0)");
						#endif

						return members[nextIndex].GetNextSelectableDrawerDown(column, subject);
					}
				}
			}
			else if(requester != subject && subject.Selectable)
			{
				#if DEV_MODE && DEBUG_NEXT_FIELD
				Debug.Log(subject + ".GetNextDrawerDown(" + column + "): self (because no members and requester not self)");
				#endif

				return subject;
			}

			if(subject.Parent != null)
			{
				#if DEV_MODE && DEBUG_NEXT_FIELD
				Debug.Log(subject + ".GetNextDrawerDown(" + column+"): parent (because index >= count)");
				#endif
				return subject.Parent.GetNextSelectableDrawerDown(column, subject);
			}
			
			#if DEV_MODE && DEBUG_NEXT_FIELD
			Debug.Log(subject + ".GetNextDrawerDown(" + column+"): FocusedDrawer (because index >= count && Parent null)");
			#endif

			//UPDATE: Changed this to FocusedDrawer instead of requester to fix bug where if you press down arrow on the Add Component Menu,
			//it would cause the GameObjectDrawer to get selected instead
			return InspectorUtility.ActiveManager.FocusedDrawer;
		}

		/// <summary>
		/// NOTE: Will not test the subject itself, only its children
		/// </summary>
		public static IDrawer TestChildrenUntilTrue(Func<IDrawer, bool> test, IParentDrawer subject)
		{
			var membs = subject.Members;
			for(int n = membs.Length - 1; n >= 0; n--)
			{
				var memb = membs[n];
				if(memb != null)
				{
					var result = memb.TestChildrenUntilTrue(test);
					if(result != null)
					{
						return result;
					}
				}
				#if DEV_MODE && DEBUG_NULL_MEMBERS
				else { Debug.LogError(subject.GetType().Name + ".TestChildrenUntilTrue - null member at #"+n); }
				#endif
			}
			return null;
		}

		/// <summary>
		/// NOTE: Will not test the subject itself, only its children
		/// </summary>
		public static IDrawer TestVisibleChildrenUntilTrue(Func<IDrawer, bool> test, IParentDrawer subject)
		{
			if(!InspectorUtility.ActiveInspector.IsOutsideViewport(subject.Bounds))
			{
				var membs = subject.VisibleMembers;
				for(int n = membs.Length - 1; n >= 0; n--)
				{
					var memb = membs[n];
					if(memb != null)
					{
						if(!InspectorUtility.ActiveInspector.IsOutsideViewport(memb.Bounds))
						{
							var result = memb.TestVisibleChildrenUntilTrue(test);
							if(result != null)
							{
								return result;
							}
						}
					}
					#if DEV_MODE && DEBUG_NULL_MEMBERS
					else { Debug.LogError(subject.GetType().Name + ".TestChildrenUntilTrue - null member at #"+n); }
					#endif
				}
			}
			return null;
		}

		public static void GetMemberBuildList([NotNull]IUnityObjectDrawer parent, [NotNull]ref List<LinkedMemberInfo> results)
		{
			GetMemberBuildList(parent, parent.MemberHierarchy, ref results, DebugModeDisplaySettings.DefaultSettings);
		}

		public static void GetMemberBuildList([NotNull]IUnityObjectDrawer parent, [NotNull]ref List<LinkedMemberInfo> results, [NotNull]DebugModeDisplaySettings debugModeSettings)
		{
			GetMemberBuildList(parent, parent.MemberHierarchy, ref results, debugModeSettings);
		}

		public static void GetMemberBuildList([NotNull]IUnityObjectDrawer parent, [NotNull]LinkedMemberHierarchy hierarchy, [NotNull]ref List<LinkedMemberInfo> results, [NotNull]DebugModeDisplaySettings debugModeSettings)
		{
			#if DEV_MODE
			Debug.Assert(parent != null, "GetMemberBuildList - parent null");
			Debug.Assert(hierarchy != null, "GetMemberBuildList - hierarchy null");
			Debug.Assert(results != null, "GetMemberBuildList - results List null");
			Debug.Assert(debugModeSettings != null, "GetMemberBuildList - Debug Mode Settings null");
			#endif

			var parentMemberInfo = parent.MemberInfo;
			var type = parent.Type;
			
			BindingFlags bindingFlags;
			LinkedMemberParent parentType;

			if(debugModeSettings.Static)
			{
				parentType = LinkedMemberParent.Static;
				bindingFlags = BindingFlagsStatic;
			}
			else
			{
				parentType = LinkedMemberParent.UnityObject;
				bindingFlags = BindingFlagsInstance;
			}

			if(debugModeSettings.ShowFields)
			{
				type.GetInspectorViewableFields(ref hierarchy, parentMemberInfo, ref results, bindingFlags, true, FieldVisibility.AllExceptHidden, parentType);
			}

			if(debugModeSettings.ShowProperties)
			{
				type.GetInspectorViewableProperties(ref hierarchy, parentMemberInfo, ref results, bindingFlags, true, PropertyVisibility.AllPublic, parentType);
			}

			if(debugModeSettings.ShowMethods)
			{
				type.GetInspectorViewableMethods(ref hierarchy, parentMemberInfo, ref results, bindingFlags, true, MethodVisibility.AllPublic, parentType);
			}
		}

		public static void GetMemberBuildList([NotNull]IParentDrawer parent, [NotNull]LinkedMemberHierarchy hierarchy, [NotNull]ref List<LinkedMemberInfo> results, bool includeHidden, BindingFlags bindingFlags = BindingFlagsInstanceAndStatic)
		{
			GetMemberBuildList(parent.Type, parent, hierarchy, ref results, includeHidden, BindingFlagsInstanceAndStatic);
		}

		public static void GetMemberBuildList([NotNull]Type parentType, [NotNull]IParentDrawer parent, [NotNull]LinkedMemberHierarchy hierarchy, [NotNull]ref List<LinkedMemberInfo> results, bool includeHidden, BindingFlags bindingFlags = BindingFlagsInstanceAndStatic)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(bindingFlags.HasFlag(BindingFlags.Instance) || bindingFlags.HasFlag(BindingFlags.Static), "GetMemberBuildList called with neither instance nor static flags enabled. Nothing will be returned.");
			Debug.Assert(bindingFlags.HasFlag(BindingFlags.Public) || bindingFlags.HasFlag(BindingFlags.NonPublic), "GetMemberBuildList called with neither public nor nonpublic flags enabled. Nothing will be returned.");
			#endif

			if(includeHidden)
			{
				GetMemberBuildList(parentType, parent, hierarchy, ref results, DebugModeDisplaySettings.DefaultSettings, bindingFlags);
				return;
			}

			var settings = InspectorUtility.Preferences;
			var showNonSerializedFields = settings.showFields;
			var propertyVisibility = settings.showProperties;
			var methodVisibility = settings.showMethods;

			GetMemberBuildList(parentType, parent, hierarchy, ref results, includeHidden, showNonSerializedFields, propertyVisibility, methodVisibility, bindingFlags);
		}

		public static void GetMemberBuildList([NotNull]IParentDrawer parent, [NotNull]LinkedMemberHierarchy hierarchy, [NotNull]ref List<LinkedMemberInfo> results, DebugModeDisplaySettings debugModeSettings, BindingFlags bindingFlags = BindingFlagsInstanceAndStatic)
		{
			GetMemberBuildList(parent.Type, parent, hierarchy, ref results, debugModeSettings, bindingFlags);
		}

		public static void GetMemberBuildList([NotNull]Type parentType, [NotNull]IParentDrawer parent, [NotNull]LinkedMemberHierarchy hierarchy, [NotNull]ref List<LinkedMemberInfo> results, DebugModeDisplaySettings debugModeSettings, BindingFlags bindingFlags = BindingFlagsInstanceAndStatic)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(bindingFlags.HasFlag(BindingFlags.Instance) || bindingFlags.HasFlag(BindingFlags.Static), "GetMemberBuildList called with neither instance nor static flags enabled. Nothing will be returned.");
			Debug.Assert(bindingFlags.HasFlag(BindingFlags.Public) || bindingFlags.HasFlag(BindingFlags.NonPublic), "GetMemberBuildList called with neither public nor nonpublic flags enabled. Nothing will be returned.");
			#endif

			var parentMemberInfo = parent.MemberInfo;

			LinkedMemberParent parentMemberType;
			if(parentMemberInfo == null)
			{
				parentMemberType = parentType.IsUnityObject() ? LinkedMemberParent.UnityObject : LinkedMemberParent.Missing;
			}
			else
			{
				parentMemberType = parentMemberInfo.IsStatic ? LinkedMemberParent.Static : LinkedMemberParent.LinkedMemberInfo;
			}

			if(debugModeSettings.Static)
			{
				if(debugModeSettings.ShowFields)
				{
					parentType.GetInspectorViewableFieldsInStaticMode(ref hierarchy, parentMemberInfo, ref results, true, FieldVisibility.AllExceptHidden, PropertyVisibility.AllPublic, MethodVisibility.AttributeExposedOnly);
				}

				if(debugModeSettings.ShowProperties)
				{
					parentType.GetInspectorViewablePropertiesInStaticMode(ref hierarchy, parentMemberInfo, ref results, true, FieldVisibility.AllExceptHidden, PropertyVisibility.AllPublic, MethodVisibility.AttributeExposedOnly);
				}

				if(debugModeSettings.ShowMethods)
				{
					parentType.GetInspectorViewableMethodsInStaticMode(ref hierarchy, parentMemberInfo, ref results, true, MethodVisibility.AllPublic);
				}
			}
			else
			{
				if(debugModeSettings.ShowFields)
				{
					parentType.GetInspectorViewableFields(ref hierarchy, parentMemberInfo, ref results, bindingFlags, true, FieldVisibility.AllExceptHidden, parentMemberType);
				}

				if(debugModeSettings.ShowProperties)
				{
					parentType.GetInspectorViewableProperties(ref hierarchy, parentMemberInfo, ref results, bindingFlags, true, PropertyVisibility.AllPublic, parentMemberType);
				}

				if(debugModeSettings.ShowMethods)
				{
					parentType.GetInspectorViewableMethods(ref hierarchy, parentMemberInfo, ref results, bindingFlags, true, MethodVisibility.AllPublic, parentMemberType);
				}
			}
		}
		
		public static void GetMemberBuildList([NotNull]IParentDrawer parent, [NotNull]LinkedMemberHierarchy hierarchy, [NotNull]ref List<LinkedMemberInfo> results, bool includeHidden, FieldVisibility showNonSerializedFields, PropertyVisibility propertyVisibility, MethodVisibility methodVisibility, [CanBeNull]string[] skipMembers, BindingFlags bindingFlags = BindingFlagsInstance)
		{
			GetMemberBuildList(parent, hierarchy, ref results, includeHidden, showNonSerializedFields, propertyVisibility, methodVisibility, bindingFlags);

			if(skipMembers != null)
			{
				for(int s = skipMembers.Length - 1; s >= 0; s--)
				{
					var skipMember = skipMembers[s];
					for(int n = results.Count - 1; n >= 0; n--)
					{
						if(string.Equals(results[n].Name, skipMember))
						{
							results.RemoveAt(n);
							break;
						}
					}
				}
			}
		}
		
		public static void GetMemberBuildList([NotNull]IParentDrawer parent, [NotNull]LinkedMemberHierarchy hierarchy, [NotNull]ref List<LinkedMemberInfo> results, bool includeHidden, FieldVisibility showNonSerializedFields, PropertyVisibility propertyVisibility, MethodVisibility methodVisibility, BindingFlags bindingFlags = BindingFlagsInstance)
		{
			#if DEV_MODE
			//binding flags should be DeclaredOnly, since we want to ignore inherited types and
			//because we are going to manually iterate through all base types, and stopping once we
			//hit specific base types (to avoid too many results)
			Debug.Assert((bindingFlags & BindingFlags.DeclaredOnly) != 0, "GetMemberBuildList was called with bindingFlags that did not have DeclaredOnly flag.");
			#endif

			parent.Type.GetInspectorViewables(ref hierarchy, parent.MemberInfo, ref results, bindingFlags, includeHidden, showNonSerializedFields, propertyVisibility, methodVisibility);
		}

		public static void GetMemberBuildList([NotNull]Type parentType, [NotNull]IParentDrawer parent, [NotNull]LinkedMemberHierarchy hierarchy, [NotNull]ref List<LinkedMemberInfo> results, bool includeHidden, FieldVisibility showNonSerializedFields, PropertyVisibility propertyVisibility, MethodVisibility methodVisibility, BindingFlags bindingFlags = BindingFlagsInstance)
		{
			#if DEV_MODE
			//binding flags should be DeclaredOnly, since we want to ignore inherited types and
			//because we are going to manually iterate through all base types, and stopping once we
			//hit specific base types (to avoid too many results)
			Debug.Assert((bindingFlags & BindingFlags.DeclaredOnly) != 0, "GetMemberBuildList was called with bindingFlags that did not have DeclaredOnly flag.");
			#endif

			parentType.GetInspectorViewables(ref hierarchy, parent.MemberInfo, ref results, bindingFlags, includeHidden, showNonSerializedFields, propertyVisibility, methodVisibility);
		}
		
		public static void GetStaticMemberBuildList([NotNull]IParentDrawer parent, [NotNull]LinkedMemberHierarchy hierarchy, [NotNull]ref List<LinkedMemberInfo> results, FieldVisibility fieldVisibility, PropertyVisibility propertyVisibility, MethodVisibility methodVisibility)
		{
			parent.Type.GetStaticInspectorViewables(ref hierarchy, parent.MemberInfo, ref results, false, fieldVisibility, propertyVisibility, methodVisibility);
		}

		/// <summary>
		/// Builds drawers for members based on LinkedMemberInfos in buildList.
		/// NOTE: Members must be grouped by MemberType in order matching InspectorUtility.Preferences.MemberDisplayOrder.
		/// </summary>
		/// <param name="parent"> The parent whose members are built. </param>
		/// <param name="buildList"> List of LinkedMemberInfos from which to build members. </param>
		/// <param name="results"> [in, out] Array containing results. Input can be null. Output will never be null. </param>
		public static void BuildMembers([NotNull]IDrawerProvider drawerProvider, [CanBeNull]IParentDrawer parent, List<LinkedMemberInfo> buildList, ref IDrawer[] results)
		{
			#if DEV_MODE
			if(parent != null && CalculateDrawerDepth(parent) >= MaxDrawerDepth)
			{
				Debug.LogError("ParentDrawerUtility.BuildMembers("+parent+", "+StringUtils.ToString(buildList)+") - maximum drawer depth " + MaxDrawerDepth + " reached!");
				throw new StackOverflowException();
			}
			#endif

			#if DEV_MODE
			Debug.Log((parent != null ? parent.ToString() + "." : "") + "BuildMembers("+buildList.Count+")");
			#endif

			var membersList = DrawerListPool.Create(buildList.Count);
			BuildMembers(drawerProvider, parent, buildList, membersList);
			DrawerArrayPool.ListToArrayAndDispose(ref membersList, ref results, false);
		}

		private static int CalculateDrawerDepth(IDrawer drawer)
		{
			int depth = 0;
			for(var p = drawer.Parent; p != null; p = p.Parent)
			{
				depth++;
			}
			return depth;
		}

		/// <summary>
		/// Builds drawers for members based on LinkedMemberInfos in buildList.
		/// NOTE: Members must be grouped by MemberType in order matching InspectorUtility.Preferences.MemberDisplayOrder.
		/// </summary>
		/// <param name="parent"> The parent whose members are built. </param>
		/// <param name="buildList"> List of LinkedMemberInfos from which to build members. </param>
		/// <param name="results"> List into which built members are added. </param>
		public static void BuildMembers([NotNull]IDrawerProvider drawerProvider, [CanBeNull]IParentDrawer parent, List<LinkedMemberInfo> buildList, List<IDrawer> results)
		{
			bool readOnly = parent != null && parent.ReadOnly;

			Dictionary<string, ICustomGroupDrawer> groupDrawers = null;
			Dictionary<string, List<IDrawer>> membersByGroup = null;

			int n = 0;
			int count = buildList.Count;
			var order = InspectorUtility.Preferences.MemberDisplayOrder;
			do
			{
				for(int t = 0; t < 3; t++)
				{
					switch(order[t])
					{
						case Member.Field:
							for(; n < count; n++)
							{
								var linkedInfo = buildList[n];
								if(linkedInfo.MemberType != MemberTypes.Field)
								{
									break;
								}

								if(HandleGroupAttributeForField(drawerProvider, parent, linkedInfo, results, ref groupDrawers, ref membersByGroup))
								{
									continue;
								}
							
								DrawerUtility.TryGetDecoratorDrawer(drawerProvider, linkedInfo, ref results, parent);

								var label = linkedInfo.GetLabel();

								try
								{
									var member = drawerProvider.GetForField(linkedInfo.GetValue(0), linkedInfo.Type, linkedInfo, parent, label, readOnly);
							
									#if DEV_MODE && DEBUG_BUILD_INHERITED_FIELDS
									Debug.Log("<color=green>\"" + (parent == null ? "null" : parent.Name) + "\".BuildIntructionsInChildren - field #"+(n+1)+"/"+count+" \""+ linkedInfo.Name+"\" ("+ StringUtils.ToStringSansNamespace(linkedInfo.Type) + ") created: "+ StringUtils.ToStringSansNamespace(member.GetType())+"</color>");
									#endif
							
									results.Add(member);
								}
								catch(Exception e)
								{
									Debug.LogError(e);
								}
							}
							break;
						case Member.Property:
							for(; n < count; n++)
							{
								var linkedInfo = buildList[n];
								if(linkedInfo.MemberType != MemberTypes.Property)
								{
									break;
								}
								DrawerUtility.TryGetDecoratorDrawer(drawerProvider, linkedInfo, ref results, parent);

								var label = linkedInfo.GetLabel();

								try
								{
									var member = drawerProvider.GetForProperty(linkedInfo.Type, linkedInfo, parent, label, readOnly);

									#if DEV_MODE && DEBUG_BUILD_INHERITED_PROPERTIES
									Debug.Log("<color=green>\"" + (parent == null ? "null" : parent.Name) + "\".BuildIntructionsInChildren - "+ linkedInfo.Type.Name+" property #"+(n+1)+"/"+count+" "+ linkedInfo.Name+" created: "+member.GetType()+"</color>");
									#endif
							
									results.Add(member);
								}
								catch(Exception e)
								{
									Debug.LogError(e);
								}
							}
							break;
						case Member.Method:
							for(; n < count; n++)
							{
								var linkedInfo = buildList[n];
								if(linkedInfo.MemberType != MemberTypes.Method)
								{
									break;
								}
							
								DrawerUtility.TryGetDecoratorDrawer(drawerProvider, linkedInfo, ref results, parent);
								var label = linkedInfo.GetLabel();
								var member = drawerProvider.GetForMethod(linkedInfo, parent, label, readOnly);
							
								#if DEV_MODE && DEBUG_BUILD_INHERITED_METHODS
								Debug.Log("<color=green>\"" + (parent == null ? "null" : parent.Name) + "\".BuildIntructionsInChildren - "+ linkedInfo.Type.Name+" method #"+(n+1)+"/"+count+" "+ linkedInfo.Name+" created: "+member.GetType()+"</color>");
								#endif
							
								results.Add(member);
							}
							break;
					}
				}
			}
			while(n < count); // if list is not empty yet loop back to beginning

			if(groupDrawers != null)
			{
				foreach(var groupMembers in membersByGroup)
				{
					var name = groupMembers.Key;
					var members = groupMembers.Value;
					var groupDrawer = groupDrawers[name];
					groupDrawer.SetMembers(members.ToArray(), false, true);
				}
			}
		}

		private static bool HandleGroupAttributeForField([NotNull]IDrawerProvider drawerProvider, [CanBeNull]IParentDrawer parent, LinkedMemberInfo linkedInfo, List<IDrawer> results, ref Dictionary<string, ICustomGroupDrawer> groupDrawers, ref Dictionary<string, List<IDrawer>> membersByGroup)
		{
			var groupAttribute = linkedInfo.GetAttribute<Attributes.IGroupAttribute>();
			if(groupAttribute == null)
			{
				return false;
			}

			if(groupDrawers == null)
			{
				groupDrawers = new Dictionary<string, ICustomGroupDrawer>();
				membersByGroup = new Dictionary<string, List<IDrawer>>();
			}

			bool readOnly = parent != null && parent.ReadOnly;

			ICustomGroupDrawer groupDrawer;
			List<IDrawer> groupMembers;
			var groupLabel = groupAttribute.Label;
			if(!groupDrawers.TryGetValue(groupLabel.text, out groupDrawer))
			{
				var groupDrawerType = groupAttribute.GetDrawerType(drawerProvider);
				groupDrawer = drawerProvider.GetOrCreateInstance<ICustomGroupDrawer>(groupDrawerType);
				groupDrawer.SetupInterface(parent, groupAttribute.Label, readOnly);
				groupDrawer.LateSetup();
				groupDrawers.Add(groupLabel.text, groupDrawer);
				results.Add(groupDrawer);

				groupMembers = new List<IDrawer>();
				membersByGroup.Add(groupLabel.text, groupMembers);
			}
			else
			{
				groupMembers = membersByGroup[groupLabel.text];
			}

			DrawerUtility.TryGetDecoratorDrawer(drawerProvider, linkedInfo, ref groupMembers, groupDrawer);

			var label = linkedInfo.GetLabel();
			var member = drawerProvider.GetForField(linkedInfo.GetValue(0), linkedInfo.Type, linkedInfo, groupDrawer, label, readOnly);

			groupMembers.Add(member);

			return true;
		}
		
		public static void SendOnParentAssignedEvents([NotNull]IParentDrawer parent)
		{
			SendOnParentAssignedEvents(parent, parent.Members);
		}

		public static void SendOnParentAssignedEvents([NotNull]IParentDrawer parent, [NotNull]IDrawer[] members)
		{
			for(int n = members.Length - 1; n >= 0; n--)
			{
				members[n].OnParentAssigned(parent);
			}
		}
		
		public static void OnFilterChanged(IParentDrawer subject, SearchFilter filter, Action<SearchFilter> baseOnFilterChanged)
		{
			if(!subject.Inactive)
			{
				var members = subject.Members;
				for(int n = members.Length - 1; n >= 0; n--)
				{
					var memb = members[n];
					if(memb != null)
					{
						memb.OnFilterChanged(filter);
					}
					#if DEV_MODE && DEBUG_NULL_MEMBERS
					else { Debug.LogWarning(subject.ToString() + ".OnFilterChanged - null member "+(n+1)+"of"+ members.Length+"\nsubject.Parent="+StringUtils.ToString(subject.Parent)); }
					#endif
				}
			}
			
			baseOnFilterChanged(filter);
			subject.UpdateVisibleMembers();
		}

		public static bool UpdateVisibleMembers(IParentDrawer subject)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			if(subject.Members == subject.VisibleMembers && subject.Members.Length > 0) { Debug.LogError(subject.ToString()+" members array references same array as visible members - This can lead to bugs!\n"+StringUtils.ToString(subject.Members)); }
			#endif

			#if DEV_MODE && DEBUG_UPDATE_VISIBLE_MEMBERS
			if(subject.GetType() == typeof(NullableDrawer))
			Debug.Log(StringUtils.ToColorizedString("UpdateVisibleMembers(", subject, ") called with MembersAreVisible=", subject.MembersAreVisible, ", Unfoldedness=", subject.Unfoldedness, ", Unfolded=", subject.Unfolded, ", DrawInSingleRow=", subject.DrawInSingleRow, ", Members.Length=", subject.Members.Length));
			#endif

			if(subject.MembersAreVisible)
			{
				var members = subject.MembersBuilt;
				int count = members.Length;
				var list = DrawerListPool.Create(count);

				for(int n = 0; n < count; n++)
				{
					var test = members[n];
					if(test != null)
					{
						if(test.ShouldShowInInspector)
						{
							list.Add(test);
						}
						#if DEV_MODE && DEBUG_UPDATE_VISIBLE_MEMBERS_STEPS
						else { Debug.Log(subject + ".UpdateVisibleMembers() - member "+(n+1)+"/"+count+" "+test.ToString()+" ShowInInspector was false"); }
						#endif
					}
					#if DEV_MODE && DEBUG_NULL_MEMBERS
					else { Debug.LogWarning(subject + ".UpdateVisibleMembers() - null member "+(n+1)+"of"+count + "\nsubject.Parent=" + StringUtils.ToString(subject.Parent)); }
					#endif
				}

				if(list.ContentsMatch(subject.VisibleMembers))
				{
					#if DEV_MODE && DEBUG_UPDATE_VISIBLE_MEMBERS
					Debug.Log("UpdateVisibleMembers - List " + StringUtils.ToString(list)+" ContentsMatch VisibleMembers " + StringUtils.ToString(subject.VisibleMembers) + ": aborting...");
					#endif

					DrawerListPool.Dispose(ref list);
					return false;
				}

				subject.SetVisibleMembers(DrawerArrayPool.CreateAndDisposeList(ref list), !subject.Inactive);
			}
			else
			{
				#if DEV_MODE && DEBUG_UPDATE_VISIBLE_MEMBERS_STEPS
				Debug.Log(subject + ".UpdateVisibleMembers() - MembersAreVisible was false");
				#endif

				if(subject.VisibleMembers.Length == 0)
				{
					return false;
				}
				subject.SetVisibleMembers(ArrayPool<IDrawer>.ZeroSizeArray, !subject.Inactive);
			}

			#if DEV_MODE && DEBUG_UPDATE_VISIBLE_MEMBERS
			if(subject.GetType() == typeof(NullableDrawer))
			Debug.Log(subject.GetType().Name+" / \""+subject.Name+"\".UpdateVisibleMembers() after: "+subject.VisibleMembers.Length+"/"+subject.Members.Length+": "+StringUtils.TypesToString(subject.VisibleMembers, ", "));
			#endif
			
			return true;
		}

		/// <summary>
		/// Checks whether ParentDrawer pass the search filter.
		/// True if ShowInInspector is true for any visible member or if basePassesSearchFilter returns true.
		/// </summary>
		/// <param name="subject"> The subject to test. </param>
		/// <param name="filter"> The filter to test against. </param>
		/// <param name="basePassesSearchFilter"> If no member of subject passes search filter, then this is used to test if subject itself passes filter. </param>
		/// <returns> True if ShowInInspector is true for any member of subject or if subject passes the search filter. </returns>
		public static bool PassesSearchFilter(IParentDrawer subject, SearchFilter filter, Func<SearchFilter, bool> basePassesSearchFilter)
		{
			if(!filter.HasFilterAffectingInspectedTargetContent)
			{
				return true;
			}

			//if any member passed search filter then parent should also pass the search filter
			//since otherwise the child would not be drawn
			if(subject.MembersAreVisible)
			{
				var members = subject.VisibleMembers;
				for(int n = members.Length - 1; n >= 0; n--)
				{
					var memb = members[n];
					if(memb != null)
					{
						if(memb.ShouldShowInInspector)
						{
							#if DEV_MODE && DEBUG_PASSES_SEARCH_FILTER
							Debug.Log(subject + ".PassesSearchFilter(\""+filter.RawInput + "\"): "+ StringUtils.True + " (because VisibleMembers["+n+"].ShowInInspector was "+StringUtils.True+")");
							#endif

							return true;
						}
					}
					#if DEV_MODE && DEBUG_NULL_MEMBERS
					else { Debug.LogError(subject+".PassesSearchFilter() - member #"+n+" was null"); }
					#endif
				}
			}

			return basePassesSearchFilter(filter);
		}

		/// <summary>
		/// Tries to find Drawer for target field in members.
		/// This is a slow method, and using DrawerGroup.FindDrawer(Component) should be used instead when possible.
		/// </summary>
		/// <param name="checkMembers">members to go through when finding field</param>
		/// <param name="field"> The field whose Drawer should be returned </param>
		/// <returns>
		/// The found graphical user interface drawers.
		/// </returns>
		public static IDrawer FindDrawerForField(IDrawer[] checkMembers, FieldInfo field)
		{
			for(int n = checkMembers.Length - 1; n >= 0; n--)
			{
				var member = checkMembers[n];
				var fieldInfo = member.MemberInfo;
				if(fieldInfo != null && fieldInfo.MemberInfo == field)
				{
					return member;
				}
			}

			for(int n = checkMembers.Length - 1; n >= 0; n--)
			{
				var parent = checkMembers[n] as IParentDrawer;
				if(parent != null)
				{
					var found = FindDrawerForField(parent.Members, field);
					if(found != null)
					{
						return found;
					}
				}
			}
			
			return null;
		}

		/// <summary>
		/// Gets zero-based index of member, starting from the left, amongst the all the
		/// visible member drawers drawn on the same row.
		/// If DrawInSingleRow is false, then this should always return zero.
		/// </summary>
		/// <param name="parent"> The parent that owns the member. </param>
		/// <param name="member"> The member whose index to get. </param>
		/// <returns> The member index on the row. </returns>
		public static int GetMemberRowIndex(IParentDrawer parent, IDrawer member)
		{
			var members = parent.VisibleMembers;
			for(int n = members.Length - 1; n >= 0; n--)
			{
				if(members[n] == member)
				{
					return parent.DrawInSingleRow ? n : 0;
				}
			}
			return -1;
		}

		public static void AddMenuItemsFromContextMenuAttribute(object[] targets, ref Menu menu)
		{
			if(targets == null || targets.Length == 0)
			{
				return;
			}

			var target = targets[0];
			if(target == null)
			{
				return;
			}

			bool isFirst = true;
			for(var type = target.GetType(); type != null; type = type.BaseType)
			{
				if(type == Types.MonoBehaviour || type == Types.ScriptableObject)
				{
					return;
				}

				var methodInfos = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Public);
				var methodCount = methodInfos.Length;

				for(int m = 0; m < methodCount; m++)
				{
					var methodInfo = methodInfos[m];
					var items = methodInfo.GetCustomAttributes(Types.ContextMenu, true);
					int count = items.Length;

					if(count > 0)
					{
						for(int n = 0; n < count; n++)
						{
							if(isFirst)
							{
								isFirst = false;
								menu.AddSeparatorIfNotRedundant();
							}

							var item = items[n] as ContextMenu;
							if(!item.validate) //TO DO: handle greying out
							{
								menu.Add(item.menuItem, () =>
								{
									methodInfo.AutoInvoke(targets);
								});
							}
						}
					}
				}
			}
		}
		
		public static void OnDisposing([NotNull]IInspector inspector, [NotNull]IParentDrawer subject)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(inspector != null);
			Debug.Assert(subject != null);
			#endif

			try
			{
				if(inspector.HasFilterAffectingInspectedTargetContent)
				{
					inspector.State.drawers.RestoreFoldedStateWhenDisposingDuringFiltering(subject);
				}
			}
			catch(NullReferenceException e) //TEMP
			{
				Debug.LogError(e);
			}
		}
	}
}