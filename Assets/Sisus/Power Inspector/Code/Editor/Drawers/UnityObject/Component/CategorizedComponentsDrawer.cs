#define ENABLE_UNFOLD_ANIMATIONS

//#define DEBUG_SET_UNFOLDED
//#define DEBUG_ON_MOUSE_UP

using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEditor;

namespace Sisus
{
	/// <summary>
	/// Same as DataSetDrawer except it is for displaying custom members.
	/// Won't automatically build its members, but expects SetMembers to be used for manually setting them.
	/// </summary>
	[Serializable]
	public class CategorizedComponentsDrawer : ParentDrawer<Component[]>
	{
		private static Color labelIdleColor;
		private static bool labelIdleColorCached;

		/// <summary>
		/// The width
		/// </summary>
		private const float IconWidth = 16f;

		/// <summary>
		/// Total horizontal offset between two component icons. measured from the same edge of both icons.
		/// </summary>
		private const float IconOffset = 20f;

		/// <summary>
		/// Total horizontal space used for drawing the header label, measured from the very left edge of the header,
		/// and including the space needed to draw the foldout control.
		/// </summary>
		private float labelRequiredWidth;

		/// <summary>
		/// Total horizontal space used for drawing the icons, measured from the very right edge of the header.
		/// </summary>
		private float iconsTotalWidth = 0f;

		/// <summary>
		/// Class responsible for drawing the prefix label.
		/// </summary>
		protected PrefixDrawer prefixLabelDrawer;

		private readonly TweenedBool unfoldedness = new TweenedBool();

		private int mouseoveredIconIndex = -1;

		private GUIContent[] icons;

		private GUIStyle labelGuiStyle;

		

		/// <inheritdoc/>
		public override int AppendIndentLevel
		{
			get
			{
				return 0;
			}
		}

		/// <inheritdoc/>
		public override bool Selectable
		{
			get
			{
				return passedLastFilterCheck && ShownInInspector;
			}
		}

		/// <inheritdoc/>
		public override float HeaderHeight
		{
			get
			{
				#if UNITY_2019_3_OR_NEWER
				return 30f;
				#else
				return 25f;
				#endif
			}
		}

		/// <inheritdoc/>
		public override Type Type
		{
			get { return typeof(object[]); }
		}

		/// <inheritdoc/>
		protected override bool RebuildingMembersAllowed
		{
			get { return false; }
		}

		/// <inheritdoc/>
		public override float Unfoldedness
		{
			get
			{
				return unfoldedness;
			}
		}

		/// <inheritdoc/>
		public override bool Unfolded
		{
			get
			{
				return unfoldedness;
			}

			set
			{
				bool was = Unfolded;

				#if DEV_MODE && DEBUG_SET_UNFOLDED
				Debug.Log(StringUtils.ToColorizedString(ToString(), ".Unfolded = ", value, " (was: ", was, ") with inactive=", inactive, ", MembersAreVisible=", MembersAreVisible, ", Foldable=", Foldable, ", memberBuildState=", memberBuildState));
				#endif
				
				if(value == was)
				{
					#if DEV_MODE && DEBUG_SET_UNFOLDED
					Debug.Log(StringUtils.ToColorizedString(ToString(), ".Unfolded = ", value, " aborting because Unfolded already matched value..."));
					#endif

					#if DEV_MODE && PI_ASSERTATIONS
					Debug.Assert(value || unfoldedness <= 0f, StringUtils.ToColorizedString(ToString(), " Unfoldedness = ", value, " - Unfolded aready was ", value, ", but unfoldedness was ", (float)unfoldedness));
					Debug.Assert(!value || unfoldedness >= 1f, StringUtils.ToColorizedString(ToString(), " Unfoldedness = ", value, " - Unfolded aready was ", value, ", but unfoldedness was ", (float)unfoldedness));
					#endif
					return;
				}

				if(prefixLabelDrawer != null)
				{
					prefixLabelDrawer.Unfolded = value;
					UpdatePrefixDrawer();
				}

				#if ENABLE_UNFOLD_ANIMATIONS
				if(value)
				{
					// skip folding state change animations when there's a filter,
					// to make the changes to the inspector feel snappier when the
					// user is altering the filter
					if(Inspector.HasFilterAffectingInspectedTargetContent)
					{
						unfoldedness.SetValueInstant(true);
					}
					else
					{
						unfoldedness.SetTarget(Inspector.InspectorDrawer, true);
					}
					OnFullClosednessChanged(true);
				}
				else
				{
					unfoldedness.SetTarget(Inspector.InspectorDrawer, false, OnFullClosednessChanged);
				}
				#else
				unfoldedness.SetValueInstant(value);
				OnFullClosednessChanged(value);
				#endif
			}
		}

