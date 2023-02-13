using JetBrains.Annotations;
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sisus
{
	/// <summary>
	/// Window responsible for drawing Power Inspector preferences view.
	/// </summary>
	public sealed class PowerInspectorPreferencesWindow : InspectorDrawerWindow<PowerInspectorPreferencesWindow, PreferencesInspector>
	{
		[CanBeNull]
		public static PreferencesDrawer GetExistingWindow()
		{
			var manager = InspectorUtility.ActiveManager;
			if(manager == null)
			{
				return null;
			}

			var preferencesInspector = (PreferencesInspector)manager.GetLastSelectedInspector(typeof(PreferencesInspector));
			if(preferencesInspector == null)
			{
				return null;
			}
			
			return GetPrefencesDrawer(preferencesInspector);
		}

		[CanBeNull]
		private static PreferencesDrawer GetPrefencesDrawer(IInspectorDrawer preferencesWindow)
		{
			return GetPrefencesDrawer(preferencesWindow.MainView);
		}

		[CanBeNull]
		private static PreferencesDrawer GetPrefencesDrawer([NotNull]IInspector preferencesInspector)
		{
			var drawers = preferencesInspector.State.drawers;
			return drawers.Length == 1 ? preferencesInspector.State.drawers[0] as PreferencesDrawer : null;
		}

		/// <summary>
		/// Focuses existing preferences window if one is open, otherwise opens a new window and focuses that.
		/// </summary>
		public static void Open()
		{
			GetExistingOrCreateNewWindow();
		}

		[CanBeNull]
		public static PreferencesDrawer GetExistingOrCreateNewWindow()
		{
			var inspectorManager = InspectorUtility.ActiveManager;
			if(inspectorManager != null)
			{
				var preferencesInstance = (PowerInspectorPreferencesWindow)inspectorManager.GetLastSelectedInspectorDrawer(typeof(PowerInspectorPreferencesWindow));
				if(preferencesInstance != null)
				{
					#if DEV_MODE && PI_ASSERTATIONS
					Debug.Assert((object)preferencesInstance != null);
					#endif

					#if DEV_MODE
					Debug.Log("Using existing preferences window: "+preferencesInstance);
					#endif

					preferencesInstance.FocusWindow();
					return GetPrefencesDrawer(preferencesInstance);
				}
			}

			if(Event.current == null)
			{
				#if DEV_MODE
				Debug.LogWarning("Opening preferences window on next OnGUI. Returning null");
				#endif

				DrawGUI.OnNextBeginOnGUI(Open, true);
				return null;
			}

			#if DEV_MODE
			Debug.Log("Opening preferences window now!");
			#endif

			var minSize = PreferencesDrawer.GetExpectedMinSize();
			var created = CreateNew<PowerInspectorPreferencesWindow>("Preferences", ArrayPool<Object>.CreateWithContent(PowerInspector.GetPreferences()), true, false, minSize, minSize);
			return GetPrefencesDrawer(created);
		}

		/// <inheritdoc/>
		public override bool CanSplitView
		{
			get
			{
				return false;
			}
		}

		/// <inheritdoc/>
		public override Type InspectorType
		{
			get
			{
				return typeof(PreferencesInspector);
			}
		}

		/// <inheritdoc/>
		protected override string TitleText
		{
			get
			{
				return "Preferences";
			}
		}

		/// <inheritdoc/>
		protected override void Setup(bool lockView, Vector2 scrollPos, float minWidth = 280f, float minHeight = 130f, float maxWidth = 0f, float maxHeight = 0f)
		{
			var preferences = GetPreferences();

			#if DEV_MODE && PI_ASSERTATIONS
			var minSize = PreferencesDrawer.GetExpectedMinSize();
			Debug.Assert(minWidth <= 0f || minWidth == minSize.x, StringUtils.ToString(minWidth));
			Debug.Assert(minHeight <= 0f || minHeight == minSize.y, StringUtils.ToString(minHeight));
			Debug.Assert(maxWidth <= 0f || maxWidth == minSize.x, StringUtils.ToString(maxWidth));
			Debug.Assert(maxHeight <= 0f || maxHeight == minSize.y, StringUtils.ToString(maxHeight));
			#endif

			base.Setup(true, scrollPos, minWidth, minHeight, maxWidth, maxHeight);
			SelectionManager.Select(preferences);
		}
		
		/// <inheritdoc/>
		protected override ISelectionManager GetDefaultSelectionManager()
		{
			return new LockedSelectionManager(GetPreferences());
		}
	}
}