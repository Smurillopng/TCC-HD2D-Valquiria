using Sisus;
using Sisus.Attributes;
using UnityEngine;

[ToolbarItemFor(typeof(PowerInspectorToolbar), 40, ToolbarItemAlignment.Left, true)]
public class DebugModePlusToolbarItem : ToolbarItem
{
	private const float Width = 20f;
	private const float IconSize = 20f;

	private Rect iconRect = new Rect();
	private GUIContent label;

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
			return PowerInspectorDocumentation.GetTerminologyUrl("debug-mode");
		}
	}

	/// <inheritdoc/>
	protected override void Setup()
	{
		label = GUIContentPool.Create(inspector.Preferences.graphics.DebugModeOnIcon, "Debug Mode+: On\nAll class members will be listed even if normally hidden.");
	}

	/// <inheritdoc/>
	public override bool ShouldShow()
	{
		// Only show this toolbar item when debug mode+ is enabled
		return inspector.State.DebugMode;
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
	protected override void OnGUI(Rect itemPosition, bool mouseovered)
	{
		GUI.Label(iconRect, label, InspectorPreferences.Styles.Blank);
	}

	/// <inheritdoc/>
	protected override bool OnActivated(Event inputEvent, bool isClick)
	{
		inspector.DisableDebugMode();

		// return true to consume the input event
		return true;
	}

	/// <inheritdoc/>
	protected override void BuildContextMenu(ref Menu menu, bool extendedMenu)
	{
		menu.Add("Debug Mode+/Off", "Disable Debug Mode For All Inspected Targets", inspector.DisableDebugMode, false);
		menu.Add("Debug Mode+/On", "Enable Debug Mode For All Inspected Targets", inspector.EnableDebugMode, true);

		// call base to add Help menu item
		base.BuildContextMenu(ref menu, extendedMenu);
	}
}