		/// <summary>
		/// Called when drawer were fully folded (closedness was 0)
		/// and just now started unfolding (closedness is > 0), or when they
		/// were at least partially unfolded (closedness was > 0) and just became
		/// fully folded (closedness is 0).
		/// </summary>
		/// <param name="unfolded"> True if became partially unfolded, false if became fully folded. </param>
		private void OnFullClosednessChanged(bool unfolded)
		{
			if(inactive)
			{
				return;
			}

			ParentDrawerUtility.OnMemberVisibilityChanged(this, unfolded);

			if(memberBuildState == MemberBuildState.MembersBuilt)
			{
				UpdateVisibleMembers();
			}

			UpdatePrefixDrawer();
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="setReadOnly"> True if control should be read only. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static CategorizedComponentsDrawer Create(IParentDrawer parent, GUIContent label)
		{
			CategorizedComponentsDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new CategorizedComponentsDrawer();
			}
			result.Setup(parent, label);
			result.LateSetup();
			return result;
		}

		/// <summary>
		/// Hides the default constructor. The Create method should be used instead.
		/// </summary>
		private CategorizedComponentsDrawer() { }

		/// <inheritdoc/>
		protected override void Setup(IParentDrawer setParent, GUIContent setLabel)
		{
			base.Setup(setParent, setLabel);
			UpdatePrefixDrawer();
		}

		/// <inheritdoc/>
		public override void LateSetup()
		{
			base.LateSetup();

			//keep inactive flag true until SetMembers has been called
			inactive = true;
		}

		/// <inheritdoc/>
		protected override void DoGenerateMemberBuildList() { }

		/// <inheritdoc/>
		protected override void DoBuildMembers()
		{
			#if DEV_MODE
			Debug.LogWarning(ToString()+".BuildMembers call ignored. SetMembers should be used instead.");
			#endif
		}
		
		/// <inheritdoc/>
		public override void SetMembers(IDrawer[] setMembers, bool sendVisibilityChangedEvents = true)
		{
			#if DEV_MODE
			Debug.Log(ToString()+".SetMembers("+StringUtils.ToString(setMembers)+")");
			#endif

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(!Array.Exists(setMembers, member=>member == null));
			#endif

			inactive = false;
			UpdatePrefixDrawer();

			base.SetMembers(setMembers, sendVisibilityChangedEvents);

			int count = members.Length;
			EditorGUIUtility.SetIconSize(new Vector2(IconWidth, IconWidth));

			icons = new GUIContent[count];
			for(int n = 0; n < count; n++)
			{
				var member = members[n];
				var target = member.UnityObject;
				if(target != null)
				{
					var content = EditorGUIUtility.ObjectContent(target, null);
					var icon = GUIContentPool.Create(content.image, member.Name);
					icons[n] = icon;
				}
				else
				{
					icons[n] = GUIContentPool.Create(Inspector.Preferences.graphics.missingAssetIcon);
				}
			}

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(memberBuildState == MemberBuildState.MembersBuilt);
			#endif
		}

		/// <inheritdoc/>
		public override bool DrawPrefix(Rect position)
		{
			bool textColorAltered = false;

			if(Event.current.type == EventType.Repaint)
			{
				var lineRect = position;
				lineRect.height = 1f;
				var theme = Inspector.Preferences.theme;
				DrawGUI.DrawLine(lineRect, theme.ComponentSeparatorLine);

				#if SAFE_MODE
				if(prefixLabelDrawer == null)
				{
					#if DEV_MODE
					Debug.LogWarning(GetType().Name + ".DrawPrefix - prefixLabelDrawer of "+this+" under parent "+(parent == null ? "null" : parent.ToString())+" was null!");
					#endif

					return false;
				}
				#endif
			
				if(mouseoveredIconIndex == -1)
				{
					if(Selected)
					{
						textColorAltered = true;
						SetLabelGUIStyleColor(theme.PrefixSelectedText);
					}
					else if(Mouseovered)
					{
						if(InspectorUtility.Preferences.mouseoverEffects.unityObjectHeaderTint)
						{
							textColorAltered = true;
							SetLabelGUIStyleColor(theme.PrefixMouseoveredText);
						}
					}
				}
			}

			var drawPos = position;
			drawPos.x += 3f;
			drawPos.width -= 3f;
			drawPos.y += 3f;
			drawPos.height -= 3f;

			if(lastPassedFilterTestType != FilterTestType.None)
			{
				prefixLabelDrawer.Draw(drawPos, Inspector.State.filter, label.text, lastPassedFilterTestType);
			}
			else
			{
				prefixLabelDrawer.Draw(drawPos);
			}

			if(textColorAltered)
			{
				SetLabelGUIStyleColor(labelIdleColor);
			}

			DrawGUI.LayoutSpace(position.height);

			if(Event.current.type == EventType.Repaint)
			{
				DrawComponentIcons(position);
			}

			return false;
		}

