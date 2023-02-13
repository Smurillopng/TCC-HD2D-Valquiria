using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using Sisus.Attributes;
using UnityEditor;

namespace Sisus
{
	public delegate void OpeningViewMenu(ref Menu menu);

	[ToolbarItemFor(typeof(PowerInspectorToolbar), 30, ToolbarItemAlignment.Left, true)]
	public class ViewMenuToolbarItem : ToolbarItem, IMenuToolbarItem
	{
		private GUIContent label;
		private float width = 42f;
		private GUIStyle guiStyle;

		private OpeningViewMenu onViewMenuOpening;

		/// <inheritdoc/>
		public override float MinWidth
		{
			get
			{
				return width;
			}
		}

		/// <inheritdoc/>
		public override float MaxWidth
		{
			get
			{
				return width;
			}
		}
		
		/// <summary>
		/// Called right before the View-menu is opened, this allows any
		/// subscriber to add more items to the menu that will pop open.
		/// </summary>
		/// <value>
		/// A reference to the menu that is opening.
		/// </value>
		public OpeningViewMenu OnViewMenuOpening
		{
			get
			{
				return onViewMenuOpening;
			}
			
			set
			{
				onViewMenuOpening = value;
				
				// temp fix for a bug causing onViewMenuOpening to get filled with many entries
				if(onViewMenuOpening != null)
				{
					var invocationList = onViewMenuOpening.GetInvocationList();
					int count = invocationList.Length;
					if(invocationList.Length > 1)
					{
						#if DEV_MODE
						Debug.LogWarning(GetType().Name+ ".OnViewMenuOpening = "+StringUtils.ToString(onViewMenuOpening));
						#endif

						var hashSet = new HashSet<KeyValuePair<Type, MethodInfo>>();
						
						for(int n = count - 1; n >= 0; n--)
						{
							var item = invocationList[n];
							var target = item.Target;
							var method = item.Method;

							if(target == null)
							{
								if(!method.IsStatic)
								{
									#if DEV_MODE
									Debug.LogWarning(GetType().Name+ ".OnViewMenuOpening.set - removing item "+(n+1)+"/"+count+" \""+method.Name+" from invocation list because target was null and method was not static.");
									#endif

									invocationList = invocationList.RemoveAt(n);
									continue;
								}

								var addStatic = new KeyValuePair<Type, MethodInfo>(null, method);

								if(hashSet.Contains(addStatic))
								{
									#if DEV_MODE
									Debug.LogWarning(GetType().Name+ ".OnViewMenuOpening.set - removing item "+(n+1)+"/"+count+" \""+method.Name+" (static) from invocation list it already contained it.");
									#endif
									
									invocationList = invocationList.RemoveAt(n);
									continue;
								}
								hashSet.Add(addStatic);
								continue;
							}
							
							var addInstance = new KeyValuePair<Type, MethodInfo>(null, method);
							if(hashSet.Contains(addInstance))
							{
								#if DEV_MODE
								Debug.LogWarning(GetType().Name+ ".OnViewMenuOpening.set - removing item "+(n+1)+"/"+count+" \""+StringUtils.ToString(item)+ "  from invocation list because it already contained it.");
								int countWas = invocationList.Length;
								#endif
								
								invocationList = invocationList.RemoveAt(n);

								#if DEV_MODE && PI_ASSERTATIONS
								Debug.Assert(invocationList.Length == countWas - 1, "invocationList.Length is "+invocationList.Length+" when it should be "+(countWas - 1));
								#endif

								continue;
							}

							hashSet.Add(addInstance);
						}
					}
				}
			}
		}

		/// <inheritdoc/>
		public override string DocumentationPageUrl
		{
			get
			{
				return PowerInspectorDocumentation.GetFeatureUrl("view-menu");
			}
		}

		/// <inheritdoc/>
		protected override void Setup()
		{
			guiStyle = InspectorPreferences.Styles.ToolbarMenu;

			label = inspector.Preferences.labels.ViewMenu;

			var size = guiStyle.CalcSize(label);
			width = size.x;			

			#if DEV_MODE
			if(OnViewMenuOpening != null) { Debug.Log(GetType().Name+".OnViewMenuOpening before cleared during Setup: "+StringUtils.ToString(OnViewMenuOpening)); }
			#endif

			OnViewMenuOpening = null;
		}

		/// <inheritdoc/>
		protected override void OnRepaint(Rect itemPosition)
		{
			GUI.Label(itemPosition, label, guiStyle);
		}

		/// <inheritdoc/>
		protected override bool OnActivated(Event inputEvent, bool isClick)
		{
			return OpenContextMenu(inputEvent, inputEvent.control);
		}

		/// <inheritdoc/>
		public override bool OnRightClick(Event inputEvent)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(!inspector.IgnoreToolbarMouseInputs(), GetType().Name+ ".HandleOnBeingActivated called with IgnoreToolbarMouseInputs "+StringUtils.True);
			Debug.Assert(inputEvent != null);
			Debug.Assert(inputEvent.type != EventType.Used);
			#endif

			if(HandleOnBeingActivated(inputEvent, ActivationMethod.ExpandedContextMenu))
			{
				return true;
			}

