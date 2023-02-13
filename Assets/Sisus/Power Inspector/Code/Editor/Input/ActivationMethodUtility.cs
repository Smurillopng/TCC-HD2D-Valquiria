using UnityEngine;

namespace Sisus
{
	public static class ActivationMethodUtility
	{
		public static ActivationMethod GetForClick(Event clickEvent)
		{
			switch(clickEvent.button)
			{
				case 0:
					return ActivationMethod.LeftClick;
				case 1:
					return clickEvent.control ? ActivationMethod.ExpandedContextMenu : ActivationMethod.RightClick;
				case 2:
					return ActivationMethod.MiddleClick;
				default:
					#if DEV_MODE
					Debug.LogWarning("ActivationMethodUtility..GetForClick returning Undetermined because button was "+clickEvent.button);
					#endif
					return ActivationMethod.Undetermined;
			}
		}

		public static ActivationMethod GetForKeyboardEvent(Event keyboardEvent)
		{
			switch(keyboardEvent.keyCode)
			{
				case KeyCode.Menu:
					return keyboardEvent.control ? ActivationMethod.ExpandedContextMenu : ActivationMethod.KeyboardMenu;
				default:
					return ActivationMethod.KeyboardActivate;
			}
		}
	}
}