		private void SetLabelGUIStyleColor(Color color)
		{
			labelGuiStyle.normal.textColor = color;
			labelGuiStyle.active.textColor = color;

			//new tests to try and get it to work when folded out:
			labelGuiStyle.hover.textColor = color;
			labelGuiStyle.onNormal.textColor = color;
			labelGuiStyle.onActive.textColor = color;
			labelGuiStyle.focused.textColor = color;
			labelGuiStyle.onFocused.textColor = color;
		}

		private void DrawComponentIcons(Rect position)
		{
			var guiColorWas = GUI.color;
			var guiColorEnabled = guiColorWas;
			var guiColorDisabled = guiColorWas;
			guiColorDisabled.a *= 0.5f;

			var drawPos = position;
			drawPos.x = position.xMax;
			drawPos.width = IconWidth;

			float stopX = position.xMax - iconsTotalWidth;

			for(int n = icons.Length - 1; n >= 0; n--)
			{
				drawPos.x -= IconOffset;

				// stop drawing icons when they would start clipping with the prefix label
				if(drawPos.x < stopX)
				{
					break;
				}

				var component = members[n].UnityObject as Component;
				GUI.color = component != null && component.IsEnabled() ? guiColorEnabled : guiColorDisabled;
				GUI.Label(drawPos, icons[n], InspectorPreferences.Styles.Centered);
			}

			GUI.color = guiColorWas;
		}

		/// <inheritdoc/>
		public override bool DrawBody(Rect position)
		{
			float unfoldedness = Unfoldedness;
			if(unfoldedness <= 0f)
			{
				position.height = 1f;
				DrawGUI.DrawLine(position, Inspector.Preferences.theme.ComponentSeparatorLine);

				return false;
			}

			Profiler.BeginSample("CategorizedComponentsDrawer.DrawBody");

			bool dirty;

			if(unfoldedness >= 1f)
			{
				dirty = DrawFoldableContent(position);
			}
			else
			{
				#if DEV_MODE && PI_ASSERTATIONS
				var assertColor = GUI.color;
				#endif

				using(new MemberScaler(position.min, unfoldedness))
				{
					dirty = DrawFoldableContent(position);
				}

				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(GUI.color == assertColor, ToString() + " - After DrawFoldableContent");
				#endif
			}

			Profiler.EndSample();
			
			return dirty;
		}

		private bool DrawFoldableContent(Rect position)
		{
			bool dirty = false;

			DrawGUI.IndentLevel += AppendIndentLevel;

			var colorWas = GUI.color;
			int count = visibleMembers.Length;
			for(int n = 0; n < count; n++)
			{
				IDrawer member;
				try
				{
					member = visibleMembers[n];
				}
				//it's rare but possible for the visibleMembers length to change
				//in the middle of the loop, from actions taken during a member's Draw method
				catch(IndexOutOfRangeException)
				{
					DrawGUI.IndentLevel -= AppendIndentLevel;
					throw new ExitGUIException();
				}

				if(member == null)
				{
					#if DEV_MODE
					Debug.LogError("CategorizedComponentsDrawer member was null during DrawBody!");
					#endif
					RebuildMemberBuildList();
					RebuildMembers();
					DrawGUI.IndentLevel -= AppendIndentLevel;
					throw new ExitGUIException();
				}

				float height = member.Height;

				position.height = height;

				if(member.Draw(position))
				{
					dirty = true;
				}

				position.y += height;

				GUI.color = colorWas;
			}

			DrawGUI.IndentLevel -= AppendIndentLevel;

			return dirty;
		}

