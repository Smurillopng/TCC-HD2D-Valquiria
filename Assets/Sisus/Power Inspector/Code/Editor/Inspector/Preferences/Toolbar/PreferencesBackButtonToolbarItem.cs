#if !POWER_INSPECTOR_LITE
using UnityEngine;
using Sisus.Attributes;

namespace Sisus
{
	[ToolbarItemFor(typeof(PreferencesToolbar), 10, ToolbarItemAlignment.Left, true)]
	public class PreferencesBackButtonToolbarItem : BackButtonToolbarItem
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
				preferencesDrawer.SetPreviousViewActive(false);
			}
			else
			{
				inspector.State.ViewIsLocked = true;
				inspector.RebuildDrawers(inspector.Preferences);
			}
			
			return true;
		}

		/// <inheritdoc/>
		protected override bool CanBeActivated()
		{
			var preferencesDrawer = inspector.State.drawers.First() as IAssetWithSideBarDrawer;
			return preferencesDrawer == null || preferencesDrawer.ActiveViewIndex > 0;
		}
	}
}
#endif