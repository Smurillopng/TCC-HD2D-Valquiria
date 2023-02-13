#if !POWER_INSPECTOR_LITE
using UnityEngine;
using Sisus.Attributes;

namespace Sisus
{
	[ToolbarItemFor(typeof(PowerInspectorToolbar), 10, ToolbarItemAlignment.Right, true)]
	public class SplitViewToolbarItem : ToolbarItem
	{
		private const float Width = 20f;
		private const float IconSize = 12f;
		private const float XPadding = 4f;

		private Rect iconRect = new Rect();
		private InspectorLabels labels;
		private ISplittableInspectorDrawer splittableDrawer;

		/// <inheritdoc/>
		public override float MinWidth
		{
			get
			{
				return Width;
			}
		}

		/// <inheritdoc/>
		public override float MaxWidth
		{
			get
			{
				return Width;
			}
		}

		/// <inheritdoc/>
		public override string DocumentationPageUrl
		{
			get
			{
				return PowerInspectorDocumentation.GetFeatureUrl("split-view");
			}
		}

		/// <inheritdoc/>
		protected override void Setup()
		{
			labels = inspector.Preferences.labels;
			splittableDrawer = inspector.InspectorDrawer as ISplittableInspectorDrawer;
		}

		/// <inheritdoc/>
		public override bool ShouldShow()
		{
			return base.ShouldShow() && splittableDrawer != null;
		}

		/// <inheritdoc/>
		protected override void UpdateDrawPositions(Rect itemPosition)
		{
			iconRect.x = itemPosition.x + XPadding;
			iconRect.y = itemPosition.y + (toolbar.Height - IconSize) * 0.5f;
			iconRect.width = IconSize;
			iconRect.height = IconSize;
		}

		/// <inheritdoc/>
		public override void DrawBackground(Rect itemPosition, GUIStyle toolbarBackgroundStyle) { }

		/// <inheritdoc/>
		protected override void OnRepaint(Rect itemPosition)
		{
			GUI.Label(iconRect, splittableDrawer.ViewIsSplit ? labels.CloseSplitViewIcon : labels.SplitViewIcon, InspectorPreferences.Styles.Blank);
		}

		/// <inheritdoc/>
		protected override bool OnActivated(Event inputEvent, bool isClick)
		{
			DrawGUI.Use(inputEvent);

			if(!splittableDrawer.ViewIsSplit)
			{
				inspector.OnNextLayout(splittableDrawer.OpenSplitView);
			}
			else if(inspector == splittableDrawer.MainView)
			{
				inspector.OnNextLayout(splittableDrawer.CloseMainView);
			}
			else
			{
				inspector.OnNextLayout(splittableDrawer.CloseSplitView);
			}
			return true;
		}

		/// <inheritdoc/>
		public override bool OnRightClick(Event inputEvent)
		{
			var menu = Menu.Create();
			menu.Add("Help", OpenDocumentation);
			menu.OpenAt(Bounds);
			return true;
		}

		private static void OpenDocumentation()
		{
			PowerInspectorDocumentation.ShowFeature("split-view");
		}

		protected override void OnCopyCommandGiven()
		{
			var inspected = inspector.State.inspected;
			if(inspected.Length > 0 && inspected[0] != null)
			{
				if(inspected.Length == 1)
				{
					Clipboard.CopyObjectReference(inspected[0], inspected[0].GetType());
					Clipboard.SendCopyToClipboardMessage("Copied{0} reference.", "Inspected");
				}
				else
				{
					Clipboard.CopyObjectReferences(inspected, inspected[0].GetType());
					Clipboard.SendCopyToClipboardMessage("Copied{0} references.", "Inspected");
				}
			}
		}

		/// <inheritdoc/>
		protected override void OnPasteCommandGiven()
		{
			if(Clipboard.HasObjectReference())
			{
				inspector.RebuildDrawers(Clipboard.PasteObjectReferences().ToArray(), false);
			}
		}
	}
}
#endif