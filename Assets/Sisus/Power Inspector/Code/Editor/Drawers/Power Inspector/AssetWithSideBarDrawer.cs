//#define DEBUG_MOUSEOVER_DETECTION

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sisus
{
	/// <summary>
	/// Handles drawing the InspectorPreferences component inside the inspector view.
	/// </summary>
	public class AssetWithSideBarDrawer : AssetDrawer, IAssetWithSideBarDrawer
	{
		public float sideBarItemHeight = 25f;
		private float height;

		private readonly Dictionary<string, List<LinkedMemberInfo>> memberBuildListByHeader = new Dictionary<string, List<LinkedMemberInfo>>(0);
		private int activeHeaderIndex;
		private string[] headers;
		private float sideBarWidth;
		private Rect sideBarPosition;
		private Vector2 scrollPosition;		

		/// <inheritdoc/>
		public bool HasScrollView
		{
			get
			{
				return true;
			}
		}

		private bool HasSideBar
		{
			get
			{
				return true;
			}
		}

		/// <inheritdoc/>
		public override PrefixResizer PrefixResizer
		{
			get
			{
				return PrefixResizer.Disabled;
			}
		}

		public string ActiveView
		{
			get
			{
				return activeHeaderIndex == -1 ? "" : headers[activeHeaderIndex];
			}
		}

		public int ActiveViewIndex
		{
			get
			{
				return activeHeaderIndex;
			}
		}

		public string[] Views
		{
			get
			{
				return headers;
			}
		}
		
		/// <inheritdoc/>
		protected override string OverrideDocumentationUrl(out string documentationTitle)
		{
			documentationTitle = "Preferences";
			return PowerInspectorDocumentation.GetCategoryUrl("preferences");
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="targets"> The targets that the drawers represent. </param>
		/// <param name="parent"> The parent drawers of the created drawers. Can be null. </param>
		/// <param name="inspector"> The inspector in which the IDrawer are contained. Can not be null. </param>
		/// <returns> The drawer instance, ready to be used. </returns>
		public new static AssetWithSideBarDrawer Create([NotNull]Object[] targets, [CanBeNull]IParentDrawer parent, [NotNull]IInspector inspector)
		{
			#if DEV_MODE
			Debug.Assert(inspector != null, "InspectorPreferences.Create inspector was null for targets "+StringUtils.ToString(targets));
			#endif

			AssetWithSideBarDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new AssetWithSideBarDrawer();
			}
			result.Setup(targets, parent, null, inspector);
			result.LateSetup();
			return result;
		}
		
		public void SetActiveView(int index)
		{
			if(index < 0)
			{
				index = 0;
			}
			else if(index >= headers.Length)
			{
				index = headers.Length - 1;
			}
			
			if(activeHeaderIndex != index)
			{
				activeHeaderIndex = index;
				Inspector.InspectorDrawer.RefreshView();

				// Delay to improve UI responsiveness
				OnNextLayout(UpdateMembersForView);
			}
		}

		private void UpdateMembersForView()
		{
			UpdateActiveMemberBuildList();

			// Delay to improve UI responsiveness
			OnNextLayout(RebuildMembers);
		}

		/// <inheritdoc/>
		public override void OnAfterMembersBuilt()
		{
			// If left arrow button is pressed on any root member, then select this (as focus is moved to the side bar)
			for(int n = members.Length - 1; n >= 0; n--)
			{
				members[n].OnKeyboardInputBeingGiven += OnMemberKeyboardInputBeingGiven;
			}
			base.OnAfterMembersBuilt();
		}
		
		private bool OnMemberKeyboardInputBeingGiven(IDrawer keyboardInputReceiver, Event inputEvent, KeyConfigs keys)
		{
			if(inputEvent.keyCode == KeyCode.LeftArrow)
			{
				Select(ReasonSelectionChanged.SelectControlLeft);
				DrawGUI.Use(inputEvent);
				return true;
			}
			return false;
		}

		/// <inheritdoc/>
		public void SetActiveView(string setActive)
		{
			SetActiveView(Array.IndexOf(headers, setActive));
		}

		/// <inheritdoc/>
		public void SetNextViewActive(bool loopToBeginningIfOutOfBounds)
		{
			int count = headers.Length;
			if(count == 0)
			{
				return;
			}

			int next = ActiveViewIndex + 1;
			if(next < count)
			{
				SetActiveView(headers[next]);
			}
			else if(loopToBeginningIfOutOfBounds)
			{

				SetActiveView(headers[0]);
			}
		}

		/// <inheritdoc/>
		public void SetPreviousViewActive(bool loopToEndIfOutOfBounds)
		{
			int count = headers.Length;
			if(count == 0)
			{
				return;
			}

			int previous = ActiveViewIndex - 1;
			if(previous >= 0)
			{
				SetActiveView(headers[previous]);
			}
			else if(loopToEndIfOutOfBounds)
			{
				SetActiveView(headers[count - 1]);
			}
		}

		/// <inheritdoc/>
		protected override void DoGenerateMemberBuildList()
		{
			base.DoGenerateMemberBuildList();
			
			sideBarWidth = 0f;

			memberBuildListByHeader.Clear();
			
			string currentHeader = "General";
			var list = new List<LinkedMemberInfo>(10);
			for(int n = 0, count = memberBuildList.Count; n < count; n++)
			{
				var memberInfo = memberBuildList[n];
				var header = memberInfo.GetAttribute<HeaderAttribute>();
				if(header != null)
				{
					AddToBuildListByHeader(currentHeader, list);
					currentHeader = header.header;
				}
				list.Add(memberInfo);
			}
			AddToBuildListByHeader(currentHeader, list);
			currentHeader = "";

			// add some padding to side bar width
			sideBarWidth += 5f;

			headers = memberBuildListByHeader.Keys.ToArray();
			activeHeaderIndex = 0;
			
			memberBuildListByHeader.Add("", memberBuildList);

			UpdateActiveMemberBuildList();

			height = headers.Length * sideBarItemHeight  + HeaderHeight;

			#if DEV_MODE
			Debug.Log(StringUtils.ToColorizedString("headers=", headers, ", ActiveView=", ActiveView, "\nmemberBuildListByHeader:\n", StringUtils.ToString(memberBuildListByHeader, "\n")));
			#endif
		}

		/// <inheritdoc/>
		protected override void DoBuildMembers()
		{
			base.DoBuildMembers();

			RemoveDuplicateHeaders(ref members);
		}

		private void RemoveDuplicateHeaders(ref IDrawer[] drawers)
		{
			string lastHeader = "";
			for(int n = 0, count = drawers.Length; n < count; n++)
			{
				var drawer = drawers[n];
				if(drawer is HeaderDrawer)
				{
					string header = drawer.Name;
					if(string.Equals(lastHeader, header))
					{
						drawers = drawers.RemoveAt(n);
						n--;
						count--;
					}
					lastHeader = header;
				}
			}
		}

		private void AddToBuildListByHeader(string header, List<LinkedMemberInfo> list)
		{
			if(list.Count == 0)
			{
				return;
			}

			// if the same header is used multiple times, merge all members together.
			// This makes it possible to combine fields, properties and methods under
			// the same header.
			List<LinkedMemberInfo> existingList;
			if(memberBuildListByHeader.TryGetValue(header, out existingList))
			{
				existingList.AddRange(list);
			}
			else
			{
				var copy = new List<LinkedMemberInfo>(list);
				memberBuildListByHeader.Add(header, copy);

				if(Event.current != null)
                {
					float width = GUI.skin.button.CalcSize(new GUIContent(header)).x;
					if(width > sideBarWidth)
					{
						sideBarWidth = width;
					}
				}
			}
			list.Clear();
		}

		private void UpdateActiveMemberBuildList()
		{
			memberBuildList = HasSideBar ? memberBuildListByHeader[ActiveView] : memberBuildListByHeader[""];
		}

		/// <inheritdoc cref="IDrawer.OnFilterChanged" />
		public override void OnFilterChanged(SearchFilter filter)
		{
			UpdateActiveMemberBuildList();
			base.OnFilterChanged(filter);
		}

		/// <inheritdoc cref="IDrawer.Height" />
		public override float Height
		{
			get
			{
				return height;
			}
		}

		private float ContentHeight
		{
			get
			{
				return base.Height;
			}
		}
		
		/// <inheritdoc/>
		public override bool DrawBody(Rect position)
		{
			if(!HasSideBar)
			{
				return base.DrawBody(position);
			}

			bool dirty = DrawSideBar(sideBarPosition);

			if(position.width < sideBarWidth + PrefixLabelWidth + 5f)
			{
				return dirty;
			}

			var viewRect = position;
			viewRect.height = ContentHeight;
			viewRect.x = 0f;
			viewRect.y = 0f;

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(viewRect.height > 0f);
			#endif
			
			position.height = DrawGUI.InspectorHeight - Inspector.ToolbarHeight - HeaderHeight;

			bool hasScrollBar = viewRect.height > position.height;
			if(hasScrollBar)
			{
				viewRect.width -= DrawGUI.ScrollBarWidth;
			}

			DrawGUI.BeginScrollView(position, viewRect, ref scrollPosition);
			{
				if(DrawBodyMultiRow(viewRect))
				{
					dirty = true;
				}
			}
			DrawGUI.EndScrollView();

			return dirty;
		}

		/// <inheritdoc cref="IDrawer.DetectMouseoverForSelfAndChildren" />
		public override bool DetectMouseoverForSelfAndChildren(Vector2 mousePosition)
		{
			if(ClickToSelectArea.Contains(mousePosition))
			{
				#if DEV_MODE && DEBUG_MOUSEOVER_DETECTION
				Debug.Log(StringUtils.ToColorizedString("DetectMouseoverForSubjectAndChildren(", subjethisct, "): ", true, "! (via self)"));
				#endif
				return true;
			}
			
			var memberCoordinateSpaceOffset = bodyLastDrawPosition.position - lastDrawPosition.position;

			return ParentDrawerUtility.DetectMouseoverForVisibleMembers(this, mousePosition - memberCoordinateSpaceOffset);
		}

		/// <inheritdoc cref="IDrawer.DetectMouseover" />
		public override bool DetectMouseover(Vector2 mousePosition)
		{
			if(ClickToSelectArea.Contains(mousePosition))
			{
				#if DEV_MODE && DEBUG_MOUSEOVER_DETECTION
				Debug.Log(StringUtils.ToColorizedString("DetectMouseover(", this, "): Contains(mousePos): ", true, " with "+visibleMembers.Length+" VisibleMembers..."));
				#endif

				if(PrefixResizerMouseovered)
				{
					#if DEV_MODE && DEBUG_MOUSEOVER_DETECTION
					Debug.Log(StringUtils.ToColorizedString("DetectMouseover(", this, "): ", this == unityObject, " because !PrefixResizingEnabledOverControl and PrefixResizerMouseovered"));
					#endif
					return true;
				}

				var memberCoordinateSpaceOffset = bodyLastDrawPosition.position - lastDrawPosition.position;

				if(ParentDrawerUtility.DetectMouseoverForVisibleMembers(this, mousePosition - memberCoordinateSpaceOffset))
				{
					return false;
				}
				
				#if DEV_MODE && DEBUG_MOUSEOVER_DETECTION
				Debug.Log(StringUtils.ToColorizedString("DetectMouseover(", this, "): ", true, "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!"));
				#endif

				return true;
			}

			#if DEV_MODE && DEBUG_MOUSEOVER_DETECTION
			Debug.Log(StringUtils.ToColorizedString("DetectMouseover(", this, "): ", ClickToSelectArea, ".MouseIsOver: ", false));
			#endif

			return false;
		}
		
		/// <inheritdoc/>
		protected override void GetDrawPositions(Rect position)
		{
			base.GetDrawPositions(position);

			if(HasSideBar)
			{
				sideBarPosition = bodyLastDrawPosition;
				sideBarPosition.width = sideBarWidth;

				bodyLastDrawPosition.width = bodyLastDrawPosition.width - sideBarWidth;
				bodyLastDrawPosition.x += sideBarWidth;
			}
		}
		
		private bool DrawSideBar(Rect position)
		{
			bool changed = false;

			var drawRect = position;
			drawRect.height = sideBarItemHeight;
			var guiColorWas = GUI.color;
			for(int n = 0, count = headers.Length; n < count; n++)
			{
				var header = headers[n];
				if(n == activeHeaderIndex)
				{
					GUI.color = Selected ? Inspector.Preferences.theme.BackgroundSelected : Color.grey;
					GUI.Label(drawRect, header, InspectorPreferences.Styles.SideBarItem);
				}
				else
				{
					GUI.color = guiColorWas;
					if(GUI.Button(drawRect, header, InspectorPreferences.Styles.SideBarItem))
					{
						SetActiveView(header);
						changed = true;
					}
				}
				drawRect.y += sideBarItemHeight;
			}
			GUI.color = guiColorWas;

			drawRect.height = DrawGUI.InspectorHeight - drawRect.y;
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(drawRect.height > 0f, "DrawSideBar("+drawRect+") height <= 0f: " + drawRect.height);
			#endif
			GUI.Label(drawRect, "", InspectorPreferences.Styles.SideBarItem);

			return changed;
		}

		/// <inheritdoc cref="IAssetDrawer.AddItemsToOpeningViewMenu" />
		public override void AddItemsToOpeningViewMenu(ref Menu menu)
		{
			menu.Add("Help/Preferences", PowerInspectorDocumentation.ShowCategory, "preferences");
			base.AddItemsToOpeningViewMenu(ref menu);
		}

		/// <inheritdoc cref="IDrawer.Dispose" />
		public override void Dispose()
		{
			headers = null;
			activeHeaderIndex = 0;
			memberBuildListByHeader.Clear();
			base.Dispose();
		}

		/// <inheritdoc cref="IDrawer.OnKeyboardInputGiven" />
		public override bool OnKeyboardInputGiven(Event inputEvent, KeyConfigs keys)
		{
			if(inputEvent.type == EventType.KeyDown) // && sideBarSelected)
			{
				switch(inputEvent.keyCode)
				{
					case KeyCode.Tab:
						if(inputEvent.shift)
						{
							SetPreviousViewActive(true);
							DrawGUI.Use(inputEvent);
							return true;
						}
						SetNextViewActive(true);
						DrawGUI.Use(inputEvent);
						return true;
					case KeyCode.UpArrow:
						SetPreviousViewActive(false);
						DrawGUI.Use(inputEvent);
						return true;
					case KeyCode.DownArrow:
						SetNextViewActive(false);
						DrawGUI.Use(inputEvent);
						return true;
					case KeyCode.Home:
						SetActiveView(0);
						return true;
					case KeyCode.End:
						SetActiveView(headers.Length - 1);
						return true;
					case KeyCode.PageUp:
						SetActiveView(activeHeaderIndex - 10);
						return true;
					case KeyCode.PageDown:
						SetActiveView(activeHeaderIndex + 10);
						return true;
					case KeyCode.RightArrow:
						SelectNextFieldDown(0);
						return true;
				}
			}
			return base.OnKeyboardInputGiven(inputEvent, keys);
		}
	}
}