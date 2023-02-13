using System;
using JetBrains.Annotations;
using Sisus.Attributes;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sisus
{
	[Serializable, DrawerForAsset(typeof(Material), false, true)]
	public class MaterialDrawer : CustomEditorAssetDrawer
	{
		/// <inheritdoc />
		public override float HeaderHeight
		{
			get
			{
				return 50f;
			}
		}

		/// <inheritdoc cref="IUnityObjectDrawer.Unfolded" />
		public override bool Unfolded
		{
			get
			{
				if(Foldable)
				{
					return unfoldedness;
				}

				#if DEV_MODE && PI_ASSERTATIONS
				if(parent == null) { Debug.LogWarning(ToString()+".Unfolded called with parent null. Was this called during Setup before parent was assigned?"); }
				#endif

				return true;
			}
		}
		
		/// <inheritdoc />
		public override float Unfoldedness
		{
			get
			{
				return Foldable ? unfoldedness : base.Unfoldedness;
			}
		}

		/// <inheritdoc cref="IUnityObjectDrawer.Foldable" />
		public override bool Foldable
		{
			get
			{
				#if DEV_MODE && PI_ASSERTATIONS
				if(parent == null) { Debug.LogWarning(ToString()+".Unfolded called with parent null. Was this called during Setup before parent was assigned?"); }
				#endif

				return parent != null && parent.Members.Length > 1;
			}
		}

		/// <inheritdoc />
		protected override Editor HeaderEditor
		{
			get
			{
				//this is needed to make the forceVisible unfolding to work
				return Editor;
			}
		}

		/// <inheritdoc />
		public override PrefixResizer PrefixResizer
		{
			get
			{
				//prefix width in MaterialInspector is static so the prefix resizer should not be shown
				return DebugMode ? PrefixResizer.TopOnly : PrefixResizer.Disabled;
			}
		}

		#if UNITY_2018_1_OR_NEWER // Presets were added in Unity 2018.1
		/// <inheritdoc />
		protected override bool HasPresetIcon
		{
			get
			{
				return Editable;
			}
		}
		#endif

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="targets"> The targets that the drawer represent. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="inspector"> The inspector in which the IDrawer are contained. Can not be null. </param>
		/// <returns> The instance, ready to be used. </returns>
		[NotNull]
		public static new MaterialDrawer Create(Object[] targets, [CanBeNull]IParentDrawer parent, [NotNull]IInspector inspector)
		{
			MaterialDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new MaterialDrawer();
			}
			result.Setup(targets, targets, typeof(MaterialEditor), parent, inspector);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc/>
		public override void OnProjectOrHierarchyChanged(OnChangedEventSubject changed, ref bool hasNullReferences)
		{
			switch(changed)
			{
				// Errors can come from the MeshRendererEditor when changing play mode for some reason.
				case OnChangedEventSubject.PlayModeChange:
					if(editor != null)
					{
						Editors.Dispose(ref editor, true);
					}
					if(assetEditor != null)
					{
						Editors.Dispose(ref assetEditor, true);
					}
					hasNullReferences = true;
					break;
				case OnChangedEventSubject.Project:
				case OnChangedEventSubject.Undefined:
					if(targets.ContainsNullObjects())
					{
						if(editor != null)
						{
							Editors.Dispose(ref editor, true);
						}
						if(assetEditor != null)
						{
							Editors.Dispose(ref assetEditor, true);
						}
						hasNullReferences = true;
					}
					break;
			}

			base.OnProjectOrHierarchyChanged(changed, ref hasNullReferences);
		}

		/// <inheritdoc />
		protected override void Setup(Object[] setTargets, Object[] setEditorTargets, Type setEditorType, IParentDrawer setParent, IInspector setInspector)
		{
			base.Setup(setTargets, setEditorTargets, setEditorType, setParent, setInspector);

			// There have been lots of issues with calling GetLastRect with the header of the MaterialDrawer, so skipping that completely.
			headerHeightDetermined = true;

			// we have to set IsVisible
			var materialEditor = Editor as MaterialEditor;
			if(materialEditor != null)
			{
				#if DEV_MODE
				Debug.Log("materialEditor not null with setEditorType="+ StringUtils.ToString(setEditorType)+ ", Editor="+StringUtils.TypeToString(Editor) + ", HeaderEditor=" + StringUtils.TypeToString(HeaderEditor));
				#endif

				#if CHANGE_MISSING_SHADER_TO_INTERNAL_ERROR_SHADER
				// If the selected shader is missing, the default inspector is drawn as if the Hidden/InternalErrorShader was selected.
				// If the shader is left null instead, then MissingArgumentException will be thrown during PropertiesGUI.
				// We can mimic the behaviour of the default inspector by changing the shader to Hidden/InternalErrorShader, but this will
				// cause the shader change to get applied in the asset too, which we don't want.
				var shaderField = typeof(MaterialEditor).GetField("m_Shader", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
				if(shaderField != null)
				{
					var shader = shaderField.GetValue(materialEditor);
					Debug.Log("m_Shader: " + (shader == null ? "null" : shader.ToString()));
					if(shader == null)
					{
						var setShader = Shader.Find("Hidden/InternalErrorShader");
						if(setShader != null)
						{
							Debug.Log("Found InternalErrorShader: " + setShader);

							materialEditor.SetShader(setShader);
							//shaderField.SetValue(materialEditor, setShader);

							/*
							var createCustomShaderGUI = typeof(MaterialEditor).GetMethod("CreateCustomShaderGUI", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
							if(createCustomShaderGUI != null)
							{
								Debug.Log("CreateInspectorGUI: "+createCustomShaderGUI);

								createCustomShaderGUI.InvokeWithParameters(materialEditor, setShader, "");
							}
							*/
						}
					}
				}
				#endif

				if(!Foldable)
				{
					for(int n = targets.Length - 1; n >= 0; n--)
					{
						var target = targets[n];
						if(target != null)
						{
							ComponentUnfoldedUtility.SetIsUnfolded(target, true);
						}
					}
				}

				#if UNITY_2019_2_OR_NEWER
				// In Unity 2019.2+ m_IsVisible field no longer exists in MaterialEditor.
				// Instead value of isVisible property is determined using Editor.firstInspectedEditor || InternalEditorUtility.GetIsInspectorExpanded(Editor.target).
				#elif UNITY_2018_3_OR_NEWER
				// In Unity 2018.3 to 2019.1 and older have to set m_IsVisible field value of the MaterialEditor to true, or otherwise calling OnInspectorGUI for the Editor has no effect.
				var isVisible = typeof(MaterialEditor).GetField("m_IsVisible", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
				if(isVisible != null)
				{
					isVisible.SetValue(Editor, true);
				}
				#if DEV_MODE
				else { Debug.LogWarning("MaterialEditor field \"m_IsVisible\" not found"); }
				#endif
				#else
				// In Unity 2018.2 and older have to set forceVisible field value of the MaterialEditor to true, or otherwise calling OnInspectorGUI for the Editor has no effect.
				var forceVisible = typeof(MaterialEditor).GetProperty("forceVisible", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
				if(forceVisible != null)
				{
					forceVisible.SetValue(Editor, true, null);
				}
				#if DEV_MODE
				else { Debug.LogWarning("MaterialEditor field \"forceVisible\" not found"); }
				#endif
				#endif
			}
			#if DEV_MODE && PI_ASSERTATIONS
			else
			{
				#if DEV_MODE
				Debug.Log("materialEditor null with setEditorType="+ StringUtils.ToString(setEditorType)+ ", Editor="+StringUtils.TypeToString(Editor) + ", HeaderEditor=" + StringUtils.TypeToString(HeaderEditor));
				#endif

				Debug.Assert(setEditorType != typeof(MaterialEditor));
			}
			#endif
		}

		/// <inheritdoc />
		public override bool DrawPrefix(Rect position)
        {
			#if UNITY_2019_2_OR_NEWER
			var hideAndBlockFoldoutArrowRect = position;
			hideAndBlockFoldoutArrowRect.height = 11f;
			hideAndBlockFoldoutArrowRect.width = 50f;
			hideAndBlockFoldoutArrowRect.y += position.height - 11f;

			if(Event.current.type == EventType.MouseDown && hideAndBlockFoldoutArrowRect.Contains(Event.current.mousePosition))
            {
				DrawGUI.Use(Event.current);
            }
			#endif

			bool foldable = Foldable;

			if(foldable)
			{
				GUILayout.BeginArea(position);
				position.x = 0f;
				position.y = 0f;
			}

			bool dirty = base.DrawPrefix(position);

			if(foldable)
			{
				GUILayout.EndArea();
				GUILayout.Space(HeaderHeight);
			}

			#if UNITY_2019_2_OR_NEWER
			var guiColorWas = GUI.color;
			GUI.color = Color.white;
			DrawGUI.Active.ColorRect(hideAndBlockFoldoutArrowRect, Preferences.theme.AssetHeaderBackground);
			GUI.color = guiColorWas;
			#endif

			return dirty;
		}

		/// <inheritdoc />
		public override void OnMouseUpAfterDownOverControl(Event inputEvent, bool isClick)
        {
			base.OnMouseUpAfterDownOverControl(inputEvent, isClick);

			// It's possible for the user to click some parts of the header to fold the material drawer (even with the blocking in place in DrawPrefix).
			// To avoid this immediately set targets back unfolded after the mouse up event
			if(!Foldable)
			{
				for(int n = targets.Length - 1; n >= 0; n--)
				{
					var target = targets[n];
					if(target != null)
					{
						OnNextLayout(()=>ComponentUnfoldedUtility.SetIsUnfolded(target, true));
					}
				}
			}
		}

        protected override void DoBuildHeaderButtons()
        {
			#if UNITY_2018_1_OR_NEWER // The Edit... button doesn't exist in older Unity versions
			if(!Editable)
			{
				var color = Color.white;
				color.a = 0.5f;
				AddHeaderButton(Button.Create(new GUIContent("Edit..."), ()=> { }, color));
			}
			else
			{
				AddHeaderButton(Button.Create(new GUIContent("Edit..."), null));
			}
			#endif
		}

        /// <inheritdoc />
        protected override void GetHeaderSubtitle(ref GUIContent subtitle)
		{
			// no subtitle text to prevent clipping with the shader dropdown element
		}

		/// <inheritdoc />
		public override void Dispose()
        {
			for(int n = targets.Length - 1; n >= 0; n--)
			{
				var target = targets[n];
				if(target != null)
				{
					ComponentUnfoldedUtility.SetIsUnfolded(target, false);
				}
			}
			base.Dispose();
		}
    }
}