			return OpenContextMenu(inputEvent, true);
		}

		/// <inheritdoc/>
		protected override void BuildContextMenu(ref Menu menu, bool extendedMenu)
		{
			menu.Add("Debug Mode+/Off", "Disable Debug Mode For All Inspected Targets", inspector.DisableDebugMode, !inspector.State.DebugMode);
			menu.Add("Debug Mode+/On", "Enable Debug Mode For All Inspected Targets", inspector.EnableDebugMode, inspector.State.DebugMode);
			
			menu.AddSeparatorIfNotRedundant();

			menu.Add("Multi-Editing/Merged", EnableMergedMultiEditMode, UserSettings.MergedMultiEditMode);
			menu.Add("Multi-Editing/Stacked", DisableMergedMultiEditMode, !UserSettings.MergedMultiEditMode);

			menu.Add("Help/Documentation", PowerInspectorDocumentation.Show);
			menu.Add("Help/Forum", OpenUrlFromContextMenu, "https://forum.unity.com/threads/released-power-inspector-full-inspector-overhaul.736022/");
			menu.AddSeparator("Help/");
			menu.Add("Help/Toolbar/Toolbar", PowerInspectorDocumentation.ShowFeature, "toolbar");
			menu.Add("Help/Toolbar/Back And Forward Buttons", PowerInspectorDocumentation.ShowFeature, "back-and-forward-buttons");
			menu.Add("Help/Toolbar/View Menu", PowerInspectorDocumentation.ShowFeature, "view-menu");
			menu.Add("Help/Toolbar/Search Box", PowerInspectorDocumentation.ShowFeature, "search-box");
			menu.Add("Help/Toolbar/Split View", PowerInspectorDocumentation.ShowFeature, "split-view");
			menu.Add("Help/Features/Copy-Paste", PowerInspectorDocumentation.ShowFeature, "copy-paste");
			menu.Add("Help/Features/Reset", PowerInspectorDocumentation.ShowFeature, "reset");
			menu.Add("Help/Features/Context Menu", PowerInspectorDocumentation.ShowFeature, "context-menu-items");
			menu.Add("Help/Features/Debug Mode+", PowerInspectorDocumentation.ShowFeature, "debug-mode");
			menu.Add("Help/Features/Display Anything", PowerInspectorDocumentation.ShowFeature, "display-anything");
			menu.Add("Help/Features/Hierarchy Folders", PowerInspectorDocumentation.ShowFeature, "hierarchy-folders");
			menu.Add("Help/View Modes/Target Window", PowerInspectorDocumentation.ShowFeature, "target-window");
			menu.Add("Help/View Modes/Multi-Editing Modes", PowerInspectorDocumentation.ShowFeature, "multi-editing-modes");
			
			var rootDrawer = inspector.State.drawers.Members;
			for(int n = 0, count = rootDrawer.Length; n < count; n++)
			{
				var root = rootDrawer[n] as IRootDrawer;
				if(root != null)
				{
					root.AddItemsToOpeningViewMenu(ref menu);
					//break; //UPDATE: multiple drawers adding items to the menu should now be supported
				}
			}

			menu.AddSeparator();

			menu.Add("Preferences", PowerInspectorPreferences.OpenIfNotOpenAndFocus);

			menu.AddSeparator();
			menu.MoveCategoryToBottom("Help/");
			menu.AddSeparator("Help/");
			menu.Add("Help/Troubleshooting/Troubleshooting Documentation", PowerInspectorDocumentation.Show, "category/troubleshooting/");
			menu.Add("Help/Troubleshooting/Issue Tracker", OpenUrlFromContextMenu, "https://github.com/SisusCo/Power-Inspector/issues");
			
			if(extendedMenu)
			{
				#if DEV_MODE
				menu.AddSeparator();
				menu.Add("Debugging/Headless Mode", () => UnityObjectDrawerUtility.HeadlessMode = !UnityObjectDrawerUtility.HeadlessMode, UnityObjectDrawerUtility.HeadlessMode);
				#endif

				menu.AddSeparator();

				var windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
				var self = inspector.InspectorDrawer as EditorWindow;

				menu.Add("EditorWindow Debugger/this (" + self.GetType().Name + ")", ()=>PowerInspectorWindowUtility.OpenWindowNowOrNextLayout(self, true, false));

				for(int n = windows.Length - 1; n >= 0; n--)
				{
					var window = windows[n];
					if(window == self)
					{
						continue;
					}
					menu.AddEvenIfDuplicate("EditorWindow Debugger/" + window.GetType().Name, ()=>
					{
						inspector.State.ViewIsLocked = true;
						inspector.RebuildDrawers(ArrayPool<UnityEngine.Object>.CreateWithContent(window), true);
					});
				}
			}
		}

		private void DisableMergedMultiEditMode()
		{
			UserSettings.MergedMultiEditMode = false;
			inspector.ForceRebuildDrawers();
		}

		private void EnableMergedMultiEditMode()
		{
			UserSettings.MergedMultiEditMode = true;
			inspector.ForceRebuildDrawers();
		}


		private static void OpenUrlFromContextMenu(object url)
		{
			Application.OpenURL((string)url);
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