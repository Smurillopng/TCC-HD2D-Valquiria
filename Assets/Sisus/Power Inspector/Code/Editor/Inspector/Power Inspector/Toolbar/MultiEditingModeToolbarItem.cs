using Sisus;
using Sisus.Attributes;
using UnityEngine;

[ToolbarItemFor(typeof(PowerInspectorToolbar), 30, ToolbarItemAlignment.Right, true)]
public class MultiEditingModeToolbarItem : ToolbarItem
{
	private const float Width = 20f;
	private const float IconSize = 20f;

	private Rect iconRect = new Rect();
	private GUIContent mergedLabel;
	private GUIContent stackedLabel;

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
			return PowerInspectorDocumentation.GetTerminologyUrl("multi-editing-modes");
		}
	}

	/// <inheritdoc/>
	protected override void Setup()
	{
		var labels = inspector.Preferences.labels;
		mergedLabel = labels.MergedMultiEditing;
		stackedLabel = labels.StackedMultiEditing;
	}

	/// <inheritdoc/>
	public override bool ShouldShow()
	{
		// Only show this toolbar item when multiple targets are being inspected
		return inspector.State.inspected.Length > 1;
	}

	/// <inheritdoc/>
	protected override void UpdateDrawPositions(Rect itemPosition)
	{
		iconRect.x = itemPosition.x;
		iconRect.y = itemPosition.y + (toolbar.Height - IconSize) * 0.5f;
		iconRect.width = IconSize;
		iconRect.height = IconSize;
	}

	/// <inheritdoc/>
	protected override void OnRepaint(Rect itemPosition)
	{
		GUI.Label(iconRect, UserSettings.MergedMultiEditMode ? mergedLabel : stackedLabel, InspectorPreferences.Styles.Blank);
	}

	/// <inheritdoc/>
	protected override bool OnActivated(Event inputEvent, bool isClick)
	{
		SetMergedMultiEditingMode(!UserSettings.MergedMultiEditMode);

		// return true to consume the input event
		return true;
	}

	/// <inheritdoc/>
	protected override void BuildContextMenu(ref Menu menu, bool extendedMenu)
	{
		var merged = UserSettings.MergedMultiEditMode;
		menu.Add("Merged Multi-Editing", "", ()=> SetMergedMultiEditingMode(true), merged);
		menu.Add("Stacked Multi-Editing", "", ()=> SetMergedMultiEditingMode(false), !merged);

		// call base to add Help menu item
		base.BuildContextMenu(ref menu, extendedMenu);
	}

	private void SetMergedMultiEditingMode(bool setTo)
	{
		UserSettings.MergedMultiEditMode = setTo;
		inspector.ForceRebuildDrawers();
	}
}