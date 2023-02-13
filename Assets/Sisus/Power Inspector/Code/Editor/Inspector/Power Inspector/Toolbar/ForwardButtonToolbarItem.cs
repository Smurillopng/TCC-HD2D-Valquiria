#if !POWER_INSPECTOR_LITE
using System;
using UnityEngine;
using Sisus.Attributes;

namespace Sisus
{
	[ToolbarItemFor(typeof(PowerInspectorToolbar), 20, ToolbarItemAlignment.Left, true)]
	public class ForwardButtonToolbarItem : ToolbarItem
	{
		private const float Width = 28f;

		public Action<Rect, ActivationMethod> onActivated;
		protected InspectorGraphics graphics;

		protected Rect iconRect = new Rect(7f, 3f, 8f, 12f);

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
				return PowerInspectorDocumentation.GetFeatureUrl("back-and-forward-buttons");
			}
		}

		/// <inheritdoc/>
		protected override void Setup()
		{
			var preferences = inspector.Preferences;
			graphics = preferences.graphics;
		}

		/// <inheritdoc/>
		protected override void UpdateDrawPositions(Rect itemPosition)
		{
			iconRect.x = itemPosition.x + 11f;
			iconRect.y = itemPosition.y + (toolbar.Height - 12f) * 0.5f;
		}

		/// <inheritdoc/>
		protected override void OnRepaint(Rect itemPosition)
		{
			if(CanBeActivated())
			{
				GUI.DrawTexture(iconRect, graphics.NavigationArrowRight);
			}
			else
			{
				var color = GUI.color;
				color.a = 0.5f;
				GUI.color = color;
				GUI.DrawTexture(iconRect, graphics.NavigationArrowRight);
				color.a = 1f;
				GUI.color = color;
			}
		}

		/// <inheritdoc/>
		public override bool OnKeyboardInputGivenWhenNotSelected(Event inputEvent, KeyConfigs keys)
		{
			if(keys.openNavigateForwardMenu.DetectAndUseInput(inputEvent))
			{
				OnRightClick(inputEvent);
				return true;
			}

			if(keys.stepForwardInSelectionHistory.DetectAndUseInput(inputEvent))
			{
				OnActivated(inputEvent, false);
				return true;
			}

			return false;
		}

		/// <inheritdoc/>
		public override bool OnKeyboardInputGiven(Event inputEvent, KeyConfigs keys)
		{
			if(OnKeyboardInputGivenWhenNotSelected(inputEvent, keys))
			{
				return true;
			}

			return base.OnKeyboardInputGiven(inputEvent, keys);
		}

		/// <inheritdoc/>
		public override bool OnRightClick(Event inputEvent)
		{
			if(CanBeActivated())
			{
				inspector.State.selectionHistory.OpenNavigateForwardMenuAt(inspector, Bounds);
				GUIUtility.ExitGUI();
				return true;
			}
			return false;
		}

		/// <inheritdoc/>
		public override bool OnMiddleClick(Event inputEvent)
		{
			if(CanBeActivated())
			{
				DrawGUI.Ping(inspector.State.selectionHistory.PeekNextInSelectionHistory());
				GUIUtility.ExitGUI();
				return true;
			}
			return false;
		}

		/// <inheritdoc/>
		protected override bool OnActivated(Event inputEvent, bool isClick)
		{
			if(!CanBeActivated())
			{
				return false;
			}

			if(onActivated != null)
			{
				onActivated(Bounds, ActivationMethod.KeyboardMenu);
			}

			inspector.StepForwardInSelectionHistory();
			return true;
		}
		
		/// <summary>
		/// Determines whether or not this item currently can be activated by clicking or by keyboard commands.
		/// </summary>
		/// <returns> True if can be activated, false if not. </returns>
		protected virtual bool CanBeActivated()
		{
			return inspector.State.selectionHistory.HasNextItems();
		}
	}
}
#endif