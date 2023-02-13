using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Sisus
{
	/// <summary>
	/// Drawer for a tab bar.
	/// </summary>
	[Serializable]
	public sealed class TabBarDrawer : BaseDrawer
	{
		private int selectedTab;
		private bool readOnly;

		/// <summary>
		/// The labels to display on the tabs.
		/// </summary>
		private GUIContent[] tabLabels;
		
		/// <inheritdoc/>
		public override Type Type
		{
			get
			{
				return typeof(int);
			}
		}

		/// <inheritdoc/>
		public override bool ReadOnly
		{
			get
			{
				return base.ReadOnly || readOnly;
			}

			set
			{
				readOnly = value;
			}
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="selectedTab"> Zero-based index of the currently selected tab. </param>
		/// <param name="tabLabels"> The labels for the tabs on the tab bar. </param>
		/// <param name="onSelectedTabChanged"> Delegate to invoke when selected tab changes, with parameter value being index of newly selected tab. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="readOnly"> True if tabs should be greyed out and not be interactive. </param>
		/// <returns> The drawer instance, ready to be used. </returns>
		public static TabBarDrawer Create(int selectedTab, GUIContent[] tabLabels, OnValueChanged onSelectedTabChanged, IParentDrawer parent, bool readOnly)
		{
			TabBarDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new TabBarDrawer();
			}
			result.Setup(selectedTab, tabLabels, onSelectedTabChanged, parent, readOnly);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc/>
		protected override void Setup(IParentDrawer setParent, GUIContent setLabel)
		{
			throw new NotSupportedException("Use the other Setup method.");
		}

		/// <summary>
		/// Sets up the drawer so that it is ready to be used.
		/// </summary>
		/// <param name="setSelectedTab"> Zero-based index of the currently selected tab. </param>
		/// <param name="setTabLabels"> The labels for the tabs on the tab bar. </param>
		/// <param name="setOnSelectedTabChanged"> Delegate to invoke when selected tab changes, with parameter value being index of newly selected tab. </param>
		/// <param name="setParent"> Drawer whose member these Drawer. </param>
		/// <param name="setReadOnly"> True if button should be greyed out and not be interactive. </param>
		private void Setup(int setSelectedTab, [NotNull]GUIContent[] setTabLabels, [CanBeNull]OnValueChanged setOnSelectedTabChanged, [CanBeNull]IParentDrawer setParent, bool setReadOnly)
		{
			#if DEV_MODE
			Debug.Assert(tabLabels.Length > 0);
			#endif

			selectedTab = setSelectedTab;
			tabLabels = setTabLabels;
			OnValueChanged = setOnSelectedTabChanged;
			readOnly = setReadOnly;
			Setup(setParent, GUIContent.none);
		}

		/// <inheritdoc/>
		public override bool SetValue(object newValue)
		{
			return SelectTab((int)newValue);
		}

		private bool SelectTab(int setSelectedTab)
		{
			if(selectedTab != setSelectedTab)
			{
				selectedTab = setSelectedTab;

				OnValidate();

				if(OnValueChanged != null)
				{
					OnValueChanged(this, setSelectedTab);
				}
				return true;
			}
			return false;
		}
		
		/// <inheritdoc/>
		public override object GetValue(int index)
		{
			return selectedTab;
		}

		/// <inheritdoc/>
		public override bool DrawPrefix(Rect position)
		{
			return SelectTab(DrawGUI.Active.Toolbar(position, selectedTab, tabLabels));
		}

		/// <inheritdoc/>
		protected override void BuildContextMenu(ref Menu menu, bool extendedMenu)
		{
			#if !POWER_INSPECTOR_LITE
			menu.Add("Copy", CopyToClipboard);
			
			if(CanPasteFromClipboard())
			{
				int setSelectedTab = Clipboard.Paste<int>();
				if(setSelectedTab >= 0 && setSelectedTab < tabLabels.Length)
				{
					menu.Add("Paste", PasteFromClipboard);
				}
			}
			#endif
		}

		/// <inheritdoc/>
		public override bool CanPasteFromClipboard()
		{
			return Clipboard.CanPasteAs(Types.Int);
		}

		/// <inheritdoc/>
		public override bool OnKeyboardInputGiven(Event inputEvent, KeyConfigs keys)
		{
			switch(inputEvent.keyCode)
			{
				case KeyCode.LeftArrow:
					DrawGUI.Use(inputEvent);	
					int select = selectedTab - 1;
					if(select >= 0)
					{
						GUI.changed = true;
						SelectTab(select);
					}
					return true;
				case KeyCode.RightArrow:
					DrawGUI.Use(inputEvent);
					select = selectedTab + 1;
					if(select < tabLabels.Length)
					{
						GUI.changed = true;
						SelectTab(select);
					}
					return true;
				case KeyCode.KeypadEnter:
					DrawGUI.Use(inputEvent);
					select = selectedTab + 1;
					if(select >= tabLabels.Length)
					{
						select = 0;
					}
					GUI.changed = true;
					SelectTab(select);
					return true;
			}

			return base.OnKeyboardInputGiven(inputEvent, keys);
		}

		/// <inheritdoc/>
		protected override void DoRandomize()
		{
			SelectTab(UnityEngine.Random.Range(0, tabLabels.Length));
		}
	}
}