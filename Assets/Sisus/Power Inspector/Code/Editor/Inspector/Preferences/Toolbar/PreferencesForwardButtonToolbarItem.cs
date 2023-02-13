#if !POWER_INSPECTOR_LITE
using UnityEngine;
using Sisus.Attributes;

namespace Sisus
{
	[ToolbarItemFor(typeof(PreferencesToolbar), 20, ToolbarItemAlignment.Left, true)]
	public class PreferencesForwardButtonToolbarItem : ForwardButtonToolbarItem
	{
		/// <inheritdoc/>
		public override string DocumentationPageUrl
		{
			get
			{
				return "";
			}
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

			var preferencesDrawer = inspector.State.drawers.First() as IAssetWithSideBarDrawer;
			if(preferencesDrawer != null)
			{
				preferencesDrawer.SetNextViewActive(false);
				return true;
			}

			return false;
		}

		/// <inheritdoc/>
		protected override bool CanBeActivated()
		{
			var preferencesDrawer = inspector.State.drawers.First() as IAssetWithSideBarDrawer;
			return preferencesDrawer != null && preferencesDrawer.ActiveViewIndex < preferencesDrawer.Views.Length - 1;
		}
	}
}
#endif