		/// <inheritdoc/>
		public override void OnMouseoverEnter(Event inputEvent, bool isDrag)
		{
			GUI.changed = true;
			base.OnMouseoverEnter(inputEvent, isDrag);
		}

		/// <inheritdoc/>
		public override void OnMouseoverExit(Event inputEvent)
		{
			GUI.changed = true;
			base.OnMouseoverExit(inputEvent);
		}

		/// <inheritdoc/>
		public override void OnMouseover()
		{
			#if DEV_MODE
			if(DrawGUI.IsUnityObjectDrag) { Debug.Log("OnMouseover with IsUnityObjectDrag=true, IsDrag=" + Inspector.Manager.MouseDownInfo.IsDrag()); }
			#endif

			if(InspectorUtility.Preferences.mouseoverEffects.unityObjectHeader)
			{
				var rect = lastDrawPosition;
				rect.height = Height;
				rect.y += 1f;
				rect.height -= 2f;
				rect.width -= 1f;
				DrawGUI.DrawLeftClickAreaMouseoverEffect(rect, localDrawAreaOffset);
			}
		}

		/// <inheritdoc cref="IDrawer.OnClick" />
		public override bool OnClick(Event inputEvent)
		{
			if(mouseoveredIconIndex != -1)
			{
				HandleOnClickSelection(inputEvent, ReasonSelectionChanged.ThisClicked);
				DrawGUI.Active.DragAndDropObjectReferences = MembersBuilt[mouseoveredIconIndex].UnityObjects;
				return true;
			}

			return base.OnClick(inputEvent);
		}

		public override void OnMouseoverDuringDrag(MouseDownInfo mouseDownInfo, UnityEngine.Object[] dragAndDropObjectReferences)
		{
			if(!mouseDownInfo.CursorMovedAfterMouseDown)
			{
				// This is just to replace the ugly drag ignored cursor with something a little bit better
				// during click events. Unfortunately DragAndDropVisualMode.None didn't seem to work.
				DrawGUI.Active.DragAndDropVisualMode = DragAndDropVisualMode.Move;
				GUI.changed = true;
				return;
			}

			base.OnMouseoverDuringDrag(mouseDownInfo, dragAndDropObjectReferences);
		}

		/// <inheritdoc cref="IDrawer.OnMouseUpAfterDownOverControl" />
		public override void OnMouseUpAfterDownOverControl(Event inputEvent, bool isClick)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(!isClick || Inspector.MouseoveredPart != InspectorPart.None, "IsClick was true but MouseoveredPart was None!");
			#endif

			#if DEV_MODE && DEBUG_ON_MOUSE_UP
			Debug.Log(ToString()+ ".OnMouseUpAfterDownOverControl with inputEvent="+StringUtils.ToString(inputEvent)+", isClick=" + StringUtils.ToColorizedString(isClick)+ ", CursorMovedAfterMouseDown=" + !Inspector.Manager.MouseDownInfo.CursorMovedAfterMouseDown);
			#endif

			// DragExited event can caused click input to be ignored, due to the fact that it's possible to
			// drag Object references from the icons. However here we want to treat those as clicks.
			if(!isClick && !Inspector.Manager.MouseDownInfo.CursorMovedAfterMouseDown)
			{
				isClick = true;
			}
			
			if(isClick)
			{
				#if DEV_MODE
				if(!isClick) { Debug.LogWarning("OnMouseUpAfterDownOverControl isClick was false but CursorMovedAfterMouseDown was false."); }
				#endif

				DrawGUI.Use(inputEvent);

				bool wasUnfolded = Unfolded;
				if(mouseoveredIconIndex != -1)
				{
					if(!wasUnfolded)
					{
						SetUnfolded(true, false);
					}

					var targetMember = members[mouseoveredIconIndex] as IParentDrawer;
					if(targetMember != null && targetMember.Foldable)
					{
						targetMember.SetUnfolded(!wasUnfolded || !targetMember.Unfolded);
					}
					return;
				}
				else
				{
					SetUnfolded(!wasUnfolded, Event.current.alt);
				}

				// clear Drag N Drop references
				Inspector.Manager.MouseDownInfo.Clear(true);

				DrawGUI.Active.DragAndDropVisualMode = DragAndDropVisualMode.None;

				ExitGUIUtility.ExitGUI();
				return;
			}
			else if(!Mouseovered)
			{
				// helps with issue where label can get stuck in selected / mouseovered color
				SetLabelGUIStyleColor(labelIdleColor);
			}

