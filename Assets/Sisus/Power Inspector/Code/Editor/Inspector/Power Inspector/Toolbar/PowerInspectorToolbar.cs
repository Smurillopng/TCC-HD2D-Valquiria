//#define DEBUG_KEYBOARD_INPUT

#if !POWER_INSPECTOR_LITE
using UnityEngine;
using Sisus.Attributes;

namespace Sisus
{
	[ToolbarFor(typeof(PowerInspector), true)]
	public sealed class PowerInspectorToolbar : InspectorToolbar
	{
		#if UNITY_2019_3_OR_NEWER
		public const float DefaultToolbarHeight = 20f;
		#else
		public const float DefaultToolbarHeight = 18f;
		#endif
		
		public readonly float ToolbarHeight = DefaultToolbarHeight;
		
		/// <inheritdoc/>
		public override float Height
		{
			get
			{
				return ToolbarHeight;
			}
		}

		public PowerInspectorToolbar() : base()
		{
			ToolbarHeight = DefaultToolbarHeight;
		}

		public PowerInspectorToolbar(float setHeight) : base()
		{
			ToolbarHeight = setHeight;
		}
		
		/// <inheritdoc/>
		public override bool OnKeyboardInputGiven(Event inputEvent, KeyConfigs keys)
		{
			#if DEV_MODE && DEBUG_KEYBOARD_INPUT
			Debug.Log("OnKeyboardInputGiven("+StringUtils.ToString(inputEvent)+ ") with EditingTextField="+DrawGUI.EditingTextField);
			#endif

			if(keys.prevComponent.DetectAndUseInput(inputEvent))
			{
				var backButton = GetVisibleItem<BackButtonToolbarItem>();
				if(backButton != null)
				{
					return backButton.OnKeyboardInputGiven(inputEvent, keys);
				}
			}

			if(keys.nextComponent.DetectAndUseInput(inputEvent))
			{
				var forwardButton = GetVisibleItem<ForwardButtonToolbarItem>();
				if(forwardButton != null)
				{
					return forwardButton.OnKeyboardInputGiven(inputEvent, keys);
				}
			}

			return base.OnKeyboardInputGiven(inputEvent, keys);
		}
	}
}
#endif