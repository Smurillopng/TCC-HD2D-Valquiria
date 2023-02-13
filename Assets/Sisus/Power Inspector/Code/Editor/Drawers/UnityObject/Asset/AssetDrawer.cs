using System;
using System.IO;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Sisus
{
	/// <summary>
	/// Drawer representing UnityEngine.Object targets that cannot exist in the scene hierarchy, meaning
	/// all except for Components and GameObjects. Most commonly used for ScriptableObjects.
	/// 
	/// As opposed to CustomEditorAssetDrawer, this does not use Custom Editors when drawing the members
	/// of the Object, but uses Reflection to get members and builds Drawer for handling drawing them.
	/// </summary>
	[Serializable]
	public class AssetDrawer : UnityObjectDrawer<AssetDrawer, Object>, IEditorlessAssetDrawer, IOnProjectOrHierarchyChanged
	{
		private GUIContent headerSubtitle = new GUIContent();

		private float headerHeight = EditorGUIDrawer.AssetTitlebarHeightWithOneButtonRow;
		private bool headerHeightDetermined;

		private GUIContent[] assetLabels;
		private GUIContent[] assetLabelsOnlyOnSomeTargets;

		/// <inheritdoc/>
		public virtual bool WantsSearchBoxDisabled
		{
			get
			{
				return false;
			}
		}

		#if UNITY_EDITOR
		/// <inheritdoc />
		public GUIContent[] AssetLabels
		{
			get
			{
				return assetLabels;
			}
		}

		/// <inheritdoc />
		public GUIContent[] AssetLabelsOnlyOnSomeTargets
		{
			get
			{
				return assetLabelsOnlyOnSomeTargets;
			}
		}
		#endif

		/// <inheritdoc/>
		protected sealed override float HeaderToolbarIconWidth
		{
			get
			{
				return AssetHeaderToolbarIconWidth;
			}
		}

		/// <inheritdoc/>
		protected sealed override float HeaderToolbarIconHeight
		{
			get
			{
				return AssetHeaderToolbarIconHeight;
			}
		}

		#if UNITY_EDITOR
		/// <inheritdoc />
		protected override MonoScript MonoScript
		{
			get
			{
				var target = Target as ScriptableObject;
				return target == null ? null : MonoScript.FromScriptableObject(target);
			}
		}
		#endif
		
		#if UNITY_EDITOR
		/// <inheritdoc />
		protected override bool IsAsset
		{
			get
			{
				return true;
			}
		}
		#endif

		/// <inheritdoc cref="IUnityObjectDrawer.HeaderHeight" />
		public override float HeaderHeight
		{
			get
			{
				return headerHeight;
			}
		}

		/// <inheritdoc cref="IParentDrawer.Foldable" />
		public override bool Foldable
		{
			get
			{
				return false;
			}
		}

		/// <inheritdoc cref="IParentDrawer.Unfoldedness" />
		public override float Unfoldedness
		{
			get
			{
				return 1f;
			}
		}

		/// <inheritdoc cref="IParentDrawer.Unfolded" />
		public override bool Unfolded
		{
			get
			{
				return true;
			}

			set { throw new NotSupportedException("Unfolded state of assets can't be altered"); }
		}

		/// <inheritdoc />
		protected override Color PrefixBackgroundColor
		{
			get
			{
				return inspector.Preferences.theme.AssetHeaderBackground;
			}
		}

		/// <inheritdoc />
		protected override float ToolbarIconsTopOffset
		{
			get
			{
				return AssetToolbarIconsTopOffset;
			}
		}

		/// <inheritdoc />
		protected sealed override float HeaderToolbarIconsRightOffset
		{
			get
			{
				return AssetHeaderToolbarIconsRightOffset;
			}
		}

		/// <inheritdoc />
		protected sealed override float HeaderToolbarIconsOffset
		{
			get
			{
				return AssetHeaderToolbarIconsOffset;
			}
		}

		/// <summary>
		/// Should the initial asset header height estimate be based on headeer that contains two rows of buttons?
		/// </summary>
		protected virtual bool HeaderHasTwoRowsOfButtons
		{
			get
			{
				return AddressablesUtility.IsInstalled;
			}
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="targets"> The targets that the drawers represent. Can not be null. </param>
		/// <param name="parent"> The parent drawers of the created drawers. Can be null. </param>
		/// <param name="inspector"> The inspector in which the IDrawer are contained. Can not be null. </param>
		/// <returns> The drawer instance, ready to be used. </returns>
		public static AssetDrawer Create([NotNull]Object[] targets, [CanBeNull]IParentDrawer parent, [NotNull]IInspector inspector)
		{
			AssetDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new AssetDrawer();
			}
			result.Setup(targets, parent, null, inspector);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc cref="IDrawer.LateSetup" />
		public override void LateSetup()
		{
			base.LateSetup();
			GetHeaderSubtitle(ref headerSubtitle);
		}

		/// <inheritdoc/>
		public void SetupInterface(Object[] setTargets, IParentDrawer setParent, IInspector setInspector)
		{
			Setup(setTargets, setParent, null, setInspector);
		}

		/// <inheritdoc />
		protected override void Setup(Object[] setTargets, IParentDrawer setParent, GUIContent setLabel, IInspector setInspector)
		{
			headerHeight = DrawGUI.Active.AssetTitlebarHeight(HeaderHasTwoRowsOfButtons);
			headerHeightDetermined = false;

			base.Setup(setTargets, setParent, setLabel, setInspector);

			#if UNITY_EDITOR
			Sisus.AssetLabels.Get(targets, ref assetLabels, ref assetLabelsOnlyOnSomeTargets);
			Sisus.AssetLabels.OnAssetLabelsChanged += OnAssetLabelsChanged;
			#endif
		}

		protected override GUIContent GenerateLabel(Type type)
		{
			return GUIContentPool.Create(type.Name);
		}

		private void OnAssetLabelsChanged(Object[] labelsChangedforTargets)
		{
			if(labelsChangedforTargets.ContentsMatch(targets))
			{
				Sisus.AssetLabels.Get(targets, ref assetLabels, ref assetLabelsOnlyOnSomeTargets);
			}
		}

		protected override void DrawHeaderBase(Rect position)
		{
			HandlePrefixHighlightingForFilter(position, 55f, 4f);
			if(editor == null)
			{
				Editors.GetEditor(ref editor, Target);
			}
			Rect drawnRect;
			bool setHeaderHeightDetermined = headerHeightDetermined;
			try
			{
				drawnRect = EditorGUIDrawer.AssetHeader(position, editor, ref headerHeightDetermined);
			}
			#if DEV_MODE
			catch(MissingReferenceException e) // asset has probably been moved or destroyed
			{
				Debug.LogError(e);
			#else
			catch(MissingReferenceException)
			{
			#endif
				inspector.ForceRebuildDrawers();
				ExitGUIUtility.ExitGUI();
				return;
			}

			if(!headerHeightDetermined && setHeaderHeightDetermined)
			{
				headerHeightDetermined = true;

				if(headerHeight != drawnRect.height)
				{
					if(drawnRect.height >= DrawGUI.SingleLineHeight)
					{
						#if DEV_MODE
						Debug.Log(ToString()+".headerHeight = "+drawnRect.height);
						#endif

						headerHeight = drawnRect.height;
					}
					else
					{
						#if DEV_MODE
						Debug.Log(ToString()+".headerHeight = "+DrawGUI.Active.AssetTitlebarHeight(HeaderHasTwoRowsOfButtons));
						#endif

						headerHeight = DrawGUI.Active.AssetTitlebarHeight(HeaderHasTwoRowsOfButtons);
					}
				}
			}
		}
		
		/// <inheritdoc/>
		protected override Object[] FindObjectsOfType()
		{
			return Resources.FindObjectsOfTypeAll(Type);
		}

		/// <inheritdoc/>
		protected override void NameByType()
		{
			#if UNITY_EDITOR
			Undo.RecordObjects(targets, "Auto-Name");
			#endif

			var target = targets[0];
			if(target != null)
			{
				var typeName = target.GetType().Name;
				for(int n = targets.Length - 1; n >= 0; n--)
				{
					target = targets[n];

					#if UNITY_EDITOR
					EditorUtility.SetDirty(target);
					#endif

					target.name = typeName;
				}
			}
		}

		/// <inheritdoc/>
		protected override void FindReferencesInScene()
		{
			DrawGUI.ExecuteMenuItem("Assets/Find References In Scene");
		}

		/// <inheritdoc cref="IDrawer.Duplicate" />
		public override void Duplicate()
		{
			#if UNITY_EDITOR
			AssetDrawerUtility.Duplicate(targets);
			#endif
		}

		/// <summary> Sets subtitle text and tooltip for display below the main header text. </summary>
		/// <param name="subtitle"> [in,out] The subtitle GUContent to set. This cannot be null. </param>
		private void GetHeaderSubtitle([NotNull]ref GUIContent subtitle)
		{
			var type = Type;
			if(type == null)
			{
				subtitle.text = "Object";
				return;
			}

			#if UNITY_EDITOR
			string localPath = AssetDatabase.GetAssetPath(Target);
			bool hasAssetPath = localPath.Length > 0;
			#endif

			#if UNITY_EDITOR
			if(type == Types.DefaultAsset)
			{
				// for default assets generate subtitle from file extensions if possible
				string extension = hasAssetPath ? Path.GetExtension(localPath) : "";
				if(extension.Length > 1)
				{
					subtitle.text = FileExtensionToWords(extension.Substring(1));
				}
				else
				{
					subtitle.text = "Default Asset";
					return;
				}
			}
			else
			// if Type is not DefaultAsset, we can generate subtitle from type
			#endif
			{
				subtitle.text = StringUtils.SplitPascalCaseToWords(StringUtils.ToStringSansNamespace(type));
			}
			
			#if UNITY_EDITOR
			// Add "Asset" suffix if target is an asset with a file path
			if(hasAssetPath && !subtitle.text.EndsWith("Asset", StringComparison.Ordinal))
			{
				subtitle.text = string.Concat(subtitle.text, " Asset");
			}
			#endif
		}

		private static string FileExtensionToWords(string extensionWithoutDot)
		{
			return StringUtils.SplitPascalCaseToWords(extensionWithoutDot);
		}
		
		/// <inheritdoc />
		public override bool DrawPrefix(Rect position)
		{
			// UPDATE:
			// Don't grey out header of uneditable assets, just the body.
			// This matches better how the default inspector does it.
			var guiColorWas = GUI.color;
			if(DrawGreyedOut)
			{
				var color = GUI.color;
				color.a = 1f;
				GUI.color = color;
			}

			bool dirty = base.DrawPrefix(position);
			DrawSubtitle(position);

			GUI.color = guiColorWas;

			return dirty;
		}

		/// <summary> Draw subtitle text below the main header text. </summary>
		/// <param name="headerRect"> The draw rect of the whole header. </param>
		private void DrawSubtitle(Rect headerRect)
		{
			var subtitleRect = headerRect;
			subtitleRect.height = DrawGUI.SingleLineHeight;
			subtitleRect.y += 22f;
			float iconOffset = 43f;
			subtitleRect.x += iconOffset;
			subtitleRect.width -= iconOffset;
			float removeFromRight = HeaderButtonsWidth + HeaderButtonsPadding;
			if(removeFromRight < InternalOpenButtonWidth)
			{
				removeFromRight = InternalOpenButtonWidth;
			}
			subtitleRect.width -= removeFromRight;

			if(subtitleRect.width > 0f)
			{
				GUI.Label(subtitleRect, headerSubtitle, InspectorPreferences.Styles.SubHeader);
			}

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(HeaderHeight >= subtitleRect.yMax - headerRect.y);
			Debug.Assert(headerRect.xMax >= subtitleRect.xMax);
			Debug.Assert(!HeadlessMode);
			#endif
		}

		/// <inheritdoc cref="IDrawer.BuildRightClickMenu" />
		protected override void BuildContextMenu(ref Menu menu, bool extendedMenu)
		{
			if(BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}

			#if UNITY_EDITOR
			string localPath = AssetDatabase.GetAssetPath(Target);
			if(localPath.Length > 0)
			{
				string fullPath = FileUtility.LocalToFullPath(localPath);
				if(fullPath.Length > 0)
				{
					menu.AddSeparatorIfNotRedundant();
					menu.Add("Disable", Disable);
				}
			}
			#endif

			#if UNITY_EDITOR
			if(extendedMenu && Editable)
			{
				menu.AddSeparatorIfNotRedundant();
				menu.Add("Set Dirty", () =>
				{
					foreach(var target in targets)
					{
						EditorUtility.SetDirty(target);
					}
				});
			}
			#endif

			if(!BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}
		}

		#if UNITY_EDITOR
		/// <summary> Disables the target ScriptableObject asset by appending a supernumerary extension (like ".disabled" or ".tmp") to their filenames. </summary>
		private void Disable()
		{
			FileUtility.Disable(targets, inspector.Preferences.disabledScriptExtension);
		}
		#endif

		/// <inheritdoc cref="IDrawer.Dispose" />
		public override void Dispose()
		{
			#if UNITY_EDITOR
			GUIContentArrayPool.Dispose(ref assetLabels);
			Sisus.AssetLabels.OnAssetLabelsChanged -= OnAssetLabelsChanged;
			#endif

			base.Dispose();
		}

		/// <inheritdoc/>
		public void OnProjectOrHierarchyChanged(OnChangedEventSubject changed, ref bool hasNullReferences)
		{
			#if DEV_MODE
			Debug.Log(ToString()+ ".OnProjectOrHierarchyChanged("+ changed + ", ref "+ hasNullReferences+")...");
			#endif

			if(changed != OnChangedEventSubject.Project && changed != OnChangedEventSubject.Undefined)
			{
				return;
			}

			for(int n = targets.Length - 1; n >= 0; n--)
			{
				var target = targets[n];
				if(target == null)
				{
					if(UnityObjectExtensions.TryToFixNull(ref target))
					{
						#if DEV_MODE
						Debug.LogWarning(ToString()+".OnHierarchyChanged fixed targets["+n+"] (\""+target.name+"\") being null.");
						#endif
						continue;
					}
					
					#if DEV_MODE
					Debug.Log(ToString()+".OnHierarchyChanged targets["+n+"] was null and could not be fixed.");
					#endif

					hasNullReferences = true;
				}
			}

			#if UNITY_EDITOR
			if(editor != null && Editors.DisposeIfInvalid(ref editor))
			{
				hasNullReferences = true; // set to true so that drawers get rebuilt, in case e.g. asset paths have changed.
			}
			#endif
		}
	}
}