			base.OnMouseUpAfterDownOverControl(inputEvent, isClick);
		}

		/// <inheritdoc/>
		public override void Dispose()
		{
			SetLabelGUIStyleColor(labelIdleColor);
			labelGuiStyle = null;
			mouseoveredIconIndex = -1;

			base.Dispose();
		}

		/// <summary>
		/// Updates the prefix drawer.
		/// </summary>
		protected virtual void UpdatePrefixDrawer()
		{
			var selected = Selected;
			var mouseovered = Mouseovered;
			const bool unappliedChanges = true;
			const bool textClipping = false;
			var unfolded = Unfolded;

			if(prefixLabelDrawer != null)
			{
				prefixLabelDrawer.Dispose();
			}

			labelGuiStyle = DrawGUI.GetFoldoutStyle(selected, mouseovered, unappliedChanges, textClipping);

			var fontFromStyle = (GUIStyle)"AM MixerHeader2";
			labelGuiStyle.font = fontFromStyle.font;
			labelGuiStyle.fontStyle = fontFromStyle.fontStyle;
			labelGuiStyle.fontSize = fontFromStyle.fontSize;

			if(!labelIdleColorCached)
			{
				labelIdleColorCached = true;
				labelIdleColor = labelGuiStyle.normal.textColor;
			}
			else
			{
				SetLabelGUIStyleColor(labelIdleColor);
			}

			labelRequiredWidth = labelGuiStyle.CalcSize(label).x + 6f;

			prefixLabelDrawer = PrefixDrawer.CreateFoldout(label, unfolded, textClipping, labelGuiStyle);
		}

		/// <inheritdoc/>
		protected override void GetDrawPositions(Rect position)
		{
			base.GetDrawPositions(position);

			mouseoveredIconIndex = -1;
			if(Mouseovered)
			{
				var iconPos = position;
				iconPos.x = position.xMax;
				iconPos.width = IconWidth;
				var mousePos = Event.current.mousePosition;
				for(int n = icons.Length - 1; n >= 0; n--)
				{
					iconPos.x -= IconOffset;

					if(iconPos.Contains(mousePos))
					{
						mouseoveredIconIndex = n;
						break;
					}
				}
			}

			float widthNeededToDrawAllIcons = icons.Length * IconOffset;
			const int MinDisplayedIcons = 4;
			float maxWidthForIcons = Mathf.Max(position.width - labelRequiredWidth, IconOffset * MinDisplayedIcons);
			iconsTotalWidth = Mathf.Min(widthNeededToDrawAllIcons, maxWidthForIcons);
		}

		/// <inheritdoc/>
		protected override void OnSelectedInternal(ReasonSelectionChanged reason, IDrawer previous, bool isMultiSelection)
		{
			base.OnSelectedInternal(reason, previous, isMultiSelection);
			UpdatePrefixDrawer();
			GUI.changed = true;
		}

		/// <inheritdoc/>
		protected override void OnDeselectedInternal(ReasonSelectionChanged reason, [CanBeNull] IDrawer losingFocusTo)
		{
			base.OnDeselectedInternal(reason, losingFocusTo);
			UpdatePrefixDrawer();
			GUI.changed = true;
		}

		/// <inheritdoc/>
		protected override Type GetMemberType(Component[] memberBuildListItem)
		{
			return memberBuildListItem[0].GetType();
		}

		/// <inheritdoc/>
		protected override object GetMemberValue(Component[] memberBuildListItem)
		{
			return memberBuildListItem[0];
		}


		/// <inheritdoc/>
		public override bool OnRightClick(Event inputEvent)
		{
			if(mouseoveredIconIndex != -1)
			{
				var member = MembersBuilt[mouseoveredIconIndex];

				if(!member.Selectable && Selectable)
				{
					Select(ReasonSelectionChanged.ThisClicked);
					ContextMenuUtility.OnMenuClosed += (obj)=>Select(ReasonSelectionChanged.ThisClicked);
				}

				return member.OnRightClick(inputEvent);
			}
			return base.OnRightClick(inputEvent);
		}

		/// <inheritdoc/>
		protected override void BuildContextMenu(ref Menu menu, bool extendedMenu)
		{
			if(BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}

			menu.Add("Collapse All", () => SetUnfolded(false, true));
			menu.Add("Expand All", () => SetUnfolded(true, true));


			if(!BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}
		}
	}
}